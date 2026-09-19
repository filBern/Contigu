using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round draft overlay: the player picks exactly one upgrade from 3
    /// mixed Bank/Grid options (spec 5.2), walking through a sub-choice flow
    /// first for upgrades that need one (Retirer/Dupliquer/Recolorer a piece
    /// type).
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        private const float CardWidth = 200f;
        private const float CardHeight = 234f;
        private const float TypeRowHeight = 56f;
        private const float TypeRowPreviewSize = 44f;

        /// <summary>Fires once the upgrade has been chosen and resolved.</summary>
        public event Action<UpgradeDefinition, UpgradeSubChoice> UpgradeConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;

        private Text _sectionLabel;
        private RectTransform _cardsContainer;

        private RectTransform _subChoiceRoot;

        private UpgradeDraft _currentDraft;

        public RectTransform Build(Transform parent, DeckManager deck, TooltipView tooltip)
        {
            _deck = deck;
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "DraftOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Round complete!", 26, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            header.rectTransform.sizeDelta = new Vector2(900f, 40f);

            _sectionLabel = UIFactory.CreateText(_root, "SectionLabel", "Choose an upgrade (1 of 3)", 18, UITheme.TextPrimary);
            _sectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _sectionLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _sectionLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _cardsContainer = BuildCardRow("Cards", -115f);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private RectTransform BuildCardRow(string name, float topOffset)
        {
            var container = UIFactory.CreateUIObject(name, _root);
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = new Vector2(0f, topOffset);

            var layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return container;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        public void Show(UpgradeDraft draft)
        {
            _currentDraft = draft;
            _root.gameObject.SetActive(true);
            HideSubChoice();
            RebuildCards();
        }

        private void RebuildCards()
        {
            ClearChildren(_cardsContainer);
            _sectionLabel.text = "Choose an upgrade (1 of 3)";
            for (int i = 0; i < _currentDraft.Options.Length; i++)
            {
                BuildCard(_cardsContainer, _currentDraft.Options[i]);
            }
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void BuildCard(RectTransform parent, UpgradeDefinition def)
        {
            var card = UIFactory.CreateSlicedImage(parent, "Card_" + def.Id, UISprites.UpgradeCardBackground);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent HorizontalLayoutGroup collapses it toward zero.
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;

            var nameBanner = UIFactory.CreateSlicedImage(card.transform, "NameBanner", UISprites.UpgradeNameBanner);
            nameBanner.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            nameBanner.rectTransform.sizeDelta = new Vector2(CardWidth - 12f, 40f);

            var nameLabel = UIFactory.CreateText(nameBanner.transform, "Name", def.Name, 16, UITheme.TextPrimary);
            nameLabel.raycastTarget = false;
            UIFactory.StretchFull(nameLabel.rectTransform);

            // Rarity + pool ("type"), on explicit request — a small colored
            // subtitle line between the name and description, same idea as
            // TooltipView's optional subtitle for a tile trait's badge.
            var rarityLabel = UIFactory.CreateText(card.transform, "Rarity",
                UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool),
                12, UpgradeVisualDefaults.GetRarityColor(def.Rarity));
            rarityLabel.fontStyle = FontStyle.Italic;
            rarityLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            rarityLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 16f);

            var descLabel = UIFactory.CreateText(card.transform, "Desc", def.Description, 14, UITheme.TextMuted);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            descLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 96f);

            var chooseBtn = UIFactory.CreateButton(card.transform, "Choose", "Choose", UISprites.ChooseButtonBackground, 14);
            var chooseRect = chooseBtn.GetComponent<RectTransform>();
            chooseRect.anchorMin = new Vector2(0.5f, 0f);
            chooseRect.anchorMax = new Vector2(0.5f, 0f);
            chooseRect.pivot = new Vector2(0.5f, 0f);
            chooseRect.anchoredPosition = new Vector2(0f, 14f);
            chooseRect.sizeDelta = new Vector2(CardWidth - 30f, 38f);
            chooseBtn.onClick.AddListener(() => OnChooseClicked(def));
        }

        private void OnChooseClicked(UpgradeDefinition def)
        {
            if (!def.RequiresSubChoice)
            {
                FinalizeChoice(def, default(UpgradeSubChoice));
                return;
            }
            ShowTypeChoice(def);
        }

        private void FinalizeChoice(UpgradeDefinition def, UpgradeSubChoice sub)
        {
            HideSubChoice();
            _root.gameObject.SetActive(false);
            if (UpgradeConfirmed != null)
            {
                UpgradeConfirmed(def, sub);
            }
        }

        private void EnsureSubChoiceRoot()
        {
            if (_subChoiceRoot != null)
            {
                return;
            }
            var overlay = UIFactory.CreatePanel(_root, "SubChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _subChoiceRoot = overlay.rectTransform;
            UIFactory.StretchFull(_subChoiceRoot);
            _subChoiceRoot.gameObject.SetActive(false);
        }

        private void HideSubChoice()
        {
            if (_subChoiceRoot != null)
            {
                _subChoiceRoot.gameObject.SetActive(false);
            }
        }

        private void ClearSubChoiceChildren()
        {
            for (int i = _subChoiceRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_subChoiceRoot.GetChild(i).gameObject);
            }
        }

        private void ShowTypeChoice(UpgradeDefinition def)
        {
            EnsureSubChoiceRoot();
            ClearSubChoiceChildren();
            _subChoiceRoot.gameObject.SetActive(true);

            var title = UIFactory.CreateText(_subChoiceRoot, "Title", "Choose a piece type", 22, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            var listContainer = UIFactory.CreateUIObject("List", _subChoiceRoot);
            listContainer.anchorMin = new Vector2(0.5f, 0.5f);
            listContainer.anchorMax = new Vector2(0.5f, 0.5f);
            listContainer.pivot = new Vector2(0.5f, 0.5f);
            listContainer.anchoredPosition = new Vector2(0f, 0f);
            listContainer.sizeDelta = new Vector2(700f, 480f);
            var grid = listContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, TypeRowHeight);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.UpperCenter;

            bool removeMode = def.Id == UpgradeId.RemovePiece;
            var composition = _deck.GetDeckComposition();
            foreach (var kvp in composition)
            {
                var shape = kvp.Key.Shape;
                var color = kvp.Key.Color;
                int count = kvp.Value;
                if (removeMode && !_deck.CanRemove(shape, color))
                {
                    continue;
                }
                BuildTypeRow(listContainer, def, shape, color, count);
            }

            var cancelBtn = UIFactory.CreateButton(_subChoiceRoot, "Cancel", "Cancel", UISprites.CancelButtonBackground);
            var cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(0f, 30f);
            cancelRect.sizeDelta = new Vector2(160f, 44f);
            cancelBtn.onClick.AddListener(HideSubChoice);
        }

        /// <summary>
        /// One row in the type picker: a shape/color preview (same look as a
        /// hand slot, see ShapePreviewFactory) instead of a plain "Shape /
        /// Color" text label — clearer at a glance, and it doubles as a way to
        /// show whether any copy of this type is currently enchanted (see
        /// FindRepresentativeTrait), which a text label couldn't convey at all.
        /// </summary>
        private void BuildTypeRow(RectTransform parent, UpgradeDefinition def, ShapeId shape, PieceColor color, int count)
        {
            var row = UIFactory.CreatePanel(parent, "Type_" + shape + "_" + color, UITheme.ButtonIdle);
            var rowBtn = row.gameObject.AddComponent<Button>();
            rowBtn.onClick.AddListener(() => OnTypeChosen(def, shape, color));

            var previewContainer = UIFactory.CreateUIObject("Preview", row.transform);
            previewContainer.anchorMin = new Vector2(0f, 0.5f);
            previewContainer.anchorMax = new Vector2(0f, 0.5f);
            previewContainer.pivot = new Vector2(0f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(8f, 0f);
            previewContainer.sizeDelta = new Vector2(TypeRowPreviewSize, TypeRowPreviewSize);

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, row.gameObject);

            var countLabel = UIFactory.CreateText(row.transform, "Count", "x" + count, 15, UITheme.TextPrimary);
            countLabel.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            countLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            countLabel.rectTransform.sizeDelta = new Vector2(44f, 30f);
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

        private void OnTypeChosen(UpgradeDefinition def, ShapeId shape, PieceColor color)
        {
            if (def.Id == UpgradeId.RecolorPiece)
            {
                ShowColorChoice(def, shape, color);
                return;
            }
            FinalizeChoice(def, new UpgradeSubChoice(shape, color));
        }

        private void ShowColorChoice(UpgradeDefinition def, ShapeId shape, PieceColor fromColor)
        {
            ClearSubChoiceChildren();

            var title = UIFactory.CreateText(_subChoiceRoot, "Title", "Choose the target color", 22, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            var listContainer = UIFactory.CreateUIObject("Colors", _subChoiceRoot);
            listContainer.anchorMin = new Vector2(0.5f, 0.5f);
            listContainer.anchorMax = new Vector2(0.5f, 0.5f);
            listContainer.pivot = new Vector2(0.5f, 0.5f);
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
                btn.onClick.AddListener(() => FinalizeChoice(def, new UpgradeSubChoice(shape, fromColor, targetColor)));
            }

            var cancelBtn = UIFactory.CreateButton(_subChoiceRoot, "Cancel", "Cancel", UISprites.CancelButtonBackground);
            var cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(0f, 30f);
            cancelRect.sizeDelta = new Vector2(160f, 44f);
            cancelBtn.onClick.AddListener(HideSubChoice);
        }
    }
}
