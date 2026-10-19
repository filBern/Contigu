using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Owns the 6x6 grid state: placement validation, the connected-color-group
    /// bonus, line/column clears, and the persistent cell modifiers (golden,
    /// tinted, multiplier zone) plus the boss round's locked cells. Pure C#, no
    /// MonoBehaviour dependency, so it is unit-testable in isolation.
    /// </summary>
    public sealed partial class GridManager
    {
        public const int Size = 6;

        private readonly Cell[,] _cells;

        /// <summary>Dispatch table for <see cref="ApplyPreClearModifiers"/> — built per instance (not static) since most entries call instance ApplyXxx helpers that read <see cref="_cells"/>.</summary>
        private readonly Dictionary<ModifierId, PreClearModifierEffect> _preClearEffects;

        /// <summary>Same rationale as <see cref="_preClearEffects"/>, for <see cref="ApplyPostClearModifiers"/>.</summary>
        private readonly Dictionary<ModifierId, PostClearModifierEffect> _postClearEffects;

        /// <summary>Scored group size of the last placement made this round, or null before the round's first placement — tracked for Momentum (Dégradé), reset by <see cref="ResetForNewRound"/>.</summary>
        private int? _lastGroupSize;

        /// <summary>Consecutive placements made this round without a line/column clear, as of BEFORE the placement currently in progress — see <see cref="PlacementsSinceLastClear"/>.</summary>
        private int _placementsSinceLastClear;

        /// <summary>Shape of the last piece placed this round, or null before the round's first placement — tracked for Repetition, reset by <see cref="ResetForNewRound"/>.</summary>
        private ShapeId? _lastPlacedShapeId;

        /// <summary>How many placements in a row this round (ending at, and including, the most recent one) share the same shape — 1 means no repeat yet (either the round's first placement, or this shape differs from the previous one). Reset by <see cref="ResetForNewRound"/>.</summary>
        private int _repetitionStreak;

        /// <summary>Fill color of the last piece placed this round, or null before the round's first placement — tracked for Color Switch (Alternance des pièces), reset by <see cref="ResetForNewRound"/>.</summary>
        private PieceColor? _lastPlacedColor;

        /// <summary>
        /// Permanent run-long counter for Gradient: how many cleared
        /// rows/columns have ever satisfied its no-two-adjacent-same-color
        /// condition, across every round played so far this run. Unlike
        /// every other tracked field on this class, this one is deliberately
        /// NOT reset by <see cref="ResetForNewRound"/> — see <see cref="ApplyGradient"/>.
        /// </summary>
        private int _gradientPermanentBonus;

        /// <summary>
        /// Dwindling (Epuisement): the flat points bonus this placement's
        /// own instance of the modifier would grant right now — starts at
        /// ScoringConstants.EpuisementStartingBonus, drops by
        /// ScoringConstants.EpuisementDecayPerPlacement after every
        /// placement this modifier is held for, floored at 0. Permanent for
        /// the whole run, same as <see cref="_gradientPermanentBonus"/> —
        /// NOT reset by <see cref="ResetForNewRound"/>.
        /// </summary>
        private int _epuisementValue = ScoringConstants.EpuisementStartingBonus;

        /// <summary>
        /// How many placements in a row this round have gone by without a
        /// line/column clear, reflecting only placements already fully
        /// processed by <see cref="PlacePiece"/> — read this BEFORE calling
        /// PlacePiece for the "Spark Tile" piece trait, so a placement's own
        /// clear doesn't erase the streak it's scoring against. Reset to 0
        /// by <see cref="ResetForNewRound"/>.
        /// </summary>
        public int PlacementsSinceLastClear
        {
            get { return _placementsSinceLastClear; }
        }

        /// <summary>Gradient's current permanent multiplier (1 + <see cref="_gradientPermanentBonus"/>) — what it would apply right now if a qualifying line were cleared this instant.</summary>
        public int GradientCurrentMultiplier
        {
            get { return 1 + _gradientPermanentBonus; }
        }

        /// <summary>Preview of the xN multiplier the next placement would apply if it kept this same-shape streak alive — <see cref="_repetitionStreak"/> + 1, the streak length that next placement would reach.</summary>
        public int RepetitionCurrentMultiplier
        {
            get { return _repetitionStreak + 1; }
        }

        /// <summary>Dwindling's current flat points bonus (see <see cref="_epuisementValue"/>) — what the next placement would earn from it right now.</summary>
        public int EpuisementCurrentBonus
        {
            get { return _epuisementValue; }
        }

        /// <summary>How many cells are filled on the board right now — Density's driver (see <see cref="ApplyDensite"/>).</summary>
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
            _preClearEffects = BuildPreClearEffects();
            _postClearEffects = BuildPostClearEffects();
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

        /// <summary>Clears fill, lock and modifier state (golden/tinted/multiplier) for a new round — see Cell.ResetForNewRound.</summary>
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

        public PlacementResult PlacePiece(PieceShape shape, PieceColor color, int anchorX, int anchorY, IReadOnlyList<ModifierId> activeModifiers = null, IReadOnlyList<int> modifierLevels = null)
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
                cell.FilledShapeId = shape.Id;
                placedCells.Add(new Vector2Int(x, y));
            }

            var result = new PlacementResult();
            result.Success = true;
            result.PlacedCells = placedCells;

            var events = new List<ScoreEvent>();

            // Every cell in the merged connected group is rescored, not just the
            // newly placed ones. A piece's own cells are always mutually
            // connected (every shape in the catalog is edge-connected), so a
            // single flood-fill from any placed cell finds the whole group.
            var groupCells = FindConnectedGroup(placedCells[0]);
            result.GroupCells = groupCells;

            // Captured before being overwritten below, for Momentum (Dégradé)
            // to compare this placement's group size against the previous one.
            int? previousGroupSize = _lastGroupSize;
            _lastGroupSize = groupCells.Count;

            // Captured before this placement's own clear (if any) updates the
            // streak below — 0 here means either the immediately previous
            // placement this round cleared a line, or this is the round's
            // very first placement (previousGroupSize tells them apart) — see
            // Rafale in ApplyPostClearModifiers.
            int streakBeforePlacement = _placementsSinceLastClear;

            bool continuesShapeStreak = _lastPlacedShapeId.HasValue && _lastPlacedShapeId.Value == shape.Id;
            _repetitionStreak = continuesShapeStreak ? _repetitionStreak + 1 : 1;
            _lastPlacedShapeId = shape.Id;
            PieceColor? previousPlacedColor = _lastPlacedColor;
            _lastPlacedColor = color;

            // Tinted-match/multiplier-zone factor is applied once, at the end
            // of this whole placement (see PlacementResult.GroupMultiplier and
            // .TotalScore), not baked into each cell's own score. Every event
            // below carries its plain, unmultiplied amount.
            int groupMultiplier = ComputeGroupMultiplier(groupCells);
            // Same groupCells but only multiplier-zone cells count (see
            // ComputeLineClearMultiplier) — Tinted never reaches the line-clear
            // bonus.
            int lineClearMultiplier = ComputeLineClearMultiplier(groupCells);
            int groupBonus = 0;
            int goldenBonus = 0;
            int shapeMasteryBonus = 0;
            int colorMasteryBonus = 0;

            for (int i = 0; i < groupCells.Count; i++)
            {
                // Progressive: the Nth cell scored (1-indexed) is worth
                // N * GroupBonusPerCell.
                int cellScore = (i + 1) * ScoringConstants.GroupBonusPerCell;
                events.Add(new ScoreEvent(ScoreEventType.Group, groupCells[i], cellScore));
                groupBonus += cellScore;

                // Golden fires every time the cell is part of a scored group,
                // not just when it was originally placed, since re-touching a
                // group rescores every cell in it.
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsGolden)
                {
                    goldenBonus += ScoringConstants.GoldenCellBonus;
                    events.Add(new ScoreEvent(ScoreEventType.Golden, groupCells[i], ScoringConstants.GoldenCellBonus));
                }

                // Piece/Color Mastery: same "fires every time this cell's
                // group scores again" rule as Golden above — each cell was
                // stamped with its own frozen bonus at placement time (see
                // RunManager.PlacePiece, before this method runs).
                if (cell.ShapeMasteryBonus > 0)
                {
                    shapeMasteryBonus += cell.ShapeMasteryBonus;
                    events.Add(new ScoreEvent(ScoreEventType.ShapeMastery, groupCells[i], cell.ShapeMasteryBonus));
                }
                if (cell.ColorMasteryBonus > 0)
                {
                    colorMasteryBonus += cell.ColorMasteryBonus;
                    events.Add(new ScoreEvent(ScoreEventType.ColorMastery, groupCells[i], cell.ColorMasteryBonus));
                }
            }

            result.GroupBonus = groupBonus;
            result.GoldenBonus = goldenBonus;
            result.GroupMultiplier = groupMultiplier;
            result.LineClearMultiplier = lineClearMultiplier;
            result.ShapeMasteryBonus = shapeMasteryBonus;
            result.ColorMasteryBonus = colorMasteryBonus;

            int modifierBonus = 0;
            int modifierMultiplier = 1;
            int modifierLueurBonus = 0;
            int modifierAdditiveMultBonus = 0;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPreClearModifiers(activeModifiers, shape, groupCells, placedCells, groupBonus, events, previousGroupSize, _repetitionStreak, previousPlacedColor, out int preMultiplier, out int preLueur, out int preAdditiveMult, modifierLevels);
                modifierMultiplier *= preMultiplier;
                modifierLueurBonus += preLueur;
                modifierAdditiveMultBonus += preAdditiveMult;
            }

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.ClearedCellColors = clearInfo.ClearedCellColors;
            result.ClearedCellFrozen = clearInfo.ClearedCellFrozen;
            result.ClearedCellShapes = clearInfo.ClearedCellShapes;
            result.ClearedCellTraits = clearInfo.ClearedCellTraits;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            result.ClearedLineCount = clearInfo.ClearedLineCount;
            // Bastion cells (Cell.IsBastion) earn the same per-cell bonus as
            // an actually-cleared cell without being in ClearedCells — they're
            // never emptied (see CollectLineCell/BastionBonusCells).
            result.LineClearScore = SumLineAmounts(clearInfo);
            // Lueur is a separate currency from score, driven by color
            // diversity per cleared line (see PlacementResult.LueurEarned/LueurGroups).
            var lueurGroups = ComputeLueurGroups(clearInfo.ClearedLines);
            result.LueurGroups = lueurGroups;
            result.LueurEarned = SumLueur(lueurGroups);

            _placementsSinceLastClear = clearInfo.ClearedCells.Count > 0 ? 0 : _placementsSinceLastClear + 1;

            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], clearInfo.LineAmounts[clearInfo.ClearedCells[i]]));
            }
            for (int i = 0; i < clearInfo.BastionBonusCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Bastion, clearInfo.BastionBonusCells[i], clearInfo.LineAmounts[clearInfo.BastionBonusCells[i]]));
            }
            // Clearing the whole grid with this one placement doubles the placement's Mult, applied last (see PlacementResult.Mult's ordered fold).
            if (clearInfo.GridEmptied)
            {
                var sweepEvent = new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.GridClearMultiplier);
                sweepEvent.TriggeringModifierIndex = int.MaxValue;
                events.Add(sweepEvent);
                modifierMultiplier *= ScoringConstants.GridClearMultiplier;
            }

            // Rafale: did the immediately previous placement this round also
            // clear a line? previousGroupSize.HasValue rules out the round's
            // very first placement, which would otherwise look identical
            // (streak also starts at 0).
            bool clearedByPreviousPlacement = previousGroupSize.HasValue && streakBeforePlacement == 0;

            float modifierProgressiveAdditiveMult = 0f;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPostClearModifiers(activeModifiers, clearInfo, placedCells, clearedByPreviousPlacement, events, out int postMultiplier, out int postLueur, out float postProgressiveAdditiveMult, out int postAdditiveMult, modifierLevels);
                modifierMultiplier *= postMultiplier;
                modifierLueurBonus += postLueur;
                modifierProgressiveAdditiveMult += postProgressiveAdditiveMult;
                modifierAdditiveMultBonus += postAdditiveMult;
            }

            result.ModifierBonus = modifierBonus;
            result.ModifierMultiplier = modifierMultiplier;
            result.ModifierLueurBonus = modifierLueurBonus;
            result.AdditiveMultBonus = modifierAdditiveMultBonus;
            result.ProgressiveAdditiveMult += modifierProgressiveAdditiveMult;
            // Combo reuses the same "did the previous placement clear?"
            // signal as Rafale, but multiplies the whole placement's total
            // (see PlacementResult.ComboMultiplier/.TotalScore).
            result.ComboMultiplier = ComputeComboMultiplier(activeModifiers, clearedByPreviousPlacement, placedCells, events);
            result.ScoreEvents = events;

            return result;
        }

        /// <summary>Every distinct non-Joker color within each line this placement cleared becomes its own <see cref="LueurGroup"/> (every cell of that color in the line, whether or not they're actually adjacent) worth EconomyConstants.LueurPerColorGroup.</summary>
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
                // isn't guaranteed) so the reveal is consistent between clears.
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

        /// <summary>Stacks per copy of Combo held — 1 (no-op) unless the previous placement this round cleared a line. Emits its own ModifierMultiplier event tagged with its position in <paramref name="activeModifiers"/> so <see cref="PlacementResult.Mult"/>'s ordered fold applies Combo at the right position relative to other Mult modifiers, rather than always last.</summary>
        private static int ComputeComboMultiplier(IReadOnlyList<ModifierId> activeModifiers, bool clearedByPreviousPlacement, List<Vector2Int> placedCells, List<ScoreEvent> events)
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
                    var comboEvent = new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ComboMultiplierFactor);
                    comboEvent.TriggeringModifier = ModifierId.Combo;
                    comboEvent.TriggeringModifierIndex = i;
                    events.Add(comboEvent);
                }
            }
            return multiplier;
        }

        /// <summary>
        /// Everything a pre-clear modifier's effect delegate (see <see
        /// cref="_preClearEffects"/>) might need to read, plus the
        /// accumulators it mutates as a side effect (Multiplier/Lueur/
        /// AdditiveMult — <see cref="ApplyPreClearModifiers"/> reads these
        /// back out once the whole dispatch loop finishes). One instance is
        /// built per placement and reused across every active modifier.
        /// </summary>
        private sealed class PreClearModifierContext
        {
            public PieceShape Shape;
            public List<Vector2Int> GroupCells;
            public List<Vector2Int> PlacedCells;
            public List<ScoreEvent> Events;
            public int? PreviousGroupSize;
            public int RepetitionStreak;
            public PieceColor? PreviousPlacedColor;
            public PieceColor OwnColor;
            public PieceColor JokerResolvedColor;
            public int ActiveModifierCount;
            public IReadOnlyList<ModifierId> ActiveModifiers;

            /// <summary>This modifier's own position in <see cref="ActiveModifiers"/> for the iteration currently invoking an effect delegate — set fresh before each call so a modifier like Écho can look at its neighbors (see <see cref="ApplyEcho"/>).</summary>
            public int CurrentIndex;
            public int Multiplier = 1;
            public int Lueur;
            public int AdditiveMult;
        }
    }
}
