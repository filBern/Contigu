using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Built-in display defaults (colors and names) so the game is fully playable
    /// without anyone having to create <see cref="PieceColorDatabase"/> /
    /// <see cref="PieceShapeDatabase"/> assets in the editor first. A database
    /// asset, if assigned on <c>GameBootstrap</c>, overrides these per-entry.
    ///
    /// Colors are built from the v1 8-color game palette (also used for chrome —
    /// see Presentation.UITheme): #372e4d, #5f699c, #65aed6, #a4ebcc, #effae6,
    /// #f0b38d, #b56d7f, #614363. The 4 base piece colors take the palette's 4
    /// most saturated hues (one each), Joker takes the remaining mid-tone slate
    /// for a deliberately calmer, "wildcard" feel against the 4 vivid pieces.
    /// </summary>
    public static class VisualDefaults
    {
        private static readonly Dictionary<PieceColor, Color> ColorMap = new Dictionary<PieceColor, Color>
        {
            { PieceColor.Coral, new Color(0.941f, 0.702f, 0.553f) }, // #f0b38d
            { PieceColor.Teal, new Color(0.396f, 0.682f, 0.839f) }, // #65aed6
            { PieceColor.Violet, new Color(0.710f, 0.427f, 0.498f) }, // #b56d7f
            { PieceColor.Lime, new Color(0.643f, 0.922f, 0.800f) }, // #a4ebcc
            { PieceColor.Joker, new Color(0.373f, 0.412f, 0.612f) } // #5f699c
        };

        private static readonly Dictionary<PieceColor, string> ColorNames = new Dictionary<PieceColor, string>
        {
            { PieceColor.Coral, "Coral" },
            { PieceColor.Teal, "Teal" },
            { PieceColor.Violet, "Violet" },
            { PieceColor.Lime, "Lime" },
            { PieceColor.Joker, "Joker" }
        };

        private static readonly Dictionary<ShapeId, string> ShapeNames = new Dictionary<ShapeId, string>
        {
            { ShapeId.Single, "Single" },
            { ShapeId.DomH, "Domino H" },
            { ShapeId.DomV, "Domino V" },
            { ShapeId.TriL, "L-Tromino" },
            { ShapeId.TriIH, "I-Tromino H" },
            { ShapeId.TriIV, "I-Tromino V" },
            { ShapeId.Sq2, "Square" },
            { ShapeId.LTetro, "L-Tetromino" },
            { ShapeId.TTetro, "T-Tetromino" },
            { ShapeId.STetro, "S-Tetromino" }
        };

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

        public static readonly Color GoldenColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d — warmest palette color; blends into a pale gold over EmptyCellColor
        public static readonly Color TintedOutline = new Color(0.937f, 0.980f, 0.902f, 0.85f); // #effae6
        public static readonly Color MultiplierOutline = new Color(0.396f, 0.682f, 0.839f, 0.85f); // #65aed6 — kept distinct from Golden/Tinted so the 3 badge types stay distinguishable on a cell
        public static readonly Color LockedColor = new Color(0.216f, 0.180f, 0.302f); // #372e4d — same as UITheme.Background: locked cells recede into the void
        public static readonly Color EmptyCellColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
    }
}
