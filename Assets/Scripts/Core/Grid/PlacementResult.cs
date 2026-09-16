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

        /// <summary>
        /// Score from this placement's resulting connected same-color group
        /// (group size x per-cell value x tinted/multiplier-zone factor),
        /// rescored in full every time the group grows.
        /// </summary>
        public int GroupBonus;
        public int GoldenBonus;
        public int LineClearScore;

        /// <summary>Sum of every bonus from the player's active modifiers on this placement (see <see cref="ModifierId"/>).</summary>
        public int ModifierBonus;

        public IReadOnlyList<Vector2Int> ClearedCells = System.Array.Empty<Vector2Int>();

        /// <summary>Each cleared cell's color right before it was cleared, parallel to <see cref="ClearedCells"/> — lets the presentation layer keep showing a completed line as filled until it's ready to clear it visually.</summary>
        public IReadOnlyList<PieceColor> ClearedCellColors = System.Array.Empty<PieceColor>();

        public int LineClearCellCount;

        /// <summary>
        /// Every individual scoring contribution behind this placement's totals,
        /// in the order they occurred (golden bonuses, then group cells, then one
        /// entry per cleared cell) — lets the presentation layer show each point
        /// addition on its own instead of a single lump total.
        /// </summary>
        public IReadOnlyList<ScoreEvent> ScoreEvents = System.Array.Empty<ScoreEvent>();

        public int TotalScore
        {
            get { return GroupBonus + GoldenBonus + LineClearScore + ModifierBonus; }
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
