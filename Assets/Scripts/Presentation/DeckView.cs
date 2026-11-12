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
    /// representative trait badge if any copy is enchanted). Shares
    /// its row-building approach with the shop's seed-type choices (see BuildTypeRow/FindRepresentativeTrait
    /// below) because it uses the same underlying data (DeckManager.GetDeckComposition).
    ///
    /// Grouped by color into its own labeled section per PieceColor, each a
    /// single horizontal line of compact type cards, manually positioned
    /// section-by-section (no LayoutGroup) so every color's cards stay
    /// centered under its heading.
    /// </summary>
    public sealed class DeckView : MonoBehaviour
    {
        private const float MaxCardWidth = 138f;
        private const float MaxCardHeight = 128f;
        private const float MinCardHeight = 64f;
        private const float ListTop = 110f;
        private const float BottomMargin = 14f;
        private const float CardSpacing = 6.25f;
        private const float PreviewTileSize = 27f;
        private const float FooterHeight = 30f;
        private const float SectionHeaderHeight = 28f;
        private const float SectionHeaderToGridGap = 5f;
        private const float SectionGap = 6f;
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
        private float _cardHeight = MaxCardHeight;
        private float _cardWidth = MaxCardWidth;
        private float _listWidth = ListWidth;

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        public RectTransform Build(Transform parent, RunManager run, TooltipView tooltip)
        {
            _run = run;
            _deck = run.Deck;
            _tooltip = tooltip;

            // The shop's back wall (the same dark planks), so the seed bag reads as part of the same stall.
            var overlay = UIFactory.CreatePanel(parent, "DeckOverlay", new Color(0.17f, 0.115f, 0.085f, 1f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);
            StallBackdrop.AddPlanks(_root);

            // The title as a plaque (a plot, like the shop's), the seed count under it.
            var header = ShopSign.BuildPlaque(_root, "HeaderPlaque", "Your Seed Bag", 26, "Wood");
            header.anchorMin = new Vector2(0.5f, 1f);
            header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -22f);
            header.sizeDelta = new Vector2(360f, 48f);

            _countLabel = UIFactory.CreateText(_root, "Count", "", 22, new Color(0.96f, 0.89f, 0.75f, 1f));
            _countLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchoredPosition = new Vector2(0f, -78f);
            _countLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            // Top-pivoted (not centered) since sections are positioned by a
            // running Y cursor in RebuildRows, top-down — each section's
            // height depends on how many distinct types that color has.
            _listContainer = UIFactory.CreateUIObject("List", _root);
            _listContainer.anchorMin = new Vector2(0.5f, 1f);
            _listContainer.anchorMax = new Vector2(0.5f, 1f);
            _listContainer.pivot = new Vector2(0.5f, 1f);
            _listContainer.anchoredPosition = new Vector2(0f, -ListTop);
            _listContainer.sizeDelta = new Vector2(ListWidth, ListHeight);

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Close", UISprites.CancelButtonBackground);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-30f, -30f);
            closeRect.sizeDelta = new Vector2(160f, 44f);
            closeBtn.onClick.AddListener(Hide);
            UIFactory.KeepOnTop(closeBtn);

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
            // Full-screen and opaque: nothing else (lifted shop panels included) may show through or above it.
            _root.SetAsLastSibling();
            OverlayLift.SetPanelsHidden(true);
            Canvas.ForceUpdateCanvases();
            RebuildRows();
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(false);
            OverlayLift.SetPanelsHidden(false);
        }

        private static readonly PieceColor[] ColorSectionOrder =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime, PieceColor.Joker
        };

        private void RebuildRows()
        {
            ClearChildren(_listContainer);
            _countLabel.text = _deck.DeckCount + " seeds";

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

            // Cards grow to fill the free height: fewer colors, taller cards.
            // The overlay is as big as the screen's canvas, which is not always the reference size (a wide window crops the height): fit to what is really there.
            var rootRect = _root.rect;
            float availableHeight = rootRect.height > 100f ? rootRect.height - ListTop - BottomMargin : ListHeight;
            float availableWidth = rootRect.width > 100f ? Mathf.Min(ListWidth, rootRect.width - 24f) : ListWidth;
            float perSection = availableHeight / Mathf.Max(1, sections.Count) - SectionHeaderHeight - SectionHeaderToGridGap - SectionGap;
            _cardHeight = Mathf.Clamp(perSection, MinCardHeight, MaxCardHeight);
            _cardWidth = Mathf.Min(MaxCardWidth, (availableWidth - (ColumnsPerSection - 1) * CardSpacing) / ColumnsPerSection);
            _listWidth = availableWidth;
            _listContainer.sizeDelta = new Vector2(_listWidth, availableHeight);

            // Every section shares the same horizontal offset/width, based on whichever section has the most distinct types (capped at the number of possible shapes), so the whole block reads as centered rather than flush left.
            int columnsUsed = Mathf.Min(ColumnsPerSection, widestSection);
            float contentWidth = columnsUsed * _cardWidth + (columnsUsed - 1) * CardSpacing;
            float xOffset = (_listWidth - contentWidth) / 2f;

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
            var label = UIFactory.CreateText(_listContainer, "Header_" + color, headerText, 24, VisualDefaults.GetColor(color), TextAnchor.LowerLeft);
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
                float x = xOffset + col * (_cardWidth + CardSpacing);
                float cardY = y - row * (_cardHeight + CardSpacing);
                BuildTypeCard(x, cardY, types[i].Shape, color, types[i].Count);
            }

            int rowCount = Mathf.CeilToInt(types.Count / (float)ColumnsPerSection);
            float gridHeight = rowCount * _cardHeight + (rowCount - 1) * CardSpacing;
            return y - gridHeight;
        }

        private static void ClearChildren(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        /// <summary>Same look as the shop seed-type choice, minus the Button/onClick — this view is purely informational, never a picker. Positioned explicitly at (x, y) within _listContainer rather than through a LayoutGroup, since each color section wraps its own row count.</summary>
        private void BuildTypeCard(float x, float y, ShapeId shape, PieceColor color, int count)
        {
            // A planter in the seed's own color (red / blue / gold / green bed around pale soil; a wooden one for the Joker).
            var row = UIFactory.CreateSlicedImage(_listContainer, "Type_" + shape + "_" + color, UISprites.Garden(PlanterFor(color)) ?? UISprites.CardBackground);
            row.color = Color.white;
            UIFactory.AddDropShadow(row, 5f, 0.4f);
            row.rectTransform.anchorMin = new Vector2(0f, 1f);
            row.rectTransform.anchorMax = new Vector2(0f, 1f);
            row.rectTransform.pivot = new Vector2(0f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(x, y);
            row.rectTransform.sizeDelta = new Vector2(_cardWidth, _cardHeight);

            // The piece sits centered above the footer line (level on the left, count on the right).
            var previewContainer = UIFactory.CreateUIObject("Preview", row.transform);
            previewContainer.anchorMin = previewContainer.anchorMax = new Vector2(0.5f, 1f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            float previewAreaHeight = _cardHeight - FooterHeight - 14f;
            previewContainer.anchoredPosition = new Vector2(0f, -(10f + previewAreaHeight / 2f));
            previewContainer.sizeDelta = new Vector2(_cardWidth - 20f, previewAreaHeight);

            var trait = FindRepresentativeTrait(shape, color);
            // The tile shrinks just enough for the tallest shapes to fit above the footer (many colors = shorter cards).
            var shapeData = PieceShapeCatalog.Get(shape);
            float tile = Mathf.Min(PreviewTileSize, (previewAreaHeight - 6f) / Mathf.Max(1, ShapePreviewFactory.RowsOf(shapeData)));
            ShapePreviewFactory.BuildWithTraits(previewContainer, shapeData, color, trait, _tooltip, row.gameObject, tile);

            int level = _run.GetColorMasteryLevel(color) + _run.GetShapeMasteryLevel(shape) - 1;
            var levelLabel = UIFactory.CreateText(row.transform, "Level", "Lv. " + level, 17, UITheme.TextMuted, TextAnchor.MiddleLeft);
            levelLabel.raycastTarget = false;
            levelLabel.rectTransform.anchorMin = levelLabel.rectTransform.anchorMax = new Vector2(0f, 0f);
            levelLabel.rectTransform.pivot = new Vector2(0f, 0f);
            levelLabel.rectTransform.anchoredPosition = new Vector2(16f, 8f);
            levelLabel.rectTransform.sizeDelta = new Vector2(70f, FooterHeight);

            var countLabel = UIFactory.CreateText(row.transform, "Count", "x" + count, 26, UITheme.TextPrimary, TextAnchor.MiddleRight);
            countLabel.raycastTarget = false;
            countLabel.rectTransform.anchorMin = countLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            countLabel.rectTransform.pivot = new Vector2(1f, 0f);
            countLabel.rectTransform.anchoredPosition = new Vector2(-16f, 7f);
            countLabel.rectTransform.sizeDelta = new Vector2(60f, FooterHeight + 2f);
        }

        /// <summary>The planter sprite name for a seed color: the bed takes the flower's color.</summary>
        private static string PlanterFor(PieceColor color)
        {
            switch (color)
            {
                case PieceColor.Coral: return "RedLight";
                case PieceColor.Teal: return "BlueLight";
                case PieceColor.Violet: return "GoldLight";
                case PieceColor.Lime: return "GreenLight";
                default: return "WoodLight";
            }
        }

        /// <summary>Same representative-sample approach as GameBootstrap.RepresentativeTraits — see there for why a per-type row can't show more than one sample trait.</summary>
        private IReadOnlyList<PieceTrait> FindRepresentativeTrait(ShapeId shape, PieceColor color)
        {
            // The most upgraded copy of this type stands for it.
            var tokens = _deck.Deck;
            IReadOnlyList<PieceTrait> best = null;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Matches(shape, color) && tokens[i].TraitCount > (best == null ? 0 : best.Count))
                {
                    best = tokens[i].Traits;
                }
            }
            return best;
        }
    }
}
