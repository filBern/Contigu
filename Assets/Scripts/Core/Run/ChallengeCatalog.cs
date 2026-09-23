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
        /// <summary>The original, unchanged run — same numbers as RunConfig, just wrapped so RunManager can read every challenge the same way instead of special-casing Classic.</summary>
        public static readonly ChallengeDefinition Classic = new ChallengeDefinition
        {
            Id = ChallengeId.Classic,
            Name = "Classic",
            Description = "The original run: 8 rounds, the boss only starts locking cells on the last one.",
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
            Description = "A smaller starting deck and tighter piece budgets every round, with quotas trimmed to match.",
            UnlockCost = 5,
            RoundCount = RunConfig.RoundCount,
            Quotas = new[] { 250, 425, 725, 1225, 2075, 3525, 6000, 10200 },
            PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18 },
            BossActiveEveryRound = false,
            BossLockPiecesInterval = RunConfig.BossLockPiecesInterval,
            BossLockCellsPerInterval = RunConfig.BossLockCellsPerInterval
        };

        /// <summary>Same quotas/budgets/starting deck as Classic — only the boss changes: its cell-lock ticks every round (not just the last), at a gentler pace than Classic's own finale (1 cell every 5 pieces here, vs. 2 every 3 for Classic's single boss round).</summary>
        public static readonly ChallengeDefinition Chaos = new ChallengeDefinition
        {
            Id = ChallengeId.Chaos,
            Name = "Chaos",
            Description = "The boss's cell-lock is active from round 1, at a gentler pace throughout instead of one big spike at the end.",
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
