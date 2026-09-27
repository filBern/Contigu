using System;
using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Slot-machine-style overlay shown once, right at the start of a run,
    /// for the free modifier RunManager.GrantStartingModifier just granted
    /// (spec extension, explicit request: "au départ d'une run, il y ait un
    /// carousel qui choisissent un modifier au hasard, comme pour dicter une
    /// stratégie initiale que le joueur devra utiliser"). A horizontal reel
    /// of random modifier badges (see ModifierBadgeFactory) spins under a
    /// fixed highlight frame and decelerates onto the real pick, then the
    /// same modifier-card presentation UpgradeRevealView.ShowModifierGrant
    /// uses (via ModifierCardFactory) reveals its name/icon/description
    /// below, blocking until dismissed.
    /// </summary>
    public sealed class ModifierCarouselView : MonoBehaviour
    {
        private const float ReelWidth = 340f;
        private const float ReelHeight = 110f;
        private const float BadgeSize = 80f;
        private const float BadgeSpacing = 110f;
        // Total badges in the strip — only the LAST one is the real pick;
        // every other one is just eye candy the reel blows past on its way
        // there.
        private const int ReelLength = 22;
        private const float SpinDuration = 2.4f;

        private const float TitleHeight = 40f;
        private const float ModifierCardWidth = 260f;
        private const float ModifierCardBadgeSize = 110f;
        private const float ModifierCardNameHeight = 30f;
        private const int ModifierCardNameFontSize = 18;
        private const int ModifierCardDescFontSize = 14;
        private const float OkHeight = 44f;
        private const float BlockSpacing = 20f;
        // Same "the canvas is always exactly this tall in its own local
        // units" reasoning as UpgradeRevealView.CanvasHeight — see
        // GameBootstrap.BuildCanvas.
        private const float CanvasHeight = 800f;

        /// <summary>Fires once the player dismisses the reveal.</summary>
        public event Action Dismissed;

        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _titleRect;
        private RectTransform _viewportRect;
        private RectTransform _reelRect;
        private RectTransform _cardContainer;
        private RectTransform _okRect;
        private Button _okButton;
        private Coroutine _spinCoroutine;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "ModifierCarouselOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "Modificateur de depart", 22, UITheme.TextPrimary);
            _titleRect = title.rectTransform;
            _titleRect.anchorMin = new Vector2(0.5f, 1f);
            _titleRect.anchorMax = new Vector2(0.5f, 1f);
            _titleRect.pivot = new Vector2(0.5f, 1f);
            _titleRect.sizeDelta = new Vector2(600f, TitleHeight);

            var viewport = UIFactory.CreatePanel(_root, "Viewport", UITheme.Panel);
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

            // Fixed frame in the exact center of the viewport, added AFTER
            // the reel so it draws on top of whichever badge is passing
            // underneath — whichever one is centered here once the spin
            // stops is the modifier that actually got granted.
            var highlight = UIFactory.CreatePanel(viewport.transform, "Highlight", Color.clear);
            highlight.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.sizeDelta = new Vector2(BadgeSize + 14f, BadgeSize + 14f);
            var highlightOutline = highlight.gameObject.AddComponent<Outline>();
            highlightOutline.effectColor = VisualDefaults.GoldenColor;
            highlightOutline.effectDistance = new Vector2(2f, -2f);

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

        /// <summary><paramref name="granted"/> is RunManager.StartingModifier — already picked and applied by the time this shows; the spin is purely presentational suspense, same as UpgradeRevealView's reveals never gamble with anything Core hasn't already resolved.</summary>
        public void Show(ModifierId granted)
        {
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

            _cardContainer.gameObject.SetActive(false);
            _okButton.gameObject.SetActive(false);
            _reelRect.anchoredPosition = Vector2.zero;

            // Purely decorative filler badges — a fresh System.Random here
            // (not the run's own seeded IRandomProvider) is fine since none
            // of this affects anything Core actually resolves.
            var filler = new System.Random();
            for (int i = 0; i < ReelLength - 1; i++)
            {
                BuildReelBadge(ModifierCatalog.All[filler.Next(ModifierCatalog.All.Length)], i);
            }
            BuildReelBadge(ModifierCatalog.Get(granted), ReelLength - 1);

            LayoutTitleAndViewport();

            _root.gameObject.SetActive(true);
            _spinCoroutine = StartCoroutine(SpinRoutine(granted));
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
            // Slides the reel left until the very last badge (the real
            // pick) sits centered under the fixed highlight frame — an
            // ease-out cubic (fast start, slow finish) rather than the
            // linear-then-snap every other "juice" coroutine in this game
            // uses, since a spin reads as fake if it doesn't visibly
            // decelerate before landing.
            float endX = -(ReelLength - 1) * BadgeSpacing;
            float t = 0f;
            while (t < SpinDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SpinDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                _reelRect.anchoredPosition = new Vector2(Mathf.Lerp(0f, endX, eased), 0f);
                yield return null;
            }
            _reelRect.anchoredPosition = new Vector2(endX, 0f);

            RevealCard(granted);
            _spinCoroutine = null;
        }

        private void RevealCard(ModifierId granted)
        {
            var def = ModifierCatalog.Get(granted);
            var cardImage = UIFactory.CreateSlicedImage(_cardContainer, "GrantedModifierCard", UISprites.CardBackground);
            cardImage.color = UITheme.Panel;
            var outline = cardImage.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
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

            LayoutCardAndOk(cardHeight);

            _cardContainer.gameObject.SetActive(true);
            _okButton.gameObject.SetActive(true);
        }

        private void LayoutTitleAndViewport()
        {
            float totalHeight = TitleHeight + BlockSpacing + ReelHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);
            _titleRect.anchoredPosition = new Vector2(0f, topY);
            _viewportRect.anchoredPosition = new Vector2(0f, topY - TitleHeight - BlockSpacing);
        }

        private void LayoutCardAndOk(float cardHeight)
        {
            float y = _viewportRect.anchoredPosition.y - ReelHeight - BlockSpacing;
            _cardContainer.anchoredPosition = new Vector2(0f, y);
            y -= cardHeight + BlockSpacing;
            _okRect.anchoredPosition = new Vector2(0f, y);
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
