using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Connected-group search, previews and Joker color resolution, group/line multipliers.</summary>
    public sealed partial class GridManager
    {
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

        /// <summary>Wildroot Sprout: each such tile in the group counts as one extra tile of it — listed once more, so the group is bigger for every rule that reads its size or walks its cells.</summary>
        private void AddWildrootCells(List<Vector2Int> groupCells)
        {
            int original = groupCells.Count;
            for (int i = 0; i < original; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.OriginTrait.HasValue && cell.OriginTrait.Value.Kind == PieceTraitKind.Wildroot)
                {
                    groupCells.Add(groupCells[i]);
                }
            }
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
        /// Every cell of every row/column that would complete (and clear) if
        /// <paramref name="shape"/> were placed at (<paramref name="anchorX"/>,
        /// <paramref name="anchorY"/>), using the same row/column-complete
        /// rule <see cref="CheckAndClearLines"/> itself uses, without
        /// mutating grid state. Assumes the placement is valid (<see
        /// cref="CanPlace"/>). A cell can appear more than once if it
        /// belongs to both a completing row and a completing column at
        /// once — callers that need a de-duplicated set should collect
        /// these into their own HashSet.
        /// </summary>
        public List<Vector2Int> PreviewClearedLineCells(PieceShape shape, int anchorX, int anchorY)
        {
            var offsets = shape.Cells;
            var footprint = new HashSet<Vector2Int>(offsets.Count);
            for (int i = 0; i < offsets.Count; i++)
            {
                footprint.Add(new Vector2Int(anchorX + offsets[i].x, anchorY + offsets[i].y));
            }

            var result = new List<Vector2Int>();
            for (int y = 0; y < Size; y++)
            {
                if (IsRowCompleteWithFootprint(y, footprint))
                {
                    for (int x = 0; x < Size; x++)
                    {
                        result.Add(new Vector2Int(x, y));
                    }
                }
            }
            for (int x = 0; x < Size; x++)
            {
                if (IsColumnCompleteWithFootprint(x, footprint))
                {
                    for (int y = 0; y < Size; y++)
                    {
                        result.Add(new Vector2Int(x, y));
                    }
                }
            }
            return result;
        }

        /// <summary>Same rule as <see cref="IsRowComplete"/>, but also treats every cell in <paramref name="footprint"/> as filled — the hypothetical placement <see cref="PreviewClearedLineCells"/> checks.</summary>
        private bool IsRowCompleteWithFootprint(int y, HashSet<Vector2Int> footprint)
        {
            return IsLineComplete(true, y, footprint);
        }

        /// <summary>Column counterpart to <see cref="IsRowCompleteWithFootprint"/>.</summary>
        private bool IsColumnCompleteWithFootprint(int x, HashSet<Vector2Int> footprint)
        {
            return IsLineComplete(false, x, footprint);
        }

        /// <summary>
        /// A joker piece's own cells have no fixed color of their own, so
        /// they can potentially anchor the resulting group on any distinct
        /// real color reachable through them (through other jokers too — see
        /// <see cref="FindCandidateAnchorColors"/>). Tries every such
        /// candidate color and keeps whichever resulting group scores the
        /// most (see <see cref="EstimateGroupScore"/>). No real-colored
        /// neighbor anywhere reachable just returns the connected cluster of
        /// joker cells itself.
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

            // A stack-based flood fill visits cells in an arbitrary order.
            // Sorted here into reading order (highest Y first — see
            // GridView.Build, y increases upward on screen — then ascending
            // X) so the per-cell progressive group bonus and its popup
            // animation read left-to-right, top-to-bottom. Purely cosmetic:
            // the group bonus total is unaffected by cell order.
            group.Sort((a, b) =>
            {
                int rowCompare = b.y.CompareTo(a.y);
                return rowCompare != 0 ? rowCompare : a.x.CompareTo(b.x);
            });
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
        /// cells, applied once to this whole placement's group bonus +
        /// golden bonus (see PlacementResult.GroupMultiplier/.TotalScore)
        /// rather than baked in per cell. Each matching tinted cell and each
        /// multiplier-zone cell in the group stacks its own x2. Does not
        /// reach the line-clear bonus — see <see cref="ComputeLineClearMultiplier"/>.
        /// </summary>
        private int ComputeGroupMultiplier(List<Vector2Int> groupCells)
        {
            int multiplier = 1;

            for (int i = 0; i < groupCells.Count; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (cell.IsPoisoned)
                {
                    // A blighted plot's own bonuses (tinted, multiplier zone, permanent bonus square) are switched off.
                    continue;
                }
                if (cell.IsTinted && cell.FilledColor.HasValue && cell.FilledColor.Value == cell.TintedColor)
                {
                    multiplier *= ScoringConstants.TintedMatchMultiplier;
                }
                if (cell.IsMultiplierZone || cell.IsPermanentMultiplier)
                {
                    multiplier *= ScoringConstants.MultiplierZoneMultiplier;
                }
            }

            return multiplier;
        }

        /// <summary>Same idea as <see cref="ComputeGroupMultiplier"/>, but counts only multiplier-zone cells — Tinted never reaches <see cref="PlacementResult.LineClearScore"/>.</summary>
        private int ComputeLineClearMultiplier(List<Vector2Int> groupCells)
        {
            int multiplier = 1;

            for (int i = 0; i < groupCells.Count; i++)
            {
                var cell = _cells[groupCells[i].x, groupCells[i].y];
                if (!cell.IsPoisoned && (cell.IsMultiplierZone || cell.IsPermanentMultiplier))
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

            /// <summary>Each cleared cell's <see cref="Cell.FilledShapeId"/> as it was right before clearing, parallel to <see cref="ClearedCells"/> — Color Hater's own sibling (RunManager.ApplyShapeHaterScoreRule) needs this the same way <see cref="ClearedCellColors"/> feeds ApplyCursedColorScoreRule, since the cell's own shape stamp is already gone from Core by the time that rule runs. Nullable, unlike ClearedCellColors — a cell filled directly (test setup, or any pre-existing board state never routed through <see cref="PlacePiece"/>) never had a shape stamped on it at all.</summary>
            public readonly IReadOnlyList<ShapeId?> ClearedCellShapes;

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

            /// <summary>Line-clear points per cell (cleared AND Bastion cells): each cell scores <see cref="ScoringConstants.LineClearBonusPerCell"/> times the 1-based order of the first completed line containing it (1st line 3/cell, 2nd 6/cell, 3rd 9/cell...), so breaking several lines in one placement snowballs.</summary>
            public readonly IReadOnlyDictionary<Vector2Int, int> LineAmounts;

            /// <summary>True when this placement's line clear left no filled cell on the whole grid.</summary>
            public readonly bool GridEmptied;

            /// <summary>Parallel to <see cref="ClearedCells"/>: true for a Frozen empty plot that the line just broke free (its <see cref="ClearedCellColors"/> entry is a meaningless default, never a real color).</summary>
            public readonly IReadOnlyList<bool> ClearedCellFrozen;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells, IReadOnlyList<PieceColor> clearedCellColors, IReadOnlyList<ShapeId?> clearedCellShapes, IReadOnlyList<PieceTrait?> clearedCellTraits, int clearedLineCount, IReadOnlyList<ClearedLine> clearedLines, IReadOnlyList<Vector2Int> bastionBonusCells, IReadOnlyDictionary<Vector2Int, int> lineAmounts, bool gridEmptied, IReadOnlyList<bool> clearedCellFrozen)
            {
                ClearedCells = clearedCells;
                ClearedCellColors = clearedCellColors;
                ClearedCellShapes = clearedCellShapes;
                ClearedCellTraits = clearedCellTraits;
                ClearedLineCount = clearedLineCount;
                ClearedLines = clearedLines;
                BastionBonusCells = bastionBonusCells;
                LineAmounts = lineAmounts;
                GridEmptied = gridEmptied;
                ClearedCellFrozen = clearedCellFrozen;
            }
        }
    }
}
