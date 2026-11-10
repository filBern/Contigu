namespace Contigu.Core
{
    /// <summary>
    /// The selectable challenges. Classic is always available;
    /// the others are unlocked by playing (see UnlockCatalog).
    /// </summary>
    public static class ChallengeCatalog
    {
        /// <summary>The original run — same quotas/budgets as RunConfig, with a random boss effect every four rounds.</summary>
        public static readonly ChallengeDefinition Classic = new ChallengeDefinition
        {
            Id = ChallengeId.Classic,
            Name = "Spring",
            Description = "The gentle start: 12 rounds, each its own enemy encounter. The place to learn the garden.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            PieceBudgetBonus = 6,
            EnemyHpMultiplier = 1.62f
        };

        /// <summary>Smaller starting deck (see InitialDeckFactory.BuildMarathon), tighter piece budgets every round — a tenser run from the very first placement instead of Classic's single late-run spike.</summary>
        public static readonly ChallengeDefinition Marathon = new ChallengeDefinition
        {
            Id = ChallengeId.Marathon,
            Name = "Winter",
            WeatherProfile = WeatherProfile.Winter,
            Description = "A smaller starting seed bag and tighter seed budgets. 12 enemy rounds with far less room for error, against locks, thieves and ice: no blight at all. Home of the Hoarfrost.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18, 24, 24, 24, 26, 26, 28, 30 },
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            PieceBudgetBonus = 10,
            EnemyHpMultiplier = 2.6f,
            WorldEnemy = EnemyId.Hoarfrost,
            EnemySwaps = new[] { new EnemySwap(EnemyId.Poisoner, EnemyId.Locker), new EnemySwap(EnemyId.Plague, EnemyId.HeavyLocker), new EnemySwap(EnemyId.Blightmoth, EnemyId.Thief), new EnemySwap(EnemyId.Reclaimer, EnemyId.Leech) }
        };

        /// <summary>Weather every round, never calm — and it hits 1.5x harder. Rewards planning around the forecast shown in the shop.</summary>
        public static readonly ChallengeDefinition Summer = new ChallengeDefinition
        {
            Id = ChallengeId.Summer,
            Name = "Summer",
            Description = "Every round brings real weather - never calm skies - and it hits 1.5x harder. Plan around the forecast. Blight and fertilizer pests, no locks. Home of the Locust Swarm.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            WeatherProfile = WeatherProfile.Extreme,
            WeatherIntensity = 1.5f,
            PieceBudgetBonus = 6,
            EnemyHpMultiplier = 1.4f,
            WorldEnemy = EnemyId.Locust,
            EnemySwaps = new[] { new EnemySwap(EnemyId.Locker, EnemyId.Poisoner), new EnemySwap(EnemyId.HeavyLocker, EnemyId.Plague), new EnemySwap(EnemyId.Thief, EnemyId.Blightmoth), new EnemySwap(EnemyId.Leech, EnemyId.Reclaimer) }
        };

        /// <summary>Fertilizers wither away after 4 rounds and a random new one sprouts in each slot; the shop pays more Pollen and the enemies are far gentler, so builds are rebuilt on the fly.</summary>
        public static readonly ChallengeDefinition Autumn = new ChallengeDefinition
        {
            Id = ChallengeId.Autumn,
            Name = "Autumn",
            WeatherProfile = WeatherProfile.Autumn,
            Description = "Fertilizers wither after 4 rounds - a random new one sprouts in their place - and every round pays extra Pollen. Enemies are gentler, with fewer big locks and blights, and pests that gnaw at fertilizers. Home of the Rotgrub.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            FertilizerLifespanRounds = 4,
            RoundEndPollenBonus = 10,
            EnemyHpMultiplier = 1.7f,
            PieceBudgetBonus = 12,
            WorldEnemy = EnemyId.Rotgrub,
            EnemySwaps = new[] { new EnemySwap(EnemyId.HeavyLocker, EnemyId.Locker), new EnemySwap(EnemyId.Plague, EnemyId.Poisoner), new EnemySwap(EnemyId.Thief, EnemyId.Blightmoth) }
        };

        /// <summary>Only 2 of the 4 seed colors exist this run (drawn at the start): big groups come easily, so the enemies are sturdier.</summary>
        public static readonly ChallengeDefinition Orchard = new ChallengeDefinition
        {
            Id = ChallengeId.Orchard,
            Name = "Orchard",
            WeatherProfile = WeatherProfile.Orchard,
            Description = "Only 2 seed colors exist this run. Groups connect easily, but the enemies are sturdier. Fewer haters of colors, more shields and locks. Home of the Fruitworm.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            ActiveColorCount = 2,
            EnemyHpMultiplier = 4.8f,
            PieceBudgetBonus = 6,
            WorldEnemy = EnemyId.Fruitworm,
            EnemySwaps = new[] { new EnemySwap(EnemyId.ColorHater, EnemyId.Reclaimer), new EnemySwap(EnemyId.Poisoner, EnemyId.Locker), new EnemySwap(EnemyId.Plague, EnemyId.HeavyLocker) }
        };

        /// <summary>A snug 5x5 plot: lines are short and frequent, but there is no room to spare.</summary>
        public static readonly ChallengeDefinition Greenhouse = new ChallengeDefinition
        {
            Id = ChallengeId.Greenhouse,
            Name = "Greenhouse",
            WeatherProfile = WeatherProfile.Greenhouse,
            Description = "A snug 5x5 plot. Lines are short and frequent, but there is no room to spare. Blight and blighted fertilizers instead of locks, which would choke a plot this small. Home of the Whitefly Cloud.",
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval,
            GridSize = 5,
            EnemyHpMultiplier = 1.25f,
            PieceBudgetBonus = 10,
            WorldEnemy = EnemyId.Whitefly,
            EnemySwaps = new[] { new EnemySwap(EnemyId.Locker, EnemyId.Poisoner), new EnemySwap(EnemyId.HeavyLocker, EnemyId.Plague), new EnemySwap(EnemyId.Reclaimer, EnemyId.Blightmoth) }
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
