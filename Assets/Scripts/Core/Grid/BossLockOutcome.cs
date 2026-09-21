using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Result of one boss-round lock tick (see
    /// <see cref="GridManager.LockFreeCellsAndCheckClears"/>) — which cells
    /// just got locked, and any score the lock itself produced by completing
    /// a row/column that had nothing left to fill.
    /// </summary>
    public sealed class BossLockOutcome
    {
        public IReadOnlyList<Vector2Int> LockedCells = System.Array.Empty<Vector2Int>();

        public IReadOnlyList<Vector2Int> ClearedCells = System.Array.Empty<Vector2Int>();

        /// <summary>Parallel to <see cref="ClearedCells"/>, same convention as <see cref="PlacementResult.ClearedCellColors"/>.</summary>
        public IReadOnlyList<PieceColor> ClearedCellColors = System.Array.Empty<PieceColor>();

        public int LineClearScore;

        /// <summary>Lueur earned if the lock itself completed a line — same rule as PlacementResult.LueurEarned.</summary>
        public int LueurEarned;

        public IReadOnlyList<ScoreEvent> ScoreEvents = System.Array.Empty<ScoreEvent>();
    }
}
