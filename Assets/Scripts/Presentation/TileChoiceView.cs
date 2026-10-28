using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Shown on the shop screen once a purchase leaves a tile upgrade to place (Forge, Sprout Pack, a Blister sprout): the player picks which
    /// candidate deck tokens receive it, on the shop's own shelves (see <see cref="ShelfStage"/>). Confirm enables once exactly
    /// EconomyConstants.ShopTileChoiceCount are selected (or fewer, if the deck has fewer candidates).
    /// </summary>
    public sealed class TileChoiceView : MonoBehaviour
    {
        private const float BadgeFadeDuration = 0.35f;

        /// <summary>Fires with the chosen deck indices once the player confirms.</summary>
        public event Action<IReadOnlyList<int>> TileChoiceConfirmed;

        private RunManager _run;
        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private ShelfStage _stage;
        private ShelfLayout _layout;

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

            // Fully transparent: the choice happens ON the shop screen (its stall backdrop stays visible, its shelves are swapped for these).
            var overlay = UIFactory.CreatePanel(parent, "TileChoiceOverlay", new Color(0f, 0f, 0f, 0f));
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);

            _stage = new ShelfStage(_root, tooltip);
            _stage.ConfirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Closes the overlay without resolving it (the purchase was abandoned).</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        /// <summary>Where the shop's shelves stand, so the seeds rest on exactly the same ones.</summary>
        public void SetShelfLayout(ShelfLayout layout)
        {
            _layout = layout;
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

            _stage.Begin(_layout, ShelfStage.ForUpgrade(def, source), Loc.T("choice.selectSeeds", _requiredCount, def.Name), _candidates.Count, true);
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildPreviewCell(_candidates[i], i);
            }

            RefreshConfirmInteractable();
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        private void BuildPreviewCell(int deckIndex, int position)
        {
            float cellSize = _stage.CellSize;
            var cell = PieceBox.CreateCell(_stage.RowFor(position), "Tile_" + deckIndex, cellSize, false);
            _cellBackgroundByIndex[deckIndex] = cell;

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnCellClicked(deckIndex));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = Vector2.zero;
            previewContainer.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
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

            // The seed's upgrades it already carries, plus (when selected) the one it is about to get on a free tile.
            var shown = new List<PieceTrait>(token.Traits);
            if (showTrait && _previewTraitKind.HasValue)
            {
                int cellCount = PieceShapeCatalog.Get(token.Shape).Cells.Count;
                int freeCell = 0;
                for (int c = 0; c < cellCount; c++)
                {
                    if (!token.UsesCell(c))
                    {
                        freeCell = c;
                        break;
                    }
                }
                // Tinted always tints to the token's own color; the exact tile is decided on confirm (this is a preview only).
                PieceColor? tintedColor = PieceTrait.TargetColorFor(_previewTraitKind.Value, token.Color);
                shown.RemoveAll(existing => existing.LocalCellIndex == freeCell && token.TraitCount >= cellCount);
                shown.Add(new PieceTrait(_previewTraitKind.Value, freeCell, tintedColor));
            }

            var badge = ShapePreviewFactory.BuildWithTraits(previewContainer, PieceShapeCatalog.Get(token.Shape), token.Color, shown, _tooltip, _cellBackgroundByIndex[deckIndex].gameObject);
            if (animate && badge != null && !MotionSettings.Reduced)
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
            _stage.ConfirmButton.interactable = _selected.Count == _requiredCount;
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
