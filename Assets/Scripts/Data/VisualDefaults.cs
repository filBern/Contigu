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
    /// </summary>
    public static class VisualDefaults
    {
        private static readonly Dictionary<PieceColor, Color> ColorMap = new Dictionary<PieceColor, Color>
        {
            { PieceColor.Coral, new Color(1f, 0.45f, 0.42f) },
            { PieceColor.Teal, new Color(0.20f, 0.75f, 0.71f) },
            { PieceColor.Violet, new Color(0.62f, 0.46f, 0.95f) },
            { PieceColor.Lime, new Color(0.68f, 0.85f, 0.26f) },
            { PieceColor.Joker, new Color(0.95f, 0.80f, 0.20f) }
        };

        private static readonly Dictionary<PieceColor, string> ColorNames = new Dictionary<PieceColor, string>
        {
            { PieceColor.Coral, "Corail" },
            { PieceColor.Teal, "Sarcelle" },
            { PieceColor.Violet, "Violet" },
            { PieceColor.Lime, "Lime" },
            { PieceColor.Joker, "Joker" }
        };

        private static readonly Dictionary<ShapeId, string> ShapeNames = new Dictionary<ShapeId, string>
        {
            { ShapeId.Single, "Solo" },
            { ShapeId.DomH, "Domino H" },
            { ShapeId.DomV, "Domino V" },
            { ShapeId.TriL, "Tri-L" },
            { ShapeId.TriIH, "Tri-I H" },
            { ShapeId.TriIV, "Tri-I V" },
            { ShapeId.Sq2, "Carré" },
            { ShapeId.LTetro, "Tétro-L" },
            { ShapeId.TTetro, "Tétro-T" },
            { ShapeId.STetro, "Tétro-S" }
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

        public static readonly Color GoldenColor = new Color(1f, 0.84f, 0.15f);
        public static readonly Color TintedOutline = new Color(1f, 1f, 1f, 0.85f);
        public static readonly Color MultiplierOutline = new Color(0.9f, 0.3f, 0.9f, 0.85f);
        public static readonly Color LockedColor = new Color(0.15f, 0.15f, 0.18f);
        public static readonly Color EmptyCellColor = new Color(0.93f, 0.93f, 0.96f);
    }
}
