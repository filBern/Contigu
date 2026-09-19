using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown right after the tile/grid upgrade draft: the player picks 1
    /// of 3 offered modifiers to add to their persistent set (unlimited — no
    /// cap on how many can be active at once).
    /// </summary>
    public sealed class ModifierDraftView : MonoBehaviour
    {
        private const float CardWidth = 200f;
        private const float CardHeight = 210f;
        private const float BadgeSize = 130f;

        /// <summary>Fires when the player picks one of the 3 drafted modifiers.</summary>
        public event Action<ModifierId> ModifierPicked;

        private RectTransform _root;
        private Text _header;
        private Text _sectionLabel;
        private RectTransform _cardsContainer;
        private TooltipView _tooltip;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            var overlay = UIFactory.CreatePanel(parent, "ModifierDraftOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            _header = UIFactory.CreateText(_root, "Header", "", 26, UITheme.TextPrimary);
            _header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _header.rectTransform.pivot = new Vector2(0.5f, 1f);
            _header.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            _header.rectTransform.sizeDelta = new Vector2(900f, 40f);

            _sectionLabel = UIFactory.CreateText(_root, "SectionLabel", "", 18, UITheme.TextPrimary);
            _sectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _sectionLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _cardsContainer = UIFactory.CreateUIObject("Cards", _root);
            _cardsContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardsContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardsContainer.pivot = new Vector2(0.5f, 1f);
            _cardsContainer.anchoredPosition = new Vector2(0f, -115f);

            // Fixed 2-column grid rather than a single row of 3, to stay
            // consistent width-wise regardless of how many options are drafted.
            var layout = _cardsContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CardWidth, CardHeight);
            layout.spacing = new Vector2(20f, 16f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            var fitter = _cardsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void ShowPick(IReadOnlyList<ModifierDefinition> options)
        {
            _header.text = "Choose a modifier!";
            _sectionLabel.text = "1 of " + options.Count + " — stacks with your active modifiers";
            ClearChildren(_cardsContainer);
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(options[i]);
            }
            _root.gameObject.SetActive(true);
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void BuildCard(ModifierDefinition def)
        {
            // GridLayoutGroup drives each child's size directly from its own
            // cellSize, so unlike a Horizontal/VerticalLayoutGroup the card
            // doesn't need its own sizeDelta or LayoutElement set.
            var card = UIFactory.CreatePanel(_cardsContainer, "ModCard_" + def.Id, UITheme.PanelLight);
            // The card's own fill sits close in luminance to the black overlay
            // behind it, so without a rim the card edge is hard to read — a
            // dark outline around the panel itself gives it a defined border.
            var cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var badge = ModifierBadgeFactory.Create(card.transform, def, BadgeSize, _tooltip);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            badge.rectTransform.pivot = new Vector2(0.5f, 1f);
            badge.rectTransform.anchoredPosition = new Vector2(0f, -15f);

            var chooseBtn = UIFactory.CreateButton(card.transform, "Choose", "Choose", UISprites.ChooseButtonBackground, 14);
            var chooseRect = chooseBtn.GetComponent<RectTransform>();
            chooseRect.anchorMin = new Vector2(0.5f, 0f);
            chooseRect.anchorMax = new Vector2(0.5f, 0f);
            chooseRect.pivot = new Vector2(0.5f, 0f);
            chooseRect.anchoredPosition = new Vector2(0f, 14f);
            chooseRect.sizeDelta = new Vector2(CardWidth - 30f, 38f);
            chooseBtn.onClick.AddListener(() => OnCardChosen(def.Id));
        }

        private void OnCardChosen(ModifierId id)
        {
            _root.gameObject.SetActive(false);
            if (ModifierPicked != null)
            {
                ModifierPicked(id);
            }
        }
    }
}
