using MainCore.Commands.Features.UpgradeBuilding;
using MainCore.Commands.Misc;
using MainCore.Tasks.Base;
using System.Collections.Concurrent;

namespace MainCore.Tasks
{
    [Handler]
    public static partial class UpgradeBuildingTask
    {
        // Tracks consecutive hard-failures per build target so a job that keeps failing (e.g. a build
        // page whose upgrade never registers) gets pushed to the bottom instead of blocking the queue.
        private static readonly ConcurrentDictionary<string, int> _planFails = new();
        private const int FailThreshold = 3;

        public sealed class Task : VillageTask
        {
            public Task(AccountId accountId, VillageId villageId) : base(accountId, villageId)
            {
            }

            protected override string TaskName => "Upgrade building";
        }

        private static async ValueTask<Result> HandleAsync(
            Task task,
            ILogger logger,
            IChromeBrowser browser,
            GetBuildPlanCommand.Handler getBuildPlanCommand,
            ToBuildPageCommand.Handler toBuildPageCommand,
            HandleResourceCommand.Handler handleResourceCommand,
            AddCroplandCommand.Handler addCroplandCommand,
            HandleUpgradeCommand.Handler handleUpgradeCommand,
            UpdateBuildingCommand.Handler updateBuildingCommand,
            DeprioritizeBuildJobCommand.Handler deprioritizeBuildJobCommand,
            MainCore.Commands.Navigate.ToDorfCommand.Handler toDorfCommand,
            CancellationToken cancellationToken)
        {
            Result result;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return Cancel.Error;

                var (_, isFailed, plan, errors) = await getBuildPlanCommand.HandleAsync(new(task.AccountId, task.VillageId), cancellationToken);
                if (isFailed)
                {
                    var nextExecuteErrors = errors.OfType<NextExecuteError>().OrderBy(x => x.NextExecute).ToList();
                    if (nextExecuteErrors.Count > 0)
                    {
                        task.ExecuteAt = nextExecuteErrors.Select(x => x.NextExecute).Min();
                    }

                    return Skip.Error.WithErrors(errors);
                }

                logger.Information("Build {Type} to level {Level} at location {Location}", plan.Type, plan.Level, plan.Location);

                var failKey = $"{task.VillageId.Value}-{plan.Location}-{plan.Type}";

                async ValueTask<Result> onBuildFailed(Result failed)
                {
                    var fails = _planFails.AddOrUpdate(failKey, 1, (_, v) => v + 1);
                    if (fails < FailThreshold) return failed;

                    _planFails.TryRemove(failKey, out _);
                    logger.Warning("Build {Type} at location {Location} failed {Fails} times - moving it to the bottom of the queue", plan.Type, plan.Location, fails);
                    await deprioritizeBuildJobCommand.HandleAsync(new(task.VillageId, plan.Location, plan.Type), cancellationToken);

                    if (plan.Type.IsResourceField())
                    {
                        // A resource field that repeatedly has no upgrade button/cost is almost always
                        // at this village's actual cap (e.g. non-capital fields cap below the capital
                        // max) - blacklist it so ResourceBuild stops re-minting the same dead job, and
                        // back out to the village overview so the browser isn't left on a dead-end page.
                        logger.Warning("{Type} at location {Location} looks capped for this village - excluding it from auto resource-build for a while", plan.Type, plan.Location);
                        GetBuildPlanCommand.MarkFieldCapped(task.VillageId, plan.Location);
                        await toDorfCommand.HandleAsync(new(0), CancellationToken.None);
                    }

                    return Skip.Error.WithErrors(failed.Errors);
                }

                result = await toBuildPageCommand.HandleAsync(new(task.VillageId, plan), cancellationToken);
                if (result.IsFailed) return await onBuildFailed(result);

                result = await handleResourceCommand.HandleAsync(new(task.AccountId, task.VillageId, plan), cancellationToken);
                if (result.IsFailed)
                {
                    if (result.HasError<LackOfFreeCrop>())
                    {
                        await addCroplandCommand.HandleAsync(new(task.VillageId), cancellationToken);
                        continue;
                    }

                    if (result.HasError<StorageLimit>())
                    {
                        return Stop.Error.WithErrors(result.Errors);
                    }
                    if (result.HasError<MissingResource>())
                    {
                        var time = UpgradeParser.GetTimeWhenEnoughResource(browser.Html, plan.Type);
                        task.ExecuteAt = DateTime.Now.Add(time);
                        return Skip.Error.WithErrors(result.Errors);
                    }

                    return result;
                }

                result = await handleUpgradeCommand.HandleAsync(new(task.VillageId, plan), cancellationToken);
                if (result.IsFailed) return await onBuildFailed(result);

                _planFails.TryRemove(failKey, out _);
                logger.Information("Upgrade for {Type} at location {Location} completed successfully.", plan.Type, plan.Location);

                result = await updateBuildingCommand.HandleAsync(new(task.VillageId), cancellationToken);
                if (result.IsFailed) return result;
            }
        }
    }
}