namespace MainCore.Commands.Misc
{
    /// <summary>
    /// Turns a single target build plan into an ordered list of plans where every prerequisite
    /// (infrastructure only - resource fields are excluded) is queued before the building that needs it.
    /// Locations for newly created buildings are auto-assigned to free village slots; the target keeps
    /// its own location when one was already chosen (Location &gt; 0).
    /// </summary>
    [Handler]
    public static partial class ExpandPrerequisitesCommand
    {
        public sealed record Command(VillageId VillageId, NormalBuildPlan Target) : IVillageCommand;

        private static readonly List<int> _excludedLocations = new() { 26, 39, 40 }; // main building, rally point, wall

        private static async ValueTask<List<NormalBuildPlan>> HandleAsync(
            Command command,
            GetLayoutBuildingsCommand.Handler getLayoutBuildingsQuery,
            ILogger logger
            )
        {
            var (villageId, target) = command;
            var layout = await getLayoutBuildingsQuery.HandleAsync(new(villageId));
            Collapse(layout);
            return Expand(layout, target, logger);
        }

        /// <summary>Collapse current/queue/job levels into CurrentLevel so planned upgrades are visible.</summary>
        public static void Collapse(List<BuildingItem> layout)
        {
            foreach (var item in layout) item.CurrentLevel = item.Level;
        }

        /// <summary>
        /// Expand <paramref name="target"/> against an already-collapsed <paramref name="layout"/>.
        /// Mutates the layout (occupies slots, bumps levels) so callers can expand many targets in sequence.
        /// </summary>
        public static List<NormalBuildPlan> Expand(List<BuildingItem> layout, NormalBuildPlan target, ILogger logger)
        {
            var result = new List<NormalBuildPlan>();
            Resolve(target, layout, result, new HashSet<BuildingEnums>(), logger, depth: 0);
            return result;
        }

        private static void Resolve(
            NormalBuildPlan plan,
            List<BuildingItem> layout,
            List<NormalBuildPlan> result,
            HashSet<BuildingEnums> visiting,
            ILogger logger,
            int depth)
        {
            if (depth > 30) return; // safety net; the prerequisite graph is a DAG

            foreach (var prerequisite in plan.Type.GetPrerequisiteBuildings())
            {
                if (prerequisite.Type.IsResourceField()) continue; // infra-only
                if (GetLevel(layout, prerequisite.Type) >= prerequisite.Level) continue;
                if (!visiting.Add(prerequisite.Type)) continue; // cycle guard

                Resolve(
                    new NormalBuildPlan { Type = prerequisite.Type, Level = prerequisite.Level, Location = 0 },
                    layout, result, visiting, logger, depth + 1);

                visiting.Remove(prerequisite.Type);
            }

            if (plan.Type.IsResourceField())
            {
                result.Add(plan);
                return;
            }

            // A single building already at/above the wanted level needs no job.
            if (!plan.Type.IsMultipleBuilding() && GetLevel(layout, plan.Type) >= plan.Level) return;

            if (plan.Location <= 0) plan.Location = ResolveLocation(plan, layout);
            if (plan.Location == -1)
            {
                logger.Warning("No free building slot left - skipping {Type} to level {Level}", plan.Type, plan.Level);
                return;
            }

            result.Add(plan);
            OccupySlot(layout, plan);
        }

        private static int GetLevel(List<BuildingItem> layout, BuildingEnums type)
        {
            return layout
                .Where(x => x.Type == type)
                .Select(x => x.CurrentLevel)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static int ResolveLocation(NormalBuildPlan plan, List<BuildingItem> layout)
        {
            if (plan.Type.IsWall()) return 40;

            if (plan.Type.IsMultipleBuilding())
            {
                var largest = layout
                    .Where(x => x.Type == plan.Type)
                    .OrderByDescending(x => x.CurrentLevel)
                    .FirstOrDefault();
                if (largest is not null && largest.CurrentLevel < plan.Type.GetMaxLevel()) return largest.Location;
                return FirstFreeSlot(layout);
            }

            var existing = layout.Find(x => x.Type == plan.Type);
            if (existing is not null) return existing.Location;

            return FirstFreeSlot(layout);
        }

        private static int FirstFreeSlot(List<BuildingItem> layout)
        {
            return layout
                .Where(x => x.Type == BuildingEnums.Site)
                .Select(x => x.Location)
                .Where(x => !_excludedLocations.Contains(x))
                .OrderBy(x => x)
                .DefaultIfEmpty(-1)
                .First();
        }

        private static void OccupySlot(List<BuildingItem> layout, NormalBuildPlan plan)
        {
            var slot = layout.Find(x => x.Location == plan.Location);
            if (slot is null) return;
            slot.Type = plan.Type;
            if (slot.CurrentLevel < plan.Level) slot.CurrentLevel = plan.Level;
        }
    }
}
