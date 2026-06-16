namespace MainCore.Enums
{
    public enum VillageSettingEnums
    {
        // Building
        UseHeroResourceForBuilding = 1,

        ApplyRomanQueueLogicWhenBuilding,
        UseSpecialUpgrade,

        // Complete now
        CompleteImmediately,

        // General
        Tribe,

        // Train troop
        TrainTroopEnable,

        TrainTroopRepeatTimeMin,
        TrainTroopRepeatTimeMax,
        TrainWhenLowResource,

        BarrackTroop,
        BarrackAmountMin,
        BarrackAmountMax,

        StableTroop,
        StableAmountMin,
        StableAmountMax,

        GreatBarrackTroop,
        GreatBarrackAmountMin,
        GreatBarrackAmountMax,

        GreatStableTroop,
        GreatStableAmountMin,
        GreatStableAmountMax,

        WorkshopTroop,
        WorkshopAmountMin,
        WorkshopAmountMax,

        // NPC
        AutoNPCEnable,

        AutoNPCOverflow,
        AutoNPCGranaryPercent,
        AutoNPCWood,
        AutoNPCClay,
        AutoNPCIron,
        AutoNPCCrop,

        // Refresh
        AutoRefreshEnable,

        AutoRefreshMin,
        AutoRefreshMax,

        // Claim quest
        AutoClaimQuestEnable,

        CompleteImmediatelyTime,

        // IMPORTANT: append new settings at the END only. These enum values are stored in the DB,
        // so inserting in the middle shifts every later value and corrupts saved settings.
        AutoBuildResourceWhenIdle,
    }
}