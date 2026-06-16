using MainCore.Commands.Misc;
using MainCore.Models.Presets;

namespace MainCore.Commands.UI.Villages.BuildViewModel
{
    /// <summary>
    /// Applies a build preset to a village: each normal entry is expanded with its prerequisites
    /// (ExpandPrerequisitesCommand) and each resource entry becomes a ResourceBuild job. All jobs are
    /// appended to the bottom of the queue, in preset order.
    /// </summary>
    [Handler]
    public static partial class ApplyPresetCommand
    {
        public sealed record Command(VillageId VillageId, BuildPreset Preset) : IVillageCommand;

        private static async ValueTask HandleAsync(
            Command command,
            GetLayoutBuildingsCommand.Handler getLayoutBuildingsQuery,
            AddJobCommand.Handler addJobCommand,
            ILogger logger
            )
        {
            var (villageId, preset) = command;

            // One shared layout threaded through every entry so slot/level state accumulates in-memory.
            var layout = await getLayoutBuildingsQuery.HandleAsync(new(villageId));
            ExpandPrerequisitesCommand.Collapse(layout);

            foreach (var entry in preset.Entries)
            {
                if (entry.Resource is not null)
                {
                    await addJobCommand.HandleAsync(new(villageId, entry.Resource.ToJob()));
                    continue;
                }

                if (entry.Normal is null) continue;

                var plans = ExpandPrerequisitesCommand.Expand(layout, entry.Normal, logger);
                foreach (var plan in plans)
                {
                    await addJobCommand.HandleAsync(new(villageId, plan.ToJob()));
                }
            }
        }
    }
}
