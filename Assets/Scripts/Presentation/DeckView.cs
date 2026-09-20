using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Read-only, toggleable overlay listing the player's full persistent deck
    /// composition (one row per shape/color combo, with its count and a
    /// representative trait badge if any copy is enchanted) — lets the player
    /// check what's in their deck without having to wait for the next draft
    /// (on explicit request). Reuses DraftView's exact row-building approach
    /// (see BuildTypeRow/FindRepresentativeTrait there) since it's the same
    /// underlying data (DeckManager.GetDeckComposition), just shown without
    /// the pick/sub-choice flow around it.
    /// </summary>
    public sealed class DeckView : MonoBehaviour
    {
        private const float RowHeight = 56f;
        private const float RowPreviewSize = 44f;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _listContainer;
        private Text _countLabel;

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        public RectTransform Build(Transform parent, DeckManager deck, TooltipView tooltip)
        {
            _deck = deck;
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "DeckOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Your Deck", 26, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            header.rectTransform.sizeDelta = new Vector2(900f, 40f);

            _countLabel = UIFactory.CreateText(_root, "Count", "", 18, UITheme.TextPrimary);
            _countLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            _countLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _listContainer = UIFactory.CreateUIObject("List", _root);
            _listContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _listContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _listContainer.pivot = new Vector2(0.5f, 0.5f);
            _listContainer.anchoredPosition = new Vector2(0f, -30f);
            _listContainer.sizeDelta = new Vector2(920f, 540f);
            var grid = _listContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300f, RowHeight);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.UpperCenter;

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Close", UISprites.CancelButtonBackground);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(160f, 44f);
            closeBtn.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
            RebuildRows();
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        private void RebuildRows()
        {
            ClearChildren(_listContainer);
            _countLabel.text = _deck.DeckCount + " pieces";
            foreach (var kvp in _deck.GetDeckComposition())
            {
                BuildTypeRow(_listContainer, kvp.Key.Shape, kvp.Key.Color, kvp.Value);
            }
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        /// <summary>Same look as DraftView.BuildTypeRow, minus the Button/onClick — this view is purely informational, never a picker.</summary>
        private void BuildTypeRow(RectTransform parent, ShapeId shape, PieceColor color, int count)
        {
            var row = UIFactory.CreatePanel(parent, "Type_" + shape + "_" + color, UITheme.ButtonIdle);

            var previewContainer = UIFactory.CreateUIObject("Preview", row.transform);
            previewContainer.anchorMin = new Vector2(0f, 0.5f);
            previewContainer.anchorMax = new Vector2(0f, 0.5f);
            previewContainer.pivot = new Vector2(0f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(8f, 0f);
            previewContainer.sizeDelta = new Vector2(RowPreviewSize, RowPreviewSize);

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, row.gameObject);

            var countLabel = UIFactory.CreateText(row.transform, "Count", "x" + count, 15, UITheme.TextPrimary);
            countLabel.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            countLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            countLabel.rectTransform.sizeDelta = new Vector2(44f, 30f);
        }

        /// <summary>Same representative-sample approach as DraftView.FindRepresentativeTrait — see there for why a per-type row can't show more than one sample trait.</summary>
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
    }
}
