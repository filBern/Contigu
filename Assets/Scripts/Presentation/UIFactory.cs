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

        /// <summary>The "Colorful UI" pack's font, used everywhere text is created (titles, labels, buttons, and description paragraphs alike, on explicit request) — falls back to Unity's built-in legacy font if the asset pack isn't present (e.g. a checkout that hasn't pulled it yet).</summary>
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
            return FinishButton(img, label, fontSize);
        }

        /// <summary>Same as the Color overload, but skinned with a "Colorful UI" pack sprite (see UISprites) instead of a flat fill.</summary>
        public static Button CreateButton(Transform parent, string name, string label, Sprite bgSprite, int fontSize = 16)
        {
            var img = CreateSlicedImage(parent, name, bgSprite);
            return FinishButton(img, label, fontSize);
        }

        private static Button FinishButton(Image img, string label, int fontSize)
        {
            var btn = img.gameObject.AddComponent<Button>();
            // Selectable normally self-assigns this via Reset(), which Unity
            // only calls for components added through the Inspector — every
            // button here is built purely from script via AddComponent, so
            // without this line targetGraphic silently stays null and NONE
            // of the hover/press color tint below ever actually renders.
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            // Both states DARKEN from normal instead of highlighted trying to
            // brighten PAST it — every sprite-skinned button (the vast
            // majority, e.g. every shop Buy/Reroll/Leave button) starts at a
            // plain white tint (see CreateSlicedImage), and a color channel
            // can't exceed 1 on a non-HDR UI Image: the old highlightedColor
            // of (1.15,1.15,1.15) just clamped back down to white, so hover
            // showed no visible change at all (bug report, shop buttons:
            // "ajoute une nuance visuelle pour le hover et le click"). Now
            // both hover and pressed are always visible regardless of the
            // button's own base tint, pressed darker than highlighted so the
            // two stay distinguishable from each other too.
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            btn.colors = colors;

            // Quick scale-punch on click, on top of the color tint above —
            // see ButtonPunchEffect.
            var punch = img.gameObject.AddComponent<ButtonPunchEffect>();
            btn.onClick.AddListener(punch.Punch);

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
