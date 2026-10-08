using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Everything that happened as a result of placing one piece: enough detail
    /// for the presentation layer to show floating "+X" feedback.
    /// </summary>
    public sealed class PlacementResult
    {
        public bool Success;
        public string FailureReason;

        public IReadOnlyList<Vector2Int> PlacedCells = System.Array.Empty<Vector2Int>();

        /// <summary>
        /// Every cell in this placement's resulting connected same-color
        /// group (see GridManager.FindConnectedGroup) — a superset of <see
        /// cref="PlacedCells"/> once this placement merges into a
        /// pre-existing group. Used by RunManager to find any Joker-exclusive
        /// combat trait (see PieceTrait.JokerCombatKinds) stamped on an
        /// older cell that this placement's merge just pulled back into a
        /// scored group, so its combat effect retriggers too.
        /// </summary>
        public IReadOnlyList<Vector2Int> GroupCells = System.Array.Empty<Vector2Int>();

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
        /// applied once to the sum of <see cref="GroupBonus"/> + <see
        /// cref="GoldenBonus"/> in <see cref="TotalScore"/>. 1 when nothing
        /// in this placement's group carried either flag. Never applies to
        /// ModifierBonus/TraitBonus, which stay independent additive amounts.
        /// </summary>
        public int GroupMultiplier = 1;

        /// <summary>
        /// Aggregate multiplier from this placement's multiplier-zone cells
        /// only (see GridManager.ComputeLineClearMultiplier) — applied to
        /// <see cref="LineClearScore"/> alone in <see cref="TotalScore"/>.
        /// Deliberately excludes Tinted cells, unlike <see
        /// cref="GroupMultiplier"/>, keeping Tinted a narrower effect. 1
        /// when nothing in this placement's group is a multiplier zone.
        /// </summary>
        public int LineClearMultiplier = 1;

        /// <summary>
        /// Multiplies this placement's whole total score (see <see
        /// cref="TotalScore"/>) — the "Combo" modifier's doing. 1 when Combo
        /// isn't held or didn't fire; stacks (x4, x8, ...) if held more than
        /// once, same convention as <see cref="GroupMultiplier"/>.
        /// </summary>
        public int ComboMultiplier = 1;

        /// <summary>
        /// Multiplies this placement's whole total score (see <see
        /// cref="TotalScore"/>), same tier as <see cref="ComboMultiplier"/>
        /// — the aggregate of every "xN"-style modifier currently held. 1
        /// when none of these fired this placement; stacks multiplicatively
        /// with itself (several firing at once, or a "per line"/"per bridge"
        /// one firing more than once), same as every other multiplier field
        /// here.
        /// </summary>
        public int ModifierMultiplier = 1;

        /// <summary>
        /// A genuine additive "+Mult" pool, distinct from <see
        /// cref="ModifierMultiplier"/>'s multiplicative "xN" family. 0 when
        /// nothing contributed. <see cref="Mult"/> applies it as (1 +
        /// AdditiveMultBonus) — e.g. a single "+1 Mult" modifier makes Mult
        /// exactly double, but this pool adds instead of multiplying when
        /// more than one contributes (two "+1 Mult" modifiers together are
        /// (1+1+1)=x3, not x2*x2=x4).
        /// </summary>
        public int AdditiveMultBonus;

        /// <summary>
        /// True fractional additive "+Mult" contribution from Density
        /// (Densité), Enchanted Cards, and Experience (see GridManager.
        /// ApplyDensite/RunManager.ApplyDeckStateModifierBonuses) — computed
        /// as a genuine float, kept separate from <see cref="AdditiveMultBonus"/>
        /// (int) so that field stays exact for its other, whole-number-only
        /// contributors. Added in on top when computing <see cref="Mult"/>.
        /// 0 when none of the three is held.
        /// </summary>
        public float ProgressiveAdditiveMult;

        /// <summary>Sum of every bonus from the player's active modifiers on this placement that's still a flat/per-cell bonus rather than a multiplier (see <see cref="ModifierId"/>/<see cref="ModifierMultiplier"/>).</summary>
        public int ModifierBonus;

        /// <summary>Sum of every bonus produced directly by the placed piece's own <see cref="PieceTrait"/> (e.g. Mirror Tile's duplicated group bonus) rather than by a Cell flag — see <see cref="ScoreEventType.Trait"/>. Populated by RunManager, not GridManager, since GridManager knows nothing about PieceTrait.</summary>
        public int TraitBonus;

        /// <summary>Sum of every cell's <see cref="Cell.ShapeMasteryBonus"/> stamp scored this placement (see <see cref="ScoreEventType.ShapeMastery"/>) — unlike <see cref="TraitBonus"/>, this is populated directly by GridManager from the cells' own persistent stamps (RunManager.StampMasteryBonuses sets those stamps before GridManager.PlacePiece runs, so a later placement that merely regrows an already-leveled group rescores it too, not just the piece that was leveled at purchase time).</summary>
        public int ShapeMasteryBonus;

        /// <summary>Piece Mastery's exact sibling, summed from every cell's <see cref="Cell.ColorMasteryBonus"/> stamp (see <see cref="ScoreEventType.ColorMastery"/>) — same GridManager-populated, persistent-stamp pattern as <see cref="ShapeMasteryBonus"/>.</summary>
        public int ColorMasteryBonus;

        public IReadOnlyList<Vector2Int> ClearedCells = System.Array.Empty<Vector2Int>();

        /// <summary>Each cleared cell's color right before it was cleared, parallel to <see cref="ClearedCells"/> — lets the presentation layer keep showing a completed line as filled until it's ready to clear it visually.</summary>
        public IReadOnlyList<PieceColor> ClearedCellColors = System.Array.Empty<PieceColor>();

        /// <summary>Each cleared cell's <see cref="Cell.FilledShapeId"/> right before it was cleared, parallel to <see cref="ClearedCells"/> — Color Hater's own sibling (see RunManager.ApplyShapeHaterScoreRule) needs this the same way <see cref="ClearedCellColors"/> feeds ApplyCursedColorScoreRule. Nullable, unlike ClearedCellColors — a cell filled directly rather than through GridManager.PlacePiece never had a shape stamped on it at all.</summary>
        public IReadOnlyList<ShapeId?> ClearedCellShapes = System.Array.Empty<ShapeId?>();

        /// <summary>Each cleared cell's <see cref="Cell.OriginTrait"/> right before it was cleared (null where there wasn't one), parallel to <see cref="ClearedCells"/> — same held-until-clear purpose as <see cref="ClearedCellColors"/>, so a tile's trait badge disappears in step with the tile itself instead of at the start of the score cascade.</summary>
        public IReadOnlyList<PieceTrait?> ClearedCellTraits = System.Array.Empty<PieceTrait?>();

        public int LineClearCellCount;

        /// <summary>
        /// How many rows/columns this placement completed and cleared —
        /// unlike <see cref="LineClearCellCount"/> (total cells emptied,
        /// which double-counts a cell shared by a completed row and
        /// column), this is the straight count of lines themselves. Used
        /// by RunManager to heal the "Leech" enemy.
        /// </summary>
        public int ClearedLineCount;

        /// <summary>
        /// Cell(s) destroyed by a trait effect (Void Tile's random clear,
        /// Kamikaze Tile's surrounding-tile wipe) rather than by completing
        /// a line — distinct from <see cref="ClearedCells"/>, which
        /// GridManager.CheckAndClearLines alone populates. Populated by
        /// RunManager (see ApplyVoidEffect/ApplyKamikazeEffect), same
        /// Core-doesn't-know-about-PieceTrait split as <see
        /// cref="TraitBonus"/>.
        /// </summary>
        public IReadOnlyList<Vector2Int> DestroyedCells = System.Array.Empty<Vector2Int>();

        /// <summary>Each destroyed cell's color right before it was destroyed, parallel to <see cref="DestroyedCells"/> — nullable only defensively (a destroyed cell was necessarily filled, so this should never actually be null in practice), matching <see cref="Cell.FilledColor"/>'s own type.</summary>
        public IReadOnlyList<PieceColor?> DestroyedCellColors = System.Array.Empty<PieceColor?>();

        /// <summary>Each destroyed cell's <see cref="Cell.FilledShapeId"/> right before it was destroyed, parallel to <see cref="DestroyedCells"/> — <see cref="ClearedCellShapes"/>'s own sibling for the destroy path, same nullable-only-defensively contract as <see cref="DestroyedCellColors"/>.</summary>
        public IReadOnlyList<ShapeId?> DestroyedCellShapes = System.Array.Empty<ShapeId?>();

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
        /// cref="LueurEarned"/>. Kept as its own field rather than folded
        /// into LueurEarned so that field's own contract ("always exactly
        /// the sum of LueurGroups") stays true — RunManager adds both
        /// together when crediting the run's Lueur total.
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
        /// Every additive scoring source folded together, including the
        /// per-cell <see cref="GroupMultiplier"/>/<see cref="LineClearMultiplier"/>
        /// (Tinted/Multiplier Zone cells) but not the placement-wide <see
        /// cref="ModifierMultiplier"/>/<see cref="ComboMultiplier"/> (see
        /// <see cref="Mult"/> for those). <see cref="TotalScore"/> is always
        /// exactly Chips * <see cref="Mult"/>.
        /// </summary>
        public int Chips
        {
            get { return (GroupBonus + GoldenBonus) * GroupMultiplier + LineClearScore * LineClearMultiplier + ModifierBonus + TraitBonus + ShapeMasteryBonus + ColorMasteryBonus; }
        }

        /// <summary>
        /// Starts at 1 and folds every Mult-contributing <see
        /// cref="ScoreEvents"/> entry (<see cref="ScoreEventType.ModifierMultiplier"/>/<see
        /// cref="ScoreEventType.MultBonus"/>) strictly left to right, in the
        /// order the player holds those modifiers (<see
        /// cref="ScoreEvent.TriggeringModifierIndex"/>), not by operator
        /// precedence: a "+N Mult" event adds N to the running total, an
        /// "xN" event multiplies it. This is why the fold reads off
        /// <see cref="ScoreEvents"/> rather than the separately-summed
        /// <see cref="AdditiveMultBonus"/>/<see cref="ModifierMultiplier"/>/<see
        /// cref="ComboMultiplier"/>/<see cref="ProgressiveAdditiveMult"/>
        /// fields (still populated for other readers/tests but order-
        /// independent, since summing then multiplying can't honor purchase
        /// order). <see cref="ScoreEvent.PreciseAmount"/> is used over the
        /// rounded <see cref="ScoreEvent.Amount"/> wherever a modifier set
        /// it, so progressive modifiers keep full precision through the
        /// fold — only <see cref="TotalScore"/> rounds, once, at the end.
        /// </summary>
        public float Mult
        {
            get
            {
                var multEvents = new List<ScoreEvent>();
                for (int i = 0; i < ScoreEvents.Count; i++)
                {
                    var e = ScoreEvents[i];
                    if (e.Type == ScoreEventType.ModifierMultiplier || e.Type == ScoreEventType.MultBonus)
                    {
                        multEvents.Add(e);
                    }
                }
                // Two events sharing the same index are always the same
                // modifier's own multiple firings, so an unstable sort is safe.
                multEvents.Sort((a, b) => a.TriggeringModifierIndex.CompareTo(b.TriggeringModifierIndex));

                float mult = 1f;
                for (int i = 0; i < multEvents.Count; i++)
                {
                    var e = multEvents[i];
                    float amount = e.PreciseAmount ?? e.Amount;
                    if (e.Type == ScoreEventType.ModifierMultiplier)
                    {
                        mult *= amount;
                    }
                    else
                    {
                        mult += amount;
                    }
                }
                return mult;
            }
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
