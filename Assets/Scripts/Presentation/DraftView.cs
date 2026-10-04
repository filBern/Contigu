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
    /// Sub-choice overlay for a Bank-pool upgrade the shop just revealed
    /// (Replace/Dupliquer/Recolorer need a piece type; Recolorer also needs
    /// a target color, Replace a SECOND piece type — see <see
    /// cref="ShowReplacementTypeChoice"/> — Joker has no sub-choice and
    /// never reaches this view at all, see RunManager.BuyUpgradeSlot). Used
    /// to be the whole round-end draft (a grid of 3 cards to pick from)
    /// before the Lueur shop replaced that entirely (spec extension,
    /// explicit request) — this is now only the piece/color picker half of
    /// that old flow, entered directly via <see
    /// cref="ShowForPendingUpgrade"/> instead of by choosing a card. Still
    /// shows the upgrade's own card (see UpgradeCardFactory) as a fixed
    /// header above the picker, on explicit request — a mystery shop slot
    /// only ever showed its UpgradePool before purchase, so this is the
    /// first moment the player can actually read what they got.
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        // 30% smaller than the original 140/116 (explicit request: "met les
        // carte de piece 30% plus petit" — applies here too, same "5 piece
        // candidate cards" shape as PieceChoiceView, just for Replace/
        // Dupliquer/Recolorer's existing-deck-type picker instead of Random
        // Piece's freshly-rolled candidates). PreviewCellSize bumped back up
        // to 112 (PieceChoiceView's own CellSize) once the type picker
        // started showing a level label under each preview — see
        // BuildTypePreviewCell — which needs the same headroom
        // PieceChoiceView's cells already budget for theirs; PreviewSize
        // (the glyph itself) stays matched to TileChoiceView's 81.
        private const float PreviewCellSize = 112f;
        private const float PreviewSize = 81f;
        private const float LevelLabelHeight = 20f;
        private const float ConfirmHeight = 46f;
        private const float TitleHeight = 40f;
        private const float BlockSpacing = 24f;
        // The canvas is always exactly this tall in its own local units
        // regardless of actual window size (CanvasScaler matches on height
        // — see GameBootstrap.BuildCanvas), so centering math done in this
        // space holds for any resolution. Same constant/convention as
        // PieceChoiceView/TileChoiceView.
        private const float CanvasHeight = 800f;
        private const float FadeDuration = 0.4f;
        // A brief hold after a fade finishes so the player actually
        // registers the piece disappearing/appearing before the whole
        // overlay closes out from under it.
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

        // "Replace a piece"'s own 2-step state (redesign, explicit request:
        // "Les upgrades 'remove' sont vraiment chiante, peux-tu la changer
        // pour un replace?") — step 1 picks the type going away
        // (_replaceRemoveShape/Color, captured right as step 2 opens);
        // step 2 reuses this SAME type-grid picker for which existing type
        // to duplicate instead, distinguished from a plain step 1 by
        // _isReplaceStepTwo (see OnTypeConfirmClicked).
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
        /// Opens straight to the sub-choice this upgrade needs — the piece
        /// type for Replace/Dupliquer/Recolorer, then the target color
        /// (Recolorer) or a second piece type (Replace). <paramref name="def"/> must be a Bank-pool upgrade
        /// with <see cref="UpgradeDefinition.RequiresSubChoice"/> true (the
        /// shop never calls this for anything else). <paramref
        /// name="candidateTypes"/> (RunManager.PendingUpgradeTypeCandidates)
        /// is the up-to-5 subset of the deck's composition to actually offer
        /// — explicit request, the type picker used to list every distinct
        /// type in the deck at once. <paramref name="run"/> is only needed
        /// for each candidate's Mastery level label — see
        /// BuildTypePreviewCell.
        /// </summary>
        public void ShowForPendingUpgrade(UpgradeDefinition def, IReadOnlyList<(ShapeId Shape, PieceColor Color)> candidateTypes, RunManager run)
        {
            _typeCandidates = candidateTypes;
            _run = run;
            _isReplaceStepTwo = false;
            _root.gameObject.SetActive(true);
            ClearChildren();

            // Reveal card (see UpgradeCardFactory) so the player can actually
            // read what they bought — the sub-choice screens below used to
            // just say "Choose a piece type" with no indication of which
            // upgrade that was for. Held in its own field so ClearChildren
            // (called again by ShowColorChoice, e.g. Recolorer's 2nd step)
            // never tears it down mid-flow.
            if (_cardInstance != null)
            {
                Destroy(_cardInstance.gameObject);
            }
            _cardInstance = UpgradeCardFactory.Build(_root, def);
            _cardInstance.anchorMin = new Vector2(0.5f, 1f);
            _cardInstance.anchorMax = new Vector2(0.5f, 1f);
            _cardInstance.pivot = new Vector2(0.5f, 1f);
            const float gapBelowCard = 24f;
            // Measured, not guessed — UpgradeCardFactory.Build already
            // computed the card's real height (it varies with the
            // description's length), so everything below it is placed
            // relative to that instead of a fixed offset that would either
            // overlap a long description or leave a big gap under a short
            // one. The whole block (card+title+preview row+Confirm) is now
            // vertically centered on screen too (same "measure everything,
            // center the whole block" approach as PieceChoiceView/
            // TileChoiceView) instead of pinned 20px from the top regardless
            // of content height — on explicit report: "ajuster la position
            // vertical de tout au centre de l'écran" (this view used to
            // hardcode cardTopY = -20f, hugging the top of the screen).
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

            // Preview-only, side by side — same style as TileChoiceView's
            // candidates, on explicit request ("je veux aussi qu'on affiche
            // seulement le preview"), replacing the old preview+name+count
            // row list.
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

            // _typeCandidates is already the (up-to-5, eligibility-filtered)
            // subset RunManager rolled — see UpgradeSystem.GetCandidateTypesFor.
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

        /// <summary>
        /// One candidate in the type picker — a shape/color preview (same
        /// look as a hand slot, see ShapePreviewFactory) plus its combined
        /// Mastery level below it (same "Lv. N" label and formula as
        /// PieceChoiceView's candidates — explicit request, specifically
        /// about Retirer: "il faudrait mettre le level de la pièce sous son
        /// preview pour avoir une meilleure idée de ce qu'on remove";
        /// shown for Dupliquer/Recolorer too since they share this same
        /// cell builder and the level is just as relevant context for
        /// either — a plain count/name label was explicitly turned down
        /// here before, but Mastery level wasn't part of that ask). Click
        /// selects it exclusively (radio-button style, since exactly one
        /// type is ever needed here); the Confirm button — not this click —
        /// is what actually commits to it, on explicit request ("il faut un
        /// confirm au lieu d'un immediate effect").
        /// </summary>
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

        /// <summary>
        /// The trait carried by the first deck token matching (shape, color)
        /// that has one, or null if none of that type's copies are enchanted.
        /// Since Replace/Dupliquer/Recolorer all operate on a TYPE rather than
        /// a specific token (see DeckManager.ReplaceOneOfType and friends),
        /// this is necessarily a representative sample when several copies of
        /// the same type carry different traits — showing "this type has an
        /// enchanted copy" rather than promising which exact copy an action
        /// would touch.
        /// </summary>
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
                // Step 2 of "Replace a piece": shape/color here is the
                // EXISTING type the player just picked to duplicate in
                // place of whatever step 1 faded out (_replaceRemoveShape/
                // Color) — see ShowReplacementTypeChoice.
                var replaceSub = new UpgradeSubChoice(_replaceRemoveShape, _replaceRemoveColor, addShape: shape, addColor: color);
                StartCoroutine(FadeInDuplicateThenFinalize(shape, color, replaceSub));
                return;
            }

            var sub = new UpgradeSubChoice(shape, color);

            if (def.Id == UpgradeId.RecolorPiece)
            {
                // Not a final commit yet — the target color is still needed,
                // so this only advances to that step, no animation.
                ShowColorChoice(shape, color);
                return;
            }
            if (def.Id == UpgradeId.ReplacePiece)
            {
                // Not a final commit yet either — fades the chosen piece's
                // preview out (same visual "it's leaving" cue the old
                // Remove had), then advances to step 2: which existing type
                // replaces it.
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

        /// <summary>"Replace a piece"'s second step (redesign, explicit request: "Les upgrades 'remove' sont vraiment chiante, peux-tu la changer pour un replace?") — reuses the SAME type-grid picker as step 1, just with a different title and candidate list: every OTHER existing deck type (see UpgradeSystem.GetReplacementCandidateTypesFor), excluding the one just picked to go away so the player is never offered the pointless no-op of replacing it with itself.</summary>
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

        /// <summary>Replace (step 1 of 2): fades the chosen piece's preview out to visualize it leaving the deck, same visual the old one-step Remove used, then runs <paramref name="onComplete"/> instead of finalizing outright — here, that's advancing to step 2 (see ShowReplacementTypeChoice) rather than resolving the upgrade.</summary>
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

        /// <summary>Dupliquer: fades a NEW copy of the chosen piece in next to it to visualize the extra copy being added, then resolves — explicit request. Purely visual: DeckManager.DuplicateOfType is what actually adds the real copy once <see cref="SubChoiceConfirmed"/> fires.</summary>
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
            // Same +13 offset as its row-mates in BuildTypePreviewCell (even
            // though this transient clone never gets its own level label)
            // so its piece glyph lines up with theirs instead of sitting
            // lower, now that the cell is taller than the glyph to leave
            // room for that label.
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
                // Same fix as elsewhere: pin the size so the parent
                // HorizontalLayoutGroup doesn't collapse this button.
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
