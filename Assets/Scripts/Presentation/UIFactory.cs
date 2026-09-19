using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Small helpers for building uGUI trees purely from code, so the whole game
    /// can boot from a single near-empty scene (no hand-authored prefabs needed).
    /// </summary>
    public static class UIFactory
    {
        private static Font _cachedFont;

        /// <summary>The "Colorful UI" pack's font, used everywhere text is created — falls back to Unity's built-in legacy font if the asset pack isn't present (e.g. a checkout that hasn't pulled it yet).</summary>
        public static Font DefaultFont()
        {
            if (_cachedFont == null)
            {
                _cachedFont = Resources.Load<Font>("Colorful_UI/colorful/font/Digitalt");
                if (_cachedFont == null)
                {
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            return _cachedFont;
        }

        public static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var rt = CreateUIObject(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>A 9-sliced Image built from a "Colorful UI" pack sprite (see UISprites) — the sprite's own art carries the visual weight, so color stays plain white (no tint) unless the caller sets one afterward for a state tint (selected/disabled/etc).</summary>
        public static Image CreateSlicedImage(Transform parent, string name, Sprite sprite)
        {
            var rt = CreateUIObject(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = CreateUIObject(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color bgColor, int fontSize = 16)
        {
            var img = CreatePanel(parent, name, bgColor);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            btn.colors = colors;

            var text = CreateText(img.transform, "Label", label, fontSize, UITheme.TextPrimary);
            StretchFull(text.rectTransform);
            return btn;
        }

        public static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void SetAnchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
        }

        public static void SetSize(RectTransform rt, float width, float height)
        {
            rt.sizeDelta = new Vector2(width, height);
        }

        public static void SetAnchoredPosition(RectTransform rt, float x, float y)
        {
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
