using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// The 4 base colors plus the Joker wildcard color (see spec section 4.4).
    /// </summary>
    public enum PieceColor
    {
        Coral,
        Teal,
        Violet,
        Lime,
        Joker
    }

    public static class PieceColorUtility
    {
        public static readonly IReadOnlyList<PieceColor> BaseColors = new[]
        {
            PieceColor.Coral,
            PieceColor.Teal,
            PieceColor.Violet,
            PieceColor.Lime
        };

        /// <summary>
        /// Two colors "match" for neighbor-bonus purposes if they are identical,
        /// or if either one is the Joker wildcard (spec 3.2).
        /// </summary>
        public static bool Matches(PieceColor a, PieceColor b)
        {
            return a == b || a == PieceColor.Joker || b == PieceColor.Joker;
        }
    }
}
