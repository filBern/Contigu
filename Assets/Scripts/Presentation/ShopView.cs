using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round Lueur shop (spec extension, explicit request — replaces
    /// the old draft/modifier-pick screens entirely): 3 modifier slots shown
    /// plainly, 2 upgrade slots that only reveal their pool (Bank/Grid) until
    /// bought, and a reroll that refreshes every still-unsold slot. The
    /// player can buy as many slots as they can afford, in any order, then
    /// leave when ready — nothing here is a forced single pick like the old
    /// draft was.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private const float CardWidth = 190f;
        private const float CardHeight = 200f;
        private const float BadgeSize = 90f;

        public event Action<int> ModifierBuyRequested;
        public event Action<int> UpgradeBuyRequested;
        public event Action RerollRequested;
        public event Action LeaveRequested;

        private TooltipView _tooltip;
        private RectTransform _root;
        private Text _lueurLabel;
        private RectTransform _modifierCardsContainer;
        private RectTransform _upgradeCardsContainer;
        private Button _rerollButton;
        private Text _rerollLabel;
        private Button _leaveButton;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "ShopOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "The Lueur Shop", 26, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            header.rectTransform.sizeDelta = new Vector2(900f, 36f);

            _lueurLabel = UIFactory.CreateText(_root, "Lueur", "", 20, VisualDefaults.GoldenColor);
            _lueurLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _lueurLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _lueurLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _lueurLabel.rectTransform.anchoredPosition = new Vector2(0f, -62f);
            _lueurLabel.rectTransform.sizeDelta = new Vector2(900f, 28f);

            var modifierSection = UIFactory.CreateText(_root, "ModifierLabel", "Modifiers", 16, UITheme.TextMuted);
            modifierSection.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            modifierSection.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            modifierSection.rectTransform.pivot = new Vector2(0.5f, 1f);
            modifierSection.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            modifierSection.rectTransform.sizeDelta = new Vector2(900f, 22f);

            _modifierCardsContainer = BuildCardRow("ModifierCards", -124f);

            var upgradeSection = UIFactory.CreateText(_root, "UpgradeLabel", "Upgrades", 16, UITheme.TextMuted);
            upgradeSection.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            upgradeSection.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            upgradeSection.rectTransform.pivot = new Vector2(0.5f, 1f);
            upgradeSection.rectTransform.anchoredPosition = new Vector2(0f, -344f);
            upgradeSection.rectTransform.sizeDelta = new Vector2(900f, 22f);

            _upgradeCardsContainer = BuildCardRow("UpgradeCards", -368f);

            _rerollButton = UIFactory.CreateButton(_root, "Reroll", "", UISprites.CancelButtonBackground, 16);
            _rerollLabel = _rerollButton.GetComponentInChildren<Text>();
            var rerollRect = _rerollButton.GetComponent<RectTransform>();
            rerollRect.anchorMin = new Vector2(0.5f, 0f);
            rerollRect.anchorMax = new Vector2(0.5f, 0f);
            rerollRect.pivot = new Vector2(0.5f, 0f);
            rerollRect.anchoredPosition = new Vector2(-120f, 40f);
            rerollRect.sizeDelta = new Vector2(220f, 46f);
            _rerollButton.onClick.AddListener(OnRerollClicked);

            _leaveButton = UIFactory.CreateButton(_root, "Leave", "Next round", UISprites.ChooseButtonBackground, 16);
            var leaveRect = _leaveButton.GetComponent<RectTransform>();
            leaveRect.anchorMin = new Vector2(0.5f, 0f);
            leaveRect.anchorMax = new Vector2(0.5f, 0f);
            leaveRect.pivot = new Vector2(0.5f, 0f);
            leaveRect.anchoredPosition = new Vector2(120f, 40f);
            leaveRect.sizeDelta = new Vector2(220f, 46f);
            _leaveButton.onClick.AddListener(OnLeaveClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private RectTransform BuildCardRow(string name, float topOffset)
        {
            var container = UIFactory.CreateUIObject(name, _root);
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = new Vector2(0f, topOffset);

            var layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return container;
        }

        public void Show(RunManager run)
        {
            _root.gameObject.SetActive(true);
            Refresh(run);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Rebuilds every card from the run's current shop state — called on Show and after every purchase/reroll so prices, affordability and "sold" states stay accurate.</summary>
        public void Refresh(RunManager run)
        {
            _lueurLabel.text = "Lueur: " + run.Lueur;

            ClearChildren(_modifierCardsContainer);
            for (int i = 0; i < run.ShopModifierSlots.Count; i++)
            {
                BuildModifierCard(run, i);
            }

            ClearChildren(_upgradeCardsContainer);
            for (int i = 0; i < run.ShopUpgradeSlots.Count; i++)
            {
                BuildUpgradeCard(run, i);
            }

            int rerollPrice = run.GetRerollPrice();
            _rerollLabel.text = "Reroll (" + rerollPrice + ")";
            _rerollButton.interactable = run.PendingUpgrade == null && run.Lueur >= rerollPrice;
            _leaveButton.interactable = run.PendingUpgrade == null;
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void BuildModifierCard(RunManager run, int index)
        {
            var slot = run.ShopModifierSlots[index];
            var card = UIFactory.CreatePanel(_modifierCardsContainer, "ModSlot_" + index, UITheme.PanelLight);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (slot == null)
            {
                return;
            }

            var def = ModifierCatalog.Get(slot.ModifierId);
            var badge = ModifierBadgeFactory.Create(card.transform, def, BadgeSize, _tooltip);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            badge.rectTransform.pivot = new Vector2(0.5f, 1f);
            badge.rectTransform.anchoredPosition = new Vector2(0f, -14f);

            int price = run.GetModifierSlotPrice(index);
            bool atCap = run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            BuildBuyButton(card.transform, slot.Purchased, atCap ? "Full (" + EconomyConstants.MaxActiveModifiers + ")" : price.ToString(),
                !slot.Purchased && !atCap && run.PendingUpgrade == null && run.Lueur >= price,
                () => OnModifierBuyClicked(index));
        }

        private void BuildUpgradeCard(RunManager run, int index)
        {
            var slot = run.ShopUpgradeSlots[index];
            var card = UIFactory.CreatePanel(_upgradeCardsContainer, "UpgSlot_" + index, UITheme.PanelLight);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (slot == null)
            {
                return;
            }

            // Mystery box — only the pool is shown, never the specific
            // upgrade (spec: "tout ce que tu sais c'est l'upgrade se situe
            // dans quel UpgradePool"), even once purchased (the reveal
            // happens in the follow-up sub-choice/tile-choice overlay
            // instead, not on this card).
            var poolLabel = UIFactory.CreateText(card.transform, "Pool", UpgradeVisualDefaults.GetPoolLabel(slot.Pool) + " upgrade", 20, UITheme.TextPrimary);
            poolLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchoredPosition = new Vector2(0f, -50f);
            poolLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 60f);

            var mysteryHint = UIFactory.CreateText(card.transform, "Hint", "?", 40, UITheme.TextMuted);
            mysteryHint.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            mysteryHint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            mysteryHint.rectTransform.pivot = new Vector2(0.5f, 1f);
            mysteryHint.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            mysteryHint.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 40f);

            int price = run.GetUpgradeSlotPrice(index);
            BuildBuyButton(card.transform, slot.Purchased, price.ToString(),
                !slot.Purchased && run.PendingUpgrade == null && run.Lueur >= price,
                () => OnUpgradeBuyClicked(index));
        }

        private void OnRerollClicked()
        {
            if (RerollRequested != null)
            {
                RerollRequested();
            }
        }

        private void OnLeaveClicked()
        {
            if (LeaveRequested != null)
            {
                LeaveRequested();
            }
        }

        private void OnModifierBuyClicked(int index)
        {
            if (ModifierBuyRequested != null)
            {
                ModifierBuyRequested(index);
            }
        }

        private void OnUpgradeBuyClicked(int index)
        {
            if (UpgradeBuyRequested != null)
            {
                UpgradeBuyRequested(index);
            }
        }

        private static void BuildBuyButton(Transform parent, bool purchased, string priceLabel, bool interactable, Action onClick)
        {
            var buyBtn = UIFactory.CreateButton(parent, "Buy", purchased ? "Sold" : "Buy (" + priceLabel + ")", UISprites.ChooseButtonBackground, 14);
            var buyRect = buyBtn.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, 12f);
            buyRect.sizeDelta = new Vector2(CardWidth - 24f, 36f);
            buyBtn.interactable = !purchased && interactable;
            buyBtn.onClick.AddListener(() => onClick());
        }
    }
}
