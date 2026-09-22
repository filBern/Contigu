using System;
using System.Collections.Generic;
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
        // Upgrade ("mystery box") cards only — modifier cards now size
        // themselves dynamically to fit their name/description, see
        // BuildModifierCards.
        private const float CardHeight = 200f;
        private const float BadgeSize = 90f;
        private const float BuyButtonHeight = 36f;
        private const float BuyButtonBottomMargin = 12f;

        // Modifier card layout (on explicit request: "on peut rajouter le
        // nom en haut de l'icon et sa description sous son icon" — the card
        // used to show only the badge + buy button, name/description only
        // ever appeared in the hover tooltip).
        private const float ModifierCardTopPadding = 10f;
        private const float ModifierCardGap = 6f;
        private const float ModifierNameHeight = 26f;
        private const int ModifierDescFontSize = 12;
        // Floor for the description box even when every current slot's
        // description happens to be very short, so the card never looks
        // collapsed.
        private const float ModifierDescMinHeight = 40f;

        public event Action<int> ModifierBuyRequested;
        public event Action<int> UpgradeBuyRequested;
        public event Action RerollRequested;
        public event Action LeaveRequested;

        // Where the modifier card row starts (top pivot), and the 2 gaps
        // reused below to place the "Upgrades" section under it — its own Y
        // used to be a fixed -344f assuming a fixed CardHeight, which
        // overlapped the modifier cards once those grew tall enough to fit
        // a name + description (bug report: "il y a des overlaps entre
        // modifiers et upgrades"). It's now placed right after however
        // tall the modifier row actually turns out to be this refresh.
        private const float ModifierCardsTopY = -124f;
        private const float SectionGap = 20f;
        private const float LabelToCardsGap = 24f;

        private TooltipView _tooltip;
        private RectTransform _root;
        private Text _lueurLabel;
        private RectTransform _modifierCardsContainer;
        private Text _upgradeSectionLabel;
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

            _modifierCardsContainer = BuildCardRow("ModifierCards", ModifierCardsTopY);

            _upgradeSectionLabel = UIFactory.CreateText(_root, "UpgradeLabel", "Upgrades", 16, UITheme.TextMuted);
            _upgradeSectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _upgradeSectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _upgradeSectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _upgradeSectionLabel.rectTransform.sizeDelta = new Vector2(900f, 22f);

            // Real Y positions set every Refresh(), once the modifier row's
            // actual (dynamic) height for this shop visit is known.
            _upgradeCardsContainer = BuildCardRow("UpgradeCards", 0f);

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

            float modifierCardHeight = BuildModifierCards(run);
            float upgradeSectionY = ModifierCardsTopY - modifierCardHeight - SectionGap;
            _upgradeSectionLabel.rectTransform.anchoredPosition = new Vector2(0f, upgradeSectionY);
            _upgradeCardsContainer.anchoredPosition = new Vector2(0f, upgradeSectionY - LabelToCardsGap);

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

        /// <summary>
        /// Builds all modifier cards in 2 passes so they share one uniform
        /// height even though each card's description text is a different
        /// length: pass 1 builds every card and measures its own
        /// description's natural (wrapped) height via Text.preferredHeight-
        /// style generation settings (same technique as
        /// UpgradeCardFactory.PreferredHeight); pass 2 applies the tallest
        /// one found to every card and its description box, so the buy
        /// button always lands at the same Y across the row regardless of
        /// which modifiers are currently offered. Returns that shared card
        /// height so the caller can place whatever comes below the row
        /// (the "Upgrades" section) without overlapping it.
        /// </summary>
        private float BuildModifierCards(RunManager run)
        {
            ClearChildren(_modifierCardsContainer);

            var cardRects = new List<RectTransform>(run.ShopModifierSlots.Count);
            var descRects = new List<RectTransform>(run.ShopModifierSlots.Count);
            float maxDescHeight = ModifierDescMinHeight;

            for (int i = 0; i < run.ShopModifierSlots.Count; i++)
            {
                float descHeight = BuildModifierCard(run, i, out var cardRect, out var descRect);
                cardRects.Add(cardRect);
                descRects.Add(descRect);
                if (descHeight > maxDescHeight)
                {
                    maxDescHeight = descHeight;
                }
            }

            float cardHeight = ModifierCardTopPadding + ModifierNameHeight + ModifierCardGap
                + BadgeSize + ModifierCardGap + maxDescHeight + ModifierCardGap
                + BuyButtonHeight + BuyButtonBottomMargin;

            for (int i = 0; i < cardRects.Count; i++)
            {
                cardRects[i].sizeDelta = new Vector2(CardWidth, cardHeight);
                cardRects[i].GetComponent<LayoutElement>().preferredHeight = cardHeight;
                if (descRects[i] != null)
                {
                    descRects[i].sizeDelta = new Vector2(descRects[i].sizeDelta.x, maxDescHeight);
                }
            }

            return cardHeight;
        }

        /// <summary>Builds one modifier card's contents (name, bare icon, description, buy button) and returns its description's own natural height — <paramref name="cardRect"/>/<paramref name="descRect"/> are handed back so BuildModifierCards can resize them once the row's shared height is known; an empty slot returns a null descRect and 0f height.</summary>
        private float BuildModifierCard(RunManager run, int index, out RectTransform cardRect, out RectTransform descRect)
        {
            var slot = run.ShopModifierSlots[index];
            var card = UIFactory.CreatePanel(_modifierCardsContainer, "ModSlot_" + index, UITheme.PanelLight);
            cardRect = card.rectTransform;
            cardRect.sizeDelta = new Vector2(CardWidth, 0f);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            descRect = null;
            if (slot == null)
            {
                return 0f;
            }

            var def = ModifierCatalog.Get(slot.ModifierId);

            var nameLabel = UIFactory.CreateText(card.transform, "Name", def.Name, 16, UITheme.TextPrimary);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -ModifierCardTopPadding);
            nameLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, ModifierNameHeight);

            // No colored background (explicit request: "enlever le carré
            // coloré derrière l'icon") — the card now carries the name and
            // description as its own text, so the category-colored chip
            // read as redundant clutter.
            var badge = ModifierBadgeFactory.Create(card.transform, def, BadgeSize, _tooltip, showBackground: false);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            badge.rectTransform.pivot = new Vector2(0.5f, 1f);
            float badgeY = -(ModifierCardTopPadding + ModifierNameHeight + ModifierCardGap);
            badge.rectTransform.anchoredPosition = new Vector2(0f, badgeY);

            float descWidth = CardWidth - 16f;
            var descLabel = UIFactory.CreateText(card.transform, "Desc", DescriptionTextFormatter.Colorize(def.Description), ModifierDescFontSize, UITheme.TextPrimary);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, badgeY - (BadgeSize + ModifierCardGap));
            descLabel.rectTransform.sizeDelta = new Vector2(descWidth, 0f);
            descRect = descLabel.rectTransform;

            int price = run.GetModifierSlotPrice(index);
            bool atCap = run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            BuildBuyButton(card.transform, slot.Purchased, atCap ? "Full (" + EconomyConstants.MaxActiveModifiers + ")" : price.ToString(),
                !slot.Purchased && !atCap && run.PendingUpgrade == null && run.Lueur >= price,
                () => OnModifierBuyClicked(index));

            return PreferredHeight(descLabel, descWidth);
        }

        /// <summary>Same technique as UpgradeCardFactory.PreferredHeight — the wrapped text height a Text component would need at a given width, without requiring its RectTransform to already have that width applied.</summary>
        private static float PreferredHeight(Text text, float width)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(text.text, settings);
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
            buyRect.anchoredPosition = new Vector2(0f, BuyButtonBottomMargin);
            buyRect.sizeDelta = new Vector2(CardWidth - 24f, BuyButtonHeight);
            buyBtn.interactable = !purchased && interactable;
            buyBtn.onClick.AddListener(() => onClick());
        }
    }
}
