using System;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals an upgrade that
    /// applies immediately with no follow-up choice (Joker — the only
    /// no-sub-choice Bank upgrade). It's already applied by the time this
    /// shows; this is purely so the player can see (via UpgradeCardFactory)
    /// what they just got instead of nothing at all, same as the
    /// sub-choice/tile-choice reveals get.
    /// </summary>
    public sealed class UpgradeRevealView : MonoBehaviour
    {
        private const float TitleHeight = 40f;
        private const float OkHeight = 44f;
        private const float BlockSpacing = 24f;
        // The canvas is always exactly this tall in its own local units
        // regardless of actual window size (CanvasScaler matches on height —
        // see GameBootstrap.BuildCanvas), so centering math done in this
        // space holds for any resolution.
        private const float CanvasHeight = 800f;

        /// <summary>Fires once the player dismisses the reveal.</summary>
        public event Action Dismissed;

        private RectTransform _root;
        private RectTransform _titleRect;
        private RectTransform _cardContainer;
        private RectTransform _okRect;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "UpgradeRevealOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "You got:", 22, UITheme.TextPrimary);
            _titleRect = title.rectTransform;
            _titleRect.anchorMin = new Vector2(0.5f, 1f);
            _titleRect.anchorMax = new Vector2(0.5f, 1f);
            _titleRect.pivot = new Vector2(0.5f, 1f);
            _titleRect.sizeDelta = new Vector2(600f, TitleHeight);

            // pivot (0.5, 1) here matters, not just cosmetically — it's what
            // UpgradeCardFactory.Build's own returned container anchors
            // itself against (also (0.5, 1)), so this container's pivot has
            // to match or the card ends up offset by half of whatever
            // arbitrary default size an un-sized RectTransform gets (this
            // used to be pivot (0.5, 0.5), which is exactly what silently
            // broke once UpgradeCardFactory started setting its own
            // anchor/pivot instead of leaving it to the caller).
            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);

            var okButton = UIFactory.CreateButton(_root, "Ok", "OK", UISprites.ChooseButtonBackground, 18);
            _okRect = okButton.GetComponent<RectTransform>();
            _okRect.anchorMin = new Vector2(0.5f, 1f);
            _okRect.anchorMax = new Vector2(0.5f, 1f);
            _okRect.pivot = new Vector2(0.5f, 1f);
            _okRect.sizeDelta = new Vector2(160f, OkHeight);
            okButton.onClick.AddListener(OnOkClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(UpgradeDefinition def)
        {
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }
            var card = UpgradeCardFactory.Build(_cardContainer, def);

            // Same measured-block-centered-in-the-overlay approach as
            // TileChoiceView.LayoutBlock — the card's height varies with the
            // description's length, so title/card/OK are stacked and
            // centered using that real height rather than fixed offsets.
            float cardHeight = card.sizeDelta.y;
            float totalHeight = TitleHeight + BlockSpacing + cardHeight + BlockSpacing + OkHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _titleRect.anchoredPosition = new Vector2(0f, topY);
            float y = topY - TitleHeight - BlockSpacing;

            _cardContainer.anchoredPosition = new Vector2(0f, y);
            y -= cardHeight + BlockSpacing;

            _okRect.anchoredPosition = new Vector2(0f, y);

            _root.gameObject.SetActive(true);
        }

        private void OnOkClicked()
        {
            _root.gameObject.SetActive(false);
            if (Dismissed != null)
            {
                Dismissed();
            }
        }
    }
}
