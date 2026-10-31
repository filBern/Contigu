using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Presentation
{
    public enum BackgroundSprite
    {
        Leaf,
        Petal,
        Flower,
        Glow
    }

    /// <summary>
    /// Procedurally drawn garden silhouettes for the animated background
    /// (see AnimatedBackgroundView): a pointed leaf, a teardrop petal, a
    /// five-lobed flower and a soft round glow (firefly/pollen). All white
    /// with an alpha mask so the view can tint each instance freely; drawn
    /// at a higher texture resolution than the old 32px piece sprites since
    /// the big decorative ones stretch to 200-320px.
    /// </summary>
    public static class BackgroundShapeFactory
    {
        private const int TextureSize = 128;
        private static readonly Dictionary<BackgroundSprite, Sprite> Cache = new Dictionary<BackgroundSprite, Sprite>();

        public static Sprite GetShape(BackgroundSprite kind)
        {
            if (Cache.TryGetValue(kind, out var cached))
            {
                return cached;
            }

            var sprite = Build(kind);
            Cache[kind] = sprite;
            return sprite;
        }

        private static Sprite Build(BackgroundSprite kind)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            // Coverage is sampled on a 3x3 grid per pixel so the silhouette edge is anti-aliased instead of stair-stepped.
            const int Samples = 3;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float alpha = 0f;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float dx = (x + (sx + 0.5f) / Samples) / TextureSize - 0.5f;
                            float dy = (y + (sy + 0.5f) / Samples) / TextureSize - 0.5f;
                            alpha += Coverage(kind, dx, dy);
                        }
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha / (Samples * Samples)));
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }

        /// <summary>Alpha (0..1) of the given silhouette at (dx, dy), both in [-0.5, 0.5] from the sprite's center.</summary>
        private static float Coverage(BackgroundSprite kind, float dx, float dy)
        {
            switch (kind)
            {
                case BackgroundSprite.Leaf:
                {
                    // Pointed at both ends, widest at the middle; a thin midrib is carved out along the axis.
                    float t = dx * 2f; // -1..1 along the leaf
                    float halfWidth = 0.27f * (1f - t * t);
                    if (halfWidth <= 0f || Mathf.Abs(dy) > halfWidth)
                    {
                        return 0f;
                    }
                    bool midrib = Mathf.Abs(dy) < 0.012f && Mathf.Abs(t) < 0.85f;
                    return midrib ? 0.35f : 1f;
                }
                case BackgroundSprite.Petal:
                {
                    // Teardrop: narrow at the base (left), full and round at the tip (right).
                    float t = dx + 0.5f; // 0..1 base -> tip
                    if (t < 0f || t > 1f)
                    {
                        return 0f;
                    }
                    float halfWidth = 0.34f * Mathf.Pow(Mathf.Sin(t * Mathf.PI * 0.5f), 0.9f) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(t, 6f)));
                    return Mathf.Abs(dy) <= halfWidth ? 1f : 0f;
                }
                case BackgroundSprite.Flower:
                {
                    // Five rounded lobes around a small hollow center.
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float outer = 0.30f + 0.17f * Mathf.Cos(5f * angle);
                    if (r > outer || r < 0.05f)
                    {
                        return 0f;
                    }
                    return 1f;
                }
                default:
                {
                    // Soft radial glow with a brighter core.
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    if (r >= 1f)
                    {
                        return 0f;
                    }
                    float falloff = 1f - r;
                    return falloff * falloff;
                }
            }
        }
    }
}
