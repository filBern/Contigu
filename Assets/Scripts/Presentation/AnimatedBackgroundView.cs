using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// A night in the last garden, drifting behind the whole UI: faint
    /// oversized flowers and leaves turning slowly in the dark, petals and
    /// leaves tinted with the four bloom colors (Ember/Frost/Gold/Moss)
    /// falling and swaying down the screen, and fireflies of golden light
    /// pulsing as they wander.
    ///
    /// Built as a direct child of the Canvas, right after the flat
    /// Background panel and before MainRoot (see GameBootstrap.BuildCanvas),
    /// so everything sits above the flat fill but below every real UI
    /// element: it only shows through the negative space around the
    /// grid/HUD/hand, never over anything the player reads. Shared by the
    /// game and the main menu (see MainMenuBootstrap).
    /// </summary>
    public sealed class AnimatedBackgroundView : MonoBehaviour
    {
        // Large decor: tone-on-tone moss green, faint enough to read as depth rather than as objects.
        private const float DecorGreenAmount = 0.22f;
        private const float DecorAlpha = 0.5f;
        private const float FallingAlpha = 0.42f;
        // How far past the visible edge (canvas units) a falling petal travels before it wraps back to the top.
        private const float WrapMargin = 60f;
        private static readonly Color DefaultFireflyColor = new Color(1f, 0.85f, 0.4f);
        private Color _fireflyColor = DefaultFireflyColor;

        private struct DecorMotion
        {
            public RectTransform Rect;
            public Vector2 Center;
            public Vector2 Amplitude;
            public Vector2 PeriodSeconds;
            public Vector2 Phase;
            public float RotationDegreesPerSecond;
        }

        private struct FallingMotion
        {
            public RectTransform Rect;
            public float StartX;
            public float FallSpeed;
            public float SwayAmplitude;
            public float SwayPeriod;
            public float Phase;
            public float StartY;
            public float SpinDegreesPerSecond;
            public float BaseRotation;
        }

        private struct FireflyMotion
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Center;
            public Vector2 Amplitude;
            public Vector2 PeriodSeconds;
            public Vector2 Phase;
            public float PulsePeriod;
            public float PulsePhase;
            public float MaxAlpha;
        }

        // Large faint silhouettes: which sprite, size, base position, drift amplitude/period/phase (so no two move in sync), slow rotation.
        private static readonly (BackgroundSprite Sprite, float Size, Vector2 Center, Vector2 Amplitude, Vector2 Period, Vector2 Phase, float RotationSpeed)[] Decor =
        {
            (BackgroundSprite.Flower, 280f, new Vector2(-500f, 280f), new Vector2(50f, 40f), new Vector2(26f, 32f), new Vector2(0f, 1.3f), 2.5f),
            (BackgroundSprite.Leaf, 340f, new Vector2(500f, 290f), new Vector2(60f, 70f), new Vector2(30f, 23f), new Vector2(2.1f, 0.4f), -1.8f),
            (BackgroundSprite.Leaf, 220f, new Vector2(-540f, -70f), new Vector2(45f, 55f), new Vector2(21f, 27f), new Vector2(1.6f, 3.1f), 3.0f),
            (BackgroundSprite.Flower, 240f, new Vector2(530f, -100f), new Vector2(55f, 45f), new Vector2(25f, 19f), new Vector2(3.6f, 2.8f), -2.4f),
            (BackgroundSprite.Leaf, 360f, new Vector2(-470f, -300f), new Vector2(55f, 60f), new Vector2(28f, 24f), new Vector2(1.1f, 4.2f), 1.4f),
            (BackgroundSprite.Flower, 190f, new Vector2(480f, -290f), new Vector2(40f, 50f), new Vector2(22f, 28f), new Vector2(4.4f, 0.9f), -3.2f)
        };

        private const int FallingCount = 18;
        private const int FireflyCount = 20;

        private RectTransform _container;
        private DecorMotion[] _decor;
        private FallingMotion[] _falling;
        private FireflyMotion[] _fireflies;

        public void Build(Transform parent)
        {
            _container = UIFactory.CreateUIObject("AnimatedBackground", parent);
            UIFactory.StretchFull(_container);

            // Fixed seed: the same arrangement every launch, so the menu and the game feel like the same garden.
            var rng = new System.Random(1337);

            BuildDecor();
            BuildFalling(rng);
            BuildFireflies(rng);
        }

        /// <summary>Re-tones the drifting decor, falling petals/leaves and fireflies for a challenge's world (see ChallengeAmbiance); null tints keep the four bloom colors.</summary>
        public void ApplyTheme(ChallengeAmbiance ambiance)
        {
            if (_decor == null)
            {
                return;
            }
            _fireflyColor = ambiance.FireflyColor;
            int visibleFireflies = Mathf.RoundToInt(_fireflies.Length * Mathf.Clamp01(ambiance.FireflyFraction));
            for (int i = 0; i < _fireflies.Length; i++)
            {
                _fireflies[i].Image.enabled = i < visibleFireflies;
            }
            var tint = Color.Lerp(UITheme.Background, ambiance.DecorTone, ambiance.DecorAmount);
            tint.a = DecorAlpha;
            for (int i = 0; i < _decor.Length; i++)
            {
                var decorImage = _decor[i].Rect.GetComponent<Image>();
                decorImage.color = tint;
                decorImage.sprite = BackgroundShapeFactory.GetShape(DecorShape(ambiance.DecorStyle, Decor[i].Sprite));
            }
            var bloomColors = new[] { PieceColor.Coral, PieceColor.Violet, PieceColor.Lime, PieceColor.Teal };
            for (int i = 0; i < _falling.Length; i++)
            {
                var color = ambiance.FallingTints != null && ambiance.FallingTints.Length > 0
                    ? ambiance.FallingTints[i % ambiance.FallingTints.Length]
                    : VisualDefaults.GetColor(bloomColors[i % bloomColors.Length]);
                color.a = FallingAlpha;
                var fallingImage = _falling[i].Rect.GetComponent<Image>();
                fallingImage.color = color;
                fallingImage.sprite = BackgroundShapeFactory.GetShape(FallingShape(ambiance.FallingStyle, i));
            }
        }

        private static BackgroundSprite DecorShape(BackgroundShapeStyle style, BackgroundSprite original)
        {
            switch (style)
            {
                case BackgroundShapeStyle.Leaves: return BackgroundSprite.Leaf;
                case BackgroundShapeStyle.Petals: return BackgroundSprite.Flower;
                case BackgroundShapeStyle.Soft: return BackgroundSprite.Glow;
                default: return original;
            }
        }

        private static BackgroundSprite FallingShape(BackgroundShapeStyle style, int index)
        {
            switch (style)
            {
                case BackgroundShapeStyle.Leaves: return BackgroundSprite.Leaf;
                case BackgroundShapeStyle.Petals: return BackgroundSprite.Petal;
                case BackgroundShapeStyle.Soft: return BackgroundSprite.Glow;
                default: return index % 3 == 0 ? BackgroundSprite.Leaf : BackgroundSprite.Petal;
            }
        }

        private void BuildDecor()
        {
            var moss = VisualDefaults.GetColor(PieceColor.Lime);
            var tint = Color.Lerp(UITheme.Background, moss, DecorGreenAmount);
            tint.a = DecorAlpha;

            _decor = new DecorMotion[Decor.Length];
            for (int i = 0; i < Decor.Length; i++)
            {
                var spec = Decor[i];
                var rect = CreateSprite("Decor" + i, spec.Sprite, spec.Size, tint);
                rect.anchoredPosition = spec.Center;
                _decor[i] = new DecorMotion
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

        private void BuildFalling(System.Random rng)
        {
            var colors = new[] { PieceColor.Coral, PieceColor.Violet, PieceColor.Lime, PieceColor.Teal };
            _falling = new FallingMotion[FallingCount];
            for (int i = 0; i < FallingCount; i++)
            {
                var color = VisualDefaults.GetColor(colors[i % colors.Length]);
                color.a = FallingAlpha;
                var sprite = i % 3 == 0 ? BackgroundSprite.Leaf : BackgroundSprite.Petal;
                float size = Lerp(26f, 46f, (float)rng.NextDouble());
                var rect = CreateSprite("Falling" + i, sprite, size, color);

                _falling[i] = new FallingMotion
                {
                    Rect = rect,
                    StartX = Lerp(-640f, 640f, (float)rng.NextDouble()),
                    FallSpeed = Lerp(16f, 42f, (float)rng.NextDouble()),
                    SwayAmplitude = Lerp(20f, 55f, (float)rng.NextDouble()),
                    SwayPeriod = Lerp(5f, 11f, (float)rng.NextDouble()),
                    Phase = Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble()),
                    StartY = Lerp(-1f, 1f, (float)rng.NextDouble()),
                    SpinDegreesPerSecond = Lerp(-40f, 40f, (float)rng.NextDouble()),
                    BaseRotation = Lerp(0f, 360f, (float)rng.NextDouble())
                };
            }
        }

        private void BuildFireflies(System.Random rng)
        {
            _fireflies = new FireflyMotion[FireflyCount];
            for (int i = 0; i < FireflyCount; i++)
            {
                float size = Lerp(16f, 34f, (float)rng.NextDouble());
                var color = _fireflyColor;
                color.a = 0f;
                var rect = CreateSprite("Firefly" + i, BackgroundSprite.Glow, size, color);
                var image = rect.GetComponent<Image>();

                var center = new Vector2(Lerp(-620f, 620f, (float)rng.NextDouble()), Lerp(-340f, 340f, (float)rng.NextDouble()));
                _fireflies[i] = new FireflyMotion
                {
                    Rect = rect,
                    Image = image,
                    Center = center,
                    Amplitude = new Vector2(Lerp(30f, 90f, (float)rng.NextDouble()), Lerp(20f, 70f, (float)rng.NextDouble())),
                    PeriodSeconds = new Vector2(Lerp(9f, 20f, (float)rng.NextDouble()), Lerp(8f, 17f, (float)rng.NextDouble())),
                    Phase = new Vector2(Lerp(0f, 6.28f, (float)rng.NextDouble()), Lerp(0f, 6.28f, (float)rng.NextDouble())),
                    PulsePeriod = Lerp(2.5f, 6f, (float)rng.NextDouble()),
                    PulsePhase = Lerp(0f, 6.28f, (float)rng.NextDouble()),
                    MaxAlpha = Lerp(0.35f, 0.75f, (float)rng.NextDouble())
                };
                rect.anchoredPosition = center;
            }
        }

        private RectTransform CreateSprite(string name, BackgroundSprite kind, float size, Color color)
        {
            var rt = UIFactory.CreateUIObject(name, _container);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = BackgroundShapeFactory.GetShape(kind);
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;

            var rect = img.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private void Update()
        {
            if (_decor == null)
            {
                return;
            }

            float t = Time.time;
            for (int i = 0; i < _decor.Length; i++)
            {
                var shape = _decor[i];
                float x = shape.Center.x + Mathf.Sin(t / shape.PeriodSeconds.x * Mathf.PI * 2f + shape.Phase.x) * shape.Amplitude.x;
                float y = shape.Center.y + Mathf.Cos(t / shape.PeriodSeconds.y * Mathf.PI * 2f + shape.Phase.y) * shape.Amplitude.y;
                shape.Rect.anchoredPosition = new Vector2(x, y);
                shape.Rect.localRotation = Quaternion.Euler(0f, 0f, t * shape.RotationDegreesPerSecond);
            }

            // The canvas is always 800 units tall (match-height scaler) but its width follows the window's aspect ratio.
            float halfHeight = _container.rect.height * 0.5f;
            float halfWidth = _container.rect.width * 0.5f;
            float span = halfHeight * 2f + WrapMargin * 2f;
            for (int i = 0; i < _falling.Length; i++)
            {
                var p = _falling[i];
                // Position down the fall loops over [top+margin, bottom-margin], wrapping seamlessly; StartY offsets each petal's place in that loop.
                float travelled = p.FallSpeed * t + (p.StartY * 0.5f + 0.5f) * span;
                float y = halfHeight + WrapMargin - Mathf.Repeat(travelled, span);
                float xBase = Mathf.Clamp(p.StartX, -halfWidth, halfWidth);
                float x = xBase + Mathf.Sin(t / p.SwayPeriod * Mathf.PI * 2f + p.Phase) * p.SwayAmplitude;
                p.Rect.anchoredPosition = new Vector2(x, y);
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.BaseRotation + t * p.SpinDegreesPerSecond);
            }

            for (int i = 0; i < _fireflies.Length; i++)
            {
                var f = _fireflies[i];
                float x = f.Center.x + Mathf.Sin(t / f.PeriodSeconds.x * Mathf.PI * 2f + f.Phase.x) * f.Amplitude.x;
                float y = f.Center.y + Mathf.Cos(t / f.PeriodSeconds.y * Mathf.PI * 2f + f.Phase.y) * f.Amplitude.y;
                f.Rect.anchoredPosition = new Vector2(x, y);
                float pulse = Mathf.Sin(t / f.PulsePeriod * Mathf.PI * 2f + f.PulsePhase) * 0.5f + 0.5f;
                var c = _fireflyColor;
                c.a = f.MaxAlpha * pulse * pulse;
                f.Image.color = c;
            }
        }
    }
}
