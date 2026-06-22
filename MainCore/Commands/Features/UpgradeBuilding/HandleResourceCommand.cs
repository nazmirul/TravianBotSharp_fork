using MainCore.Commands.Features.NpcResource;
using MainCore.Commands.Features.UseHeroItem;

namespace MainCore.Commands.Features.UpgradeBuilding
{
    [Handler]
    public static partial class HandleResourceCommand
    {
        public sealed record Command(AccountId AccountId, VillageId VillageId, NormalBuildPlan Plan) : IAccountVillageCommand
        {
            public void Deconstruct(out AccountId accountId, out VillageId villageId) => (accountId, villageId) = (AccountId, VillageId);
        }

        private static async ValueTask<Result> HandleAsync(
            Command command,
            UpdateStorageCommand.Handler updateStorageCommand,
            UseHeroResourceCommand.Handler useHeroResourceCommand,
            ValidateEnoughResourceCommand.Handler validateEnoughResourceCommand,
            GetMissingResourceCommand.Handler getMissingResourceCommand,
            ToNpcResourcePageCommand.Handler toNpcResourcePageCommand,
            NpcResourceCommand.Handler npcResourceCommand,
            ISettingService settingService,
            IChromeBrowser browser,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var (accountId, villageId, plan) = command;

            await updateStorageCommand.HandleAsync(new(accountId, villageId), cancellationToken);

            var requiredResource = GetRequiredResource(browser, plan.Type);

            var result = await validateEnoughResourceCommand.HandleAsync(new(villageId, requiredResource), cancellationToken);
            if (!result.IsFailed) return Result.Ok();

            if (result.HasError<LackOfFreeCrop>()) return result;
            if (result.HasError<StorageLimit>()) return result;

            var url = browser.CurrentUrl;

            // 1) Top up from hero inventory if enabled.
            if (settingService.BooleanByName(villageId, VillageSettingEnums.UseHeroResourceForBuilding))
            {
                logger.Information("Don't have enough resource. Use resource in hero inventory to upgrade building");
                var missingResource = await getMissingResourceCommand.HandleAsync(new(villageId, requiredResource), cancellationToken);

                var heroResult = await useHeroResourceCommand.HandleAsync(new(accountId, missingResource), cancellationToken);
                await browser.Navigate(url, cancellationToken);
                if (heroResult.IsFailed) logger.Warning("Hero resource transfer did not complete");

                await updateStorageCommand.HandleAsync(new(accountId, villageId), cancellationToken);
                result = await validateEnoughResourceCommand.HandleAsync(new(villageId, requiredResource), cancellationToken);
                if (!result.IsFailed) return Result.Ok();
            }

            // 2) Still short -> NPC exchange (convert surplus to the needed resources) if enabled.
            if (settingService.BooleanByName(villageId, VillageSettingEnums.AutoNPCEnable))
            {
                var npcPage = await toNpcResourcePageCommand.HandleAsync(new(villageId), cancellationToken);
                if (npcPage.IsSuccess)
                {
                    logger.Information("Don't have enough resource. NPC exchange to cover build cost");
                    await npcResourceCommand.HandleAsync(new(villageId), cancellationToken);
                    await browser.Navigate(url, cancellationToken);
                    await updateStorageCommand.HandleAsync(new(accountId, villageId), cancellationToken);
                    result = await validateEnoughResourceCommand.HandleAsync(new(villageId, requiredResource), cancellationToken);
                    if (!result.IsFailed) return Result.Ok();
                }
            }

            // Still short after hero/NPC: return MissingResource so the task waits for natural resources.
            return result;
        }

        private static long[] GetRequiredResource(IChromeBrowser browser, BuildingEnums building)
        {
            var doc = browser.Html;

            var resources = UpgradeParser.GetRequiredResource(doc, building);
            if (resources is null || resources.Count != 5) return new long[5];

            var resourceBuilding = new long[5];
            for (var i = 0; i < 5; i++)
            {
                resourceBuilding[i] = resources[i].InnerText.ParseLong();
            }

            return resourceBuilding;
        }
    }
}