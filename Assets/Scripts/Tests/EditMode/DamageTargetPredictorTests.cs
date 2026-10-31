using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public sealed class DamageTargetPredictorTests
    {
        private static RunManager RunWithEncounter(params EnemyId[] enemies)
        {
            var run = new RunManager(new SystemRandomProvider(5));
            run.DebugSetEncounter(enemies);
            return run;
        }

        /// <summary>Puts a prepared token in hand slot 0 (only slot 0 is used), returning the slot index.</summary>
        private static int HandWith(RunManager run, ShapeId shape, PieceColor color, PieceTraitKind? trait)
        {
            var token = trait.HasValue ? new PieceToken(shape, color, new PieceTrait(trait.Value, 0)) : new PieceToken(shape, color);
            run.Deck.AddPreparedToken(token);
            int nextSlotToPlay = 0;
            for (int guard = 0; guard < 300; guard++)
            {
                for (int i = 0; i < DeckManager.HandSize; i++)
                {
                    var slot = run.Deck.Hand[i];
                    if (slot.HasValue && slot.Value.Shape == shape && slot.Value.Color == color
                        && (slot.Value.Trait.HasValue ? slot.Value.Trait.Value.Kind : (PieceTraitKind?)null) == trait)
                    {
                        return i;
                    }
                }
                run.Deck.PlayFromHand(nextSlotToPlay);
                nextSlotToPlay = (nextSlotToPlay + 1) % DeckManager.HandSize;
            }
            Assert.Fail("prepared token never reached the hand");
            return -1;
        }

        [Test]
        public void NoCombatTrait_TargetsOnlyTheFrontAliveEnemy()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner);
            int slot = HandWith(run, ShapeId.Single, PieceColor.Coral, null);

            var indices = DamageTargetPredictor.FindDamagedEnemyIndices(run, slot, 0, 0, out var kind);

            Assert.IsNull(kind);
            CollectionAssert.AreEqual(new[] { 0 }, indices);
        }

        [Test]
        public void RangeToken_TargetsTheBackEnemy_EvenOnItsFirstPlacement()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner);
            int slot = HandWith(run, ShapeId.Single, PieceColor.Joker, PieceTraitKind.Range);

            var indices = DamageTargetPredictor.FindDamagedEnemyIndices(run, slot, 0, 0, out var kind);

            Assert.AreEqual(PieceTraitKind.Range, kind);
            CollectionAssert.AreEqual(new[] { 2 }, indices);
        }

        [Test]
        public void PrecisionToken_TargetsTheWeakestAliveEnemy()
        {
            var run = RunWithEncounter(EnemyId.Poisoner, EnemyId.Basic, EnemyId.Locker);
            int slot = HandWith(run, ShapeId.Single, PieceColor.Joker, PieceTraitKind.Precision);

            var indices = DamageTargetPredictor.FindDamagedEnemyIndices(run, slot, 0, 0, out var kind);

            Assert.AreEqual(PieceTraitKind.Precision, kind);
            CollectionAssert.AreEqual(new[] { 1 }, indices, "Basic (250 HP) is the weakest");
        }

        [Test]
        public void BombeToken_ListsEveryAliveEnemy()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner);
            int slot = HandWith(run, ShapeId.Single, PieceColor.Joker, PieceTraitKind.Bombe);

            var indices = DamageTargetPredictor.FindDamagedEnemyIndices(run, slot, 0, 0, out var kind);

            Assert.AreEqual(PieceTraitKind.Bombe, kind);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
        }

        [Test]
        public void Prediction_MatchesTheEnemyThatActuallyTakesTheDamage_ForRange()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker);
            int slot = HandWith(run, ShapeId.Sq2, PieceColor.Joker, PieceTraitKind.Range);
            var predicted = DamageTargetPredictor.FindDamagedEnemyIndices(run, slot, 0, 0, out _);
            int hpBefore = run.CurrentEncounter[predicted[0]].CurrentHp;

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Less(run.CurrentEncounter[predicted[0]].CurrentHp, hpBefore, "the predicted target is the one that lost HP");
            Assert.AreEqual(run.CurrentEncounter[0].CurrentMaxHp, run.CurrentEncounter[0].CurrentHp, "the front enemy is untouched");
        }
    }
}
