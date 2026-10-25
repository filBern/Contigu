using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>The tile upgrades that score flat placement points (Pip … Gardener).</summary>
    public sealed class TileUpgradeTests
    {
        private static RunManager NewRun(int seed = 11)
        {
            var run = new RunManager(new SystemRandomProvider(seed));
            run.DebugDisableWeather();
            return run;
        }

        private static void Fill(RunManager run, PieceColor color, params (int x, int y)[] cells)
        {
            foreach (var c in cells)
            {
                Assert.IsTrue(run.Grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), color, c.x, c.y).Success);
            }
        }

        /// <summary>Adds a single-tile seed carrying <paramref name="trait"/> to the deck and deals it into the hand.</summary>
        private static int HandSlotWith(RunManager run, PieceToken token)
        {
            run.Deck.AddPreparedToken(token);
            run.DebugSetShufflesRemaining(500);
            for (int attempt = 0; attempt < 500; attempt++)
            {
                for (int slot = 0; slot < run.Deck.Hand.Count; slot++)
                {
                    var t = run.Deck.Hand[slot];
                    if (t.HasValue && t.Value.Trait.HasValue && t.Value.Trait.Value.Kind == token.Trait.Value.Kind && t.Value.Shape == token.Shape && t.Value.Color == token.Color)
                    {
                        return slot;
                    }
                }
                run.ShuffleHand();
            }
            Assert.Fail("the tagged seed never reached the hand");
            return -1;
        }

        private static PlacementResult PlantAt(RunManager run, PieceTraitKind kind, PieceColor color, int x, int y, PieceColor? target = null)
        {
            int slot = HandSlotWith(run, new PieceToken(ShapeId.Single, color, new PieceTrait(kind, 0, target)));
            var outcome = run.PlacePiece(slot, x, y);
            Assert.IsTrue(outcome.Placement.Success);
            return outcome.Placement;
        }

        private static PlacementResult PlaceDomino(RunManager run, PieceTrait first, PieceTrait second, int x, int y)
        {
            int slot = HandSlotWith(run, new PieceToken(ShapeId.DomH, PieceColor.Coral, first, new[] { second }));
            var outcome = run.PlacePiece(slot, x, y);
            Assert.IsTrue(outcome.Placement.Success);
            return outcome.Placement;
        }

        [Test]
        public void StackingUpgrades_Kin_Amplifier_Echo_ReadTheirNeighbors()
        {
            // Kin on one tile of a domino, a Pip on the other: Kin +4 for the upgraded neighbor, Pip +1.
            var kin = PlaceDomino(NewRun(), new PieceTrait(PieceTraitKind.Kin, 0), new PieceTrait(PieceTraitKind.Pip, 1), 2, 2);
            Assert.AreEqual(ScoringConstants.KinBonusPerNeighbor + ScoringConstants.PipBonus, kin.TraitBonus);
            // On the garden's edge the missing neighbors just do not count.
            var corner = PlaceDomino(NewRun(), new PieceTrait(PieceTraitKind.Kin, 0), new PieceTrait(PieceTraitKind.Pip, 1), 0, 0);
            Assert.AreEqual(ScoringConstants.KinBonusPerNeighbor + ScoringConstants.PipBonus, corner.TraitBonus);
            // Amplifier counts its neighbor's points twice; Echo copies them.
            var amplifier = PlaceDomino(NewRun(), new PieceTrait(PieceTraitKind.Amplifier, 0), new PieceTrait(PieceTraitKind.Pip, 1), 2, 2);
            Assert.AreEqual(2 * ScoringConstants.PipBonus, amplifier.TraitBonus);
            var echo = PlaceDomino(NewRun(), new PieceTrait(PieceTraitKind.Echo, 0), new PieceTrait(PieceTraitKind.Pip, 1), 2, 2);
            Assert.AreEqual(2 * ScoringConstants.PipBonus, echo.TraitBonus);
        }

        [Test]
        public void Collector_Resonance_Pouch_Compound_CountTheGardenAndTheBag()
        {
            Assert.AreEqual(ScoringConstants.CollectorBonusPerKind, PlantAt(NewRun(), PieceTraitKind.Collector, PieceColor.Coral, 2, 2).TraitBonus);
            var withPip = NewRun();
            PlantAt(withPip, PieceTraitKind.Pip, PieceColor.Coral, 0, 0);
            Assert.AreEqual(2 * ScoringConstants.CollectorBonusPerKind, PlantAt(withPip, PieceTraitKind.Collector, PieceColor.Coral, 5, 5).TraitBonus);

            Assert.AreEqual(ScoringConstants.ResonanceBonusPerTile, PlantAt(NewRun(), PieceTraitKind.Resonance, PieceColor.Coral, 2, 2).TraitBonus);
            var crowded = NewRun();
            crowded.DebugSetPiecesRemaining(50);
            PlantAt(crowded, PieceTraitKind.Pip, PieceColor.Coral, 0, 0);
            PlantAt(crowded, PieceTraitKind.Pip, PieceColor.Coral, 5, 5);
            Assert.AreEqual(3 * ScoringConstants.ResonanceBonusPerTile, PlantAt(crowded, PieceTraitKind.Resonance, PieceColor.Coral, 3, 3).TraitBonus);

            var pouch = NewRun();
            int slot = HandSlotWith(pouch, new PieceToken(ShapeId.Single, PieceColor.Coral, new PieceTrait(PieceTraitKind.Pouch, 0)));
            int upgradedSeeds = 0;
            foreach (var token in pouch.Deck.Deck)
            {
                if (token.Trait.HasValue)
                {
                    upgradedSeeds++;
                }
            }
            Assert.AreEqual(upgradedSeeds * ScoringConstants.PouchBonusPerUpgradedSeed, pouch.PlacePiece(slot, 2, 2).Placement.TraitBonus);

            var rich = NewRun();
            rich.DebugSetLueur(57);
            Assert.AreEqual(5, PlantAt(rich, PieceTraitKind.Compound, PieceColor.Coral, 2, 2).ModifierLueurBonus);
        }

        [Test]
        public void Perennial_Crescendo_Scholar_FullHouse_GrowWithTheRun()
        {
            var perennial = NewRun();
            Assert.AreEqual(1, PlantAt(perennial, PieceTraitKind.Perennial, PieceColor.Coral, 0, 0).TraitBonus);
            // The first one scores again (it is in the group); a second one now starts from the run-long count.
            Assert.GreaterOrEqual(PlantAt(perennial, PieceTraitKind.Perennial, PieceColor.Coral, 5, 5).TraitBonus, 2);

            var crescendo = NewRun();
            PlantAt(crescendo, PieceTraitKind.Pip, PieceColor.Coral, 0, 0);
            Assert.AreEqual(ScoringConstants.CrescendoBonusPerSeed, PlantAt(crescendo, PieceTraitKind.Crescendo, PieceColor.Coral, 5, 5).TraitBonus);

            var scholar = NewRun();
            scholar.DebugTriggerShapeMasteryGrant();
            scholar.DebugTriggerColorMasteryGrant();
            int levels = 0;
            foreach (ShapeId shape in System.Enum.GetValues(typeof(ShapeId)))
            {
                levels += scholar.GetShapeMasteryLevel(shape) - 1;
            }
            foreach (var color in PieceColorUtility.BaseColors)
            {
                levels += scholar.GetColorMasteryLevel(color) - 1;
            }
            Assert.Greater(levels, 0);
            Assert.AreEqual(levels * ScoringConstants.ScholarBonusPerLevel, PlantAt(scholar, PieceTraitKind.Scholar, PieceColor.Coral, 4, 4).TraitBonus);

            var house = NewRun();
            house.DebugSetPiecesRemaining(50);
            int others = ScoringConstants.FullHouseMinFilled - 1;
            for (int x = 0; x < others; x++)
            {
                PlantAt(house, PieceTraitKind.Pip, PieceColor.Coral, x, 0);
            }
            // The Pips already on the row join the group and score again (+1 each) next to the Full House's own 12.
            Assert.AreEqual(ScoringConstants.FullHouseBonus + others * ScoringConstants.PipBonus, PlantAt(house, PieceTraitKind.FullHouse, PieceColor.Coral, others, 0).TraitBonus);
        }

        [Test]
        public void ASeed_CanCarryTwoUpgrades_OnDifferentTiles_AndBothFire()
        {
            var run = NewRun();
            var token = new PieceToken(ShapeId.DomH, PieceColor.Coral, new PieceTrait(PieceTraitKind.Pip, 0), new[] { new PieceTrait(PieceTraitKind.Pip, 1) });
            Assert.AreEqual(2, token.TraitCount);
            int slot = HandSlotWith(run, token);
            var placement = run.PlacePiece(slot, 2, 2).Placement;
            Assert.IsTrue(placement.Success);
            Assert.AreEqual(2 * ScoringConstants.PipBonus, placement.TraitBonus);
        }

        [Test]
        public void TaggingASeedTwice_PutsTheSecondUpgradeOnAnotherTile()
        {
            var run = NewRun();
            var deck = run.Deck;
            int index = -1;
            for (int i = 0; i < deck.Deck.Count && index < 0; i++)
            {
                if (deck.Deck[i].Shape == ShapeId.Sq2 && !deck.Deck[i].Trait.HasValue)
                {
                    index = i;
                }
            }
            Assert.GreaterOrEqual(index, 0);
            var rng = new SystemRandomProvider(5);
            for (int round = 0; round < 4; round++)
            {
                deck.TagSpecificTokens(new[] { index }, PieceTraitKind.Pip, rng);
            }
            var token = deck.Deck[index];
            Assert.AreEqual(4, token.TraitCount, "a 4-tile seed holds four upgrades");
            var cells = new HashSet<int>();
            foreach (var trait in token.Traits)
            {
                Assert.IsTrue(cells.Add(trait.LocalCellIndex), "each upgrade sits on its own tile");
            }
            // Once every tile has one, a new upgrade replaces one instead of stacking on a tile.
            deck.TagSpecificTokens(new[] { index }, PieceTraitKind.Lucky, rng);
            Assert.AreEqual(4, deck.Deck[index].TraitCount);
        }

        [Test]
        public void EveryNewTileUpgrade_IsInTheGridPool_WithATraitKindAndAnIcon()
        {
            var ids = new[]
            {
                UpgradeId.PipTile, UpgradeId.LuckyTile, UpgradeId.CompanionTile, UpgradeId.ColonyTile, UpgradeId.BorderTile, UpgradeId.HeartTile, UpgradeId.CornerTile,
                UpgradeId.HermitTile, UpgradeId.CuddlyTile, UpgradeId.DawnTile, UpgradeId.DuskTile, UpgradeId.MiserTile, UpgradeId.StormyTile, UpgradeId.ElderTile,
                UpgradeId.HarvesterTile, UpgradeId.BreezeTile, UpgradeId.HarmonyTile, UpgradeId.PrismTile, UpgradeId.GardenerTile
            };
            foreach (var id in ids)
            {
                Assert.IsTrue(UpgradeSystem.TraitKindFor(id).HasValue, id.ToString());
                var def = System.Array.Find(UpgradeCatalog.GridPool, d => d.Id == id);
                Assert.IsNotNull(def, id.ToString());
                Assert.AreEqual(UpgradePool.Grid, def.Pool);
            }
        }

        [Test]
        public void Companion_PinsADifferentColor()
        {
            foreach (var color in new[] { PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime })
            {
                var target = PieceTrait.TargetColorFor(PieceTraitKind.Companion, color);
                Assert.IsTrue(target.HasValue);
                Assert.AreNotEqual(color, target.Value);
            }
            Assert.AreEqual(PieceColor.Teal, PieceTrait.TargetColorFor(PieceTraitKind.Tinted, PieceColor.Teal));
            Assert.IsNull(PieceTrait.TargetColorFor(PieceTraitKind.Pip, PieceColor.Teal));
        }

        [Test]
        public void PipAndLucky_ScoreFlatPoints()
        {
            Assert.AreEqual(ScoringConstants.PipBonus, PlantAt(NewRun(), PieceTraitKind.Pip, PieceColor.Coral, 2, 2).TraitBonus);
            for (int seed = 1; seed <= 25; seed++)
            {
                int bonus = PlantAt(NewRun(seed), PieceTraitKind.Lucky, PieceColor.Coral, 2, 2).TraitBonus;
                Assert.That(bonus, Is.InRange(0, ScoringConstants.LuckyMaxBonus));
            }
        }

        [Test]
        public void Companion_PaysPerTouchingTileOfItsColor()
        {
            var run = NewRun();
            Fill(run, PieceColor.Teal, (1, 2), (3, 2));
            Fill(run, PieceColor.Violet, (2, 1));
            Assert.AreEqual(2 * ScoringConstants.CompanionBonusPerTile, PlantAt(run, PieceTraitKind.Companion, PieceColor.Coral, 2, 2, PieceColor.Teal).TraitBonus);
        }

        [Test]
        public void BorderHeartAndCorner_ReadThePosition()
        {
            Assert.AreEqual(ScoringConstants.BorderBonus, PlantAt(NewRun(), PieceTraitKind.Border, PieceColor.Coral, 0, 3).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Border, PieceColor.Coral, 2, 2).TraitBonus);
            Assert.AreEqual(ScoringConstants.HeartBonus, PlantAt(NewRun(), PieceTraitKind.Heart, PieceColor.Coral, 2, 3).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Heart, PieceColor.Coral, 0, 0).TraitBonus);
            Assert.AreEqual(ScoringConstants.CornerBonus, PlantAt(NewRun(), PieceTraitKind.Corner, PieceColor.Coral, GridManager.Size - 1, 0).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Corner, PieceColor.Coral, 1, 0).TraitBonus);
        }

        [Test]
        public void HermitAndCuddly_ReadTheNeighbors()
        {
            Assert.AreEqual(ScoringConstants.HermitBonus, PlantAt(NewRun(), PieceTraitKind.Hermit, PieceColor.Coral, 2, 2).TraitBonus);
            var run = NewRun();
            Fill(run, PieceColor.Teal, (1, 2));
            Assert.AreEqual(0, PlantAt(run, PieceTraitKind.Hermit, PieceColor.Coral, 2, 2).TraitBonus);

            var crowded = NewRun();
            Fill(crowded, PieceColor.Teal, (1, 2), (3, 2), (2, 1));
            Assert.AreEqual(3 * ScoringConstants.CuddlyBonusPerNeighbor, PlantAt(crowded, PieceTraitKind.Cuddly, PieceColor.Coral, 2, 2).TraitBonus);

            var diagonal = NewRun();
            Fill(diagonal, PieceColor.Teal, (1, 1), (3, 3), (2, 1));
            Assert.AreEqual(3 * ScoringConstants.CuddlyBonusPerNeighbor, PlantAt(diagonal, PieceTraitKind.Cuddly, PieceColor.Coral, 2, 2).TraitBonus, "two diagonals and one side");

            var domino = NewRun();
            int slot = HandSlotWith(domino, new PieceToken(ShapeId.DomH, PieceColor.Coral, new PieceTrait(PieceTraitKind.Cuddly, 0)));
            var placed = domino.PlacePiece(slot, 2, 2).Placement;
            Assert.AreEqual(ScoringConstants.CuddlyBonusPerNeighbor, placed.TraitBonus, "the other tile of its own domino counts");
        }

        [Test]
        public void HarmonyAndPrism_ReadTheNeighborColors()
        {
            var same = NewRun();
            Fill(same, PieceColor.Coral, (1, 2), (3, 2));
            Assert.AreEqual(ScoringConstants.HarmonyBonus, PlantAt(same, PieceTraitKind.Harmony, PieceColor.Coral, 2, 2).TraitBonus);
            var mixed = NewRun();
            Fill(mixed, PieceColor.Coral, (1, 2));
            Fill(mixed, PieceColor.Teal, (3, 2));
            Assert.AreEqual(0, PlantAt(mixed, PieceTraitKind.Harmony, PieceColor.Coral, 2, 2).TraitBonus);

            var rainbow = NewRun();
            Fill(rainbow, PieceColor.Teal, (1, 2));
            Fill(rainbow, PieceColor.Violet, (3, 2));
            Fill(rainbow, PieceColor.Lime, (2, 1));
            Assert.AreEqual(ScoringConstants.PrismBonus, PlantAt(rainbow, PieceTraitKind.Prism, PieceColor.Coral, 2, 2).TraitBonus);
            Assert.AreEqual(0, PlantAt(mixed, PieceTraitKind.Prism, PieceColor.Violet, 4, 4).TraitBonus);
        }

        [Test]
        public void ColonyDawnDuskMiserElderBreezeGardenerStormy_ReadTheRunState()
        {
            var big = NewRun();
            Fill(big, PieceColor.Coral, (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (0, 1), (1, 1), (2, 1), (3, 1), (4, 1));
            Assert.AreEqual(ScoringConstants.ColonyBonus, PlantAt(big, PieceTraitKind.Colony, PieceColor.Coral, 0, 2).TraitBonus, "11 cells in the group");
            var small = NewRun();
            Fill(small, PieceColor.Coral, (0, 0));
            Assert.AreEqual(0, PlantAt(small, PieceTraitKind.Colony, PieceColor.Coral, 1, 0).TraitBonus);

            Assert.AreEqual(ScoringConstants.DawnBonus, PlantAt(NewRun(), PieceTraitKind.Dawn, PieceColor.Coral, 2, 2).TraitBonus, "the very first seed");
            var late = NewRun();
            for (int i = 0; i < ScoringConstants.DawnSeeds; i++)
            {
                PlantAt(late, PieceTraitKind.Pip, PieceColor.Violet, i, 5);
            }
            Assert.AreEqual(0, PlantAt(late, PieceTraitKind.Dawn, PieceColor.Coral, 4, 4).TraitBonus, "the fourth seed is too late");

            var dusk = NewRun();
            int duskSlot = HandSlotWith(dusk, new PieceToken(ShapeId.Single, PieceColor.Coral, new PieceTrait(PieceTraitKind.Dusk, 0)));
            Assert.AreEqual(0, dusk.PlacePiece(duskSlot, 2, 2).Placement.TraitBonus, "other seeds are still in the hand");
            var lastInHand = NewRun();
            int lastSlot = HandSlotWith(lastInHand, new PieceToken(ShapeId.Single, PieceColor.Coral, new PieceTrait(PieceTraitKind.Dusk, 0)));
            for (int other = 0; other < lastInHand.Deck.Hand.Count; other++)
            {
                if (other == lastSlot || !lastInHand.Deck.Hand[other].HasValue)
                {
                    continue;
                }
                var shape = PieceShapeCatalog.GetRotated(lastInHand.Deck.Hand[other].Value.Shape, lastInHand.Deck.HandRotations[other]);
                var positions = new List<Vector2Int>(GridManager.AllPositions());
                positions.Reverse();
                foreach (var pos in positions)
                {
                    if (lastInHand.Grid.CanPlace(shape, pos.x, pos.y))
                    {
                        Assert.IsTrue(lastInHand.PlacePiece(other, pos.x, pos.y).Placement.Success);
                        break;
                    }
                }
            }
            Assert.IsTrue(lastInHand.Deck.Hand[lastSlot].HasValue);
            Assert.AreEqual(ScoringConstants.DuskBonus, lastInHand.PlacePiece(lastSlot, 2, 2).Placement.TraitBonus);

            var rich = NewRun();
            rich.DebugSetLueur(7);
            Assert.AreEqual(7, PlantAt(rich, PieceTraitKind.Miser, PieceColor.Coral, 2, 2).TraitBonus);
            var richer = NewRun();
            richer.DebugSetLueur(500);
            Assert.AreEqual(500 * ScoringConstants.MiserBonusPerPollen, PlantAt(richer, PieceTraitKind.Miser, PieceColor.Coral, 2, 2).TraitBonus);

            var breeze = NewRun();
            breeze.DebugSetShufflesRemaining(100);
            // HandSlotWith burns shuffles looking for the seed, so compare against what is left after dealing it.
            int slot = HandSlotWith(breeze, new PieceToken(ShapeId.Single, PieceColor.Coral, new PieceTrait(PieceTraitKind.Breeze, 0)));
            int shuffles = breeze.ShufflesRemaining;
            Assert.AreEqual(shuffles * ScoringConstants.BreezeBonusPerShuffle, breeze.PlacePiece(slot, 2, 2).Placement.TraitBonus);

            var gardener = NewRun();
            Assert.IsTrue(gardener.DebugGrantModifier(ModifierId.MultUn));
            Assert.IsTrue(gardener.DebugGrantModifier(ModifierId.MultDeux));
            Assert.AreEqual(2 * ScoringConstants.GardenerBonusPerFertilizer, PlantAt(gardener, PieceTraitKind.Gardener, PieceColor.Coral, 2, 2).TraitBonus);

            var storm = NewRun();
            storm.DebugSetWeather(WeatherId.Rain);
            Assert.AreEqual(ScoringConstants.StormyBonus, PlantAt(storm, PieceTraitKind.Stormy, PieceColor.Coral, 2, 2).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Stormy, PieceColor.Coral, 2, 2).TraitBonus);

            var elder = NewRun();
            elder.DebugForceRoundComplete();
            Assert.IsTrue(elder.LeaveShop());
            Assert.AreEqual(ScoringConstants.ElderBonusPerRound, PlantAt(elder, PieceTraitKind.Elder, PieceColor.Coral, 2, 2).TraitBonus);
        }

        [Test]
        public void UpgradedTiles_AreCountedAgain_EveryTimeTheyScore()
        {
            var run = NewRun();
            Assert.AreEqual(ScoringConstants.PipBonus, PlantAt(run, PieceTraitKind.Pip, PieceColor.Coral, 2, 2).TraitBonus);
            // Joining the same Coral group scores the new Pip AND the older one again.
            Assert.AreEqual(2 * ScoringConstants.PipBonus, PlantAt(run, PieceTraitKind.Pip, PieceColor.Coral, 3, 2).TraitBonus);
            // A tile that is not part of the scored group does not count again.
            Assert.AreEqual(ScoringConstants.PipBonus, PlantAt(run, PieceTraitKind.Pip, PieceColor.Teal, 5, 5).TraitBonus);
        }

        [Test]
        public void OlderUpgradedTile_KeepsItsPositionalBonus_WhenAnotherSeedJoinsItsGroup()
        {
            var run = NewRun();
            PlantAt(run, PieceTraitKind.Corner, PieceColor.Coral, 0, 0);
            var joined = PlantAt(run, PieceTraitKind.Pip, PieceColor.Coral, 1, 0);
            Assert.AreEqual(ScoringConstants.CornerBonus + ScoringConstants.PipBonus, joined.TraitBonus, "the Corner Sprout scores again from its corner");
        }

        [Test]
        public void OlderGoldenTile_ScoresItsGoldenBonusAgain()
        {
            var control = NewRun();
            Fill(control, PieceColor.Coral, (2, 2));
            int single = PlantAt(control, PieceTraitKind.Golden, PieceColor.Coral, 3, 2).GoldenBonus;

            var run = NewRun();
            PlantAt(run, PieceTraitKind.Golden, PieceColor.Coral, 2, 2);
            int both = PlantAt(run, PieceTraitKind.Golden, PieceColor.Coral, 3, 2).GoldenBonus;
            Assert.Greater(single, 0);
            Assert.Greater(both, single, "the older golden tile is golden again");
        }

        [Test]
        public void ClusterAndCrossfire_ReadTheBoard()
        {
            var run = NewRun();
            PlantAt(run, PieceTraitKind.Pip, PieceColor.Coral, 2, 2);
            Assert.AreEqual(ScoringConstants.ClusterBonusPerUpgrade + ScoringConstants.PipBonus, PlantAt(run, PieceTraitKind.Cluster, PieceColor.Coral, 3, 2).TraitBonus, "the older Pip counts as an upgraded tile, and scores itself again");

            var cross = NewRun();
            Fill(cross, PieceColor.Teal, (0, 2), (1, 2), (2, 1), (2, 4));
            Assert.AreEqual(4 * ScoringConstants.CrossfireBonusPerPlot, PlantAt(cross, PieceTraitKind.Crossfire, PieceColor.Coral, 2, 2).TraitBonus);
        }

        [Test]
        public void Even_RainbowRow_Sunseeker_Hunter_Executioner()
        {
            var even = NewRun();
            Fill(even, PieceColor.Coral, (2, 2));
            Assert.AreEqual(ScoringConstants.EvenBonus, PlantAt(even, PieceTraitKind.Even, PieceColor.Coral, 3, 2).TraitBonus, "a group of 2");
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Even, PieceColor.Coral, 2, 2).TraitBonus, "a lone tile is a group of 1");

            var rainbow = NewRun();
            Fill(rainbow, PieceColor.Teal, (1, 2));
            Fill(rainbow, PieceColor.Violet, (2, 2));
            Fill(rainbow, PieceColor.Lime, (3, 2));
            Assert.AreEqual(ScoringConstants.RainbowRowBonus, PlantAt(rainbow, PieceTraitKind.RainbowRow, PieceColor.Coral, 0, 2).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.RainbowRow, PieceColor.Coral, 0, 2).TraitBonus);

            var sun = NewRun();
            sun.DebugSetWeather(WeatherId.GrowLamp);
            Assert.AreEqual(ScoringConstants.SunseekerBonus, PlantAt(sun, PieceTraitKind.Sunseeker, PieceColor.Coral, 2, 2).TraitBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Sunseeker, PieceColor.Coral, 2, 2).TraitBonus);

            var hunt = NewRun();
            hunt.DebugSetEncounter(EnemyId.Basic, EnemyId.Locker);
            Assert.AreEqual(2 * ScoringConstants.HunterBonusPerEnemy, PlantAt(hunt, PieceTraitKind.Hunter, PieceColor.Coral, 2, 2).TraitBonus);

            var exec = NewRun();
            exec.DebugSetEncounter(EnemyId.Basic);
            Assert.AreEqual(0, PlantAt(exec, PieceTraitKind.Executioner, PieceColor.Coral, 2, 2).TraitBonus, "full HP");
            var low = NewRun();
            low.DebugSetEncounter(EnemyId.Basic);
            low.CurrentEncounter[0].ApplyDamage(low.CurrentEncounter[0].CurrentMaxHp * 9 / 10);
            Assert.AreEqual(ScoringConstants.ExecutionerBonus, PlantAt(low, PieceTraitKind.Executioner, PieceColor.Coral, 2, 2).TraitBonus);
        }

        [Test]
        public void Spillover_Honeycomb_Dewdrop_Keystone_Growth_Gambler()
        {
            var run = NewRun();
            Fill(run, PieceColor.Teal, (0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
            Assert.AreEqual(ScoringConstants.SpilloverBonus, PlantAt(run, PieceTraitKind.Spillover, PieceColor.Coral, GridManager.Size - 1, 0).TraitBonus, "its own tile was harvested");
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Spillover, PieceColor.Coral, 2, 2).TraitBonus);

            var honey = NewRun();
            honey.DebugSetLueur(0);
            var honeyResult = PlantAt(honey, PieceTraitKind.Honeycomb, PieceColor.Coral, 2, 2);
            Assert.AreEqual(ScoringConstants.HoneycombPollen, honeyResult.ModifierLueurBonus);
            Assert.AreEqual(ScoringConstants.HoneycombPollen, honey.Lueur);

            var dew = NewRun();
            Fill(dew, PieceColor.Teal, (0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
            Assert.AreEqual(ScoringConstants.DewdropPollenPerLine, PlantAt(dew, PieceTraitKind.Dewdrop, PieceColor.Coral, GridManager.Size - 1, 0).ModifierLueurBonus);

            Assert.AreEqual(ScoringConstants.KeystoneMultBonus, PlantAt(NewRun(), PieceTraitKind.Keystone, PieceColor.Coral, 0, 0).AdditiveMultBonus);
            Assert.AreEqual(ScoringConstants.KeystoneMultBonus, PlantAt(NewRun(), PieceTraitKind.Keystone, PieceColor.Coral, 2, 2).AdditiveMultBonus);
            Assert.AreEqual(0, PlantAt(NewRun(), PieceTraitKind.Keystone, PieceColor.Coral, 1, 0).AdditiveMultBonus);

            var growth = NewRun();
            Assert.AreEqual(1, PlantAt(growth, PieceTraitKind.Growth, PieceColor.Coral, 2, 2).TraitBonus);
            // Joining its group scores it again, one point richer, next to the new Pip.
            Assert.AreEqual(ScoringConstants.PipBonus + 2, PlantAt(growth, PieceTraitKind.Pip, PieceColor.Coral, 3, 2).TraitBonus);

            var seen = new HashSet<int>();
            for (int seed = 1; seed <= 40; seed++)
            {
                seen.Add(PlantAt(NewRun(seed), PieceTraitKind.Gambler, PieceColor.Coral, 2, 2).TraitBonus);
            }
            CollectionAssert.AreEquivalent(new[] { ScoringConstants.GamblerWinBonus, -ScoringConstants.GamblerLossPenalty }, seen);
        }

        [Test]
        public void WardenAndAegis_FreeAndShieldPlots()
        {
            var run = NewRun();
            foreach (var p in new[] { new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(2, 1), new Vector2Int(2, 3) })
            {
                var cell = run.Grid.GetCell(p);
                cell.IsLocked = true;
                cell.IsFrozen = true;
            }
            run.Grid.GetCell(2, 3).IsPoisoned = true;
            run.Grid.GetCell(0, 0).IsLocked = true;
            var result = PlantAt(run, PieceTraitKind.Warden, PieceColor.Coral, 2, 2);
            Assert.AreEqual(4, result.CleansedCells.Count);
            foreach (var p in result.CleansedCells)
            {
                var cell = run.Grid.GetCell(p);
                Assert.IsFalse(cell.IsLocked || cell.IsFrozen || cell.IsPoisoned, p.ToString());
                Assert.IsTrue(cell.IsImmune);
            }
            Assert.IsTrue(run.Grid.GetCell(0, 0).IsLocked, "plots that are not around it keep their lock");

            // Immune plots are never picked again by the enemy effects.
            var picked = run.Grid.FreezeRandomCells(GridManager.Size * GridManager.Size, new SystemRandomProvider(1));
            foreach (var p in picked)
            {
                Assert.IsFalse(run.Grid.GetCell(p).IsImmune && (p == new Vector2Int(1, 2) || p == new Vector2Int(3, 2) || p == new Vector2Int(2, 1) || p == new Vector2Int(2, 3)), p.ToString());
            }

            var own = NewRun();
            own.Grid.GetCell(2, 2).IsPoisoned = true;
            var aegis = PlantAt(own, PieceTraitKind.Aegis, PieceColor.Coral, 2, 2);
            Assert.AreEqual(1, aegis.CleansedCells.Count);
            Assert.IsFalse(own.Grid.GetCell(2, 2).IsPoisoned);
            Assert.IsTrue(own.Grid.GetCell(2, 2).IsImmune);
        }

        [Test]
        public void Sower_PlantsAFreeTileOnANeighborPlot()
        {
            var run = NewRun();
            var first = PlantAt(run, PieceTraitKind.Sower, PieceColor.Teal, 2, 2);
            Assert.AreEqual(1, first.SownTiles.Count);
            var sown = first.SownTiles[0];
            Assert.AreEqual(PieceColor.Teal, sown.Color);
            Assert.IsTrue(run.Grid.GetCell(sown.Position).IsFilled);
            Assert.AreEqual(1, Mathf.Abs(sown.Position.x - 2) + Mathf.Abs(sown.Position.y - 2), "it lands right next to its sower");
        }

        [Test]
        public void Harvester_PaysPerLineItsSeedHarvests()
        {
            var run = NewRun();
            Fill(run, PieceColor.Teal, (0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
            Assert.AreEqual(ScoringConstants.HarvesterBonusPerLine, PlantAt(run, PieceTraitKind.Harvester, PieceColor.Coral, GridManager.Size - 1, 0).TraitBonus);
        }
    }
}
