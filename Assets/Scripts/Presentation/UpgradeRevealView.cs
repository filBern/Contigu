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
        /// <summary>Fires once the player dismisses the reveal.</summary>
        public event Action Dismissed;

        private RectTransform _root;
        private RectTransform _cardContainer;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "UpgradeRevealOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "You got:", 22, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _cardContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _cardContainer.pivot = new Vector2(0.5f, 0.5f);
            _cardContainer.anchoredPosition = new Vector2(0f, 20f);

            var okButton = UIFactory.CreateButton(_root, "Ok", "OK", UISprites.ChooseButtonBackground, 18);
            var okRect = okButton.GetComponent<RectTransform>();
            okRect.anchorMin = new Vector2(0.5f, 0f);
            okRect.anchorMax = new Vector2(0.5f, 0f);
            okRect.pivot = new Vector2(0.5f, 0f);
            okRect.anchoredPosition = new Vector2(0f, 60f);
            okRect.sizeDelta = new Vector2(160f, 44f);
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
            UpgradeCardFactory.Build(_cardContainer, def);
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
