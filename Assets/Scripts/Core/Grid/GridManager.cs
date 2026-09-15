using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Owns the 8x8 grid state: placement validation, the immediate neighbor-color
    /// bonus, line/column clears, and the persistent cell modifiers (golden,
    /// tinted, multiplier zone) plus the boss round's locked cells. Pure C#, no
    /// MonoBehaviour dependency, so it is unit-testable in isolation (spec section 8).
    /// </summary>
    public sealed class GridManager
    {
        public const int Size = 8;

        private readonly Cell[,] _cells;

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
        /// Clears fill and lock state for a new round. Modifiers (golden/tinted/
        /// multiplier) are NOT touched here since the same Cell instances persist
        /// across rounds for the whole run (spec section 2).
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
        /// Places a piece, applying neighbor bonuses, golden bonus, and any
        /// resulting line/column clears. Assumes the caller already validated the
        /// placement (or will inspect the returned failure).
        /// </summary>
        public PlacementResult PlacePiece(PieceShape shape, PieceColor color, int anchorX, int anchorY)
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
            int neighborBonus = 0;
            int goldenBonus = 0;

            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                var cell = _cells[pos.x, pos.y];

                if (cell.IsGolden)
                {
                    goldenBonus += ScoringConstants.GoldenCellBonus;
                    events.Add(new ScoreEvent(ScoreEventType.Golden, pos, ScoringConstants.GoldenCellBonus));
                }

                int multiplier = 1;
                if (cell.IsTinted && cell.FilledColor.Value == cell.TintedColor)
                {
                    multiplier *= ScoringConstants.TintedMatchMultiplier;
                }
                if (cell.IsMultiplierZone)
                {
                    multiplier *= ScoringConstants.MultiplierZoneMultiplier;
                }

                int perMatchAmount = ScoringConstants.NeighborBonusPerPair * multiplier;

                // One event per matching neighbor direction, rather than a single
                // summed total, so each point addition can be shown individually.
                // Only neighbors that were ALREADY filled before this placement
                // count — sibling cells from the same piece don't score against
                // each other, so the bonus always rewards connecting to the
                // existing board rather than a piece's own internal shape.
                if (IsScorableNeighbor(pos.x - 1, pos.y, cell.FilledColor.Value, placedCells))
                {
                    events.Add(new ScoreEvent(ScoreEventType.Neighbor, pos, perMatchAmount));
                    neighborBonus += perMatchAmount;
                }
                if (IsScorableNeighbor(pos.x + 1, pos.y, cell.FilledColor.Value, placedCells))
                {
                    events.Add(new ScoreEvent(ScoreEventType.Neighbor, pos, perMatchAmount));
                    neighborBonus += perMatchAmount;
                }
                if (IsScorableNeighbor(pos.x, pos.y - 1, cell.FilledColor.Value, placedCells))
                {
                    events.Add(new ScoreEvent(ScoreEventType.Neighbor, pos, perMatchAmount));
                    neighborBonus += perMatchAmount;
                }
                if (IsScorableNeighbor(pos.x, pos.y + 1, cell.FilledColor.Value, placedCells))
                {
                    events.Add(new ScoreEvent(ScoreEventType.Neighbor, pos, perMatchAmount));
                    neighborBonus += perMatchAmount;
                }
            }

            result.NeighborBonus = neighborBonus;
            result.GoldenBonus = goldenBonus;

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            result.LineClearScore = clearInfo.ClearedCells.Count * ScoringConstants.LineClearBonusPerCell;

            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], ScoringConstants.LineClearBonusPerCell));
            }

            result.ScoreEvents = events;

            return result;
        }

        private bool IsMatchingNeighbor(int x, int y, PieceColor placedColor)
        {
            if (!InBounds(x, y))
            {
                return false;
            }

            var neighbor = _cells[x, y];
            if (!neighbor.IsFilled || !neighbor.FilledColor.HasValue)
            {
                return false;
            }

            return PieceColorUtility.Matches(placedColor, neighbor.FilledColor.Value);
        }

        /// <summary>
        /// Same as <see cref="IsMatchingNeighbor"/>, but excludes cells that are
        /// part of the SAME piece currently being placed (<paramref name="placedCells"/>)
        /// — only a color match against the board as it stood before this
        /// placement counts toward the neighbor bonus.
        /// </summary>
        private bool IsScorableNeighbor(int x, int y, PieceColor placedColor, List<Vector2Int> placedCells)
        {
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (placedCells[i].x == x && placedCells[i].y == y)
                {
                    return false;
                }
            }

            return IsMatchingNeighbor(x, y, placedColor);
        }

        private readonly struct ClearInfo
        {
            public readonly IReadOnlyList<Vector2Int> ClearedCells;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells)
            {
                ClearedCells = clearedCells;
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

            for (int y = 0; y < Size; y++)
            {
                if (IsRowComplete(y))
                {
                    for (int x = 0; x < Size; x++)
                    {
                        if (!_cells[x, y].IsLocked)
                        {
                            cellsToClear.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            for (int x = 0; x < Size; x++)
            {
                if (IsColumnComplete(x))
                {
                    for (int y = 0; y < Size; y++)
                    {
                        if (!_cells[x, y].IsLocked)
                        {
                            cellsToClear.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            var cleared = new List<Vector2Int>(cellsToClear.Count);
            foreach (var pos in cellsToClear)
            {
                var cell = _cells[pos.x, pos.y];
                cell.IsFilled = false;
                cell.FilledColor = null;
                cleared.Add(pos);
            }

            return new ClearInfo(cleared);
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

        // ---- Persistent modifiers (applied once by upgrades, spec 5.4) ----

        public IReadOnlyList<Vector2Int> ApplyGoldenCellsRandom(int count, IRandomProvider rng)
        {
            var chosen = PickRandomUnmodifiedCells(count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsGolden = true;
            }
            return chosen;
        }

        public IReadOnlyList<Vector2Int> ApplyTintedCellsRandom(int count, IRandomProvider rng)
        {
            var chosen = PickRandomUnmodifiedCells(count, rng);
            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < chosen.Count; i++)
            {
                var cell = GetCell(chosen[i]);
                cell.IsTinted = true;
                cell.TintedColor = baseColors[rng.Next(baseColors.Count)];
            }
            return chosen;
        }

        public IReadOnlyList<Vector2Int> ApplyMultiplierCellsRandom(int count, IRandomProvider rng)
        {
            var chosen = PickRandomUnmodifiedCells(count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsMultiplierZone = true;
            }
            return chosen;
        }

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
                if (!cell.IsLocked && !cell.HasAnyModifier)
                {
                    candidates.Add(pos);
                }
            }

            if (candidates.Count < count)
            {
                // Fallback: not enough plain cells, allow modified (but still
                // unlocked) ones too rather than under-delivering the boss effect.
                foreach (var pos in AllPositions())
                {
                    var cell = GetCell(pos);
                    if (!cell.IsLocked && cell.HasAnyModifier)
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

        private List<Vector2Int> PickRandomUnmodifiedCells(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.HasAnyModifier)
                {
                    candidates.Add(pos);
                }
            }
            return PickN(candidates, count, rng);
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
