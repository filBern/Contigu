namespace Contigu.Core
{
    /// <summary>
    /// The selectable challenges. Classic is always available;
    /// the others need Stars (see MetaStats.Stars/IsUnlocked,
    /// MetaStatsRecorder.TryUnlockChallenge).
    /// </summary>
    public static class ChallengeCatalog
    {
        /// <summary>The original run — same quotas/budgets as RunConfig, with a random boss effect every four rounds.</summary>
        public static readonly ChallengeDefinition Classic = new ChallengeDefinition
        {
            Id = ChallengeId.Classic,
            Name = "Spring",
            Description = "15 rounds, each its own enemy encounter.",
            UnlockCost = 0,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            PieceBudgetBonus = 6,
            EnemyHpMultiplier = 0.8f
        };

        /// <summary>Smaller starting deck (see InitialDeckFactory.BuildMarathon), tighter piece budgets every round, quotas trimmed ~15% to compensate — a tenser run from the very first placement instead of Classic's single late-run spike.</summary>
        public static readonly ChallengeDefinition Marathon = new ChallengeDefinition
        {
            Id = ChallengeId.Marathon,
            UsesEncounters = false,
            Name = "Winter",
            Description = "A smaller starting seed bag and tighter seed budgets. A random boss rule appears every four rounds.",
            UnlockCost = 5,
            RoundCount = RunConfig.RoundCount,
            Quotas = new[] { 250, 425, 725, 1225, 2075, 3525, 6000, 10200, 17500, 32000, 58000, 105000, 190000, 340000, 600000 },
            PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18, 24, 24, 24, 26, 26, 28, 30 },
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        /// <summary>Weather every round, never calm — and it hits 1.5x harder. Rewards planning around the forecast shown in the shop.</summary>
        public static readonly ChallengeDefinition Summer = new ChallengeDefinition
        {
            Id = ChallengeId.Summer,
            Name = "Summer",
            Description = "Every round brings real weather — never calm skies — and it hits 1.5x harder. Plan around the forecast.",
            UnlockCost = 8,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            WeatherProfile = WeatherProfile.Extreme,
            WeatherIntensity = 1.5f,
            PieceBudgetBonus = 6,
            EnemyHpMultiplier = 0.8f
        };

        /// <summary>Fertilizers wither away after 4 rounds; the shop pays more Pollen to compensate, so builds are rebuilt on the fly.</summary>
        public static readonly ChallengeDefinition Autumn = new ChallengeDefinition
        {
            Id = ChallengeId.Autumn,
            Name = "Autumn",
            Description = "Fertilizers wither away after 4 rounds, but every round pays extra Pollen. Keep your garden growing.",
            UnlockCost = 12,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            FertilizerLifespanRounds = 4,
            RoundEndPollenBonus = 10,
            EnemyHpMultiplier = 0.7f,
            PieceBudgetBonus = 6
        };

        /// <summary>Only 2 of the 4 seed colors exist this run (drawn at the start): big groups come easily, so the enemies are sturdier.</summary>
        public static readonly ChallengeDefinition Orchard = new ChallengeDefinition
        {
            Id = ChallengeId.Orchard,
            Name = "Orchard",
            Description = "Only 2 seed colors exist this run. Groups connect easily, but the enemies are sturdier.",
            UnlockCost = 15,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            ActiveColorCount = 2,
            EnemyHpMultiplier = 1.4f,
            PieceBudgetBonus = 6
        };

        /// <summary>A snug 5x5 plot: lines are short and frequent, but there is no room to spare.</summary>
        public static readonly ChallengeDefinition Greenhouse = new ChallengeDefinition
        {
            Id = ChallengeId.Greenhouse,
            Name = "Greenhouse",
            Description = "A snug 5x5 plot. Lines are short and frequent, but there is no room to spare.",
            UnlockCost = 18,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            GridSize = 5,
            EnemyHpMultiplier = 0.75f,
            PieceBudgetBonus = 6
        };

        public static readonly ChallengeDefinition[] All = { Classic, Marathon, Summer, Autumn, Orchard, Greenhouse };

        public static ChallengeDefinition Get(ChallengeId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }
            return Classic;
        }
    }
}
