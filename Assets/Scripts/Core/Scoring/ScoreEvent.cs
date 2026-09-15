using UnityEngine;

namespace Contigu.Core
{
    /// <summary>What kind of scoring rule produced a <see cref="ScoreEvent"/>.</summary>
    public enum ScoreEventType
    {
        /// <summary>One matching-color neighbor pair (spec 3.2), already including any tinted/multiplier-zone factor.</summary>
        Neighbor,

        /// <summary>A golden cell filled (spec 3.2).</summary>
        Golden,

        /// <summary>One cell cleared by a completed line/column (spec 3.3).</summary>
        LineClear
    }

    /// <summary>
    /// A single, atomic point-scoring event tied to one grid cell — the building
    /// block behind a placement's aggregate score, so the presentation layer can
    /// show each contribution individually instead of one lump total (spec 9.7's
    /// "+X" feedback, made granular).
    /// </summary>
    public sealed class ScoreEvent
    {
        public readonly ScoreEventType Type;
        public readonly Vector2Int Position;
        public readonly int Amount;

        public ScoreEvent(ScoreEventType type, Vector2Int position, int amount)
        {
            Type = type;
            Position = position;
            Amount = amount;
        }
    }
}
