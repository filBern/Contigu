using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders a small grid-square preview of a piece shape/color — same look
    /// as a filled grid cell (VisualDefaults.TileSprite tinted the piece's
    /// color) — with an optional enchanted-tile trait badge overlaid on its
    /// LocalCellIndex square. Shared by HandView (a dealt hand piece, its own
    /// rotation) and DraftView (a deck-composition row in the Retirer/
    /// Dupliquer/Recolorer type picker, always shown unrotated).
    /// </summary>
    public static class ShapePreviewFactory
    {
        /// <summary>
        /// Fills <paramref name="container"/> (its sizeDelta sets the preview's
        /// pixel budget; cell size is computed to fit inside it unless
        /// <paramref name="fixedCellSize"/> is supplied) with one square per
        /// cell of <paramref name="shape"/>. When <paramref name="trait"/> is
        /// given, its enchanted cell also gets a small corner badge; every
        /// cell gets one instead for a Joker-exclusive combat kind (see
        /// PieceTrait.IsJokerCombatKind), since those tag the whole piece
        /// rather than one specific cell. Hovering a badge shows
        /// <paramref name="tooltip"/>; clicking it forwards the click to
        /// <paramref name="clickForwardTarget"/> so the badge never swallows
        /// a click meant for a bigger clickable element it sits inside.
        /// Returns the last badge built's RectTransform (null if
        /// <paramref name="trait"/> is null). <paramref name="fixedCellSize"/>
        /// lets compact catalogs use equal tile dimensions across shapes of
        /// different bounding-box sizes.
        /// </summary>
        public static RectTransform Build(RectTransform container, PieceShape shape, PieceColor color, PieceTrait? trait, TooltipView tooltip, GameObject clickForwardTarget, float? fixedCellSize = null)
        {
            return BuildWithTraits(container, shape, color, trait.HasValue ? new[] { trait.Value } : null, tooltip, clickForwardTarget, fixedCellSize);
        }

        /// <summary>
        /// One tile size for every piece shown in a box of <paramref name="box"/> (shop choices): a 3-wide piece just overflows its box a little,
        /// a 2-wide one is no bigger per tile, and a 4-wide one overflows more rather than shrinking. Capped at the real grid's cell size.
        /// </summary>
        public static float UniformTile(float box)
        {
            if (TileOverride > 0f)
            {
                return TileOverride;
            }
            return Mathf.Min(VisualDefaults.GridCellSize * 4f / 3f, box * 1.12f * 4f / 3f / 3f);
        }

        /// <summary>While set (&gt; 0), <see cref="UniformTile"/> returns it: a shop choice picks one tile size for all its pieces (see ShelfStage) before drawing them.</summary>
        public static float TileOverride;

        /// <summary>How many tiles wide a shape is.</summary>
        public static int ColumnsOf(PieceShape shape)
        {
            int columns = 1;
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                columns = Mathf.Max(columns, shape.Cells[i].x + 1);
            }
            return columns;
        }

        /// <summary>Same as <see cref="Build"/> for a piece carrying several upgrades, one badge on each upgraded cell.</summary>
        public static RectTransform BuildWithTraits(RectTransform container, PieceShape shape, PieceColor color, IReadOnlyList<PieceTrait> traits, TooltipView tooltip, GameObject clickForwardTarget, float? fixedCellSize = null)
        {
            RectTransform builtBadge = null;
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
            // Capped at the real grid's own cell size so a small shape never stretches larger than it renders once placed.
            float cell = fixedCellSize.HasValue
                ? fixedCellSize.Value
                : Mathf.Min(container.sizeDelta.x / cols, container.sizeDelta.y / rows, VisualDefaults.GridCellSize);

            PieceTrait? everyCellTrait = null;
            var traitAt = new Dictionary<Vector2Int, PieceTrait>();
            for (int t = 0; traits != null && t < traits.Count; t++)
            {
                if (PieceTrait.IsJokerCombatKind(traits[t].Kind))
                {
                    everyCellTrait = traits[t];
                }
                else
                {
                    traitAt[shape.Cells[traits[t].LocalCellIndex]] = traits[t];
                }
            }

            float startX = -(cols * cell) / 2f + cell / 2f;
            // Y increases upward, matching GridView's convention; otherwise shapes render vertically flipped from how they look once placed.
            float startY = -(rows * cell) / 2f + cell / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // An unfilled bounding-box square (a shape like an L-tromino has some) draws nothing at all.
                    if (!occupied.Contains(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    // Same card art/tint as a filled grid cell (see GridCellView.ApplyState).
                    var img = UIFactory.CreateSlicedImage(container, "c" + x + "_" + y, VisualDefaults.GetTileSprite(color));
                    img.color = VisualDefaults.GetTileTint(color);
                    img.rectTransform.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                    img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchoredPosition = new Vector2(startX + x * cell, startY + y * cell);

                    PieceTrait cellTrait = default(PieceTrait);
                    bool hasCellTrait = everyCellTrait.HasValue;
                    if (hasCellTrait)
                    {
                        cellTrait = everyCellTrait.Value;
                    }
                    else if (traitAt.TryGetValue(new Vector2Int(x, y), out var found))
                    {
                        hasCellTrait = true;
                        cellTrait = found;
                    }
                    if (hasCellTrait)
                    {
                        // About a third of the cell (as on the grid), so the upgrade stays readable on big previews and a small preview cell is not covered by it.
                        float badgeSize = Mathf.Clamp(cell * 0.34f, 10f, 30f);
                        builtBadge = BuildTraitBadge(img.transform, cellTrait, tooltip, clickForwardTarget, badgeSize);
                    }
                }
            }

            return builtBadge;
        }

        /// <summary>Simplified sibling of <see cref="Build"/>: just the shape's silhouette as solid squares in one flat color (no piece color, colorblind icon or trait badge). Only filled cells get a square.</summary>
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
            // Same real-size cap as Build above.
            float cell = Mathf.Min(container.sizeDelta.x / cols, container.sizeDelta.y / rows, VisualDefaults.GridCellSize);

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

        private static RectTransform BuildTraitBadge(Transform parent, PieceTrait trait, TooltipView tooltip, GameObject clickForwardTarget, float size)
        {
            // Top-right corner, same convention as GridCellView's placed trait-origin badge and its hover-preview equivalent.
            var badgeSprite = PieceTraitVisualDefaults.GetBadgeSprite(trait.Kind);
            var badge = UIFactory.CreatePanel(parent, "TraitBadge", badgeSprite != null ? Color.white : PieceTraitVisualDefaults.GetBadgeColor(trait));
            badge.sprite = badgeSprite;
            badge.rectTransform.anchorMin = new Vector2(1f, 1f);
            badge.rectTransform.anchorMax = new Vector2(1f, 1f);
            badge.rectTransform.pivot = new Vector2(1f, 1f);
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            badge.rectTransform.anchoredPosition = new Vector2(-1f, -1f);
            var outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(size * 0.09f, -size * 0.09f);

            var badgeView = badge.gameObject.AddComponent<TraitBadgeView>();
            badgeView.Init(tooltip, trait, clickForwardTarget);
            return badge.rectTransform;
        }
    }
}
