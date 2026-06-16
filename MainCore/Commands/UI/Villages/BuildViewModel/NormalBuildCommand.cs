using MainCore.Commands.Misc;
using MainCore.UI.Models.Input;

namespace MainCore.Commands.UI.Villages.BuildViewModel
{
    [Handler]
    public static partial class NormalBuildCommand
    {
        public sealed record Command(VillageId VillageId, NormalBuildPlan plan) : IVillageCommand;

        private static async ValueTask<Result> HandleAsync(
            Command command,
            GetLayoutBuildingsCommand.Handler getLayoutBuildingsQuery,
            ExpandPrerequisitesCommand.Handler expandPrerequisitesCommand,
            AddJobCommand.Handler addJobCommand
            )
        {
            var (villageId, plan) = command;

            var buildings = await getLayoutBuildingsQuery.HandleAsync(new(villageId));
            var building = buildings.Find(x => x.Location == plan.Location);

            if (building is null)
            {
                plan.ValidateLocation(buildings);
            }

            // Auto-insert any missing infrastructure prerequisites before the target, in order.
            var plans = await expandPrerequisitesCommand.HandleAsync(new(villageId, plan));
            foreach (var p in plans)
            {
                await addJobCommand.HandleAsync(new(villageId, p.ToJob()));
            }
            return Result.Ok();
        }

        private static void ValidateLocation(this NormalBuildPlan plan, List<BuildingItem> buildings)
        {
            if (plan.Type.IsWall())
            {
                plan.Location = 40;
                return;
            }
            if (plan.Type.IsMultipleBuilding())
            {
                var sameTypeBuildings = buildings.Where(x => x.Type == plan.Type);
                if (!sameTypeBuildings.Any()) return;
                if (sameTypeBuildings.Any(x => x.Location == plan.Location)) return;
                var largestLevelBuilding = sameTypeBuildings.MaxBy(x => x.Level)!;
                if (largestLevelBuilding.Level == plan.Type.GetMaxLevel()) return;
                plan.Location = largestLevelBuilding.Location;
                return;
            }

            if (plan.Type.IsResourceField())
            {
                var field = buildings.First(x => x.Location == plan.Location);
                if (plan.Type == field.Type) return;
                plan.Type = field.Type;
                return;
            }

            var building = buildings.Find(x => x.Type == plan.Type);
            if (building is null) return;
            if (plan.Location == building.Location) return;
            plan.Location = building.Location;
        }

        public static NormalBuildPlan ToPlan(this NormalBuildInput input, int location)
        {
            var (type, level) = input.Get();
            return new NormalBuildPlan()
            {
                Location = location,
                Type = type,
                Level = level,
            };
        }
    }
}