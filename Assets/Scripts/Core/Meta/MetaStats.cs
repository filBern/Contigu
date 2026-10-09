namespace Contigu.Core
{
    /// <summary>
    /// Persistent, cross-run stats, including gameplay-affecting meta-
    /// progression (see Stars/ChallengeUnlocked* below and ChallengeCatalog).
    /// Plain public-field data so Unity's JsonUtility can (de)serialize it
    /// directly; see <see cref="IMetaStatsStore"/> for how it's actually
    /// persisted.
    /// </summary>
    [System.Serializable]
    public sealed class MetaStats
    {
        public int TotalRunsPlayed;
        public int TotalVictories;
        public int BestScore;

        /// <summary>Highest round NUMBER (1-based, see RunManager.CurrentRoundNumber) ever reached across every run — at least RunConfig.RoundCount on any past victory, since clearing the final round means reaching it.</summary>
        public int BestRoundReached;

        /// <summary>Cross-run currency (see MetaStatsRecorder.RecordRunOutcome for how a run earns it, TryUnlockChallenge for how it's spent). A dedicated field per non-Classic challenge rather than a generic collection — only 3 challenges exist, and JsonUtility doesn't serialize a HashSet/Dictionary cleanly.</summary>
        public int Stars;
        public bool MarathonUnlocked;
        public bool ChaosUnlocked;

        /// <summary>Classic is always available; every other challenge needs its own unlock flag.</summary>
        public bool IsUnlocked(ChallengeId id)
        {
            switch (id)
            {
                case ChallengeId.Classic:
                    return true;
                case ChallengeId.Marathon:
                    return MarathonUnlocked;
                case ChallengeId.Chaos:
                    return ChaosUnlocked;
                default:
                    return false;
            }
        }
    }
}
