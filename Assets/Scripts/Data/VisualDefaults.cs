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
            { PieceColor.Coral, new Color(0.906f, 0.298f, 0.235f) }, // #e74c3c (Red)
            { PieceColor.Teal, new Color(0.204f, 0.596f, 0.859f) }, // #3498db (Blue)
            { PieceColor.Violet, new Color(0.945f, 0.769f, 0.059f) }, // #f1c40f (Yellow)
            { PieceColor.Lime, new Color(0.180f, 0.800f, 0.443f) }, // #2ecc71 (Green)
            { PieceColor.Joker, new Color(0.608f, 0.349f, 0.714f) } // #9b59b6 (vivid purple)
        };

        private static readonly Dictionary<PieceColor, string> ColorNames = new Dictionary<PieceColor, string>
        {
            { PieceColor.Coral, "Red" },
            { PieceColor.Teal, "Blue" },
            { PieceColor.Violet, "Yellow" },
            { PieceColor.Lime, "Green" },
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
