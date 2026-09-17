using System;
using System.Collections.Generic;
using Contigu.Core;
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

        private GridManager _grid;
        private GridCellView[,] _cells;
        private PieceShape _selectedShape;
        private PieceColor? _selectedColor;
        private readonly List<Vector2Int> _hoveredFootprint = new List<Vector2Int>();

        public RectTransform Build(Transform parent, GridManager grid, float cellSize)
        {
            _grid = grid;
            _cells = new GridCellView[GridManager.Size, GridManager.Size];

            var container = UIFactory.CreateUIObject("GridContainer", parent);
            float spacing = 3f;
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

            // Neutral overlay drawn on top of the flat color fill and below
            // every badge — gives a filled piece cell a distinct "block" look
            // instead of a flat rect. Only shown for an actually-filled cell
            // (see GridCellView.ApplyState); built as the first child so it
            // sits above Background but below every badge/label below.
            var fillTile = UIFactory.CreatePanel(cellGo, "FillTile", Color.white);
            UIFactory.StretchFull(fillTile.rectTransform);
            fillTile.gameObject.SetActive(false);

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

            var badgeSpecial = UIFactory.CreatePanel(cellGo, "BadgeSpecial", Color.magenta);
            UIFactory.SetAnchor(badgeSpecial.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f));
            badgeSpecial.rectTransform.pivot = new Vector2(1f, 0f);
            badgeSpecial.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeSpecial.rectTransform.anchoredPosition = new Vector2(-3f, 3f);
            var badgeSpecialOutline = badgeSpecial.gameObject.AddComponent<Outline>();
            badgeSpecialOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeSpecialOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeSpecial.gameObject.SetActive(false);

            // Colorblind-accessibility icon, centered on the fill so a piece's
            // color is never the only way to tell it apart from another.
            var badgeColorIcon = UIFactory.CreatePanel(cellGo, "BadgeColorIcon", Color.white);
            UIFactory.SetAnchor(badgeColorIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            badgeColorIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badgeColorIcon.rectTransform.sizeDelta = new Vector2(48f, 48f);
            badgeColorIcon.rectTransform.anchoredPosition = Vector2.zero;
            var badgeColorIconOutline = badgeColorIcon.gameObject.AddComponent<Outline>();
            badgeColorIconOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            badgeColorIconOutline.effectDistance = new Vector2(1f, -1f);
            badgeColorIcon.gameObject.SetActive(false);

            // Solid marker shown instead of the color-icon preview while
            // hovering an invalid placement — a plain colored square (no
            // sprite needed), small and central so it reads as a clear "not
            // here" rather than blending into the red background tint alone.
            var invalidMarker = UIFactory.CreatePanel(cellGo, "InvalidMarker", UITheme.Danger);
            UIFactory.SetAnchor(invalidMarker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            invalidMarker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            invalidMarker.rectTransform.sizeDelta = new Vector2(20f, 20f);
            invalidMarker.rectTransform.anchoredPosition = Vector2.zero;
            var invalidMarkerOutline = invalidMarker.gameObject.AddComponent<Outline>();
            invalidMarkerOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            invalidMarkerOutline.effectDistance = new Vector2(1.5f, -1.5f);
            invalidMarker.gameObject.SetActive(false);

            // Spells out a modifier cell's effect ("+18", "x2", "x4") as text,
            // on top of the color badges above.
            var effectLabel = UIFactory.CreateText(cellGo, "EffectLabel", "", 11, Color.white, TextAnchor.LowerCenter);
            effectLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            effectLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            effectLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            effectLabel.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            effectLabel.rectTransform.sizeDelta = new Vector2(-4f, 14f);
            effectLabel.fontStyle = FontStyle.Bold;
            var effectLabelOutline = effectLabel.gameObject.AddComponent<Outline>();
            effectLabelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            effectLabelOutline.effectDistance = new Vector2(1f, -1f);
            effectLabel.gameObject.SetActive(false);

            var cellView = cellGo.gameObject.AddComponent<GridCellView>();
            cellView.Init(this, x, y, background, fillTile, badgeGolden, badgeSpecial, badgeColorIcon, invalidMarker, effectLabel);
            _cells[x, y] = cellView;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) GridManager instance.</summary>
        public void Rebind(GridManager grid)
        {
            _grid = grid;
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
        /// Same as <see cref="Refresh"/>, except the cells at <paramref name="heldCells"/>
        /// are painted as still filled with their given color instead of their
        /// actual (already-cleared) grid state — used to hold a just-completed
        /// line visually filled while its score is still playing out, before
        /// <see cref="ClearCellVisual"/> empties each cell in turn.
        /// </summary>
        public void RefreshHoldingClearedCells(IReadOnlyList<Vector2Int> heldCells, IReadOnlyList<PieceColor> heldColors)
        {
            var overrideColor = new Dictionary<Vector2Int, PieceColor>();
            for (int i = 0; i < heldCells.Count; i++)
            {
                overrideColor[heldCells[i]] = heldColors[i];
            }

            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (overrideColor.TryGetValue(pos, out var color))
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y), color);
                    }
                    else
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y));
                    }
                }
            }
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

        public void SetSelectedShape(PieceShape shape, PieceColor? color = null)
        {
            _selectedShape = shape;
            _selectedColor = color;
            ClearHover();
        }

        public void OnCellHoverEnter(int x, int y)
        {
            ClearHover();
            if (_selectedShape == null)
            {
                return;
            }

            bool valid = _grid.CanPlace(_selectedShape, x, y);
            var overlay = valid ? UITheme.HoverValid : UITheme.HoverInvalid;
            var offsets = _selectedShape.Cells;
            for (int i = 0; i < offsets.Count; i++)
            {
                int cx = x + offsets[i].x;
                int cy = y + offsets[i].y;
                if (GridManager.InBounds(cx, cy))
                {
                    _cells[cx, cy].SetHoverTint(overlay, valid, _selectedColor);
                    _hoveredFootprint.Add(new Vector2Int(cx, cy));
                }
            }
        }

        public void OnCellHoverExit(int x, int y)
        {
            ClearHover();
        }

        private void ClearHover()
        {
            for (int i = 0; i < _hoveredFootprint.Count; i++)
            {
                var pos = _hoveredFootprint[i];
                RefreshCell(pos.x, pos.y);
            }
            _hoveredFootprint.Clear();
        }

        public void OnCellClicked(int x, int y)
        {
            CellClicked?.Invoke(x, y);
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
    }
}
