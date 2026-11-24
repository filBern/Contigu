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
        private const float MaxCardWidth = 250f;
        private const float MaxCardHeight = 200f;
        private const float MinCardHeight = 64f;
        private const float ListTop = 188f;
        private const float BottomMargin = 14f;
        private const float CardSpacing = 14f;
        private const float PreviewTileSize = 46f;
        private const float FooterHeight = 30f;
        private const float SectionHeaderHeight = 28f;
        private const float SectionHeaderToGridGap = 5f;
        private const float SectionGap = 6f;
        private const float ListWidth = 1150f;
        private const float ListHeight = 700f;

        // One tab per color present in the bag (Poppy, Iris, ..., Joker): a single color's cards at a time, big enough to read, instead of every color crammed on one screen.
        private const int CardColumns = 4;
        private PieceColor? _selectedColor;
        private RectTransform _tabsContainer;
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
            var overlay = UIFactory.CreatePanel(parent, "DeckOverlay", UITheme.Background);
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

            _countLabel = UIFactory.CreateText(_root, "Count", "", 22, UITheme.Ink);
            _countLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _countLabel.rectTransform.anchoredPosition = new Vector2(0f, -78f);
            _countLabel.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _tabsContainer = UIFactory.CreateUIObject("Tabs", _root);
            _tabsContainer.anchorMin = _tabsContainer.anchorMax = new Vector2(0.5f, 1f);
            _tabsContainer.pivot = new Vector2(0.5f, 1f);
            _tabsContainer.anchoredPosition = new Vector2(0f, -112f);
            _tabsContainer.sizeDelta = new Vector2(ListWidth, 62f);

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
            ClearChildren(_tabsContainer);
            _countLabel.text = _deck.DeckCount + " seeds";
            // The Lean Bag pledge only counts at the end of the run, so the bag starts above its limit: say so here, where the player looks at the bag.
            if (_run != null && (_run.Pledges & Pledge.LeanBag) != Pledge.None)
            {
                _countLabel.text += "   -   Lean Bag pledge: win with " + PledgeCatalog.LeanBagMaxSeeds + " or fewer";
            }

            var composition = _deck.GetDeckComposition();
            var present = new List<(PieceColor Color, List<(ShapeId Shape, int Count)> Types, int Total)>();
            for (int c = 0; c < ColorSectionOrder.Length; c++)
            {
                var types = CollectTypesForColor(composition, ColorSectionOrder[c]);
                if (types.Count == 0)
                {
                    continue;
                }
                int total = 0;
                for (int t = 0; t < types.Count; t++) { total += types[t].Count; }
                present.Add((ColorSectionOrder[c], types, total));
            }
            if (present.Count == 0)
            {
                return;
            }
            if (!_selectedColor.HasValue || present.FindIndex(x => x.Color == _selectedColor.Value) < 0)
            {
                _selectedColor = present[0].Color;
            }

            // The tabs: one per color, tinted like its planter, the chosen one lit and the others dimmed. Each is a big touch target.
            float tabWidth = Mathf.Min(220f, (_listWidth - (present.Count - 1) * 10f) / present.Count);
            float tabsWidth = present.Count * tabWidth + (present.Count - 1) * 10f;
            for (int i = 0; i < present.Count; i++)
            {
                var color = present[i].Color;
                var tab = UIFactory.CreateButton(_tabsContainer, "Tab_" + color, VisualDefaults.GetColorName(color) + " " + present[i].Total, UISprites.Garden(PlanterFor(color).Replace("Light", "")) ?? UISprites.CardBackground, 18);
                var tabRect = tab.GetComponent<RectTransform>();
                tabRect.anchorMin = tabRect.anchorMax = new Vector2(0.5f, 0.5f);
                tabRect.pivot = new Vector2(0.5f, 0.5f);
                tabRect.sizeDelta = new Vector2(tabWidth, MobileSetup.TouchMin(56f));
                tabRect.anchoredPosition = new Vector2(-tabsWidth / 2f + tabWidth / 2f + i * (tabWidth + 10f), 0f);
                var tabImage = tab.GetComponent<Image>();
                bool chosen = color == _selectedColor.Value;
                tabImage.color = chosen ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                var captured = color;
                tab.onClick.AddListener(() => { _selectedColor = captured; RebuildRows(); });
            }

            var selected = present.Find(x => x.Color == _selectedColor.Value);
            var rootRect = _root.rect;
            float availableHeight = rootRect.height > 100f ? rootRect.height - ListTop - BottomMargin : ListHeight;
            float availableWidth = rootRect.width > 100f ? Mathf.Min(ListWidth, rootRect.width - 24f) : ListWidth;
            int rows = Mathf.CeilToInt(selected.Types.Count / (float)CardColumns);
            _cardWidth = Mathf.Min(MaxCardWidth, (availableWidth - (CardColumns - 1) * CardSpacing) / CardColumns);
            _cardHeight = Mathf.Clamp((availableHeight - (rows - 1) * CardSpacing) / Mathf.Max(1, rows), MinCardHeight, MaxCardHeight);
            _listWidth = availableWidth;
            _listContainer.sizeDelta = new Vector2(_listWidth, availableHeight);
            int columnsUsed = Mathf.Min(CardColumns, selected.Types.Count);
            float contentWidth = columnsUsed * _cardWidth + (columnsUsed - 1) * CardSpacing;
            BuildColorSectionGrid(selected.Types, selected.Color, 0f, (_listWidth - contentWidth) / 2f);
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

        /// <summary>Places all shape cards in one centered horizontal row for this color, with each card aligned to the same columns used by the other sections. Returns the Y cursor for whatever comes next.</summary>
        private float BuildColorSectionGrid(List<(ShapeId Shape, int Count)> types, PieceColor color, float y, float xOffset)
        {
            for (int i = 0; i < types.Count; i++)
            {
                int col = i % CardColumns;
                int row = i / CardColumns;
                float x = xOffset + col * (_cardWidth + CardSpacing);
                float cardY = y - row * (_cardHeight + CardSpacing);
                BuildTypeCard(x, cardY, types[i].Shape, color, types[i].Count);
            }

            int rowCount = Mathf.CeilToInt(types.Count / (float)CardColumns);
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
