using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Detecting and clearing completed rows/columns and the scoring amounts attached to them.</summary>
    public sealed partial class GridManager
    {
        /// <summary>A row/column is complete if every non-locked cell in it is filled. A row/column with zero non-locked cells is degenerate and never counts as complete.</summary>
        private static int SumLineAmounts(ClearInfo clearInfo)
        {
            int total = 0;
            foreach (var pair in clearInfo.LineAmounts)
            {
                total += pair.Value;
            }
            return total;
        }

        private ClearInfo CheckAndClearLines()
        {
            var cellsToClear = new HashSet<Vector2Int>();
            var bastionBonus = new HashSet<Vector2Int>();
            var frozenCleared = new HashSet<Vector2Int>();
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
                        CollectLineCell(new Vector2Int(x, y), cellsToClear, bastionBonus, frozenCleared);
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
                        CollectLineCell(new Vector2Int(x, y), cellsToClear, bastionBonus, frozenCleared);
                    }
                }
            }

            // Escalating per-cell value by line order (rows first, then columns); a cell shared by two lines keeps the first line's value.
            var lineAmounts = new Dictionary<Vector2Int, int>();
            for (int k = 0; k < clearedLines.Count; k++)
            {
                var line = clearedLines[k];
                for (int i = 0; i < Size; i++)
                {
                    var pos = line.IsRow ? new Vector2Int(i, line.Index) : new Vector2Int(line.Index, i);
                    if ((cellsToClear.Contains(pos) || bastionBonus.Contains(pos)) && !lineAmounts.ContainsKey(pos))
                    {
                        lineAmounts[pos] = ScoringConstants.LineClearBonusPerCell * (k + 1);
                    }
                }
            }

            var cleared = new List<Vector2Int>(cellsToClear.Count);
            var clearedColors = new List<PieceColor>(cellsToClear.Count);
            var clearedShapes = new List<ShapeId?>(cellsToClear.Count);
            var clearedTraits = new List<PieceTrait?>(cellsToClear.Count);
            var clearedFrozen = new List<bool>(cellsToClear.Count);
            foreach (var pos in cellsToClear)
            {
                var cell = _cells[pos.x, pos.y];
                clearedColors.Add(cell.FilledColor ?? default(PieceColor)); // capture before clearing — a Frozen cell going away has no fill, keep the lists parallel
                clearedShapes.Add(cell.FilledShapeId); // capture before clearing — nullable, see ClearedCellShapes' own doc comment
                clearedTraits.Add(cell.OriginTrait); // capture before clearing
                clearedFrozen.Add(frozenCleared.Contains(pos));
                cell.ClearFill();
                cleared.Add(pos);
            }

            bool gridEmptied = cleared.Count > 0;
            for (int x = 0; x < Size && gridEmptied; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    if (_cells[x, y].IsFilled)
                    {
                        gridEmptied = false;
                        break;
                    }
                }
            }

            return new ClearInfo(cleared, clearedColors, clearedShapes, clearedTraits, clearedLineCount, clearedLines, new List<Vector2Int>(bastionBonus), lineAmounts, gridEmptied, clearedFrozen);
        }

        /// <summary>
        /// One cell of a row/column just found complete: an unlocked cell is
        /// wiped as normal (added to <paramref name="cellsToClear"/>), but a
        /// locked Bastion cell (Cell.IsBastion) stays filled/locked and
        /// instead earns its line-clear bonus through <paramref
        /// name="bastionBonus"/>. Both are HashSets so a cell shared by a
        /// completed row and column in the same placement is only ever
        /// credited once.
        /// </summary>
        private void CollectLineCell(Vector2Int pos, HashSet<Vector2Int> cellsToClear, HashSet<Vector2Int> bastionBonus, HashSet<Vector2Int> frozenCleared)
        {
            var cell = _cells[pos.x, pos.y];
            if (!cell.IsFilled && !cell.IsLocked)
            {
                // The empty plot a Gap Sprout forgave: nothing to harvest there.
                return;
            }
            if (!cell.IsLocked && cell.OriginTrait.HasValue && cell.OriginTrait.Value.Kind == PieceTraitKind.Evergreen)
            {
                // Evergreen Sprout: the tile stays planted and still earns the line points, like a Bastion tile.
                bastionBonus.Add(pos);
            }
            else if (!cell.IsLocked)
            {
                cellsToClear.Add(pos);
            }
            else if (cell.IsBastion)
            {
                bastionBonus.Add(pos);
            }
            else if (cell.IsFrozen)
            {
                // Unlike every other locked cell this line's completion skips over (Bastion preserves
                // itself, an ordinary ProgressiveCellLock/Locker obstacle just stays put), a Frozen cell is
                // meant to actually go away the moment its line completes — its one and only removal path
                // (see Cell.IsFrozen's own doc comment).
                cell.IsLocked = false;
                cell.IsFrozen = false;
                frozenCleared.Add(pos);
                cellsToClear.Add(pos);
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
            return IsLineComplete(true, y, null);
        }

        private bool IsColumnComplete(int x)
        {
            return IsLineComplete(false, x, null);
        }

        /// <summary>
        /// The one "is this row/column complete" rule: every unlocked cell filled (a Locker obstacle cell is never skipped, see
        /// Cell.IsLineClearObstacle), at least one unlocked cell — except that each Gap Sprout tile in the line forgives one empty plot.
        /// With a <paramref name="footprint"/>, those cells count as filled (the hypothetical placement PreviewClearedLineCells checks).
        /// </summary>
        private bool IsLineComplete(bool isRow, int index, HashSet<Vector2Int> footprint)
        {
            bool hasUnlockedCell = false;
            int empties = 0;
            int gapTiles = 0;
            for (int i = 0; i < Size; i++)
            {
                var pos = isRow ? new Vector2Int(i, index) : new Vector2Int(index, i);
                var cell = _cells[pos.x, pos.y];
                if (cell.IsLocked && !cell.IsLineClearObstacle)
                {
                    continue;
                }
                hasUnlockedCell = true;
                bool filled = cell.IsFilled || (footprint != null && footprint.Contains(pos));
                if (!filled)
                {
                    if (cell.IsLocked)
                    {
                        return false;
                    }
                    empties++;
                }
                else if (cell.IsFilled && cell.OriginTrait.HasValue && cell.OriginTrait.Value.Kind == PieceTraitKind.Gap)
                {
                    gapTiles++;
                }
            }
            return hasUnlockedCell && empties <= gapTiles;
        }
    }
}
