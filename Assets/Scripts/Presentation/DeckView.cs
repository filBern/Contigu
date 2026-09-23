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
    /// representative trait badge if any copy is enchanted) — lets the player
    /// check what's in their deck without having to wait for the next draft
    /// (on explicit request). Reuses DraftView's exact row-building approach
    /// (see BuildTypeRow/FindRepresentativeTrait there) since it's the same
    /// underlying data (DeckManager.GetDeckComposition), just shown without
    /// the pick/sub-choice flow around it.
    ///
    /// Grouped by color into its own labeled section per PieceColor, each a
    /// narrow multi-column wrap of small type cards (on explicit report —
    /// "L'écran du deck est vraiment chaotique, j'aimerais que les pièces
    /// soient filtered par couleur et qu'elles prennent moins de largeur
    /// chacune": the original single ungrouped 3-column grid mixed every
    /// color together in whatever order DeckManager.GetDeckComposition's
    /// dictionary happened to iterate, at a fixed 300px-wide card for what
    /// amounts to a small shape preview and an "xN" label). Manually
    /// positioned section-by-section (no LayoutGroup) since each section's
    /// row count — and so its height — depends on how many distinct types
    /// that color actually has in the deck.
    /// </summary>
    public sealed class DeckView : MonoBehaviour
    {
        private const float CardWidth = 140f;
        private const float CardHeight = 46f;
        private const float CardSpacing = 8f;
        private const int ColumnsPerSection = 6;
        private const float RowPreviewSize = 30f;
        private const float SectionHeaderHeight = 22f;
        private const float SectionHeaderToGridGap = 4f;
        private const float SectionGap = 14f;
        private const float ListWidth = 920f;

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

            // Top-pivoted (not centered) since sections are positioned by a
            // running Y cursor in RebuildRows, top-down — each section's
            // height depends on how many distinct types that color has.
            _listContainer = UIFactory.CreateUIObject("List", _root);
            _listContainer.anchorMin = new Vector2(0.5f, 1f);
            _listContainer.anchorMax = new Vector2(0.5f, 1f);
            _listContainer.pivot = new Vector2(0.5f, 1f);
            _listContainer.anchoredPosition = new Vector2(0f, -110f);
            _listContainer.sizeDelta = new Vector2(ListWidth, 560f);

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

        // Fixed display order (not enum declaration order specifically, but
        // it happens to match) — Joker last since it's the rare wildcard
        // case, so it only ever appears once every other section already has.
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

            // Every section shares the SAME horizontal offset/width (based
            // on whichever section actually has the most distinct types,
            // capped at ColumnsPerSection) rather than each hugging the
            // container's own left edge — on explicit report, with every
            // color under a full row the whole block still sat flush left
            // inside the wider fixed-width container instead of reading as
            // centered on screen.
            int columnsUsed = Mathf.Min(ColumnsPerSection, widestSection);
            float contentWidth = columnsUsed * CardWidth + (columnsUsed - 1) * CardSpacing;
            float xOffset = (ListWidth - contentWidth) / 2f;

            float y = 0f;
            for (int i = 0; i < sections.Count; i++)
            {
                y = BuildColorSectionHeader(sections[i].Color, y, xOffset, contentWidth);
                y -= SectionHeaderToGridGap;
                y = BuildColorSectionGrid(sections[i].Types, sections[i].Color, y, xOffset);
                y -= SectionGap;
            }
        }

        /// <summary>
        /// DomH/DomV (a 2-cell "domino") and TriIH/TriIV (a 3-cell straight
        /// line) are the same piece rotated 90° — shown here as a single
        /// merged row per color rather than two, since seeing both
        /// orientations listed separately reads as duplicate entries in this
        /// read-only summary (explicit report with a screenshot circling
        /// exactly these two pairs: "on peut donc retirer les doublons dans
        /// l'écran de deck"). This is purely a display grouping — the deck
        /// itself, DraftView's picker, and gameplay are untouched; both
        /// orientations remain separately drawable pieces.
        /// </summary>
        private static readonly Dictionary<ShapeId, ShapeId> OrientationDuplicateOf = new Dictionary<ShapeId, ShapeId>
        {
            { ShapeId.DomV, ShapeId.DomH },
            { ShapeId.TriIV, ShapeId.TriIH }
        };

        private static List<ShapeId> GetOrientationDuplicates(ShapeId representative)
        {
            var duplicates = new List<ShapeId>();
            foreach (var kvp in OrientationDuplicateOf)
            {
                if (kvp.Value == representative)
                {
                    duplicates.Add(kvp.Key);
                }
            }
            return duplicates;
        }

        /// <summary>Every (shape, count) the deck currently has in <paramref name="color"/>, in InitialDeckFactory.ShapeOrder's fixed order — GetDeckComposition's own Dictionary iteration order isn't guaranteed and, in practice, mixes shapes unpredictably (see this class's own doc comment on the original bug report). Orientation duplicates (see OrientationDuplicateOf) are folded into their representative shape's count.</summary>
        private List<(ShapeId Shape, int Count)> CollectTypesForColor(IReadOnlyDictionary<(ShapeId Shape, PieceColor Color), int> composition, PieceColor color)
        {
            var result = new List<(ShapeId, int)>();
            var shapeOrder = InitialDeckFactory.ShapeOrder;
            for (int i = 0; i < shapeOrder.Length; i++)
            {
                var shape = shapeOrder[i];
                if (OrientationDuplicateOf.ContainsKey(shape))
                {
                    continue;
                }

                int count = 0;
                if (composition.TryGetValue((shape, color), out int own))
                {
                    count += own;
                }
                var duplicates = GetOrientationDuplicates(shape);
                for (int d = 0; d < duplicates.Count; d++)
                {
                    if (composition.TryGetValue((duplicates[d], color), out int duplicateCount))
                    {
                        count += duplicateCount;
                    }
                }

                if (count > 0)
                {
                    result.Add((shape, count));
                }
            }
            return result;
        }

        /// <summary>Section label tinted the color it groups — e.g. "CORAL" in Coral's own display color — so the grouping reads at a glance without needing to read the word itself. Starts at <paramref name="xOffset"/> and spans <paramref name="contentWidth"/>, matching its grid's own centered columns below it. Returns the Y cursor for whatever comes next.</summary>
        private float BuildColorSectionHeader(PieceColor color, float y, float xOffset, float contentWidth)
        {
            var label = UIFactory.CreateText(_listContainer, "Header_" + color, VisualDefaults.GetColorName(color).ToUpperInvariant(), 15, VisualDefaults.GetColor(color), TextAnchor.LowerLeft);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(xOffset, y);
            label.rectTransform.sizeDelta = new Vector2(contentWidth, SectionHeaderHeight);
            return y - SectionHeaderHeight;
        }

        /// <summary>Wraps <paramref name="types"/> across ColumnsPerSection narrow columns, as many rows as needed, every column starting at <paramref name="xOffset"/> — the same offset every section shares, so columns stay aligned across the whole (centered) block. Returns the Y cursor for whatever comes next.</summary>
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
            previewContainer.anchoredPosition = new Vector2(6f, 0f);
            previewContainer.sizeDelta = new Vector2(RowPreviewSize, RowPreviewSize);

            var trait = FindRepresentativeTrait(shape, color);
            ShapePreviewFactory.Build(previewContainer, PieceShapeCatalog.Get(shape), color, trait, _tooltip, row.gameObject);

            var countLabel = UIFactory.CreateText(row.transform, "Count", "x" + count, 14, UITheme.TextPrimary);
            countLabel.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            countLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            countLabel.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
            countLabel.rectTransform.sizeDelta = new Vector2(34f, 26f);
        }

        /// <summary>Same representative-sample approach as DraftView.FindRepresentativeTrait — see there for why a per-type row can't show more than one sample trait. Also checks <paramref name="shape"/>'s orientation duplicate (if any), since a merged row represents both orientations' tokens.</summary>
        private PieceTrait? FindRepresentativeTrait(ShapeId shape, PieceColor color)
        {
            var trait = FindTraitForExactType(shape, color);
            if (trait.HasValue)
            {
                return trait;
            }

            var duplicates = GetOrientationDuplicates(shape);
            for (int d = 0; d < duplicates.Count; d++)
            {
                trait = FindTraitForExactType(duplicates[d], color);
                if (trait.HasValue)
                {
                    return trait;
                }
            }
            return null;
        }

        private PieceTrait? FindTraitForExactType(ShapeId shape, PieceColor color)
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
