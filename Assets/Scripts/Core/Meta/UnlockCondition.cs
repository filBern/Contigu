namespace Contigu.Core
{
    /// <summary>What a run can measure to unlock something (see <see cref="UnlockCatalog"/>). Values are observed by <see cref="UnlockTracker"/>; the scope of each condition says how they add up.</summary>
    public enum UnlockMetric
    {
        // ---- One value per placement
        Score, Lines, GroupSize, TraitCells, TraitPoints, Destroyed, Uprooted, GoldenScored, FrozenBroken, GardenCleared,
        /// <summary>Distinct other colors touching the planted seed.</summary>
        ColorsTouched,
        /// <summary>Distinct planted tiles touching the seed.</summary>
        NeighborsTouched,
        /// <summary>Distinct same-color groups the seed joins together.</summary>
        MergedGroups,
        Corners, Middle, Isolated,
        /// <summary>1 per seed planted.</summary>
        Seeds,
        SeedSize12, SeedSize3, SeedSize4, SquarePlayed, VerticalSeed, HorizontalSeed, Straight3,
        SlotPlayed1, SlotPlayed2, SlotPlayed3,
        SeedUpgraded, SeedTwoUpgrades,
        HarvestOnLastSeedOfHand, HarvestOnFirstSeedOfRound, HandEmptied, BlightScored,
        /// <summary>Length of the current streak, counted each placement.</summary>
        SameShapeStreak, SameColorStreak, DifferentColorStreak, HarvestStreak, BiggerGroupStreak,
        /// <summary>1 each time the planted shape differs from the previous seed's.</summary>
        ShapeChanges,

        // ---- A snapshot of the run, observed after every placement and shop action
        Pollen, FertilizersHeld, FertilizerCategories, DevotionsHeld,
        BagSize, BagUpgraded, BagUpgradeKinds, BagShapes, BagJokers, BagOneColor,
        GardenTiles, GardenUpgraded, GardenUpgradeKinds, GardenColors, GoldenPlots, CornersFilled, AfflictedPlots, MasteryLevels,
        /// <summary>Percent of the garden that is planted.</summary>
        GardenFillPercent,
        RoundReached,

        // ---- Events
        RoundClearedWeather, RoundClearedSunny, RoundClearedHarshWeather, RoundClearedFrost, RoundClearedWind,
        RoundClearedNoShuffle, RoundClearedNoHarvest, RoundClearedClean, RoundClearedOnLastSeed,
        /// <summary>Seeds still in the budget when a round is cleared.</summary>
        SeedsLeftAtClear,
        GardenFillPercentAtClear,
        ClearedWinterRound, ClearedGreenhouseRound,
        EnemiesDefeated, DefeatedBramble, DefeatedBlightspore, DefeatedHaterBoss, DefeatedLeechOrVine, DefeatedResident, DefeatedBoss,
        /// <summary>Seeds left in the budget when an enemy is defeated.</summary>
        EnemyDefeatedSeedsLeft,
        /// <summary>Plots locked, frozen or blighted by enemies (counted after every Shuffle).</summary>
        PlotsAfflicted,
        RunWon, RunWonSpring
    }

    /// <summary>How a metric's observations add up before being compared to the threshold.</summary>
    public enum UnlockScope
    {
        /// <summary>The best single observation (one placement, or a snapshot at any moment).</summary>
        Single,
        /// <summary>The sum over the current round.</summary>
        Round,
        /// <summary>The sum over the current run.</summary>
        Run,
        /// <summary>The sum over every run (saved).</summary>
        Total
    }

    /// <summary>One unlock condition: a metric, how it accumulates, and the threshold to reach.</summary>
    public sealed class UnlockCondition
    {
        public readonly UnlockMetric Metric;
        public readonly UnlockScope Scope;
        public readonly int Threshold;
        /// <summary>True when the value must stay at or below the threshold (only the fill percent at a round's end).</summary>
        public readonly bool AtMost;
        /// <summary>The sentence shown in the Collection.</summary>
        public readonly string Text;

        public UnlockCondition(UnlockMetric metric, UnlockScope scope, int threshold, string text, bool atMost = false)
        {
            Metric = metric;
            Scope = scope;
            Threshold = threshold;
            Text = text;
            AtMost = atMost;
        }

        public bool IsMet(int value)
        {
            return AtMost ? value <= Threshold : value >= Threshold;
        }
    }
}
