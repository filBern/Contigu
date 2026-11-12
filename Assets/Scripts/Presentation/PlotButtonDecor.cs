using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>The small flowers that grow on either side of a plot button's label. They only show on a button wide enough for them; a narrow one keeps its label alone.</summary>
    public sealed class PlotButtonDecor : MonoBehaviour
    {
        private const float MinWidth = 150f;
        private const float MinHeight = 36f;
        private static Sprite[] _flowers;

        private RectTransform _rect;
        private Image _left;
        private Image _right;
        private RectTransform _label;

        private static Sprite[] Flowers()
        {
            if (_flowers == null)
            {
                _flowers = new[]
                {
                    Resources.Load<Sprite>("Buttons/Plot/Flowers/FlowerRed"),
                    Resources.Load<Sprite>("Buttons/Plot/Flowers/FlowerYellow"),
                    Resources.Load<Sprite>("Buttons/Plot/Flowers/FlowerBlue"),
                    Resources.Load<Sprite>("Buttons/Plot/Flowers/FlowerWhite")
                };
            }
            return _flowers;
        }

        public static void Attach(Image face, Text label, int seed)
        {
            var decor = face.gameObject.AddComponent<PlotButtonDecor>();
            decor._rect = face.rectTransform;
            decor._label = label.rectTransform;
            var sprites = Flowers();
            decor._left = MakeFlower(face.transform, "FlowerL", sprites[seed % sprites.Length], 0f, -12f);
            decor._right = MakeFlower(face.transform, "FlowerR", sprites[(seed + 1) % sprites.Length], 1f, 14f);
            // The flowers sit behind the label (first children), never in front of the text.
            decor._left.transform.SetAsFirstSibling();
            decor._right.transform.SetAsFirstSibling();
            decor.Refresh();
        }

        private static Image MakeFlower(Transform parent, string name, Sprite sprite, float anchorX, float angle)
        {
            var flower = UIFactory.CreatePanel(parent, name, Color.white);
            flower.raycastTarget = false;
            flower.sprite = sprite;
            flower.preserveAspect = true;
            flower.enabled = sprite != null;
            flower.rectTransform.anchorMin = flower.rectTransform.anchorMax = new Vector2(anchorX, 0.5f);
            flower.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            flower.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return flower;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_rect != null)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            float width = _rect.rect.width;
            float height = _rect.rect.height;
            bool show = width >= MinWidth && height >= MinHeight;
            float size = Mathf.Clamp(height * 0.42f, 16f, 26f);
            float inset = 9f + size * 0.5f + 3f;
            _left.gameObject.SetActive(show);
            _right.gameObject.SetActive(show);
            _left.rectTransform.sizeDelta = _right.rectTransform.sizeDelta = new Vector2(size, size);
            _left.rectTransform.anchoredPosition = new Vector2(inset, 0f);
            _right.rectTransform.anchoredPosition = new Vector2(-inset, 0f);
            // The label keeps clear of the flowers when they show; on a narrow button it only avoids the bed.
            float margin = show ? inset + size * 0.5f + 4f : 10f;
            _label.offsetMin = new Vector2(margin, _label.offsetMin.y);
            _label.offsetMax = new Vector2(-margin, _label.offsetMax.y);
        }
    }
}
