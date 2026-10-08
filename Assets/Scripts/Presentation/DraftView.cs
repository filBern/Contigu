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
    /// Sub-choice overlay for a Bank-pool upgrade the shop just revealed.
    /// Replace/Dupliquer/Recolorer need a piece type; Recolorer also needs a
    /// target color; Replace needs a SECOND piece type (see
    /// <see cref="ShowReplacementTypeChoice"/>). Joker has no sub-choice and
    /// never reaches this view (see RunManager.BuyUpgradeSlot). Entered
    /// directly via <see cref="ShowForPendingUpgrade"/>, showing the
    /// upgrade's own card (see UpgradeCardFactory) as a fixed header above
    /// the picker.
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        // PreviewCellSize matches PieceChoiceView's CellSize to leave room for the level label under each preview (see BuildTypePreviewCell); PreviewSize matches TileChoiceView's glyph size.
        private const float PreviewCellSize = 112f;
        private const float PreviewSize = 81f;
        private const float LevelLabelHeight = 20f;
        private const float ConfirmHeight = 46f;
        private const float TitleHeight = 40f;
        private const float BlockSpacing = 24f;
        // Canvas is always this tall in local units regardless of window size (CanvasScaler matches on height; see GameBootstrap.BuildCanvas), so centering math here holds at any resolution.
        private const float CanvasHeight = 800f;
        private const float FadeDuration = 0.4f;
        private const float PostFadeHold = 0.15f;

        /// <summary>Fires once the sub-choice has been made and the upgrade should be resolved.</summary>
        public event Action<UpgradeSubChoice> SubChoiceConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RunManager _run;
        private RectTransform _root;
        private RectTransform _cardInstance;
        private float _bodyTopY;
        private IReadOnlyList<(ShapeId Shape, PieceColor Color)> _typeCandidates;

        private RectTransform _typePreviewsContainer;
        private Button _typeConfirmButton;
        private readonly Dictionary<int, Image> _typeCellByIndex = new Dictionary<int, Image>();
        private readonly Dictionary<int, RectTransform> _typePreviewContainerByIndex = new Dictionary<int, RectTransform>();
        private int _selectedTypeIndex = -1;

        // "Replace a piece" 2-step state: step 1 picks the type going away (_replaceRemoveShape/Color); step 2 reuses the same type-grid picker for the replacement type, distinguished by _isReplaceStepTwo (see OnTypeConfirmClicked).
        private bool _isReplaceStepTwo;
        private ShapeId _replaceRemoveShape;
        private PieceColor _replaceRemoveColor;

        public RectTransform Build(Transform parent, DeckManager deck, TooltipView tooltip)
        {
            _deck = deck;
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "SubChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);
            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        /// <summary>
        /// Opens straight to the sub-choice this upgrade needs: piece type
        /// for Replace/Dupliquer/Recolorer, then the target color
        /// (Recolorer) or a second piece type (Replace). <paramref name="def"/>
        /// must be a Bank-pool upgrade with <see cref="UpgradeDefinition.RequiresSubChoice"/>
        /// true. <paramref name="candidateTypes"/> is the up-to-5 subset of
        /// the deck's composition to offer. <paramref name="run"/> is only
        /// needed for each candidate's Mastery level label (see BuildTypePreviewCell).
        /// </summary>
        public void ShowForPendingUpgrade(UpgradeDefinition def, IReadOnlyList<(ShapeId Shape, PieceColor Color)> candidateTypes, RunManager run)
        {
            _typeCandidates = candidateTypes;
            _run = run;
            _isReplaceStepTwo = false;
            _root.gameObject.SetActive(true);
            ClearChildren();

            // Held in its own field so ClearChildren (called again by ShowColorChoice, e.g. Recolorer's 2nd step) never tears it down mid-flow.
            if (_cardInstance != null)
            {
                Destroy(_cardInstance.gameObject);
            }
            _cardInstance = UpgradeCardFactory.Build(_root, def);
            _cardInstance.anchorMin = new Vector2(0.5f, 1f);
            _cardInstance.anchorMax = new Vector2(0.5f, 1f);
            _cardInstance.pivot = new Vector2(0.5f, 1f);
            const float gapBelowCard = 24f;
            // Card height varies with description length, so everything below it is placed relative to the measured height rather than a fixed offset. The whole block is vertically centered on screen.
            float cardHeight = _cardInstance.sizeDelta.y;
            float totalHeight = cardHeight + gapBelowCard + TitleHeight + PreviewCellSize + BlockSpacing + ConfirmHeight;
            float cardTopY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);
            _cardInstance.anchoredPosition = new Vector2(0f, cardTopY);
            _bodyTopY = cardTopY - cardHeight - gapBelowCard;

            ShowTypeChoice(def);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        private void ClearChildren()
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (_cardInstance != null && child == _cardInstance)
                {
                    continue;
                }
                Destroy(child.gameObject);
            }
        }

        private void ShowTypeChoice(UpgradeDefinition def, string titleText = "Choose a piece type")
        {
            ClearChildren();
            _selectedTypeIndex = -1;
            _typeCellByIndex.Clear();
            _typePreviewContainerByIndex.Clear();

            var title = UIFactory.CreateText(_root, "Title", titleText, 22, UITheme.TextOnBackground);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, _bodyTopY);
            title.rectTransform.sizeDelta = new Vector2(600f, TitleHeight);

            _typePreviewsContainer = UIFactory.CreateUIObject("TypePreviews", _root);
            _typePreviewsContainer.anchorMin = new Vector2(0.5f, 1f);
            _typePreviewsContainer.anchorMax = new Vector2(0.5f, 1f);
            _typePreviewsContainer.pivot = new Vector2(0.5f, 1f);
            _typePreviewsContainer.anchoredPosition = new Vector2(0f, _bodyTopY - TitleHeight);
            var layout = _typePreviewsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _typePreviewsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < _typeCandidates.Count; i++)
            {
                BuildTypePreviewCell(i);
            }

            var confirmButton = UIFactory.CreateButton(_root, "TypeConfirm", "Confirm", UISprites.ChooseButtonBackground, 18);
            _typeConfirmButton = confirmButton;
            var confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 1f);
            confirmRect.anchorMax = new Vector2(0.5f, 1f);
            confirmRect.pivot = new Vector2(0.5f, 1f);
            confirmRect.anchoredPosition = new Vector2(0f, _bodyTopY - TitleHeight - PreviewCellSize - BlockSpacing);
            confirmRect.sizeDelta = new Vector2(200f, ConfirmHeight);
            confirmButton.interactable = false;
            confirmButton.onClick.AddListener(() => OnTypeConfirmClicked(def));
        }

        /// <summary>One candidate in the type picker: a shape/color preview (see ShapePreviewFactory) plus its combined Mastery level below. Click selects it exclusively (radio-button style); the Confirm button is what commits.</summary>
        private void BuildTypePreviewCell(int index)
        {
            var shape = _typeCandidates[index].Shape;
            var color = _typeCandidates[index].Color;

            var cell = UIFactory.CreatePanel(_typePreviewsContainer, "Type_" + index, UITheme.ButtonIdle);
            cell.rectTransform.sizeDelta = new Vector2(PreviewCellSize, PreviewCellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = PreviewCellSize;
            cellLayout.preferredHeight = PreviewCellSize;
            _typeCellByIndex[index] = cell;

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnTypeCellClicked(index));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(0f, 13f);
            previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);
            _typePreviewContainerByIndex[index] = previewContainer;

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, cell.gameObject);

            int level = _run.GetColorMasteryLevel(color) + _run.GetShapeMasteryLevel(shape) - 1;
            var levelLabel = UIFactory.CreateText(cell.transform, "TypeLevel", "Lv. " + level, 14, Color.black);
            levelLabel.raycastTarget = false;
            levelLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            levelLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            levelLabel.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            levelLabel.rectTransform.sizeDelta = new Vector2(0f, LevelLabelHeight);
        }

        /// <summary>The trait carried by the first deck token matching (shape, color) that has one, or null if none of that type's copies are enchanted. Replace/Dupliquer/Recolorer operate on a type rather than a specific token (see DeckManager.ReplaceOneOfType), so this is a representative sample when copies carry different traits.</summary>
        private PieceTrait? FindRepresentativeTrait(ShapeId shape, PieceColor color)
        {
            var tokens = _deck.Deck;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Matches(shape, color) && tokens[i].Trait.HasValue)
                {
                    return tokens[i].Trait;
                }
            }
            return null;
        }

        private void OnTypeCellClicked(int index)
        {
            _selectedTypeIndex = _selectedTypeIndex == index ? -1 : index;
            foreach (var kvp in _typeCellByIndex)
            {
                kvp.Value.color = kvp.Key == _selectedTypeIndex ? UITheme.ButtonSelected : UITheme.ButtonIdle;
            }
            _typeConfirmButton.interactable = _selectedTypeIndex >= 0;
        }

        private void OnTypeConfirmClicked(UpgradeDefinition def)
        {
            if (_selectedTypeIndex < 0)
            {
                return;
            }
            var shape = _typeCandidates[_selectedTypeIndex].Shape;
            var color = _typeCandidates[_selectedTypeIndex].Color;

            SetTypeCellsInteractable(false);
            _typeConfirmButton.interactable = false;

            if (_isReplaceStepTwo)
            {
                // Step 2: shape/color here is the existing type chosen to replace whatever step 1 faded out (see ShowReplacementTypeChoice).
                var replaceSub = new UpgradeSubChoice(_replaceRemoveShape, _replaceRemoveColor, addShape: shape, addColor: color);
                StartCoroutine(FadeInDuplicateThenFinalize(shape, color, replaceSub));
                return;
            }

            var sub = new UpgradeSubChoice(shape, color);

            if (def.Id == UpgradeId.RecolorPiece)
            {
                ShowColorChoice(shape, color);
                return;
            }
            if (def.Id == UpgradeId.ReplacePiece)
            {
                // Fades the chosen piece's preview out, then advances to step 2: which existing type replaces it.
                StartCoroutine(FadeOutSelectedThenAdvance(_typePreviewContainerByIndex[_selectedTypeIndex], () => ShowReplacementTypeChoice(shape, color)));
                return;
            }
            if (def.Id == UpgradeId.DuplicatePiece)
            {
                StartCoroutine(FadeInDuplicateThenFinalize(shape, color, sub));
                return;
            }
            FinalizeChoice(sub);
        }

        /// <summary>"Replace a piece" step 2: reuses the same type-grid picker as step 1 with a different title and candidate list, excluding the type just picked to go away (see UpgradeSystem.GetReplacementCandidateTypesFor).</summary>
        private void ShowReplacementTypeChoice(ShapeId removeShape, PieceColor removeColor)
        {
            _replaceRemoveShape = removeShape;
            _replaceRemoveColor = removeColor;
            _isReplaceStepTwo = true;
            _typeCandidates = _run.Upgrades.GetReplacementCandidateTypesFor(_deck, removeShape, removeColor);
            ShowTypeChoice(UpgradeCatalog.ReplacePiece, "Choose a piece to duplicate instead");
        }

        private void SetTypeCellsInteractable(bool interactable)
        {
            foreach (var cell in _typeCellByIndex.Values)
            {
                var btn = cell.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = interactable;
                }
            }
        }

        /// <summary>Replace (step 1 of 2): fades the chosen piece's preview out to visualize it leaving the deck, then runs <paramref name="onComplete"/> (advancing to step 2, see ShowReplacementTypeChoice) instead of finalizing outright.</summary>
        private IEnumerator FadeOutSelectedThenAdvance(RectTransform previewContainer, Action onComplete)
        {
            var canvasGroup = previewContainer.gameObject.AddComponent<CanvasGroup>();
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                if (previewContainer == null)
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
                yield return null;
            }
            if (previewContainer == null)
            {
                yield break;
            }
            canvasGroup.alpha = 0f;
            yield return new WaitForSeconds(PostFadeHold);
            onComplete();
        }

        /// <summary>Dupliquer: fades a new copy of the chosen piece in next to it, then resolves. Purely visual: DeckManager.DuplicateOfType adds the real copy once <see cref="SubChoiceConfirmed"/> fires.</summary>
        private IEnumerator FadeInDuplicateThenFinalize(ShapeId shape, PieceColor color, UpgradeSubChoice sub)
        {
            var trait = FindRepresentativeTrait(shape, color);
            var clone = UIFactory.CreatePanel(_typePreviewsContainer, "Duplicate", UITheme.ButtonIdle);
            clone.rectTransform.sizeDelta = new Vector2(PreviewCellSize, PreviewCellSize);
            var cloneLayout = clone.gameObject.AddComponent<LayoutElement>();
            cloneLayout.preferredWidth = PreviewCellSize;
            cloneLayout.preferredHeight = PreviewCellSize;

            var previewContainer = UIFactory.CreateUIObject("Preview", clone.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            // Same +13 offset as BuildTypePreviewCell's cells so the glyph lines up with its row-mates.
            previewContainer.anchoredPosition = new Vector2(0f, 13f);
            previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, clone.gameObject);

            var canvasGroup = clone.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                if (clone == null)
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
                yield return null;
            }
            if (clone == null)
            {
                yield break;
            }
            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(PostFadeHold);
            FinalizeChoice(sub);
        }

        private void ShowColorChoice(ShapeId shape, PieceColor fromColor)
        {
            ClearChildren();

            var title = UIFactory.CreateText(_root, "Title", "Choose the target color", 22, UITheme.TextOnBackground);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, _bodyTopY);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            var listContainer = UIFactory.CreateUIObject("Colors", _root);
            listContainer.anchorMin = new Vector2(0.5f, 1f);
            listContainer.anchorMax = new Vector2(0.5f, 1f);
            listContainer.pivot = new Vector2(0.5f, 1f);
            listContainer.anchoredPosition = new Vector2(0f, _bodyTopY - 50f);
            var hLayout = listContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            var colorsFitter = listContainer.gameObject.AddComponent<ContentSizeFitter>();
            colorsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            colorsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var targetColor = baseColors[i];
                if (targetColor == fromColor)
                {
                    continue;
                }
                var btn = UIFactory.CreateButton(listContainer, "Color", VisualDefaults.GetColorName(targetColor), VisualDefaults.GetColor(targetColor), 14);
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 60f);
                // Pin the size so the parent HorizontalLayoutGroup doesn't collapse this button.
                var colorBtnLayout = btn.gameObject.AddComponent<LayoutElement>();
                colorBtnLayout.preferredWidth = 140f;
                colorBtnLayout.preferredHeight = 60f;
                btn.onClick.AddListener(() => FinalizeChoice(new UpgradeSubChoice(shape, fromColor, targetColor)));
            }
        }

        private void FinalizeChoice(UpgradeSubChoice sub)
        {
            _root.gameObject.SetActive(false);
            if (SubChoiceConfirmed != null)
            {
                SubChoiceConfirmed(sub);
            }
        }
    }
}
