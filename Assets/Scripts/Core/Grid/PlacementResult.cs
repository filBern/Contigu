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

        /// <summary>
        /// A genuine ADDITIVE "+Mult" pool (Balatro-style), distinct from
        /// <see cref="ModifierMultiplier"/>'s multiplicative "xN" family —
        /// spec extension, explicit request ("+1 mult, +2 mult et +4 mult",
        /// "+1 mult chaque modifier possédé", etc., phrased with a literal
        /// "+" rather than "x"). 0 when nothing contributed. <see
        /// cref="Mult"/> applies it as (1 + AdditiveMultBonus) — e.g. a
        /// single "+1 Mult" modifier makes Mult exactly double, matching
        /// how a lone "x2" ModifierMultiplier modifier would, but this pool
        /// adds instead of multiplying when more than one contributes (two
        /// "+1 Mult" modifiers together are (1+1+1)=x3, not x2*x2=x4).
        /// </summary>
        public int AdditiveMultBonus;

        /// <summary>
        /// TRUE fractional ADDITIVE "+Mult" contribution from Enchanted
        /// Cards/Experience (see RunManager.ApplyDeckStateModifierBonuses) —
        /// 0.1 per card/piece, computed as a genuine float instead of the
        /// integer-stepped approximation ((1+count)/10) previously used to
        /// avoid float in the pipeline (on explicit request: "Les upgrade
        /// progressive, on doit multiplier comme si c'était un float au
        /// lieu d'arrondir a la baisse. On arrondit le score total de la
        /// pièce posé par la suite"). Kept separate from <see
        /// cref="AdditiveMultBonus"/> (int) so that field stays exact for
        /// its existing readers (MultUn/Deux/Quatre, Solidarite,
        /// MultCinqRisque, all genuinely whole numbers) — this adds in on
        /// top when computing <see cref="Mult"/>. 0 when neither modifier
        /// is held.
        /// </summary>
        public float ProgressiveAdditiveMult;

        /// <summary>
        /// TRUE fractional multiplier from Density (Densité) — filled/10 as
        /// a genuine float (never floored mid-calculation), floored only at
        /// 1f so it can never be a debuff below its threshold (same
        /// explicit request as <see cref="ProgressiveAdditiveMult"/> above).
        /// Kept separate from <see cref="ModifierMultiplier"/> (int) so
        /// that field stays a clean whole-number "how many xN modifiers
        /// fired" count for its existing readers — this multiplies in
        /// separately when computing <see cref="Mult"/>. 1 when Densite
        /// isn't held or hasn't reached its threshold yet.
        /// </summary>
        public float ProgressiveMultiplier = 1f;

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
        /// Lueur earned from the player's active modifiers this placement —
        /// a second, independent source of Lueur alongside <see
        /// cref="LueurEarned"/> (spec extension, explicit request: "quelques
        /// modifiers qui rapportent des lueur"). Kept as its own field
        /// rather than folded into LueurEarned so that field's own
        /// contract ("always exactly the sum of LueurGroups") stays true —
        /// RunManager adds both together when crediting the run's Lueur
        /// total. See <see cref="ScoreEventType.LueurBonus"/> for the
        /// individual events behind this sum (one per qualifying
        /// modifier occurrence, tagged with which one via <see
        /// cref="ScoreEvent.TriggeringModifier"/>, same as <see
        /// cref="ModifierBonus"/>/<see cref="ModifierMultiplier"/>).
        /// </summary>
        public int ModifierLueurBonus;

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

        /// <summary>
        /// Balatro-style "mult" — the additive "+Mult" pool (see <see
        /// cref="AdditiveMultBonus"/>/<see cref="ProgressiveAdditiveMult"/>)
        /// applied as (1 + that pool), then every placement-wide "xN"
        /// multiplier stacked on top, including Densité's true fractional
        /// factor (<see cref="ProgressiveMultiplier"/>). A float rather
        /// than an int specifically so progressive modifiers keep their
        /// full precision all the way through the multiplication chain —
        /// only <see cref="TotalScore"/> rounds, once, at the very end (on
        /// explicit request: "on arrondit le score total de la pièce posé
        /// par la suite").
        /// </summary>
        public float Mult
        {
            get { return (1f + AdditiveMultBonus + ProgressiveAdditiveMult) * ModifierMultiplier * ComboMultiplier * ProgressiveMultiplier; }
        }

        /// <summary>Chips * Mult, rounded ONCE here — never anywhere upstream (see <see cref="Mult"/>).</summary>
        public int TotalScore
        {
            get { return Mathf.RoundToInt(Chips * Mult); }
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
