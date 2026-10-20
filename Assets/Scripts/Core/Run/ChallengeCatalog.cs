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
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        /// <summary>Smaller starting deck (see InitialDeckFactory.BuildMarathon), tighter piece budgets every round, quotas trimmed ~15% to compensate — a tenser run from the very first placement instead of Classic's single late-run spike.</summary>
        public static readonly ChallengeDefinition Marathon = new ChallengeDefinition
        {
            Id = ChallengeId.Marathon,
            Name = "Winter",
            Description = "A smaller starting seed bag and tighter seed budgets. A random boss rule appears every four rounds.",
            UnlockCost = 5,
            RoundCount = RunConfig.RoundCount,
            Quotas = new[] { 250, 425, 725, 1225, 2075, 3525, 6000, 10200, 17500, 32000, 58000, 105000, 190000, 340000, 600000 },
            PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18, 24, 24, 24, 26, 26, 28, 30 },
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        public static readonly ChallengeDefinition[] All = { Classic, Marathon };

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
