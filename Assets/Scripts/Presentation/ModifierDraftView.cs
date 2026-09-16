using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown right after the tile/grid upgrade draft: the player picks 1
    /// of 3 offered modifiers to add to their persistent set. If that pushes the
    /// active count past <see cref="RunManager.MaxActiveModifiers"/>, the same
    /// overlay switches to a removal screen (pick 1 of the currently active
    /// modifiers to discard) before the round can advance.
    /// </summary>
    public sealed class ModifierDraftView : MonoBehaviour
    {
        private const float CardWidth = 200f;
        private const float CardHeight = 210f;
        private const float BadgeSize = 130f;

        /// <summary>Fires when the player picks one of the 3 drafted modifiers.</summary>
        public event Action<ModifierId> ModifierPicked;

        /// <summary>Fires when the player picks one active modifier to remove (over the 5-slot cap).</summary>
        public event Action<ModifierId> ModifierRemoved;

        private RectTransform _root;
        private Text _header;
        private Text _sectionLabel;
        private RectTransform _cardsContainer;
        private TooltipView _tooltip;

        private bool _isRemovalMode;

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
            AddTextOutline(_header);

            _sectionLabel = UIFactory.CreateText(_root, "SectionLabel", "", 18, UITheme.TextPrimary);
            _sectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _sectionLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);
            AddTextOutline(_sectionLabel);

            _cardsContainer = UIFactory.CreateUIObject("Cards", _root);
            _cardsContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardsContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardsContainer.pivot = new Vector2(0.5f, 1f);
            _cardsContainer.anchoredPosition = new Vector2(0f, -115f);

            var layout = _cardsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _cardsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void ShowPick(IReadOnlyList<ModifierDefinition> options)
        {
            _isRemovalMode = false;
            _header.text = "Choose a modifier!";
            _sectionLabel.text = "1 of " + options.Count + " — stacks with your active modifiers";
            RebuildCards(options, "Choose");
            _root.gameObject.SetActive(true);
        }

        public void ShowRemoval(IReadOnlyList<ModifierId> activeModifiers)
        {
            _isRemovalMode = true;
            _header.text = "Too many modifiers!";
            _sectionLabel.text = "You have " + activeModifiers.Count + ", maximum " + RunManager.MaxActiveModifiers + " — choose one to remove";

            var defs = new List<ModifierDefinition>(activeModifiers.Count);
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                defs.Add(ModifierCatalog.Get(activeModifiers[i]));
            }
            RebuildCards(defs, "Remove");
            _root.gameObject.SetActive(true);
        }

        private void RebuildCards(IReadOnlyList<ModifierDefinition> options, string buttonLabel)
        {
            ClearChildren(_cardsContainer);
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(options[i], buttonLabel);
            }
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void BuildCard(ModifierDefinition def, string buttonLabel)
        {
            var card = UIFactory.CreatePanel(_cardsContainer, "ModCard_" + def.Id, UITheme.PanelLight);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent HorizontalLayoutGroup collapses it toward zero.
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
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

            var chooseBtn = UIFactory.CreateButton(card.transform, "Choose", buttonLabel, UITheme.ButtonSelected, 14);
            var chooseRect = chooseBtn.GetComponent<RectTransform>();
            chooseRect.anchorMin = new Vector2(0.5f, 0f);
            chooseRect.anchorMax = new Vector2(0.5f, 0f);
            chooseRect.pivot = new Vector2(0.5f, 0f);
            chooseRect.anchoredPosition = new Vector2(0f, 14f);
            chooseRect.sizeDelta = new Vector2(CardWidth - 30f, 38f);
            chooseBtn.onClick.AddListener(() => OnCardChosen(def.Id));
        }

        private static void AddTextOutline(Text label)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void OnCardChosen(ModifierId id)
        {
            _root.gameObject.SetActive(false);
            if (_isRemovalMode)
            {
                if (ModifierRemoved != null)
                {
                    ModifierRemoved(id);
                }
            }
            else
            {
                if (ModifierPicked != null)
                {
                    ModifierPicked(id);
                }
            }
        }
    }
}
