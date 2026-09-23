namespace Contigu.Core
{
    /// <summary>
    /// Persistent, cross-run stats (spec extension, explicit request: "meta
    /// progression" -> lightweight option chosen over currency-driven
    /// unlocks — "suivi de stats et meilleurs scores", no gameplay effect).
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

        /// <summary>Highest round NUMBER (1-based, see RunManager.CurrentRoundNumber) ever reached across every run — RunConfig.RoundCount (8) on any past victory, since clearing the boss round means reaching it.</summary>
        public int BestRoundReached;
    }
}
