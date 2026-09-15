using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Builds the starting ~24 token deck (spec 4.5): a reasonably balanced spread
    /// across the 10 shapes and 4 base colors, with no Joker at the start (Joker is
    /// only obtainable via the "Pièce joker" upgrade).
    /// </summary>
    public static class InitialDeckFactory
    {
        private static readonly ShapeId[] ShapeOrder =
        {
            ShapeId.Single, ShapeId.DomH, ShapeId.DomV,
            ShapeId.TriL, ShapeId.TriIH, ShapeId.TriIV,
            ShapeId.Sq2, ShapeId.LTetro, ShapeId.TTetro, ShapeId.STetro
        };

        // Copies per shape, sums to 24.
        private static readonly Dictionary<ShapeId, int> CopiesPerShape = new Dictionary<ShapeId, int>
        {
            { ShapeId.Single, 3 },
            { ShapeId.DomH, 3 },
            { ShapeId.DomV, 3 },
            { ShapeId.TriL, 2 },
            { ShapeId.TriIH, 2 },
            { ShapeId.TriIV, 2 },
            { ShapeId.Sq2, 3 },
            { ShapeId.LTetro, 2 },
            { ShapeId.TTetro, 2 },
            { ShapeId.STetro, 2 }
        };

        public static List<PieceToken> Build()
        {
            var tokens = new List<PieceToken>();
            var baseColors = PieceColorUtility.BaseColors;
            int colorCursor = 0;

            for (int s = 0; s < ShapeOrder.Length; s++)
            {
                var shapeId = ShapeOrder[s];
                int copies = CopiesPerShape[shapeId];
                for (int i = 0; i < copies; i++)
                {
                    var color = baseColors[colorCursor % baseColors.Count];
                    colorCursor++;
                    tokens.Add(new PieceToken(shapeId, color));
                }
            }

            return tokens;
        }
    }
}
