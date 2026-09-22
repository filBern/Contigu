using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Owns the 8x8 grid state: placement validation, the connected-color-group
    /// bonus, line/column clears, and the persistent cell modifiers (golden,
    /// tinted, multiplier zone) plus the boss round's locked cells. Pure C#, no
    /// MonoBehaviour dependency, so it is unit-testable in isolation.
    /// </summary>
    public sealed class GridManager
    {
        public const int Size = 8;

        private readonly Cell[,] _cells;

        /// <summary>Scored group size of the last placement made this round, or null before the round's first placement — tracked for "Momentum" (Dégradé), reset by <see cref="ResetForNewRound"/>.</summary>
        private int? _lastGroupSize;

        /// <summary>Consecutive placements made this round without a line/column clear, as of BEFORE the placement currently in progress — see <see cref="PlacementsSinceLastClear"/>.</summary>
        private int _placementsSinceLastClear;

        /// <summary>Shape of the last piece placed this round, or null before the round's first placement — tracked for "Repetition", reset by <see cref="ResetForNewRound"/>.</summary>
        private ShapeId? _lastPlacedShapeId;

        /// <summary>How many placements in a row this round (ending at, and including, the most recent one) share the same shape — 1 means no repeat yet (either the round's first placement, or this shape differs from the previous one). Tracked for the progressive "Repetition" modifier, reset by <see cref="ResetForNewRound"/>.</summary>
        private int _repetitionStreak;

        /// <summary>Fill color of the last piece placed this round, or null before the round's first placement — tracked for "Color Switch" (Alternance des pièces), reset by <see cref="ResetForNewRound"/>.</summary>
        private PieceColor? _lastPlacedColor;

        /// <summary>
        /// Permanent RUN-long counter for "Gradient" — how many cleared
        /// rows/columns have EVER satisfied its no-two-adjacent-same-color
        /// condition, across every round played so far this run. Unlike
        /// every other tracked field on this class, this one is deliberately
        /// NOT reset by <see cref="ResetForNewRound"/> (explicit request:
        /// "Gradiant modifier est tellement difficile a faire... Ajoute x1 a
        /// ton multiplier pour toutes les round a chaque fois que tu réussi
        /// a accomplir le modifier. Ne se reset jamais.") — Gradient used to
        /// be a per-placement xN that reset every placement like its 5
        /// line-pattern siblings (Arc-en-ciel, Alternance...); it's now a
        /// standalone permanent multiplier that only ever grows for the rest
        /// of the run, see <see cref="ApplyGradient"/>.
        /// </summary>
        private int _gradientPermanentBonus;

        /// <summary>
        /// Dwindling (Epuisement): the flat points bonus this placement's
        /// own instance of the modifier would grant right now — starts at
        /// ScoringConstants.EpuisementStartingBonus, drops by
        /// ScoringConstants.EpuisementDecayPerPlacement after EVERY
        /// placement this modifier is held for, floored at 0. PERMANENT for
        /// the whole run, same as <see cref="_gradientPermanentBonus"/>
        /// above — used to reset every round, but that made it plateau
        /// around its starting value for players whose rounds only fit a
        /// placement or two before the quota was reached (on explicit
        /// report: "Dwelding upgrade ne descend pas sous 95, il devrait
        /// descendre de 5 a chaque pièce joué" — the request's own wording,
        /// "à chaque pièce joué", has no round-boundary exception).
        /// </summary>
        private int _epuisementValue = ScoringConstants.EpuisementStartingBonus;

        /// <summary>
        /// How many placements in a row this round have gone by without a
        /// line/column clear, as of right now (i.e. reflecting only
        /// placements already fully processed by <see cref="PlacePiece"/> —
        /// read this BEFORE calling PlacePiece for the "Spark Tile" piece
        /// trait, so a placement's own clear doesn't erase the streak it's
        /// scoring against). Reset to 0 by <see cref="ResetForNewRound"/>.
        /// </summary>
        public int PlacementsSinceLastClear
        {
            get { return _placementsSinceLastClear; }
        }

        /// <summary>Gradient's current permanent multiplier (1 + <see cref="_gradientPermanentBonus"/>) — what it would apply RIGHT NOW if a qualifying line were cleared this instant. Read by the tooltip's progressive-state line (see RunManager.GetProgressiveModifierStateText).</summary>
        public int GradientCurrentMultiplier
        {
            get { return 1 + _gradientPermanentBonus; }
        }

        /// <summary>Repetition's current same-shape streak (see <see cref="_repetitionStreak"/>) — the xN multiplier it would apply RIGHT NOW if the next placement kept the streak alive, clamped to 1 (its actual no-op floor) while below the 2-in-a-row threshold.</summary>
        public int RepetitionCurrentMultiplier
        {
            get { return _repetitionStreak < 2 ? 1 : _repetitionStreak; }
        }

        /// <summary>Dwindling's current flat points bonus (see <see cref="_epuisementValue"/>) — exactly what the NEXT placement would earn from it right now.</summary>
        public int EpuisementCurrentBonus
        {
            get { return _epuisementValue; }
        }

        /// <summary>How many cells are filled on the board right now — Density's driver (see <see cref="ApplyDensite"/>), exposed for the tooltip's progressive-state line.</summary>
        public int FilledCellCount
        {
            get
            {
                int filled = 0;
                foreach (var pos in AllPositions())
                {
                    if (_cells[pos.x, pos.y].IsFilled)
                    {
                        filled++;
                    }
                }
                return filled;
            }
        }

        public GridManager()
        {
            _cells = new Cell[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _cells[x, y] = new Cell();
                }
            }
        }

        public static bool InBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        public Cell GetCell(int x, int y)
        {
            return _cells[x, y];
        }

        public Cell GetCell(Vector2Int pos)
        {
            return _cells[pos.x, pos.y];
        }

        public static IEnumerable<Vector2Int> AllPositions()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        /// <summary>
        /// Clears fill, lock AND modifier state (golden/tinted/multiplier) for
        /// a new round — see Cell.ResetForNewRound. A "Seeder"-tagged piece's
        /// golden stamp is the only thing that can still be set here (every
        /// other trait clears itself within the same placement); this is what
        /// makes Seeder's effect last "for the rest of the round" rather than
        /// permanently for the whole run.
        /// </summary>
        public void ResetForNewRound()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _cells[x, y].ResetForNewRound();
                }
            }
            _lastGroupSize = null;
            _placementsSinceLastClear = 0;
            _lastPlacedShapeId = null;
            _repetitionStreak = 0;
            _lastPlacedColor = null;
        }

        public bool CanPlace(PieceShape shape, int anchorX, int anchorY)
        {
            var offsets = shape.Cells;
            for (int i = 0; i < offsets.Count; i++)
            {
                int x = anchorX + offsets[i].x;
                int y = anchorY + offsets[i].y;
                if (!InBounds(x, y))
                {
                    return false;
                }

                var cell = _cells[x, y];
                if (cell.IsLocked || cell.IsFilled)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True if at least one of <paramref name="shapes"/> can be placed
        /// somewhere on the grid right now — used to detect a "stuck" board
        /// (no legal move left for any piece currently in hand).
        /// </summary>
        public bool HasAnyValidPlacement(IEnumerable<PieceShape> shapes)
        {
            foreach (var shape in shapes)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        if (CanPlace(shape, x, y))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Places a piece, applying the connected-group bonus, golden bonus, any
        /// active modifier bonuses, and any resulting line/column clears. Assumes
        /// the caller already validated the placement (or will inspect the
        /// returned failure).
        /// </summary>
        public PlacementResult PlacePiece(PieceShape shape, PieceColor color, int anchorX, int anchorY, IReadOnlyList<ModifierId> activeModifiers = null)
        {
            if (!CanPlace(shape, anchorX, anchorY))
            {
                return PlacementResult.Failure("Invalid placement");
            }

            var offsets = shape.Cells;
            var placedCells = new List<Vector2Int>(offsets.Count);

            for (int i = 0; i < offsets.Count; i++)
            {
                int x = anchorX + offsets[i].x;
                int y = anchorY + offsets[i].y;
                var cell = _cells[x, y];
                cell.IsFilled = true;
                cell.FilledColor = color;
                placedCells.Add(new Vector2Int(x, y));
            }

            var result = new PlacementResult();
            result.Success = true;
            result.PlacedCells = placedCells;

            var events = new List<ScoreEvent>();

            // Group bonus: the whole connected same-color group this placement
            // touches is rescored in full — every cell in the merged group
            // contributes again, not just the newly placed ones, like replaying
            // an extended Scrabble word. All of a piece's own cells are always
            // mutually connected (every shape in the catalog is edge-connected),
            // so a single flood-fill from any placed cell finds the whole group.
            var groupCells = FindConnectedGroup(placedCells[0]);

            // Captured before being overwritten below, for "Momentum" (Dégradé)
            // to compare this placement's group size against the previous one.
            int? previousGroupSize = _lastGroupSize;
            _lastGroupSize = groupCells.Count;

            // Captured before this placement's own clear (if any) updates the
            // streak below — 0 here means either the immediately previous
            // placement this round cleared a line, OR this is the round's
            // very first placement (previousGroupSize tells them apart) — see
            // "Rafale" in ApplyPostClearModifiers.
            int streakBeforePlacement = _placementsSinceLastClear;

            // "Repetition" needs how many placements in a row (ending at THIS
            // one) share the same shape, not just whether the immediately
            // previous one matches, so its bonus can scale with streak length
            // (2nd consecutive same-shape placement is x2, 3rd is x3, etc).
            bool continuesShapeStreak = _lastPlacedShapeId.HasValue && _lastPlacedShapeId.Value == shape.Id;
            _repetitionStreak = continuesShapeStreak ? _repetitionStreak + 1 : 1;
            _lastPlacedShapeId = shape.Id;
            // Captured before being overwritten below, for "Color Switch"
            // (different color from last time).
            PieceColor? previousPlacedColor = _lastPlacedColor;
            _lastPlacedColor = color;

            // The tinted-match/multiplier-zone factor is no longer baked into
            // each cell's own score — it's applied ONCE, at the very end of
            // this whole placement (see PlacementResult.GroupMultiplier and
            // .TotalScore), Balatro-style, instead of quietly inflating the
            // group bonus per cell. Every event below carries its plain,
            // unmultiplied "standard" amount.
            int groupMultiplier = ComputeGroupMultiplier(groupCells);
            // Computed from the SAME groupCells but only multiplier-zone
            // cells count (see ComputeLineClearMultiplier) — Tinted no
            // longer reaches the line-clear bonus, which is what keeps it
            // distinct from Multiplier Zone now that its color always
            // matches its own piece.
            int lineClearMultiplier = ComputeLineClearMultiplier(groupCells);
            int groupBonus = 0;
            int goldenBonus = 0;

            for (int i = 0; i < groupCells.Count; i++)
            {
                // Progressive: the Nth cell scored (1-indexed) is worth
                // N * GroupBonusPerCell — see that constant's doc comment.
                int cellScore = (i + 1) * ScoringConstants.GroupBonusPerCell;
                events.Add(new ScoreEvent(ScoreEventType.Group, groupCells[i], cellScore));
                groupBonus += cellScore;

                // Golden fires every time the cell is part of a scored group —
                // not just when it was originally placed — since re-touching a
                // group rescores every cell in it, golden included. Still a flat
                // bonus, independent of group size — but now IS multiplied at
                // the end along with everything else (see GroupMultiplier),
                // unlike before when it was deliberately exempt.
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsGolden)
                {
                    goldenBonus += ScoringConstants.GoldenCellBonus;
                    events.Add(new ScoreEvent(ScoreEventType.Golden, groupCells[i], ScoringConstants.GoldenCellBonus));
                }
            }

            result.GroupBonus = groupBonus;
            result.GoldenBonus = goldenBonus;
            result.GroupMultiplier = groupMultiplier;
            result.LineClearMultiplier = lineClearMultiplier;

            int modifierBonus = 0;
            int modifierMultiplier = 1;
            int modifierLueurBonus = 0;
            int modifierAdditiveMultBonus = 0;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPreClearModifiers(activeModifiers, shape, groupCells, placedCells, groupBonus, events, previousGroupSize, _repetitionStreak, previousPlacedColor, out int preMultiplier, out int preLueur, out int preAdditiveMult);
                modifierMultiplier *= preMultiplier;
                modifierLueurBonus += preLueur;
                modifierAdditiveMultBonus += preAdditiveMult;
            }

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.ClearedCellColors = clearInfo.ClearedCellColors;
            result.ClearedCellTraits = clearInfo.ClearedCellTraits;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            // Bastion cells (Cell.IsBastion) earn the same per-cell bonus as
            // an actually-cleared cell without being in ClearedCells (they're
            // never emptied) — see CollectLineCell/BastionBonusCells.
            result.LineClearScore = (clearInfo.ClearedCells.Count + clearInfo.BastionBonusCells.Count) * ScoringConstants.LineClearBonusPerCell;
            // "Lueur" currency — a completely separate axis from score,
            // driven by color DIVERSITY per cleared line rather than points
            // (see PlacementResult.LueurEarned/LueurGroups).
            var lueurGroups = ComputeLueurGroups(clearInfo.ClearedLines);
            result.LueurGroups = lueurGroups;
            result.LueurEarned = SumLueur(lueurGroups);

            // Updates the streak for the NEXT placement to read (see
            // PlacementsSinceLastClear) — this placement's own clear (if any)
            // resets it, otherwise it extends by one.
            _placementsSinceLastClear = clearInfo.ClearedCells.Count > 0 ? 0 : _placementsSinceLastClear + 1;

            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], ScoringConstants.LineClearBonusPerCell));
            }
            for (int i = 0; i < clearInfo.BastionBonusCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Bastion, clearInfo.BastionBonusCells[i], ScoringConstants.LineClearBonusPerCell));
            }

            // "Rafale": did the immediately previous placement this round
            // also clear a line? previousGroupSize.HasValue rules out the
            // round's very first placement, which would otherwise look
            // identical (streak also starts at 0).
            bool clearedByPreviousPlacement = previousGroupSize.HasValue && streakBeforePlacement == 0;

            float modifierProgressiveMultiplier = 1f;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPostClearModifiers(activeModifiers, clearInfo, placedCells, clearedByPreviousPlacement, events, out int postMultiplier, out int postLueur, out float postProgressiveMultiplier);
                modifierMultiplier *= postMultiplier;
                modifierLueurBonus += postLueur;
                modifierProgressiveMultiplier *= postProgressiveMultiplier;
            }

            result.ModifierBonus = modifierBonus;
            result.ModifierMultiplier = modifierMultiplier;
            result.ModifierLueurBonus = modifierLueurBonus;
            result.AdditiveMultBonus = modifierAdditiveMultBonus;
            result.ProgressiveMultiplier = modifierProgressiveMultiplier;
            // "Combo": reuses the exact same "did the previous placement
            // clear?" signal as Rafale, but multiplies the WHOLE placement's
            // total (see PlacementResult.ComboMultiplier/.TotalScore) — same
            // tier as ModifierMultiplier above, kept as its own field since
            // it's resolved from round-streak state Compute*Modifiers above
            // doesn't otherwise need.
            result.ComboMultiplier = ComputeComboMultiplier(activeModifiers, clearedByPreviousPlacement);
            result.ScoreEvents = events;

            return result;
        }

        /// <summary>
        /// "Lueur" currency (see PlacementResult.LueurGroups): every DISTINCT
        /// non-Joker color within each line this placement cleared becomes
        /// its own <see cref="LueurGroup"/> (every cell of that color in the
        /// line, whether or not they're actually adjacent) worth
        /// EconomyConstants.LueurPerColorGroup — "2 points par couleur", on
        /// explicit request (was briefly "2 points par groupe", i.e. per
        /// CONTIGUOUS same-color run instead of per color; changed back to
        /// per color since that split one color into several paying entries
        /// whenever it wasn't all adjacent). A line's color sequence is
        /// exactly what the 8 line-pattern modifiers (Arc-en-ciel,
        /// Alternance, ...) already read off <see cref="ClearedLine.Colors"/>,
        /// so this reuses that same data with no extra bookkeeping.
        /// </summary>
        private static List<LueurGroup> ComputeLueurGroups(IReadOnlyList<ClearedLine> clearedLines)
        {
            var groups = new List<LueurGroup>();
            for (int i = 0; i < clearedLines.Count; i++)
            {
                var line = clearedLines[i];
                var colors = line.Colors;
                var cellsByColor = new Dictionary<PieceColor, List<Vector2Int>>();
                for (int c = 0; c < colors.Count; c++)
                {
                    if (colors[c] == PieceColor.Joker)
                    {
                        continue;
                    }
                    if (!cellsByColor.TryGetValue(colors[c], out var cells))
                    {
                        cells = new List<Vector2Int>();
                        cellsByColor[colors[c]] = cells;
                    }
                    cells.Add(line.IsRow ? new Vector2Int(c, line.Index) : new Vector2Int(line.Index, c));
                }
                // Fixed color order (not Dictionary enumeration order, which
                // isn't guaranteed) so the animation's group-by-group reveal
                // is consistent from one clear to the next.
                var baseColors = PieceColorUtility.BaseColors;
                for (int b = 0; b < baseColors.Count; b++)
                {
                    if (cellsByColor.TryGetValue(baseColors[b], out var cells))
                    {
                        groups.Add(new LueurGroup(cells, EconomyConstants.LueurPerColorGroup));
                    }
                }
            }
            return groups;
        }

        private static int SumLueur(IReadOnlyList<LueurGroup> groups)
        {
            int total = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                total += groups[i].Amount;
            }
            return total;
        }

        /// <summary>Stacks x2 per copy of "Combo" held, same convention as <see cref="ComputeGroupMultiplier"/> — 1 (no-op) unless the previous placement this round cleared a line.</summary>
        private static int ComputeComboMultiplier(IReadOnlyList<ModifierId> activeModifiers, bool clearedByPreviousPlacement)
        {
            if (activeModifiers == null || !clearedByPreviousPlacement)
            {
                return 1;
            }

            int multiplier = 1;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                if (activeModifiers[i] == ModifierId.Combo)
                {
                    multiplier *= ScoringConstants.ComboMultiplierFactor;
                }
            }
            return multiplier;
        }

        /// <summary>
        /// Modifiers that need the group/placement state as it stood right before
        /// line clears wipe completed rows/columns (Prisme/Chaîne/Méga-chaîne need
        /// the group; Forteresse/Prisonnier need neighbor fill state; Architecte
        /// only needs the shape). Each active modifier is evaluated once per
        /// occurrence, so holding the same modifier twice stacks its effect.
        /// </summary>
        private int ApplyPreClearModifiers(IReadOnlyList<ModifierId> activeModifiers, PieceShape shape, List<Vector2Int> groupCells, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events, int? previousGroupSize, int repetitionStreak, PieceColor? previousPlacedColor, out int modifierMultiplier, out int lueurBonus, out int additiveMultBonus)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            // "Joker": a Joker piece's own cell(s) stay PieceColor.Joker in
            // storage (see FindConnectedGroup) — every color-conditional
            // modifier below normally just never matches it. When Joker is
            // held, this instead resolves to whichever of the 4 base colors
            // would score the most from the Devotion/Éclat modifiers
            // currently active, so ApplyColorDevotionMultiplier/ApplyEclat
            // below use THIS instead of re-reading the cell directly.
            var jokerResolvedColor = ResolveJokerColorForModifiers(ownColor, activeModifiers, groupBonus, groupCells.Count);

            int total = 0;
            int multiplier = 1;
            int lueur = 0;
            int additiveMult = 0;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var id = activeModifiers[i];
                int eventsBefore = events.Count;
                int bonus;
                switch (id)
                {
                    case ModifierId.Prisme:
                        bonus = 0;
                        multiplier *= ApplyPrisme(placedCells, events);
                        break;
                    case ModifierId.Chaine:
                        bonus = ApplyChaine(groupCells, placedCells, events);
                        break;
                    case ModifierId.MegaChaine:
                        bonus = ApplyMegaChaine(groupCells, placedCells, events);
                        break;
                    case ModifierId.Forteresse:
                        bonus = ApplyForteresse(groupCells, events);
                        break;
                    case ModifierId.Prisonnier:
                        bonus = ApplyPrisonnier(groupCells, events);
                        break;
                    case ModifierId.Architecte:
                        bonus = 0;
                        multiplier *= ApplyArchitecte(shape, placedCells, events);
                        break;
                    case ModifierId.Puriste:
                        bonus = 0;
                        multiplier *= ApplyPuriste(groupCells, placedCells, groupBonus, events);
                        break;
                    case ModifierId.Tricolore:
                        bonus = 0;
                        multiplier *= ApplyTricolore(placedCells, events);
                        break;
                    case ModifierId.Complementaire:
                        bonus = 0;
                        multiplier *= ApplyComplementaire(placedCells, events);
                        break;
                    case ModifierId.Ilot:
                        bonus = 0;
                        multiplier *= ApplyIlot(groupCells, placedCells, events);
                        break;
                    case ModifierId.Couronne:
                        bonus = ApplyCouronne(groupCells, events);
                        break;
                    case ModifierId.Carrefour:
                        bonus = ApplyCarrefour(groupCells, events);
                        break;
                    case ModifierId.CercleChromatique:
                        bonus = ApplyCercleChromatique(groupCells, events);
                        break;
                    case ModifierId.Monochrome:
                        bonus = ApplyMonochrome(groupCells, events);
                        break;
                    case ModifierId.Contraste:
                        bonus = ApplyContraste(placedCells, events);
                        break;
                    case ModifierId.Degrade:
                        bonus = 0;
                        multiplier *= ApplyDegrade(groupCells.Count, previousGroupSize, placedCells, events);
                        break;
                    case ModifierId.Emmitouflee:
                        bonus = ApplyEmmitouflee(groupCells, events);
                        break;
                    case ModifierId.Jardinier:
                        bonus = ApplyJardinier(groupCells, events);
                        break;
                    case ModifierId.DevotionCoral:
                        bonus = 0;
                        multiplier *= ApplyColorDevotionMultiplier(PieceColor.Coral, jokerResolvedColor, placedCells, events);
                        break;
                    case ModifierId.DevotionTeal:
                        bonus = 0;
                        multiplier *= ApplyColorDevotionMultiplier(PieceColor.Teal, jokerResolvedColor, placedCells, events);
                        break;
                    case ModifierId.DevotionViolet:
                        bonus = 0;
                        multiplier *= ApplyColorDevotionMultiplier(PieceColor.Violet, jokerResolvedColor, placedCells, events);
                        break;
                    case ModifierId.DevotionLime:
                        bonus = 0;
                        multiplier *= ApplyColorDevotionMultiplier(PieceColor.Lime, jokerResolvedColor, placedCells, events);
                        break;
                    case ModifierId.FormeSingle:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.Single, shape, placedCells, events);
                        break;
                    case ModifierId.FormeDomH:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.DomH, shape, placedCells, events);
                        break;
                    case ModifierId.FormeDomV:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.DomV, shape, placedCells, events);
                        break;
                    case ModifierId.FormeTriL:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.TriL, shape, placedCells, events);
                        break;
                    case ModifierId.FormeTriIH:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.TriIH, shape, placedCells, events);
                        break;
                    case ModifierId.FormeTriIV:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.TriIV, shape, placedCells, events);
                        break;
                    case ModifierId.FormeSq2:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.Sq2, shape, placedCells, events);
                        break;
                    case ModifierId.FormeLTetro:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.LTetro, shape, placedCells, events);
                        break;
                    case ModifierId.FormeTTetro:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.TTetro, shape, placedCells, events);
                        break;
                    case ModifierId.FormeSTetro:
                        bonus = 0;
                        multiplier *= ApplyShapeSpecialistMultiplier(ShapeId.STetro, shape, placedCells, events);
                        break;
                    case ModifierId.FormeSinglePoints:
                        bonus = ApplyShapeGlow(ShapeId.Single, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeDomHPoints:
                        bonus = ApplyShapeGlow(ShapeId.DomH, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeDomVPoints:
                        bonus = ApplyShapeGlow(ShapeId.DomV, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeTriLPoints:
                        bonus = ApplyShapeGlow(ShapeId.TriL, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeTriIHPoints:
                        bonus = ApplyShapeGlow(ShapeId.TriIH, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeTriIVPoints:
                        bonus = ApplyShapeGlow(ShapeId.TriIV, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeSq2Points:
                        bonus = ApplyShapeGlow(ShapeId.Sq2, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeLTetroPoints:
                        bonus = ApplyShapeGlow(ShapeId.LTetro, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeTTetroPoints:
                        bonus = ApplyShapeGlow(ShapeId.TTetro, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.FormeSTetroPoints:
                        bonus = ApplyShapeGlow(ShapeId.STetro, shape, placedCells, groupCells, events);
                        break;
                    case ModifierId.GrandFormat:
                        bonus = ApplyGrandFormat(placedCells, events);
                        break;
                    case ModifierId.HorsNorme:
                        bonus = ApplyHorsNorme(placedCells, events);
                        break;
                    case ModifierId.EclatCoral:
                        bonus = ApplyEclat(PieceColor.Coral, jokerResolvedColor, placedCells, groupCells, events);
                        break;
                    case ModifierId.EclatTeal:
                        bonus = ApplyEclat(PieceColor.Teal, jokerResolvedColor, placedCells, groupCells, events);
                        break;
                    case ModifierId.EclatViolet:
                        bonus = ApplyEclat(PieceColor.Violet, jokerResolvedColor, placedCells, groupCells, events);
                        break;
                    case ModifierId.EclatLime:
                        bonus = ApplyEclat(PieceColor.Lime, jokerResolvedColor, placedCells, groupCells, events);
                        break;
                    case ModifierId.Diagonale:
                        bonus = ApplyDiagonale(groupCells, events);
                        break;
                    case ModifierId.Nid:
                        bonus = ApplyNid(groupCells, events);
                        break;
                    case ModifierId.Solitaire:
                        bonus = 0;
                        multiplier *= ApplySolitaire(groupCells, placedCells, events);
                        break;
                    case ModifierId.PetitFormat:
                        bonus = ApplyPetitFormat(placedCells, events);
                        break;
                    case ModifierId.Fraicheur:
                        bonus = 0;
                        multiplier *= ApplyFraicheur(placedCells, events);
                        break;
                    case ModifierId.Pont:
                        bonus = 0;
                        multiplier *= ApplyPont(ownColor, placedCells, events);
                        break;
                    case ModifierId.Encerclement:
                        bonus = ApplyEncerclement(groupCells, events);
                        break;
                    case ModifierId.Boucher:
                        bonus = ApplyBoucher(placedCells, events);
                        break;
                    case ModifierId.GrosseFamille:
                        bonus = 0;
                        multiplier *= ApplyGrosseFamille(groupCells, placedCells, events);
                        break;
                    case ModifierId.Repetition:
                        bonus = 0;
                        multiplier *= ApplyRepetition(repetitionStreak, placedCells, events);
                        break;
                    case ModifierId.RepetitionLueur:
                        bonus = 0;
                        lueur += ApplyRepetitionLueur(repetitionStreak, placedCells, events);
                        break;
                    case ModifierId.AlternancePieces:
                        bonus = 0;
                        multiplier *= ApplyAlternancePieces(ownColor, previousPlacedColor, placedCells, events);
                        break;
                    case ModifierId.Precision:
                        bonus = ApplyPrecision(placedCells, events);
                        break;
                    case ModifierId.Surpopulation:
                        bonus = ApplySurpopulation(placedCells, events);
                        break;
                    case ModifierId.Minimaliste:
                        bonus = 0;
                        multiplier *= ApplyMinimaliste(placedCells, events);
                        break;
                    case ModifierId.Joker:
                        // No score of its own — purely a passive rule change
                        // resolved above (see jokerResolvedColor) for
                        // Devotion/Éclat.
                        bonus = 0;
                        break;
                    case ModifierId.Combo:
                        // Not a flat/per-cell bonus — resolved separately as
                        // PlacementResult.ComboMultiplier (see ComputeComboMultiplier).
                        bonus = 0;
                        break;
                    case ModifierId.MultUn:
                        bonus = 0;
                        additiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultUnBonus, placedCells, events);
                        break;
                    case ModifierId.MultDeux:
                        bonus = 0;
                        additiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultDeuxBonus, placedCells, events);
                        break;
                    case ModifierId.MultQuatre:
                        bonus = 0;
                        additiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultQuatreBonus, placedCells, events);
                        break;
                    case ModifierId.MultCinqRisque:
                        bonus = 0;
                        additiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultCinqRisqueBonus, placedCells, events);
                        break;
                    case ModifierId.Solidarite:
                        bonus = 0;
                        additiveMult += ApplySolidarite(activeModifiers.Count, placedCells, events);
                        break;
                    case ModifierId.Epuisement:
                        bonus = ApplyEpuisement(placedCells, events);
                        break;
                    case ModifierId.Copieur:
                        // Never actually held — buying it in the shop adds
                        // another copy of whichever modifier was purchased
                        // right before it instead of adding Copieur itself
                        // (see RunManager.BuyModifierSlot), so this case
                        // should never be reached in practice.
                        bonus = 0;
                        break;
                    default:
                        bonus = 0;
                        break;
                }
                TagNewEvents(events, eventsBefore, id);
                total += bonus;
            }
            modifierMultiplier = multiplier;
            lueurBonus = lueur;
            additiveMultBonus = additiveMult;
            return total;
        }

        /// <summary>"Devotion" (per-color): xN multiplier (see ScoringConstants.DevotionMultiplier) when the placement's own fill color matches <paramref name="targetColor"/> — <paramref name="ownColor"/> is the placement's REAL color, unless "Joker" resolves a Joker piece to a different color first (see ResolveJokerColorForModifiers). Was "fully doubles this placement's group bonus" (additive); converted to a genuine multiplier so every color/shape modifier has both a +pts version (Éclat below/the new Forme*Points siblings) and a +mult one, on explicit request. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyColorDevotionMultiplier(PieceColor targetColor, PieceColor ownColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (ownColor != targetColor)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.DevotionMultiplier));
            return ScoringConstants.DevotionMultiplier;
        }

        /// <summary>"Specialist" (per-shape): xN multiplier (see ScoringConstants.FormeSpecialistMultiplier) when the placed piece's own shape matches <paramref name="targetShape"/> — same conversion, and for the same reason, as Devotion above. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyShapeSpecialistMultiplier(ShapeId targetShape, PieceShape shape, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (shape.Id != targetShape)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.FormeSpecialistMultiplier));
            return ScoringConstants.FormeSpecialistMultiplier;
        }

        /// <summary>Grand Format: bonus per placed cell (the piece's own cell count, not the merged group) once the placed piece is at least ScoringConstants.GrandFormatMinPieceSize cells.</summary>
        private static int ApplyGrandFormat(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count < ScoringConstants.GrandFormatMinPieceSize)
            {
                return 0;
            }

            int bonus = placedCells.Count * ScoringConstants.GrandFormatBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Hors Norme: flat bonus whenever the placed piece's own cell count is anything OTHER than exactly ScoringConstants.HorsNormeExactPieceSize — rewards small or large pieces over "average"-sized ones.</summary>
        private static int ApplyHorsNorme(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count == ScoringConstants.HorsNormeExactPieceSize)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.HorsNormeBonus));
            return ScoringConstants.HorsNormeBonus;
        }

        /// <summary>"Éclat" (per-color): flat bonus per scored group cell when the placement's own fill color matches <paramref name="targetColor"/> — a group is always monochrome (see FindConnectedGroup), so a color match means every group cell counts, unlike Devotion this stays a flat per-tile amount rather than doubling the group bonus. <paramref name="ownColor"/> is the placement's REAL color unless "Joker" resolves it to a different one first (see ResolveJokerColorForModifiers).</summary>
        private static int ApplyEclat(PieceColor targetColor, PieceColor ownColor, List<Vector2Int> placedCells, List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            if (ownColor != targetColor)
            {
                return 0;
            }

            int bonus = groupCells.Count * ScoringConstants.EclatBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>The "+pts" sibling of ApplyShapeSpecialistMultiplier (ninth batch, on explicit request) — flat bonus (see ScoringConstants.FormeGlowBonusPerCell) per scored group cell when the placed piece's own shape matches <paramref name="targetShape"/>, mirroring ApplyEclat's per-color role above but for shape instead of color.</summary>
        private static int ApplyShapeGlow(ShapeId targetShape, PieceShape shape, List<Vector2Int> placedCells, List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            if (shape.Id != targetShape)
            {
                return 0;
            }

            int bonus = groupCells.Count * ScoringConstants.FormeGlowBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        // ---- Fifth batch of modifier bonuses (8 new ideas, on explicit request — see README) ----

        /// <summary>Diagonale: bonus per group cell sitting on either of the grid's two main diagonals (x == y, or x + y == Size - 1).</summary>
        private static int ApplyDiagonale(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (pos.x != pos.y && pos.x + pos.y != Size - 1)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.DiagonaleBonusPerCell));
                total += ScoringConstants.DiagonaleBonusPerCell;
            }
            return total;
        }

        /// <summary>Nid: bonus per group cell with EXACTLY 3 of its 4 cardinal neighbors filled — a softer, more attainable sibling of Prisonnier (needs all 4).</summary>
        private int ApplyNid(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (CountFilledCardinalNeighbors(pos.x, pos.y) != 3)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.NidBonusPerCell));
                total += ScoringConstants.NidBonusPerCell;
            }
            return total;
        }

        private int CountFilledCardinalNeighbors(int x, int y)
        {
            int count = 0;
            if (InBounds(x - 1, y) && _cells[x - 1, y].IsFilled) count++;
            if (InBounds(x + 1, y) && _cells[x + 1, y].IsFilled) count++;
            if (InBounds(x, y - 1) && _cells[x, y - 1].IsFilled) count++;
            if (InBounds(x, y + 1) && _cells[x, y + 1].IsFilled) count++;
            return count;
        }

        /// <summary>Solitaire: xN multiplier (see ScoringConstants.SolitaireMultiplier) when this placement's scored group is entirely its own piece — nothing pre-existing merged into it — AND the piece itself is more than 1 cell (the opposite condition from Catalyst, which rewards merging with pre-existing cells; the size-1 case is already Îlot's). Returns 1 (no-op) otherwise.</summary>
        private static int ApplySolitaire(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count <= 1 || groupCells.Count != placedCells.Count)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.SolitaireMultiplier));
            return ScoringConstants.SolitaireMultiplier;
        }

        /// <summary>Petit Format: bonus per placed cell when the piece being placed has at most ScoringConstants.PetitFormatMaxPieceSize cells — the small-piece mirror of Grand Format.</summary>
        private static int ApplyPetitFormat(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count > ScoringConstants.PetitFormatMaxPieceSize)
            {
                return 0;
            }

            int bonus = placedCells.Count * ScoringConstants.PetitFormatBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Fraîcheur: xN multiplier (see ScoringConstants.FraicheurMultiplier) when this placement's own fill color is not present ANYWHERE else already on the board (a genuinely new color for this board state) — checked against every other cell, this placement's own cells excluded. Returns 1 (no-op) otherwise.</summary>
        private int ApplyFraicheur(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            var placedSet = new HashSet<Vector2Int>(placedCells);
            foreach (var pos in AllPositions())
            {
                if (placedSet.Contains(pos))
                {
                    continue;
                }
                var cell = _cells[pos.x, pos.y];
                if (cell.IsFilled && cell.FilledColor.HasValue && cell.FilledColor.Value == ownColor)
                {
                    return 1;
                }
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.FraicheurMultiplier));
            return ScoringConstants.FraicheurMultiplier;
        }

        /// <summary>Espace Libre: xN multiplier (see ScoringConstants.EspaceLibreMultiplier) when, right after this placement (and any of its own line clears), at most ScoringConstants.EspaceLibreMaxFilledCells cells on the whole board are still filled — rewards keeping the board deliberately open. Returns 1 (no-op) otherwise.</summary>
        private int ApplyEspaceLibre(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int filled = 0;
            foreach (var pos in AllPositions())
            {
                if (_cells[pos.x, pos.y].IsFilled)
                {
                    filled++;
                }
            }
            if (filled > ScoringConstants.EspaceLibreMaxFilledCells)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.EspaceLibreMultiplier));
            return ScoringConstants.EspaceLibreMultiplier;
        }

        /// <summary>
        /// Density (Densité): progressive xN multiplier, the opposite of
        /// Espace Libre — N is how many cells are filled on the board right
        /// after this placement (and any of its own line clears), divided
        /// by ScoringConstants.DensiteFilledCellsPerMultiplierStep as a
        /// TRUE float (never floored mid-calculation — on explicit
        /// request: "on doit multiplier comme si c'était un float au lieu
        /// d'arrondir a la baisse"). Returns 1 (no-op, never a debuff)
        /// while fewer than one step's worth of cells are filled. The
        /// per-modifier badge popup still shows a rounded whole number
        /// (ScoreEvent.Amount stays int) — only <see
        /// cref="PlacementResult.TotalScore"/> needs the full precision,
        /// via <see cref="PlacementResult.ProgressiveMultiplier"/>.
        /// </summary>
        private float ApplyDensite(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int filled = 0;
            foreach (var pos in AllPositions())
            {
                if (_cells[pos.x, pos.y].IsFilled)
                {
                    filled++;
                }
            }

            float multiplier = filled / (float)ScoringConstants.DensiteFilledCellsPerMultiplierStep;
            if (multiplier < 1f)
            {
                return 1f;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], Mathf.RoundToInt(multiplier)));
            return multiplier;
        }

        /// <summary>Rafale: xN multiplier (see ScoringConstants.RafaleMultiplier) when this placement clears at least one row/column AND the immediately previous placement this round also did — two clears back to back. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyRafale(bool clearedByPreviousPlacement, ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!clearedByPreviousPlacement || clearInfo.ClearedCells.Count == 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.RafaleMultiplier));
            return ScoringConstants.RafaleMultiplier;
        }

        // ---- Sixth batch of modifier bonuses (11 more, player-authored brainstorm — see README) ----

        /// <summary>Bridge (Pont): xN multiplier (see ScoringConstants.PontMultiplierPerBridge) PER pre-existing group this placement bridges together beyond the first one — bridging 2 formerly-separate groups applies once, 3 groups applies twice (stacking multiplicatively), etc. Returns 1 (no-op) if this placement touches at most one pre-existing group (nothing to bridge).</summary>
        private int ApplyPont(PieceColor ownColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int groupsTouched = CountDistinctPreExistingGroupsTouched(placedCells, ownColor);
            if (groupsTouched < 2)
            {
                return 1;
            }

            int multiplier = 1;
            for (int i = 1; i < groupsTouched; i++)
            {
                multiplier *= ScoringConstants.PontMultiplierPerBridge;
            }
            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>
        /// How many DISTINCT already-existing connected components (using
        /// the same color/joker compatibility rule as <see
        /// cref="FindConnectedGroup"/>) touch this placement's own cells —
        /// this placement's own cells are excluded from every flood-fill, so
        /// two pre-existing groups on opposite sides of the piece are
        /// counted separately even though placing the piece would merge
        /// them into one (that's exactly what "bridging" means for Pont).
        /// Each distinct component is only ever counted once even if
        /// several of the piece's own cells touch it.
        /// </summary>
        private int CountDistinctPreExistingGroupsTouched(List<Vector2Int> placedCells, PieceColor pieceColor)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var globallyVisited = new HashSet<Vector2Int>();
            int distinctGroups = 0;
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                distinctGroups += TryCountNeighborGroup(pos.x - 1, pos.y, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x + 1, pos.y, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x, pos.y - 1, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x, pos.y + 1, placedSet, globallyVisited, pieceColor);
            }
            return distinctGroups;
        }

        private int TryCountNeighborGroup(int x, int y, HashSet<Vector2Int> placedSet, HashSet<Vector2Int> globallyVisited, PieceColor pieceColor)
        {
            if (!InBounds(x, y))
            {
                return 0;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos) || globallyVisited.Contains(pos))
            {
                return 0;
            }
            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return 0;
            }

            var neighborColor = cell.FilledColor.Value;
            if (pieceColor != PieceColor.Joker && neighborColor != PieceColor.Joker && neighborColor != pieceColor)
            {
                return 0;
            }

            var visited = new HashSet<Vector2Int> { pos };
            var stack = new Stack<Vector2Int>();
            stack.Push(pos);
            PieceColor? anchor = neighborColor == PieceColor.Joker ? (PieceColor?)null : neighborColor;
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                FloodVisitExcludingPiece(cur.x - 1, cur.y, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x + 1, cur.y, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x, cur.y - 1, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x, cur.y + 1, placedSet, ref anchor, visited, stack);
            }
            globallyVisited.UnionWith(visited);
            return 1;
        }

        /// <summary>Same neighbor/anchor-color rules as <see cref="TryVisitGroupNeighbor"/>, but additionally never steps into <paramref name="placedSet"/> — used to flood-fill a PRE-existing component's true extent without the flood leaking through the piece currently being placed into a different, unrelated component on its other side.</summary>
        private void FloodVisitExcludingPiece(int x, int y, HashSet<Vector2Int> placedSet, ref PieceColor? anchorColor, HashSet<Vector2Int> visited, Stack<Vector2Int> stack)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos) || visited.Contains(pos))
            {
                return;
            }
            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return;
            }

            var color = cell.FilledColor.Value;
            if (color != PieceColor.Joker)
            {
                if (anchorColor.HasValue && anchorColor.Value != color)
                {
                    return;
                }
                anchorColor = color;
            }

            visited.Add(pos);
            stack.Push(pos);
        }

        /// <summary>Encirclement: bonus per group cell whose 8 surrounding tiles are all filled OR off the edge of the grid — a softer sibling of Fortress, which never credits an edge/corner cell (out of bounds always fails its check).</summary>
        private int ApplyEncerclement(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilledOrOffGrid(pos.x, pos.y))
                {
                    continue;
                }
                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.EncerclementBonusPerCell));
                total += ScoringConstants.EncerclementBonusPerCell;
            }
            return total;
        }

        private bool AreAllNeighborsFilledOrOffGrid(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!InBounds(nx, ny))
                    {
                        continue; // off the grid counts as "filled" for Encerclement
                    }
                    if (!_cells[nx, ny].IsFilled)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sealer (Boucher): bonus per PRE-EXISTING tile that this placement
        /// itself causes to become "encircled" (see Encerclement/
        /// AreAllNeighborsFilledOrOffGrid) — any already-filled tile that
        /// borders one of this placement's own cells was, by definition, NOT
        /// fully encircled before this placement (that very neighbor slot
        /// was still empty), so if it qualifies now, this placement is what
        /// just sealed it.
        /// </summary>
        private int ApplyBoucher(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var candidates = new HashSet<Vector2Int>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        int x = pos.x + dx;
                        int y = pos.y + dy;
                        if (!InBounds(x, y))
                        {
                            continue;
                        }
                        var npos = new Vector2Int(x, y);
                        if (placedSet.Contains(npos) || !_cells[x, y].IsFilled)
                        {
                            continue;
                        }
                        candidates.Add(npos);
                    }
                }
            }

            int total = 0;
            foreach (var pos in candidates)
            {
                if (!AreAllNeighborsFilledOrOffGrid(pos.x, pos.y))
                {
                    continue;
                }
                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.BoucherBonusPerCell));
                total += ScoringConstants.BoucherBonusPerCell;
            }
            return total;
        }

        /// <summary>Big Family (Grosse Famille): xN multiplier (see ScoringConstants.GrosseFamilleMultiplier) when this placement's color exists in exactly ONE connected group on the whole board — no other same-color cell anywhere outside this placement's own scored group. Returns 1 (no-op) otherwise.</summary>
        private int ApplyGrosseFamille(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            var groupSet = new HashSet<Vector2Int>(groupCells);
            foreach (var pos in AllPositions())
            {
                if (groupSet.Contains(pos))
                {
                    continue;
                }
                var cell = _cells[pos.x, pos.y];
                if (cell.IsFilled && cell.FilledColor.HasValue && cell.FilledColor.Value == ownColor)
                {
                    return 1;
                }
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.GrosseFamilleMultiplier));
            return ScoringConstants.GrosseFamilleMultiplier;
        }

        /// <summary>Repetition (progressive): xN multiplier where N is <paramref name="repetitionStreak"/>, the number of placements in a row (this one included) sharing the same shape — x2 on the 2nd consecutive same-shape placement, x3 on the 3rd, and so on. Returns 1 (no-op) on the first placement of a new streak.</summary>
        private static int ApplyRepetition(int repetitionStreak, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (repetitionStreak < 2)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], repetitionStreak));
            return repetitionStreak;
        }

        /// <summary>Golden Repetition (RepetitionLueur): flat Lueur (see EconomyConstants.RepetitionLueurBonus) when this piece is the same shape as the immediately previous placement this round — the Lueur-earning sibling of Repetition, on the same "does this streak continue" condition but unlike it, not progressive (repeats the same flat amount every time it fires rather than scaling with streak length). Returns 0 (no-op) on the first placement of a new streak.</summary>
        private static int ApplyRepetitionLueur(int repetitionStreak, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (repetitionStreak < 2)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], EconomyConstants.RepetitionLueurBonus));
            return EconomyConstants.RepetitionLueurBonus;
        }

        /// <summary>Shared by the 3 flat, unconditional "+Mult" modifiers (Mult +1/+2/+4) and Risky Mult (ninth batch, on explicit request) — always fires, adds <paramref name="amount"/> to PlacementResult.AdditiveMultBonus (a genuine ADDITIVE pool, unlike every ModifierMultiplier modifier above).</summary>
        private static int ApplyFlatAdditiveMult(int amount, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], amount));
            return amount;
        }

        /// <summary>Solidarity (Solidarite): +N Mult (additive, see PlacementResult.AdditiveMultBonus) where N is the total number of modifiers currently held (this one included, every duplicate copy counting separately), on explicit request ("un modifier +1 mult chaque modifier possédé").</summary>
        private static int ApplySolidarite(int modifierCount, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], modifierCount));
            return modifierCount;
        }

        /// <summary>Dwindling (Epuisement): the current decaying flat points bonus (see _epuisementValue), then drops it by ScoringConstants.EpuisementDecayPerPlacement for the NEXT placement (floored at 0) — on explicit request ("+100pts, réduit de 5 a chaque coup"). Permanent for the whole run, same as Gradient's counter — NOT reset by ResetForNewRound (see _epuisementValue's own doc comment for why the earlier per-round reset was removed).</summary>
        private int ApplyEpuisement(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int bonus = _epuisementValue;
            if (bonus > 0)
            {
                events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            }
            _epuisementValue = Mathf.Max(0, _epuisementValue - ScoringConstants.EpuisementDecayPerPlacement);
            return bonus;
        }

        /// <summary>Color Switch (Alternance des pièces): xN multiplier (see ScoringConstants.AlternancePiecesMultiplier) when this piece's color differs from the immediately previous placement's color this round — the piece-to-piece sibling of the existing line-level "Alternation" (Alternance) modifier. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyAlternancePieces(PieceColor ownColor, PieceColor? previousPlacedColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!previousPlacedColor.HasValue || previousPlacedColor.Value == ownColor)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.AlternancePiecesMultiplier));
            return ScoringConstants.AlternancePiecesMultiplier;
        }

        /// <summary>Precision: bonus per placed cell when EVERY one of this placement's own cells has at least one pre-existing filled orthogonal neighbor (this placement's own other cells don't count).</summary>
        private int ApplyPrecision(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (CountExistingOrthogonalNeighbors(placedCells[i], placedSet) < 1)
                {
                    return 0;
                }
            }

            int bonus = placedCells.Count * ScoringConstants.PrecisionBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Overcrowding (Surpopulation): bonus per placed cell when EVERY one of this placement's own cells has at least 2 pre-existing filled orthogonal neighbors — a stricter sibling of Precision.</summary>
        private int ApplySurpopulation(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (CountExistingOrthogonalNeighbors(placedCells[i], placedSet) < 2)
                {
                    return 0;
                }
            }

            int bonus = placedCells.Count * ScoringConstants.SurpopulationBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        private int CountExistingOrthogonalNeighbors(Vector2Int pos, HashSet<Vector2Int> placedSet)
        {
            int count = 0;
            if (IsExistingFilledNeighbor(pos.x - 1, pos.y, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x + 1, pos.y, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x, pos.y - 1, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x, pos.y + 1, placedSet)) count++;
            return count;
        }

        private bool IsExistingFilledNeighbor(int x, int y, HashSet<Vector2Int> placedSet)
        {
            if (!InBounds(x, y))
            {
                return false;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos))
            {
                return false; // part of this same piece, doesn't count as "pre-existing"
            }
            return _cells[x, y].IsFilled;
        }

        /// <summary>Minimalist (Minimaliste): xN multiplier (see ScoringConstants.MinimalisteMultiplier) when this placement's WHOLE footprint touches EXACTLY one distinct pre-existing filled cell in total — the "just barely touching" middle ground between Îlot (zero neighbors, isolated) and Precision (one or more, checked per cell). Returns 1 (no-op) otherwise.</summary>
        private int ApplyMinimaliste(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var distinctNeighbors = new HashSet<Vector2Int>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                AddIfExistingFilledNeighbor(pos.x - 1, pos.y, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x + 1, pos.y, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x, pos.y - 1, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x, pos.y + 1, placedSet, distinctNeighbors);
            }
            if (distinctNeighbors.Count != 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.MinimalisteMultiplier));
            return ScoringConstants.MinimalisteMultiplier;
        }

        private void AddIfExistingFilledNeighbor(int x, int y, HashSet<Vector2Int> placedSet, HashSet<Vector2Int> result)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos))
            {
                return;
            }
            if (_cells[x, y].IsFilled)
            {
                result.Add(pos);
            }
        }

        /// <summary>
        /// "Joker" (spec extension, player-authored): when this placement's
        /// own color is actually <see cref="PieceColor.Joker"/> and the
        /// "Joker" modifier is held, resolves to whichever of the 4 base
        /// colors would score the most from the Devotion/Éclat modifiers
        /// currently active — those are the two families where "this
        /// placement's own color" directly gates a bonus. Falls back to the
        /// piece's real color (Joker stays Joker) whenever the modifier
        /// isn't held, the piece isn't actually a Joker, or no candidate
        /// color would score anything anyway.
        /// </summary>
        private static PieceColor ResolveJokerColorForModifiers(PieceColor actualColor, IReadOnlyList<ModifierId> activeModifiers, int groupBonus, int groupCellCount)
        {
            if (actualColor != PieceColor.Joker || !ContainsModifier(activeModifiers, ModifierId.Joker))
            {
                return actualColor;
            }

            var baseColors = PieceColorUtility.BaseColors;
            PieceColor best = actualColor;
            int bestScore = 0;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var candidate = baseColors[i];
                int score = 0;
                if (ContainsModifier(activeModifiers, DevotionModifierFor(candidate)))
                {
                    // Devotion is now a genuine xN multiplier (see
                    // ApplyColorDevotionMultiplier), not an additive
                    // "doubles the group bonus" — its points-equivalent
                    // "extra" for this comparison is groupBonus times
                    // (multiplier - 1), same as every other xN catch-up
                    // elsewhere (e.g. GameBootstrap's multipliedExtra).
                    // With the multiplier at 2 this is still exactly
                    // groupBonus, so this heuristic's behavior is unchanged.
                    score += groupBonus * (ScoringConstants.DevotionMultiplier - 1);
                }
                if (ContainsModifier(activeModifiers, EclatModifierFor(candidate)))
                {
                    score += groupCellCount * ScoringConstants.EclatBonusPerCell;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return bestScore > 0 ? best : actualColor;
        }

        private static ModifierId DevotionModifierFor(PieceColor color)
        {
            switch (color)
            {
                case PieceColor.Coral: return ModifierId.DevotionCoral;
                case PieceColor.Teal: return ModifierId.DevotionTeal;
                case PieceColor.Violet: return ModifierId.DevotionViolet;
                default: return ModifierId.DevotionLime;
            }
        }

        private static ModifierId EclatModifierFor(PieceColor color)
        {
            switch (color)
            {
                case PieceColor.Coral: return ModifierId.EclatCoral;
                case PieceColor.Teal: return ModifierId.EclatTeal;
                case PieceColor.Violet: return ModifierId.EclatViolet;
                default: return ModifierId.EclatLime;
            }
        }

        private static bool ContainsModifier(IReadOnlyList<ModifierId> modifiers, ModifierId id)
        {
            if (modifiers == null)
            {
                return false;
            }
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Collectionneur/Maçon/Démolisseur/the 8 line-pattern modifiers all need the outcome of this placement's line clears, so they can only be evaluated after <see cref="CheckAndClearLines"/> runs.</summary>
        private int ApplyPostClearModifiers(IReadOnlyList<ModifierId> activeModifiers, ClearInfo clearInfo, List<Vector2Int> placedCells, bool clearedByPreviousPlacement, List<ScoreEvent> events, out int modifierMultiplier, out int lueurBonus, out float progressiveMultiplier)
        {
            int total = 0;
            int multiplier = 1;
            int lueur = 0;
            float progressiveMult = 1f;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var id = activeModifiers[i];
                int eventsBefore = events.Count;
                int bonus;
                switch (id)
                {
                    case ModifierId.Collectionneur:
                        bonus = ApplyCollectionneur(clearInfo, placedCells, events);
                        break;
                    case ModifierId.CollectionneurLueur:
                        bonus = 0;
                        lueur += ApplyCollectionneurLueur(clearInfo, placedCells, events);
                        break;
                    case ModifierId.Macon:
                        bonus = 0;
                        multiplier *= ApplyMacon(clearInfo, placedCells, events);
                        break;
                    case ModifierId.Demolisseur:
                        bonus = 0;
                        multiplier *= ApplyDemolisseur(clearInfo, placedCells, events);
                        break;
                    case ModifierId.ArcEnCiel:
                        bonus = 0;
                        multiplier *= ApplyPerLineMultiplier(clearInfo, placedCells, events, ContainsAllBaseColors, ScoringConstants.ArcEnCielMultiplierPerLine);
                        break;
                    case ModifierId.ArcEnCielLueur:
                        bonus = 0;
                        lueur += ApplyPerLineLueur(clearInfo, placedCells, events, ContainsAllBaseColors, EconomyConstants.ArcEnCielLueurPerLine);
                        break;
                    case ModifierId.Alternance:
                        bonus = 0;
                        multiplier *= ApplyPerLineMultiplier(clearInfo, placedCells, events, IsAlternatingTwoColors, ScoringConstants.AlternanceMultiplierPerLine);
                        break;
                    case ModifierId.AlternanceLueur:
                        bonus = 0;
                        lueur += ApplyPerLineLueur(clearInfo, placedCells, events, IsAlternatingTwoColors, EconomyConstants.AlternanceLueurPerLine);
                        break;
                    case ModifierId.Palindrome:
                        bonus = 0;
                        multiplier *= ApplyPerLineMultiplier(clearInfo, placedCells, events, IsPalindrome, ScoringConstants.PalindromeMultiplierPerLine);
                        break;
                    case ModifierId.Gradient:
                        bonus = 0;
                        multiplier *= ApplyGradient(clearInfo, placedCells, events);
                        break;
                    case ModifierId.Bloc:
                        bonus = 0;
                        multiplier *= ApplyPerLineMultiplier(clearInfo, placedCells, events, IsAllBlocksOfAtLeastTwo, ScoringConstants.BlocMultiplierPerLine);
                        break;
                    case ModifierId.MonochromeLigne:
                        bonus = 0;
                        multiplier *= ApplyPerLineMultiplier(clearInfo, placedCells, events, IsMonochromeLine, ScoringConstants.MonochromeLigneMultiplierPerLine);
                        break;
                    case ModifierId.MonochromeLigneLueur:
                        bonus = 0;
                        lueur += ApplyPerLineLueur(clearInfo, placedCells, events, IsMonochromeLine, EconomyConstants.MonochromeLigneLueurPerLine);
                        break;
                    case ModifierId.EspaceLibre:
                        bonus = 0;
                        multiplier *= ApplyEspaceLibre(placedCells, events);
                        break;
                    case ModifierId.Rafale:
                        bonus = 0;
                        multiplier *= ApplyRafale(clearedByPreviousPlacement, clearInfo, placedCells, events);
                        break;
                    case ModifierId.Densite:
                        bonus = 0;
                        progressiveMult *= ApplyDensite(placedCells, events);
                        break;
                    default:
                        bonus = 0;
                        break;
                }
                TagNewEvents(events, eventsBefore, id);
                total += bonus;
            }
            modifierMultiplier = multiplier;
            lueurBonus = lueur;
            progressiveMultiplier = progressiveMult;
            return total;
        }

        /// <summary>Stamps every event appended since <paramref name="startIndex"/> with the modifier that produced it, so the presentation layer knows which one to highlight.</summary>
        private static void TagNewEvents(List<ScoreEvent> events, int startIndex, ModifierId id)
        {
            for (int i = startIndex; i < events.Count; i++)
            {
                events[i].TriggeringModifier = id;
            }
        }

        /// <summary>Prisme: xN multiplier (see ScoringConstants.PrismeMultiplier) when the placement (itself + its direct neighbors) touches 4 distinct non-joker colors (or 3 + a joker). Returns 1 (no-op) when it doesn't qualify.</summary>
        private int ApplyPrisme(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            bool hasJoker = touching.Remove(PieceColor.Joker);

            bool qualifies = touching.Count >= ScoringConstants.PrismeMinDistinctColors
                || (touching.Count == ScoringConstants.PrismeMinDistinctColors - 1 && hasJoker);
            if (!qualifies)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.PrismeMultiplier));
            return ScoringConstants.PrismeMultiplier;
        }

        /// <summary>
        /// Every distinct color among the placement's own cells and every cell
        /// orthogonally adjacent to any of them — used by Prisme/Tricolore/
        /// Complémentaire, which measure color diversity AROUND a placement
        /// rather than within its scored group. This is deliberately different
        /// from the connected group: since <see cref="FindConnectedGroup"/>
        /// locks a group onto a single real color (a joker never bridges two
        /// different colors together, see its doc comment), a scored group can
        /// never contain more than one non-joker color, so "distinct colors in
        /// the group" is never satisfiable and these modifiers look at what the
        /// placement touches instead.
        /// </summary>
        private HashSet<PieceColor> CollectTouchingColors(List<Vector2Int> placedCells)
        {
            var colors = new HashSet<PieceColor>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                colors.Add(_cells[pos.x, pos.y].FilledColor.Value);
                AddColorIfFilled(colors, pos.x - 1, pos.y);
                AddColorIfFilled(colors, pos.x + 1, pos.y);
                AddColorIfFilled(colors, pos.x, pos.y - 1);
                AddColorIfFilled(colors, pos.x, pos.y + 1);
            }
            return colors;
        }

        private void AddColorIfFilled(HashSet<PieceColor> colors, int x, int y)
        {
            if (InBounds(x, y) && _cells[x, y].IsFilled && _cells[x, y].FilledColor.HasValue)
            {
                colors.Add(_cells[x, y].FilledColor.Value);
            }
        }

        private int ApplyChaine(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count < ScoringConstants.ChaineMinGroupSize)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.ChaineBonus));
            return ScoringConstants.ChaineBonus;
        }

        private int ApplyMegaChaine(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count < ScoringConstants.MegaChaineMinGroupSize)
            {
                return 0;
            }

            int extraCells = groupCells.Count - ScoringConstants.MegaChaineMinGroupSize;
            int bonus = ScoringConstants.MegaChaineBaseBonus + extraCells * ScoringConstants.MegaChaineBonusPerExtraCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        private int ApplyForteresse(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: true))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.ForteresseBonusPerCell));
                total += ScoringConstants.ForteresseBonusPerCell;
            }
            return total;
        }

        private int ApplyPrisonnier(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.PrisonnierBonusPerCell));
                total += ScoringConstants.PrisonnierBonusPerCell;
            }
            return total;
        }

        /// <summary>
        /// True if every one of a cell's neighbors is in-bounds and filled — an
        /// out-of-bounds neighbor always fails this, so edge/corner cells can
        /// never qualify for Forteresse/Prisonnier.
        /// </summary>
        private bool AreAllNeighborsFilled(int x, int y, bool includeDiagonals)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    if (!includeDiagonals && dx != 0 && dy != 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int ny = y + dy;
                    if (!InBounds(nx, ny) || !_cells[nx, ny].IsFilled)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Architecte: xN multiplier (see ScoringConstants.ArchitecteMultiplier) for placing a 2x2 square piece. Returns 1 (no-op) otherwise.</summary>
        private int ApplyArchitecte(PieceShape shape, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (shape.Id != ShapeId.Sq2)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ArchitecteMultiplier));
            return ScoringConstants.ArchitecteMultiplier;
        }

        /// <summary>Puriste: xN multiplier (see ScoringConstants.PuristeMultiplier) when the placement's scored group is monochrome (jokers ignored) and actually scored something. Returns 1 (no-op) otherwise.</summary>
        private int ApplyPuriste(List<Vector2Int> groupCells, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events)
        {
            PieceColor? monoColor = null;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var color = _cells[groupCells[i].x, groupCells[i].y].FilledColor.Value;
                if (color == PieceColor.Joker)
                {
                    continue;
                }
                if (!monoColor.HasValue)
                {
                    monoColor = color;
                }
                else if (monoColor.Value != color)
                {
                    return 1;
                }
            }

            if (groupBonus <= 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.PuristeMultiplier));
            return ScoringConstants.PuristeMultiplier;
        }

        private int ApplyCollectionneur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCellColors.Count == 0)
            {
                return 0;
            }

            var distinctColors = new HashSet<PieceColor>();
            for (int i = 0; i < clearInfo.ClearedCellColors.Count; i++)
            {
                distinctColors.Add(clearInfo.ClearedCellColors[i]);
            }

            int bonus = distinctColors.Count * ScoringConstants.CollectionneurBonusPerColor;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Glowing Collector (CollectionneurLueur): Lueur (see EconomyConstants.CollectionneurLueurPerColor) per distinct color among this placement's cleared cells — the Lueur-earning sibling of Collectionneur, same distinct-color count, same trigger condition (no cleared cells this placement = 0, no event).</summary>
        private int ApplyCollectionneurLueur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCellColors.Count == 0)
            {
                return 0;
            }

            var distinctColors = new HashSet<PieceColor>();
            for (int i = 0; i < clearInfo.ClearedCellColors.Count; i++)
            {
                distinctColors.Add(clearInfo.ClearedCellColors[i]);
            }

            int lueur = distinctColors.Count * EconomyConstants.CollectionneurLueurPerColor;
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], lueur));
            return lueur;
        }

        /// <summary>Tricolore: xN multiplier (see ScoringConstants.TricoloreMultiplier) when the placement (itself + its direct neighbors) touches exactly TricoloreExactDistinctColors distinct non-joker colors. Returns 1 (no-op) otherwise.</summary>
        private int ApplyTricolore(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            touching.Remove(PieceColor.Joker);

            if (touching.Count != ScoringConstants.TricoloreExactDistinctColors)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.TricoloreMultiplier));
            return ScoringConstants.TricoloreMultiplier;
        }

        /// <summary>Arbitrary complementary pairing across the 4 base colors — not derived from a color wheel, just a fixed pairing for this modifier.</summary>
        private static readonly PieceColor[][] ComplementaryPairs =
        {
            new[] { PieceColor.Coral, PieceColor.Violet },
            new[] { PieceColor.Teal, PieceColor.Lime }
        };

        /// <summary>Complémentaire: xN multiplier (see ScoringConstants.ComplementaireMultiplier) when the placement (itself + its direct neighbors) touches both colors of a complementary pair. Returns 1 (no-op) otherwise.</summary>
        private int ApplyComplementaire(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var present = CollectTouchingColors(placedCells);

            bool qualifies = false;
            for (int i = 0; i < ComplementaryPairs.Length; i++)
            {
                if (present.Contains(ComplementaryPairs[i][0]) && present.Contains(ComplementaryPairs[i][1]))
                {
                    qualifies = true;
                    break;
                }
            }

            if (!qualifies)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ComplementaireMultiplier));
            return ScoringConstants.ComplementaireMultiplier;
        }

        /// <summary>Îlot: xN multiplier (see ScoringConstants.IlotMultiplier) when the placement's resulting group is a single isolated cell. Returns 1 (no-op) otherwise.</summary>
        private int ApplyIlot(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count != 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.IlotMultiplier));
            return ScoringConstants.IlotMultiplier;
        }

        private int ApplyCouronne(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (pos.x != 0 && pos.x != Size - 1 && pos.y != 0 && pos.y != Size - 1)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CouronneBonusPerCell));
                total += ScoringConstants.CouronneBonusPerCell;
            }
            return total;
        }

        private int ApplyCarrefour(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }
                if (!HasAtLeastTwoDistinctCardinalNeighborColorsDifferentFromOwn(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CarrefourBonusPerCell));
                total += ScoringConstants.CarrefourBonusPerCell;
            }
            return total;
        }

        /// <summary>
        /// Assumes all 4 cardinal neighbors are already known filled (see
        /// <see cref="AreAllNeighborsFilled"/>), so each has a non-null
        /// <see cref="Cell.FilledColor"/>. Requires at least 2 distinct neighbor
        /// colors that ALSO differ from the cell's own color — a neighbor sharing
        /// the cell's own color doesn't count toward the "crossroads" of
        /// different colors.
        /// </summary>
        private bool HasAtLeastTwoDistinctCardinalNeighborColorsDifferentFromOwn(int x, int y)
        {
            var ownColor = _cells[x, y].FilledColor.Value;
            var colors = new HashSet<PieceColor>();
            AddIfDifferentFromOwn(colors, _cells[x - 1, y].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x + 1, y].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x, y - 1].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x, y + 1].FilledColor.Value, ownColor);
            return colors.Count >= 2;
        }

        private static void AddIfDifferentFromOwn(HashSet<PieceColor> colors, PieceColor neighborColor, PieceColor ownColor)
        {
            if (neighborColor != ownColor)
            {
                colors.Add(neighborColor);
            }
        }

        private int ApplyCercleChromatique(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }
                if (!CardinalNeighborsCoverAllBaseColors(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CercleChromatiqueBonusPerCell));
                total += ScoringConstants.CercleChromatiqueBonusPerCell;
            }
            return total;
        }

        /// <summary>Assumes all 4 cardinal neighbors are already known filled (see <see cref="AreAllNeighborsFilled"/>). A joker neighbor consumes one of the 4 slots without contributing a base color, so it can never complete the wheel on its own.</summary>
        private bool CardinalNeighborsCoverAllBaseColors(int x, int y)
        {
            var colors = new HashSet<PieceColor>
            {
                _cells[x - 1, y].FilledColor.Value,
                _cells[x + 1, y].FilledColor.Value,
                _cells[x, y - 1].FilledColor.Value,
                _cells[x, y + 1].FilledColor.Value
            };
            colors.Remove(PieceColor.Joker);
            return colors.Count == PieceColorUtility.BaseColors.Count;
        }

        /// <summary>Stricter sibling of Puriste: the group must be a single real color with ZERO jokers anywhere in it, not just ignoring them.</summary>
        private int ApplyMonochrome(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            PieceColor? monoColor = null;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var color = _cells[groupCells[i].x, groupCells[i].y].FilledColor.Value;
                if (color == PieceColor.Joker)
                {
                    return 0;
                }
                if (!monoColor.HasValue)
                {
                    monoColor = color;
                }
                else if (monoColor.Value != color)
                {
                    return 0;
                }
            }

            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Modifier, groupCells[i], ScoringConstants.MonochromeBonusPerCell));
                total += ScoringConstants.MonochromeBonusPerCell;
            }
            return total;
        }

        private int ApplyContraste(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                var ownColor = _cells[pos.x, pos.y].FilledColor.Value;
                if (!HasContrastingNeighbor(pos.x, pos.y, ownColor))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.ContrasteBonusPerCell));
                total += ScoringConstants.ContrasteBonusPerCell;
            }
            return total;
        }

        private bool HasContrastingNeighbor(int x, int y, PieceColor ownColor)
        {
            return IsFilledWithDifferentColor(x - 1, y, ownColor)
                || IsFilledWithDifferentColor(x + 1, y, ownColor)
                || IsFilledWithDifferentColor(x, y - 1, ownColor)
                || IsFilledWithDifferentColor(x, y + 1, ownColor);
        }

        private bool IsFilledWithDifferentColor(int x, int y, PieceColor ownColor)
        {
            return InBounds(x, y) && _cells[x, y].IsFilled && _cells[x, y].FilledColor.HasValue && _cells[x, y].FilledColor.Value != ownColor;
        }

        /// <summary>Dégradé (Momentum): xN multiplier (see ScoringConstants.DegradeMultiplier) whenever this placement's scored group is strictly larger than the previous placement's this round. Returns 1 (no-op) otherwise.</summary>
        private int ApplyDegrade(int currentGroupSize, int? previousGroupSize, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!previousGroupSize.HasValue || currentGroupSize <= previousGroupSize.Value)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.DegradeMultiplier));
            return ScoringConstants.DegradeMultiplier;
        }

        private int ApplyEmmitouflee(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllDiagonalNeighborsFilled(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.EmmitoufleeBonusPerCell));
                total += ScoringConstants.EmmitoufleeBonusPerCell;
            }
            return total;
        }

        private bool AreAllDiagonalNeighborsFilled(int x, int y)
        {
            return IsInBoundsAndFilled(x - 1, y - 1) && IsInBoundsAndFilled(x + 1, y - 1)
                && IsInBoundsAndFilled(x - 1, y + 1) && IsInBoundsAndFilled(x + 1, y + 1);
        }

        private bool IsInBoundsAndFilled(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y].IsFilled;
        }

        private int ApplyJardinier(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!HasAnyModifierNeighbor(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.JardinierBonusPerCell));
                total += ScoringConstants.JardinierBonusPerCell;
            }
            return total;
        }

        private bool HasAnyModifierNeighbor(int x, int y)
        {
            return HasModifierAt(x - 1, y) || HasModifierAt(x + 1, y) || HasModifierAt(x, y - 1) || HasModifierAt(x, y + 1);
        }

        private bool HasModifierAt(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y].HasAnyModifier;
        }

        /// <summary>Maçon: xN multiplier (see ScoringConstants.MaconMultiplier) for a placement that clears no line/column at all. Returns 1 (no-op) otherwise.</summary>
        private int ApplyMacon(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCells.Count > 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.MaconMultiplier));
            return ScoringConstants.MaconMultiplier;
        }

        /// <summary>Démolisseur: xN multiplier (see ScoringConstants.DemolisseurMultiplierPerLine) PER simultaneously-cleared line, once at least DemolisseurMinLines rows/columns clear at once — stacks multiplicatively (3 lines at once is xN*xN*xN). Returns 1 (no-op) otherwise.</summary>
        private int ApplyDemolisseur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedLineCount < ScoringConstants.DemolisseurMinLines)
            {
                return 1;
            }

            int multiplier = 1;
            for (int i = 0; i < clearInfo.ClearedLineCount; i++)
            {
                multiplier *= ScoringConstants.DemolisseurMultiplierPerLine;
            }
            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>Shared driver for the 5 remaining line-pattern modifiers that just need a per-line yes/no predicate over its ordered color sequence — xN multiplier fires once per qualifying cleared line, stacking multiplicatively (2 qualifying lines at once is xN*xN), reset every placement. Was a flat per-line bonus (ApplyPerLineBonus) before these were converted to multipliers. Gradient used to be the 6th (see <see cref="ApplyGradient"/> for why it's no longer here).</summary>
        private int ApplyPerLineMultiplier(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events, System.Func<IReadOnlyList<PieceColor>, bool> predicate, int multiplierPerLine)
        {
            int multiplier = 1;
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!predicate(lines[i].Colors))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplierPerLine));
                multiplier *= multiplierPerLine;
            }
            return multiplier;
        }

        /// <summary>Shared driver for the 3 Lueur-earning line-pattern modifiers (ArcEnCielLueur/AlternanceLueur/MonochromeLigneLueur) — same idea as ApplyPerLineMultiplier above, reusing the exact same predicates, but adds flat Lueur per qualifying cleared line instead of multiplying a score factor.</summary>
        private int ApplyPerLineLueur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events, System.Func<IReadOnlyList<PieceColor>, bool> predicate, int lueurPerLine)
        {
            int total = 0;
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!predicate(lines[i].Colors))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], lueurPerLine));
                total += lueurPerLine;
            }
            return total;
        }

        /// <summary>
        /// Gradient (permanent, on explicit request): unlike its 5
        /// line-pattern siblings above, this one never resets. Every cleared
        /// row/column in THIS placement satisfying <see cref="IsGradientLine"/>
        /// PERMANENTLY increments <see cref="_gradientPermanentBonus"/> by 1
        /// (once per qualifying line, so 2 qualifying lines at once still
        /// add +2). The returned multiplier is always (1 + that counter) —
        /// applied to THIS placement immediately (including the very line
        /// clear that just grew it), and to every placement for the rest of
        /// the run from then on, whether or not it clears any line at all.
        /// Returns 1 (no-op) only while the counter is still 0, i.e. Gradient
        /// has never fired yet this run.
        /// </summary>
        private int ApplyGradient(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsGradientLine(lines[i].Colors))
                {
                    _gradientPermanentBonus++;
                }
            }

            int multiplier = 1 + _gradientPermanentBonus;
            if (multiplier <= 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>Non-joker colors present, ignoring how many times each repeats.</summary>
        private static bool ContainsAllBaseColors(IReadOnlyList<PieceColor> colors)
        {
            var seen = new HashSet<PieceColor>();
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] != PieceColor.Joker)
                {
                    seen.Add(colors[i]);
                }
            }
            return seen.Count >= PieceColorUtility.BaseColors.Count;
        }

        /// <summary>Exactly 2 distinct colors present, AND every adjacent pair differs (ABAB...). A joker anywhere breaks the strict adjacency check, so it never qualifies.</summary>
        private static bool IsAlternatingTwoColors(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            if (new HashSet<PieceColor>(colors).Count != 2)
            {
                return false;
            }
            for (int i = 1; i < colors.Count; i++)
            {
                if (colors[i] == colors[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPalindrome(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 0, j = colors.Count - 1; i < j; i++, j--)
            {
                if (colors[i] != colors[j])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>No two ADJACENT cells share a color — a weaker, more general condition than <see cref="IsAlternatingTwoColors"/> (which additionally caps the line at exactly 2 distinct colors), so a 3+ color cycling line can satisfy Gradient without satisfying Alternance.</summary>
        private static bool IsGradientLine(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 1; i < colors.Count; i++)
            {
                if (colors[i] == colors[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Every cell shares its color with at least one immediate neighbor in the line — no isolated single-cell color anywhere.</summary>
        private static bool IsAllBlocksOfAtLeastTwo(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 0; i < colors.Count; i++)
            {
                bool matchesLeft = i > 0 && colors[i - 1] == colors[i];
                bool matchesRight = i < colors.Count - 1 && colors[i + 1] == colors[i];
                if (!matchesLeft && !matchesRight)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Every non-joker color in the line is the same one (jokers ignored, same convention as Puriste).</summary>
        private static bool IsMonochromeLine(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count == 0)
            {
                return false;
            }
            PieceColor? mono = null;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] == PieceColor.Joker)
                {
                    continue;
                }
                if (!mono.HasValue)
                {
                    mono = colors[i];
                }
                else if (mono.Value != colors[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Flood-fills the connected group of filled cells reachable from
        /// <paramref name="start"/> by orthogonal steps. A joker cell always
        /// joins (it has no color of its own to conflict with), but it does NOT
        /// bridge two otherwise-incompatible real colors into one group: the
        /// group anchors on a single non-joker color, and any other-colored
        /// cell — reached directly or through a joker — is excluded from then
        /// on. So green-joker-blue is two separate potential groups sharing
        /// that joker cell, never one green+joker+blue group.
        /// </summary>
        private List<Vector2Int> FindConnectedGroup(Vector2Int start)
        {
            var startColor = _cells[start.x, start.y].FilledColor.Value;
            var seed = new List<Vector2Int> { start };
            if (startColor != PieceColor.Joker)
            {
                return FloodFillGroup(seed, startColor);
            }
            return ResolveBestJokerGroup(seed);
        }

        /// <summary>
        /// Read-only preview of the connected group a placement WOULD produce
        /// at (anchorX, anchorY) — this piece's own cells plus every
        /// already-filled same-color cell connected to them, using the exact
        /// same flood-fill/joker rules as an actual placement's own group
        /// bonus (see <see cref="FindConnectedGroup"/>/<see cref="TryVisitGroupNeighbor"/>),
        /// but without mutating any grid state. Lets the presentation layer
        /// highlight the full prospective group while the player is still
        /// choosing where to drop a piece, not just the piece's own
        /// footprint. Assumes the placement is valid (<see cref="CanPlace"/>)
        /// — callers should check that first, same as <see cref="PlacePiece"/>.
        /// </summary>
        public List<Vector2Int> PreviewGroup(PieceShape shape, PieceColor color, int anchorX, int anchorY)
        {
            var offsets = shape.Cells;
            var footprint = new List<Vector2Int>(offsets.Count);
            // Seeds the flood-fill with the piece's own cells as if they were
            // already filled with `color` — mirrors PlacePiece, which marks
            // them filled in _cells BEFORE calling FindConnectedGroup from
            // one of them.
            for (int i = 0; i < offsets.Count; i++)
            {
                footprint.Add(new Vector2Int(anchorX + offsets[i].x, anchorY + offsets[i].y));
            }

            if (color != PieceColor.Joker)
            {
                return FloodFillGroup(footprint, color);
            }
            return ResolveBestJokerGroup(footprint);
        }

        /// <summary>Same as <see cref="PreviewGroup"/>, but returns the resulting group's own estimated score (see <see cref="EstimateGroupScore"/>) instead of its cells — lets a caller outside GridManager (RunManager.ResolveChameleonColor) compare hypothetical placement colors without needing the private scoring helper itself exposed.</summary>
        public int PreviewGroupScore(PieceShape shape, PieceColor color, int anchorX, int anchorY)
        {
            return EstimateGroupScore(PreviewGroup(shape, color, anchorX, anchorY));
        }

        /// <summary>
        /// A joker piece's own cells have no fixed color of their own, so
        /// they can potentially anchor the resulting group on ANY distinct
        /// real color reachable through them (through other jokers too — see
        /// <see cref="FindCandidateAnchorColors"/>). On explicit request
        /// ("lorsqu'un joker est posé, il devrait être jumelé avec le groupe
        /// faisant le plus de points"), tries every such candidate color and
        /// keeps whichever resulting group scores the most (see <see
        /// cref="EstimateGroupScore"/>), instead of whichever one a fixed
        /// traversal order happened to reach first. No real-colored neighbor
        /// anywhere reachable just returns the connected cluster of joker
        /// cells itself, same as before.
        /// </summary>
        private List<Vector2Int> ResolveBestJokerGroup(List<Vector2Int> seedCells)
        {
            var candidateColors = FindCandidateAnchorColors(seedCells);
            if (candidateColors.Count == 0)
            {
                return FloodFillGroup(seedCells, null);
            }

            List<Vector2Int> bestGroup = null;
            int bestScore = -1;
            for (int i = 0; i < candidateColors.Count; i++)
            {
                var group = FloodFillGroup(seedCells, candidateColors[i]);
                int score = EstimateGroupScore(group);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGroup = group;
                }
            }
            return bestGroup;
        }

        /// <summary>Every distinct real color orthogonally reachable from <paramref name="seedCells"/> through joker cells only (never through a real-colored cell, which would already anchor its own separate group) — the set of colors a joker placement could pick as its anchor.</summary>
        private List<PieceColor> FindCandidateAnchorColors(List<Vector2Int> seedCells)
        {
            var jokerCluster = new HashSet<Vector2Int>(seedCells);
            var stack = new Stack<Vector2Int>(seedCells);
            var candidates = new List<PieceColor>();
            var seenColors = new HashSet<PieceColor>();

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                CollectJokerNeighbor(pos.x - 1, pos.y, jokerCluster, stack, candidates, seenColors);
                CollectJokerNeighbor(pos.x + 1, pos.y, jokerCluster, stack, candidates, seenColors);
                CollectJokerNeighbor(pos.x, pos.y - 1, jokerCluster, stack, candidates, seenColors);
                CollectJokerNeighbor(pos.x, pos.y + 1, jokerCluster, stack, candidates, seenColors);
            }

            return candidates;
        }

        private void CollectJokerNeighbor(int x, int y, HashSet<Vector2Int> jokerCluster, Stack<Vector2Int> stack, List<PieceColor> candidates, HashSet<PieceColor> seenColors)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            var pos = new Vector2Int(x, y);
            if (jokerCluster.Contains(pos))
            {
                return;
            }

            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return;
            }

            if (cell.FilledColor.Value == PieceColor.Joker)
            {
                jokerCluster.Add(pos);
                stack.Push(pos);
                return;
            }

            if (seenColors.Add(cell.FilledColor.Value))
            {
                candidates.Add(cell.FilledColor.Value);
            }
        }

        /// <summary>(groupBonus + goldenBonus) * groupMultiplier for a hypothetical group — same formula PlacePiece uses for its own PlacementResult, reused to compare candidate anchor colors (see <see cref="ResolveBestJokerGroup"/>).</summary>
        private int EstimateGroupScore(List<Vector2Int> groupCells)
        {
            int score = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                score += (i + 1) * ScoringConstants.GroupBonusPerCell;
                if (_cells[groupCells[i].x, groupCells[i].y].IsGolden)
                {
                    score += ScoringConstants.GoldenCellBonus;
                }
            }
            return score * ComputeGroupMultiplier(groupCells);
        }

        private List<Vector2Int> FloodFillGroup(List<Vector2Int> seedCells, PieceColor? anchorColor)
        {
            var visited = new HashSet<Vector2Int>(seedCells);
            var stack = new Stack<Vector2Int>(seedCells);
            var group = new List<Vector2Int>();

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                group.Add(pos);

                TryVisitGroupNeighbor(pos.x - 1, pos.y, ref anchorColor, visited, stack);
                TryVisitGroupNeighbor(pos.x + 1, pos.y, ref anchorColor, visited, stack);
                TryVisitGroupNeighbor(pos.x, pos.y - 1, ref anchorColor, visited, stack);
                TryVisitGroupNeighbor(pos.x, pos.y + 1, ref anchorColor, visited, stack);
            }

            return group;
        }

        private void TryVisitGroupNeighbor(int x, int y, ref PieceColor? anchorColor, HashSet<Vector2Int> visited, Stack<Vector2Int> stack)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            var pos = new Vector2Int(x, y);
            if (visited.Contains(pos))
            {
                return;
            }

            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return;
            }

            var neighborColor = cell.FilledColor.Value;
            if (neighborColor != PieceColor.Joker)
            {
                if (anchorColor.HasValue && anchorColor.Value != neighborColor)
                {
                    // A real color that conflicts with the group's already-
                    // established color — even if reached via a joker — never
                    // joins.
                    return;
                }
                anchorColor = neighborColor;
            }

            visited.Add(pos);
            stack.Push(pos);
        }

        /// <summary>
        /// Aggregate multiplier from this placement's tinted/multiplier-zone
        /// cells — applied ONCE to this whole placement's group bonus +
        /// golden bonus (see PlacementResult.GroupMultiplier/.TotalScore)
        /// rather than baked into the group bonus per cell (Balatro-style
        /// "multiply at the end", explicit request). Each matching tinted
        /// cell AND each multiplier-zone cell in the group stacks its own x2
        /// (two of either in the same combo combine to x4, three to x8,
        /// ...) — multiplier-zone used to only count once regardless of how
        /// many cells had it, but that made "Multiplier Beacon" (which can
        /// tag many cells in one row/column at once) pointless beyond a
        /// single x2, identical to the plain single-cell Multiplier trait.
        /// Stacking it the same way Tinted already does gives Beacon real
        /// extra teeth when several of its marked cells land in the same
        /// scored group, and makes both factors consistent with each other.
        /// Does NOT reach the line-clear bonus — see
        /// <see cref="ComputeLineClearMultiplier"/> for that, which is the
        /// one place Tinted and Multiplier Zone now actually differ.
        /// </summary>
        private int ComputeGroupMultiplier(List<Vector2Int> groupCells)
        {
            int multiplier = 1;

            for (int i = 0; i < groupCells.Count; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsTinted && cell.FilledColor.HasValue && cell.FilledColor.Value == cell.TintedColor)
                {
                    multiplier *= ScoringConstants.TintedMatchMultiplier;
                }
                if (cell.IsMultiplierZone)
                {
                    multiplier *= ScoringConstants.MultiplierZoneMultiplier;
                }
            }

            return multiplier;
        }

        /// <summary>
        /// Same idea as <see cref="ComputeGroupMultiplier"/>, but counts ONLY
        /// multiplier-zone cells — Tinted deliberately never reaches
        /// <see cref="PlacementResult.LineClearScore"/> (see
        /// <see cref="PlacementResult.LineClearMultiplier"/>). On explicit
        /// player feedback: once Tinted's target color always matched its
        /// own piece (see DeckManager.TagTintedTokensRandom), it became
        /// functionally identical to Multiplier Zone despite being the
        /// cheaper Common-rarity pick — this is what keeps it a real but
        /// narrower effect instead of a strictly-better duplicate.
        /// </summary>
        private int ComputeLineClearMultiplier(List<Vector2Int> groupCells)
        {
            int multiplier = 1;

            for (int i = 0; i < groupCells.Count; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsMultiplierZone)
                {
                    multiplier *= ScoringConstants.MultiplierZoneMultiplier;
                }
            }

            return multiplier;
        }

        /// <summary>One cleared row/column's ordered color sequence (only its non-locked cells), captured right before it's wiped — feeds the 8 line-pattern modifiers (Arc-en-ciel/Alternance/Symétrie/Palindrome/Gradient/Sans doublon/Bloc/Monochrome-ligne).</summary>
        private readonly struct ClearedLine
        {
            public readonly IReadOnlyList<PieceColor> Colors;
            public readonly bool IsRow;

            /// <summary>y for a row, x for a column.</summary>
            public readonly int Index;

            public ClearedLine(IReadOnlyList<PieceColor> colors, bool isRow, int index)
            {
                Colors = colors;
                IsRow = isRow;
                Index = index;
            }
        }

        private readonly struct ClearInfo
        {
            public readonly IReadOnlyList<Vector2Int> ClearedCells;

            /// <summary>Each cleared cell's color as it was right before clearing, parallel to <see cref="ClearedCells"/> — the presentation layer needs this to keep rendering a completed line as still-filled while it holds before clearing.</summary>
            public readonly IReadOnlyList<PieceColor> ClearedCellColors;

            /// <summary>Each cleared cell's <see cref="Cell.OriginTrait"/> as it was right before clearing (null where there wasn't one), parallel to <see cref="ClearedCells"/> — same held-until-clear purpose as <see cref="ClearedCellColors"/>.</summary>
            public readonly IReadOnlyList<PieceTrait?> ClearedCellTraits;

            /// <summary>How many individual rows/columns completed simultaneously by this placement (distinct from <see cref="ClearedCells"/>.Count, which is a cell count) — used by Démolisseur.</summary>
            public readonly int ClearedLineCount;

            /// <summary>One entry per completed row/column this placement, each with its own pre-clear color sequence — used by the 8 line-pattern modifiers.</summary>
            public readonly IReadOnlyList<ClearedLine> ClearedLines;

            /// <summary>
            /// Any Bastion-tile cell (Cell.IsBastion) that sat inside a row/column
            /// completed this placement — unlike <see cref="ClearedCells"/>, these
            /// are never actually emptied (they're locked, so the clearing loop
            /// skips them, see <see cref="CheckAndClearLines"/>), but they still
            /// earn the line-clear bonus for the line they were part of.
            /// </summary>
            public readonly IReadOnlyList<Vector2Int> BastionBonusCells;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells, IReadOnlyList<PieceColor> clearedCellColors, IReadOnlyList<PieceTrait?> clearedCellTraits, int clearedLineCount, IReadOnlyList<ClearedLine> clearedLines, IReadOnlyList<Vector2Int> bastionBonusCells)
            {
                ClearedCells = clearedCells;
                ClearedCellColors = clearedCellColors;
                ClearedCellTraits = clearedCellTraits;
                ClearedLineCount = clearedLineCount;
                ClearedLines = clearedLines;
                BastionBonusCells = bastionBonusCells;
            }
        }

        /// <summary>
        /// A row/column is complete if every non-locked cell in it is filled
        /// (spec 3.3). A row/column with zero non-locked cells is degenerate and
        /// never counts as complete.
        /// </summary>
        private ClearInfo CheckAndClearLines()
        {
            var cellsToClear = new HashSet<Vector2Int>();
            var bastionBonus = new HashSet<Vector2Int>();
            int clearedLineCount = 0;
            var clearedLines = new List<ClearedLine>();

            for (int y = 0; y < Size; y++)
            {
                if (IsRowComplete(y))
                {
                    clearedLineCount++;
                    clearedLines.Add(new ClearedLine(ExtractLineColors(isRow: true, index: y), isRow: true, index: y));
                    for (int x = 0; x < Size; x++)
                    {
                        CollectLineCell(new Vector2Int(x, y), cellsToClear, bastionBonus);
                    }
                }
            }

            for (int x = 0; x < Size; x++)
            {
                if (IsColumnComplete(x))
                {
                    clearedLineCount++;
                    clearedLines.Add(new ClearedLine(ExtractLineColors(isRow: false, index: x), isRow: false, index: x));
                    for (int y = 0; y < Size; y++)
                    {
                        CollectLineCell(new Vector2Int(x, y), cellsToClear, bastionBonus);
                    }
                }
            }

            var cleared = new List<Vector2Int>(cellsToClear.Count);
            var clearedColors = new List<PieceColor>(cellsToClear.Count);
            var clearedTraits = new List<PieceTrait?>(cellsToClear.Count);
            foreach (var pos in cellsToClear)
            {
                var cell = _cells[pos.x, pos.y];
                clearedColors.Add(cell.FilledColor.Value); // capture before clearing
                clearedTraits.Add(cell.OriginTrait); // capture before clearing
                cell.ClearFill();
                cleared.Add(pos);
            }

            return new ClearInfo(cleared, clearedColors, clearedTraits, clearedLineCount, clearedLines, new List<Vector2Int>(bastionBonus));
        }

        /// <summary>
        /// One cell of a row/column just found complete: an unlocked cell is
        /// wiped as normal (added to <paramref name="cellsToClear"/>), but a
        /// locked Bastion cell (Cell.IsBastion — "n'est pas cleared mais fait
        /// quand même les points cleared") is never added there — it stays
        /// filled/locked exactly as it was — and instead earns its line-clear
        /// bonus through <paramref name="bastionBonus"/>. Both are HashSets so
        /// a cell shared by a completed row AND a completed column in the same
        /// placement is only ever credited once.
        /// </summary>
        private void CollectLineCell(Vector2Int pos, HashSet<Vector2Int> cellsToClear, HashSet<Vector2Int> bastionBonus)
        {
            var cell = _cells[pos.x, pos.y];
            if (!cell.IsLocked)
            {
                cellsToClear.Add(pos);
            }
            else if (cell.IsBastion)
            {
                bastionBonus.Add(pos);
            }
        }

        /// <summary>Ordered colors along a row (index = y) or column (index = x), skipping locked cells entirely — called before any clearing happens this call, so every relevant cell here is still filled.</summary>
        private List<PieceColor> ExtractLineColors(bool isRow, int index)
        {
            var colors = new List<PieceColor>(Size);
            for (int i = 0; i < Size; i++)
            {
                var cell = isRow ? _cells[i, index] : _cells[index, i];
                if (!cell.IsLocked && cell.FilledColor.HasValue)
                {
                    colors.Add(cell.FilledColor.Value);
                }
            }
            return colors;
        }

        private bool IsRowComplete(int y)
        {
            bool hasUnlockedCell = false;
            for (int x = 0; x < Size; x++)
            {
                var cell = _cells[x, y];
                if (cell.IsLocked)
                {
                    continue;
                }
                hasUnlockedCell = true;
                if (!cell.IsFilled)
                {
                    return false;
                }
            }
            return hasUnlockedCell;
        }

        private bool IsColumnComplete(int x)
        {
            bool hasUnlockedCell = false;
            for (int y = 0; y < Size; y++)
            {
                var cell = _cells[x, y];
                if (cell.IsLocked)
                {
                    continue;
                }
                hasUnlockedCell = true;
                if (!cell.IsFilled)
                {
                    return false;
                }
            }
            return hasUnlockedCell;
        }

        // ---- Persistent modifiers (boss round only, spec 6.1 — golden/tinted/
        // multiplier are no longer applied to fixed grid cells; see PieceTrait) ----

        /// <summary>
        /// Locks up to <paramref name="count"/> random cells for the boss round,
        /// avoiding cells that already carry a golden/tinted/multiplier modifier
        /// when possible (spec 6.1).
        /// </summary>
        public IReadOnlyList<Vector2Int> LockRandomCells(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled && !cell.HasAnyModifier)
                {
                    candidates.Add(pos);
                }
            }

            if (candidates.Count < count)
            {
                // Fallback: not enough plain cells, allow modified (but still
                // unlocked and unfilled) ones too rather than under-delivering
                // the boss effect.
                foreach (var pos in AllPositions())
                {
                    var cell = GetCell(pos);
                    if (!cell.IsLocked && !cell.IsFilled && cell.HasAnyModifier)
                    {
                        candidates.Add(pos);
                    }
                }
            }

            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsLocked = true;
            }
            return chosen;
        }

        /// <summary>
        /// Boss round mechanic (replaces the old upfront-N-cell lock at round
        /// start — judged too hard on explicit request: "le boss est beaucoup
        /// trop difficile, on va faire autre chose"): locks up to
        /// <paramref name="count"/> random still-EMPTY, unlocked cells — never
        /// a cell the player has actually filled — ratcheting the board's
        /// playable area down gradually instead of all at once. Locking a cell
        /// can itself complete a row/column (if every OTHER cell in it was
        /// already filled or locked) — that's re-validated here via the same
        /// <see cref="CheckAndClearLines"/> a real placement uses, so it scores
        /// and clears exactly the same way ("il va falloir valider pour clear
        /// line si jamais ça permet de clear line").
        /// </summary>
        public BossLockOutcome LockFreeCellsAndCheckClears(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled)
                {
                    candidates.Add(pos);
                }
            }

            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsLocked = true;
            }

            var outcome = new BossLockOutcome();
            outcome.LockedCells = chosen;
            if (chosen.Count == 0)
            {
                return outcome;
            }

            var clearInfo = CheckAndClearLines();
            outcome.ClearedCells = clearInfo.ClearedCells;
            outcome.ClearedCellColors = clearInfo.ClearedCellColors;
            outcome.LineClearScore = (clearInfo.ClearedCells.Count + clearInfo.BastionBonusCells.Count) * ScoringConstants.LineClearBonusPerCell;
            outcome.LueurEarned = SumLueur(ComputeLueurGroups(clearInfo.ClearedLines));

            if (clearInfo.ClearedCells.Count > 0)
            {
                // Same streak this placement's own clear would update — a boss
                // lock completing a line is still a line clear as far as
                // "Spark Tile"/"Rafale" are concerned.
                _placementsSinceLastClear = 0;
            }

            var events = new List<ScoreEvent>(clearInfo.ClearedCells.Count + clearInfo.BastionBonusCells.Count);
            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], ScoringConstants.LineClearBonusPerCell));
            }
            for (int i = 0; i < clearInfo.BastionBonusCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Bastion, clearInfo.BastionBonusCells[i], ScoringConstants.LineClearBonusPerCell));
            }
            outcome.ScoreEvents = events;

            return outcome;
        }

        /// <summary>
        /// Clears one random already-filled, unlocked cell not in
        /// <paramref name="exclude"/> — used by the "Void Tile" piece trait
        /// (RunManager applies it AFTER this placement's own PlacePiece call
        /// returns, excluding that placement's own cells, so Void never
        /// erases the very cells it just scored). Returns the cleared
        /// position, or null if nothing else on the grid was eligible.
        /// </summary>
        public Vector2Int? ClearRandomFilledCell(IRandomProvider rng, IReadOnlyList<Vector2Int> exclude)
        {
            var excludeSet = new HashSet<Vector2Int>(exclude);
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (cell.IsFilled && !cell.IsLocked && !excludeSet.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var chosen = candidates[rng.Next(candidates.Count)];
            var chosenCell = GetCell(chosen);
            chosenCell.ClearFill();
            return chosen;
        }

        private static List<Vector2Int> PickN(List<Vector2Int> candidates, int n, IRandomProvider rng)
        {
            var pool = new List<Vector2Int>(candidates);
            var result = new List<Vector2Int>();
            int take = Mathf.Min(n, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
