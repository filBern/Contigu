using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round draft overlay: the player picks exactly one Bank (tile)
    /// upgrade from 3 AND one Grid upgrade from 3 — two independent picks,
    /// either one first — walking through a sub-choice flow first for upgrades
    /// that need one (Retirer/Dupliquer/Recolorer a piece type).
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        private const float CardWidth = 200f;
        private const float CardHeight = 210f;

        /// <summary>Fires once both a tile and a grid upgrade have been chosen and resolved.</summary>
        public event Action<UpgradeDefinition, UpgradeSubChoice, UpgradeDefinition, UpgradeSubChoice> DraftConfirmed;

        private DeckManager _deck;
        private RectTransform _root;

        private Text _tileSectionLabel;
        private RectTransform _tileCardsContainer;
        private Text _gridSectionLabel;
        private RectTransform _gridCardsContainer;

        private RectTransform _subChoiceRoot;

        private UpgradeDraft _currentDraft;
        private bool _tileChosen;
        private UpgradeDefinition _chosenTileUpgrade;
        private UpgradeSubChoice _chosenTileSubChoice;
        private bool _gridChosen;
        private UpgradeDefinition _chosenGridUpgrade;
        private UpgradeSubChoice _chosenGridSubChoice;

        /// <summary>Whether the in-progress sub-choice flow belongs to the tile pick (vs. the grid pick).</summary>
        private bool _subChoiceIsForTile;

        public RectTransform Build(Transform parent, DeckManager deck)
        {
            _deck = deck;

            var overlay = UIFactory.CreatePanel(parent, "DraftOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Manche terminée !", 26, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            header.rectTransform.sizeDelta = new Vector2(900f, 40f);

            _tileSectionLabel = UIFactory.CreateText(_root, "TileSectionLabel", "", 18, UITheme.TextPrimary);
            _tileSectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _tileSectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _tileSectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _tileSectionLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _tileSectionLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _tileCardsContainer = BuildCardRow("TileCards", -115f);

            _gridSectionLabel = UIFactory.CreateText(_root, "GridSectionLabel", "", 18, UITheme.TextPrimary);
            _gridSectionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _gridSectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _gridSectionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _gridSectionLabel.rectTransform.anchoredPosition = new Vector2(0f, -350f);
            _gridSectionLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _gridCardsContainer = BuildCardRow("GridCards", -385f);

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
            _tileChosen = false;
            _gridChosen = false;
            _root.gameObject.SetActive(true);
            HideSubChoice();
            RebuildCards();
        }

        private void RebuildCards()
        {
            ClearChildren(_tileCardsContainer);
            ClearChildren(_gridCardsContainer);

            _tileCardsContainer.gameObject.SetActive(!_tileChosen);
            _tileSectionLabel.text = _tileChosen
                ? "Pièce : " + _chosenTileUpgrade.Name + "  ✓"
                : "Choisissez une amélioration de pièce (1 parmi 3)";
            if (!_tileChosen)
            {
                for (int i = 0; i < _currentDraft.TileOptions.Length; i++)
                {
                    BuildCard(_tileCardsContainer, _currentDraft.TileOptions[i], true);
                }
            }

            _gridCardsContainer.gameObject.SetActive(!_gridChosen);
            _gridSectionLabel.text = _gridChosen
                ? "Grille : " + _chosenGridUpgrade.Name + "  ✓"
                : "Choisissez une amélioration de grille (1 parmi 3)";
            if (!_gridChosen)
            {
                for (int i = 0; i < _currentDraft.GridOptions.Length; i++)
                {
                    BuildCard(_gridCardsContainer, _currentDraft.GridOptions[i], false);
                }
            }
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void BuildCard(RectTransform parent, UpgradeDefinition def, bool isTile)
        {
            var card = UIFactory.CreatePanel(parent, "Card_" + def.Id, UITheme.PanelLight);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent HorizontalLayoutGroup collapses it toward zero.
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;

            var nameLabel = UIFactory.CreateText(card.transform, "Name", def.Name, 16, UITheme.TextPrimary);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            nameLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 44f);

            var descLabel = UIFactory.CreateText(card.transform, "Desc", def.Description, 12, UITheme.TextMuted);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            descLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 100f);

            var chooseBtn = UIFactory.CreateButton(card.transform, "Choose", "Choisir", UITheme.ButtonSelected, 14);
            var chooseRect = chooseBtn.GetComponent<RectTransform>();
            chooseRect.anchorMin = new Vector2(0.5f, 0f);
            chooseRect.anchorMax = new Vector2(0.5f, 0f);
            chooseRect.pivot = new Vector2(0.5f, 0f);
            chooseRect.anchoredPosition = new Vector2(0f, 14f);
            chooseRect.sizeDelta = new Vector2(CardWidth - 30f, 38f);
            chooseBtn.onClick.AddListener(() => OnChooseClicked(def, isTile));
        }

        private void OnChooseClicked(UpgradeDefinition def, bool isTile)
        {
            if (!def.RequiresSubChoice)
            {
                FinalizeChoice(def, default(UpgradeSubChoice), isTile);
                return;
            }
            ShowTypeChoice(def, isTile);
        }

        private void FinalizeChoice(UpgradeDefinition def, UpgradeSubChoice sub, bool isTile)
        {
            if (isTile)
            {
                _chosenTileUpgrade = def;
                _chosenTileSubChoice = sub;
                _tileChosen = true;
            }
            else
            {
                _chosenGridUpgrade = def;
                _chosenGridSubChoice = sub;
                _gridChosen = true;
            }

            HideSubChoice();

            if (_tileChosen && _gridChosen)
            {
                _root.gameObject.SetActive(false);
                if (DraftConfirmed != null)
                {
                    DraftConfirmed(_chosenTileUpgrade, _chosenTileSubChoice, _chosenGridUpgrade, _chosenGridSubChoice);
                }
                return;
            }

            RebuildCards();
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

        private void ShowTypeChoice(UpgradeDefinition def, bool isTile)
        {
            _subChoiceIsForTile = isTile;
            EnsureSubChoiceRoot();
            ClearSubChoiceChildren();
            _subChoiceRoot.gameObject.SetActive(true);

            var title = UIFactory.CreateText(_subChoiceRoot, "Title", "Choisissez un type de pièce", 22, UITheme.TextPrimary);
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
            listContainer.sizeDelta = new Vector2(700f, 440f);
            var grid = listContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 46f);
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
                string label = VisualDefaults.GetShapeName(shape) + " / " + VisualDefaults.GetColorName(color) + "  x" + count;
                var btn = UIFactory.CreateButton(listContainer, "Type", label, UITheme.ButtonIdle, 13);
                btn.onClick.AddListener(() => OnTypeChosen(def, shape, color));
            }

            var cancelBtn = UIFactory.CreateButton(_subChoiceRoot, "Cancel", "Annuler", UITheme.Danger);
            var cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(0f, 30f);
            cancelRect.sizeDelta = new Vector2(160f, 44f);
            cancelBtn.onClick.AddListener(HideSubChoice);
        }

        private void OnTypeChosen(UpgradeDefinition def, ShapeId shape, PieceColor color)
        {
            if (def.Id == UpgradeId.RecolorPiece)
            {
                ShowColorChoice(def, shape, color);
                return;
            }
            FinalizeChoice(def, new UpgradeSubChoice(shape, color), _subChoiceIsForTile);
        }

        private void ShowColorChoice(UpgradeDefinition def, ShapeId shape, PieceColor fromColor)
        {
            ClearSubChoiceChildren();

            var title = UIFactory.CreateText(_subChoiceRoot, "Title", "Choisissez la couleur cible", 22, UITheme.TextPrimary);
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
                btn.onClick.AddListener(() => FinalizeChoice(def, new UpgradeSubChoice(shape, fromColor, targetColor), _subChoiceIsForTile));
            }

            var cancelBtn = UIFactory.CreateButton(_subChoiceRoot, "Cancel", "Annuler", UITheme.Danger);
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
