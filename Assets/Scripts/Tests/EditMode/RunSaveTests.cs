using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Saving a run and resuming it from the start of its current round.</summary>
    public sealed class RunSaveTests
    {
        private static RunManager RichRun(ChallengeDefinition challenge = null)
        {
            var run = new RunManager(new SystemRandomProvider(7), challenge);
            run.DebugForceRoundComplete();
            run.DebugGrantLueur(77);
            run.DebugGrantModifier(ModifierId.Tirelire);
            run.DebugGrantModifier(ModifierId.Jardinier);
            run.DebugTriggerShapeMasteryGrant();
            run.DebugTriggerColorMasteryGrant();
            run.Deck.TagSpecificTokens(new[] { 0, 3 }, PieceTraitKind.Scholar, new SystemRandomProvider(1));
            Assert.IsTrue(run.LeaveShop());
            return run;
        }

        [Test]
        public void SnapshotText_RoundTripsExactly()
        {
            var snapshot = RichRun().RoundStartSnapshot;
            string text = snapshot.ToText();
            var parsed = RunSnapshot.FromText(text);
            Assert.IsNotNull(parsed);
            Assert.AreEqual(text, parsed.ToText());
        }

        [Test]
        public void Restore_RebuildsTheRunAtTheStartOfItsRound()
        {
            var original = RichRun();
            var snapshot = RunSnapshot.FromText(original.RoundStartSnapshot.ToText());

            var restored = RunManager.Restore(snapshot, new SystemRandomProvider(99));

            Assert.AreEqual(original.CurrentRoundIndex, restored.CurrentRoundIndex);
            Assert.AreEqual(original.Lueur, restored.Lueur);
            Assert.AreEqual(original.TotalScore, restored.TotalScore);
            Assert.AreEqual(original.ShufflesRemaining, restored.ShufflesRemaining);
            Assert.AreEqual(original.ActiveModifiers.Count, restored.ActiveModifiers.Count);
            Assert.AreEqual(original.Deck.DeckCount, restored.Deck.DeckCount);
            Assert.AreEqual(RunState.InProgress, restored.State);
            Assert.AreEqual(original.RoundStartSnapshot.ToText(), restored.RoundStartSnapshot.ToText(), "Saving the resumed run again changes nothing");
        }

        [Test]
        public void Restore_KeepsTheUpgradesOnTheSeeds()
        {
            var original = RichRun();
            var restored = RunManager.Restore(RunSnapshot.FromText(original.RoundStartSnapshot.ToText()), new SystemRandomProvider(5));

            int upgraded = 0;
            foreach (var token in restored.Deck.Deck)
            {
                if (token.Trait.HasValue && token.Trait.Value.Kind == PieceTraitKind.Scholar)
                {
                    upgraded++;
                }
            }
            Assert.AreEqual(2, upgraded);
        }

        [Test]
        public void SavingInTheShop_ResumesInThatShopBeforeAnyPurchase()
        {
            var run = new RunManager(new SystemRandomProvider(11));
            run.DebugDisableWeather();
            run.DebugForceRoundComplete();
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            run.DebugGrantLueur(500);
            string checkpointText = run.CheckpointSnapshot.ToText();
            int lueurAtOpening = run.CheckpointSnapshot.Lueur;
            var offeredFirst = run.ShopBlisterSlots[0];
            // Buy something afterwards: the checkpoint must not know about it.
            run.BuyBlisterSlot(0);

            Assert.AreEqual(checkpointText, run.CheckpointSnapshot.ToText());
            var restored = RunManager.Restore(RunSnapshot.FromText(checkpointText), new SystemRandomProvider(3));

            Assert.AreEqual(RunState.AwaitingShop, restored.State);
            Assert.AreEqual(lueurAtOpening, restored.Lueur);
            Assert.AreEqual(run.CurrentRoundIndex, restored.CurrentRoundIndex);
            Assert.IsFalse(restored.ShopBlisterSlots[0].Purchased);
            Assert.AreEqual(offeredFirst.Kind, restored.ShopBlisterSlots[0].Kind);
            for (int i = 0; i < run.ShopUpgradeSlots.Count; i++)
            {
                Assert.AreEqual(run.ShopUpgradeSlots[i].HiddenUpgrade.Id, restored.ShopUpgradeSlots[i].HiddenUpgrade.Id);
            }
            Assert.IsTrue(restored.LeaveShop(), "The restored shop can be left to start the next round");
        }

        [Test]
        public void Restore_KeepsGradientAndDwindlingRunCounters()
        {
            var snapshot = RichRun().RoundStartSnapshot;
            snapshot.GradientBonus = 3;
            snapshot.EpuisementValue = 7;
            var restored = RunManager.Restore(RunSnapshot.FromText(snapshot.ToText()), new SystemRandomProvider(2));
            Assert.AreEqual(4f, restored.Grid.GradientCurrentMultiplier, 1e-4f);
            Assert.AreEqual(7, restored.Grid.EpuisementCurrentBonus);
        }

        [Test]
        public void FromText_RejectsGarbageAndOtherVersions()
        {
            Assert.IsNull(RunSnapshot.FromText(null));
            Assert.IsNull(RunSnapshot.FromText(""));
            Assert.IsNull(RunSnapshot.FromText("nonsense"));
            Assert.IsNull(RunSnapshot.FromText("version=999\n"));
        }

        [Test]
        public void Restore_WorksForEveryChallenge()
        {
            foreach (var challenge in ChallengeCatalog.All)
            {
                var run = new RunManager(new SystemRandomProvider(3), challenge);
                var restored = RunManager.Restore(RunSnapshot.FromText(run.RoundStartSnapshot.ToText()), new SystemRandomProvider(4));
                Assert.IsNotNull(restored, challenge.Id.ToString());
                Assert.AreEqual(run.CurrentRoundIndex, restored.CurrentRoundIndex, challenge.Id.ToString());
                Assert.AreEqual(run.CurrentWeather, restored.CurrentWeather, challenge.Id.ToString());
            }
        }
    }
}
