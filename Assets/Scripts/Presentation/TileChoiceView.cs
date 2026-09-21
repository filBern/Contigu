using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals a Grid-pool
    /// (piece-trait) upgrade: the player picks which of a handful of
    /// candidate deck tokens actually receive it (spec: "un choix de 5
    /// tiles"), instead of the old random assignment. Toggle any candidate
    /// row on/off; Confirm enables once exactly
    /// EconomyConstants.ShopTileChoiceCount are selected (or fewer, if the
    /// deck didn't even have that many candidates to offer). Shows the
    /// upgrade's own card (see UpgradeCardFactory) above the row list, on
    /// explicit request, so a mystery shop slot's reveal is actually
    /// readable and not just a name.
    /// </summary>
    public sealed class TileChoiceView : MonoBehaviour
    {
        private const float RowHeight = 56f;
        private const float RowPreviewSize = 44f;

        /// <summary>Fires with the chosen deck indices once the player confirms.</summary>
        public event Action<IReadOnlyList<int>> TileChoiceConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardContainer;
        private Text _title;
        private RectTransform _rowsContainer;
        private Button _confirmButton;

        private readonly List<int> _candidates = new List<int>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly Dictionary<int, Image> _rowBackgroundByIndex = new Dictionary<int, Image>();
        private int _requiredCount;

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

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            var layout = _rowsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _rowsContainer.gameObject.AddComponent<ContentSizeFitter>();
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
            _rowBackgroundByIndex.Clear();
            _requiredCount = Mathf.Min(requiredCount, _candidates.Count);

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
            _rowsContainer.anchoredPosition = new Vector2(0f, bodyTopY - 40f);

            _title.text = "Choose " + _requiredCount + " of " + _candidates.Count + " pieces";

            for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowsContainer.GetChild(i).gameObject);
            }
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildRow(_candidates[i]);
            }

            RefreshConfirmInteractable();
            _root.gameObject.SetActive(true);
        }

        private void BuildRow(int deckIndex)
        {
            var token = _deck.Deck[deckIndex];
            var row = UIFactory.CreatePanel(_rowsContainer, "Tile_" + deckIndex, UITheme.ButtonIdle);
            row.rectTransform.sizeDelta = new Vector2(340f, RowHeight);
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = 340f;
            rowLayout.preferredHeight = RowHeight;
            _rowBackgroundByIndex[deckIndex] = row;

            var rowBtn = row.gameObject.AddComponent<Button>();
            rowBtn.onClick.AddListener(() => OnRowClicked(deckIndex));

            var previewContainer = UIFactory.CreateUIObject("Preview", row.transform);
            previewContainer.anchorMin = new Vector2(0f, 0.5f);
            previewContainer.anchorMax = new Vector2(0f, 0.5f);
            previewContainer.pivot = new Vector2(0f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(8f, 0f);
            previewContainer.sizeDelta = new Vector2(RowPreviewSize, RowPreviewSize);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(token.Shape), token.Color, token.Trait, _tooltip, row.gameObject);

            var label = UIFactory.CreateText(row.transform, "Label", VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")", 15, UITheme.TextPrimary);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(RowPreviewSize + 18f, 0f);
            label.rectTransform.sizeDelta = new Vector2(-(RowPreviewSize + 90f), 30f);
        }

        private void OnRowClicked(int deckIndex)
        {
            if (_selected.Contains(deckIndex))
            {
                _selected.Remove(deckIndex);
            }
            else if (_selected.Count < _requiredCount)
            {
                _selected.Add(deckIndex);
            }

            _rowBackgroundByIndex[deckIndex].color = _selected.Contains(deckIndex) ? UITheme.ButtonSelected : UITheme.ButtonIdle;
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
