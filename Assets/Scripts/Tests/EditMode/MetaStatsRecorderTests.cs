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

        [Test]
        public void RecordRunOutcome_DefeatOnRoundFour_EarnsOneStarPerRoundActuallyCleared()
        {
            var current = new MetaStats();

            // Defeated ON round 4 (1-based CurrentRoundNumber, the caller's
            // convention) means rounds 1-3 were actually cleared first.
            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 1200, roundReached: 4, victory: false);

            Assert.AreEqual(3, updated.Stars);
        }

        [Test]
        public void RecordRunOutcome_Victory_EarnsOneStarPerRoundPlusTheVictoryBonus()
        {
            var current = new MetaStats();

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 20000, roundReached: RunConfig.RoundCount, victory: true);

            Assert.AreEqual(RunConfig.RoundCount + 2, updated.Stars);
        }

        [Test]
        public void RecordRunOutcome_DefeatOnRoundOne_EarnsNoStars()
        {
            var current = new MetaStats { Stars = 4 };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 50, roundReached: 1, victory: false);

            Assert.AreEqual(4, updated.Stars, "Dying on round 1 cleared nothing, so it must not earn or lose Stars.");
        }

        [Test]
        public void RecordRunOutcome_PreservesStarsAndUnlockedChallenges()
        {
            var current = new MetaStats { Stars = 12, MarathonUnlocked = true, ChaosUnlocked = false };

            var updated = MetaStatsRecorder.RecordRunOutcome(current, finalScore: 100, roundReached: 2, victory: false);

            Assert.AreEqual(12 + 1, updated.Stars);
            Assert.IsTrue(updated.MarathonUnlocked, "Recording a run must never forget an already-unlocked challenge.");
            Assert.IsFalse(updated.ChaosUnlocked);
        }

        [Test]
        public void TryUnlockChallenge_Classic_AlwaysSucceedsWithoutSpendingStars()
        {
            var current = new MetaStats { Stars = 0 };

            var (updated, success) = MetaStatsRecorder.TryUnlockChallenge(current, ChallengeCatalog.Classic);

            Assert.IsTrue(success);
            Assert.AreEqual(0, updated.Stars);
        }

        [Test]
        public void TryUnlockChallenge_NotEnoughStars_FailsAndSpendsNothing()
        {
            var current = new MetaStats { Stars = 2 };

            var (updated, success) = MetaStatsRecorder.TryUnlockChallenge(current, ChallengeCatalog.Marathon);

            Assert.IsFalse(success);
            Assert.AreEqual(2, updated.Stars);
            Assert.IsFalse(updated.MarathonUnlocked);
        }

        [Test]
        public void TryUnlockChallenge_EnoughStars_SpendsExactlyTheUnlockCostAndFlipsTheFlag()
        {
            var current = new MetaStats { Stars = ChallengeCatalog.Marathon.UnlockCost };

            var (updated, success) = MetaStatsRecorder.TryUnlockChallenge(current, ChallengeCatalog.Marathon);

            Assert.IsTrue(success);
            Assert.AreEqual(0, updated.Stars);
            Assert.IsTrue(updated.MarathonUnlocked);
        }

        [Test]
        public void TryUnlockChallenge_AlreadyUnlocked_SucceedsWithoutSpendingAgain()
        {
            var current = new MetaStats { Stars = 3, ChaosUnlocked = true };

            var (updated, success) = MetaStatsRecorder.TryUnlockChallenge(current, ChallengeCatalog.Chaos);

            Assert.IsTrue(success);
            Assert.AreEqual(3, updated.Stars, "Already-unlocked challenges must never be charged for again.");
        }
    }
}
