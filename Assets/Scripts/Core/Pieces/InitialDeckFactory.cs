using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Builds the starting 40-token deck (spec 4.5, revised — see Build's own
    /// doc comment): every one of the 10 shapes in every one of the 4 base
    /// colors, exactly once each, no Joker at the start (Joker is only
    /// obtainable via the "Joker piece" upgrade).
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

        /// <summary>The "Marathon" challenge's smaller starting deck (spec extension, explicit request — see ChallengeCatalog.Marathon), sums to 16: every shape still gets at least 1 copy (never below DeckManager.MinDeckSize=10, kept well above it), just fewer of each than the standard deck, and — unlike Build() below — not every shape reaches every color, on purpose: a smaller, less complete deck is exactly what makes Marathon harder.</summary>
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

        /// <summary>
        /// One copy of every (shape, color) combination — 10 x 4 = 40
        /// tokens. Previously a fixed 24-token deck with an uneven number
        /// of copies per shape (3 for Single/DomH/DomV/Sq2, 2 for
        /// everything else) cycled across colors via a running cursor —
        /// which meant a shape with only 2 or 3 copies could never reach
        /// all 4 colors, so a given color could be missing shapes
        /// entirely (on explicit report, spotted via the new color-
        /// grouped DeckView: "Il manque des pièces dans le deck non?
        /// Single tile green, etc." -> "J'aimerais que toutes les
        /// couleurs aient toutes les formes"). Full, uniform coverage
        /// instead: no shape/color combination is ever absent from the
        /// starting deck.
        /// </summary>
        public static List<PieceToken> Build()
        {
            var tokens = new List<PieceToken>();
            var baseColors = PieceColorUtility.BaseColors;
            for (int s = 0; s < ShapeOrder.Length; s++)
            {
                for (int c = 0; c < baseColors.Count; c++)
                {
                    tokens.Add(new PieceToken(ShapeOrder[s], baseColors[c]));
                }
            }
            return tokens;
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
