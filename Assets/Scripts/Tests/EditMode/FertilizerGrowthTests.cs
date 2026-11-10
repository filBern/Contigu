using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Maturation: a fertilizer that scored grows Bud → Bloom → Fruit; a Fruit is harvested; each stage and harvest strengthens it.</summary>
    public sealed class FertilizerGrowthTests
    {
        private static RunManager NewRun(WeatherId weather, params ModifierId[] held)
        {
            var run = new RunManager(new SystemRandomProvider(5));
            run.DebugDisableWeather();
            run.DebugSetWeather(weather);
            foreach (var id in held)
            {
                Assert.IsTrue(run.DebugGrantModifier(id));
            }
            return run;
        }

        private static PlacementResult PlaceAny(RunManager run)
        {
            for (int slot = 0; slot < run.Deck.Hand.Count; slot++)
            {
                if (!run.Deck.Hand[slot].HasValue || run.IsHandSlotLocked(slot))
                {
                    continue;
                }
                var shape = PieceShapeCatalog.GetRotated(run.Deck.Hand[slot].Value.Shape, run.Deck.HandRotations[slot]);
                foreach (var pos in GridManager.AllPositions())
                {
                    if (run.Grid.CanPlace(shape, pos.x, pos.y))
                    {
                        var outcome = run.PlacePiece(slot, pos.x, pos.y);
                        Assert.IsTrue(outcome.Placement.Success);
                        return outcome.Placement;
                    }
                }
            }
            Assert.Fail("no placement found");
            return null;
        }

        [Test]
        public void Stages_FollowTheThresholds()
        {
            Assert.AreEqual(GrowthStage.Bud, FertilizerGrowth.StageOf(0));
            Assert.AreEqual(GrowthStage.Bud, FertilizerGrowth.StageOf(FertilizerGrowth.BloomAt - 1));
            Assert.AreEqual(GrowthStage.Bloom, FertilizerGrowth.StageOf(FertilizerGrowth.BloomAt));
            Assert.AreEqual(GrowthStage.Bloom, FertilizerGrowth.StageOf(FertilizerGrowth.FruitAt - 1));
            Assert.AreEqual(GrowthStage.Fruit, FertilizerGrowth.StageOf(FertilizerGrowth.FruitAt));
            Assert.AreEqual(0f, FertilizerGrowth.Bonus(0, 0), 1e-4f);
            Assert.AreEqual(FertilizerGrowth.BloomBonus + 2 * FertilizerGrowth.BonusPerHarvest, FertilizerGrowth.Bonus(FertilizerGrowth.BloomAt, 2), 1e-4f);
        }

        [Test]
        public void HarvestBonus_HasNoCeiling()
        {
            Assert.AreEqual(10f, FertilizerGrowth.Bonus(0, 100), 1e-3f);
        }

        [Test]
        public void AFertilizerThatScoredGrowsAfterTheRound_OneThatDidNotStaysPut()
        {
            // Rain: Barometer scores; Clear-weather-only logic is irrelevant here, Cornerstone-like idle fertilizers do not.
            var run = NewRun(WeatherId.Rain, ModifierId.Barometre, ModifierId.ChasseurTempete);
            PlaceAny(run); // Barometer pays under Rain, Storm Chaser needs harsh weather
            run.DebugForceRoundComplete();
            Assert.AreEqual(1, run.GetModifierGrowth(0), "Barometer scored");
            Assert.AreEqual(0, run.GetModifierGrowth(1), "Storm Chaser did not");
        }

        [Test]
        public void GrowthStrengthensTheEffect()
        {
            var bud = PlaceAny(NewRun(WeatherId.Rain, ModifierId.Barometre)).AdditiveMultBonus;
            Assert.AreEqual(ScoringConstants.BarometerMultBonus, bud);

            var run = NewRun(WeatherId.Rain, ModifierId.Barometre);
            run.DebugSetModifierGrowth(0, FertilizerGrowth.FruitAt, 2); // Fruit (+0.5) and 2 harvests (+0.2): factor 1.7
            Assert.AreEqual(5, PlaceAny(run).AdditiveMultBonus, "round(3 x 1.7)");

            var seasoned = NewRun(WeatherId.Rain, ModifierId.Barometre);
            seasoned.DebugSetModifierGrowth(0, 0, 20); // factor 3.0, nothing caps it
            Assert.AreEqual(9, PlaceAny(seasoned).AdditiveMultBonus);
        }

        [Test]
        public void AFruitIsHarvestedAtTheEndOfTheRound()
        {
            var plain = NewRun(WeatherId.Rain, ModifierId.Barometre);
            plain.DebugForceRoundComplete();
            int plainPollen = plain.Lueur;

            var run = NewRun(WeatherId.Rain, ModifierId.Barometre);
            run.DebugSetModifierGrowth(0, FertilizerGrowth.FruitAt, 1);
            run.DebugForceRoundComplete();

            Assert.AreEqual(plainPollen + FertilizerGrowth.HarvestPollen, run.Lueur, "the harvest pays Pollen");
            Assert.AreEqual(0, run.GetModifierGrowth(0), "back to a Bud");
            Assert.AreEqual(2, run.GetModifierHarvests(0), "one more harvest, kept for good");
            CollectionAssert.AreEqual(new[] { ModifierId.Barometre }, run.LastHarvestedModifiers);
        }

        [Test]
        public void GrowthTravelsWithTheFertilizer()
        {
            var run = NewRun(WeatherId.Rain, ModifierId.Barometre, ModifierId.ChasseurTempete, ModifierId.Hate);
            run.DebugSetModifierGrowth(0, 2, 3);
            run.SwapModifiers(0, 2);
            Assert.AreEqual(ModifierId.Barometre, run.ActiveModifiers[2]);
            Assert.AreEqual(2, run.GetModifierGrowth(2));
            Assert.AreEqual(3, run.GetModifierHarvests(2));
            Assert.AreEqual(0, run.GetModifierGrowth(0));
            run.MoveModifier(2, 0);
            Assert.AreEqual(ModifierId.Barometre, run.ActiveModifiers[0]);
            Assert.AreEqual(2, run.GetModifierGrowth(0));
            Assert.AreEqual(3, run.GetModifierHarvests(0));
        }

        [Test]
        public void SnapshotKeepsTheGrowth()
        {
            var run = NewRun(WeatherId.Rain, ModifierId.Barometre);
            run.DebugForceRoundComplete();
            run.DebugSetModifierGrowth(0, 2, 4);
            Assert.IsTrue(run.LeaveShop());
            var snapshot = RunSnapshot.FromText(run.RoundStartSnapshot.ToText());
            Assert.IsNotNull(snapshot);
            var restored = RunManager.Restore(snapshot, new SystemRandomProvider(9));
            Assert.AreEqual(run.GetModifierGrowth(0), restored.GetModifierGrowth(0));
            Assert.AreEqual(run.GetModifierHarvests(0), restored.GetModifierHarvests(0));
        }

        [Test]
        public void Sap_FlowsFromABloomOrFruitToItsRightNeighbor()
        {
            // Barometer (Roguelike?) and Haste are in different categories as far as resonance goes; only the sap is checked here.
            var run = NewRun(WeatherId.Rain, ModifierId.Hate, ModifierId.Barometre);
            Assert.AreEqual(0f, run.GetModifierSapBonus(1), 1e-4f, "a Bud gives nothing");
            run.DebugSetModifierGrowth(0, FertilizerGrowth.BloomAt);
            Assert.AreEqual(FertilizerGrowth.SapFromBloom, run.GetModifierSapBonus(1), 1e-4f);
            run.DebugSetModifierGrowth(0, FertilizerGrowth.FruitAt);
            Assert.AreEqual(FertilizerGrowth.SapFromFruit, run.GetModifierSapBonus(1), 1e-4f);
            Assert.AreEqual(0f, run.GetModifierSapBonus(0), 1e-4f, "nothing flows into the first slot, nor leftwards");
        }

        [Test]
        public void Sap_StrengthensTheReceiverAndOrderMatters()
        {
            var run = NewRun(WeatherId.Rain, ModifierId.Hate, ModifierId.Barometre);
            run.DebugSetModifierGrowth(0, FertilizerGrowth.FruitAt); // the Fruit feeds the Barometer on its right
            Assert.AreEqual(FertilizerGrowth.SapFromFruit, run.GetModifierSapBonus(1), 1e-4f);
            run.SwapModifiers(0, 1); // the Fruit now stands on the right: nothing flows leftwards
            Assert.AreEqual(0f, run.GetModifierSapBonus(0), 1e-4f);
            Assert.AreEqual(0f, run.GetModifierSapBonus(1), 1e-4f);
        }

        [Test]
        public void Resonance_CountsNeighborsOfTheSameCategory_NoCeiling()
        {
            // Three fertilizers of one category in a row: the middle one has two matching neighbors, the ends one.
            ModifierId a = default, b = default, c = default;
            ModifierCategory? found = null;
            var byCategory = new System.Collections.Generic.Dictionary<ModifierCategory, System.Collections.Generic.List<ModifierId>>();
            foreach (var def in ModifierCatalog.All)
            {
                if (ModifierCatalog.IsRetired(def.Id)) { continue; }
                if (!byCategory.ContainsKey(def.Category)) { byCategory[def.Category] = new System.Collections.Generic.List<ModifierId>(); }
                byCategory[def.Category].Add(def.Id);
            }
            foreach (var pair in byCategory)
            {
                if (pair.Value.Count >= 3) { found = pair.Key; a = pair.Value[0]; b = pair.Value[1]; c = pair.Value[2]; break; }
            }
            Assert.IsTrue(found.HasValue);
            var run = NewRun(WeatherId.Clear, a, b, c);
            Assert.AreEqual(1, run.GetModifierResonanceNeighbors(0));
            Assert.AreEqual(2, run.GetModifierResonanceNeighbors(1));
            Assert.AreEqual(2 * FertilizerGrowth.ResonancePerNeighbor, run.GetModifierResonanceBonus(1), 1e-4f);
        }

        [Test]
        public void TheTotalBonusAddsGrowthSapAndResonance()
        {
            var run = NewRun(WeatherId.Clear, ModifierId.Hate, ModifierId.Barometre);
            run.DebugSetModifierGrowth(0, FertilizerGrowth.BloomAt);
            run.DebugSetModifierGrowth(1, FertilizerGrowth.FruitAt, 2);
            float expected = FertilizerGrowth.FruitBonus + 2 * FertilizerGrowth.BonusPerHarvest + FertilizerGrowth.SapFromBloom + run.GetModifierResonanceBonus(1);
            Assert.AreEqual(expected, run.GetModifierTotalBonus(1), 1e-4f);
        }
    }
}
