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

        // ---- RecordEndlessExtension (explicit request: "j'aimerais que
        // le joueur ait l'option d'aller en endless mode... pour continuer
        // sa run") — RunDefeat's counterpart to RecordRunOutcome once a run
        // kept going past its scheduled Victory (see RunManager.IsEndless),
        // deliberately NOT the same method: that run's TotalRunsPlayed/
        // TotalVictories/victory-bonus Stars were already folded in the
        // moment it first reached RunVictory, so calling RecordRunOutcome
        // a second time here would double-count one physical run as two. ----

        [Test]
        public void RecordEndlessExtension_NeverIncrementsTotalRunsPlayedOrTotalVictories()
        {
            var current = new MetaStats { TotalRunsPlayed = 3, TotalVictories = 1 };

            var updated = MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 3, roundReached: 11);

            Assert.AreEqual(3, updated.TotalRunsPlayed, "The victory that unlocked Endless already counted this as one run played.");
            Assert.AreEqual(1, updated.TotalVictories, "The victory that unlocked Endless already counted this as one victory.");
        }

        [Test]
        public void RecordEndlessExtension_AwardsOneStarPerAdditionalRoundCleared_NoVictoryBonus()
        {
            var current = new MetaStats { Stars = 10 };

            var updated = MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 3, roundReached: 11);

            Assert.AreEqual(13, updated.Stars, "3 more rounds cleared should earn exactly 3 more Stars — rounds 1-8's Stars and the victory bonus were already paid out by RecordRunOutcome.");
        }

        [Test]
        public void RecordEndlessExtension_RaisesBestScoreAndBestRoundReached_WhenHigher()
        {
            var current = new MetaStats { BestScore = 40000, BestRoundReached = 8 };

            var updated = MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 3, roundReached: 11);

            Assert.AreEqual(50000, updated.BestScore);
            Assert.AreEqual(11, updated.BestRoundReached, "BestRoundReached should be free to climb past the old scheduled RoundCount once Endless exists.");
        }

        [Test]
        public void RecordEndlessExtension_KeepsThePriorBests_WhenNotHigher()
        {
            var current = new MetaStats { BestScore = 90000, BestRoundReached = 15 };

            var updated = MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 3, roundReached: 11);

            Assert.AreEqual(90000, updated.BestScore, "A worse endless stretch must never lower the recorded best score.");
            Assert.AreEqual(15, updated.BestRoundReached, "A shorter endless stretch must never lower the recorded best round.");
        }

        [Test]
        public void RecordEndlessExtension_PreservesStarsAndUnlockedChallenges()
        {
            var current = new MetaStats { Stars = 12, MarathonUnlocked = true, ChaosUnlocked = false };

            var updated = MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 2, roundReached: 10);

            Assert.AreEqual(14, updated.Stars);
            Assert.IsTrue(updated.MarathonUnlocked, "Recording an endless extension must never forget an already-unlocked challenge.");
            Assert.IsFalse(updated.ChaosUnlocked);
        }

        [Test]
        public void RecordEndlessExtension_NeverMutatesTheInputStats()
        {
            var current = new MetaStats { TotalRunsPlayed = 3, TotalVictories = 1, BestScore = 40000, BestRoundReached = 8, Stars = 10 };

            MetaStatsRecorder.RecordEndlessExtension(current, finalScore: 50000, additionalRoundsCleared: 3, roundReached: 11);

            Assert.AreEqual(3, current.TotalRunsPlayed);
            Assert.AreEqual(1, current.TotalVictories);
            Assert.AreEqual(40000, current.BestScore);
            Assert.AreEqual(8, current.BestRoundReached);
            Assert.AreEqual(10, current.Stars);
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
