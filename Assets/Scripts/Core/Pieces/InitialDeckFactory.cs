using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Builds the starting ~24 token deck (spec 4.5): a reasonably balanced spread
    /// across the 10 shapes and 4 base colors, with no Joker at the start (Joker is
    /// only obtainable via the "Joker piece" upgrade).
    /// </summary>
    public static class InitialDeckFactory
    {
        /// <summary>All 10 shapes in a fixed array — also reused by DeckManager.AddJoker to pick a uniformly random shape.</summary>
        public static readonly ShapeId[] ShapeOrder =
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

        /// <summary>The "Marathon" challenge's smaller starting deck (spec extension, explicit request — see ChallengeCatalog.Marathon), sums to 16: every shape still gets at least 1 copy (never below DeckManager.MinDeckSize=10, kept well above it), just fewer of each than the 24-token standard deck.</summary>
        private static readonly Dictionary<ShapeId, int> MarathonCopiesPerShape = new Dictionary<ShapeId, int>
        {
            { ShapeId.Single, 2 },
            { ShapeId.DomH, 2 },
            { ShapeId.DomV, 2 },
            { ShapeId.TriL, 1 },
            { ShapeId.TriIH, 1 },
            { ShapeId.TriIV, 1 },
            { ShapeId.Sq2, 2 },
            { ShapeId.LTetro, 2 },
            { ShapeId.TTetro, 2 },
            { ShapeId.STetro, 1 }
        };

        public static List<PieceToken> Build()
        {
            return BuildFrom(CopiesPerShape);
        }

        public static List<PieceToken> BuildMarathon()
        {
            return BuildFrom(MarathonCopiesPerShape);
        }

        private static List<PieceToken> BuildFrom(Dictionary<ShapeId, int> copiesPerShape)
        {
            var tokens = new List<PieceToken>();
            var baseColors = PieceColorUtility.BaseColors;
            int colorCursor = 0;

            for (int s = 0; s < ShapeOrder.Length; s++)
            {
                var shapeId = ShapeOrder[s];
                int copies = copiesPerShape[shapeId];
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
