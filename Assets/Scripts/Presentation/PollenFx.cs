using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Makes a Pollen counter feel precious: a soft pulsing golden bloom behind the icon, a slow breathing of the
    /// icon itself, and tiny glowing pollen specks drifting up and twinkling around it. Add to the counter's
    /// container (a layout group) and call <see cref="Init"/> with the icon and number label; everything is drawn
    /// behind them and ignores the layout. Runs on unscaled time.
    /// </summary>
    public sealed class PollenFx : MonoBehaviour
    {
        private const int SpeckCount = 12;
        private static readonly Color GlowColor = new Color(1f, 0.82f, 0.28f, 1f);
        private static readonly Color SpeckColor = new Color(1f, 0.93f, 0.62f, 1f);

        private RectTransform _icon;
        private RectTransform _fxRoot;
        private Image _bloom;
        private Image _bloomCore;
        private Image[] _specks;
        private float[] _age;
        private float[] _life;
        private Vector2[] _origin;
        private float[] _sway;

        public void Init(RectTransform icon, Text label)
        {
            _icon = icon;
            var glow = BackgroundShapeFactory.GetShape(BackgroundSprite.Glow);

            var root = UIFactory.CreateUIObject("PollenFx", transform);
            root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            root.SetAsFirstSibling();
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            // Sub-pixel drifting must not be snapped by the pixel-perfect root canvas.
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overridePixelPerfect = true;
            canvas.pixelPerfect = false;
            _fxRoot = root;

            _bloom = MakeGlow("Bloom", glow, 190f, new Color(GlowColor.r, GlowColor.g, GlowColor.b, 0.22f));
            _bloomCore = MakeGlow("BloomCore", glow, 84f, new Color(1f, 0.9f, 0.5f, 0.5f));

            _specks = new Image[SpeckCount];
            _age = new float[SpeckCount];
            _life = new float[SpeckCount];
            _origin = new Vector2[SpeckCount];
            _sway = new float[SpeckCount];
            for (int i = 0; i < SpeckCount; i++)
            {
                _specks[i] = MakeGlow("Speck" + i, glow, Random.Range(9f, 17f), SpeckColor);
                Respawn(i, true);
            }

            // Warm halo on the number itself.
            if (label != null)
            {
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.78f, 0.2f, 0.28f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        private Image MakeGlow(string name, Sprite sprite, float size, Color color)
        {
            var img = UIFactory.CreatePanel(_fxRoot, name, color);
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        private void Respawn(int i, bool scatterAge)
        {
            _life[i] = Random.Range(1.6f, 2.8f);
            _age[i] = scatterAge ? Random.Range(0f, _life[i]) : 0f;
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(8f, 42f);
            _origin[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.8f - 6f);
            _sway[i] = Random.Range(0f, Mathf.PI * 2f);
        }

        private void LateUpdate()
        {
            if (_icon == null || _fxRoot == null)
            {
                return;
            }
            _fxRoot.position = _icon.position;

            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
            float scale = 0.92f + 0.16f * pulse;
            _bloom.rectTransform.localScale = new Vector3(scale, scale, 1f);
            var c = _bloom.color;
            _bloom.color = new Color(c.r, c.g, c.b, 0.16f + 0.12f * pulse);
            float coreScale = 0.9f + 0.2f * (1f - pulse);
            _bloomCore.rectTransform.localScale = new Vector3(coreScale, coreScale, 1f);
            var cc = _bloomCore.color;
            _bloomCore.color = new Color(cc.r, cc.g, cc.b, 0.3f + 0.25f * pulse);

            // The icon itself breathes a little.
            float breathe = 1f + 0.05f * Mathf.Sin(t * 2.4f);
            _icon.localScale = new Vector3(breathe, breathe, 1f);

            for (int i = 0; i < SpeckCount; i++)
            {
                _age[i] += Time.unscaledDeltaTime;
                if (_age[i] >= _life[i])
                {
                    Respawn(i, false);
                }
                float p = _age[i] / _life[i];
                float rise = p * 46f;
                float x = _origin[i].x + Mathf.Sin(_sway[i] + p * 5f) * 7f;
                _specks[i].rectTransform.anchoredPosition = new Vector2(x, _origin[i].y + rise);
                float fade = Mathf.Sin(p * Mathf.PI);
                float twinkle = 0.65f + 0.35f * Mathf.Sin(t * 9f + i * 1.7f);
                var sc = SpeckColor;
                _specks[i].color = new Color(sc.r, sc.g, sc.b, fade * twinkle);
                float s = Mathf.Lerp(1f, 0.5f, p);
                _specks[i].rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
