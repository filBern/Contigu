using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public sealed class EnemyEffectCellDiffTests
    {
        private static Vector2Int P(int x, int y) { return new Vector2Int(x, y); }

        [Test]
        public void UnchangedCells_ProduceNoChange()
        {
            var diff = EnemyEffectCellDiff.Compute(new[] { P(1, 1) }, new[] { P(2, 2) }, new[] { P(1, 1) }, new[] { P(2, 2) }, new Vector2Int[0]);
            Assert.IsFalse(diff.HasChange);
        }

        [Test]
        public void MovedLock_IsOneNewCellAndOneReleasedCell()
        {
            var diff = EnemyEffectCellDiff.Compute(new[] { P(1, 1) }, new Vector2Int[0], new[] { P(3, 3) }, new Vector2Int[0], new Vector2Int[0]);
            CollectionAssert.AreEquivalent(new[] { P(3, 3) }, diff.NewMalusCells);
            CollectionAssert.AreEquivalent(new[] { P(1, 1) }, diff.ReleasedLockedCells);
            Assert.IsEmpty(diff.ReleasedPoisonedCells);
            Assert.IsTrue(diff.HasChange);
        }

        [Test]
        public void ReRolledBlight_SeparatesNewAndReleasedBlightedCells()
        {
            var diff = EnemyEffectCellDiff.Compute(new Vector2Int[0], new[] { P(0, 0), P(5, 5) }, new Vector2Int[0], new[] { P(5, 5), P(6, 6) }, new Vector2Int[0]);
            CollectionAssert.AreEquivalent(new[] { P(6, 6) }, diff.NewMalusCells);
            CollectionAssert.AreEquivalent(new[] { P(0, 0) }, diff.ReleasedPoisonedCells);
        }

        [Test]
        public void BossLockedCells_AreAlwaysReportedAsNew()
        {
            var diff = EnemyEffectCellDiff.Compute(new Vector2Int[0], new Vector2Int[0], new Vector2Int[0], new Vector2Int[0], new[] { P(4, 4) });
            CollectionAssert.AreEqual(new[] { P(4, 4) }, diff.NewMalusCells);
            Assert.IsTrue(diff.HasChange);
        }
    }
}
