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
        /// Places a piece, applying the connected-group bonus, golden bonus, and
        /// any resulting line/column clears. Assumes the caller already validated
        /// the placement (or will inspect the returned failure).
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

            // Group bonus: the whole connected same-color group this placement
            // touches is rescored in full — every cell in the merged group
            // contributes again, not just the newly placed ones, like replaying
            // an extended Scrabble word. All of a piece's own cells are always
            // mutually connected (every shape in the catalog is edge-connected),
            // so a single flood-fill from any placed cell finds the whole group.
            var groupCells = FindConnectedGroup(placedCells[0]);
            int groupMultiplier = ComputeGroupMultiplier(groupCells);
            int perCellGroupScore = ScoringConstants.GroupBonusPerCell * groupMultiplier;
            int groupBonus = 0;
            int goldenBonus = 0;

            for (int i = 0; i < groupCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Group, groupCells[i], perCellGroupScore));
                groupBonus += perCellGroupScore;

                // Golden fires every time the cell is part of a scored group —
                // not just when it was originally placed — since re-touching a
                // group rescores every cell in it, golden included. Still a flat
                // bonus, independent of group size or the group multiplier.
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsGolden)
                {
                    goldenBonus += ScoringConstants.GoldenCellBonus;
                    events.Add(new ScoreEvent(ScoreEventType.Golden, groupCells[i], ScoringConstants.GoldenCellBonus));
                }
            }

            result.GroupBonus = groupBonus;
            result.GoldenBonus = goldenBonus;

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.ClearedCellColors = clearInfo.ClearedCellColors;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            result.LineClearScore = clearInfo.ClearedCells.Count * ScoringConstants.LineClearBonusPerCell;

            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], ScoringConstants.LineClearBonusPerCell));
            }

            result.ScoreEvents = events;

            return result;
        }

        /// <summary>
        /// Flood-fills the connected group of filled cells reachable from
        /// <paramref name="start"/> by orthogonal steps where each consecutive
        /// pair's colors match (<see cref="PieceColorUtility.Matches"/>, joker
        /// included), transitively — so a joker can bridge two different colors
        /// into one group.
        /// </summary>
        private List<Vector2Int> FindConnectedGroup(Vector2Int start)
        {
            var visited = new HashSet<Vector2Int> { start };
            var stack = new Stack<Vector2Int>();
            stack.Push(start);
            var group = new List<Vector2Int>();

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                group.Add(pos);
                var currentColor = _cells[pos.x, pos.y].FilledColor.Value;

                TryVisitGroupNeighbor(pos.x - 1, pos.y, currentColor, visited, stack);
                TryVisitGroupNeighbor(pos.x + 1, pos.y, currentColor, visited, stack);
                TryVisitGroupNeighbor(pos.x, pos.y - 1, currentColor, visited, stack);
                TryVisitGroupNeighbor(pos.x, pos.y + 1, currentColor, visited, stack);
            }

            return group;
        }

        private void TryVisitGroupNeighbor(int x, int y, PieceColor fromColor, HashSet<Vector2Int> visited, Stack<Vector2Int> stack)
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

            if (!PieceColorUtility.Matches(fromColor, cell.FilledColor.Value))
            {
                return;
            }

            visited.Add(pos);
            stack.Push(pos);
        }

        /// <summary>
        /// Whole-group multiplier from tinted/multiplier-zone cells. Each
        /// matching tinted cell in the group stacks its own x2 (two tinted
        /// cells in the same combo combine to x4, three to x8, ...); a
        /// multiplier-zone cell only needs to be present once (not per-
        /// occurrence) for its own x2. Both kinds of factor multiply together.
        /// </summary>
        private int ComputeGroupMultiplier(List<Vector2Int> groupCells)
        {
            int multiplier = 1;
            bool multiplierZone = false;

            for (int i = 0; i < groupCells.Count; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsTinted && cell.FilledColor.HasValue && cell.FilledColor.Value == cell.TintedColor)
                {
                    multiplier *= ScoringConstants.TintedMatchMultiplier;
                }
                if (cell.IsMultiplierZone)
                {
                    multiplierZone = true;
                }
            }

            if (multiplierZone)
            {
                multiplier *= ScoringConstants.MultiplierZoneMultiplier;
            }
            return multiplier;
        }

        private readonly struct ClearInfo
        {
            public readonly IReadOnlyList<Vector2Int> ClearedCells;

            /// <summary>Each cleared cell's color as it was right before clearing, parallel to <see cref="ClearedCells"/> — the presentation layer needs this to keep rendering a completed line as still-filled while it holds before clearing.</summary>
            public readonly IReadOnlyList<PieceColor> ClearedCellColors;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells, IReadOnlyList<PieceColor> clearedCellColors)
            {
                ClearedCells = clearedCells;
                ClearedCellColors = clearedCellColors;
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
            var clearedColors = new List<PieceColor>(cellsToClear.Count);
            foreach (var pos in cellsToClear)
            {
                var cell = _cells[pos.x, pos.y];
                clearedColors.Add(cell.FilledColor.Value); // capture before clearing
                cell.IsFilled = false;
                cell.FilledColor = null;
                cleared.Add(pos);
            }

            return new ClearInfo(cleared, clearedColors);
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
