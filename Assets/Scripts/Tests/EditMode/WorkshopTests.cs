using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>The Workshop that replaced the Casino: packs, Compost and Forge.</summary>
    public sealed class WorkshopTests
    {
        private static RunManager ShopRun(int seed = 4)
        {
            var run = new RunManager(new SystemRandomProvider(seed));
            run.DebugDisableWeather();
            run.DebugForceRoundComplete();
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            run.DebugGrantLueur(1000);
            return run;
        }

        [Test]
        public void TheWorkshop_HoldsTwoDifferentPacks_Compost_AndAForgeWithAVisibleUpgrade()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var run = ShopRun(seed);
                var slots = run.ShopUpgradeSlots;
                Assert.AreEqual(4, slots.Count);
                Assert.IsTrue(System.Array.IndexOf(UpgradeCatalog.WorkshopPacks, slots[0].HiddenUpgrade) >= 0);
                Assert.IsTrue(System.Array.IndexOf(UpgradeCatalog.WorkshopPacks, slots[1].HiddenUpgrade) >= 0);
                Assert.AreNotSame(slots[0].HiddenUpgrade, slots[1].HiddenUpgrade);
                Assert.AreEqual(UpgradeId.Compost, slots[2].HiddenUpgrade.Id);
                Assert.AreEqual(UpgradeId.Forge, slots[3].HiddenUpgrade.Id);
                Assert.IsNotNull(slots[3].Payload);
                Assert.IsTrue(UpgradeSystem.TraitKindFor(slots[3].Payload.Id).HasValue, "the Forge only offers real tile upgrades");
            }
        }

        [Test]
        public void SeedPack_OffersThreeUpgradedSeeds_AndKeepsTheChosenOne()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.SeedPack);
            int before = run.Deck.DeckCount;
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            Assert.AreEqual(3, run.PendingUpgradePieceCandidates.Count);
            foreach (var token in run.PendingUpgradePieceCandidates)
            {
                Assert.IsTrue(token.Trait.HasValue);
            }
            Assert.IsTrue(run.ResolveUpgradePieceChoice(1));
            Assert.AreEqual(before + 1, run.Deck.DeckCount);
            Assert.IsNull(run.PendingUpgrade);
        }

        [Test]
        public void SproutPack_RevealsThreeUpgrades_ThenTheSeedToEnchant()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.SproutPack);
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            Assert.AreEqual(3, run.PendingPackUpgrades.Count);
            var chosen = run.PendingPackUpgrades[2];
            Assert.IsFalse(run.ResolvePackUpgradeChoice(3));
            Assert.IsTrue(run.ResolvePackUpgradeChoice(2));
            Assert.AreSame(chosen, run.PendingUpgrade);
            Assert.Greater(run.PendingUpgradeTileCandidates.Count, 0);
            Assert.IsTrue(run.ResolveUpgradeTileChoice(new List<int> { run.PendingUpgradeTileCandidates[0] }));
            Assert.IsNull(run.PendingUpgrade);
        }

        [Test]
        public void FertilizerPack_RevealsThreeFertilizers_AndGivesTheChosenOneForFree()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.FertilizerPack);
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            Assert.AreEqual(3, run.PendingPackModifiers.Count);
            var chosen = run.PendingPackModifiers[1];
            int pollen = run.Lueur;
            Assert.IsTrue(run.ResolvePackModifierChoice(1));
            CollectionAssert.Contains(run.ActiveModifiers, chosen);
            Assert.AreEqual(pollen, run.Lueur, "taking the fertilizer costs nothing more");
            Assert.IsNull(run.PendingUpgrade);
        }

        [Test]
        public void SkippingAPack_LeavesNothingPending_AndRefundsNothing()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.FertilizerPack);
            int pollen = run.Lueur;
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            int afterBuy = run.Lueur;
            Assert.Less(afterBuy, pollen);
            Assert.IsTrue(run.SkipPack());
            Assert.IsNull(run.PendingUpgrade);
            Assert.AreEqual(afterBuy, run.Lueur);
            Assert.AreEqual(0, run.ActiveModifiers.Count);
        }

        [Test]
        public void SeedPackSeeds_SometimesCarryMoreThanOneUpgrade_ButMostlyJustOne()
        {
            var run = ShopRun();
            int total = 0;
            int multi = 0;
            for (int roll = 0; roll < 600; roll++)
            {
                foreach (var token in run.Upgrades.GetCandidatePiecesFor(UpgradeCatalog.SeedPack))
                {
                    total++;
                    Assert.GreaterOrEqual(token.TraitCount, 1);
                    if (token.TraitCount > 1)
                    {
                        multi++;
                        var cells = new System.Collections.Generic.HashSet<int>();
                        foreach (var trait in token.Traits)
                        {
                            Assert.IsTrue(cells.Add(trait.LocalCellIndex), "extra upgrades sit on other tiles");
                        }
                    }
                }
            }
            Assert.Greater(multi, 0, "a few seeds carry two upgrades");
            Assert.Less(multi, total / 3, "but most carry only one");
        }

        [Test]
        public void BlisterSlots_AreAFertilizerAboutTwoTimesOutOfThree()
        {
            int total = 0;
            int fertilizers = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                var run = ShopRun(seed);
                foreach (var slot in run.ShopBlisterSlots)
                {
                    total++;
                    if (slot.Kind == ShopSlotKind.Modifier)
                    {
                        fertilizers++;
                    }
                }
            }
            double share = fertilizers / (double)total;
            TestContext.WriteLine("Blister fertilizer share: " + share.ToString("0.00"));
            Assert.Greater(share, 0.55, "fertilizers are the most common Blister item");
            Assert.Less(share, 0.80, "but upgrades still show up");
        }

        [Test]
        public void AbandonPending_ClosesAnyChoice_WithoutRefund()
        {
            var run = ShopRun();
            Assert.IsFalse(run.AbandonPending());
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.SeedPack);
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            Assert.IsNotNull(run.PendingUpgrade);
            Assert.Greater(run.PendingUpgradePieceCandidates.Count, 0);
            int afterBuy = run.Lueur;
            Assert.IsTrue(run.AbandonPending());
            Assert.IsNull(run.PendingUpgrade);
            Assert.AreEqual(0, run.PendingUpgradePieceCandidates.Count);
            Assert.AreEqual(afterBuy, run.Lueur);
        }

        [Test]
        public void FertilizerPack_IsNotOffered_WhenTheGardenIsFull()
        {
            var run = ShopRun();
            for (int i = 0; i < ModifierCatalog.All.Length && run.ActiveModifiers.Count < EconomyConstants.MaxActiveModifiers; i++)
            {
                run.DebugGrantModifier(ModifierCatalog.All[i].Id);
            }
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.FertilizerPack);
            Assert.IsFalse(run.BuyUpgradeSlot(0));
            Assert.IsFalse(run.ShopUpgradeSlots[0].Purchased);
        }

        [Test]
        public void MasteryPack_OffersThreeDistinctMasteries_AndLevelsTheChosenOne()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(0, UpgradeCatalog.MasteryPack);
            Assert.IsTrue(run.BuyUpgradeSlot(0));
            Assert.AreEqual(3, run.PendingMasteryOptions.Count);
            var option = run.PendingMasteryOptions[0];
            Assert.IsTrue(run.ResolveMasteryChoice(0));
            if (option.Shape.HasValue)
            {
                Assert.AreEqual(1 + option.Gain, run.GetShapeMasteryLevel(option.Shape.Value));
            }
            else
            {
                Assert.AreEqual(1 + option.Gain, run.GetColorMasteryLevel(option.Color.Value));
            }
        }

        [Test]
        public void Compost_RemovesTheChosenSeed_AndStaysSoldAfterAReroll()
        {
            var run = ShopRun();
            int before = run.Deck.DeckCount;
            Assert.IsTrue(run.BuyUpgradeSlot(2));
            Assert.Greater(run.PendingUpgradeTileCandidates.Count, 0);
            Assert.IsTrue(run.ResolveCompostChoice(run.PendingUpgradeTileCandidates[0]));
            Assert.AreEqual(before - 1, run.Deck.DeckCount);

            Assert.IsTrue(run.RerollShop());
            Assert.IsTrue(run.ShopUpgradeSlots[2].Purchased, "the Compost stays sold for this visit after a reroll");
        }

        [Test]
        public void Compost_RemovesExactlyTheChosenSeed_EvenAnUpgradedOne()
        {
            var run = ShopRun();
            run.Deck.TagSpecificTokens(new[] { 0 }, PieceTraitKind.Golden, new SystemRandomProvider(1));
            Assert.IsTrue(run.BuyUpgradeSlot(2));
            var candidates = run.PendingUpgradeTileCandidates;
            int upgradedBefore = 0;
            foreach (var token in run.Deck.Deck) { if (token.Trait.HasValue) { upgradedBefore++; } }
            int pick = candidates[0];
            bool pickedUpgraded = run.Deck.Deck[pick].Trait.HasValue;
            Assert.IsTrue(run.ResolveCompostChoice(pick));
            int upgradedAfter = 0;
            foreach (var token in run.Deck.Deck) { if (token.Trait.HasValue) { upgradedAfter++; } }
            Assert.AreEqual(pickedUpgraded ? upgradedBefore - 1 : upgradedBefore, upgradedAfter);
        }

        [Test]
        public void Compost_AbandonedPurchase_DoesNotRaiseItsPrice()
        {
            var run = ShopRun();
            int first = run.GetUpgradeSlotPrice(2);
            Assert.IsTrue(run.BuyUpgradeSlot(2));
            Assert.IsTrue(run.AbandonPending());
            run.DebugForceRoundComplete();
            Assert.IsTrue(run.LeaveShop());
            run.DebugForceRoundComplete();
            Assert.AreEqual(first, run.GetUpgradeSlotPrice(2), "no seed was removed, so the next Compost costs the same");
        }

        [Test]
        public void Compost_PriceGrowsWithEveryUseThisRun()
        {
            var run = ShopRun();
            int first = run.GetUpgradeSlotPrice(2);
            Assert.IsTrue(run.BuyUpgradeSlot(2));
            run.ResolveCompostChoice(run.PendingUpgradeTileCandidates[0]);
            run.DebugForceRoundComplete();
            Assert.IsTrue(run.LeaveShop());
            run.DebugForceRoundComplete();
            Assert.AreEqual(EconomyConstants.CompostBasePrice, first);
            Assert.AreEqual(EconomyConstants.CompostBasePrice + EconomyConstants.CompostPriceIncreasePerUse, run.GetUpgradeSlotPrice(2), "a fresh visit, one Compost already bought this run");
        }

        [Test]
        public void PrismSprout_IsNeverOfferedWithOnlyTwoSeedColors()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed), ChallengeCatalog.Orchard);
                run.DebugDisableWeather();
                run.DebugForceRoundComplete();
                foreach (var slot in run.ShopBlisterSlots) { AssertNotPrism(slot); }
                foreach (var slot in run.ShopUpgradeSlots) { AssertNotPrism(slot); }
                run.DebugGrantLueur(500);
                if (run.BuyUpgradeSlot(0) && run.PendingPackUpgrades.Count > 0)
                {
                    foreach (var up in run.PendingPackUpgrades) { Assert.AreNotEqual(UpgradeId.PrismTile, up.Id); }
                }
            }
        }

        private static void AssertNotPrism(ShopSlot slot)
        {
            if (slot == null || slot.Kind != ShopSlotKind.Upgrade) { return; }
            Assert.AreNotEqual(UpgradeId.PrismTile, slot.HiddenUpgrade.Id);
            if (slot.Payload != null) { Assert.AreNotEqual(UpgradeId.PrismTile, slot.Payload.Id); }
        }

        [Test]
        public void Mimic_IsNeverHandedOutAsAHeldFertilizer()
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed));
                run.DebugDisableWeather();
                run.DebugForceRoundComplete();
                run.DebugTriggerRandomModifierGrant();
                foreach (var id in run.ActiveModifiers) { Assert.AreNotEqual(ModifierId.Copieur, id); }
            }
        }

        [Test]
        public void EveryRoundStartsWithAFreshFullHand()
        {
            var run = new RunManager(new SystemRandomProvider(4));
            run.DebugDisableWeather();
            for (int i = 0; i < run.Deck.Hand.Count; i++)
            {
                if (run.Deck.Hand[i].HasValue)
                {
                    var anchor = FirstFreeAnchor(run, i);
                    run.PlacePiece(i, anchor.x, anchor.y);
                    break;
                }
            }
            int leftOver = 0;
            for (int i = 0; i < run.Deck.Hand.Count; i++) { if (run.Deck.Hand[i].HasValue) { leftOver++; } }
            Assert.Greater(leftOver, 0, "The test needs seeds left over from the first round");
            run.DebugForceRoundComplete();
            Assert.IsTrue(run.LeaveShop());
            for (int i = 0; i < run.Deck.Hand.Count; i++)
            {
                Assert.IsTrue(run.Deck.Hand[i].HasValue, "Slot " + i + " should be dealt at the start of round 2");
            }
        }

        private static UnityEngine.Vector2Int FirstFreeAnchor(RunManager run, int slot)
        {
            var shape = PieceShapeCatalog.GetRotated(run.Deck.Hand[slot].Value.Shape, run.Deck.HandRotations[slot]);
            foreach (var pos in GridManager.AllPositions())
            {
                if (run.Grid.CanPlace(shape, pos.x, pos.y)) { return pos; }
            }
            return new UnityEngine.Vector2Int(0, 0);
        }

        [Test]
        public void Forge_AppliesItsVisibleUpgrade_ToAnyOfManySeeds()
        {
            var run = ShopRun();
            run.DebugSetWorkshopSlot(3, UpgradeCatalog.Forge, UpgradeCatalog.CornerTile);
            Assert.IsTrue(run.BuyUpgradeSlot(3));
            Assert.AreSame(UpgradeCatalog.CornerTile, run.PendingUpgrade);
            Assert.GreaterOrEqual(run.PendingUpgradeTileCandidates.Count, EconomyConstants.ShopTileCandidateCount, "a much wider choice than a normal Sprout Upgrade");
            int target = run.PendingUpgradeTileCandidates[0];
            Assert.IsTrue(run.ResolveUpgradeTileChoice(new List<int> { target }));
            Assert.AreEqual(PieceTraitKind.Corner, run.Deck.Deck[target].Trait.Value.Kind);
        }
    }
}
