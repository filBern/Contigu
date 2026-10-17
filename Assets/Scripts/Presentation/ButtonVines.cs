using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Little plants around a button, in the same style as the title letters: a flowering vine curling around
    /// the top-left corner, a mirrored one at the bottom-right, and a sprout on the top edge. Added to every
    /// button by <see cref="UIFactory"/>; sized from the button's own rect, and skipped on buttons too small
    /// to carry decoration (icon buttons, count badges). Art: Resources/UI/VineCorner.png, VineSprout.png.
    /// </summary>
    public sealed class ButtonVines : MonoBehaviour
    {
        private const float MinHeight = 34f;
        private const float MinWidth = 84f;

        private RectTransform _rect;
        private RectTransform _cornerTopLeft;
        private RectTransform _cornerBottomRight;
        private RectTransform _sprout;

        private void Start()
        {
            _rect = (RectTransform)transform;
            var corner = Resources.Load<Sprite>("UI/VineCorner");
            var sprout = Resources.Load<Sprite>("UI/VineSprout");
            if (corner == null || sprout == null)
            {
                enabled = false;
                return;
            }
            _cornerTopLeft = Make("VineTL", corner);
            _cornerBottomRight = Make("VineBR", corner);
            _sprout = Make("VineSprout", sprout);
            Layout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_cornerTopLeft != null)
            {
                Layout();
            }
        }

        private RectTransform Make(string name, Sprite sprite)
        {
            var go = UIFactory.CreatePanel(transform, name, Color.white);
            go.sprite = sprite;
            go.preserveAspect = true;
            go.raycastTarget = false;
            return go.rectTransform;
        }

        private void Layout()
        {
            var r = _rect.rect;
            bool show = r.height >= MinHeight && r.width >= MinWidth;
            _cornerTopLeft.gameObject.SetActive(show);
            _cornerBottomRight.gameObject.SetActive(show);
            _sprout.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            float s = Mathf.Clamp(r.height * 0.85f, 26f, 44f);
            float inset = s * 0.12f;

            Place(_cornerTopLeft, new Vector2(0f, 1f), new Vector2(s * 0.5f - inset, -s * 0.5f + inset), s, Vector3.one);
            Place(_cornerBottomRight, new Vector2(1f, 0f), new Vector2(-s * 0.5f + inset, s * 0.5f - inset), s, new Vector3(-1f, -1f, 1f));

            // Sprout on the top edge, nearer the right end so it never crowds the top-left vine.
            var sp = _sprout;
            sp.anchorMin = sp.anchorMax = new Vector2(0.74f, 1f);
            sp.pivot = new Vector2(0.5f, 0.3f);
            sp.sizeDelta = new Vector2(s * 0.7f, s * 0.7f);
            sp.anchoredPosition = Vector2.zero;
            sp.localScale = Vector3.one;
            sp.SetAsLastSibling();
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, float size, Vector3 scale)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            rt.localScale = scale;
            rt.SetAsLastSibling();
        }
    }
}
