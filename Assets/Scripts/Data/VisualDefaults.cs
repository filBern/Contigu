using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Built-in display defaults (colors and names) for every <see cref="PieceColor"/>
    /// and <see cref="ShapeId"/> — the only source of this display metadata; no
    /// ScriptableObject override mechanism exists.
    ///
    /// The underlying <see cref="PieceColor"/> enum names (Coral/Teal/Violet/Lime)
    /// are not renamed to match the displayed color names, since that would ripple
    /// through ~100 modifier ids (DevotionCoral, EclatTeal, ...); only the rendered
    /// <see cref="ColorMap"/> value and displayed <see cref="ColorNames"/> string
    /// differ from the enum name.
    /// </summary>
    public static class VisualDefaults
    {
        /// <summary>
        /// Pixel size of one actual grid cell (see GameBootstrap.CellSize,
        /// which reads this instead of its own literal). Presentation.ShapePreviewFactory
        /// caps its per-cell preview size at this value so previews (hand slot,
        /// cursor ghost, draft/deck rows) never render larger than the piece will
        /// appear once placed on the grid.
        /// </summary>
        public const float GridCellSize = 54f;

        private static readonly Dictionary<PieceColor, Color> ColorMap = new Dictionary<PieceColor, Color>
        {
            { PieceColor.Coral, new Color(0.784f, 0.251f, 0.247f) }, // #c8403f (Poppy)
            { PieceColor.Teal, new Color(0.310f, 0.498f, 0.816f) }, // #4f7fd0 (Bluebell)
            { PieceColor.Violet, new Color(0.910f, 0.659f, 0.180f) }, // #e8a82e (Sunflower)
            { PieceColor.Lime, new Color(0.353f, 0.620f, 0.271f) }, // #5a9e45 (Fern)
            { PieceColor.Joker, new Color(0.549f, 0.388f, 0.690f) } // #8c63b0 (Lavender)
        };

        private static readonly Dictionary<PieceColor, string> ColorNames = new Dictionary<PieceColor, string>
        {
            { PieceColor.Coral, "Ember" },
            { PieceColor.Teal, "Frost" },
            { PieceColor.Violet, "Gold" },
            { PieceColor.Lime, "Moss" },
            { PieceColor.Joker, "Joker" }
        };

        private static readonly Dictionary<ShapeId, string> ShapeNames = new Dictionary<ShapeId, string>
        {
            { ShapeId.Single, "Single" },
            { ShapeId.DomH, "Domino" },
            { ShapeId.TriL, "L-Tromino" },
            { ShapeId.TriIH, "I-Tromino" },
            { ShapeId.Sq2, "Square" },
            { ShapeId.LTetro, "L-Tetromino" },
            { ShapeId.TTetro, "T-Tetromino" },
            { ShapeId.STetro, "S-Tetromino" }
        };

        public static readonly Sprite GoldenTileSprite = Resources.Load<Sprite>("Icons/GoldenTile");
        public static readonly Sprite LockedTileSprite = Resources.Load<Sprite>("Icons/LockedTile");

        // No longer drawn — TileSprite below now gives every cell its own
        // card-shaped border directly (see GridCellView.ApplyState). Kept
        // in case a future look wants it back.
        public static readonly Sprite FillTileSprite = Resources.Load<Sprite>("Tiles/fill-tile-piece");

        /// <summary>
        /// Every grid cell and piece-preview square's shared base look: 9-sliced
        /// card art (duplicated from Presentation.UISprites.CardBackground, since
        /// Data must not depend on Presentation), shown at its natural pale color
        /// for an empty cell and tinted with a piece's color for a filled one.
        /// </summary>
        public static readonly Sprite TileSprite = Resources.Load<Sprite>("Colorful_UI/colorful/sprites/gameUI/card_bg_3");

        public static Color GetColor(PieceColor color)
        {
            return ColorMap.TryGetValue(color, out var c) ? c : Color.gray;
        }

        public static string GetColorName(PieceColor color)
        {
            return ColorNames.TryGetValue(color, out var n) ? n : color.ToString();
        }

        public static string GetShapeName(ShapeId shape)
        {
            return ShapeNames.TryGetValue(shape, out var n) ? n : shape.ToString();
        }

        public static readonly Color GoldenColor = new Color(1f, 0.773f, 0.239f); // #ffc53d
        public static readonly Color MultiplierOutline = new Color(0.396f, 0.682f, 0.839f, 0.85f); // #65aed6 — kept distinct from Golden/Tinted so badge types stay distinguishable
        public static readonly Color LockedColor = new Color(0.071f, 0.188f, 0.290f); // #12304a — same as UITheme.Background
        public static readonly Color EmptyCellColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
    }
}
