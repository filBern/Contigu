using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The title of a shop side panel ("Fertilizers", "Coming up"): a small wooden sign hanging from the top of its card by two cords,
    /// carved like the Blister and Workshop plaques. Flat and nailed, so it reads as a label, not as a button.
    /// </summary>
    public static class ShopSign
    {
        private static readonly Color Wood = new Color(0.55f, 0.37f, 0.20f, 1f);
        private static readonly Color Highlight = new Color(0.66f, 0.46f, 0.26f, 1f);
        private static readonly Color Dark = new Color(0.13f, 0.09f, 0.06f, 1f);
        private static readonly Color Lettering = new Color(1f, 0.96f, 0.84f);

        private const float SignHeight = 44f;
        private const float SignTop = 14f;
        private const float CordLength = 18f;
        private const float CordSpacing = 54f;

        public static void Build(RectTransform card, string title, int fontSize)
        {
            var root = UIFactory.CreateUIObject("ShopSign", card);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, SignTop + SignHeight);

            for (int i = 0; i < 2; i++)
            {
                var cord = UIFactory.CreatePanel(root, "Cord" + i, Dark);
                cord.raycastTarget = false;
                cord.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                cord.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cord.rectTransform.pivot = new Vector2(0.5f, 1f);
                cord.rectTransform.anchoredPosition = new Vector2(i == 0 ? -CordSpacing : CordSpacing, -4f);
                cord.rectTransform.sizeDelta = new Vector2(4f, CordLength);
            }

            var board = UIFactory.CreateSlicedImage(root, "Board", UISprites.CardBackground);
            board.color = Wood;
            board.raycastTarget = false;
            UIFactory.AddThickOutline(board, Dark);
            var boardRect = board.rectTransform;
            boardRect.anchorMin = new Vector2(0.5f, 1f);
            boardRect.anchorMax = new Vector2(0.5f, 1f);
            boardRect.pivot = new Vector2(0.5f, 1f);
            boardRect.anchoredPosition = new Vector2(0f, -SignTop);
            boardRect.sizeDelta = new Vector2(card.rect.width > 0f ? card.rect.width - 30f : 200f, SignHeight);

            var lip = UIFactory.CreatePanel(boardRect, "Lip", Highlight);
            lip.raycastTarget = false;
            lip.rectTransform.anchorMin = new Vector2(0f, 1f);
            lip.rectTransform.anchorMax = new Vector2(1f, 1f);
            lip.rectTransform.pivot = new Vector2(0.5f, 1f);
            lip.rectTransform.anchoredPosition = new Vector2(0f, -3f);
            lip.rectTransform.sizeDelta = new Vector2(-12f, 3f);

            for (int i = 0; i < 2; i++)
            {
                var nail = UIFactory.CreatePanel(boardRect, "Nail" + i, Dark);
                nail.raycastTarget = false;
                nail.rectTransform.anchorMin = new Vector2(i == 0 ? 0f : 1f, 0.5f);
                nail.rectTransform.anchorMax = new Vector2(i == 0 ? 0f : 1f, 0.5f);
                nail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                nail.rectTransform.anchoredPosition = new Vector2(i == 0 ? 11f : -11f, 0f);
                nail.rectTransform.sizeDelta = new Vector2(6f, 6f);
            }

            var label = UIFactory.CreateText(boardRect, "Title", title, fontSize, Lettering);
            label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleCenter;
            UIFactory.StretchFull(label.rectTransform);
        }
    }
}
