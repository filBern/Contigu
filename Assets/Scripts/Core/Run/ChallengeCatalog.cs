namespace Contigu.Core
{
    /// <summary>
    /// The 3 selectable challenges (spec extension, explicit request:
    /// "Meta progression avec différents challenge qui offrent différents
    /// boss et état de depart", scoped down from "everything unlockable"
    /// to a concrete first pass, confirmed by the player: "Oui, bon point
    /// de départ"). Classic is always available; Marathon/Chaos need
    /// Stars (see MetaStats.Stars/IsUnlocked, MetaStatsRecorder.
    /// TryUnlockChallenge).
    /// </summary>
    public static class ChallengeCatalog
    {
        /// <summary>The original run — same quotas/budgets as RunConfig, with a random boss effect every four rounds.</summary>
        public static readonly ChallengeDefinition Classic = new ChallengeDefinition
        {
            Id = ChallengeId.Classic,
            Name = "Classic",
            Description = "15 rounds, each its own enemy encounter.",
            UnlockCost = 0,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossActiveEveryRound = false,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        /// <summary>Smaller starting deck (see InitialDeckFactory.BuildMarathon), tighter piece budgets every round, quotas trimmed ~15% to compensate — a tenser run from the very first placement instead of Classic's single late-run spike.</summary>
        public static readonly ChallengeDefinition Marathon = new ChallengeDefinition
        {
            Id = ChallengeId.Marathon,
            Name = "Marathon",
            Description = "A smaller starting deck and tighter piece budgets. A random boss rule appears every four rounds.",
            UnlockCost = 5,
            RoundCount = RunConfig.RoundCount,
            Quotas = new[] { 250, 425, 725, 1225, 2075, 3525, 6000, 10200, 17500, 32000, 58000, 105000, 190000, 340000, 600000 },
            PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18, 24, 24, 24, 26, 26, 28, 30 },
            BossActiveEveryRound = false,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        /// <summary>Same quotas/budgets/starting deck as Classic. Chaos adds its gradual cell-lock every round on top of the random scheduled boss effect at rounds 4 and 8.</summary>
        public static readonly ChallengeDefinition Chaos = new ChallengeDefinition
        {
            Id = ChallengeId.Chaos,
            Name = "Chaos",
            Description = "The board's cell-lock is active from round 1 at a gentler pace. Every four rounds, an additional random boss rule appears.",
            UnlockCost = 10,
            RoundCount = RunConfig.RoundCount,
            Quotas = RunConfig.Quotas,
            PieceBudgets = RunConfig.PieceBudgets,
            BossActiveEveryRound = true,
            BossLockPiecesInterval = 5,
            BossLockCellsPerInterval = 1
        };

        public static readonly ChallengeDefinition[] All = { Classic, Marathon, Chaos };

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
