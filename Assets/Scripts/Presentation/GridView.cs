using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders the 8x8 <see cref="GridManager"/> as a grid of clickable cells and
    /// previews the footprint of the currently selected hand piece on hover.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        public event Action<int, int> CellClicked;

        /// <summary>Fires whenever the hovered footprint's validity changes, including to false when hover leaves the grid — used by <see cref="HandView"/> to hide its drag ghost while a valid footprint tint is already shown.</summary>
        public event Action<bool> HoverValidityChanged;

        private GridManager _grid;
        private GridCellView[,] _cells;
        private PieceShape _selectedShape;
        private PieceColor? _selectedColor;
        private PieceTrait? _selectedTrait;
        private readonly List<Vector2Int> _hoveredFootprint = new List<Vector2Int>();
        // Separate from _hoveredFootprint: tracks whole rows/columns that would clear, mostly cells outside the piece's own footprint.
        private readonly List<Vector2Int> _lineClearPreviewCells = new List<Vector2Int>();
        private TooltipView _tooltip;
        private RectTransform _container;
        private float _cellStride;

        public RectTransform Build(Transform parent, GridManager grid, float cellSize, TooltipView tooltip)
        {
            _grid = grid;
            _tooltip = tooltip;
            _cells = new GridCellView[GridManager.Size, GridManager.Size];

            var container = UIFactory.CreateUIObject("GridContainer", parent);
            _container = container;
            float spacing = 6f;
            _cellStride = cellSize + spacing;
            float total = GridManager.Size * cellSize + (GridManager.Size - 1) * spacing;
            container.sizeDelta = new Vector2(total, total);

            var layout = container.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(spacing, spacing);
            layout.startCorner = GridLayoutGroup.Corner.LowerLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridManager.Size;

            // Invisible, full-grid-size raycast target on the container itself: a child cell's Image always wins the raycast
            // when the pointer is precisely on it, so this only catches pointer events landing in the inter-cell spacing gap.
            // See GridGapCatcher/OnGapPointerEnter/OnGapPointerClick for resolving a gap point to its nearest cell.
            var gapCatcherImage = container.gameObject.AddComponent<Image>();
            gapCatcherImage.color = new Color(0f, 0f, 0f, 0f);
            var gapCatcher = container.gameObject.AddComponent<GridGapCatcher>();
            gapCatcher.Init(this);

            // Instantiated bottom row (y=0) first so LowerLeft start corner
            // produces an on-screen layout where y increases upward.
            for (int y = 0; y < GridManager.Size; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    CreateCell(container, x, y);
                }
            }

            Refresh();
            return container;
        }

        private void CreateCell(Transform parent, int x, int y)
        {
            var cellGo = UIFactory.CreateUIObject("Cell_" + x + "_" + y, parent);
            var background = cellGo.gameObject.AddComponent<Image>();
            background.color = Color.white;
            // Sliced so the rounded card border sprite stays sharp at cell size instead of stretching.
            background.type = Image.Type.Sliced;

            // Overlay for a filled cell's "block" look, above Background but below every badge (see GridCellView.ApplyState).
            var fillTile = UIFactory.CreatePanel(cellGo, "FillTile", Color.white);
            UIFactory.StretchFull(fillTile.rectTransform);
            fillTile.gameObject.SetActive(false);

            // Gold border on cells of a row/column that would clear if the hovered piece landed here
            // (see GridManager.PreviewClearedLineCells). Built from 4 thin bars rather than Outline/Shadow,
            // since those rely on the base Graphic's alpha and would obscure the cell's own fill color.
            var lineClearOverlay = BuildLineClearBorder(cellGo);

            var badgeGolden = UIFactory.CreatePanel(cellGo, "BadgeGolden", Color.yellow);
            UIFactory.SetAnchor(badgeGolden.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f));
            badgeGolden.rectTransform.pivot = new Vector2(0f, 1f);
            badgeGolden.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeGolden.rectTransform.anchoredPosition = new Vector2(3f, -3f);
            // Dark outline so the badge stays readable regardless of the cell's
            // fill color (a plain gold square can blend into a light piece color).
            var badgeGoldenOutline = badgeGolden.gameObject.AddComponent<Outline>();
            badgeGoldenOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeGoldenOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeGolden.gameObject.SetActive(false);

            // Marks which deck upgrade originally enchanted this cell (see Cell.OriginTrait).
            var badgeTraitOrigin = UIFactory.CreatePanel(cellGo, "BadgeTraitOrigin", Color.cyan);
            UIFactory.SetAnchor(badgeTraitOrigin.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            badgeTraitOrigin.rectTransform.pivot = new Vector2(1f, 1f);
            badgeTraitOrigin.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeTraitOrigin.rectTransform.anchoredPosition = new Vector2(-3f, -3f);
            var badgeTraitOriginOutline = badgeTraitOrigin.gameObject.AddComponent<Outline>();
            badgeTraitOriginOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeTraitOriginOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeTraitOrigin.gameObject.SetActive(false);

            var badgeSpecial = UIFactory.CreatePanel(cellGo, "BadgeSpecial", Color.magenta);
            UIFactory.SetAnchor(badgeSpecial.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f));
            badgeSpecial.rectTransform.pivot = new Vector2(1f, 0f);
            badgeSpecial.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeSpecial.rectTransform.anchoredPosition = new Vector2(-3f, 3f);
            var badgeSpecialOutline = badgeSpecial.gameObject.AddComponent<Outline>();
            badgeSpecialOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeSpecialOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeSpecial.gameObject.SetActive(false);

            // Poisoner mechanic (see Cell.IsPoisoned/RunManager.ResolvePoisonerShuffleEffect).
            var badgePoison = UIFactory.CreatePanel(cellGo, "BadgePoison", UITheme.Danger);
            UIFactory.SetAnchor(badgePoison.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f));
            badgePoison.rectTransform.pivot = new Vector2(0f, 0f);
            badgePoison.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgePoison.rectTransform.anchoredPosition = new Vector2(3f, 3f);
            var badgePoisonOutline = badgePoison.gameObject.AddComponent<Outline>();
            badgePoisonOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgePoisonOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgePoison.gameObject.SetActive(false);

            // Colorblind-mode shape (see ColorblindMode/ColorblindShapeFactory); created before InvalidMarker
            // so a hover-invalid preview still draws on top of it.
            var colorblindShape = UIFactory.CreatePanel(cellGo, "ColorblindShape", Color.black);
            UIFactory.SetAnchor(colorblindShape.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            colorblindShape.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            colorblindShape.rectTransform.sizeDelta = new Vector2(26f, 26f);
            colorblindShape.rectTransform.anchoredPosition = Vector2.zero;
            colorblindShape.gameObject.SetActive(false);

            // Shown instead of the color-icon preview while hovering an invalid placement.
            var invalidMarker = UIFactory.CreatePanel(cellGo, "InvalidMarker", UITheme.Danger);
            UIFactory.SetAnchor(invalidMarker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            invalidMarker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            invalidMarker.rectTransform.sizeDelta = new Vector2(20f, 20f);
            invalidMarker.rectTransform.anchoredPosition = Vector2.zero;
            var invalidMarkerOutline = invalidMarker.gameObject.AddComponent<Outline>();
            invalidMarkerOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            invalidMarkerOutline.effectDistance = new Vector2(1.5f, -1.5f);
            invalidMarker.gameObject.SetActive(false);

            // Spells out a modifier cell's effect ("+18", "x2", "x4") as text.
            var effectLabel = UIFactory.CreateText(cellGo, "EffectLabel", "", 11, Color.white, TextAnchor.LowerCenter);
            effectLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            effectLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            effectLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            effectLabel.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            effectLabel.rectTransform.sizeDelta = new Vector2(-4f, 14f);
            effectLabel.gameObject.SetActive(false);

            var cellView = cellGo.gameObject.AddComponent<GridCellView>();
            cellView.Init(this, x, y, background, fillTile, badgeGolden, badgeSpecial, badgePoison, invalidMarker, effectLabel, badgeTraitOrigin, colorblindShape, lineClearOverlay, _tooltip);
            _cells[x, y] = cellView;
        }

        /// <summary>4 thin bars forming a hollow border around the cell's edges, toggled as one unit by GridCellView.SetLineClearPreview.</summary>
        private static GameObject BuildLineClearBorder(Transform parent)
        {
            const float BarThickness = 3f;
            var borderColor = new Color(VisualDefaults.GoldenColor.r, VisualDefaults.GoldenColor.g, VisualDefaults.GoldenColor.b, 0.9f);

            var wrapper = UIFactory.CreateUIObject("LineClearOverlay", parent);
            UIFactory.StretchFull(wrapper);

            var top = UIFactory.CreatePanel(wrapper, "Top", borderColor);
            top.raycastTarget = false;
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.pivot = new Vector2(0.5f, 1f);
            top.rectTransform.sizeDelta = new Vector2(0f, BarThickness);
            top.rectTransform.anchoredPosition = Vector2.zero;

            var bottom = UIFactory.CreatePanel(wrapper, "Bottom", borderColor);
            bottom.raycastTarget = false;
            bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottom.rectTransform.sizeDelta = new Vector2(0f, BarThickness);
            bottom.rectTransform.anchoredPosition = Vector2.zero;

            var left = UIFactory.CreatePanel(wrapper, "Left", borderColor);
            left.raycastTarget = false;
            left.rectTransform.anchorMin = new Vector2(0f, 0f);
            left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f);
            left.rectTransform.sizeDelta = new Vector2(BarThickness, 0f);
            left.rectTransform.anchoredPosition = Vector2.zero;

            var right = UIFactory.CreatePanel(wrapper, "Right", borderColor);
            right.raycastTarget = false;
            right.rectTransform.anchorMin = new Vector2(1f, 0f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(BarThickness, 0f);
            right.rectTransform.anchoredPosition = Vector2.zero;

            wrapper.gameObject.SetActive(false);
            return wrapper.gameObject;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) GridManager instance.</summary>
        public void Rebind(GridManager grid)
        {
            _grid = grid;
            _lastHoverOrigin = null;
            ClearHover();
            Refresh();
        }

        public void Refresh()
        {
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    _cells[x, y].ApplyState(_grid.GetCell(x, y));
                }
            }
        }

        /// <summary>
        /// Same as <see cref="Refresh"/>, except cells at <paramref name="heldCells"/> are painted as still
        /// filled with their given color instead of their actual (already-cleared) grid state, so a
        /// just-completed line stays visually filled while its score plays out, before
        /// <see cref="ClearCellVisual"/> empties each cell in turn.
        /// <paramref name="deferredNewMalusCells"/> render with their true fill state but their lock/poison
        /// badge hidden (suppressMalus) until the end-of-sequence <see cref="Refresh"/> reveals it, so a
        /// malus applied by this same placement doesn't appear before the score finishes counting.
        /// <paramref name="deferredReleasedLockedCells"/>/<paramref name="deferredReleasedPoisonedCells"/>
        /// render as still locked/poisoned (forceShowLocked/forceShowPoisoned) even though the underlying
        /// state already released, until the same final reveal.
        /// A cell that is both deferred and in <paramref name="heldCells"/> always gets the held-filled
        /// treatment, so its clear animation stays in step with its neighbors.
        /// </summary>
        public void RefreshHoldingClearedCells(IReadOnlyList<Vector2Int> heldCells, IReadOnlyList<PieceColor> heldColors, IReadOnlyList<PieceTrait?> heldTraits, IReadOnlyList<Vector2Int> deferredNewMalusCells = null, IReadOnlyList<Vector2Int> deferredReleasedLockedCells = null, IReadOnlyList<Vector2Int> deferredReleasedPoisonedCells = null)
        {
            var overrideColor = new Dictionary<Vector2Int, PieceColor>();
            var overrideTrait = new Dictionary<Vector2Int, PieceTrait?>();
            for (int i = 0; i < heldCells.Count; i++)
            {
                overrideColor[heldCells[i]] = heldColors[i];
                overrideTrait[heldCells[i]] = heldTraits[i];
            }

            var deferredNew = ToSet(deferredNewMalusCells);
            var releasedLocked = ToSet(deferredReleasedLockedCells);
            var releasedPoisoned = ToSet(deferredReleasedPoisonedCells);

            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var pos = new Vector2Int(x, y);
                    // The held-fill override always wins over a deferred skip, even for a cell in both sets.
                    if (overrideColor.TryGetValue(pos, out var color))
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y), color, overrideTrait[pos]);
                    }
                    else if (releasedLocked.Contains(pos) || releasedPoisoned.Contains(pos))
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y), forceShowLocked: releasedLocked.Contains(pos), forceShowPoisoned: releasedPoisoned.Contains(pos));
                    }
                    else if (deferredNew.Contains(pos))
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y), suppressMalus: true);
                    }
                    else
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y));
                    }
                }
            }
        }

        private static HashSet<Vector2Int> ToSet(IReadOnlyList<Vector2Int> positions)
        {
            var set = new HashSet<Vector2Int>();
            if (positions != null)
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    set.Add(positions[i]);
                }
            }
            return set;
        }

        /// <summary>Re-renders one cell from the grid's actual current state — used to visually empty a single cell of a line as it clears.</summary>
        public void ClearCellVisual(int x, int y)
        {
            RefreshCell(x, y);
        }

        private void RefreshCell(int x, int y)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].ApplyState(_grid.GetCell(x, y));
            }
        }

        /// <summary>Instantly sets every cell's malus alpha with no animation — used around Refresh() so FadeMalus can fade from/to a clean state.</summary>
        public void SetMalusAlpha(float alpha)
        {
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    _cells[x, y].SetMalusAlpha(alpha);
                }
            }
        }

        /// <summary>
        /// Fades every currently-shown malus (locked-obstacle cells and poison badges) one cell at a time,
        /// each taking <paramref name="duration"/> seconds, rather than all at once. Only changes visibility,
        /// never which cells are a malus. The set of cells to fade is captured once up front via
        /// IsShowingMalus(), as a snapshot.
        /// </summary>
        public System.Collections.IEnumerator FadeMalus(float from, float to, float duration)
        {
            var malusCells = new List<GridCellView>();
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (_cells[x, y].IsShowingMalus())
                    {
                        malusCells.Add(_cells[x, y]);
                    }
                }
            }
            for (int i = 0; i < malusCells.Count; i++)
            {
                yield return FadeOneCellMalus(malusCells[i], from, to, duration);
            }
        }

        private static System.Collections.IEnumerator FadeOneCellMalus(GridCellView cell, float from, float to, float duration)
        {
            cell.SetMalusAlpha(from);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cell.SetMalusAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            cell.SetMalusAlpha(to);
        }

        public void SetSelectedShape(PieceShape shape, PieceColor? color = null, PieceTrait? trait = null)
        {
            _selectedShape = shape;
            _selectedColor = color;
            _selectedTrait = trait;
            _lastHoverOrigin = null;
            ClearHover();
        }

        /// <summary>
        /// Same state reset as <see cref="SetSelectedShape"/>(null), but without ClearHover's per-cell
        /// <see cref="RefreshCell"/> calls, which would read live grid state and leak a lock/poison change
        /// early, before the score cascade starts. Used by OnCellClicked right before its own
        /// RefreshHoldingClearedCells call, which redraws every cell anyway.
        /// </summary>
        public void ClearSelectionStateOnly()
        {
            _selectedShape = null;
            _selectedColor = null;
            _selectedTrait = null;
            _lastHoverOrigin = null;
            _hoveredFootprint.Clear();
            _lineClearPreviewCells.Clear();
        }

        /// <summary>The last placement origin actually rendered (not the raw x/y hovered — several raw cells can resolve to the same origin via GetPlacementOrigin's snap logic). Null forces the next call to redo the work.</summary>
        private Vector2Int? _lastHoverOrigin;

        public void OnCellHoverEnter(int x, int y)
        {
            if (_selectedShape == null)
            {
                ClearHover();
                _lastHoverOrigin = null;
                HoverValidityChanged?.Invoke(false);
                return;
            }

            var origin = GetPlacementOrigin(x, y);
            if (_lastHoverOrigin.HasValue && _lastHoverOrigin.Value == origin)
            {
                // Resolved placement spot hasn't changed; skip redoing tint/pulse so the previewed group doesn't restart its pulse animation every frame.
                return;
            }
            _lastHoverOrigin = origin;
            ClearHover();

            bool valid = _grid.CanPlace(_selectedShape, origin.x, origin.y);
            var overlay = valid ? UITheme.HoverValid : UITheme.HoverInvalid;
            var offsets = _selectedShape.Cells;
            var footprint = new HashSet<Vector2Int>();
            for (int i = 0; i < offsets.Count; i++)
            {
                int cx = origin.x + offsets[i].x;
                int cy = origin.y + offsets[i].y;
                if (GridManager.InBounds(cx, cy))
                {
                    // Only the one cell matching the selected piece's own
                    // enchanted local index previews the trait badge.
                    bool isTraitCell = _selectedTrait.HasValue && _selectedTrait.Value.LocalCellIndex == i;
                    _cells[cx, cy].SetHoverTint(overlay, valid, isTraitCell ? _selectedTrait : null);
                    _hoveredFootprint.Add(new Vector2Int(cx, cy));
                    footprint.Add(new Vector2Int(cx, cy));
                }
            }

            // Also pulses every pre-existing cell that would be pulled into the same scored group as this
            // placement, so the player sees the full scoring extent, not just the piece's footprint.
            // Only meaningful for a valid placement — GridManager.PreviewGroup assumes CanPlace already passed.
            // Pulse() is self-resetting, so there's nothing to undo in ClearHover.
            if (valid)
            {
                var previewGroup = _grid.PreviewGroup(_selectedShape, _selectedColor.Value, origin.x, origin.y);
                for (int i = 0; i < previewGroup.Count; i++)
                {
                    var pos = previewGroup[i];
                    if (footprint.Contains(pos))
                    {
                        continue;
                    }
                    _cells[pos.x, pos.y].Pulse();
                }

                // Highlights every cell of any row/column this placement would clear. Unlike Pulse() above,
                // this tint needs explicit cleanup (see ClearHover), so cells touched here are tracked in _lineClearPreviewCells.
                var clearedLineCells = _grid.PreviewClearedLineCells(_selectedShape, origin.x, origin.y);
                for (int i = 0; i < clearedLineCells.Count; i++)
                {
                    var pos = clearedLineCells[i];
                    _cells[pos.x, pos.y].SetLineClearPreview(true);
                    _lineClearPreviewCells.Add(pos);
                }
            }

            HoverValidityChanged?.Invoke(valid);
        }

        // How far (in cells, each direction) GetPlacementOrigin looks for a nearby spot the hovered piece would fit.
        private const int SnapSearchRadius = 2;

        /// <summary>
        /// Shifts by half the shape's bounding box (rounded down) so the piece centers on the cursor's cell
        /// instead of hanging up-and-right of it (shapes store their origin at the bounding box's bottom-left,
        /// see PieceShapeCatalog). If that spot doesn't fit, snaps to the closest spot within
        /// <see cref="SnapSearchRadius"/> cells that does, falling back to the unsnapped spot otherwise.
        /// </summary>
        private Vector2Int GetPlacementOrigin(int x, int y)
        {
            if (_selectedShape == null)
            {
                return new Vector2Int(x, y);
            }
            int maxX = 0;
            int maxY = 0;
            var offsets = _selectedShape.Cells;
            for (int i = 0; i < offsets.Count; i++)
            {
                if (offsets[i].x > maxX) maxX = offsets[i].x;
                if (offsets[i].y > maxY) maxY = offsets[i].y;
            }
            var naiveOrigin = new Vector2Int(x - maxX / 2, y - maxY / 2);
            if (_grid.CanPlace(_selectedShape, naiveOrigin.x, naiveOrigin.y))
            {
                return naiveOrigin;
            }
            return FindNearestValidOrigin(naiveOrigin) ?? naiveOrigin;
        }

        /// <summary>Closest origin (by straight-line distance) within SnapSearchRadius cells of <paramref name="naiveOrigin"/> where the selected shape actually fits, or null if nothing in that radius does.</summary>
        private Vector2Int? FindNearestValidOrigin(Vector2Int naiveOrigin)
        {
            Vector2Int? best = null;
            int bestDistSq = int.MaxValue;
            for (int dy = -SnapSearchRadius; dy <= SnapSearchRadius; dy++)
            {
                for (int dx = -SnapSearchRadius; dx <= SnapSearchRadius; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int candidateX = naiveOrigin.x + dx;
                    int candidateY = naiveOrigin.y + dy;
                    if (!_grid.CanPlace(_selectedShape, candidateX, candidateY))
                    {
                        continue;
                    }
                    int distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = new Vector2Int(candidateX, candidateY);
                    }
                }
            }
            return best;
        }

        public void OnCellHoverExit(int x, int y)
        {
            _lastHoverOrigin = null;
            ClearHover();
            HoverValidityChanged?.Invoke(false);
        }

        /// <summary>Fired by GridGapCatcher when the pointer is within the grid's bounds but not over any cell — resolves to the nearest cell.</summary>
        public void OnGapPointerEnter(Vector2 localPoint)
        {
            var cell = NearestCell(localPoint);
            OnCellHoverEnter(cell.x, cell.y);
        }

        /// <summary>Fired by GridGapCatcher for a click or drag-drop landing in the gap between cells — same nearest-cell fallback as OnGapPointerEnter.</summary>
        public void OnGapPointerClick(Vector2 localPoint)
        {
            var cell = NearestCell(localPoint);
            OnCellClicked(cell.x, cell.y);
        }

        /// <summary>
        /// <paramref name="localPoint"/> is relative to the grid container's RectTransform. Each cell owns a
        /// slot of width/height <see cref="_cellStride"/> (cellSize plus spacing); dividing the offset from the
        /// container's bottom-left corner by that stride assigns gap pixels to the nearest cell, then clamps
        /// to the grid's bounds.
        /// </summary>
        private Vector2Int NearestCell(Vector2 localPoint)
        {
            var rect = _container.rect;
            float gx = localPoint.x - rect.xMin;
            float gy = localPoint.y - rect.yMin;
            int cellX = Mathf.Clamp(Mathf.FloorToInt(gx / _cellStride), 0, GridManager.Size - 1);
            int cellY = Mathf.Clamp(Mathf.FloorToInt(gy / _cellStride), 0, GridManager.Size - 1);
            return new Vector2Int(cellX, cellY);
        }

        private void ClearHover()
        {
            for (int i = 0; i < _hoveredFootprint.Count; i++)
            {
                var pos = _hoveredFootprint[i];
                RefreshCell(pos.x, pos.y);
            }
            _hoveredFootprint.Clear();

            for (int i = 0; i < _lineClearPreviewCells.Count; i++)
            {
                var pos = _lineClearPreviewCells[i];
                RefreshCell(pos.x, pos.y);
            }
            _lineClearPreviewCells.Clear();
        }

        public void OnCellClicked(int x, int y)
        {
            var origin = GetPlacementOrigin(x, y);
            CellClicked?.Invoke(origin.x, origin.y);
        }

        /// <summary>World/screen anchored position of a cell's center, for spawning floating score popups.</summary>
        public RectTransform GetCellTransform(int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return null;
            }
            return _cells[x, y].GetComponent<RectTransform>();
        }

        /// <summary>Plays a brief pulse on one cell — used when it scores points.</summary>
        public void PulseCell(int x, int y)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].Pulse();
            }
        }

        /// <summary>Plays a small radial burst on one cell, in <paramref name="color"/> — used right as it empties, whether from completing a line/column or from a trait effect destroying it (see GridCellView.PlayClearBurst).</summary>
        public void PlayClearBurst(int x, int y, Color color)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].PlayClearBurst(color);
            }
        }

        private const float ChameleonTransitionDuration = 0.4f;

        /// <summary>
        /// Chameleon Tile (RunManager.ResolveChameleonColor): lerps <paramref name="cells"/>' fill together
        /// from <paramref name="from"/> to <paramref name="to"/> instead of letting them snap straight to the
        /// resolved color — by the time this placement's own ApplyState call ran, the cells already show
        /// <paramref name="to"/>, so this starts by forcing them back to <paramref name="from"/> and animates
        /// forward from there. Meant to be yielded before any score/Lueur pulse reads these cells' color.
        /// </summary>
        public System.Collections.IEnumerator PlayChameleonColorTransition(IReadOnlyList<Vector2Int> cells, PieceColor from, PieceColor to)
        {
            Color fromColor = VisualDefaults.GetColor(from);
            Color toColor = VisualDefaults.GetColor(to);
            float t = 0f;
            while (t < ChameleonTransitionDuration)
            {
                t += Time.deltaTime;
                Color c = Color.Lerp(fromColor, toColor, Mathf.Clamp01(t / ChameleonTransitionDuration));
                for (int i = 0; i < cells.Count; i++)
                {
                    var pos = cells[i];
                    if (GridManager.InBounds(pos.x, pos.y))
                    {
                        _cells[pos.x, pos.y].Background.color = c;
                    }
                }
                yield return null;
            }
            for (int i = 0; i < cells.Count; i++)
            {
                var pos = cells[i];
                if (GridManager.InBounds(pos.x, pos.y))
                {
                    _cells[pos.x, pos.y].Background.color = toColor;
                }
            }
        }
    }
}
