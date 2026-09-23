using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class MetaStatsRecorderTests
    {
        [Test]
        public void RecordRunOutcome_FirstEverRun_StartsEveryTotalFromZero()
        {
            var fresh = new MetaStats();

            var updated = MetaStatsRecorder.RecordRunOutcome(fresh, finalScore: 1200, roundReached: 4, victory: false);

            Assert.AreEqual(1, updated.TotalRunsPlayed);
            Assert.AreEqual(0, updated.TotalVictories);
            Assert.AreEqual(1200, updated.BestScore);
            Assert.AreEqual(4, updated.BestRoundReached);
        }

        [Test]
        public void RecordRunOutcome_Victory_IncrementsVictoryCountAlongsideRunCount()
        {
            var current = new MetaStats { TotalRunsPlayed = 2, TotalVictories = 0, BestScore = 500, BestRoundReached = 6 };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 15000, roundReached: RunConfig.RoundCount, victory: true);

            Assert.AreEqual(3, updated.TotalRunsPlayed);
            Assert.AreEqual(1, updated.TotalVictories);
        }

        [Test]
        public void RecordRunOutcome_LowerScoreThanCurrentBest_KeepsThePriorBestScore()
        {
            var current = new MetaStats { TotalRunsPlayed = 5, TotalVictories = 1, BestScore = 9000, BestRoundReached = 8 };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 300, roundReached: 2, victory: false);

            Assert.AreEqual(9000, updated.BestScore, "A worse run must never lower the recorded best score.");
        }

        [Test]
        public void RecordRunOutcome_HigherScoreThanCurrentBest_RaisesTheBestScore()
        {
            var current = new MetaStats { TotalRunsPlayed = 5, TotalVictories = 1, BestScore = 9000, BestRoundReached = 8 };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 9001, roundReached: 8, victory: false);

            Assert.AreEqual(9001, updated.BestScore);
        }

        [Test]
        public void RecordRunOutcome_LowerRoundThanCurrentBest_KeepsThePriorBestRound()
        {
            var current = new MetaStats { TotalRunsPlayed = 5, TotalVictories = 1, BestScore = 9000, BestRoundReached = 7 };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 100, roundReached: 1, victory: false);

            Assert.AreEqual(7, updated.BestRoundReached, "A run that ends earlier must never lower the recorded best round.");
        }

        [Test]
        public void RecordRunOutcome_NeverMutatesTheInputStats()
        {
            var current = new MetaStats { TotalRunsPlayed = 5, TotalVictories = 1, BestScore = 9000, BestRoundReached = 7 };

            MetaStatsRecorder.RecordRunOutcome(current, finalScore: 20000, roundReached: 8, victory: true);

            Assert.AreEqual(5, current.TotalRunsPlayed);
            Assert.AreEqual(1, current.TotalVictories);
            Assert.AreEqual(9000, current.BestScore);
            Assert.AreEqual(7, current.BestRoundReached);
        }
    }
}
