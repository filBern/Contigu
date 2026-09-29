using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round Lueur shop. Redesigned (explicit request: "au lieu
    /// d'une section modifiers et d'une section upgrade, j'aimerais qu'on
    /// ait une section 'blister'... la section 'casino' avec ce que l'on a
    /// déjà comme section upgrade") from the old separate modifier/upgrade
    /// sections into two differently-themed ones: "Blister" (3 slots, a
    /// modifier OR upgrade drawn from one shared bag — see RunManager.
    /// RollBlisterSlot — always shown plainly, exactly like the old
    /// modifier slots were) and "Casino" (2 slots, the original mystery-box
    /// upgrade section, unchanged — only its UpgradePool is shown until
    /// bought). Reroll only refreshes Casino (explicit request: "le bouton
    /// reroll ne reroll pas la section 'blister'") — Blister already shows
    /// its exact contents up front, so rerolling it would be a different
    /// kind of purchase than "try the mystery box again." The player can
    /// buy as many slots as they can afford, in any order, then leave when
    /// ready — nothing here is a forced single pick like the old draft was.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private const float CardWidth = 190f;
        // Casino ("mystery box") cards only — Blister cards now size
        // themselves dynamically to fit their name/description, see
        // BuildBlisterCards.
        private const float CardHeight = 200f;
        private const float BadgeSize = 90f;
        private const float BuyButtonHeight = 36f;
        private const float BuyButtonBottomMargin = 12f;

        // Blister card layout (on explicit request: "on peut rajouter le
        // nom en haut de l'icon et sa description sous son icon" — the card
        // used to show only the badge + buy button, name/description only
        // ever appeared in the hover tooltip). Shared by both a Modifier-
        // kind slot (name/badge/description, via ModifierCardFactory) and
        // an Upgrade-kind slot (name/rarity+pool swatch/description, via
        // BuildBlisterUpgradeCard) so the whole row lands on one uniform
        // height regardless of the mix.
        private const float ModifierCardTopPadding = 10f;
        private const float ModifierCardGap = 6f;
        private const float ModifierNameHeight = 26f;
        // Floor for the description box even when every current slot's
        // description happens to be very short, so the card never looks
        // collapsed.
        private const float ModifierDescMinHeight = 40f;

        public event Action<int> BlisterBuyRequested;
        public event Action<int> UpgradeBuyRequested;
        public event Action RerollRequested;
        public event Action LeaveRequested;

        // Where the Blister card row starts (top pivot), and the 2 gaps
        // reused below to place the "Casino" section under it — its own Y
        // used to be a fixed -344f assuming a fixed CardHeight, which
        // overlapped the Blister cards once those grew tall enough to fit
        // a name + description (bug report: "il y a des overlaps entre
        // modifiers et upgrades"). It's now placed right after however
        // tall the Blister row actually turns out to be this refresh.
        private const float ModifierCardsTopY = -124f;
        private const float SectionGap = 20f;
        private const float LabelToCardsGap = 24f;

        private TooltipView _tooltip;
        private RectTransform _root;
        private Text _lueurLabel;
        private RectTransform _blisterCardsContainer;
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

            var header = UIFactory.CreateText(_root, "Header", "The Lueur Shop", 26, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            header.rectTransform.sizeDelta = new Vector2(900f, 36f);

            // Same "diamond icon instead of a 'Lueur: ' text prefix" treatment
            // as HudView's own Lueur readout (explicit request, after seeing
            // the itch page mockups: "au lieu de marquer Lueur: ... mettre le
            // petit losange orange") — kept consistent across every screen
            // that shows this currency rather than fixing only the HUD.
            var lueurContainer = UIFactory.CreateUIObject("LueurContainer", _root);
            lueurContainer.anchorMin = new Vector2(0.5f, 1f);
            lueurContainer.anchorMax = new Vector2(0.5f, 1f);
            lueurContainer.pivot = new Vector2(0.5f, 1f);
            lueurContainer.anchoredPosition = new Vector2(0f, -62f);
            var lueurLayout = lueurContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            lueurLayout.spacing = 6f;
            lueurLayout.childAlignment = TextAnchor.MiddleCenter;
            lueurLayout.childForceExpandWidth = false;
            lueurLayout.childForceExpandHeight = false;
            var lueurFitter = lueurContainer.gameObject.AddComponent<ContentSizeFitter>();
            lueurFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lueurFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lueurIcon = UIFactory.CreatePanel(lueurContainer, "LueurIcon", VisualDefaults.GoldenColor);
            lueurIcon.rectTransform.sizeDelta = new Vector2(12f, 12f);
            lueurIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 20f;
            lueurIconLayout.preferredHeight = 20f;

            _lueurLabel = UIFactory.CreateText(lueurContainer, "Lueur", "", 20, VisualDefaults.GoldenColor);

            var blisterSection = UIFactory.CreateText(_root, "BlisterLabel", "Blister", 16, UITheme.TextMutedOnBackground);
            blisterSection.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            blisterSection.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            blisterSection.rectTransform.pivot = new Vector2(0.5f, 1f);
            blisterSection.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            blisterSection.rectTransform.sizeDelta = new Vector2(900f, 22f);

            _blisterCardsContainer = BuildCardRow("BlisterCards", ModifierCardsTopY);

            _upgradeSectionLabel = UIFactory.CreateText(_root, "UpgradeLabel", "Casino", 16, UITheme.TextMutedOnBackground);
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

            // Tab opens the deck view from the shop too (on explicit
            // request: the hint shown on the main game screen — see
            // HudView — needed here as well since the shop is its own
            // separate overlay).
            var deckHint = UIFactory.CreateText(_root, "DeckHint", "Tab: view piece deck", 14, UITheme.TextMutedOnBackground);
            var deckHintRect = deckHint.rectTransform;
            deckHintRect.anchorMin = new Vector2(0f, 0f);
            deckHintRect.anchorMax = new Vector2(0f, 0f);
            deckHintRect.pivot = new Vector2(0f, 0f);
            deckHintRect.anchoredPosition = new Vector2(16f, 16f);
            deckHintRect.sizeDelta = new Vector2(220f, 22f);
            deckHint.alignment = TextAnchor.MiddleLeft;

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
            _lueurLabel.text = run.Lueur.ToString();

            float modifierCardHeight = BuildBlisterCards(run);
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
        /// Builds all Blister cards (a mix of Modifier-kind and Upgrade-kind
        /// slots, see RunManager.ShopBlisterSlots) in 2 passes so they share
        /// one uniform height even though each card's description text is a
        /// different length: pass 1 builds every card and measures its own
        /// description's natural (wrapped) height via Text.preferredHeight-
        /// style generation settings (same technique as
        /// UpgradeCardFactory.PreferredHeight); pass 2 applies the tallest
        /// one found to every card and its description box, so the buy
        /// button always lands at the same Y across the row regardless of
        /// which items are currently offered or their mix. Returns that
        /// shared card height so the caller can place whatever comes below
        /// the row (the "Casino" section) without overlapping it.
        /// </summary>
        private float BuildBlisterCards(RunManager run)
        {
            ClearChildren(_blisterCardsContainer);

            var cardRects = new List<RectTransform>(run.ShopBlisterSlots.Count);
            var descRects = new List<RectTransform>(run.ShopBlisterSlots.Count);
            float maxDescHeight = ModifierDescMinHeight;

            for (int i = 0; i < run.ShopBlisterSlots.Count; i++)
            {
                float descHeight = BuildBlisterCard(run, i, out var cardRect, out var descRect);
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

        /// <summary>Builds one Blister card's contents (name, bare icon or rarity swatch, description, buy button) and returns its description's own natural height — <paramref name="cardRect"/>/<paramref name="descRect"/> are handed back so BuildBlisterCards can resize them once the row's shared height is known; an empty slot returns a null descRect and 0f height. Dispatches on the slot's Kind — a Blister slot's identity is always fully shown, modifier or upgrade alike (explicit request: "on aperçoit 3 modifiers ou upgrades"), unlike a Casino slot.</summary>
        private float BuildBlisterCard(RunManager run, int index, out RectTransform cardRect, out RectTransform descRect)
        {
            var slot = run.ShopBlisterSlots[index];
            var card = UIFactory.CreateSlicedImage(_blisterCardsContainer, "BlisterSlot_" + index, UISprites.CardBackground);
            card.color = UITheme.Panel; // card_bg_3 tinted darker (explicit request), instead of the flat PanelLight fill it used before
            cardRect = card.rectTransform;
            cardRect.sizeDelta = new Vector2(CardWidth, 0f);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            UIFactory.AddThickOutline(card, UITheme.Border);

            descRect = null;
            if (slot == null)
            {
                return 0f;
            }

            float descHeight = slot.Kind == ShopSlotKind.Modifier
                ? BuildBlisterModifierCardContents(card.transform, slot.ModifierId, out descRect)
                : BuildBlisterUpgradeCardContents(card.transform, slot.HiddenUpgrade, out descRect);

            int price = run.GetBlisterSlotPrice(index);
            bool blocked = slot.Kind == ShopSlotKind.Modifier && run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            string blockedLabel = "Full (" + EconomyConstants.MaxActiveModifiers + ")";
            BuildBuyButton(card.transform, slot.Purchased, blocked ? blockedLabel : price.ToString(),
                !slot.Purchased && !blocked && run.PendingUpgrade == null && run.Lueur >= price,
                () => OnBlisterBuyClicked(index));

            return descHeight;
        }

        /// <summary>A Blister modifier card's contents — identical to the old, only-ever-modifiers card: name, bare icon, description. No colored background behind the badge (explicit request: "enlever le carré coloré derrière l'icon") and no hover tooltip (explicit request: "Pas besoin du tooltip sur les modifiers qu'on peut acheter dans le shop, seulement dans notre liste de modifiers possédé") — the card already shows its own name/description as static text, so both read as redundant. See ModifierCardFactory for the shared visual (also used by UpgradeRevealView's Random Modifier reveal).</summary>
        private float BuildBlisterModifierCardContents(Transform cardTransform, ModifierId modifierId, out RectTransform descRect)
        {
            var def = ModifierCatalog.Get(modifierId);
            return ModifierCardFactory.BuildContents(cardTransform, def, _tooltip, CardWidth, out descRect);
        }

        /// <summary>
        /// A Blister upgrade card's contents — mirrors ModifierCardFactory.
        /// BuildContents' exact layout (same TopPadding/NameHeight/Gap/
        /// BadgeSize) so a row mixing modifier and upgrade cards still
        /// shares one uniform height, but shows the upgrade's real name and
        /// description plainly instead of a "?" mystery hint — unlike a
        /// Casino card, a Blister slot's identity is never hidden. In place
        /// of a modifier's icon, a rarity-colored swatch names the
        /// upgrade's pool ("Piece Upgrade"/"Tile Upgrade" — see
        /// UpgradeVisualDefaults), since upgrades have no per-item icon art.
        /// </summary>
        private float BuildBlisterUpgradeCardContents(Transform cardTransform, UpgradeDefinition def, out RectTransform descRect)
        {
            var nameLabel = UIFactory.CreateText(cardTransform, "Name", def.Name, 16, UITheme.TextPrimary);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -ModifierCardTopPadding);
            nameLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, ModifierNameHeight);

            float swatchY = -(ModifierCardTopPadding + ModifierNameHeight + ModifierCardGap);
            var swatch = UIFactory.CreatePanel(cardTransform, "PoolSwatch", UpgradeVisualDefaults.GetRarityColor(def.Rarity));
            swatch.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            swatch.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            swatch.rectTransform.pivot = new Vector2(0.5f, 1f);
            swatch.rectTransform.anchoredPosition = new Vector2(0f, swatchY);
            swatch.rectTransform.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            UIFactory.AddThickOutline(swatch, UITheme.Border);

            var swatchLabel = UIFactory.CreateText(swatch.transform, "PoolLabel", UpgradeVisualDefaults.GetPoolLabel(def.Pool), 13, UITheme.TextPrimary);
            swatchLabel.alignment = TextAnchor.MiddleCenter;
            swatchLabel.raycastTarget = false;
            UIFactory.StretchFull(swatchLabel.rectTransform);

            float descWidth = CardWidth - 16f;
            var descLabel = UIFactory.CreateText(cardTransform, "Desc", DescriptionTextFormatter.Colorize(def.Description), 12, UITheme.TextPrimary);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, swatchY - (BadgeSize + ModifierCardGap));
            descLabel.rectTransform.sizeDelta = new Vector2(descWidth, 0f);
            descRect = descLabel.rectTransform;

            var settings = descLabel.GetGenerationSettings(new Vector2(descWidth, 0f));
            return descLabel.cachedTextGenerator.GetPreferredHeight(descLabel.text, settings);
        }

        private void BuildUpgradeCard(RunManager run, int index)
        {
            var slot = run.ShopUpgradeSlots[index];
            var card = UIFactory.CreateSlicedImage(_upgradeCardsContainer, "UpgSlot_" + index, UISprites.CardBackground);
            card.color = UITheme.Panel; // card_bg_3 tinted darker (explicit request), instead of the flat PanelLight fill it used before
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            UIFactory.AddThickOutline(card, UITheme.Border);

            if (slot == null)
            {
                return;
            }

            // Mystery box — only the pool is shown, never the specific
            // upgrade (spec: "tout ce que tu sais c'est l'upgrade se situe
            // dans quel UpgradePool"), even once purchased (the reveal
            // happens in the follow-up sub-choice/tile-choice overlay
            // instead, not on this card). Random Modifier is the one
            // exception (explicit request, while validating its boosted
            // odds: "je veux que ce soit marqué random modifier") — it's
            // named outright instead of showing the generic Bank-pool
            // "Piece upgrade" label.
            bool isRandomModifier = slot.HiddenUpgrade != null && slot.HiddenUpgrade.Id == UpgradeId.RandomModifier;
            string cardLabel = isRandomModifier ? "Random modifier" : UpgradeVisualDefaults.GetPoolLabel(slot.Pool) + " upgrade";
            var poolLabel = UIFactory.CreateText(card.transform, "Pool", cardLabel, 20, UITheme.TextPrimary);
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
            // Random Modifier is the one upgrade that respects the
            // modifier cap (explicit report: "si le joueur a un random
            // modifier comme upgrade et qu'il est full il ne devrait pas
            // pouvoir l'acheter") — buying it while already full used to
            // still charge Lueur and grant nothing (see RunManager.
            // BuyUpgradeSlot). Every other upgrade ignores the cap.
            bool atModifierCap = isRandomModifier && run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            // Modifier Upgrade needs an already-owned modifier to level up
            // — same "don't sell it with nothing for it to do" precedent
            // as the cap check above (see RunManager.BuyUpgradeSlot).
            bool isModifierUpgrade = slot.HiddenUpgrade != null && slot.HiddenUpgrade.Id == UpgradeId.ModifierUpgrade;
            bool hasNoModifiersToUpgrade = isModifierUpgrade && run.ActiveModifiers.Count == 0;
            bool blocked = atModifierCap || hasNoModifiersToUpgrade;
            string blockedLabel = atModifierCap ? "Full (" + EconomyConstants.MaxActiveModifiers + ")" : "None owned";
            BuildBuyButton(card.transform, slot.Purchased, blocked ? blockedLabel : price.ToString(),
                !slot.Purchased && !blocked && run.PendingUpgrade == null && run.Lueur >= price,
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

        private void OnBlisterBuyClicked(int index)
        {
            if (BlisterBuyRequested != null)
            {
                BlisterBuyRequested(index);
            }
        }

        private void OnUpgradeBuyClicked(int index)
        {
            if (UpgradeBuyRequested != null)
            {
                UpgradeBuyRequested(index);
            }
        }

        /// <summary>The buy button showed "Buy (123)" as plain text; now it shows the same gold-diamond Lueur icon + number used everywhere else this currency appears (explicit request: "au lieu d'afficher Buy, met l'icon de lueuer"), leaving "Sold" as plain text since there's no price left to show once purchased.</summary>
        private static void BuildBuyButton(Transform parent, bool purchased, string priceLabel, bool interactable, Action onClick)
        {
            var buyBtn = UIFactory.CreateButton(parent, "Buy", purchased ? "Sold" : "", UISprites.ChooseButtonBackground, 14);
            var buyRect = buyBtn.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, BuyButtonBottomMargin);
            buyRect.sizeDelta = new Vector2(CardWidth - 24f, BuyButtonHeight);
            buyBtn.interactable = !purchased && interactable;
            buyBtn.onClick.AddListener(() => onClick());

            if (purchased)
            {
                return;
            }

            // Same HorizontalLayoutGroup + ContentSizeFitter icon+number
            // pattern as the shop's own header Lueur readout, just centered
            // on the button instead of anchored to a corner, and sized down
            // to fit comfortably inside one this small.
            var priceContainer = UIFactory.CreateUIObject("Price", buyBtn.transform);
            priceContainer.anchorMin = new Vector2(0.5f, 0.5f);
            priceContainer.anchorMax = new Vector2(0.5f, 0.5f);
            priceContainer.pivot = new Vector2(0.5f, 0.5f);
            var priceLayout = priceContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            priceLayout.spacing = 4f;
            priceLayout.childAlignment = TextAnchor.MiddleCenter;
            priceLayout.childForceExpandWidth = false;
            priceLayout.childForceExpandHeight = false;
            var priceFitter = priceContainer.gameObject.AddComponent<ContentSizeFitter>();
            priceFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            priceFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var priceIcon = UIFactory.CreatePanel(priceContainer, "PriceIcon", VisualDefaults.GoldenColor);
            priceIcon.raycastTarget = false;
            priceIcon.rectTransform.sizeDelta = new Vector2(10f, 10f);
            priceIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var priceIconLayout = priceIcon.gameObject.AddComponent<LayoutElement>();
            priceIconLayout.preferredWidth = 16f;
            priceIconLayout.preferredHeight = 16f;

            var priceText = UIFactory.CreateText(priceContainer, "PriceLabel", priceLabel, 21, UITheme.TextPrimary);
            priceText.raycastTarget = false;
        }
    }
}
