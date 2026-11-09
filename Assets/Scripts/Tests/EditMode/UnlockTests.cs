using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Progression: the unlock catalog, the tracker, and what a run hides until it is unlocked.</summary>
    public sealed class UnlockTests
    {
        [Test]
        public void Catalog_HoldsEveryLockedItem_WithAReadableCondition()
        {
            int sprouts = 0, fertilizers = 0, challenges = 0;
            foreach (var entry in UnlockCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entry.Condition.Text), entry.Key + " needs a sentence for the Collection");
                Assert.Greater(entry.Condition.Threshold, 0, entry.Key);
                switch (entry.Kind)
                {
                    case UnlockKind.Sprout:
                        sprouts++;
                        Assert.IsTrue(System.Array.Exists(UpgradeCatalog.GridPool, u => (int)u.Id == entry.Id), entry.Key + " is not a tile upgrade of the Grid pool");
                        break;
                    case UnlockKind.Fertilizer:
                        fertilizers++;
                        Assert.IsNotNull(ModifierCatalog.Get((ModifierId)entry.Id), entry.Key);
                        break;
                    default:
                        challenges++;
                        break;
                }
                if (entry.Condition.Metric == UnlockMetric.FertilizersHeld)
                {
                    Assert.LessOrEqual(entry.Condition.Threshold, EconomyConstants.MaxActiveModifiers, entry.Key + " asks for more fertilizers than a run can hold");
                }
            }
            Assert.AreEqual(65, sprouts, "25 sprouts are available from the start, the other 65 are earned");
            Assert.AreEqual(103, fertilizers, "30 fertilizers are available from the start, the other 103 are earned");
            Assert.AreEqual(5, challenges, "Spring is always open, the five other challenges are earned");
        }

        [Test]
        public void Tracker_UnlocksOnTheRightScope()
        {
            var state = new UnlockState();
            var tracker = new UnlockTracker(state);
            tracker.StartRun();

            // Single: the best single observation counts (Multiplier Zone: 400 points with one seed).
            int score = UnlockCatalog.Find(UnlockKind.Sprout, (int)UpgradeId.MultiplierZone).Condition.Threshold;
            tracker.Observe(UnlockMetric.Score, score - 1);
            Assert.IsFalse(state.IsEarned(UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.MultiplierZone)));
            tracker.Observe(UnlockMetric.Score, score);
            Assert.IsTrue(state.IsEarned(UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.MultiplierZone)));

            // Round: the sum over one round (Harvester) and it starts over with the next round.
            var harvester = UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.HarvesterTile);
            int perRound = UnlockCatalog.FindByKey(harvester).Condition.Threshold;
            tracker.Observe(UnlockMetric.Lines, perRound - 1);
            tracker.StartRound();
            tracker.Observe(UnlockMetric.Lines, perRound - 1);
            Assert.IsFalse(state.IsEarned(harvester), "two almost-full rounds are not one full round");
            tracker.Observe(UnlockMetric.Lines, 1);
            Assert.IsTrue(state.IsEarned(harvester));

            // Run: the sum over the run (Spillover); a new run starts the count again.
            var spillover = UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.SpilloverTile);
            int perRun = UnlockCatalog.FindByKey(spillover).Condition.Threshold;
            tracker.StartRun();
            tracker.Observe(UnlockMetric.Lines, perRun / 2);
            tracker.StartRound();
            tracker.Observe(UnlockMetric.Lines, perRun / 2 - 1);
            Assert.IsFalse(state.IsEarned(spillover));
            tracker.StartRun();
            tracker.Observe(UnlockMetric.Lines, perRun / 2);
            Assert.IsFalse(state.IsEarned(spillover), "the first run's lines are not carried over");
            tracker.StartRound();
            tracker.Observe(UnlockMetric.Lines, perRun - perRun / 2);
            Assert.IsTrue(state.IsEarned(spillover), "two rounds of the same run add up");
        }

        [Test]
        public void Tracker_TotalsPersistAcrossRuns_AndSaveAndLoadKeepEverything()
        {
            var state = new UnlockState();
            var tracker = new UnlockTracker(state);
            tracker.StartRun();
            // Executioner: a number of enemies in total, across two runs.
            var key = UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.ExecutionerTile);
            int needed = UnlockCatalog.FindByKey(key).Condition.Threshold;
            int first = needed / 2, second = needed - first;
            for (int i = 0; i < first; i++) { tracker.Observe(UnlockMetric.EnemiesDefeated, 1); }

            var reloaded = UnlockState.FromText(state.ToText());
            Assert.AreEqual(first, reloaded.Total(UnlockMetric.EnemiesDefeated));
            var secondRun = new UnlockTracker(reloaded);
            secondRun.StartRun();
            for (int i = 0; i < second; i++) { secondRun.Observe(UnlockMetric.EnemiesDefeated, 1); }
            Assert.IsTrue(reloaded.IsEarned(key), "the enemies of two runs add up");
            Assert.IsTrue(secondRun.DrainNewlyUnlocked().Exists(e => e.Key == key));
            Assert.IsTrue(UnlockState.FromText(reloaded.ToText()).IsEarned(key));
            Assert.IsFalse(UnlockState.FromText("garbage\nU=Zzz,S99999\nT=oops").IsEarned(key), "a damaged save just gives a fresh state");
        }

        [Test]
        public void ARunUnlocksOnlyAFewThings_TheRestArrivesAtTheNextRuns()
        {
            var state = new UnlockState();
            var tracker = new UnlockTracker(state);
            tracker.StartRun();
            // A huge observation meets many conditions at once: only the first few unlock now.
            tracker.Observe(UnlockMetric.Score, 1000000);
            tracker.Observe(UnlockMetric.Lines, 1000);
            tracker.Observe(UnlockMetric.GroupSize, 1000);
            tracker.Observe(UnlockMetric.NeighborsTouched, 100);
            tracker.Observe(UnlockMetric.GardenTiles, 49);
            Assert.AreEqual(UnlockTracker.MaxUnlocksPerRun, tracker.DrainNewlyUnlocked().Count);
            Assert.Greater(state.Queued.Count, 0, "the rest waits in the queue");
            int queued = state.Queued.Count;

            var next = new UnlockTracker(UnlockState.FromText(state.ToText()));
            next.StartRun();
            Assert.AreEqual(System.Math.Min(queued, UnlockTracker.MaxUnlocksPerRun), next.DrainNewlyUnlocked().Count, "a queued unlock arrives at the next run's start");
        }

        [Test]
        public void AtMostConditions_AndStreaks()
        {
            var state = new UnlockState();
            var tracker = new UnlockTracker(state);
            tracker.StartRun();
            var fallow = UnlockCatalog.KeyOf(UnlockKind.Fertilizer, (int)ModifierId.EspaceLibre);
            tracker.Observe(UnlockMetric.GardenFillPercentAtClear, 60);
            Assert.IsFalse(state.IsEarned(fallow));
            tracker.Observe(UnlockMetric.GardenFillPercentAtClear, 30);
            Assert.IsTrue(state.IsEarned(fallow));

            var repetition = UnlockCatalog.KeyOf(UnlockKind.Fertilizer, (int)ModifierId.Repetition);
            for (int i = 0; i < 3; i++) { tracker.ObserveSequence(ShapeId.Single, PieceColor.Coral, 0, 1); }
            Assert.IsFalse(state.IsEarned(repetition));
            tracker.ObserveSequence(ShapeId.Single, PieceColor.Teal, 0, 1);
            Assert.IsTrue(state.IsEarned(repetition), "4 Single seeds in a row, whatever their color");
        }

        [Test]
        public void ALockedRun_NeverOffersWhatIsStillLocked()
        {
            var state = new UnlockState();
            for (int seed = 0; seed < 40; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed));
                run.DebugDisableWeather();
                run.AttachUnlocks(state);
                for (int round = 0; round < 4; round++)
                {
                    run.DebugForceRoundComplete();
                    if (run.State != RunState.AwaitingShop) { break; }
                    var slots = new List<ShopSlot>(run.ShopBlisterSlots);
                    slots.AddRange(run.ShopUpgradeSlots);
                    foreach (var slot in slots)
                    {
                        if (slot.Kind == ShopSlotKind.Modifier)
                        {
                            Assert.IsTrue(state.IsUnlocked(UnlockKind.Fertilizer, (int)slot.ModifierId), "locked fertilizer offered: " + slot.ModifierId);
                        }
                        else if (slot.HiddenUpgrade != null)
                        {
                            Assert.IsTrue(state.IsUnlocked(UnlockKind.Sprout, (int)slot.HiddenUpgrade.Id), "locked sprout offered: " + slot.HiddenUpgrade.Id);
                        }
                    }
                    run.LeaveShop();
                }
            }
        }

        [Test]
        public void APlayedRun_UnlocksWhatItDoes()
        {
            var state = new UnlockState();
            var run = new RunManager(new SystemRandomProvider(3));
            run.DebugDisableWeather();
            run.AttachUnlocks(state);
            // Round 1 is the first round reached; clearing rounds takes the run to round 3 (Gambler Sprout: reach that round).
            int target = UnlockCatalog.Find(UnlockKind.Sprout, (int)UpgradeId.GamblerTile).Condition.Threshold;
            Assert.AreEqual(3, target, "this test plays up to round 3");
            for (int i = 0; i < 2; i++)
            {
                run.DebugForceRoundComplete();
                run.LeaveShop();
            }
            Assert.IsTrue(state.IsEarned(UnlockCatalog.KeyOf(UnlockKind.Sprout, (int)UpgradeId.GamblerTile)));
            var newly = run.Unlocks.DrainNewlyUnlocked();
            Assert.IsTrue(newly.Exists(e => e.Kind == UnlockKind.Sprout && e.Id == (int)UpgradeId.GamblerTile));
            Assert.AreEqual(0, run.Unlocks.DrainNewlyUnlocked().Count, "drained once");
        }
    }
}
