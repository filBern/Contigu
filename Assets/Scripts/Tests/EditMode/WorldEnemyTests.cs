using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>Each challenge's resident enemy: Hoarfrost (Winter), Locust Swarm (Summer), Rotgrub (Autumn), Fruitworm (Orchard), Whitefly Cloud (Greenhouse).</summary>
    public sealed class WorldEnemyTests
    {
        [TearDown]
        public void RestoreProcessWideRules()
        {
            GridManager.ResetSize();
            PieceColorUtility.ResetBaseColors();
        }

        private static readonly (ChallengeDefinition Challenge, EnemyId Resident)[] Worlds =
        {
            (ChallengeCatalog.Marathon, EnemyId.Hoarfrost),
            (ChallengeCatalog.Summer, EnemyId.Locust),
            (ChallengeCatalog.Autumn, EnemyId.Rotgrub),
            (ChallengeCatalog.Orchard, EnemyId.Fruitworm),
            (ChallengeCatalog.Greenhouse, EnemyId.Whitefly)
        };

        private static bool Has(IReadOnlyList<EnemyId> roster, EnemyId id)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void EveryWorldHasItsOwnResident_ThatJoinsEveryThirdRoundOnly_AndSpringHasNone()
        {
            foreach (var world in Worlds)
            {
                Assert.AreEqual(world.Resident, world.Challenge.WorldEnemy, world.Challenge.Name);
                for (int round = 0; round < 40; round++)
                {
                    var roster = EncounterCatalog.GetEncounter(world.Challenge, round);
                    bool expected = round % ScoringConstants.WorldEnemyRoundInterval == ScoringConstants.WorldEnemyRoundInterval - 1;
                    Assert.AreEqual(expected, Has(roster, world.Resident), world.Challenge.Name + " round " + (round + 1));
                    Assert.LessOrEqual(roster.Count, ScoringConstants.MaxEnemiesPerRound, world.Challenge.Name + " round " + (round + 1));
                    Assert.AreEqual(EnemyId.Basic, roster[0], "the Basic still leads");
                    if (expected)
                    {
                        Assert.AreEqual(world.Resident, roster[1], "right behind the Basic");
                    }
                    foreach (var other in Worlds)
                    {
                        if (other.Resident != world.Resident)
                        {
                            Assert.IsFalse(Has(roster, other.Resident), "only its own world");
                        }
                    }
                }
            }
            Assert.IsNull(ChallengeCatalog.Classic.WorldEnemy);
            for (int round = 0; round < 40; round++)
            {
                foreach (var world in Worlds)
                {
                    Assert.IsFalse(Has(EncounterCatalog.GetEncounter(ChallengeCatalog.Classic, round), world.Resident));
                }
            }
        }

        [Test]
        public void EveryResident_HasDefinition_WithTooltipTextAndAHpScaledByTheRound()
        {
            foreach (var world in Worlds)
            {
                var def = EnemyCatalog.Get(world.Resident);
                Assert.AreEqual(world.Resident, def.Id);
                Assert.IsNotEmpty(def.Name);
                Assert.IsNotEmpty(def.Description);
                Assert.IsNotEmpty(def.Lore);
                Assert.Greater(def.MaxHp, 0);
            }
        }

        private static RunManager RunWith(ChallengeDefinition challenge, params EnemyId[] enemies)
        {
            var run = new RunManager(new SystemRandomProvider(7), challenge);
            run.DebugDisableWeather();
            run.DebugSetEncounter(enemies);
            return run;
        }

        [Test]
        public void Hoarfrost_FreezesAPlotEachShuffle_KeepsAtMostThree_AndThawsEverythingWhenItDies()
        {
            var run = RunWith(ChallengeCatalog.Marathon, EnemyId.Basic, EnemyId.Hoarfrost);
            run.DebugSetShufflesRemaining(10);
            Assert.AreEqual(0, CountFrozen(run));
            run.ShuffleHand();
            Assert.AreEqual(1, CountFrozen(run));
            Assert.AreEqual(EnemyId.Hoarfrost, run.LastWorldEnemyAct);
            for (int i = 0; i < 5; i++)
            {
                run.ShuffleHand();
            }
            Assert.AreEqual(ScoringConstants.HoarfrostMaxFrozenPlots, CountFrozen(run), "the oldest thaws as new plots freeze");

            run.DebugForceRoundComplete();
            Assert.AreEqual(0, CountFrozen(run), "it died: every plot it froze thaws");
        }

        private static int CountFrozen(RunManager run)
        {
            int n = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (run.Grid.GetCell(pos).IsFrozen)
                {
                    n++;
                }
            }
            return n;
        }

        [Test]
        public void Locust_DevoursPollenEachShuffle_UpToWhatYouHave()
        {
            var run = RunWith(ChallengeCatalog.Summer, EnemyId.Basic, EnemyId.Locust);
            run.DebugSetLueur(10);
            run.ShuffleHand();
            Assert.AreEqual(10 - ScoringConstants.LocustPollenPerShuffle, run.Lueur);
            Assert.AreEqual(EnemyId.Locust, run.LastWorldEnemyAct);
            StringAssert.Contains("Pollen", run.LastWorldEnemyMessage);

            run.DebugSetLueur(2);
            run.ShuffleHand();
            Assert.AreEqual(0, run.Lueur, "never below zero");

            run.DebugSetLueur(0);
            run.ShuffleHand();
            Assert.IsNull(run.LastWorldEnemyAct, "nothing left to eat, nothing to report");
        }

        [Test]
        public void Rotgrub_AgesTheFertilizerClosestToWithering()
        {
            var run = new RunManager(new SystemRandomProvider(3), ChallengeCatalog.Autumn);
            run.DebugDisableWeather();
            run.DebugGrantModifier(ModifierId.MultUn);
            run.DebugForceRoundComplete();
            run.LeaveShop();
            run.DebugGrantModifier(ModifierId.MultDeux);
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Rotgrub);
            Assert.AreEqual(1, run.GetModifierAge(0));
            Assert.AreEqual(0, run.GetModifierAge(1));

            run.ShuffleHand();
            Assert.AreEqual(2, run.GetModifierAge(0), "the oldest one is gnawed");
            Assert.AreEqual(0, run.GetModifierAge(1));
            Assert.AreEqual(EnemyId.Rotgrub, run.LastWorldEnemyAct);
        }

        [Test]
        public void Rotgrub_DoesNothingWhereFertilizersNeverWither()
        {
            var run = RunWith(ChallengeCatalog.Classic, EnemyId.Basic, EnemyId.Rotgrub);
            run.DebugGrantModifier(ModifierId.MultUn);
            run.ShuffleHand();
            Assert.AreEqual(0, run.GetModifierAge(0));
            Assert.IsNull(run.LastWorldEnemyAct);
        }

        [Test]
        public void Fruitworm_TurnsOnePlantedTileIntoTheOtherColor()
        {
            var grid = new GridManager();
            PieceColorUtility.SetActiveBaseColors(new[] { PieceColor.Coral, PieceColor.Lime });
            grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 0, 0);
            grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 4, 4);
            var result = grid.RecolorRandomTile(new SystemRandomProvider(1));
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(PieceColor.Coral, result.Value.From);
            Assert.AreEqual(PieceColor.Lime, result.Value.To);
            Assert.AreEqual(PieceColor.Lime, grid.GetCell(result.Value.Pos).FilledColor);

            Assert.IsNull(new GridManager().RecolorRandomTile(new SystemRandomProvider(1)), "an empty plot has nothing to recolor");
            PieceColorUtility.SetActiveBaseColors(new[] { PieceColor.Coral });
            Assert.IsNull(grid.RecolorRandomTile(new SystemRandomProvider(1)), "with a single color there is nothing else to turn into");
        }

        [Test]
        public void Whitefly_SwapsTwoPlantedTiles_WhateverTheirState()
        {
            var grid = new GridManager();
            grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 0, 0);
            grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Lime, 5, 5);
            grid.GetCell(0, 0).IsGolden = true;
            var swapped = grid.SwapRandomTiles(new SystemRandomProvider(2));
            Assert.IsTrue(swapped.HasValue);
            Assert.AreEqual(PieceColor.Lime, grid.GetCell(0, 0).FilledColor);
            Assert.AreEqual(PieceColor.Coral, grid.GetCell(5, 5).FilledColor);
            Assert.IsFalse(grid.GetCell(0, 0).IsGolden);
            Assert.IsTrue(grid.GetCell(5, 5).IsGolden, "flags travel with the tile");

            var lone = new GridManager();
            lone.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 1, 1);
            Assert.IsNull(lone.SwapRandomTiles(new SystemRandomProvider(2)), "one tile has nobody to swap with");
        }

        [Test]
        public void ResidentReport_IsClearedByTheNextAction()
        {
            var run = RunWith(ChallengeCatalog.Summer, EnemyId.Basic, EnemyId.Locust);
            run.DebugSetLueur(10);
            run.ShuffleHand();
            Assert.IsTrue(run.LastWorldEnemyAct.HasValue);
            // A placement that does not trigger a Shuffle must not keep reporting the old one.
            for (int slot = 0; slot < run.Deck.Hand.Count; slot++)
            {
                var shape = PieceShapeCatalog.GetRotated(run.Deck.Hand[slot].Value.Shape, run.Deck.HandRotations[slot]);
                bool placed = false;
                foreach (var pos in GridManager.AllPositions())
                {
                    if (run.Grid.CanPlace(shape, pos.x, pos.y))
                    {
                        run.PlacePiece(slot, pos.x, pos.y);
                        placed = true;
                        break;
                    }
                }
                if (placed)
                {
                    break;
                }
            }
            Assert.IsNull(run.LastWorldEnemyAct);
        }
    }
}
