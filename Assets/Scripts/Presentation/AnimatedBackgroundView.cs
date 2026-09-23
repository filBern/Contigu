using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Soft, slowly drifting color blobs behind the whole UI (on explicit
    /// request: "fond d'écran dynamique, qui bouge un peu, un peu comme
    /// pour le jeu WordPlay" — a gently moving gradient-blob background
    /// instead of the previous flat UITheme.Background fill). Built as a
    /// direct child of the Canvas, right after the flat Background panel
    /// and before MainRoot (see GameBootstrap.BuildCanvas), so every blob
    /// sits ABOVE the flat color fill but BELOW every real UI
    /// element — it only ever shows through the negative space around the
    /// grid/HUD/hand, never behind or on top of anything the player reads.
    ///
    /// Each blob is BackgroundBlobFactory's shared soft-circle sprite,
    /// tinted with one of the game's own piece colors (VisualDefaults) at
    /// low alpha so it reads as ambient color rather than competing with
    /// foreground contrast, and drifts along a simple per-blob sine/cosine
    /// loop — cheap enough to run every frame with no animation asset or
    /// physics needed, and loops forever with no seam or reset.
    /// </summary>
    public sealed class AnimatedBackgroundView : MonoBehaviour
    {
        private const float BlobSize = 620f;
        private const float BlobAlpha = 0.16f;

        private struct BlobMotion
        {
            public RectTransform Rect;
            public Vector2 Center;
            public Vector2 Amplitude;
            public Vector2 PeriodSeconds;
            public Vector2 Phase;
        }

        // One entry per blob: base position, drift amplitude, period (how
        // long a full loop takes) and phase (so blobs never move in sync).
        // Hand-picked to spread across the 1280x800 reference canvas and
        // drift slowly — "bouge un peu", not a distracting animation.
        private static readonly (Vector2 Center, Vector2 Amplitude, Vector2 Period, Vector2 Phase, PieceColor Color)[] BlobSpecs =
        {
            (new Vector2(-360f, 230f), new Vector2(90f, 70f), new Vector2(22f, 27f), new Vector2(0f, 1.3f), PieceColor.Coral),
            (new Vector2(380f, 260f), new Vector2(70f, 100f), new Vector2(26f, 19f), new Vector2(2.1f, 0.4f), PieceColor.Teal),
            (new Vector2(-320f, -250f), new Vector2(100f, 80f), new Vector2(20f, 24f), new Vector2(3.6f, 2.8f), PieceColor.Violet),
            (new Vector2(340f, -220f), new Vector2(80f, 90f), new Vector2(24f, 21f), new Vector2(1.1f, 4.2f), PieceColor.Lime)
        };

        private BlobMotion[] _blobs;

        public void Build(Transform parent)
        {
            var container = UIFactory.CreateUIObject("AnimatedBackground", parent);
            UIFactory.StretchFull(container);

            var sprite = BackgroundBlobFactory.GetBlobSprite();
            _blobs = new BlobMotion[BlobSpecs.Length];

            for (int i = 0; i < BlobSpecs.Length; i++)
            {
                var spec = BlobSpecs[i];
                var blobRt = UIFactory.CreateUIObject("Blob" + i, container);
                var img = blobRt.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                var color = VisualDefaults.GetColor(spec.Color);
                color.a = BlobAlpha;
                img.color = color;

                var rect = img.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(BlobSize, BlobSize);
                rect.anchoredPosition = spec.Center;

                _blobs[i] = new BlobMotion
                {
                    Rect = rect,
                    Center = spec.Center,
                    Amplitude = spec.Amplitude,
                    PeriodSeconds = spec.Period,
                    Phase = spec.Phase
                };
            }
        }

        private void Update()
        {
            if (_blobs == null)
            {
                return;
            }

            float t = Time.time;
            for (int i = 0; i < _blobs.Length; i++)
            {
                var blob = _blobs[i];
                float x = blob.Center.x + Mathf.Sin(t / blob.PeriodSeconds.x * Mathf.PI * 2f + blob.Phase.x) * blob.Amplitude.x;
                float y = blob.Center.y + Mathf.Cos(t / blob.PeriodSeconds.y * Mathf.PI * 2f + blob.Phase.y) * blob.Amplitude.y;
                blob.Rect.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
