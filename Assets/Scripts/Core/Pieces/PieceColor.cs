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
    }
}
