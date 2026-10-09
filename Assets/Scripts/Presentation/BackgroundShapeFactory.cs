using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Crisp, larger-resolution geometric shape sprites for the animated
    /// background (see AnimatedBackgroundView). Reuses ColorblindShapeFactory.
    /// IsInsideShape's circle/square/triangle/diamond point tests — that
    /// class's PieceColor parameter is used here purely as a shape selector
    /// (Coral=circle, Teal=square, Violet=triangle, Lime=diamond), its actual
    /// piece-color meaning is irrelevant here — but at a much higher texture
    /// resolution (256 vs. 32), since a background shape stretches to
    /// 150-500px where a 32px source would look blurry.
    /// </summary>
    public static class BackgroundShapeFactory
    {
        private const int TextureSize = 256;
        private static readonly Dictionary<PieceColor, Sprite> Cache = new Dictionary<PieceColor, Sprite>();

        public static Sprite GetShape(PieceColor shapeSelector)
        {
            if (Cache.TryGetValue(shapeSelector, out var cached))
            {
                return cached;
            }

            var sprite = Build(shapeSelector);
            Cache[shapeSelector] = sprite;
            return sprite;
        }

        private static Sprite Build(PieceColor shapeSelector)
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
                    float dx = (x + 0.5f) / TextureSize - 0.5f;
                    float dy = (y + 0.5f) / TextureSize - 0.5f;
                    bool inside = ColorblindShapeFactory.IsInsideShape(shapeSelector, dx, dy);
                    texture.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }
    }
}
