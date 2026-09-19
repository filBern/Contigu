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

            // The tinted-match/multiplier-zone factor is no longer baked into
            // each cell's own score — it's applied ONCE, at the very end of
            // this whole placement (see PlacementResult.GroupMultiplier and
            // .TotalScore), Balatro-style, instead of quietly inflating the
            // group bonus per cell. Every event below carries its plain,
            // unmultiplied "standard" amount.
            int groupMultiplier = ComputeGroupMultiplier(groupCells);
            int groupBonus = 0;
            int goldenBonus = 0;

            for (int i = 0; i < groupCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Group, groupCells[i], ScoringConstants.GroupBonusPerCell));
                groupBonus += ScoringConstants.GroupBonusPerCell;

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

            int modifierBonus = 0;
            if (activeModifiers != null && activeModifiers.Count > 0)
            {
                modifierBonus += ApplyPreClearModifiers(activeModifiers, shape, groupCells, placedCells, groupBonus, events, previousGroupSize);
            }

            var clearInfo = CheckAndClearLines();
            result.ClearedCells = clearInfo.ClearedCells;
            result.ClearedCellColors = clearInfo.ClearedCellColors;
            result.LineClearCellCount = clearInfo.ClearedCells.Count;
            result.LineClearScore = clearInfo.ClearedCells.Count * ScoringConstants.LineClearBonusPerCell;

            // Updates the streak for the NEXT placement to read (see
            // PlacementsSinceLastClear) — this placement's own clear (if any)
            // resets it, otherwise it extends by one.
            _placementsSinceLastClear = clearInfo.ClearedCells.Count > 0 ? 0 : _placementsSinceLastClear + 1;

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
        private int ApplyPreClearModifiers(IReadOnlyList<ModifierId> activeModifiers, PieceShape shape, List<Vector2Int> groupCells, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events, int? previousGroupSize)
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
                        bonus = ApplyDegrade(groupCells.Count, previousGroupSize, placedCells, events);
                        break;
                    case ModifierId.Emmitouflee:
                        bonus = ApplyEmmitouflee(groupCells, events);
                        break;
                    case ModifierId.Jardinier:
                        bonus = ApplyJardinier(groupCells, events);
                        break;
                    case ModifierId.DevotionCoral:
                        bonus = ApplyColorDevotion(PieceColor.Coral, placedCells, groupBonus, events);
                        break;
                    case ModifierId.DevotionTeal:
                        bonus = ApplyColorDevotion(PieceColor.Teal, placedCells, groupBonus, events);
                        break;
                    case ModifierId.DevotionViolet:
                        bonus = ApplyColorDevotion(PieceColor.Violet, placedCells, groupBonus, events);
                        break;
                    case ModifierId.DevotionLime:
                        bonus = ApplyColorDevotion(PieceColor.Lime, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeSingle:
                        bonus = ApplyShapeSpecialist(ShapeId.Single, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeDomH:
                        bonus = ApplyShapeSpecialist(ShapeId.DomH, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeDomV:
                        bonus = ApplyShapeSpecialist(ShapeId.DomV, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeTriL:
                        bonus = ApplyShapeSpecialist(ShapeId.TriL, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeTriIH:
                        bonus = ApplyShapeSpecialist(ShapeId.TriIH, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeTriIV:
                        bonus = ApplyShapeSpecialist(ShapeId.TriIV, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeSq2:
                        bonus = ApplyShapeSpecialist(ShapeId.Sq2, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeLTetro:
                        bonus = ApplyShapeSpecialist(ShapeId.LTetro, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeTTetro:
                        bonus = ApplyShapeSpecialist(ShapeId.TTetro, shape, placedCells, groupBonus, events);
                        break;
                    case ModifierId.FormeSTetro:
                        bonus = ApplyShapeSpecialist(ShapeId.STetro, shape, placedCells, groupBonus, events);
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

        /// <summary>"Devotion" (per-color): fully doubles this placement's group bonus when the placement's own fill color matches <paramref name="targetColor"/> — every cell of one placement always shares the same color, so checking the first placed cell is enough.</summary>
        private int ApplyColorDevotion(PieceColor targetColor, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            if (ownColor != targetColor || groupBonus <= 0)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], groupBonus));
            return groupBonus;
        }

        /// <summary>"Specialist" (per-shape): fully doubles this placement's group bonus when the placed piece's own shape matches <paramref name="targetShape"/>.</summary>
        private int ApplyShapeSpecialist(ShapeId targetShape, PieceShape shape, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events)
        {
            if (shape.Id != targetShape || groupBonus <= 0)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], groupBonus));
            return groupBonus;
        }

        /// <summary>Collectionneur/Maçon/Démolisseur/the 8 line-pattern modifiers all need the outcome of this placement's line clears, so they can only be evaluated after <see cref="CheckAndClearLines"/> runs.</summary>
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
                    case ModifierId.ArcEnCiel:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, ContainsAllBaseColors, ScoringConstants.ArcEnCielBonusPerLine);
                        break;
                    case ModifierId.Alternance:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, IsAlternatingTwoColors, ScoringConstants.AlternanceBonusPerLine);
                        break;
                    case ModifierId.Palindrome:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, IsPalindrome, ScoringConstants.PalindromeBonusPerLine);
                        break;
                    case ModifierId.Gradient:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, IsGradientLine, ScoringConstants.GradientBonusPerLine);
                        break;
                    case ModifierId.Bloc:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, IsAllBlocksOfAtLeastTwo, ScoringConstants.BlocBonusPerLine);
                        break;
                    case ModifierId.MonochromeLigne:
                        bonus = ApplyPerLineBonus(clearInfo, placedCells, events, IsMonochromeLine, ScoringConstants.MonochromeLigneBonusPerLine);
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

        private int ApplyDegrade(int currentGroupSize, int? previousGroupSize, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!previousGroupSize.HasValue || currentGroupSize <= previousGroupSize.Value)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.DegradeBonus));
            return ScoringConstants.DegradeBonus;
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

        /// <summary>Shared driver for the 7 line-pattern modifiers that just need a per-line yes/no predicate over its ordered color sequence — bonus fires once per qualifying cleared line.</summary>
        private int ApplyPerLineBonus(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events, System.Func<IReadOnlyList<PieceColor>, bool> predicate, int bonusPerLine)
        {
            int total = 0;
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!predicate(lines[i].Colors))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonusPerLine));
                total += bonusPerLine;
            }
            return total;
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
        /// Aggregate multiplier from this placement's tinted/multiplier-zone
        /// cells — applied ONCE to this whole placement's total (group bonus
        /// + golden bonus + line-clear bonus, see PlacementResult.
        /// GroupMultiplier/.TotalScore) rather than baked into the group
        /// bonus per cell (Balatro-style "multiply at the end", explicit
        /// request). Each matching tinted cell AND each multiplier-zone cell
        /// in the group stacks its own x2 (two of either in the same combo
        /// combine to x4, three to x8, ...) — multiplier-zone used to only
        /// count once regardless of how many cells had it, but that made
        /// "Multiplier Beacon" (which can tag many cells in one row/column at
        /// once) pointless beyond a single x2, identical to the plain
        /// single-cell Multiplier trait. Stacking it the same way Tinted
        /// already does gives Beacon real extra teeth when several of its
        /// marked cells land in the same scored group, and makes both
        /// factors consistent with each other.
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

            /// <summary>How many individual rows/columns completed simultaneously by this placement (distinct from <see cref="ClearedCells"/>.Count, which is a cell count) — used by Démolisseur.</summary>
            public readonly int ClearedLineCount;

            /// <summary>One entry per completed row/column this placement, each with its own pre-clear color sequence — used by the 8 line-pattern modifiers.</summary>
            public readonly IReadOnlyList<ClearedLine> ClearedLines;

            public ClearInfo(IReadOnlyList<Vector2Int> clearedCells, IReadOnlyList<PieceColor> clearedCellColors, int clearedLineCount, IReadOnlyList<ClearedLine> clearedLines)
            {
                ClearedCells = clearedCells;
                ClearedCellColors = clearedCellColors;
                ClearedLineCount = clearedLineCount;
                ClearedLines = clearedLines;
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
            var clearedLines = new List<ClearedLine>();

            for (int y = 0; y < Size; y++)
            {
                if (IsRowComplete(y))
                {
                    clearedLineCount++;
                    clearedLines.Add(new ClearedLine(ExtractLineColors(isRow: true, index: y), isRow: true, index: y));
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
                    clearedLines.Add(new ClearedLine(ExtractLineColors(isRow: false, index: x), isRow: false, index: x));
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

            return new ClearInfo(cleared, clearedColors, clearedLineCount, clearedLines);
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
            chosenCell.IsFilled = false;
            chosenCell.FilledColor = null;
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
