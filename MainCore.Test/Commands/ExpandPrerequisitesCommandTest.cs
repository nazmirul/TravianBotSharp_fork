using MainCore.Commands.Misc;
using MainCore.Enums;
using MainCore.Models;
using Serilog;

namespace MainCore.Test.Commands
{
    public class ExpandPrerequisitesCommandTest
    {
        private static List<BuildingItem> FreshVillage()
        {
            var layout = new List<BuildingItem>();

            // Resource fields (slots 1-18), level 0.
            var fieldPattern = new[]
            {
                BuildingEnums.Woodcutter, BuildingEnums.Cropland, BuildingEnums.Woodcutter, BuildingEnums.ClayPit,
                BuildingEnums.IronMine, BuildingEnums.Cropland, BuildingEnums.ClayPit, BuildingEnums.Cropland,
                BuildingEnums.Cropland, BuildingEnums.IronMine, BuildingEnums.IronMine, BuildingEnums.Cropland,
                BuildingEnums.Cropland, BuildingEnums.Woodcutter, BuildingEnums.Cropland, BuildingEnums.ClayPit,
                BuildingEnums.IronMine, BuildingEnums.Woodcutter,
            };
            for (var i = 0; i < 18; i++)
                layout.Add(new BuildingItem { Id = new(i + 1), Location = i + 1, Type = fieldPattern[i], CurrentLevel = 0 });

            // Infrastructure slots 19-40: empty (Site) except Main Building at 26 (level 1).
            for (var loc = 19; loc <= 40; loc++)
            {
                var type = loc == 26 ? BuildingEnums.MainBuilding : BuildingEnums.Site;
                var level = loc == 26 ? 1 : 0;
                layout.Add(new BuildingItem { Id = new(loc), Location = loc, Type = type, CurrentLevel = level });
            }

            return layout;
        }

        [Fact]
        public void TownHall_pulls_in_full_infrastructure_chain_in_order()
        {
            var layout = FreshVillage();
            var logger = Substitute.For<ILogger>();
            var target = new NormalBuildPlan { Type = BuildingEnums.TownHall, Level = 1, Location = 0 };

            var plans = ExpandPrerequisitesCommand.Expand(layout, target, logger);

            // No resource fields (infra-only).
            plans.ShouldAllBe(p => !p.Type.IsResourceField());

            int IndexOf(BuildingEnums type) => plans.FindIndex(p => p.Type == type);

            // Every required building is present at the required level.
            plans.ShouldContain(p => p.Type == BuildingEnums.MainBuilding && p.Level >= 10);
            plans.ShouldContain(p => p.Type == BuildingEnums.RallyPoint && p.Level >= 1);
            plans.ShouldContain(p => p.Type == BuildingEnums.Barracks && p.Level >= 3);
            plans.ShouldContain(p => p.Type == BuildingEnums.Academy && p.Level >= 10);
            plans.ShouldContain(p => p.Type == BuildingEnums.TownHall && p.Level == 1);

            // Dependency order is respected.
            IndexOf(BuildingEnums.MainBuilding).ShouldBeLessThan(IndexOf(BuildingEnums.Academy));
            IndexOf(BuildingEnums.RallyPoint).ShouldBeLessThan(IndexOf(BuildingEnums.Barracks));
            IndexOf(BuildingEnums.Barracks).ShouldBeLessThan(IndexOf(BuildingEnums.Academy));
            IndexOf(BuildingEnums.Academy).ShouldBeLessThan(IndexOf(BuildingEnums.TownHall));
            IndexOf(BuildingEnums.TownHall).ShouldBe(plans.Count - 1);

            // Newly created buildings get distinct real slots.
            var newInfra = plans.Where(p => p.Type != BuildingEnums.MainBuilding).ToList();
            newInfra.Select(p => p.Location).Distinct().Count().ShouldBe(newInfra.Count);
            plans.ShouldAllBe(p => p.Location > 0);
        }

        [Fact]
        public void Already_satisfied_building_produces_no_extra_jobs()
        {
            var layout = FreshVillage();
            // Warehouse needs Main Building 1 (already met) -> only the warehouse itself.
            var logger = Substitute.For<ILogger>();
            var target = new NormalBuildPlan { Type = BuildingEnums.Warehouse, Level = 10, Location = 0 };

            var plans = ExpandPrerequisitesCommand.Expand(layout, target, logger);

            plans.Count.ShouldBe(1);
            plans[0].Type.ShouldBe(BuildingEnums.Warehouse);
            plans[0].Level.ShouldBe(10);
        }
    }
}
