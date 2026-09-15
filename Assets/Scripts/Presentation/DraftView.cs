using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Between-round draft overlay: shows the 3 offered upgrades (spec 5.2) and,
    /// for upgrades that need it, walks the player through picking a piece type
    /// (and target color for Recolorer) before confirming (spec 5.3).
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        public event Action<UpgradeDefinition, UpgradeSubChoice> UpgradeConfirmed;

        private DeckManager _deck;
        private RectTransform _root;
        private RectTransform _cardsContainer;
        private RectTransform _subChoiceRoot;

        public RectTransform Build(Transform parent, DeckManager deck)
        {
            _deck = deck;

            var overlay = UIFactory.CreatePanel(parent, "DraftOverlay", new Color(0f, 0f, 0f, 0.78f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "Choisissez une amélioration", 28, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            title.rectTransform.sizeDelta = new Vector2(900f, 50f);

            _cardsContainer = UIFactory.CreateUIObject("Cards", _root);
            _cardsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _cardsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _cardsContainer.pivot = new Vector2(0.5f, 0.5f);
            _cardsContainer.anchoredPosition = Vector2.zero;
            var hLayout = _cardsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 24f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            var cardsFitter = _cardsContainer.gameObject.AddComponent<ContentSizeFitter>();
            cardsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        public void Show(UpgradeDraft draft)
        {
            _root.gameObject.SetActive(true);
            HideSubChoice();

            for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsContainer.GetChild(i).gameObject);
            }

            for (int i = 0; i < draft.Options.Length; i++)
            {
                BuildCard(draft.Options[i]);
            }
        }

        private void BuildCard(UpgradeDefinition def)
        {
            var card = UIFactory.CreatePanel(_cardsContainer, "Card_" + def.Id, UITheme.PanelLight);
            card.rectTransform.sizeDelta = new Vector2(260f, 320f);

            var poolLabel = UIFactory.CreateText(card.transform, "Pool", def.Pool.ToString(), 13, UITheme.TextMuted);
            poolLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            poolLabel.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            poolLabel.rectTransform.sizeDelta = new Vector2(220f, 24f);

            var nameLabel = UIFactory.CreateText(card.transform, "Name", def.Name, 20, UITheme.TextPrimary);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -46f);
            nameLabel.rectTransform.sizeDelta = new Vector2(230f, 50f);

            var descLabel = UIFactory.CreateText(card.transform, "Desc", def.Description, 14, UITheme.TextMuted);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -110f);
            descLabel.rectTransform.sizeDelta = new Vector2(230f, 130f);

            var chooseBtn = UIFactory.CreateButton(card.transform, "Choose", "Choisir", UITheme.ButtonSelected);
            var chooseRect = chooseBtn.GetComponent<RectTransform>();
            chooseRect.anchorMin = new Vector2(0.5f, 0f);
            chooseRect.anchorMax = new Vector2(0.5f, 0f);
            chooseRect.pivot = new Vector2(0.5f, 0f);
            chooseRect.anchoredPosition = new Vector2(0f, 20f);
            chooseRect.sizeDelta = new Vector2(180f, 44f);
            chooseBtn.onClick.AddListener(() => OnChooseClicked(def));
        }

        private void OnChooseClicked(UpgradeDefinition def)
        {
            if (!def.RequiresSubChoice)
            {
                Confirm(def, default(UpgradeSubChoice));
                return;
            }
            ShowTypeChoice(def);
        }

        private void Confirm(UpgradeDefinition def, UpgradeSubChoice sub)
        {
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
            Confirm(def, new UpgradeSubChoice(shape, color));
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
                btn.onClick.AddListener(() => Confirm(def, new UpgradeSubChoice(shape, fromColor, targetColor)));
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
