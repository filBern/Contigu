using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Slowly drifting, slowly rotating geometric shapes behind the whole
    /// UI (spec extension, explicit request — first a "fond d'écran
    /// dynamique, qui bouge un peu, un peu comme pour le jeu WordPlay",
    /// then revised: "je n'aime pas le background, j'aimerais quelque
    /// chose de plus geometrique qui joue avec les grosseurs et positions
    /// de shapes et qui est légèrement plus clair que le plain background
    /// qu'il y avait avant. On garde le plain background aussi comme
    /// base" — the first pass' soft colored blur-edge blobs are replaced
    /// here with crisp circles/squares/triangles/diamonds in varied sizes
    /// and positions, all tinted a single tone-on-tone shade of
    /// UITheme.Background rather than the game's own piece colors).
    ///
    /// Built as a direct child of the Canvas, right after the flat
    /// Background panel and before MainRoot (see
    /// GameBootstrap.BuildCanvas) — that flat panel is kept as the base
    /// layer underneath, per the explicit "on garde le plain background
    /// aussi comme base" — so every shape sits ABOVE the flat fill but
    /// BELOW every real UI element: it only ever shows through the
    /// negative space around the grid/HUD/hand, never over anything the
    /// player reads.
    /// </summary>
    public sealed class AnimatedBackgroundView : MonoBehaviour
    {
        // Slightly lighter than the flat UITheme.Background fill it sits
        // on top of ("légèrement plus clair que le plain background"), at
        // a low alpha so overlapping shapes read as subtle layered depth
        // rather than flat opaque stickers.
        private const float LightenAmount = 0.16f;
        private const float ShapeAlpha = 0.5f;

        private struct ShapeMotion
        {
            public RectTransform Rect;
            public Vector2 Center;
            public Vector2 Amplitude;
            public Vector2 PeriodSeconds;
            public Vector2 Phase;
            public float RotationDegreesPerSecond;
        }

        // One entry per shape: which silhouette (see BackgroundShapeFactory
        // — the PieceColor here is purely a shape selector), its size, base
        // position, drift amplitude/period/phase (so no two shapes move in
        // sync), and a slow rotation speed — "joue avec les grosseurs et
        // positions de shapes", spread across the 1280x800 reference
        // canvas mostly in the margins around the central grid/HUD/hand
        // column, plus a couple of small ones tucked closer to center
        // (harmless — anything covered by real UI just never shows).
        private static readonly (PieceColor Shape, float Size, Vector2 Center, Vector2 Amplitude, Vector2 Period, Vector2 Phase, float RotationSpeed)[] Specs =
        {
            (PieceColor.Coral, 260f, new Vector2(-480f, 280f), new Vector2(70f, 60f), new Vector2(24f, 30f), new Vector2(0f, 1.3f), 2.5f),
            (PieceColor.Violet, 300f, new Vector2(500f, 300f), new Vector2(60f, 80f), new Vector2(28f, 21f), new Vector2(2.1f, 0.4f), -1.8f),
            (PieceColor.Teal, 170f, new Vector2(-520f, -60f), new Vector2(50f, 60f), new Vector2(19f, 25f), new Vector2(1.6f, 3.1f), 3.4f),
            (PieceColor.Lime, 210f, new Vector2(520f, -90f), new Vector2(65f, 45f), new Vector2(23f, 18f), new Vector2(3.6f, 2.8f), -2.9f),
            (PieceColor.Violet, 320f, new Vector2(-460f, -300f), new Vector2(55f, 70f), new Vector2(26f, 22f), new Vector2(1.1f, 4.2f), 1.4f),
            (PieceColor.Coral, 150f, new Vector2(480f, -280f), new Vector2(45f, 55f), new Vector2(21f, 27f), new Vector2(4.4f, 0.9f), -3.6f),
            (PieceColor.Teal, 120f, new Vector2(-260f, 360f), new Vector2(35f, 40f), new Vector2(17f, 20f), new Vector2(2.9f, 1.7f), 2.1f),
            (PieceColor.Lime, 140f, new Vector2(270f, -360f), new Vector2(40f, 35f), new Vector2(20f, 16f), new Vector2(0.6f, 3.9f), -1.6f)
        };

        private ShapeMotion[] _shapes;

        public void Build(Transform parent)
        {
            var container = UIFactory.CreateUIObject("AnimatedBackground", parent);
            UIFactory.StretchFull(container);

            var tint = Color.Lerp(UITheme.Background, Color.white, LightenAmount);
            tint.a = ShapeAlpha;

            _shapes = new ShapeMotion[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];
                var shapeRt = UIFactory.CreateUIObject("Shape" + i, container);
                var img = shapeRt.gameObject.AddComponent<Image>();
                img.sprite = BackgroundShapeFactory.GetShape(spec.Shape);
                img.type = Image.Type.Simple;
                img.color = tint;

                var rect = img.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(spec.Size, spec.Size);
                rect.anchoredPosition = spec.Center;

                _shapes[i] = new ShapeMotion
                {
                    Rect = rect,
                    Center = spec.Center,
                    Amplitude = spec.Amplitude,
                    PeriodSeconds = spec.Period,
                    Phase = spec.Phase,
                    RotationDegreesPerSecond = spec.RotationSpeed
                };
            }
        }

        private void Update()
        {
            if (_shapes == null)
            {
                return;
            }

            float t = Time.time;
            for (int i = 0; i < _shapes.Length; i++)
            {
                var shape = _shapes[i];
                float x = shape.Center.x + Mathf.Sin(t / shape.PeriodSeconds.x * Mathf.PI * 2f + shape.Phase.x) * shape.Amplitude.x;
                float y = shape.Center.y + Mathf.Cos(t / shape.PeriodSeconds.y * Mathf.PI * 2f + shape.Phase.y) * shape.Amplitude.y;
                shape.Rect.anchoredPosition = new Vector2(x, y);
                shape.Rect.localRotation = Quaternion.Euler(0f, 0f, t * shape.RotationDegreesPerSecond);
            }
        }
    }
}
