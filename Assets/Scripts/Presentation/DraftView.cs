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
    /// (Retirer/Dupliquer/Recolorer need a piece type, and Recolorer also a
    /// target color — Joker has no sub-choice and never reaches this view at
    /// all, see RunManager.BuyUpgradeSlot). Used to be the whole round-end
    /// draft (a grid of 3 cards to pick from) before the Lueur shop replaced
    /// that entirely (spec extension, explicit request) — this is now only
    /// the piece/color picker half of that old flow, entered directly via
    /// <see cref="ShowForPendingUpgrade"/> instead of by choosing a card.
    /// Still shows the upgrade's own card (see UpgradeCardFactory) as a
    /// fixed header above the picker, on explicit request — a mystery shop
    /// slot only ever showed its UpgradePool before purchase, so this is the
    /// first moment the player can actually read what they got.
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        private const float PreviewCellSize = 140f;
        private const float PreviewSize = 116f;
        private const float ConfirmHeight = 46f;
        private const float TitleHeight = 40f;
        private const float BlockSpacing = 24f;
        private const float FadeDuration = 0.4f;
        // A brief hold after a fade finishes so the player actually
        // registers the piece disappearing/appearing before the whole
        // overlay closes out from under it.
        private const float PostFadeHold = 0.15f;

        /// <summary>Fires once the sub-choice has been made and the upgrade should be resolved.</summary>
        public event Action<UpgradeSubChoice> SubChoiceConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardInstance;
        private float _bodyTopY;
        private IReadOnlyList<(ShapeId Shape, PieceColor Color)> _typeCandidates;

        private RectTransform _typePreviewsContainer;
        private Button _typeConfirmButton;
        private readonly Dictionary<int, Image> _typeCellByIndex = new Dictionary<int, Image>();
        private readonly Dictionary<int, RectTransform> _typePreviewContainerByIndex = new Dictionary<int, RectTransform>();
        private int _selectedTypeIndex = -1;

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
        /// type for Retirer/Dupliquer/Recolorer, then (Recolorer only) the
        /// target color. <paramref name="def"/> must be a Bank-pool upgrade
        /// with <see cref="UpgradeDefinition.RequiresSubChoice"/> true (the
        /// shop never calls this for anything else). <paramref
        /// name="candidateTypes"/> (RunManager.PendingUpgradeTypeCandidates)
        /// is the up-to-5 subset of the deck's composition to actually offer
        /// — explicit request, the type picker used to list every distinct
        /// type in the deck at once.
        /// </summary>
        public void ShowForPendingUpgrade(UpgradeDefinition def, IReadOnlyList<(ShapeId Shape, PieceColor Color)> candidateTypes)
        {
            _typeCandidates = candidateTypes;
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
            const float cardTopY = -20f;
            const float gapBelowCard = 24f;
            _cardInstance.anchoredPosition = new Vector2(0f, cardTopY);
            // Measured, not guessed — UpgradeCardFactory.Build already
            // computed the card's real height (it varies with the
            // description's length), so everything below it is placed
            // relative to that instead of a fixed offset that would either
            // overlap a long description or leave a big gap under a short
            // one.
            _bodyTopY = cardTopY - _cardInstance.sizeDelta.y - gapBelowCard;

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

        private void ShowTypeChoice(UpgradeDefinition def)
        {
            ClearChildren();
            _selectedTypeIndex = -1;
            _typeCellByIndex.Clear();
            _typePreviewContainerByIndex.Clear();

            var title = UIFactory.CreateText(_root, "Title", "Choose a piece type", 22, UITheme.TextPrimary);
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
        /// One candidate in the type picker — just a shape/color preview
        /// (same look as a hand slot, see ShapePreviewFactory), no name/count
        /// label (explicit request). Click selects it exclusively (radio-
        /// button style, since exactly one type is ever needed here); the
        /// Confirm button — not this click — is what actually commits to it,
        /// on explicit request ("il faut un confirm au lieu d'un immediate
        /// effect").
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
            previewContainer.anchoredPosition = Vector2.zero;
            previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);
            _typePreviewContainerByIndex[index] = previewContainer;

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, cell.gameObject);
        }

        /// <summary>
        /// The trait carried by the first deck token matching (shape, color)
        /// that has one, or null if none of that type's copies are enchanted.
        /// Since Retirer/Dupliquer/Recolorer all operate on a TYPE rather than
        /// a specific token (see DeckManager.RemoveOneOfType and friends),
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
            var sub = new UpgradeSubChoice(shape, color);

            SetTypeCellsInteractable(false);
            _typeConfirmButton.interactable = false;

            if (def.Id == UpgradeId.RecolorPiece)
            {
                // Not a final commit yet — the target color is still needed,
                // so this only advances to that step, no animation.
                ShowColorChoice(shape, color);
                return;
            }
            if (def.Id == UpgradeId.RemovePiece)
            {
                StartCoroutine(FadeOutSelectedThenFinalize(_typePreviewContainerByIndex[_selectedTypeIndex], sub));
                return;
            }
            if (def.Id == UpgradeId.DuplicatePiece)
            {
                StartCoroutine(FadeInDuplicateThenFinalize(shape, color, sub));
                return;
            }
            FinalizeChoice(sub);
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

        /// <summary>Retirer: fades the chosen piece's preview out to visualize it leaving the deck, then resolves — explicit request.</summary>
        private IEnumerator FadeOutSelectedThenFinalize(RectTransform previewContainer, UpgradeSubChoice sub)
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
            FinalizeChoice(sub);
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

            var title = UIFactory.CreateText(_root, "Title", "Choose the target color", 22, UITheme.TextPrimary);
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
