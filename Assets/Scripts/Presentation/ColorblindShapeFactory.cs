using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Small black geometric shapes for ColorblindMode's per-color symbol
    /// (on explicit request: "j'aimerais qu'on ne réutilise pas les icon
    /// que j'avais fait, j'aimerais plus que tu fasse des petites formes
    /// géométrique noir au milieu de la tuile un peu comme un jeu de
    /// carte" — replaces the earlier one-letter-per-color text label,
    /// deliberately NOT the old deleted per-color icon files: these are
    /// generated procedurally at runtime instead of loaded from any art
    /// asset, so nothing here reuses that old set). One white-on-
    /// transparent shape per PieceColor, tinted black by the Image that
    /// displays it (see GridCellView/ShapePreviewFactory) — built once and
    /// cached, since every filled tile of the same color reuses the
    /// identical sprite.
    /// </summary>
    public static class ColorblindShapeFactory
    {
        private const int TextureSize = 32;
        private static readonly Dictionary<PieceColor, Sprite> Cache = new Dictionary<PieceColor, Sprite>();

        public static Sprite GetShape(PieceColor color)
        {
            if (Cache.TryGetValue(color, out var cached))
            {
                return cached;
            }

            var sprite = Build(color);
            Cache[color] = sprite;
            return sprite;
        }

        private static Sprite Build(PieceColor color)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float cx = (x + 0.5f) / TextureSize - 0.5f;
                    float cy = (y + 0.5f) / TextureSize - 0.5f;
                    bool inside = IsInsideShape(color, cx, cy);
                    texture.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }

        /// <summary>
        /// <paramref name="dx"/>/<paramref name="dy"/> are offsets from the
        /// texture's own center, in the [-0.5, 0.5] unit range one axis'
        /// full size covers — five simple, unambiguous silhouettes, one
        /// per PieceColor: circle, square, upward triangle, diamond
        /// (Coral/Teal/Violet/Lime, matching their existing red/blue/
        /// yellow/green display order — see VisualDefaults.ColorMap), and
        /// a cross for Joker, the one color the other four shapes don't
        /// use so it never gets mistaken for a "real" piece color.
        /// Internal (not private): BackgroundShapeFactory reuses this exact
        /// point-in-shape math at a much higher texture resolution for the
        /// animated background's own geometric shapes — <paramref
        /// name="color"/> is just a shape SELECTOR there, its actual
        /// piece-color meaning is irrelevant to that caller.
        /// </summary>
        internal static bool IsInsideShape(PieceColor color, float dx, float dy)
        {
            switch (color)
            {
                case PieceColor.Coral:
                    return dx * dx + dy * dy <= 0.30f * 0.30f;
                case PieceColor.Teal:
                    return Mathf.Abs(dx) <= 0.28f && Mathf.Abs(dy) <= 0.28f;
                case PieceColor.Violet:
                    return IsInsideUpwardTriangle(dx, dy);
                case PieceColor.Lime:
                    return Mathf.Abs(dx) + Mathf.Abs(dy) <= 0.34f;
                case PieceColor.Joker:
                    return (Mathf.Abs(dx) <= 0.10f && Mathf.Abs(dy) <= 0.34f)
                        || (Mathf.Abs(dy) <= 0.10f && Mathf.Abs(dx) <= 0.34f);
                default:
                    return false;
            }
        }

        /// <summary>Point-in-triangle test (sign-of-cross-product method) against a fixed upward-pointing triangle centered on the same origin every other shape uses.</summary>
        private static bool IsInsideUpwardTriangle(float dx, float dy)
        {
            var apex = new Vector2(0f, 0.36f);
            var baseLeft = new Vector2(-0.32f, -0.24f);
            var baseRight = new Vector2(0.32f, -0.24f);
            var p = new Vector2(dx, dy);

            float d1 = Cross(p - apex, baseLeft - apex);
            float d2 = Cross(p - baseLeft, baseRight - baseLeft);
            float d3 = Cross(p - baseRight, apex - baseRight);

            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
