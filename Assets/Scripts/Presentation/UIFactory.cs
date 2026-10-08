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

        /// <summary>
        /// The "Colorful UI" pack's "Digitalt" font (Assets/Resources/
        /// Colorful_UI/colorful/font/), used everywhere text is created.
        /// The font's thick, uniform stroke weight is baked into its glyphs,
        /// not a separate outline effect. Falls back to Unity's built-in
        /// legacy font if the asset pack isn't present.
        /// </summary>
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
            // only calls for components added through the Inspector; every
            // button here is built via AddComponent, so this must be set
            // explicitly or the hover/press tint below never renders.
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            // Both states darken from normal rather than highlighted trying
            // to brighten past it: a color channel can't exceed 1 on a
            // non-HDR UI Image, so brightening a white base clamps back to
            // white and shows no visible change.
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            // Flat, fully opaque muted gray so a disabled button reads the
            // same everywhere regardless of what's behind it, and stays
            // opaque enough for the thick outline and any text/icon on top
            // to remain legible.
            colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 1f);
            btn.colors = colors;

            // Quick scale-punch on click, on top of the color tint above —
            // see ButtonPunchEffect.
            var punch = img.gameObject.AddComponent<ButtonPunchEffect>();
            btn.onClick.AddListener(punch.Punch);

            // Buttons get no thick "comic" outline; every other card/panel/
            // badge still gets one via its own direct AddThickOutline call.

            // 50% bigger than whatever size the caller asked for — every
            // button goes through this one spot, so scaling here covers all
            // of them at once and stays proportional across call sites.
            var text = CreateText(img.transform, "Label", label, Mathf.RoundToInt(fontSize * 1.5f), UITheme.TextPrimary);
            StretchFull(text.rectTransform);
            return btn;
        }

        /// <summary>
        /// Thick "comic" outline around a panel/card: a single Outline
        /// component, not a hand-rolled 4-bar frame like GridView.
        /// BuildLineClearBorder/ModifierCarouselView.BuildHighlightFrame use
        /// elsewhere (those exist to outline a see-through or dynamically-
        /// resized area). Every target here is an opaque, static-sized
        /// Image, so Unity's Outline works cleanly and follows a 9-sliced
        /// sprite's rounded corners automatically. Not called from
        /// <see cref="FinishButton"/>; buttons get no outline.
        /// </summary>
        public static void AddThickOutline(Image target, Color color, float thickness = 3f)
        {
            var outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
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
