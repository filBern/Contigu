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
        /// (group size x per-cell value), UNMULTIPLIED — rescored in full
        /// every time the group grows. Any tinted/multiplier-zone factor no
        /// longer inflates this per-cell (see <see cref="GroupMultiplier"/>).
        /// </summary>
        public int GroupBonus;
        public int GoldenBonus;
        public int LineClearScore;

        /// <summary>
        /// Aggregate multiplier from this placement's tinted-match/
        /// multiplier-zone cells (see GridManager.ComputeGroupMultiplier),
        /// applied ONCE to the sum of <see cref="GroupBonus"/> + <see
        /// cref="GoldenBonus"/> in <see cref="TotalScore"/> — Balatro-style
        /// "apply the multiplier at the end" (explicit request), instead of
        /// being baked per-cell into GroupBonus alone like before. 1 when
        /// nothing in this placement's group carried either flag. Never
        /// applies to ModifierBonus/TraitBonus, which stay fully
        /// independent additive amounts.
        /// </summary>
        public int GroupMultiplier = 1;

        /// <summary>
        /// Aggregate multiplier from this placement's multiplier-zone cells
        /// ONLY (see GridManager.ComputeLineClearMultiplier) — applied to
        /// <see cref="LineClearScore"/> alone in <see cref="TotalScore"/>.
        /// Deliberately excludes Tinted cells, unlike <see
        /// cref="GroupMultiplier"/>: Tinted Tile and Multiplier Zone were
        /// functionally identical once Tinted's color always matched its
        /// own piece (see DeckManager.TagTintedTokensRandom), which made the
        /// cheaper Common-rarity Tinted strictly redundant with the
        /// Uncommon-rarity Multiplier Zone (explicit player feedback: "A ce
        /// moment elle a le même effet que MultiplierZone, il faudrait
        /// trouver une manière de les différencier"). Splitting the
        /// line-clear bonus out keeps Tinted a real, always-firing, but
        /// narrower effect. 1 when nothing in this placement's group is a
        /// multiplier zone.
        /// </summary>
        public int LineClearMultiplier = 1;

        /// <summary>
        /// Multiplies this placement's WHOLE total score (see <see
        /// cref="TotalScore"/>) — the "Combo" modifier's doing (on explicit
        /// request: "x2 sur le score TOTAL de la pose"). 1 when Combo isn't
        /// held or didn't fire; stacks (x4, x8, ...) if held more than once,
        /// same convention as <see cref="GroupMultiplier"/>. Combo used to be
        /// the only true placement-wide multiplier modifier — see <see
        /// cref="ModifierMultiplier"/>, which now covers many more.
        /// </summary>
        public int ComboMultiplier = 1;

        /// <summary>
        /// Multiplies this placement's WHOLE total score (see <see
        /// cref="TotalScore"/>), same tier as <see cref="ComboMultiplier"/>
        /// — the aggregate of every "xN"-style modifier now held (Prisme,
        /// Architecte, Puriste, Tricolore, Complémentaire, Îlot, Maçon,
        /// Démolisseur, Dégradé, Solitaire, Espace Libre, Rafale, Pont,
        /// Grosse Famille, Repetition, Alternance des pièces, Minimaliste,
        /// and the 6 line-pattern modifiers), converted from a flat +pts
        /// bonus to a real multiplier on explicit request ("j'aimerais qu'on
        /// utilise plus de multiplicateur dans les modifiers"). 1 when none
        /// of them fired this placement; stacks multiplicatively with itself
        /// (several firing at once, or a "per line"/"per bridge" one firing
        /// more than once) same as every other multiplier field here.
        /// </summary>
        public int ModifierMultiplier = 1;

        /// <summary>Sum of every bonus from the player's active modifiers on this placement that's still a flat/per-cell bonus rather than a multiplier (see <see cref="ModifierId"/>/<see cref="ModifierMultiplier"/>).</summary>
        public int ModifierBonus;

        /// <summary>Sum of every bonus produced directly by the placed piece's own <see cref="PieceTrait"/> (e.g. Mirror Tile's duplicated group bonus) rather than by a Cell flag — see <see cref="ScoreEventType.Trait"/>. Populated by RunManager, not GridManager, since GridManager knows nothing about PieceTrait.</summary>
        public int TraitBonus;

        public IReadOnlyList<Vector2Int> ClearedCells = System.Array.Empty<Vector2Int>();

        /// <summary>Each cleared cell's color right before it was cleared, parallel to <see cref="ClearedCells"/> — lets the presentation layer keep showing a completed line as filled until it's ready to clear it visually.</summary>
        public IReadOnlyList<PieceColor> ClearedCellColors = System.Array.Empty<PieceColor>();

        /// <summary>Each cleared cell's <see cref="Cell.OriginTrait"/> right before it was cleared (null where there wasn't one), parallel to <see cref="ClearedCells"/> — same held-until-clear purpose as <see cref="ClearedCellColors"/>, so a tile's trait badge disappears in step with the tile itself instead of at the start of the score cascade.</summary>
        public IReadOnlyList<PieceTrait?> ClearedCellTraits = System.Array.Empty<PieceTrait?>();

        public int LineClearCellCount;

        /// <summary>
        /// "Lueur" currency earned by this placement's own line clears —
        /// completely independent of <see cref="TotalScore"/>: it's driven
        /// by color GROUPING within each cleared line (see <see
        /// cref="LueurGroups"/>) rather than by how many points the
        /// placement scored, so a mixed-color clear can out-earn a huge
        /// monochrome one. Never multiplied by ComboMultiplier or anything
        /// else — always exactly the sum of <see cref="LueurGroups"/>.
        /// </summary>
        public int LueurEarned;

        /// <summary>
        /// Every individual Lueur-earning group behind <see
        /// cref="LueurEarned"/> — one entry per DISTINCT color within a
        /// cleared line (see GridManager.ComputeLueurGroups) — so the
        /// presentation layer can pulse/animate each color's own Lueur on
        /// its own instead of only ever adding the placement's total in one
        /// lump sum.
        /// </summary>
        public IReadOnlyList<LueurGroup> LueurGroups = System.Array.Empty<LueurGroup>();

        /// <summary>
        /// Every individual scoring contribution behind this placement's totals,
        /// in the order they occurred (golden bonuses, then group cells, then one
        /// entry per cleared cell) — lets the presentation layer show each point
        /// addition on its own instead of a single lump total.
        /// </summary>
        public IReadOnlyList<ScoreEvent> ScoreEvents = System.Array.Empty<ScoreEvent>();

        /// <summary>
        /// Balatro-style "chips" — every additive scoring source folded
        /// together, including the per-cell <see cref="GroupMultiplier"/>/
        /// <see cref="LineClearMultiplier"/> (Tinted/Multiplier Zone cells)
        /// but NOT the placement-wide <see cref="ModifierMultiplier"/>/<see
        /// cref="ComboMultiplier"/> (see <see cref="Mult"/> for those).
        /// <see cref="TotalScore"/> is always exactly Chips * <see
        /// cref="Mult"/>.
        /// </summary>
        public int Chips
        {
            get { return (GroupBonus + GoldenBonus) * GroupMultiplier + LineClearScore * LineClearMultiplier + ModifierBonus + TraitBonus; }
        }

        /// <summary>Balatro-style "mult" — every placement-wide multiplier stacked together. <see cref="TotalScore"/> is always exactly <see cref="Chips"/> * Mult.</summary>
        public int Mult
        {
            get { return ModifierMultiplier * ComboMultiplier; }
        }

        public int TotalScore
        {
            get { return Chips * Mult; }
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
