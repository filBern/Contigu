using Contigu.Core;
using NUnit.Framework;

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

        [Test]
        public void PlacementDamage_TargetsOnlyTheFrontAliveEnemy()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner);
            CollectionAssert.AreEqual(new[] { 0 }, DamageTargetPredictor.FindDamagedEnemyIndices(run));
        }

        [Test]
        public void ADeadFrontEnemy_IsSkipped()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner);
            run.CurrentEncounter[0].ApplyDamage(run.CurrentEncounter[0].CurrentHp);
            CollectionAssert.AreEqual(new[] { 1 }, DamageTargetPredictor.FindDamagedEnemyIndices(run));
        }

        [Test]
        public void Prediction_MatchesTheEnemyThatActuallyTakesTheDamage()
        {
            var run = RunWithEncounter(EnemyId.Basic, EnemyId.Locker);
            var predicted = DamageTargetPredictor.FindDamagedEnemyIndices(run);
            int hpBefore = run.CurrentEncounter[predicted[0]].CurrentHp;
            int slot = 0;
            for (int s = 0; s < run.Deck.Hand.Count; s++) { if (run.Deck.Hand[s].HasValue) { slot = s; break; } }
            var shape = PieceShapeCatalog.GetRotated(run.Deck.Hand[slot].Value.Shape, run.Deck.HandRotations[slot]);
            Vector2IntFinder.Place(run, slot, shape);
            Assert.Less(run.CurrentEncounter[predicted[0]].CurrentHp, hpBefore, "the predicted target is the one that lost HP");
        }
    }

    internal static class Vector2IntFinder
    {
        public static void Place(RunManager run, int slot, PieceShape shape)
        {
            foreach (var pos in GridManager.AllPositions())
            {
                if (run.Grid.CanPlace(shape, pos.x, pos.y))
                {
                    var outcome = run.PlacePiece(slot, pos.x, pos.y);
                    Assert.IsTrue(outcome.Placement.Success);
                    return;
                }
            }
            Assert.Fail("no placement found");
        }
    }
}
