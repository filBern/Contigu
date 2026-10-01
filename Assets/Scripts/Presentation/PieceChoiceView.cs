using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals "Random Piece"
    /// (spec extension, explicit request: "Propose 5 choix de pièces et le
    /// joueur en sélectionne une. Chaque pièce a un pourcentage de chance
    /// d'être upgradé avec une tuile spéciale") — the player picks ONE of
    /// RunManager.PendingUpgradePieceCandidates to add to their deck.
    /// Structurally close to TileChoiceView (same measured, vertically
    /// centered card+title+previews+Confirm block), but simpler in two
    /// ways: candidates are whole PieceToken values handed straight from
    /// Core rather than deck indices (there's no deck to look them up in —
    /// they don't exist yet), and any trait a candidate carries was already
    /// rolled by UpgradeSystem.GetCandidatePiecesFor, so it's just shown
    /// outright rather than progressively previewed on selection the way
    /// TileChoiceView's speculative trait preview is.
    /// </summary>
    public sealed class PieceChoiceView : MonoBehaviour
    {
        private const float CellSize = 160f;
        private const float PreviewSize = 116f;
        private const float LevelLabelHeight = 20f;
        private const float TitleHeight = 40f;
        private const float ConfirmHeight = 46f;
        private const float BlockSpacing = 24f;
        // The canvas is always exactly this tall in its own local units
        // regardless of actual window size (CanvasScaler matches on height
        // — see GameBootstrap.BuildCanvas), so centering math done in this
        // space holds for any resolution.
        private const float CanvasHeight = 800f;

        /// <summary>Fires with the chosen candidate's index once the player confirms.</summary>
        public event Action<int> PieceChoiceConfirmed;

        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardContainer;
        private Text _title;
        private RectTransform _previewsContainer;
        private Button _confirmButton;
        private RectTransform _confirmRect;

        private readonly List<PieceToken> _candidates = new List<PieceToken>();
        private readonly Dictionary<int, Image> _cellBackgroundByIndex = new Dictionary<int, Image>();
        private int _selectedIndex = -1;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "PieceChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), as part of the whole block's
            // layout — see LayoutBlock.

            _title = UIFactory.CreateText(_root, "Title", "Choose a piece", 22, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            _title.rectTransform.sizeDelta = new Vector2(700f, 40f);

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
            _confirmRect = _confirmButton.GetComponent<RectTransform>();
            _confirmRect.anchorMin = new Vector2(0.5f, 1f);
            _confirmRect.anchorMax = new Vector2(0.5f, 1f);
            _confirmRect.pivot = new Vector2(0.5f, 1f);
            _confirmRect.sizeDelta = new Vector2(200f, ConfirmHeight);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(IReadOnlyList<PieceToken> candidates, UpgradeDefinition def, RunManager run)
        {
            _candidates.Clear();
            _candidates.AddRange(candidates);
            _selectedIndex = -1;
            _cellBackgroundByIndex.Clear();

            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }
            var card = UpgradeCardFactory.Build(_cardContainer, def);

            _title.text = "Choose 1 piece to add";

            for (int i = _previewsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewsContainer.GetChild(i).gameObject);
            }
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildPreviewCell(i, run);
            }

            LayoutBlock(card.sizeDelta.y);

            RefreshConfirmInteractable();
            _root.gameObject.SetActive(true);
        }

        /// <summary>Same "measure everything, guess nothing, center the whole block" layout as TileChoiceView.LayoutBlock — see there for why.</summary>
        private void LayoutBlock(float cardHeight)
        {
            float totalHeight = cardHeight + BlockSpacing + TitleHeight + BlockSpacing + CellSize + BlockSpacing + ConfirmHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _cardContainer.anchoredPosition = new Vector2(0f, topY);
            float y = topY - cardHeight - BlockSpacing;

            _title.rectTransform.anchoredPosition = new Vector2(0f, y);
            y -= TitleHeight + BlockSpacing;

            _previewsContainer.anchoredPosition = new Vector2(0f, y);
            y -= CellSize + BlockSpacing;

            _confirmRect.anchoredPosition = new Vector2(0f, y);
        }

        private void BuildPreviewCell(int index, RunManager run)
        {
            var cell = UIFactory.CreatePanel(_previewsContainer, "Piece_" + index, UITheme.ButtonIdle);
            cell.rectTransform.sizeDelta = new Vector2(CellSize, CellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = CellSize;
            cellLayout.preferredHeight = CellSize;
            _cellBackgroundByIndex[index] = cell;

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnCellClicked(index));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(0f, 18f);
            previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);

            // The trait (if any) was already rolled for this candidate — no
            // "preview on select" step needed the way TileChoiceView's
            // speculative trait preview has, since there's no hidden
            // outcome left to reveal.
            var token = _candidates[index];
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(token.Shape), token.Color, token.Trait, _tooltip, cell.gameObject);

            int level = run.GetColorMasteryLevel(token.Color) + run.GetShapeMasteryLevel(token.Shape) - 1;
            var levelLabel = UIFactory.CreateText(cell.transform, "PieceLevel", "Lv. " + level, 14, Color.black);
            levelLabel.raycastTarget = false;
            levelLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            levelLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            levelLabel.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            levelLabel.rectTransform.sizeDelta = new Vector2(0f, LevelLabelHeight);
        }

        private void OnCellClicked(int index)
        {
            if (_selectedIndex >= 0 && _cellBackgroundByIndex.TryGetValue(_selectedIndex, out var previous))
            {
                previous.color = UITheme.ButtonIdle;
            }
            _selectedIndex = index;
            _cellBackgroundByIndex[index].color = UITheme.ButtonSelected;
            RefreshConfirmInteractable();
        }

        private void RefreshConfirmInteractable()
        {
            _confirmButton.interactable = _selectedIndex >= 0;
        }

        private void OnConfirmClicked()
        {
            if (_selectedIndex < 0)
            {
                return;
            }
            _root.gameObject.SetActive(false);
            if (PieceChoiceConfirmed != null)
            {
                PieceChoiceConfirmed(_selectedIndex);
            }
        }
    }
}
