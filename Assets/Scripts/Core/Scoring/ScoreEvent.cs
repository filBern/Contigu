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
        Trait,

        /// <summary>Lueur (not score) earned from one of the player's active modifiers — see <see cref="PlacementResult.ModifierLueurBonus"/>. Amount is Lueur, not points; flies to the Lueur label like <see cref="PlacementResult.LueurGroups"/> instead of adding to the score cascade.</summary>
        LueurBonus,

        /// <summary>A genuine ADDITIVE "+Mult" contribution (Balatro-style), as opposed to <see cref="ModifierMultiplier"/>'s multiplicative "xN" — see <see cref="PlacementResult.AdditiveMultBonus"/>. Amount is how much this modifier ADDS to that pool (1 for a "+1 Mult" modifier), not a point value and not a factor to multiply by.</summary>
        MultBonus,

        /// <summary>A flat per-placement bonus from the "Piece Mastery" upgrade (see <see cref="PlacementResult.ShapeMasteryBonus"/>, <see cref="RunManager.GetShapeMasteryLevel"/>) — not tied to any <see cref="ModifierId"/> or <see cref="PieceTrait"/>, unlike <see cref="Modifier"/>/<see cref="Trait"/>: a persistent per-exact-shape level the player bought from the shop, independent of both systems.</summary>
        ShapeMastery,

        /// <summary>Piece Mastery's exact sibling, from the "Color Mastery" upgrade — see <see cref="PlacementResult.ColorMasteryBonus"/>, <see cref="RunManager.GetColorMasteryLevel"/>.</summary>
        ColorMastery
    }

    /// <summary>
    /// A single, atomic point-scoring event tied to one grid cell — the building
    /// block behind a placement's aggregate score, so the presentation layer can
    /// show each contribution individually instead of one lump total.
    /// </summary>
    public sealed class ScoreEvent
    {
        public readonly ScoreEventType Type;
        public readonly Vector2Int Position;

        /// <summary>
        /// Not readonly (unlike Type/Position) — GridManager.TagNewEvents
        /// rescales this in place for a leveled modifier's newly-tagged
        /// events (see ModifierLevelUtility), the same way it already
        /// mutates TriggeringModifier/TriggeringModifierIndex post-
        /// construction below, rather than every one of the ~50 individual
        /// Apply* methods needing to know its own modifier's level.
        /// </summary>
        public int Amount;

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

        /// <summary>
        /// <see cref="TriggeringModifier"/>'s position within the
        /// activeModifiers list that produced this event (GridManager's own
        /// loop index when it dispatched to that modifier's Apply* method —
        /// see GridManager.TagNewEvents) — 0 when <see cref="TriggeringModifier"/>
        /// is null. Distinct copies of the same modifier id can coexist
        /// (Copieur/"Mimic" duplicates an id rather than being its own), so
        /// the id alone can't tell two of them apart; this lets the
        /// presentation layer (ModifierPanelView.GetBadgeTransform) anchor
        /// this event's popup on the specific badge that actually produced
        /// it instead of always the first one showing that id.
        /// </summary>
        public int TriggeringModifierIndex;

        /// <summary>
        /// The true, unrounded contribution behind <see cref="Amount"/> — only
        /// set for a <see cref="ScoreEventType.MultBonus"/> event from a
        /// genuinely fractional modifier (Enchanted Cards/Experience, see
        /// RunManager.ApplyDeckStateModifierBonuses), null everywhere else.
        /// <see cref="Amount"/> stays a rounded int (still used for chip/usage
        /// bookkeeping that expects a whole number); this lets the popup show
        /// the precise value instead.
        /// </summary>
        public float? PreciseAmount;

        /// <summary>
        /// A second grid position this event's own eligibility actually
        /// depended on, beyond <see cref="Position"/> itself — null for
        /// every event type that only ever reads its own cell. Set by
        /// GridManager.ApplyContraste to the contrasting neighbor cell that
        /// triggered it: RunManager.ApplyPoisonScoreRule also negates this
        /// event when this position is poisoned, not just when <see
        /// cref="Position"/> is.
        /// </summary>
        public Vector2Int? ReferencedPosition;

        public ScoreEvent(ScoreEventType type, Vector2Int position, int amount)
        {
            Type = type;
            Position = position;
            Amount = amount;
        }
    }
}
