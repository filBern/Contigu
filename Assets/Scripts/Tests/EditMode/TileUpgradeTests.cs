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
            Assert.AreEqual(ScoringConstants.MiserMaxBonus, PlantAt(richer, PieceTraitKind.Miser, PieceColor.Coral, 2, 2).TraitBonus);

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
        public void Harvester_PaysPerLineItsSeedHarvests()
        {
            var run = NewRun();
            Fill(run, PieceColor.Teal, (0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
            Assert.AreEqual(ScoringConstants.HarvesterBonusPerLine, PlantAt(run, PieceTraitKind.Harvester, PieceColor.Coral, GridManager.Size - 1, 0).TraitBonus);
        }
    }
}
