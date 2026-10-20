using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>The 4 base colors plus the Joker wildcard color.</summary>
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
        /// <summary>All 4 base colors, in canonical order.</summary>
        public static readonly IReadOnlyList<PieceColor> AllBaseColors = new[]
        {
            PieceColor.Coral,
            PieceColor.Teal,
            PieceColor.Violet,
            PieceColor.Lime
        };

        /// <summary>
        /// The base colors that exist in the run in progress — all 4, except in a challenge that trims them (Orchard keeps 2). Static for the
        /// same reason as GridManager.Size: every RunManager sets it from its challenge, and everything that rolls or counts "a base color"
        /// reads it, so no piece of a missing color can ever appear.
        /// </summary>
        public static IReadOnlyList<PieceColor> BaseColors { get; private set; } = AllBaseColors;

        public static void SetActiveBaseColors(IReadOnlyList<PieceColor> colors)
        {
            BaseColors = colors;
        }

        /// <summary>Whether <paramref name="color"/> exists in the run in progress (Joker always does).</summary>
        public static bool IsActive(PieceColor color)
        {
            if (color == PieceColor.Joker)
            {
                return true;
            }
            for (int i = 0; i < BaseColors.Count; i++)
            {
                if (BaseColors[i] == color)
                {
                    return true;
                }
            }
            return false;
        }

        public static void ResetBaseColors()
        {
            BaseColors = AllBaseColors;
        }
    }
}
