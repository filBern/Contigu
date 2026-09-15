using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Everything that happened as a result of placing one piece: enough detail
    /// for the presentation layer to show floating "+X" feedback per spec 9.7.
    /// </summary>
    public sealed class PlacementResult
    {
        public bool Success;
        public string FailureReason;

        public IReadOnlyList<Vector2Int> PlacedCells = System.Array.Empty<Vector2Int>();

        public int NeighborBonus;
        public int GoldenBonus;
        public int LineClearScore;

        public IReadOnlyList<Vector2Int> ClearedCells = System.Array.Empty<Vector2Int>();
        public int LineClearCellCount;

        public int TotalScore
        {
            get { return NeighborBonus + GoldenBonus + LineClearScore; }
        }

        public static PlacementResult Failure(string reason)
        {
            var result = new PlacementResult();
            result.Success = false;
            result.FailureReason = reason;
            return result;
        }
    }
}
