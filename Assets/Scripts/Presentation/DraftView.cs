using System;
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
    /// </summary>
    public sealed class DraftView : MonoBehaviour
    {
        private const float TypeRowHeight = 56f;
        private const float TypeRowPreviewSize = 44f;

        /// <summary>Fires once the sub-choice has been made and the upgrade should be resolved.</summary>
        public event Action<UpgradeSubChoice> SubChoiceConfirmed;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;

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
        /// shop never calls this for anything else).
        /// </summary>
        public void ShowForPendingUpgrade(UpgradeDefinition def)
        {
            _root.gameObject.SetActive(true);
            ClearChildren();
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
                Destroy(_root.GetChild(i).gameObject);
            }
        }

        private void ShowTypeChoice(UpgradeDefinition def)
        {
            ClearChildren();

            var title = UIFactory.CreateText(_root, "Title", "Choose a piece type", 22, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            var listContainer = UIFactory.CreateUIObject("List", _root);
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
                ShowColorChoice(shape, color);
                return;
            }
            FinalizeChoice(new UpgradeSubChoice(shape, color));
        }

        private void ShowColorChoice(ShapeId shape, PieceColor fromColor)
        {
            ClearChildren();

            var title = UIFactory.CreateText(_root, "Title", "Choose the target color", 22, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.rectTransform.sizeDelta = new Vector2(600f, 40f);

            var listContainer = UIFactory.CreateUIObject("Colors", _root);
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
