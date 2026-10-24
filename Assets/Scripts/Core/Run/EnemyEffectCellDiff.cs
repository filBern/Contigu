using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Which grid cells an enemy's lock/blight effect gained or released across one placement (the placement's auto-refill can move
    /// a Locker's lock or re-roll a Poisoner's blight before any animation starts). The presentation layer keeps GAINED cells hidden
    /// and RELEASED cells still looking locked/blighted until its reveal step.
    /// </summary>
    public sealed class EnemyEffectCellDiff
    {
        /// <summary>Cells newly locked or blighted (plus any boss-locked cells), in the order they were found.</summary>
        public readonly List<Vector2Int> NewMalusCells;
        public readonly List<Vector2Int> ReleasedLockedCells;
        public readonly List<Vector2Int> ReleasedPoisonedCells;

        public bool HasChange
        {
            get { return NewMalusCells.Count > 0 || ReleasedLockedCells.Count > 0 || ReleasedPoisonedCells.Count > 0; }
        }

        private EnemyEffectCellDiff(List<Vector2Int> added, List<Vector2Int> releasedLocked, List<Vector2Int> releasedPoisoned)
        {
            NewMalusCells = added;
            ReleasedLockedCells = releasedLocked;
            ReleasedPoisonedCells = releasedPoisoned;
        }

        /// <summary>The symmetric difference of the before/after snapshots (not the union): only cells whose status actually changed.</summary>
        public static EnemyEffectCellDiff Compute(
            IEnumerable<Vector2Int> lockedBefore, IEnumerable<Vector2Int> poisonedBefore,
            IEnumerable<Vector2Int> lockedAfter, IEnumerable<Vector2Int> poisonedAfter,
            IEnumerable<Vector2Int> bossLockedCells)
        {
            var lockedBeforeSet = new HashSet<Vector2Int>(lockedBefore);
            var lockedAfterSet = new HashSet<Vector2Int>(lockedAfter);
            var poisonedBeforeSet = new HashSet<Vector2Int>(poisonedBefore);
            var poisonedAfterSet = new HashSet<Vector2Int>(poisonedAfter);

            var added = new List<Vector2Int>(bossLockedCells);
            foreach (var pos in lockedAfterSet)
            {
                if (!lockedBeforeSet.Contains(pos))
                {
                    added.Add(pos);
                }
            }
            foreach (var pos in poisonedAfterSet)
            {
                if (!poisonedBeforeSet.Contains(pos))
                {
                    added.Add(pos);
                }
            }

            var releasedLocked = new List<Vector2Int>();
            foreach (var pos in lockedBeforeSet)
            {
                if (!lockedAfterSet.Contains(pos))
                {
                    releasedLocked.Add(pos);
                }
            }
            var releasedPoisoned = new List<Vector2Int>();
            foreach (var pos in poisonedBeforeSet)
            {
                if (!poisonedAfterSet.Contains(pos))
                {
                    releasedPoisoned.Add(pos);
                }
            }
            return new EnemyEffectCellDiff(added, releasedLocked, releasedPoisoned);
        }
    }
}
