using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Replaces a button's look with its complete illustration (Resources/Buttons/Full/&lt;Label&gt;.png: glossy body, the label
    /// lettered in the title's style, plants growing around it — see Design/Buttons/generate_button_art.py). The button keeps its
    /// own rect for hit-testing and layout; the art is drawn as a child, scaled so its body is as tall as the button and centered on
    /// it, with the plants overflowing above. The art is the button's tint target, so hover/press/disabled still show.
    /// </summary>
    public sealed class ButtonArt : MonoBehaviour
    {
        // The art is authored 4x (PNG pixels = 4 x game pixels, so it stays sharp on high-DPI screens). Per-button body height and
        // top/bottom margins come from Resources/Buttons/Full/layout.txt, written by the generator ("Name bodyH topMargin bottomMargin").
        private const float AuthoredScale = 4f;
        private static System.Collections.Generic.Dictionary<string, Vector3> _layouts;

        private Sprite _sprite;
        private RectTransform _art;

        /// <summary>The art for a button labelled <paramref name="label"/> ("Next round" -> Resources/Buttons/Full/Nextround), or null.</summary>
        public static Sprite Find(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return null;
            }
            return Resources.Load<Sprite>("Buttons/Full/" + label.Replace(" ", ""));
        }

        public void Init(Sprite sprite)
        {
            _sprite = sprite;
            var button = GetComponent<Button>();
            var own = GetComponent<Image>();
            if (own != null)
            {
                // The button's own pill stays only as an invisible hit area (alpha on the renderer, so later color tweaks by callers cannot reveal it).
                own.canvasRenderer.SetAlpha(0f);
            }

            var art = UIFactory.CreatePanel(transform, "Art", Color.white);
            art.sprite = sprite;
            art.preserveAspect = true;
            art.raycastTarget = false;
            _art = art.rectTransform;
            _art.anchorMin = _art.anchorMax = new Vector2(0.5f, 0.5f);
            _art.pivot = new Vector2(0.5f, 0.5f);
            _art.SetAsFirstSibling();
            if (button != null)
            {
                button.targetGraphic = art;
            }
            Layout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_art != null)
            {
                Layout();
            }
        }

        private static Vector3 LayoutFor(string name, float canvasH)
        {
            if (_layouts == null)
            {
                _layouts = new System.Collections.Generic.Dictionary<string, Vector3>();
                var file = Resources.Load<TextAsset>("Buttons/Full/layout");
                if (file != null)
                {
                    foreach (var line in file.text.Split('\n'))
                    {
                        var parts = line.Trim().Split(' ');
                        if (parts.Length == 4
                            && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var h)
                            && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mt)
                            && float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mb))
                        {
                            _layouts[parts[0]] = new Vector3(h, mt, mb);
                        }
                    }
                }
            }
            // Fallback if the entry is missing: body as tall as the top margin, 8 below.
            return _layouts.TryGetValue(name, out var l) ? l : new Vector3((canvasH - 8f) * 0.5f, (canvasH - 8f) * 0.5f, 8f);
        }

        private void Layout()
        {
            var r = ((RectTransform)transform).rect;
            float canvasW = _sprite.rect.width / AuthoredScale;
            float canvasH = _sprite.rect.height / AuthoredScale;
            var l = LayoutFor(_sprite.name, canvasH);
            float bodyH = l.x, top = l.y, bottom = l.z;
            float f = bodyH > 0f ? r.height / bodyH : 1f;
            _art.sizeDelta = new Vector2(canvasW * f, canvasH * f);
            // The body's center sits (top + bodyH/2) below the canvas top; the canvas center is canvasH/2 below it: shift the art by the difference.
            _art.anchoredPosition = new Vector2(0f, (top + bodyH * 0.5f - canvasH * 0.5f) * f);
        }
    }
}
