namespace MainCore.Models.Presets
{
    /// <summary>
    /// One entry in a preset: either a normal building target or a resource-field plan.
    /// Location is left 0 and resolved (with prerequisites) when the preset is applied.
    /// </summary>
    public sealed class PresetEntry
    {
        public NormalBuildPlan? Normal { get; init; }
        public ResourceBuildPlan? Resource { get; init; }

        public static PresetEntry Build(BuildingEnums type, int level)
            => new() { Normal = new NormalBuildPlan { Type = type, Level = level, Location = 0 } };

        public static PresetEntry Fields(ResourcePlanEnums plan, int level)
            => new() { Resource = new ResourceBuildPlan { Plan = plan, Level = level } };
    }

    public sealed class BuildPreset
    {
        public required string Name { get; init; }
        public required List<PresetEntry> Entries { get; init; }

        public override string ToString() => Name;
    }

    public static class BuildPresets
    {
        public static readonly IReadOnlyList<BuildPreset> All = new List<BuildPreset>
        {
            new()
            {
                Name = "Feeder village",
                Entries = new()
                {
                    PresetEntry.Build(BuildingEnums.Warehouse, 10),
                    PresetEntry.Build(BuildingEnums.Granary, 10),
                    PresetEntry.Fields(ResourcePlanEnums.AllResources, 6),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Residence, 10),
                    PresetEntry.Build(BuildingEnums.TownHall, 1),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Build(BuildingEnums.Cranny, 10),
                    PresetEntry.Fields(ResourcePlanEnums.AllResources, 10),
                    PresetEntry.Build(BuildingEnums.Marketplace, 10),
                },
            },
            new()
            {
                Name = "Resource village",
                Entries = new()
                {
                    PresetEntry.Build(BuildingEnums.MainBuilding, 15),
                    PresetEntry.Build(BuildingEnums.Warehouse, 10),
                    PresetEntry.Build(BuildingEnums.Granary, 10),
                    PresetEntry.Build(BuildingEnums.Marketplace, 1),
                    PresetEntry.Build(BuildingEnums.Residence, 1),
                    PresetEntry.Fields(ResourcePlanEnums.AllResources, 10),
                },
            },
            new()
            {
                Name = "Capital (15-cropper)",
                Entries = new()
                {
                    PresetEntry.Build(BuildingEnums.MainBuilding, 20),
                    PresetEntry.Build(BuildingEnums.Warehouse, 10),
                    PresetEntry.Build(BuildingEnums.Granary, 10),
                    PresetEntry.Fields(ResourcePlanEnums.AllResources, 6),
                    PresetEntry.Build(BuildingEnums.Palace, 10),
                    PresetEntry.Build(BuildingEnums.TownHall, 1),
                    PresetEntry.Fields(ResourcePlanEnums.AllResources, 10),
                    PresetEntry.Build(BuildingEnums.Barracks, 10),
                    PresetEntry.Build(BuildingEnums.Academy, 10),
                    PresetEntry.Build(BuildingEnums.Smithy, 10),
                    PresetEntry.Build(BuildingEnums.Stable, 10),
                    PresetEntry.Build(BuildingEnums.GrainMill, 5),
                    PresetEntry.Build(BuildingEnums.Sawmill, 5),
                    PresetEntry.Build(BuildingEnums.Brickyard, 5),
                    PresetEntry.Build(BuildingEnums.IronFoundry, 5),
                    PresetEntry.Build(BuildingEnums.Bakery, 5),
                },
            },
        };
    }
}
