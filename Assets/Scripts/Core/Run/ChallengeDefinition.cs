namespace Contigu.Core
{
    /// <summary>
    /// Everything a challenge (see ChallengeId/ChallengeCatalog) overrides
    /// about a run's rules — RunManager reads these instead of RunConfig's
    /// own constants directly once a non-Classic challenge is chosen, so
    /// RunConfig itself stays exactly what it always was: Classic's own
    /// numbers, now also the source ChallengeCatalog.Classic is built
    /// from rather than a second copy of them.
    /// </summary>
    public sealed class ChallengeDefinition
    {
        public ChallengeId Id;
        public string Name;
        public string Description;

        /// <summary>Stars cost to unlock (see MetaStats.Stars) — 0 for Classic, which is always available.</summary>
        public int UnlockCost;

        public int RoundCount;
        public int[] Quotas;
        public int[] PieceBudgets;

        /// <summary>True for Chaos: the boss cell-lock ticks every round instead of only the last one (see RunManager.IsBossRound).</summary>
        public bool BossActiveEveryRound;

        public int BossLockPiecesInterval;
        public int BossLockCellsPerInterval;

        public int BossRoundIndex
        {
            get { return RoundCount - 1; }
        }
    }
}
