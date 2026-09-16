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

            int modifierBonus = 0;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPreClearModifiers(activeModifiers, shape, groupCells, placedCells, groupBonus, events);
            }

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.ClearedCellColors = clearInfo.ClearedCellColors;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            result.LineClearScore = clearInfo.ClearedCells.Count * ScoringConstants.LineClearBonusPerCell;

            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], ScoringConstants.LineClearBonusPerCell));
            }

            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPostClearModifiers(activeModifiers, clearInfo, placedCells, events);
            }

            result.ModifierBonus = modifierBonus;
            result.ScoreEvents = events;

            return result;
        }

        /// <summary>
        /// Modifiers that need the group/placement state as it stood right before
        /// line clears wipe completed rows/columns (Prisme/Chaîne/Méga-chaîne need
        /// the group; Forteresse/Prisonnier need neighbor fill state; Architecte
        /// only needs the shape). Each active modifier is evaluated once per
        /// occurrence, so holding the same modifier twice stacks its effect.
        /// </summary>
        private int ApplyPreClearModifiers(IReadOnlyList<ModifierId> activeModifiers, PieceShape shape, List<Vector2Int> groupCells, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var id = activeModifiers[i];
                int eventsBefore = events.Count;
                int bonus;
                switch (id)
                {
                    case ModifierId.Prisme:
                        bonus = ApplyPrisme(placedCells, events);
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
                        bonus = ApplyArchitecte(shape, placedCells, events);
                        break;
                    case ModifierId.Puriste:
                        bonus = ApplyPuriste(groupCells, placedCells, groupBonus, events);
                        break;
                    case ModifierId.Tricolore:
                        bonus = ApplyTricolore(placedCells, events);
                        break;
                    case ModifierId.Complementaire:
                        bonus = ApplyComplementaire(placedCells, events);
                        break;
                    case ModifierId.Ilot:
                        bonus = ApplyIlot(groupCells, placedCells, events);
                        break;
                    case ModifierId.Couronne:
                        bonus = ApplyCouronne(groupCells, events);
                        break;
                    case ModifierId.TrouDansLaGrille:
                        bonus = ApplyTrouDansLaGrille(groupCells, events);
                        break;
                    case ModifierId.Carrefour:
                        bonus = ApplyCarrefour(groupCells, events);
                        break;
                    default:
                        bonus = 0;
                        break;
                }
                TagNewEvents(events, eventsBefore, id);
                total += bonus;
            }
            return total;
        }

        /// <summary>Collectionneur/Maçon/Démolisseur all need the outcome of this placement's line clears, so they can only be evaluated after <see cref="CheckAndClearLines"/> runs.</summary>
        private int ApplyPostClearModifiers(IReadOnlyList<ModifierId> activeModifiers, ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int total = 0;
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
                    case ModifierId.Macon:
                        bonus = ApplyMacon(clearInfo, placedCells, events);
                        break;
                    case ModifierId.Demolisseur:
                        bonus = ApplyDemolisseur(clearInfo, placedCells, events);
                        break;
                    default:
                        bonus = 0;
                        break;
                }
                TagNewEvents(events, eventsBefore, id);
                total += bonus;
            }
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

        private int ApplyPrisme(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            bool hasJoker = touching.Remove(PieceColor.Joker);

            bool qualifies = touching.Count >= ScoringConstants.PrismeMinDistinctColors
                || (touching.Count == ScoringConstants.PrismeMinDistinctColors - 1 && hasJoker);
            if (!qualifies)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.PrismeBonus));
            return ScoringConstants.PrismeBonus;
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

        private int ApplyArchitecte(PieceShape shape, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (shape.Id != ShapeId.Sq2)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.ArchitecteBonus));
            return ScoringConstants.ArchitecteBonus;
        }

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
                    return 0;
                }
            }

            int bonus = groupBonus / 2;
            if (bonus <= 0)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
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

        private int ApplyTricolore(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            touching.Remove(PieceColor.Joker);

            if (touching.Count != ScoringConstants.TricoloreExactDistinctColors)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.TricoloreBonus));
            return ScoringConstants.TricoloreBonus;
        }

        /// <summary>Arbitrary complementary pairing across the 4 base colors — not derived from a color wheel, just a fixed pairing for this modifier.</summary>
        private static readonly PieceColor[][] ComplementaryPairs =
        {
            new[] { PieceColor.Coral, PieceColor.Violet },
            new[] { PieceColor.Teal, PieceColor.Lime }
        };

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
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.ComplementaireBonus));
            return ScoringConstants.ComplementaireBonus;
        }

        private int ApplyIlot(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count != 1)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.IlotBonus));
            return ScoringConstants.IlotBonus;
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

        private int ApplyTrouDansLaGrille(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!IsAdjacentToLockedCell(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.TrouBonusPerCell));
                total += ScoringConstants.TrouBonusPerCell;
            }
            return total;
        }

        private bool IsAdjacentToLockedCell(int x, int y)
        {
            return (InBounds(x - 1, y) && _cells[x - 1, y].IsLocked)
                || (InBounds(x + 1, y) && _cells[x + 1, y].IsLocked)
                || (InBounds(x, y - 1) && _cells[x, y - 1].IsLocked)
                || (InBounds(x, y + 1) && _cells[x, y + 1].IsLocked);
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

        private int ApplyMacon(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCells.Count > 0)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.MaconBonus));
            return ScoringConstants.MaconBonus;
        }

        private int ApplyDemolisseur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedLineCount < ScoringConstants.DemolisseurMinLines)
            {
                return 0;
            }

            int bonus = clearInfo.ClearedLineCount * ScoringConstants.DemolisseurBonusPerLine;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>
        /// Flood-fills the connected group of filled cells reachable from
        /// <paramref name="start"/> by orthogonal steps. A joker cell always
        /// joins (it has no color of its own to conflict with), but it does NOT
        /// bridge two otherwise-incompatible real colors into one group: the
        /// group locks onto the first non-joker color it discovers (its
        /// "anchor"), and any other-colored cell — reached directly or through
        /// a joker — is excluded from then on. So green-joker-blue is two
        /// separate potential groups sharing that joker cell, never one
        /// green+joker+blue group; which one the joker actually joins on a
        /// given placement depends on which color's flood-fill reaches it.
        /// </summary>
        private List<Vector2Int> FindConnectedGroup(Vector2Int start)
        {
            var visited = new HashSet<Vector2Int> { start };
            var stack = new Stack<Vector2Int>();
            stack.Push(start);
            var group = new List<Vector2Int>();

            var startColor = _cells[start.x, start.y].FilledColor.Value;
            PieceColor? anchorColor = startColor == PieceColor.Joker ? (PieceColor?)null : startColor;

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

            /// <summary>How many individual rows/columns completed simultaneously by this placement (distinct from <see cref="ClearedCells"/>.Count, which is a cell count) — used by Démolisseur.</summary>
            public readonly int ClearedLineCount;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells, IReadOnlyList<PieceColor> clearedCellColors, int clearedLineCount)
            {
                ClearedCells = clearedCells;
                ClearedCellColors = clearedCellColors;
                ClearedLineCount = clearedLineCount;
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
            int clearedLineCount = 0;

            for (int y = 0; y < Size; y++)
            {
                if (IsRowComplete(y))
                {
                    clearedLineCount++;
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
                    clearedLineCount++;
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

            return new ClearInfo(cleared, clearedColors, clearedLineCount);
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
