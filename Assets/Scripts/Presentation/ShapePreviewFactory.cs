using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders a small grid-square preview of a piece shape/color — same look
    /// as a filled grid cell (flat color fill + colorblind icon) — with an
    /// optional enchanted-tile trait badge overlaid on its LocalCellIndex
    /// square. Shared by HandView (a dealt hand piece, its own rotation) and
    /// DraftView (a deck-composition row in the Retirer/Dupliquer/Recolorer
    /// type picker, always shown unrotated).
    /// </summary>
    public static class ShapePreviewFactory
    {
        /// <summary>
        /// Fills <paramref name="container"/> (its sizeDelta sets the preview's
        /// pixel budget — cell size is computed to fit inside it) with one
        /// square per cell of <paramref name="shape"/>. When
        /// <paramref name="trait"/> is given, its enchanted cell also gets a
        /// small corner badge; hovering it shows <paramref name="tooltip"/>
        /// with the trait's name/effect, and clicking it forwards the click to
        /// <paramref name="clickForwardTarget"/> so the badge never swallows a
        /// click meant for whatever bigger clickable element it sits inside.
        /// </summary>
        public static void Build(RectTransform container, PieceShape shape, PieceColor color, PieceTrait? trait, TooltipView tooltip, GameObject clickForwardTarget)
        {
            int maxX = 0;
            int maxY = 0;
            var occupied = new HashSet<Vector2Int>();
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                occupied.Add(c);
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            int cols = maxX + 1;
            int rows = maxY + 1;
            float cell = Mathf.Min(container.sizeDelta.x / cols, container.sizeDelta.y / rows);
            var fillColor = VisualDefaults.GetColor(color);

            Vector2Int? traitPos = trait.HasValue ? (Vector2Int?)shape.Cells[trait.Value.LocalCellIndex] : null;

            float startX = -(cols * cell) / 2f + cell / 2f;
            // Y increases UPWARD, matching GridView's own convention — otherwise
            // shapes render vertically flipped from how they actually look once
            // placed on the grid.
            float startY = -(rows * cell) / 2f + cell / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    bool filled = occupied.Contains(new Vector2Int(x, y));
                    var img = UIFactory.CreatePanel(container, "c" + x + "_" + y, filled ? fillColor : new Color(1f, 1f, 1f, 0.05f));
                    img.rectTransform.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                    img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchoredPosition = new Vector2(startX + x * cell, startY + y * cell);

                    if (filled)
                    {
                        // Same colorblind-accessibility icon as a filled grid
                        // cell (see GridCellView). Skipped for a color with no
                        // icon yet (Coral).
                        var icon = VisualDefaults.GetColorIcon(color);
                        if (icon != null)
                        {
                            var iconImg = UIFactory.CreatePanel(img.transform, "Icon", Color.white);
                            iconImg.sprite = icon;
                            UIFactory.StretchFull(iconImg.rectTransform);
                        }

                        if (traitPos.HasValue && traitPos.Value == new Vector2Int(x, y))
                        {
                            // Sized relative to the cell itself rather than a
                            // fixed 14px — at HandView's larger preview box
                            // that clamps out to the same 14px as before, but
                            // at DraftView's much smaller type-row preview a
                            // fixed 14px badge would nearly cover the whole
                            // (~15px) cell.
                            float badgeSize = Mathf.Clamp(cell * 0.55f, 8f, 14f);
                            BuildTraitBadge(img.transform, trait.Value, tooltip, clickForwardTarget, badgeSize);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Simplified sibling of <see cref="Build"/> — just the shape's
        /// silhouette as solid squares in one flat color (no piece color,
        /// colorblind icon or trait badge) — used where something needs to
        /// show WHICH shape it targets instead of naming it in text (see
        /// ModifierBadgeFactory, the Forme* "Specialist" modifiers). Only
        /// filled cells get a square; unlike Build there's no dimmed
        /// placeholder for empty cells in the bounding box.
        /// </summary>
        public static void BuildMono(RectTransform container, PieceShape shape, Color squareColor)
        {
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            int cols = maxX + 1;
            int rows = maxY + 1;
            float cell = Mathf.Min(container.sizeDelta.x / cols, container.sizeDelta.y / rows);

            float startX = -(cols * cell) / 2f + cell / 2f;
            float startY = -(rows * cell) / 2f + cell / 2f;

            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                var img = UIFactory.CreatePanel(container, "c" + c.x + "_" + c.y, squareColor);
                img.rectTransform.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.rectTransform.anchoredPosition = new Vector2(startX + c.x * cell, startY + c.y * cell);
            }
        }

        private static void BuildTraitBadge(Transform parent, PieceTrait trait, TooltipView tooltip, GameObject clickForwardTarget, float size)
        {
            var badge = UIFactory.CreatePanel(parent, "TraitBadge", PieceTraitVisualDefaults.GetBadgeColor(trait));
            badge.rectTransform.anchorMin = new Vector2(0f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0f, 1f);
            badge.rectTransform.pivot = new Vector2(0f, 1f);
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            badge.rectTransform.anchoredPosition = new Vector2(1f, -1f);
            var outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(size * 0.09f, -size * 0.09f);

            var badgeView = badge.gameObject.AddComponent<TraitBadgeView>();
            badgeView.Init(tooltip, trait, clickForwardTarget);
        }
    }
}
