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
            foreach (var pos in cellsToClear)
            {
                var cell = _cells[pos.x, pos.y];
                clearedColors.Add(cell.FilledColor ?? default(PieceColor)); // capture before clearing — a Frozen cell going away has no fill, keep the lists parallel
                clearedShapes.Add(cell.FilledShapeId); // capture before clearing — nullable, see ClearedCellShapes' own doc comment
                clearedTraits.Add(cell.OriginTrait); // capture before clearing
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

            return new ClearInfo(cleared, clearedColors, clearedShapes, clearedTraits, clearedLineCount, clearedLines, new List<Vector2Int>(bastionBonus), lineAmounts, gridEmptied);
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
            else if (cell.IsFrozen)
            {
                // Unlike every other locked cell this line's completion skips over (Bastion preserves
                // itself, an ordinary ProgressiveCellLock/Locker obstacle just stays put), a Frozen cell is
                // meant to actually go away the moment its line completes — its one and only removal path
                // (see Cell.IsFrozen's own doc comment).
                cell.IsLocked = false;
                cell.IsFrozen = false;
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
            bool hasUnlockedCell = false;
            for (int x = 0; x < Size; x++)
            {
                var cell = _cells[x, y];
                // A Locker obstacle cell is not skipped like an ordinary
                // locked cell: it falls through to the IsFilled check below,
                // which it can never pass (CanPlace already refuses it), so
                // a row/column containing one can never complete.
                if (cell.IsLocked && !cell.IsLineClearObstacle)
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
                if (cell.IsLocked && !cell.IsLineClearObstacle)
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
    }
}
