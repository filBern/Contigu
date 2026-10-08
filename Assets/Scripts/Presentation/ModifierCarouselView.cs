using System;
using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Slot-machine-style overlay for revealing a modifier the player didn't
    /// pick themselves (the shop's "Random Modifier" upgrade grant). A
    /// horizontal reel of random modifier badges spins under a fixed
    /// highlight frame and decelerates onto the real pick, then the same
    /// modifier-card presentation (via ModifierCardFactory) reveals its
    /// name/icon/description below, blocking until dismissed.
    /// </summary>
    public sealed class ModifierCarouselView : MonoBehaviour
    {
        private const float ReelWidth = 340f;
        private const float ReelHeight = 110f;
        private const float BadgeSize = 80f;
        private const float BadgeSpacing = 110f;
        // Total badges in the strip — every one but WinningIndex is just
        // eye candy the reel blows past on its way there. WinningIndex sits
        // a few slots before the end so a handful of filler badges are
        // still visible sliding past after the reel stops.
        private const int ReelLength = 22;
        private const int WinningIndex = ReelLength - 6;
        private const float SpinDuration = 2.4f;

        private const float TitleHeight = 40f;
        private const float ModifierCardWidth = 260f;
        private const float ModifierCardBadgeSize = 110f;
        private const float ModifierCardNameHeight = 30f;
        private const int ModifierCardNameFontSize = 18;
        private const int ModifierCardDescFontSize = 14;
        private const float OkHeight = 44f;
        private const float BlockSpacing = 20f;
        // The canvas is always exactly this tall in its own local units,
        // same as UpgradeRevealView.CanvasHeight — see GameBootstrap.BuildCanvas.
        private const float CanvasHeight = 800f;

        /// <summary>Fires once the player dismisses the reveal.</summary>
        public event Action Dismissed;

        private TooltipView _tooltip;
        private RectTransform _root;
        private Text _titleText;
        private RectTransform _titleRect;
        private RectTransform _viewportRect;
        private RectTransform _reelRect;
        private RectTransform _cardContainer;
        private RectTransform _okRect;
        private Button _okButton;
        private Coroutine _spinCoroutine;
        // Y position (below the fixed viewport) that the card container
        // starts at every time — set once per Show() by LayoutFixedPart,
        // then read (never written) by UpdateCard for the rest of the spin.
        private float _fixedCardTopY;
        private readonly ModifierId[] _reelModifierIds = new ModifierId[ReelLength];

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "ModifierCarouselOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "Starting modifier", 22, UITheme.TextOnBackground);
            _titleText = title;
            _titleRect = title.rectTransform;
            _titleRect.anchorMin = new Vector2(0.5f, 1f);
            _titleRect.anchorMax = new Vector2(0.5f, 1f);
            _titleRect.pivot = new Vector2(0.5f, 1f);
            _titleRect.sizeDelta = new Vector2(600f, TitleHeight);

            var viewport = UIFactory.CreatePanel(_root, "Viewport", UITheme.Panel);
            UIFactory.AddThickOutline(viewport, UITheme.Border);
            _viewportRect = viewport.rectTransform;
            _viewportRect.anchorMin = new Vector2(0.5f, 1f);
            _viewportRect.anchorMax = new Vector2(0.5f, 1f);
            _viewportRect.pivot = new Vector2(0.5f, 1f);
            _viewportRect.sizeDelta = new Vector2(ReelWidth, ReelHeight);
            // Clips the reel's off-screen badges to the viewport's bounds —
            // without this every badge past the first stays fully visible,
            // spilling out past both sides of the frame while it spins.
            viewport.gameObject.AddComponent<RectMask2D>();

            _reelRect = UIFactory.CreateUIObject("Reel", viewport.transform);
            _reelRect.anchorMin = new Vector2(0f, 0.5f);
            _reelRect.anchorMax = new Vector2(0f, 0.5f);
            _reelRect.pivot = new Vector2(0f, 0.5f);

            // Fixed frame in the exact center of the viewport, added after
            // the reel so it draws on top of whichever badge is passing
            // underneath. Built from 4 plain solid bars rather than an
            // Outline component on a Color.clear Image: Unity's Shadow/Outline
            // effect multiplies its effectColor's alpha by the base Graphic's
            // alpha, so on a fully transparent base it renders nothing.
            BuildHighlightFrame(viewport.transform);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);

            var okButton = UIFactory.CreateButton(_root, "Ok", "OK", UISprites.ChooseButtonBackground, 18);
            _okButton = okButton;
            _okRect = okButton.GetComponent<RectTransform>();
            _okRect.anchorMin = new Vector2(0.5f, 1f);
            _okRect.anchorMax = new Vector2(0.5f, 1f);
            _okRect.pivot = new Vector2(0.5f, 1f);
            _okRect.sizeDelta = new Vector2(160f, OkHeight);
            okButton.onClick.AddListener(OnOkClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>4 plain solid bars forming a hollow square border around the center of <paramref name="parent"/> (the viewport) — see the comment at its call site in Build for why this replaces an Outline-on-Color.clear approach.</summary>
        private static void BuildHighlightFrame(Transform parent)
        {
            const float FrameSize = BadgeSize + 14f;
            const float BarThickness = 4f;

            var top = UIFactory.CreatePanel(parent, "HighlightTop", VisualDefaults.GoldenColor);
            top.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            top.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            top.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            top.rectTransform.sizeDelta = new Vector2(FrameSize, BarThickness);
            top.rectTransform.anchoredPosition = new Vector2(0f, FrameSize / 2f);

            var bottom = UIFactory.CreatePanel(parent, "HighlightBottom", VisualDefaults.GoldenColor);
            bottom.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.sizeDelta = new Vector2(FrameSize, BarThickness);
            bottom.rectTransform.anchoredPosition = new Vector2(0f, -FrameSize / 2f);

            var left = UIFactory.CreatePanel(parent, "HighlightLeft", VisualDefaults.GoldenColor);
            left.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            left.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            left.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            left.rectTransform.sizeDelta = new Vector2(BarThickness, FrameSize);
            left.rectTransform.anchoredPosition = new Vector2(-FrameSize / 2f, 0f);

            var right = UIFactory.CreatePanel(parent, "HighlightRight", VisualDefaults.GoldenColor);
            right.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            right.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            right.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(BarThickness, FrameSize);
            right.rectTransform.anchoredPosition = new Vector2(FrameSize / 2f, 0f);
        }

        /// <summary>
        /// <paramref name="granted"/> is already picked and applied by the
        /// time this shows; the spin is purely presentational suspense.
        /// <paramref name="title"/> is shown above the reel.
        /// </summary>
        public void Show(ModifierId granted, string title)
        {
            _titleText.text = title;

            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }

            for (int i = _reelRect.childCount - 1; i >= 0; i--)
            {
                Destroy(_reelRect.GetChild(i).gameObject);
            }
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }

            // The card stays visible for the whole spin, kept in sync with
            // whichever badge is currently centered under the highlight
            // (see SpinRoutine). Only the OK button waits for the spin to
            // actually land, so the player can't dismiss mid-spin.
            _cardContainer.gameObject.SetActive(true);
            _okButton.gameObject.SetActive(false);
            _reelRect.anchoredPosition = Vector2.zero;

            // Purely decorative filler badges — a fresh System.Random here
            // (not the run's own seeded IRandomProvider) is fine since none
            // of this affects anything Core actually resolves.
            var filler = new System.Random();
            for (int i = 0; i < ReelLength; i++)
            {
                var def = i == WinningIndex ? ModifierCatalog.Get(granted) : ModifierCatalog.All[filler.Next(ModifierCatalog.All.Length)];
                _reelModifierIds[i] = def.Id;
                BuildReelBadge(def, i);
            }

            // Title/viewport position computed once here, using the winning
            // modifier's own card height as the centering reference, and
            // never touched again for the rest of the spin. Only the card
            // container (and the OK button under it) still move, in
            // UpdateCard, to fit each badge's own description length.
            LayoutFixedPart(MeasureCardHeight(granted));

            _root.gameObject.SetActive(true);
            _spinCoroutine = StartCoroutine(SpinRoutine(granted));
        }

        /// <summary>Builds <paramref name="id"/>'s card off-screen just long enough to read its true height, then tears it down — used once per Show() to compute LayoutFixedPart's reference height without leaving anything extra in the hierarchy.</summary>
        private float MeasureCardHeight(ModifierId id)
        {
            var probe = UIFactory.CreateSlicedImage(_cardContainer, "HeightProbe", UISprites.CardBackground);
            float descHeight = ModifierCardFactory.BuildContents(probe.transform, ModifierCatalog.Get(id), _tooltip, ModifierCardWidth, out _,
                ModifierCardBadgeSize, ModifierCardNameHeight, ModifierCardNameFontSize, ModifierCardDescFontSize);
            float height = ModifierCardFactory.TotalHeight(descHeight, ModifierCardBadgeSize, ModifierCardNameHeight);
            Destroy(probe.gameObject);
            return height;
        }

        private void BuildReelBadge(ModifierDefinition def, int index)
        {
            var badge = ModifierBadgeFactory.Create(_reelRect, def, BadgeSize, _tooltip, attachTooltip: false);
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchoredPosition = new Vector2(index * BadgeSpacing, 0f);
        }

        private IEnumerator SpinRoutine(ModifierId granted)
        {
            // Slides the reel left until the WinningIndex badge sits centered
            // under the fixed highlight frame, using an ease-out cubic so
            // the spin visibly decelerates before landing. The reel is
            // anchored to the viewport's left edge (see _reelRect's anchor
            // in Build), so centering a badge under the highlight needs
            // ReelWidth/2 added as an offset.
            float endX = ReelWidth / 2f - WinningIndex * BadgeSpacing;
            float t = 0f;
            int lastShownIndex = -1;
            while (t < SpinDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SpinDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                float x = Mathf.Lerp(0f, endX, eased);
                _reelRect.anchoredPosition = new Vector2(x, 0f);

                // Whichever badge is currently centered under the highlight,
                // solved the other way round from endX above (position ->
                // index). Only rebuilds the card on an actual change.
                int centeredIndex = Mathf.Clamp(Mathf.RoundToInt((ReelWidth / 2f - x) / BadgeSpacing), 0, ReelLength - 1);
                if (centeredIndex != lastShownIndex)
                {
                    lastShownIndex = centeredIndex;
                    UpdateCard(_reelModifierIds[centeredIndex]);
                    SfxManager.Play(SfxId.CarouselTick);
                }
                yield return null;
            }
            _reelRect.anchoredPosition = new Vector2(endX, 0f);

            // Defensive re-sync, guarding against float drift near the tail
            // end of the spin.
            UpdateCard(granted);
            _okButton.gameObject.SetActive(true);
            _spinCoroutine = null;
        }

        /// <summary>(Re)builds the reveal card's contents for <paramref name="id"/> and re-anchors it (and the OK button below it) from the fixed <see cref="_fixedCardTopY"/> set once in LayoutFixedPart. Called throughout the spin as the centered badge changes. The title and viewport above are untouched here.</summary>
        private void UpdateCard(ModifierId id)
        {
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }

            var def = ModifierCatalog.Get(id);
            var cardImage = UIFactory.CreateSlicedImage(_cardContainer, "GrantedModifierCard", UISprites.CardBackground);
            cardImage.color = UITheme.Panel;
            UIFactory.AddThickOutline(cardImage, UITheme.Border);
            cardImage.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            cardImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cardImage.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardImage.rectTransform.anchoredPosition = Vector2.zero;

            float descHeight = ModifierCardFactory.BuildContents(cardImage.transform, def, _tooltip, ModifierCardWidth, out var descRect,
                ModifierCardBadgeSize, ModifierCardNameHeight, ModifierCardNameFontSize, ModifierCardDescFontSize);
            descRect.sizeDelta = new Vector2(descRect.sizeDelta.x, descHeight);

            float cardHeight = ModifierCardFactory.TotalHeight(descHeight, ModifierCardBadgeSize, ModifierCardNameHeight);
            cardImage.rectTransform.sizeDelta = new Vector2(ModifierCardWidth, cardHeight);
            _cardContainer.sizeDelta = new Vector2(ModifierCardWidth, cardHeight);

            // Only the card (and the OK button riding just below it) move —
            // the title and viewport were already placed once, in
            // LayoutFixedPart, and stay put for the whole spin.
            _cardContainer.anchoredPosition = new Vector2(0f, _fixedCardTopY);
            _okRect.anchoredPosition = new Vector2(0f, _fixedCardTopY - cardHeight - BlockSpacing);
        }

        /// <summary>Positions the title and viewport once per Show(), against a total-height estimate built from the winning modifier's own card height, so the reel and its highlight frame stay steady for the whole spin. Also records <see cref="_fixedCardTopY"/>, the Y the card container (and the OK button) is placed at on every subsequent UpdateCard call.</summary>
        private void LayoutFixedPart(float referenceCardHeight)
        {
            float totalHeight = TitleHeight + BlockSpacing + ReelHeight + BlockSpacing + referenceCardHeight + BlockSpacing + OkHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _titleRect.anchoredPosition = new Vector2(0f, topY);
            float y = topY - TitleHeight - BlockSpacing;

            _viewportRect.anchoredPosition = new Vector2(0f, y);
            y -= ReelHeight + BlockSpacing;

            _fixedCardTopY = y;
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
