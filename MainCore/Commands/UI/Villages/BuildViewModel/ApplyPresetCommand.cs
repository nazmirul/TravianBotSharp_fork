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

            // Pre-existing instances of multiple buildings (cranny/warehouse/granary/trapper), levels
            // descending per type. Used to skip preset entries the village already satisfies - e.g. a
            // village that already has 10 crannies should not get 10 more.
            var existingMultiples = layout
                .Where(x => x.Type.IsMultipleBuilding())
                .GroupBy(x => x.Type)
                .ToDictionary(g => g.Key, g => g.Select(x => x.CurrentLevel).OrderByDescending(l => l).ToList());

            foreach (var entry in preset.Entries)
            {
                if (entry.Resource is not null)
                {
                    await addJobCommand.HandleAsync(new(villageId, entry.Resource.ToJob()));
                    continue;
                }

                if (entry.Normal is null) continue;

                // Already have an instance of this multiple building at/above the target? Consume it, skip.
                if (entry.Normal.Type.IsMultipleBuilding()
                    && existingMultiples.TryGetValue(entry.Normal.Type, out var levels))
                {
                    var idx = levels.FindIndex(l => l >= entry.Normal.Level);
                    if (idx >= 0)
                    {
                        levels.RemoveAt(idx);
                        continue;
                    }
                }

                var plans = ExpandPrerequisitesCommand.Expand(layout, entry.Normal, logger);
                foreach (var plan in plans)
                {
                    await addJobCommand.HandleAsync(new(villageId, plan.ToJob()));
                }
            }
        }
    }
}
