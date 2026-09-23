namespace Contigu.Core
{
    /// <summary>
    /// Pure update logic for folding one run's outcome into the running
    /// MetaStats totals — kept free of any file I/O (see IMetaStatsStore)
    /// so it's unit-testable without touching disk, the same reason
    /// SystemRandomProvider stays plain System.Random rather than
    /// something UnityEngine-specific.
    /// </summary>
    public static class MetaStatsRecorder
    {
        public static MetaStats RecordRunOutcome(MetaStats current, int finalScore, int roundReached, bool victory)
        {
            return new MetaStats
            {
                TotalRunsPlayed = current.TotalRunsPlayed + 1,
                TotalVictories = current.TotalVictories + (victory ? 1 : 0),
                BestScore = System.Math.Max(current.BestScore, finalScore),
                BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached)
            };
        }
    }
}
