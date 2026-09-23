using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// One soft, feathered-edge circle sprite (radial alpha falloff via
    /// smoothstep, no hard rim) for AnimatedBackgroundView's drifting
    /// color blobs — generated procedurally at runtime, same "no art
    /// asset" approach as ColorblindShapeFactory. A single shared sprite
    /// is enough: every blob just scales/tints/moves the same texture, so
    /// unlike ColorblindShapeFactory there's no per-color cache needed.
    /// </summary>
    public static class BackgroundBlobFactory
    {
        private const int TextureSize = 128;
        private static Sprite _cached;

        public static Sprite GetBlobSprite()
        {
            if (_cached != null)
            {
                return _cached;
            }

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
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * (3f - 2f * alpha); // smoothstep — soft, blurred-looking edge instead of a hard-edged disc
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            _cached = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _cached;
        }
    }
}
