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
        // The art is authored 2x (PNG pixels = 2 x game pixels). Its canvas is: top margin = body height, bottom margin = 8,
        // so body height h = (canvasHeight - 8) / 2 (see the generator).
        private const float AuthoredScale = 2f;
        private const float BottomMargin = 8f;

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

        private void Layout()
        {
            var r = ((RectTransform)transform).rect;
            float canvasW = _sprite.rect.width / AuthoredScale;
            float canvasH = _sprite.rect.height / AuthoredScale;
            float bodyH = (canvasH - BottomMargin) * 0.5f;
            float f = bodyH > 0f ? r.height / bodyH : 1f;
            _art.sizeDelta = new Vector2(canvasW * f, canvasH * f);
            // The body sits (bodyH * 1.5) below the canvas top, the canvas center is (canvasH / 2) below it: shift the art up by the difference.
            _art.anchoredPosition = new Vector2(0f, (bodyH * 0.5f - BottomMargin * 0.5f) * f);
        }
    }
}
