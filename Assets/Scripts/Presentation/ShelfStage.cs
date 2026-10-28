using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Where the shop's two shelves stand, relative to the screen's vertical middle (up is positive), and how wide they are.</summary>
    public struct ShelfLayout
    {
        public float FirstShelfY;
        public float SecondShelfY;
        public float Width;
    }

    /// <summary>The shop object a choice comes from (an anvil with a glyph, a crate, the compost bucket, an upgrade sign) and its tooltip.</summary>
    public struct ChoiceSource
    {
        public Sprite Art;
        public Sprite Glyph;
        public UpgradeVisualDefaults.GlyphSurface Surface;
        public string Name;
        public string Body;
        public string Subtitle;
    }

    /// <summary>
    /// The one stage every purchase choice plays on, so Forge, packs and Compost look alike: the shop object on top, a title, the options in
    /// one or two rows resting on the SAME two shelves the shop itself uses (same height, same width), and Confirm under them.
    /// </summary>
    public sealed class ShelfStage
    {
        private const float CanvasHalfHeight = 400f;
        private const float SourceSize = 104f;
        private const float TitleHeight = 36f;
        private const float ShelfHeight = 16f;
        private const float RowOverlap = 2f;
        // The stage's shelves are a little wider than the shop's (they pass under the side panels), to leave pieces room to breathe.
        private const float ExtraWidth = 110f;
        private const float UpperShelfDrop = 56f;
        private const float IconGapPerCell = 0.04f;
        private const int MaxPerRow = 3; // one row up to 3 options; beyond that two rows (up to 4 each: 8 is the most the shop ever shows)
        // A pack with only 3 options must not get huge cells: the stack (source, title, row) has to stay below the shop header.
        public const float SeedCellSize = 96f;
        public const float IconCellSize = 120f;
        // One row of options stands on the LOWER shelf, which leaves the room above it for much bigger cells (the stack still ends below the shop header).
        public const float SingleRowCellSize = 190f;

        private readonly TooltipView _tooltip;
        private readonly Image _source;
        private readonly Image _sourceGlyph;
        private readonly HoverTooltip _sourceTooltip;
        private readonly Text _title;
        private readonly RectTransform _row1;
        private readonly RectTransform _row2;
        private readonly RectTransform _shelf1;
        private readonly RectTransform _shelf2;
        private int _firstRowCount;

        public Button ConfirmButton { get; private set; }

        public ShelfStage(RectTransform root, TooltipView tooltip)
        {
            _tooltip = tooltip;
            _shelf1 = BuildShelf(root, "Shelf1");
            _shelf2 = BuildShelf(root, "Shelf2");
            _row1 = BuildRow(root, "Row1");
            _row2 = BuildRow(root, "Row2");

            _source = UIFactory.CreatePanel(root, "Source", Color.white);
            _source.preserveAspect = true;
            TopCenter(_source.rectTransform);
            _source.rectTransform.sizeDelta = new Vector2(SourceSize, SourceSize);
            _sourceTooltip = _source.gameObject.AddComponent<HoverTooltip>();
            _sourceGlyph = UIFactory.CreatePanel(_source.transform, "Glyph", Color.white);
            _sourceGlyph.preserveAspect = true;
            _sourceGlyph.raycastTarget = false;

            _title = UIFactory.CreateText(root, "Title", "", 22, UITheme.TextOnBackground);
            TopCenter(_title.rectTransform);
            _title.rectTransform.sizeDelta = new Vector2(700f, TitleHeight);

            ConfirmButton = UIFactory.CreateButton(root, "Confirm", "Confirm", UISprites.ChooseButtonBackground, 18);
            var confirmRect = ConfirmButton.GetComponent<RectTransform>();
            TopCenter(confirmRect);
            confirmRect.sizeDelta = new Vector2(200f, 46f);
        }

        /// <summary>Which row (a rest on which shelf) the option <paramref name="index"/> goes to.</summary>
        public RectTransform RowFor(int index)
        {
            return index < _firstRowCount ? _row1 : _row2;
        }

        public float CellSize
        {
            get { return PieceBox.CellSize; }
        }

        /// <summary>Clears the previous options and lays the stage out for <paramref name="total"/> options on the given shop shelves.</summary>
        /// <summary>The one tile size every piece of the current choice is drawn with (0 when the options are not pieces).</summary>
        public float Tile { get; private set; }

        private float[] _cellWidths;

        /// <summary>The width option <paramref name="index"/> takes in its row: its piece's real width when pieces are spaced by their own width, else a plain cell.</summary>
        public float CellWidthFor(int index)
        {
            return _cellWidths != null && index < _cellWidths.Length && _cellWidths[index] > 0f ? _cellWidths[index] : CellSize;
        }

        public void Begin(ShelfLayout layout, ChoiceSource source, string title, int total, bool withConfirm, float maxCellSize = SeedCellSize, IReadOnlyList<int> columns = null)
        {
            Clear(_row1);
            Clear(_row2);
            _firstRowCount = total <= MaxPerRow ? total : (total + 1) / 2;
            bool two = total > _firstRowCount;
            // With two rows the upper shelf (and everything resting on it) is lowered a little, closer to the lower row.
            float firstShelfY = two ? layout.FirstShelfY - UpperShelfDrop : layout.SecondShelfY;
            // Pieces are drawn bigger than their cell (a 3-wide one overflows by about a third of a cell): the cells of a row are spaced to match, so
            // neighbors never touch. The width left for the cells shrinks accordingly.
            const float RowGapPerCell = 0.34f;
            int perRow = Mathf.Max(1, _firstRowCount);
            float fitWidth = (layout.Width - 24f) * perRow / (perRow + RowGapPerCell * (perRow - 1));
            PieceBox.Configure(perRow, fitWidth, two ? maxCellSize : Mathf.Max(maxCellSize, SingleRowCellSize));
            float cell = PieceBox.CellSize;
            float spacing = RowGapPerCell * cell;
            Tile = 0f;
            _cellWidths = null;
            bool pieces = false;
            for (int i = 0; columns != null && i < columns.Count; i++)
            {
                pieces |= columns[i] > 0;
            }
            if (pieces)
            {
                // Pieces: one tile size for all, and the gap between two neighbors is the same whatever their widths: each cell is exactly as wide
                // as its piece, the rows are spaced by one gap, and the tile shrinks only if the widest row would not fit the shelf.
                const float GapPerTile = 1f; // one tile of air between two neighbors
                float width = layout.Width + ExtraWidth - 24f;
                float tileFit = float.MaxValue;
                for (int row = 0; row < 2; row++)
                {
                    int from = row == 0 ? 0 : _firstRowCount;
                    int to = row == 0 ? Mathf.Min(_firstRowCount, columns.Count) : columns.Count;
                    int sum = 0;
                    for (int i = from; i < to; i++)
                    {
                        sum += columns[i];
                    }
                    int count = to - from;
                    if (sum > 0)
                    {
                        tileFit = Mathf.Min(tileFit, width / (sum + GapPerTile * (count - 1)));
                    }
                }
                Tile = Mathf.Min(Mathf.Min(cell, maxCellSize) * 0.447f, VisualDefaults.GridCellSize * 4f / 3f, tileFit);
                spacing = GapPerTile * Tile;
                _cellWidths = new float[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    _cellWidths[i] = columns[i] > 0 ? columns[i] * Tile : 0f;
                }
            }
            if (!pieces)
            {
                spacing = IconGapPerCell * cell; // icons do not overflow their cell: no need for the pieces' wide gap
            }
            _row1.GetComponent<HorizontalLayoutGroup>().spacing = spacing;
            _row2.GetComponent<HorizontalLayoutGroup>().spacing = spacing;

            _shelf1.sizeDelta = new Vector2(layout.Width + ExtraWidth, ShelfHeight);
            _shelf2.sizeDelta = new Vector2(layout.Width + ExtraWidth, ShelfHeight);
            _shelf1.anchoredPosition = new Vector2(0f, firstShelfY - CanvasHalfHeight);
            _shelf2.anchoredPosition = new Vector2(0f, layout.SecondShelfY - CanvasHalfHeight);
            _shelf2.gameObject.SetActive(two);
            _row2.gameObject.SetActive(two);

            float row1Top = firstShelfY + cell - RowOverlap;
            _row1.anchoredPosition = new Vector2(0f, row1Top - CanvasHalfHeight);
            _row2.anchoredPosition = new Vector2(0f, layout.SecondShelfY + cell - RowOverlap - CanvasHalfHeight);

            // The title clears the tallest piece (3 tiles high), not just the cell.
            float stackTop = Mathf.Max(row1Top, firstShelfY + 3f * Tile + 6f);
            float titleTop = stackTop + 14f + TitleHeight;
            _title.text = title;
            _title.rectTransform.anchoredPosition = new Vector2(0f, titleTop - CanvasHalfHeight);
            float sourceTop = titleTop + 6f + SourceSize;
            _source.rectTransform.anchoredPosition = new Vector2(0f, sourceTop - CanvasHalfHeight);

            _source.sprite = source.Art;
            _source.enabled = source.Art != null;
            _sourceGlyph.gameObject.SetActive(source.Glyph != null);
            if (source.Glyph != null)
            {
                _sourceGlyph.sprite = source.Glyph;
                UpgradeVisualDefaults.PlaceGlyph(_sourceGlyph.rectTransform, source.Surface, SourceSize);
            }
            _sourceTooltip.Init(_tooltip, source.Name, source.Body, source.Subtitle, UITheme.TextMuted);

            var confirmRect = ConfirmButton.GetComponent<RectTransform>();
            ConfirmButton.gameObject.SetActive(withConfirm);
            float lastShelf = two ? layout.SecondShelfY : firstShelfY;
            confirmRect.anchoredPosition = new Vector2(0f, lastShelf - ShelfHeight - 12f - CanvasHalfHeight);
        }

        /// <summary>The source for a tile upgrade: the anvil with its glyph (Forge), the Sprout Pack crate with its glyph, or the upgrade's own sign.</summary>
        public static ChoiceSource ForUpgrade(UpgradeDefinition def, UpgradeId? from)
        {
            var source = new ChoiceSource
            {
                Name = def.Name,
                Body = TileUpgradePreview.Append(DescriptionTextFormatter.Colorize(def.Description, 14), def.Id),
                Subtitle = Loc.T("shop.upgrade", UpgradeVisualDefaults.GetRarityLabel(def.Rarity), UpgradeVisualDefaults.GetPoolLabel(def.Pool).Replace(" Upgrade", "")),
                Art = from == UpgradeId.Forge ? UpgradeVisualDefaults.GetWorkshopArt("Anvil")
                    : from == UpgradeId.SproutPack ? UpgradeVisualDefaults.GetWorkshopArt("SproutPack")
                    : (UpgradeVisualDefaults.GetGlyph(def.Id) ?? UpgradeVisualDefaults.GetIcon(def.Id))
            };
            if (from.HasValue)
            {
                source.Glyph = UpgradeVisualDefaults.GetGlyph(def.Id);
                source.Surface = from == UpgradeId.Forge ? UpgradeVisualDefaults.GlyphSurface.Anvil : UpgradeVisualDefaults.GlyphSurface.Crate;
            }
            return source;
        }

        /// <summary>The source for a Workshop pack or service: its own picture (crate, bucket) and description.</summary>
        public static ChoiceSource ForWorkshop(UpgradeDefinition def)
        {
            bool isService = def.Id == UpgradeId.Compost || def.Id == UpgradeId.Forge;
            return new ChoiceSource
            {
                Art = UpgradeVisualDefaults.GetWorkshopArt(def.Id.ToString()),
                Name = def.Name,
                Body = DescriptionTextFormatter.Colorize(def.Description, 14),
                Subtitle = Loc.T(isService ? "shop.workshopService" : "shop.workshopPack")
            };
        }

        private static void Clear(RectTransform row)
        {
            for (int i = row.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(row.GetChild(i).gameObject);
            }
        }

        private static void TopCenter(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        private static RectTransform BuildRow(RectTransform root, string name)
        {
            var row = UIFactory.CreateUIObject(name, root);
            TopCenter(row);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return row;
        }

        /// <summary>A wooden shelf, the same look as the shop's display shelves.</summary>
        private static RectTransform BuildShelf(RectTransform root, string name)
        {
            var board = UIFactory.CreatePanel(root, name, new Color(0.45f, 0.30f, 0.17f, 1f));
            board.raycastTarget = false;
            var rect = board.rectTransform;
            TopCenter(rect);
            var lip = UIFactory.CreatePanel(rect, "Lip", new Color(0.62f, 0.43f, 0.24f, 1f));
            lip.raycastTarget = false;
            lip.rectTransform.anchorMin = new Vector2(0f, 1f);
            lip.rectTransform.anchorMax = new Vector2(1f, 1f);
            lip.rectTransform.pivot = new Vector2(0.5f, 1f);
            lip.rectTransform.anchoredPosition = Vector2.zero;
            lip.rectTransform.sizeDelta = new Vector2(0f, 5f);
            var shadow = UIFactory.CreatePanel(rect, "Shadow", new Color(0f, 0f, 0f, 0.35f));
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = new Vector2(0f, 0f);
            shadow.rectTransform.anchorMax = new Vector2(1f, 0f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 1f);
            shadow.rectTransform.anchoredPosition = Vector2.zero;
            shadow.rectTransform.sizeDelta = new Vector2(0f, 14f);
            return rect;
        }
    }
}
