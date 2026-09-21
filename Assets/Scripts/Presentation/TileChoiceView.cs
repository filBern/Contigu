using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals a Grid-pool
    /// (piece-trait) upgrade: the player picks which of a handful of
    /// candidate deck tokens actually receive it (spec: "un choix de 5
    /// tiles"), instead of the old random assignment. Toggle any candidate
    /// preview on/off; Confirm enables once exactly
    /// EconomyConstants.ShopTileChoiceCount are selected (or fewer, if the
    /// deck didn't even have that many candidates to offer). Shows the
    /// upgrade's own card (see UpgradeCardFactory) above the previews, on
    /// explicit request, so a mystery shop slot's reveal is actually
    /// readable and not just a name.
    /// </summary>
    public sealed class TileChoiceView : MonoBehaviour
    {
        private const float CellSize = 140f;
        private const float PreviewSize = 116f;
        private const float BadgeFadeDuration = 0.35f;

        /// <summary>Fires with the chosen deck indices once the player confirms.</summary>
        public event Action<IReadOnlyList<int>> TileChoiceConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardContainer;
        private Text _title;
        private RectTransform _previewsContainer;
        private Button _confirmButton;

        private readonly List<int> _candidates = new List<int>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly Dictionary<int, Image> _cellBackgroundByIndex = new Dictionary<int, Image>();
        private readonly Dictionary<int, RectTransform> _previewContainerByIndex = new Dictionary<int, RectTransform>();
        private int _requiredCount;

        // Which trait a selected preview should show taking shape on it —
        // set once per Show() call from the upgrade being resolved, so
        // OnCellClicked doesn't need to know anything about upgrades itself.
        private PieceTraitKind? _previewTraitKind;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "TileChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);
            _cardContainer.anchoredPosition = new Vector2(0f, -20f);

            _title = UIFactory.CreateText(_root, "Title", "", 22, UITheme.TextPrimary);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), once the card above it (see
            // UpgradeCardFactory) is built and its real height known — see
            // the comment there.
            _title.rectTransform.sizeDelta = new Vector2(700f, 40f);

            // Horizontal instead of the old vertical list (explicit request:
            // "avoir seulement le preview... et mettre les 5 un a côté de
            // l'autre") — 5 previews side by side read faster than 5 stacked
            // rows, and take a lot less vertical space besides.
            _previewsContainer = UIFactory.CreateUIObject("Previews", _root);
            _previewsContainer.anchorMin = new Vector2(0.5f, 1f);
            _previewsContainer.anchorMax = new Vector2(0.5f, 1f);
            _previewsContainer.pivot = new Vector2(0.5f, 1f);
            var layout = _previewsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _previewsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _confirmButton = UIFactory.CreateButton(_root, "Confirm", "Confirm", UISprites.ChooseButtonBackground, 18);
            var confirmRect = _confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(0f, 40f);
            confirmRect.sizeDelta = new Vector2(200f, 46f);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        public void Show(DeckManager deck, IReadOnlyList<int> candidateDeckIndices, int requiredCount, UpgradeDefinition def)
        {
            _deck = deck;
            _candidates.Clear();
            _candidates.AddRange(candidateDeckIndices);
            _selected.Clear();
            _cellBackgroundByIndex.Clear();
            _previewContainerByIndex.Clear();
            _requiredCount = Mathf.Min(requiredCount, _candidates.Count);
            _previewTraitKind = UpgradeSystem.TraitKindFor(def.Id);

            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }
            var card = UpgradeCardFactory.Build(_cardContainer, def);

            // Measured, not guessed — see UpgradeCardFactory's own comment.
            // _cardContainer itself sits at a fixed -20; the card built
            // inside it starts at (0,0) relative to that, so its own
            // sizeDelta.y is exactly how far down the card actually goes.
            const float cardTopY = -20f;
            const float gapBelowCard = 24f;
            float bodyTopY = cardTopY - card.sizeDelta.y - gapBelowCard;
            _title.rectTransform.anchoredPosition = new Vector2(0f, bodyTopY);
            _previewsContainer.anchoredPosition = new Vector2(0f, bodyTopY - 40f);

            _title.text = "Select " + _requiredCount + " pieces";

            for (int i = _previewsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewsContainer.GetChild(i).gameObject);
            }
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildPreviewCell(_candidates[i]);
            }

            RefreshConfirmInteractable();
            _root.gameObject.SetActive(true);
        }

        private void BuildPreviewCell(int deckIndex)
        {
            var cell = UIFactory.CreatePanel(_previewsContainer, "Tile_" + deckIndex, UITheme.ButtonIdle);
            cell.rectTransform.sizeDelta = new Vector2(CellSize, CellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = CellSize;
            cellLayout.preferredHeight = CellSize;
            _cellBackgroundByIndex[deckIndex] = cell;

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnCellClicked(deckIndex));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = Vector2.zero;
            previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);
            _previewContainerByIndex[deckIndex] = previewContainer;

            RebuildPreview(deckIndex, showTrait: false, animate: false);
        }

        /// <summary>
        /// (Re)draws the piece preview for <paramref name="deckIndex"/> —
        /// plain when not selected, or with a preview of the actual trait
        /// this upgrade grants when <paramref name="showTrait"/> is true, so
        /// the player can see exactly what selecting this piece does rather
        /// than just a generic highlight (explicit request: "faire
        /// apparaître progressivement le visuel de la tuile upgradée pour
        /// que le joueur comprenne quelle tuile exactement est affectée").
        /// The trait shown is a preview only — DeckManager.TagSpecificTokens
        /// still picks the real cell/color once the choice is confirmed.
        /// </summary>
        private void RebuildPreview(int deckIndex, bool showTrait, bool animate)
        {
            var token = _deck.Deck[deckIndex];
            var previewContainer = _previewContainerByIndex[deckIndex];
            for (int i = previewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(previewContainer.GetChild(i).gameObject);
            }

            PieceTrait? previewTrait = null;
            if (showTrait && _previewTraitKind.HasValue)
            {
                // Tinted always tints to the token's own color (see
                // UpgradeSystem.ApplyToChosenTiles/DeckManager.TagSpecificTokens)
                // — cell index 0 is just any real cell of the shape, since the
                // exact cell is likewise only decided for real on confirm.
                PieceColor? tintedColor = _previewTraitKind.Value == PieceTraitKind.Tinted ? (PieceColor?)token.Color : null;
                previewTrait = new PieceTrait(_previewTraitKind.Value, 0, tintedColor);
            }

            var badge = ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(token.Shape), token.Color, previewTrait, _tooltip, _cellBackgroundByIndex[deckIndex].gameObject);
            if (animate && badge != null)
            {
                StartCoroutine(FadeInBadge(badge));
            }
        }

        private static IEnumerator FadeInBadge(RectTransform badge)
        {
            var canvasGroup = badge.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < BadgeFadeDuration)
            {
                // Selecting a different piece before this one finishes fading
                // in rebuilds (and destroys) this exact badge — bail out
                // rather than touch a destroyed component.
                if (badge == null)
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / BadgeFadeDuration);
                yield return null;
            }
            if (badge != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void OnCellClicked(int deckIndex)
        {
            bool wasSelected = _selected.Contains(deckIndex);
            if (wasSelected)
            {
                _selected.Remove(deckIndex);
            }
            else if (_selected.Count < _requiredCount)
            {
                _selected.Add(deckIndex);
            }

            bool nowSelected = _selected.Contains(deckIndex);
            _cellBackgroundByIndex[deckIndex].color = nowSelected ? UITheme.ButtonSelected : UITheme.ButtonIdle;
            if (nowSelected != wasSelected)
            {
                RebuildPreview(deckIndex, showTrait: nowSelected, animate: nowSelected);
            }
            RefreshConfirmInteractable();
        }

        private void RefreshConfirmInteractable()
        {
            _confirmButton.interactable = _selected.Count == _requiredCount;
        }

        private void OnConfirmClicked()
        {
            _root.gameObject.SetActive(false);
            if (TileChoiceConfirmed != null)
            {
                TileChoiceConfirmed(new List<int>(_selected));
            }
        }
    }
}
