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

        /// <summary>A Bastion-tile cell whose row/column completed — scores the line-clear bonus like <see cref="LineClear"/>, but the cell itself stays filled/locked instead of being emptied (see Cell.IsBastion).</summary>
        Bastion,

        /// <summary>A flat/per-cell bonus from one of the player's active modifiers (see <see cref="ModifierId"/>).</summary>
        Modifier,

        /// <summary>An "xN" placement-wide multiplier contribution from one of the player's active modifiers (see <see cref="ModifierId"/>/<see cref="PlacementResult.ModifierMultiplier"/>) — <see cref="ScoreEvent.Amount"/> is the factor itself (2 for x2, 3 for x3), not a point value.</summary>
        ModifierMultiplier,

        /// <summary>A bonus produced directly by a placed piece's own <see cref="PieceTrait"/> (e.g. Mirror Tile's duplicated group bonus) rather than by a Cell flag the group-scoring loop picks up on its own.</summary>
        Trait
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

        /// <summary>
        /// Which active modifier produced this event, when <see cref="Type"/> is
        /// <see cref="ScoreEventType.Modifier"/>. Left null otherwise. Not set by
        /// the constructor — the individual Apply* methods in
        /// <see cref="GridManager"/> don't know their own id, so the dispatcher
        /// (<see cref="GridManager.ApplyPreClearModifiers"/>/
        /// <see cref="GridManager.ApplyPostClearModifiers"/>) tags newly added
        /// events with it right after each call, letting the presentation layer
        /// highlight the specific modifier that just scored.
        /// </summary>
        public ModifierId? TriggeringModifier;

        public ScoreEvent(ScoreEventType type, Vector2Int position, int amount)
        {
            Type = type;
            Position = position;
            Amount = amount;
        }
    }
}
