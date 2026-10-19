using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round Lueur shop, split into two sections: "Blister" (3 slots,
    /// a modifier or upgrade drawn from one shared bag, see RunManager.
    /// RollBlisterSlot, always shown plainly) and "Casino" (2 slots, a
    /// mystery-box upgrade section where only the UpgradePool is shown until
    /// bought). Reroll only refreshes Casino, since Blister already shows its
    /// exact contents up front. The player can buy as many slots as they can
    /// afford, in any order, then leave when ready.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private const float CardWidth = 190f;
        // Casino cards only — Blister cards size themselves dynamically to
        // fit their name/description, see BuildBlisterCards.
        private const float CardHeight = 200f;
        private const float BadgeSize = 90f;
        private const float BuyButtonHeight = 46f;
        private const float BuyButtonWidth = 112f;
        // Blister cards show a bigger icon now that their description lives in a hover tooltip.
        private const float BlisterIconSize = 112f;
        private const float BuyButtonBottomMargin = 12f;

        // Blister card layout, shared by both a Modifier-kind slot (name/
        // badge/description, via ModifierCardFactory) and an Upgrade-kind
        // slot (name/rarity+pool swatch/description, via BuildBlisterUpgradeCard)
        // so the whole row lands on one uniform height regardless of the mix.
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
        // reused below to place the "Casino" section under it. The Casino
        // section is placed right after however tall the Blister row
        // actually turns out to be this refresh, since Blister card height
        // varies with description length.
        private const float ModifierCardsTopY = -124f;
        private const float SectionGap = 20f;
        private const float LabelToCardsGap = 24f;

        // Slow, gentle scale wobble on the Lueur readout, same sine-wave
        // technique as GameBootstrap's PulseStatusText.
        private const float LueurPulseAmplitude = 0.06f;
        private const float LueurPulseSpeed = 1.1f;
        // Quick overshoot pop on a successful purchase, played in place of the ambient pulse above (which
        // is paused for its duration, see PlayPurchasePop) rather than fighting it for localScale.
        private const float PurchasePopDuration = 0.3f;
        private const float PurchasePopPeakScale = 1.4f;
        private const float PurchasePopPeakFraction = 0.35f;
        // "Prochain combat" preview box under the (now-raised) Lueur readout — sized and positioned to match
        // the left-side ModifierPanelView exactly (same width, header height, header-to-content gap, badge
        // size), one enemy stacked per row instead of side-by-side since the width is a single panel's
        // rather than a wide band like HudView's enemy row.
        private const float UpcomingBoxCenterX = -225f;
        private const float UpcomingBoxWidth = ModifierPanelView.PanelWidth;
        private const float UpcomingBoxHeight = ModifierPanelView.PanelHeight;
        // The box is vertically centered on screen (anchoredPosition.y = 0, see Build) at a fixed height,
        // so its top edge sits exactly half its height above that center line.
        private const float UpcomingBoxTopY = UpcomingBoxHeight / 2f;
        // Clearance above the box's top edge for the Lueur readout's own (dynamic, ContentSizeFitter-driven)
        // height plus a visible gap, so it reads as sitting above the box rather than overlapping it.
        private const float LueurAboveBoxClearance = 70f;
        private const float UpcomingBannerHeight = ModifierPanelView.HeaderHeight;
        private const int UpcomingBannerFontSize = 28;
        private const float UpcomingBoxTopPadding = ModifierPanelView.HeaderRowGap;
        private const float UpcomingBoxBottomPadding = ModifierPanelView.BottomPadding;
        // Icon size, HP label height/font and row gap below are each this round's full-size default — a
        // round with few enough enemies to fit the box at full size uses these as-is (count<=2 always did,
        // and still does). BuildUpcomingEncounterPreview scales all three down together, proportionally,
        // only when the actual enemy count for this round wouldn't otherwise fit the box's fixed content
        // height, so every enemy the next round has is always shown — none clipped by the box's RectMask2D.
        private const float UpcomingIconSize = ModifierCardFactory.BadgeSize;
        private const float UpcomingSlotWidth = UpcomingBoxWidth - 16f;
        private const float UpcomingRowGap = ModifierCardFactory.Gap;
        // Never shrink the icon below this, however many enemies a round has — past this point the
        // portrait and its tooltip hotspot would stop being usable.
        private const float UpcomingMinIconSize = 24f;
        // Same decaying side-to-side shake HandView.ShakeSlot plays for an unplayable piece, reused here for
        // a card clicked without enough Lueur to afford it.
        private const float CantAffordShakeDuration = 0.3f;
        private const float CantAffordShakeMagnitude = 8f;
        private const float CantAffordShakeCycles = 4f;
        // A quick vertical flip-reveal on the Casino row once a reroll's new cards are already built —
        // Refresh() runs synchronously right after RerollShop(), so there's no window to animate the OLD
        // cards away; this instead gives the NEW ones a "flipping into place" entrance.
        private const float RerollFlipDuration = 0.25f;
        private const float RerollFlipOvershootScale = 1.15f;
        private const float RerollFlipOvershootFraction = 0.6f;
        // Shrink-and-fade played on a just-bought Blister card before the row is rebuilt without it — the
        // rebuild itself (see BuildBlisterCards skipping Purchased slots) is what recenters the remaining
        // cards, since HorizontalLayoutGroup + the row's ContentSizeFitter re-pack and re-center around
        // whatever children exist once the bought card's GameObject is gone.
        private const float BlisterRemoveDuration = 0.25f;

        private TooltipView _tooltip;
        private RectTransform _root;
        private Text _lueurLabel;
        private RectTransform _lueurContainer;
        private Coroutine _lueurPulseCoroutine;
        private RectTransform _upcomingEncounterBox;
        private RectTransform _upcomingEncounterRow;
        private RectTransform _weatherRow;
        private Image _weatherIcon;
        private Text _weatherLabel;
        private HoverTooltip _weatherTooltip;
        private const float WeatherRowHeight = 44f;
        private GridLayoutGroup _upcomingEncounterGrid;
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

            var header = UIFactory.CreateText(_root, "Header", "The Pollen Shop", 26, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            header.rectTransform.sizeDelta = new Vector2(900f, 36f);

            // Diamond icon instead of a "Lueur: " text prefix, consistent
            // with HudView's own readout. Larger here than in the HUD, with
            // a slow pulse (see PulseLueurLabel) since this is the one
            // screen where the player actually spends it.
            // Pivot centered (rather than right-edge) so its horizontal center stays fixed at
            // UpcomingBoxCenterX regardless of how many digits the Lueur count grows to — the box below
            // lines its own center up with this same X (see BuildUpcomingEncounterPreview's parent box).
            // Y sits clear above the box's own (now fixed, centered) top edge, see UpcomingBoxTopY/
            // LueurAboveBoxClearance, instead of the old fixed 120 which the box's current position/height
            // could end up overlapping.
            _lueurContainer = UIFactory.CreateUIObject("LueurContainer", _root);
            _lueurContainer.anchorMin = new Vector2(1f, 0.5f);
            _lueurContainer.anchorMax = new Vector2(1f, 0.5f);
            _lueurContainer.pivot = new Vector2(0.5f, 0.5f);
            _lueurContainer.anchoredPosition = new Vector2(UpcomingBoxCenterX, UpcomingBoxTopY + LueurAboveBoxClearance);
            var lueurLayout = _lueurContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            lueurLayout.spacing = 14f;
            lueurLayout.childAlignment = TextAnchor.MiddleCenter;
            lueurLayout.childForceExpandWidth = false;
            lueurLayout.childForceExpandHeight = false;
            var lueurFitter = _lueurContainer.gameObject.AddComponent<ContentSizeFitter>();
            lueurFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lueurFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lueurIcon = UIFactory.CreatePanel(_lueurContainer, "LueurIcon", VisualDefaults.GoldenColor);
            lueurIcon.rectTransform.sizeDelta = new Vector2(40f, 40f);
            if (VisualDefaults.PollenSprite != null)
            {
                lueurIcon.sprite = VisualDefaults.PollenSprite;
                lueurIcon.color = Color.white;
                lueurIcon.preserveAspect = true;
            }
            else
            {
                lueurIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 58f;
            lueurIconLayout.preferredHeight = 58f;

            _lueurLabel = UIFactory.CreateText(_lueurContainer, "Pollen", "", 84, VisualDefaults.GoldenColor);
            _lueurContainer.gameObject.AddComponent<PollenFx>().Init(lueurIcon.rectTransform, _lueurLabel);

            // "Prochain combat" preview — sits right under the (now-raised) Lueur readout, same corner.
            // Contents rebuilt every Refresh from run.UpcomingEncounterIds, since that's still null/empty
            // until the shop is actually open (it only ever reads the catalog, no RNG, see RunManager).
            var upcomingBox = UIFactory.CreateSlicedImage(_root, "UpcomingEncounterBox", UISprites.CardBackground);
            _upcomingEncounterBox = upcomingBox.rectTransform;
            // Same anchor/pivot/position recipe as ModifierPanelView.Build (anchor+pivot both (0.5,0.5),
            // vertically centered at y=0) — matching its exact recipe, not just eyeballing a Y offset, is
            // what actually makes the two boxes' top AND bottom edges line up, since they also share the
            // same fixed height (PanelHeight) below rather than one growing with its own content.
            _upcomingEncounterBox.anchorMin = new Vector2(1f, 0.5f);
            _upcomingEncounterBox.anchorMax = new Vector2(1f, 0.5f);
            _upcomingEncounterBox.pivot = new Vector2(0.5f, 0.5f);
            _upcomingEncounterBox.anchoredPosition = new Vector2(UpcomingBoxCenterX, 0f);
            _upcomingEncounterBox.sizeDelta = new Vector2(UpcomingBoxWidth, UpcomingBoxHeight);
            // Clips any overflow instead of letting it spill below the box — the box's height is fixed
            // (matching ModifierPanelView, which is itself sized for a fixed slot count) while the next
            // round's enemy count isn't capped, so a very populated round can run past the available space.
            _upcomingEncounterBox.gameObject.AddComponent<RectMask2D>();

            // Red "Next round" banner flush against the box's top edge, same width as the box.
            var upcomingBanner = UIFactory.CreateSlicedImage(_upcomingEncounterBox, "NextRoundBanner", UISprites.Garden("Red"));
            upcomingBanner.rectTransform.anchorMin = new Vector2(0f, 1f);
            upcomingBanner.rectTransform.anchorMax = new Vector2(1f, 1f);
            upcomingBanner.rectTransform.pivot = new Vector2(0.5f, 1f);
            upcomingBanner.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            upcomingBanner.rectTransform.sizeDelta = new Vector2(-20f, UpcomingBannerHeight - 8f);

            var upcomingBannerLabel = UIFactory.CreateText(upcomingBanner.transform, "BannerLabel", "Next round", UpcomingBannerFontSize, UITheme.TextOnBackground);
            upcomingBannerLabel.alignment = TextAnchor.MiddleCenter;
            UIFactory.StretchFull(upcomingBannerLabel.rectTransform);

            // One enemy per cell, one column while count<=2 (reads as a plain vertical stack), two columns
            // side by side beyond that (see BuildUpcomingEncounterPreview, which sets constraintCount/
            // cellSize/spacing each Refresh based on the next round's actual enemy count) — same
            // GridLayoutGroup technique as ModifierPanelView's own badge grid.
            _upcomingEncounterRow = UIFactory.CreateUIObject("UpcomingEncounterRow", _upcomingEncounterBox);
            _upcomingEncounterRow.anchorMin = new Vector2(0.5f, 1f);
            _upcomingEncounterRow.anchorMax = new Vector2(0.5f, 1f);
            _upcomingEncounterRow.pivot = new Vector2(0.5f, 1f);
            _upcomingEncounterRow.anchoredPosition = new Vector2(0f, -(UpcomingBannerHeight + UpcomingBoxTopPadding));
            _upcomingEncounterRow.sizeDelta = new Vector2(UpcomingSlotWidth, UpcomingBoxHeight - UpcomingBannerHeight - UpcomingBoxTopPadding - UpcomingBoxBottomPadding - WeatherRowHeight - 6f);
            _upcomingEncounterGrid = _upcomingEncounterRow.gameObject.AddComponent<GridLayoutGroup>();
            _upcomingEncounterGrid.childAlignment = TextAnchor.UpperCenter;
            _upcomingEncounterGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            // Next round's weather, at the bottom of the box; hover for what it does.
            _weatherRow = UIFactory.CreateUIObject("WeatherRow", _upcomingEncounterBox);
            _weatherRow.anchorMin = new Vector2(0.5f, 0f);
            _weatherRow.anchorMax = new Vector2(0.5f, 0f);
            _weatherRow.pivot = new Vector2(0.5f, 0f);
            _weatherRow.anchoredPosition = new Vector2(0f, UpcomingBoxBottomPadding);
            _weatherRow.sizeDelta = new Vector2(UpcomingSlotWidth, WeatherRowHeight);
            var weatherHit = _weatherRow.gameObject.AddComponent<Image>();
            weatherHit.color = new Color(0f, 0f, 0f, 0.07f);
            _weatherTooltip = _weatherRow.gameObject.AddComponent<HoverTooltip>();
            _weatherIcon = UIFactory.CreatePanel(_weatherRow, "WeatherIcon", Color.white);
            _weatherIcon.raycastTarget = false;
            _weatherIcon.preserveAspect = true;
            _weatherIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _weatherIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _weatherIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            _weatherIcon.rectTransform.anchoredPosition = new Vector2(8f, 0f);
            _weatherIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);
            _weatherLabel = UIFactory.CreateText(_weatherRow, "WeatherLabel", "", 16, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            _weatherLabel.raycastTarget = false;
            UIFactory.StretchFull(_weatherLabel.rectTransform);
            _weatherLabel.rectTransform.offsetMin = new Vector2(52f, 0f);
            _weatherLabel.rectTransform.offsetMax = new Vector2(-6f, 0f);

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
            UIFactory.DisablePlants(_rerollButton);
            _rerollLabel = _rerollButton.GetComponentInChildren<Text>();
            var rerollRect = _rerollButton.GetComponent<RectTransform>();
            rerollRect.anchorMin = new Vector2(0.5f, 0f);
            rerollRect.anchorMax = new Vector2(0.5f, 0f);
            rerollRect.pivot = new Vector2(0.5f, 0f);
            rerollRect.anchoredPosition = new Vector2(-120f, 40f);
            rerollRect.sizeDelta = new Vector2(220f, 46f);
            _rerollButton.onClick.AddListener(OnRerollClicked);

            _leaveButton = UIFactory.CreateButton(_root, "Leave", "Next round", UISprites.ChooseButtonBackground, 16);
            UIFactory.DisablePlants(_leaveButton);
            var leaveRect = _leaveButton.GetComponent<RectTransform>();
            leaveRect.anchorMin = new Vector2(0.5f, 0f);
            leaveRect.anchorMax = new Vector2(0.5f, 0f);
            leaveRect.pivot = new Vector2(0.5f, 0f);
            leaveRect.anchoredPosition = new Vector2(120f, 40f);
            leaveRect.sizeDelta = new Vector2(220f, 46f);
            _leaveButton.onClick.AddListener(OnLeaveClicked);

            // Tab opens the deck view from the shop too, since the shop is
            // its own separate overlay (same hint HudView shows).
            var deckHint = UIFactory.CreateText(_root, "DeckHint", "Tab: view your seed bag", 14, UITheme.TextMutedOnBackground);
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

        /// <summary>Replaces the shop's own Pollen readout with the HUD's, so a single counter (in the HUD's spot) serves both. While the shop is open it is lifted above the shop overlay.</summary>
        public void UseSharedLueur(RectTransform container, Text label)
        {
            if (_lueurContainer != null)
            {
                Destroy(_lueurContainer.gameObject);
            }
            _lueurContainer = container;
            _lueurLabel = label;
            _lueurOriginalIndex = container.GetSiblingIndex();
        }

        private int _lueurOriginalIndex;

        // Sibling order instead of a nested Canvas override, so the tooltip and the draft overlays (siblings later in the list) still draw above the counter.
        private void SetSharedLueurRaised(bool raised)
        {
            if (_lueurContainer == null || _lueurContainer.parent != _root.parent)
            {
                return;
            }
            if (raised)
            {
                int shopIndex = _root.GetSiblingIndex();
                _lueurContainer.SetSiblingIndex(_lueurContainer.GetSiblingIndex() < shopIndex ? shopIndex : shopIndex + 1);
            }
            else
            {
                _lueurContainer.SetSiblingIndex(_lueurOriginalIndex);
            }
        }

        /// <summary>Mirrors the fertilizer panel: puts the "Next round" box at +<paramref name="sidePanelCenterX"/> from the screen center (the fertilizers sit at -that), so both side panels are equally far from the middle.</summary>
        public void SetSidePanelCenterX(float sidePanelCenterX)
        {
            _upcomingEncounterBox.anchorMin = new Vector2(0.5f, 0.5f);
            _upcomingEncounterBox.anchorMax = new Vector2(0.5f, 0.5f);
            _upcomingEncounterBox.anchoredPosition = new Vector2(sidePanelCenterX, 0f);
        }

        /// <summary>The "Next round" enemy preview box (see OverlayLift).</summary>
        public RectTransform UpcomingBox
        {
            get { return _upcomingEncounterBox; }
        }

        public void Show(RunManager run)
        {
            _root.gameObject.SetActive(true);
            SetSharedLueurRaised(true);
            Refresh(run);
            StartCoroutine(PlayCardsEntrance());
            if (_lueurPulseCoroutine == null)
            {
                _lueurPulseCoroutine = StartCoroutine(PulseLueurLabel());
            }
        }

        private const float EntranceStagger = 0.07f;
        private const float EntranceDuration = 0.32f;

        /// <summary>The shop "opens": every card pops in from small with a little overshoot, one after the other, Blister first then the Casino.</summary>
        private System.Collections.IEnumerator PlayCardsEntrance()
        {
            var cards = new List<RectTransform>();
            foreach (var container in new[] { _blisterCardsContainer, _upgradeCardsContainer })
            {
                if (container == null)
                {
                    continue;
                }
                for (int i = 0; i < container.childCount; i++)
                {
                    cards.Add((RectTransform)container.GetChild(i));
                }
            }
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].localScale = Vector3.zero;
            }
            float elapsed = 0f;
            float total = EntranceDuration + EntranceStagger * cards.Count;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] == null)
                    {
                        continue;
                    }
                    float p = Mathf.Clamp01((elapsed - i * EntranceStagger) / EntranceDuration);
                    // 0 -> 1.12 -> 1: quick pop with overshoot.
                    float s = p < 0.65f ? Mathf.Lerp(0f, 1.12f, p / 0.65f) : Mathf.Lerp(1.12f, 1f, (p - 0.65f) / 0.35f);
                    cards[i].localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].localScale = Vector3.one;
                }
            }
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
            SetSharedLueurRaised(false);
            if (_lueurPulseCoroutine != null)
            {
                StopCoroutine(_lueurPulseCoroutine);
                _lueurPulseCoroutine = null;
            }
            _lueurContainer.localScale = Vector3.one;
        }

        /// <summary>Continuous sine-wave scale wobble on the Lueur readout. Runs only while the shop is open, started/stopped by Show/Hide.</summary>
        private System.Collections.IEnumerator PulseLueurLabel()
        {
            while (true)
            {
                float scale = 1f + LueurPulseAmplitude * Mathf.Sin(Time.time * LueurPulseSpeed);
                _lueurContainer.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        /// <summary>Rebuilds every card from the run's current shop state — called on Show and after every purchase/reroll so prices, affordability and "sold" states stay accurate.</summary>
        public void Refresh(RunManager run)
        {
            _lueurLabel.text = run.Lueur.ToString();
            BuildUpcomingEncounterPreview(run);

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
        /// Rebuilds the "Prochain combat" grid from run.UpcomingEncounterIds — one mini icon + full-HP label
        /// per enemy the next round will contain, same sprite lookup as HudView's own enemy band (see
        /// HudView.EnemyIconSprite). One column (reads as a plain vertical stack) while count&lt;=2, two
        /// columns side by side beyond that so a busier round spreads horizontally instead of just shrinking
        /// vertically forever. Shows EnemyCatalog's base MaxHp rather than any live EnemyInstance's HP, since
        /// the round hasn't been built yet (no EnemyInstance exists for it) while the shop is open. Hides the
        /// whole box when the next round has no authored encounter (plain quota round) or doesn't exist
        /// (last round of the run). The box itself stays a fixed size (UpcomingBoxWidth x UpcomingBoxHeight,
        /// matching ModifierPanelView exactly) — rather than clip a round with more enemies than fit at full
        /// size (encounters go up to 6, see EncounterCatalog), each cell scales down together just enough
        /// that the whole grid still fits the box's fixed content height, down to UpcomingMinIconSize.
        /// </summary>
        private void BuildUpcomingEncounterPreview(RunManager run)
        {
            var upcoming = run.UpcomingEncounterIds;
            _upcomingEncounterBox.gameObject.SetActive(upcoming.Count > 0);
            if (upcoming.Count == 0)
            {
                return;
            }

            var nextWeather = run.NextWeather;
            _weatherRow.gameObject.SetActive(nextWeather.HasValue);
            if (nextWeather.HasValue)
            {
                _weatherIcon.sprite = VisualDefaults.GetWeatherSprite(nextWeather.Value);
                _weatherIcon.enabled = _weatherIcon.sprite != null;
                _weatherLabel.text = VisualDefaults.GetWeatherName(nextWeather.Value, run.NextWeatherColor);
                _weatherTooltip.Init(_tooltip, VisualDefaults.GetWeatherName(nextWeather.Value, run.NextWeatherColor), VisualDefaults.GetWeatherDescription(nextWeather.Value, run.NextWeatherColor), "Next round's weather · " + WeatherCatalog.GetRarityLabel(nextWeather.Value), UITheme.TextMuted);
            }

            int count = upcoming.Count;
            int columns = 2;
            int rows = (count + columns - 1) / columns;
            float availableHeight = UpcomingBoxHeight - UpcomingBannerHeight - UpcomingBoxTopPadding - UpcomingBoxBottomPadding - WeatherRowHeight - 6f;
            float maxSlotHeight = (availableHeight - (rows - 1) * UpcomingRowGap) / rows;
            float maxSlotWidth = (UpcomingSlotWidth - (columns - 1) * UpcomingRowGap) / columns;
            float iconSize = Mathf.Max(UpcomingMinIconSize, Mathf.Min(UpcomingIconSize, maxSlotHeight, maxSlotWidth));

            _upcomingEncounterGrid.constraintCount = columns;
            _upcomingEncounterGrid.cellSize = new Vector2(iconSize, iconSize);
            _upcomingEncounterGrid.spacing = new Vector2(UpcomingRowGap, UpcomingRowGap);

            ClearChildren(_upcomingEncounterRow);
            for (int i = 0; i < upcoming.Count; i++)
            {
                var slot = UIFactory.CreateUIObject("UpcomingSlot" + i, _upcomingEncounterRow);

                var def = EnemyCatalog.Get(upcoming[i]);
                var icon = UIFactory.CreatePanel(slot, "Sprite" + i, Color.white);
                icon.preserveAspect = true;
                UIFactory.StretchFull(icon.rectTransform);
                icon.sprite = HudView.EnemyIconSprite(upcoming[i]);

                // Same hover-tooltip component as HudView's own enemy band (see EnemyIconView), just fed
                // the base MaxHp here since no EnemyInstance exists yet for a round that isn't built.
                var iconView = icon.gameObject.AddComponent<EnemyIconView>();
                // Same three sections as HudView's tooltip (name, effect, story line), with the HP as a footer at the bottom-right.
                iconView.Init(_tooltip, def.Name, def.Description, def.Lore, "HP " + EncounterCatalog.ScaleHp(def.MaxHp, EncounterCatalog.GetHpScale(run.CurrentRoundIndex + 1)));
            }
        }

        /// <summary>
        /// Builds all Blister cards (a mix of Modifier-kind and Upgrade-kind
        /// slots) in 2 passes so they share one uniform height despite
        /// differing description lengths: pass 1 builds every card and
        /// measures its description's natural wrapped height; pass 2 applies
        /// the tallest one found to every card so the buy button always
        /// lands at the same Y. Returns that shared card height so the
        /// caller can place the Casino section below it without overlapping.
        /// </summary>
        private float BuildBlisterCards(RunManager run)
        {
            ClearChildren(_blisterCardsContainer);

            var cardRects = new List<RectTransform>(run.ShopBlisterSlots.Count);
            var descRects = new List<RectTransform>(run.ShopBlisterSlots.Count);
            float maxDescHeight = 0f;

            for (int i = 0; i < run.ShopBlisterSlots.Count; i++)
            {
                // A purchased slot no longer gets a card at all — see PlayBlisterRemoval, which plays its
                // shrink/fade-out animation on the old card before this rebuild runs — so the row packs
                // down to just the remaining ones instead of keeping a disabled "Sold" card forever.
                if (run.ShopBlisterSlots[i] != null && run.ShopBlisterSlots[i].Purchased)
                {
                    continue;
                }
                float descHeight = BuildBlisterCard(run, i, out var cardRect, out var descRect);
                cardRects.Add(cardRect);
                descRects.Add(descRect);
                if (descHeight > maxDescHeight)
                {
                    maxDescHeight = descHeight;
                }
            }

            float cardHeight = ModifierCardTopPadding + ModifierNameHeight + ModifierCardGap
                + BlisterIconSize + ModifierCardGap + maxDescHeight + ModifierCardGap
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

        /// <summary>Builds one Blister card's contents (name, bare icon or rarity swatch, description, buy button) and returns its description's natural height. <paramref name="cardRect"/>/<paramref name="descRect"/> are handed back so BuildBlisterCards can resize them once the row's shared height is known; an empty slot returns a null descRect and 0f height.</summary>
        private float BuildBlisterCard(RunManager run, int index, out RectTransform cardRect, out RectTransform descRect)
        {
            var slot = run.ShopBlisterSlots[index];
            var card = UIFactory.CreateSlicedImage(_blisterCardsContainer, "BlisterSlot_" + index, UISprites.CardBackground);
            card.color = UITheme.Panel;
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

            // Name + effect on hover, since the card itself no longer spells the description out.
            var hover = card.gameObject.AddComponent<HoverTooltip>();
            if (slot.Kind == ShopSlotKind.Modifier)
            {
                var modifierDef = ModifierCatalog.Get(slot.ModifierId);
                hover.Init(_tooltip, modifierDef.Name, DescriptionTextFormatter.Colorize(modifierDef.Description, 14), "Fertilizer · " + ModifierVisualDefaults.GetCategoryLabel(modifierDef.Category), UITheme.TextMuted);
            }
            else
            {
                var upgradeDef = slot.HiddenUpgrade;
                hover.Init(_tooltip, upgradeDef.Name, DescriptionTextFormatter.Colorize(upgradeDef.Description, 14),
                    "Upgrade · " + UpgradeVisualDefaults.GetRarityLabel(upgradeDef.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(upgradeDef.Pool).Replace(" Upgrade", ""), UITheme.TextMuted);
            }

            int price = run.GetBlisterSlotPrice(index);
            bool blocked = slot.Kind == ShopSlotKind.Modifier && run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            string blockedLabel = "Full (" + EconomyConstants.MaxActiveModifiers + ")";
            BuildBuyButton(card.transform, slot.Purchased, blocked ? blockedLabel : price.ToString(),
                !slot.Purchased && !blocked && run.PendingUpgrade == null,
                run.Lueur >= price,
                () => OnBlisterBuyClicked(index));

            return descHeight;
        }

        /// <summary>A Blister modifier card's contents: name, bare icon, description. No hover tooltip, since the card already shows name/description as static text. See ModifierCardFactory for the shared visual (also used by UpgradeRevealView's Random Modifier reveal).</summary>
        private float BuildBlisterModifierCardContents(Transform cardTransform, ModifierId modifierId, out RectTransform descRect)
        {
            var def = ModifierCatalog.Get(modifierId);
            return ModifierCardFactory.BuildContents(cardTransform, def, _tooltip, CardWidth, out descRect, badgeSize: BlisterIconSize, withDescription: false);
        }

        /// <summary>
        /// A Blister upgrade card's contents — mirrors ModifierCardFactory.
        /// BuildContents' layout (TopPadding/NameHeight/Gap/BadgeSize) so a
        /// row mixing modifier and upgrade cards shares one uniform height.
        /// Shows the upgrade's real name/description plainly, with a
        /// rarity-colored swatch naming its pool in place of a per-item icon.
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
            var signIcon = UpgradeVisualDefaults.GetIcon(def.Id);
            if (signIcon != null)
            {
                // The upgrade's own garden-sign icon (a tall signpost, unlike a fertilizer's round sack); rarity rides on a small gem in its corner.
                var sign = UIFactory.CreatePanel(cardTransform, "UpgradeSign", Color.white);
                sign.sprite = signIcon;
                sign.preserveAspect = true;
                sign.raycastTarget = false;
                sign.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                sign.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                sign.rectTransform.pivot = new Vector2(0.5f, 1f);
                sign.rectTransform.anchoredPosition = new Vector2(0f, swatchY);
                sign.rectTransform.sizeDelta = new Vector2(BlisterIconSize, BlisterIconSize);
                var gem = UIFactory.CreatePanel(sign.transform, "RarityGem", UpgradeVisualDefaults.GetRarityColor(def.Rarity));
                gem.raycastTarget = false;
                gem.rectTransform.anchorMin = new Vector2(1f, 0f);
                gem.rectTransform.anchorMax = new Vector2(1f, 0f);
                gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                gem.rectTransform.anchoredPosition = new Vector2(-10f, 22f);
                gem.rectTransform.sizeDelta = new Vector2(16f, 16f);
                gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                UIFactory.AddThickOutline(gem, UITheme.Border);
            }
            else
            {
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
            }

            // No description row: it moved into the card's hover tooltip (see BuildBlisterCard).
            descRect = null;
            return 0f;
        }

        private void BuildUpgradeCard(RunManager run, int index)
        {
            var slot = run.ShopUpgradeSlots[index];
            var card = UIFactory.CreateSlicedImage(_upgradeCardsContainer, "UpgSlot_" + index, UISprites.CardBackground);
            card.color = UITheme.Panel;
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
            // upgrade, even once purchased (the reveal happens in the
            // follow-up sub-choice/tile-choice overlay). Random Modifier is
            // named outright instead of showing the generic pool label.
            bool isRandomModifier = slot.HiddenUpgrade != null && slot.HiddenUpgrade.Id == UpgradeId.RandomModifier;
            // GetPoolLabel already ends in "Upgrade", so it isn't appended again here.
            string cardLabel = isRandomModifier ? "Alchemical Boon" : UpgradeVisualDefaults.GetPoolLabel(slot.Pool);
            // The category's name on top, then its mystery illustration (the real item's silhouette kind, hidden behind a "?"):
            // a sign for an upgrade pool, a sack for the Alchemical Boon.
            var poolLabel = UIFactory.CreateText(card.transform, "Pool", cardLabel, 20, UITheme.TextPrimary);
            poolLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            poolLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 30f);

            var mysteryArt = Resources.Load<Sprite>("Icons/Mystery/" + (isRandomModifier ? "Boon" : slot.Pool.ToString()));
            if (mysteryArt != null)
            {
                var art = UIFactory.CreatePanel(card.transform, "MysteryArt", Color.white);
                art.sprite = mysteryArt;
                art.preserveAspect = true;
                art.raycastTarget = false;
                art.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                art.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                art.rectTransform.pivot = new Vector2(0.5f, 1f);
                art.rectTransform.anchoredPosition = new Vector2(0f, -44f);
                art.rectTransform.sizeDelta = new Vector2(98f, 98f);
            }
            else
            {
                var mysteryHint = UIFactory.CreateText(card.transform, "Hint", "?", 40, UITheme.TextMuted);
                mysteryHint.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                mysteryHint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                mysteryHint.rectTransform.pivot = new Vector2(0.5f, 1f);
                mysteryHint.rectTransform.anchoredPosition = new Vector2(0f, -70f);
                mysteryHint.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 40f);
            }

            int price = run.GetUpgradeSlotPrice(index);
            // Random Modifier is the one upgrade that respects the modifier
            // cap; every other upgrade ignores it (see RunManager.BuyUpgradeSlot).
            bool atModifierCap = isRandomModifier && run.ActiveModifiers.Count >= EconomyConstants.MaxActiveModifiers;
            // Modifier Upgrade needs an already-owned modifier to level up.
            bool isModifierUpgrade = slot.HiddenUpgrade != null && slot.HiddenUpgrade.Id == UpgradeId.ModifierUpgrade;
            bool hasNoModifiersToUpgrade = isModifierUpgrade && run.ActiveModifiers.Count == 0;
            bool blocked = atModifierCap || hasNoModifiersToUpgrade;
            string blockedLabel = atModifierCap ? "Full (" + EconomyConstants.MaxActiveModifiers + ")" : "None owned";
            BuildBuyButton(card.transform, slot.Purchased, blocked ? blockedLabel : price.ToString(),
                !slot.Purchased && !blocked && run.PendingUpgrade == null,
                run.Lueur >= price,
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

        /// <summary>
        /// Shows the gold-diamond Lueur icon + price, matching every other place this currency appears;
        /// "Sold" is plain text once purchased. <paramref name="canInteract"/> covers every reason the slot
        /// itself can't be bought at all (already sold, modifier cap full, no modifiers to upgrade, a
        /// pending upgrade choice blocking every purchase) — the button stays enabled whenever that's true
        /// even if <paramref name="affordable"/> (enough Lueur) is false, so clicking it still does
        /// something: it shakes the card instead of silently doing nothing, which a plain disabled button
        /// never could.
        /// </summary>
        private void BuildBuyButton(Transform parent, bool purchased, string priceLabel, bool canInteract, bool affordable, Action onClick)
        {
            var buyBtn = UIFactory.CreateButton(parent, "Buy", purchased ? "Sold" : "", UISprites.ChooseButtonBackground, 14);
            UIFactory.DisablePlants(buyBtn);
            var buyRect = buyBtn.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, BuyButtonBottomMargin);
            // A text label ("None owned", "Full (n)") needs more room than a price number.
            bool isTextLabel = !purchased && !int.TryParse(priceLabel, out _);
            buyRect.sizeDelta = new Vector2(isTextLabel ? CardWidth - 24f : BuyButtonWidth, BuyButtonHeight);
            buyBtn.interactable = !purchased && canInteract;
            var cardRect = (RectTransform)parent;
            buyBtn.onClick.AddListener(() =>
            {
                if (!affordable)
                {
                    // Not enough Pollen: the button itself flashes red and shakes, on top of the card shake.
                    var feedback = buyBtn.GetComponent<ButtonFeedback>();
                    if (feedback != null)
                    {
                        feedback.Deny();
                    }
                    StartCoroutine(ShakeCard(cardRect));
                    return;
                }
                onClick();
            });

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
            float priceIconSize = 16f;
            if (VisualDefaults.PollenSprite != null)
            {
                // The Pollen icon (same as the counter), not the old gold diamond.
                priceIcon.sprite = VisualDefaults.PollenSprite;
                priceIcon.color = Color.white;
                priceIcon.preserveAspect = true;
                priceIconSize = 26f;
            }
            else
            {
                priceIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            var priceIconLayout = priceIcon.gameObject.AddComponent<LayoutElement>();
            priceIconLayout.preferredWidth = priceIconSize;
            priceIconLayout.preferredHeight = priceIconSize;

            // Red instead of the default tint when the slot is otherwise buyable but the player just can't
            // afford it yet — the button stays clickable (see the onClick lambda above), so this is the
            // visual cue that used to come from Unity's own disabled-button graying.
            var priceText = UIFactory.CreateText(priceContainer, "PriceLabel", priceLabel, 21, affordable ? UITheme.TextPrimary : UITheme.Danger);
            priceText.raycastTarget = false;
            if (!affordable)
            {
                // The Pollen icon keeps its own colors; only the diamond fallback goes red.
                if (VisualDefaults.PollenSprite == null)
                {
                    priceIcon.color = UITheme.Danger;
                }
            }
        }

        /// <summary>Short, decaying side-to-side shake on a card clicked without enough Lueur to afford it — same technique as HandView.ShakeSlot.</summary>
        private System.Collections.IEnumerator ShakeCard(RectTransform rect)
        {
            Vector2 originalPos = rect.anchoredPosition;
            float t = 0f;
            while (t < CantAffordShakeDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / CantAffordShakeDuration);
                float damping = 1f - progress;
                float offsetX = Mathf.Sin(progress * CantAffordShakeCycles * Mathf.PI * 2f) * CantAffordShakeMagnitude * damping;
                rect.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
                yield return null;
            }
            rect.anchoredPosition = originalPos;
        }

        /// <summary>Quick overshoot scale pop on the Lueur readout, played on a successful purchase — pauses the ambient PulseLueurLabel wobble for its duration (both drive the same localScale) and resumes it afterward.</summary>
        public void PlayPurchasePop()
        {
            StartCoroutine(PurchasePopRoutine());
        }

        private System.Collections.IEnumerator PurchasePopRoutine()
        {
            bool wasPulsing = _lueurPulseCoroutine != null;
            if (wasPulsing)
            {
                StopCoroutine(_lueurPulseCoroutine);
                _lueurPulseCoroutine = null;
            }
            float t = 0f;
            while (t < PurchasePopDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PurchasePopDuration);
                float scale = p < PurchasePopPeakFraction
                    ? Mathf.Lerp(1f, PurchasePopPeakScale, p / PurchasePopPeakFraction)
                    : Mathf.Lerp(PurchasePopPeakScale, 1f, (p - PurchasePopPeakFraction) / (1f - PurchasePopPeakFraction));
                _lueurContainer.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _lueurContainer.localScale = Vector3.one;
            if (wasPulsing)
            {
                _lueurPulseCoroutine = StartCoroutine(PulseLueurLabel());
            }
        }

        /// <summary>
        /// Shrinks and fades out a just-bought Blister card in place, then calls <paramref name="onComplete"/>
        /// (the caller's own post-purchase Refresh/follow-up dispatch) once it's done — so the player sees the
        /// bought card actually leave instead of just flipping to a disabled "Sold" state, and the row only
        /// recenters (via BuildBlisterCards skipping Purchased slots) once that departure has played out.
        /// Falls back to calling onComplete immediately if the card can't be found (e.g. already gone).
        /// </summary>
        public void PlayBlisterRemoval(int index, Action onComplete)
        {
            var cardTransform = _blisterCardsContainer.Find("BlisterSlot_" + index) as RectTransform;
            if (cardTransform == null)
            {
                onComplete();
                return;
            }
            StartCoroutine(BlisterRemoveRoutine(cardTransform, onComplete));
        }

        private System.Collections.IEnumerator BlisterRemoveRoutine(RectTransform cardRect, Action onComplete)
        {
            var canvasGroup = cardRect.gameObject.AddComponent<CanvasGroup>();
            Vector3 startScale = cardRect.localScale;
            float t = 0f;
            while (t < BlisterRemoveDuration)
            {
                if (cardRect == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / BlisterRemoveDuration);
                float scale = Mathf.Lerp(1f, 0f, p);
                cardRect.localScale = startScale * scale;
                canvasGroup.alpha = 1f - p;
                yield return null;
            }
            onComplete();
        }

        /// <summary>Quick vertical flip-reveal on the Casino row, played after a reroll's new cards are already built (Refresh() runs synchronously right after RerollShop(), so there's no window to animate the old ones away) — scales from flat to a slight overshoot to settled, reading as the new cards flipping into place.</summary>
        public void PlayRerollFlip()
        {
            StartCoroutine(RerollFlipRoutine());
        }

        private System.Collections.IEnumerator RerollFlipRoutine()
        {
            var rt = _upgradeCardsContainer;
            float t = 0f;
            while (t < RerollFlipDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / RerollFlipDuration);
                float scaleY = p < RerollFlipOvershootFraction
                    ? Mathf.Lerp(0f, RerollFlipOvershootScale, p / RerollFlipOvershootFraction)
                    : Mathf.Lerp(RerollFlipOvershootScale, 1f, (p - RerollFlipOvershootFraction) / (1f - RerollFlipOvershootFraction));
                rt.localScale = new Vector3(1f, scaleY, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
