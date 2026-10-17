using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Little plants growing from the top edge of a button, like a planter box — a taller one near the left and a
    /// small one near the right. Each button gets its own variety (poppy, bluebell, sunflower, fern, lavender,
    /// mushrooms, berries, clover, tulips, ivy), handed out in turn by <see cref="UIFactory"/> so neighbouring
    /// buttons differ. Sized from the button's own rect and skipped on buttons too small to carry decoration
    /// (icon buttons, count badges). Art: Resources/UI/Plants/V&lt;n&gt;A.png / V&lt;n&gt;B.png (Design/Buttons/generate_plants.py).
    /// </summary>
    public sealed class ButtonVines : MonoBehaviour
    {
        public const int VarietyCount = 10;
        private const float MinHeight = 34f;
        private const float MinWidth = 84f;

        /// <summary>Which plant variety this button wears (1..VarietyCount); set by UIFactory right after creation.</summary>
        public int Variety = 1;

        private RectTransform _rect;
        private RectTransform _tall;
        private RectTransform _small;

        private void Start()
        {
            _rect = (RectTransform)transform;
            var tallSprite = Resources.Load<Sprite>("UI/Plants/V" + Variety + "A");
            var smallSprite = Resources.Load<Sprite>("UI/Plants/V" + Variety + "B");
            if (tallSprite == null || smallSprite == null)
            {
                enabled = false;
                return;
            }
            _tall = Make("PlantTall", tallSprite);
            _small = Make("PlantSmall", smallSprite);
            Layout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_tall != null)
            {
                Layout();
            }
        }

        private RectTransform Make(string name, Sprite sprite)
        {
            var img = UIFactory.CreatePanel(transform, name, Color.white);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img.rectTransform;
        }

        private void Layout()
        {
            var r = _rect.rect;
            bool show = r.height >= MinHeight && r.width >= MinWidth;
            _tall.gameObject.SetActive(show);
            _small.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            float s = Mathf.Clamp(r.height * 0.9f, 28f, 46f);
            // Base sits just inside the top edge so the stem looks planted in the button.
            Stand(_tall, 0.17f, s);
            Stand(_small, 0.80f, s * 0.72f);
        }

        private static void Stand(RectTransform rt, float xRatio, float size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(xRatio, 1f);
            rt.pivot = new Vector2(0.5f, 0.06f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }
    }
}
