using UnityEngine;

namespace Contigu.Core
{
    /// <summary>What kind of scoring rule produced a <see cref="ScoreEvent"/>.</summary>
    public enum ScoreEventType
    {
        /// <summary>
        /// One cell of a placement's resulting connected same-color group,
        /// already including any tinted/multiplier-zone group factor. The whole
        /// group is rescored on every placement that grows it, so this can fire
        /// for cells placed in an earlier turn too.
        /// </summary>
        Group,

        /// <summary>A golden cell filled — a flat bonus, independent of group size or color.</summary>
        Golden,

        /// <summary>One cell cleared by a completed line/column.</summary>
        LineClear,

        /// <summary>A bonus from one of the player's active modifiers (see <see cref="ModifierId"/>).</summary>
        Modifier
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
