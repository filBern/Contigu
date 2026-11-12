using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The titles of the shop's boxes ("Fertilizers", "Coming up") and of its shelves ("Blister", "Workshop"): a small plot, like the buttons
    /// (a colored bed around dark soil, the title in the bed's own light color, a flower on either side). Not interactive, so it reads as a label.
    /// </summary>
    public static class ShopSign
    {
        private const float SignHeight = 44f;
        private const float SignTop = 12f;

        /// <summary>A title plaque at the top of a box, as wide as the box (minus a margin).</summary>
        public static void Build(RectTransform card, string title, int fontSize, string plot = "Wood")
        {
            var plaque = BuildPlaque(card, "ShopSign", title, fontSize, plot);
            plaque.anchorMin = new Vector2(0f, 1f);
            plaque.anchorMax = new Vector2(1f, 1f);
            plaque.pivot = new Vector2(0.5f, 1f);
            plaque.anchoredPosition = new Vector2(0f, -SignTop);
            plaque.sizeDelta = new Vector2(-30f, SignHeight);
        }

        /// <summary>A plaque (plot body, lettering, flowers); the caller places and sizes it.</summary>
        public static RectTransform BuildPlaque(Transform parent, string name, string title, int fontSize, string plot)
        {
            var body = UIFactory.CreateSlicedImage(parent, name, UISprites.Garden(plot) ?? UISprites.CardBackground);
            body.color = Color.white;
            body.raycastTarget = false;
            UIFactory.AddDropShadow(body, 5f, 0.4f);
            var label = UIFactory.CreateText(body.transform, "Title", title, fontSize, UIFactory.PlotTextColor("Plot" + plot));
            label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleCenter;
            UIFactory.StretchFull(label.rectTransform);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;
            PlotButtonDecor.Attach(body, label, Mathf.Abs(name.GetHashCode()));
            return body.rectTransform;
        }
    }
}
