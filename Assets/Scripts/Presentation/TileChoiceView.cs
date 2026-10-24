using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals a Grid-pool (piece-trait) upgrade: the player
    /// picks which candidate deck tokens receive it. Confirm enables once exactly
    /// EconomyConstants.ShopTileChoiceCount are selected (or fewer, if the deck has fewer candidates).
    /// </summary>
    public sealed class TileChoiceView : MonoBehaviour
    {
        private const float SignSize = 120f;
        private const float BadgeFadeDuration = 0.35f;
        private const float TitleHeight = 40f;
        private const float ConfirmHeight = 46f;
        private const float BlockSpacing = 24f;
        // The canvas is always exactly this tall in its own local units
        // regardless of actual window size (CanvasScaler matches on height —
        // see GameBootstrap.BuildCanvas), so centering math done in this
        // space holds for any resolution.
        private const float CanvasHeight = 800f;

        /// <summary>Fires with the chosen deck indices once the player confirms.</summary>
        public event Action<IReadOnlyList<int>> TileChoiceConfirmed;

        private RunManager _run;
        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardContainer;
        private Text _title;
        private RectTransform _previewsContainer;
        private RectTransform _previewsContainer2;
        private RectTransform _shelf1;
        private RectTransform _shelf2;
        private const float ShelfHeight = 16f;
        private const float RowGap = 6f;
        private Button _confirmButton;
        private RectTransform _confirmRect;

        private readonly List<int> _candidates = new List<int>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly Dictionary<int, Image> _cellBackgroundByIndex = new Dictionary<int, Image>();
        private readonly Dictionary<int, RectTransform> _previewContainerByIndex = new Dictionary<int, RectTransform>();
        private int _requiredCount;

        // Set once per Show() call from the upgrade being resolved, so OnCellClicked doesn't need to know about upgrades itself.
        private PieceTraitKind? _previewTraitKind;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "TileChoiceOverlay", new Color(0f, 0f, 0f, 0f));
            // Fully transparent: the choice happens ON the shop screen (its stall backdrop stays visible, its shelves are swapped for the seeds).
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), as part of the whole block's layout — see LayoutBlock.

            _title = UIFactory.CreateText(_root, "Title", "", 22, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), as part of the whole block's layout — see LayoutBlock.
            _title.rectTransform.sizeDelta = new Vector2(700f, 40f);

            _previewsContainer = BuildSeedRow("Previews");
            _shelf1 = BuildShelf("Shelf1");
            _previewsContainer2 = BuildSeedRow("Previews2");
            _shelf2 = BuildShelf("Shelf2");

            _confirmButton = UIFactory.CreateButton(_root, "Confirm", "Confirm", UISprites.ChooseButtonBackground, 18);
            _confirmRect = _confirmButton.GetComponent<RectTransform>();
            _confirmRect.anchorMin = new Vector2(0.5f, 1f);
            _confirmRect.anchorMax = new Vector2(0.5f, 1f);
            _confirmRect.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), as part of the same measured, vertically-centered block — see LayoutBlock.
            _confirmRect.sizeDelta = new Vector2(200f, ConfirmHeight);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>One bare row of seeds: no parchment box, just the seeds side by side, resting on a shelf below.</summary>
        private RectTransform BuildSeedRow(string name)
        {
            var row = UIFactory.CreateUIObject(name, _root);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return row;
        }

        /// <summary>A wooden shelf (same look as the shop's display shelves) under a row of seeds.</summary>
        private RectTransform BuildShelf(string name)
        {
            var board = UIFactory.CreatePanel(_root, name, new Color(0.45f, 0.30f, 0.17f, 1f));
            board.raycastTarget = false;
            var rect = board.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var lip = UIFactory.CreatePanel(rect, "Lip", new Color(0.62f, 0.43f, 0.24f, 1f));
            lip.raycastTarget = false;
            lip.rectTransform.anchorMin = new Vector2(0f, 1f);
            lip.rectTransform.anchorMax = new Vector2(1f, 1f);
            lip.rectTransform.pivot = new Vector2(0.5f, 1f);
            lip.rectTransform.anchoredPosition = Vector2.zero;
            lip.rectTransform.sizeDelta = new Vector2(0f, 5f);
            var shadow = UIFactory.CreatePanel(rect, "Shadow", new Color(0f, 0f, 0f, 0.35f));
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = new Vector2(0f, 0f);
            shadow.rectTransform.anchorMax = new Vector2(1f, 0f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 1f);
            shadow.rectTransform.anchoredPosition = Vector2.zero;
            shadow.rectTransform.sizeDelta = new Vector2(0f, 14f);
            return rect;
        }

        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        public void Show(RunManager run, DeckManager deck, IReadOnlyList<int> candidateDeckIndices, int requiredCount, UpgradeDefinition def, UpgradeId? source = null)
        {
            _run = run;
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
            // The shop object that led here (the anvil with the upgrade's glyph, the pack's crate, or the upgrade's own sign); its name, category and description are in the hover tooltip.
            var card = UIFactory.CreatePanel(_cardContainer, "Source", Color.white);
            card.sprite = source == UpgradeId.Forge ? UpgradeVisualDefaults.GetWorkshopArt("Anvil")
                : source == UpgradeId.SproutPack ? UpgradeVisualDefaults.GetWorkshopArt("SproutPack")
                : UpgradeVisualDefaults.GetIcon(def.Id);
            card.preserveAspect = true;
            card.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            card.rectTransform.pivot = new Vector2(0.5f, 1f);
            card.rectTransform.anchoredPosition = Vector2.zero;
            card.rectTransform.sizeDelta = new Vector2(SignSize, SignSize);
            var glyphSprite = source.HasValue ? UpgradeVisualDefaults.GetGlyph(def.Id) : null;
            if (glyphSprite != null)
            {
                var glyph = UIFactory.CreatePanel(card.transform, "Glyph", Color.white);
                glyph.sprite = glyphSprite;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                UpgradeVisualDefaults.PlaceGlyph(glyph.rectTransform, source == UpgradeId.Forge ? UpgradeVisualDefaults.GlyphSurface.Anvil : UpgradeVisualDefaults.GlyphSurface.Crate, SignSize);
            }
            var hover = card.gameObject.AddComponent<HoverTooltip>();
            hover.Init(_tooltip, def.Name, DescriptionTextFormatter.Colorize(def.Description, 14),
                "Upgrade · " + UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool).Replace(" Upgrade", ""), UITheme.TextMuted);

            _title.text = "Select " + _requiredCount + " seeds";

            for (int i = _previewsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewsContainer.GetChild(i).gameObject);
            }
            for (int i = _previewsContainer2.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewsContainer2.GetChild(i).gameObject);
            }
            // Two rows (a second one only when needed), sized to stay between the two tablets.
            int firstRow = (_candidates.Count + 1) / 2;
            float middleWidth = 2f * (PieceBox.SidePanelCenterX - ModifierPanelView.PanelWidth * 0.5f) - 40f;
            PieceBox.Configure(Mathf.Max(1, firstRow), middleWidth);
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildPreviewCell(_candidates[i], i < firstRow ? _previewsContainer : _previewsContainer2);
            }
            _previewsContainer2.gameObject.SetActive(_candidates.Count > firstRow);
            _shelf2.gameObject.SetActive(_candidates.Count > firstRow);

            LayoutBlock(SignSize);

            RefreshConfirmInteractable();
            _root.gameObject.SetActive(true);
        }

        /// <summary>Stacks card/title/previews/Confirm as one block and centers it vertically in the overlay. Every height here is either measured (the card) or a fixed constant, never guessed.</summary>
        private void LayoutBlock(float cardHeight)
        {
            float rowHeight = PieceBox.CellSize + RowGap;
            int rows = _previewsContainer2.gameObject.activeSelf ? 2 : 1;
            float totalHeight = cardHeight + BlockSpacing + TitleHeight + BlockSpacing + rows * (rowHeight + ShelfHeight + 8f) + BlockSpacing + ConfirmHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _cardContainer.anchoredPosition = new Vector2(0f, topY);
            float y = topY - cardHeight - BlockSpacing;

            _title.rectTransform.anchoredPosition = new Vector2(0f, y);
            y -= TitleHeight + BlockSpacing;

            float shelfWidth = Mathf.Max(PieceBox.CellSize * Mathf.Ceil(_candidates.Count / 2f) + 40f, 280f);
            y = PlaceRow(_previewsContainer, _shelf1, y, rowHeight, shelfWidth);
            if (rows == 2)
            {
                y = PlaceRow(_previewsContainer2, _shelf2, y, rowHeight, shelfWidth);
            }
            y -= BlockSpacing - 8f;

            _confirmRect.anchoredPosition = new Vector2(0f, y);
        }

        private static float PlaceRow(RectTransform row, RectTransform shelf, float y, float rowHeight, float shelfWidth)
        {
            row.anchoredPosition = new Vector2(0f, y);
            shelf.anchoredPosition = new Vector2(0f, y - rowHeight + 4f);
            shelf.sizeDelta = new Vector2(shelfWidth, ShelfHeight);
            return y - rowHeight - ShelfHeight - 8f;
        }

        private void BuildPreviewCell(int deckIndex, RectTransform row)
        {
            var cell = PieceBox.CreateCell(row, "Tile_" + deckIndex, PieceBox.CellSize, false);
            _cellBackgroundByIndex[deckIndex] = cell;

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnCellClicked(deckIndex));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = Vector2.zero;
            previewContainer.sizeDelta = new Vector2(PieceBox.PreviewSize, PieceBox.PreviewSize);
            _previewContainerByIndex[deckIndex] = previewContainer;

            RebuildPreview(deckIndex, showTrait: false, animate: false);
        }

        /// <summary>(Re)draws the piece preview for <paramref name="deckIndex"/> — plain when not selected, or with the actual trait this upgrade grants when <paramref name="showTrait"/> is true. The trait shown is a preview only — DeckManager.TagSpecificTokens still picks the real cell/color once confirmed.</summary>
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
                // Tinted always tints to the token's own color; cell index 0 is a placeholder, the exact cell is decided on confirm.
                PieceColor? tintedColor = PieceTrait.TargetColorFor(_previewTraitKind.Value, token.Color);
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
                // Selecting a different piece before this finishes fading in destroys this badge — bail out.
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
            _cellBackgroundByIndex[deckIndex].color = nowSelected ? PieceBox.Selected : PieceBox.Idle;
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
