using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Read-only, toggleable overlay listing the player's full persistent deck
    /// composition (one row per shape/color combo, with its count and a
    /// representative trait badge if any copy is enchanted). Reuses DraftView's
    /// row-building approach (see BuildTypeRow/FindRepresentativeTrait there)
    /// since it shares the same underlying data (DeckManager.GetDeckComposition).
    ///
    /// Grouped by color into its own labeled section per PieceColor, each a
    /// single horizontal line of compact type cards, manually positioned
    /// section-by-section (no LayoutGroup) so every color's cards stay
    /// centered under its heading.
    /// </summary>
    public sealed class DeckView : MonoBehaviour
    {
        private const float CardWidth = 135f;
        private const float CardHeight = 90f;
        private const float CardSpacing = 6.25f;
        private const float RowPreviewSize = 56.25f;
        private const float PreviewTileSize = 18.75f;
        private const float LevelLabelHeight = 20f;
        private const float SectionHeaderHeight = 27.5f;
        private const float SectionHeaderToGridGap = 5f;
        private const float SectionGap = 8f;
        private const float ListWidth = 1150f;
        private const float ListHeight = 700f;

        // One column per possible shape keeps all shapes of a color on a
        // single line (Joker uses the same bounded shape catalog).
        private static int ColumnsPerSection
        {
            get { return InitialDeckFactory.ShapeOrder.Length; }
        }

        private RunManager _run;
        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _listContainer;
        private Text _countLabel;

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        public RectTransform Build(Transform parent, RunManager run, TooltipView tooltip)
        {
            _run = run;
            _deck = run.Deck;
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "DeckOverlay", new Color(0f, 0f, 0f, 0.82f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Your Deck", 26, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            header.rectTransform.sizeDelta = new Vector2(900f, 40f);

            _countLabel = UIFactory.CreateText(_root, "Count", "", 18, UITheme.TextOnBackground);
            _countLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            _countLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            // Top-pivoted (not centered) since sections are positioned by a
            // running Y cursor in RebuildRows, top-down — each section's
            // height depends on how many distinct types that color has.
            _listContainer = UIFactory.CreateUIObject("List", _root);
            _listContainer.anchorMin = new Vector2(0.5f, 1f);
            _listContainer.anchorMax = new Vector2(0.5f, 1f);
            _listContainer.pivot = new Vector2(0.5f, 1f);
            _listContainer.anchoredPosition = new Vector2(0f, -110f);
            _listContainer.sizeDelta = new Vector2(ListWidth, ListHeight);

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Close", UISprites.CancelButtonBackground);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-30f, -30f);
            closeRect.sizeDelta = new Vector2(160f, 44f);
            closeBtn.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(RunManager run)
        {
            _run = run;
            _deck = run.Deck;
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
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(true);
            RebuildRows();
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(false);
        }

        private static readonly PieceColor[] ColorSectionOrder =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime, PieceColor.Joker
        };

        private void RebuildRows()
        {
            ClearChildren(_listContainer);
            _countLabel.text = _deck.DeckCount + " pieces";

            var composition = _deck.GetDeckComposition();
            var sections = new List<(PieceColor Color, List<(ShapeId Shape, int Count)> Types)>();
            int widestSection = 0;
            for (int c = 0; c < ColorSectionOrder.Length; c++)
            {
                var color = ColorSectionOrder[c];
                var types = CollectTypesForColor(composition, color);
                if (types.Count == 0)
                {
                    continue;
                }
                sections.Add((color, types));
                if (types.Count > widestSection)
                {
                    widestSection = types.Count;
                }
            }

            // Every section shares the same horizontal offset/width, based on whichever section has the most distinct types (capped at the number of possible shapes), so the whole block reads as centered rather than flush left.
            int columnsUsed = Mathf.Min(ColumnsPerSection, widestSection);
            float contentWidth = columnsUsed * CardWidth + (columnsUsed - 1) * CardSpacing;
            float xOffset = (ListWidth - contentWidth) / 2f;

            float y = 0f;
            for (int i = 0; i < sections.Count; i++)
            {
                int colorTotal = 0;
                var types = sections[i].Types;
                for (int t = 0; t < types.Count; t++)
                {
                    colorTotal += types[t].Count;
                }
                y = BuildColorSectionHeader(sections[i].Color, colorTotal, y, xOffset, contentWidth);
                y -= SectionHeaderToGridGap;
                y = BuildColorSectionGrid(types, sections[i].Color, y, xOffset);
                y -= SectionGap;
            }
        }

        /// <summary>Every (shape, count) the deck currently has in <paramref name="color"/>, in InitialDeckFactory.ShapeOrder's fixed order; GetDeckComposition's own Dictionary iteration order isn't guaranteed.</summary>
        private List<(ShapeId Shape, int Count)> CollectTypesForColor(IReadOnlyDictionary<(ShapeId Shape, PieceColor Color), int> composition, PieceColor color)
        {
            var result = new List<(ShapeId, int)>();
            var shapeOrder = InitialDeckFactory.ShapeOrder;
            for (int i = 0; i < shapeOrder.Length; i++)
            {
                if (composition.TryGetValue((shapeOrder[i], color), out int count))
                {
                    result.Add((shapeOrder[i], count));
                }
            }
            return result;
        }

        /// <summary>Section label tinted the color it groups, stating <paramref name="totalCount"/> (this color's piece total across every shape). Starts at <paramref name="xOffset"/> and spans <paramref name="contentWidth"/>, matching its grid's centered columns. Returns the Y cursor for whatever comes next.</summary>
        private float BuildColorSectionHeader(PieceColor color, int totalCount, float y, float xOffset, float contentWidth)
        {
            string headerText = VisualDefaults.GetColorName(color).ToUpperInvariant() + " (" + totalCount + ")";
            var label = UIFactory.CreateText(_listContainer, "Header_" + color, headerText, 19, VisualDefaults.GetColor(color), TextAnchor.LowerLeft);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(xOffset, y);
            label.rectTransform.sizeDelta = new Vector2(contentWidth, SectionHeaderHeight);
            return y - SectionHeaderHeight;
        }

        /// <summary>Places all shape cards in one centered horizontal row for this color, with each card aligned to the same columns used by the other sections. Returns the Y cursor for whatever comes next.</summary>
        private float BuildColorSectionGrid(List<(ShapeId Shape, int Count)> types, PieceColor color, float y, float xOffset)
        {
            for (int i = 0; i < types.Count; i++)
            {
                int col = i % ColumnsPerSection;
                int row = i / ColumnsPerSection;
                float x = xOffset + col * (CardWidth + CardSpacing);
                float cardY = y - row * (CardHeight + CardSpacing);
                BuildTypeCard(x, cardY, types[i].Shape, color, types[i].Count);
            }

            int rowCount = Mathf.CeilToInt(types.Count / (float)ColumnsPerSection);
            float gridHeight = rowCount * CardHeight + (rowCount - 1) * CardSpacing;
            return y - gridHeight;
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        /// <summary>Same look as DraftView.BuildTypeRow, minus the Button/onClick — this view is purely informational, never a picker. Positioned explicitly at (x, y) within _listContainer rather than through a LayoutGroup, since each color section wraps its own row count.</summary>
        private void BuildTypeCard(float x, float y, ShapeId shape, PieceColor color, int count)
        {
            var row = UIFactory.CreatePanel(_listContainer, "Type_" + shape + "_" + color, UITheme.ButtonIdle);
            row.rectTransform.anchorMin = new Vector2(0f, 1f);
            row.rectTransform.anchorMax = new Vector2(0f, 1f);
            row.rectTransform.pivot = new Vector2(0f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(x, y);
            row.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);

            var previewContainer = UIFactory.CreateUIObject("Preview", row.transform);
            previewContainer.anchorMin = new Vector2(0f, 0.5f);
            previewContainer.anchorMax = new Vector2(0f, 0.5f);
            previewContainer.pivot = new Vector2(0f, 0.5f);
            previewContainer.anchoredPosition = new Vector2(9.375f, 11.875f);
            previewContainer.sizeDelta = new Vector2(RowPreviewSize, RowPreviewSize);

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, row.gameObject, PreviewTileSize);

            int level = _run.GetColorMasteryLevel(color) + _run.GetShapeMasteryLevel(shape) - 1;
            var levelLabel = UIFactory.CreateText(row.transform, "Level", "Lv. " + level, 14, Color.black);
            levelLabel.raycastTarget = false;
            levelLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            levelLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            levelLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            levelLabel.rectTransform.anchoredPosition = new Vector2(0f, -28.125f);
            levelLabel.rectTransform.sizeDelta = new Vector2(52.5f, LevelLabelHeight);

            var countLabel = UIFactory.CreateText(row.transform, "Count", "x" + count, 18, UITheme.TextPrimary);
            countLabel.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            countLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            // Pull the count inward so it stays visually close to the piece
            // instead of scaling up the old empty gap along with the card.
            countLabel.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
            countLabel.rectTransform.sizeDelta = new Vector2(50f, 32.5f);
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
