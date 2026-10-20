using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>Barometer, Storm Chaser, Weatherproof, Thorns, Pest Control, Haste, Whirlwind and Cornerstone.</summary>
    public sealed class NewFertilizerTests
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
        public void Barometer_PaysMultOnlyWhenTheSkiesAreNotClear()
        {
            Assert.AreEqual(ScoringConstants.BarometerMultBonus, PlaceAny(NewRun(WeatherId.Rain, ModifierId.Barometre)).AdditiveMultBonus);
            Assert.AreEqual(0, PlaceAny(NewRun(WeatherId.Clear, ModifierId.Barometre)).AdditiveMultBonus);
        }

        [Test]
        public void StormChaser_DoublesMultOnlyInHarshWeather()
        {
            foreach (var harsh in new[] { WeatherId.Frost, WeatherId.Heatwave, WeatherId.Hurricane })
            {
                var placement = PlaceAny(NewRun(harsh, ModifierId.ChasseurTempete));
                Assert.AreEqual(ScoringConstants.StormChaserMultiplier, placement.ModifierMultiplier, harsh.ToString());
                Assert.GreaterOrEqual(placement.Mult, ScoringConstants.StormChaserMultiplier);
            }
            Assert.AreEqual(1, PlaceAny(NewRun(WeatherId.Rain, ModifierId.ChasseurTempete)).ModifierMultiplier);
            Assert.AreEqual(1, PlaceAny(NewRun(WeatherId.Clear, ModifierId.ChasseurTempete)).ModifierMultiplier);
        }

        [Test]
        public void Haste_ScalesWithTheSeedsLeftThisRound()
        {
            var run = NewRun(WeatherId.Clear, ModifierId.Hate);
            float expected = run.PiecesRemainingThisRound * ScoringConstants.HasteMultPerSeedLeft;
            Assert.AreEqual(expected, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
            float next = run.PiecesRemainingThisRound * ScoringConstants.HasteMultPerSeedLeft;
            Assert.Less(next, expected, "one seed fewer left, a little less Mult");
            Assert.AreEqual(next, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
        }

        [Test]
        public void Whirlwind_ScalesWithTheShufflesInTheBank()
        {
            var run = NewRun(WeatherId.Clear, ModifierId.Tourbillon);
            Assert.AreEqual(run.ShufflesRemaining * ScoringConstants.WhirlwindMultPerShuffle, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
            run.DebugSetShufflesRemaining(0);
            Assert.AreEqual(0f, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
        }

        [Test]
        public void PestControl_CountsTheEnemiesAlreadyDefeated()
        {
            var run = NewRun(WeatherId.Clear, ModifierId.Exterminateur);
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Locker);
            Assert.AreEqual(0, PlaceAny(run).AdditiveMultBonus);

            run.CurrentEncounter[0].ApplyDamage(run.CurrentEncounter[0].CurrentMaxHp);
            Assert.IsTrue(run.CurrentEncounter[0].IsDead);
            Assert.AreEqual(ScoringConstants.PestControlMultPerKill, PlaceAny(run).AdditiveMultBonus);
        }

        [Test]
        public void Thorns_PayMultOnlyWhileAnEnemyHasALockOrBlightOnTheGarden()
        {
            var calm = NewRun(WeatherId.Clear, ModifierId.Epines);
            Assert.AreEqual(0, PlaceAny(calm).AdditiveMultBonus, "round 1's lone Basic has no malus");

            var run = NewRun(WeatherId.Clear, ModifierId.Epines);
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Locker);
            Assert.AreEqual(0, PlaceAny(run).AdditiveMultBonus, "nothing locked yet");
            run.ShuffleHand();
            Assert.IsTrue(run.CurrentEncounter[1].LockedCell.HasValue);
            Assert.AreEqual(ScoringConstants.ThornsMultBonus, PlaceAny(run).AdditiveMultBonus);
        }

        [Test]
        public void Weatherproof_StopsTheHeatwaveBurn_ButNotWithoutIt()
        {
            var bare = NewRun(WeatherId.Heatwave);
            PlaceAny(bare);
            bare.ShuffleHand();
            Assert.IsTrue(bare.LastBurnedTile.HasValue, "a Heatwave Shuffle burns a tile");

            var proof = NewRun(WeatherId.Heatwave, ModifierId.Impermeable);
            PlaceAny(proof);
            proof.ShuffleHand();
            Assert.IsFalse(proof.LastBurnedTile.HasValue, "Weatherproof shrugs it off");
        }

        [Test]
        public void Weatherproof_StopsFrostFromFreezingPlots()
        {
            for (int seed = 1; seed < 300; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed));
                run.DebugGrantModifier(ModifierId.Impermeable);
                for (int i = 0; i < 8; i++)
                {
                    run.DebugForceRoundComplete();
                    if (run.State == RunState.AwaitingShop)
                    {
                        run.LeaveShop();
                    }
                    if (run.CurrentWeather != WeatherId.Frost)
                    {
                        continue;
                    }
                    foreach (var pos in GridManager.AllPositions())
                    {
                        Assert.IsFalse(run.Grid.GetCell(pos).IsFrozen, "frost cannot freeze a Weatherproof garden");
                    }
                    return;
                }
            }
            Assert.Fail("no frosty round found");
        }

        [Test]
        public void Cornerstone_DoublesWhenAPlacedTileCoversACorner()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var mods = new List<ModifierId> { ModifierId.PierreAngulaire };
            int last = GridManager.Size - 1;
            foreach (var corner in new[] { new Vector2Int(0, 0), new Vector2Int(last, 0), new Vector2Int(0, last), new Vector2Int(last, last) })
            {
                var fresh = new GridManager();
                var result = fresh.PlacePiece(single, PieceColor.Coral, corner.x, corner.y, mods);
                Assert.AreEqual(ScoringConstants.CornerstoneMultiplier, result.ModifierMultiplier, corner.ToString());
                Assert.AreEqual(ScoringConstants.CornerstoneMultiplier, result.Mult, 1e-4f);
            }
            var middle = grid.PlacePiece(single, PieceColor.Coral, 2, 2, mods);
            Assert.AreEqual(1, middle.ModifierMultiplier);
            // A bigger piece only needs one of its tiles on the corner.
            var domino = PieceShapeCatalog.Get(ShapeId.DomH);
            var corner2 = new GridManager().PlacePiece(domino, PieceColor.Coral, 0, 0, mods);
            Assert.AreEqual(ScoringConstants.CornerstoneMultiplier, corner2.ModifierMultiplier);
        }

        [Test]
        public void Cornerstone_FollowsTheSmallerGreenhousePlot()
        {
            new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Greenhouse);
            try
            {
                var result = new GridManager().PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 4, 4, new List<ModifierId> { ModifierId.PierreAngulaire });
                Assert.AreEqual(ScoringConstants.CornerstoneMultiplier, result.ModifierMultiplier, "(4,4) is a corner of the 5x5 plot");
                var inner = new GridManager().PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 5 - 2, 0, new List<ModifierId> { ModifierId.PierreAngulaire });
                Assert.AreEqual(1, inner.ModifierMultiplier);
            }
            finally
            {
                GridManager.ResetSize();
                PieceColorUtility.ResetBaseColors();
            }
        }

        [Test]
        public void EveryNewFertilizer_IsInTheCatalog_WithADescriptionAndAProgressiveStateWhereItHasOne()
        {
            var ids = new[] { ModifierId.Barometre, ModifierId.ChasseurTempete, ModifierId.Impermeable, ModifierId.Epines, ModifierId.Exterminateur, ModifierId.Hate, ModifierId.Tourbillon, ModifierId.PierreAngulaire };
            var run = NewRun(WeatherId.Clear);
            foreach (var id in ids)
            {
                var def = ModifierCatalog.Get(id);
                Assert.IsNotNull(def, id.ToString());
                Assert.IsNotEmpty(def.Description);
                CollectionAssert.Contains(ModifierCatalog.All, def);
            }
            StringAssert.Contains("Currently", run.GetProgressiveModifierStateText(ModifierId.Hate));
            StringAssert.Contains("Currently", run.GetProgressiveModifierStateText(ModifierId.Tourbillon));
        }
    }
}
