namespace Contigu.Core
{
    /// <summary>
    /// Reference scoring values. Kept as a single tunable surface so balancing
    /// doesn't require touching game logic.
    /// </summary>
    public static class ScoringConstants
    {
        /// <summary>
        /// Points per cell in a placement's resulting connected same-color group
        /// (see <see cref="GridManager"/> group scoring). The WHOLE group is
        /// rescored in full every time a placement grows it — like replaying an
        /// extended Scrabble word — so a bigger connected blob is worth more
        /// every time it's touched again, not just once.
        /// </summary>
        public const int GroupBonusPerCell = 1;

        /// <summary>Fixed bonus for filling a golden cell, independent of color. Computed separately and simply added to the total — never multiplied by group size or the group multiplier. Fires again every time the cell is part of a rescored group, not just when first placed.</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column.</summary>
        public const int LineClearBonusPerCell = 12;

        /// <summary>Multiplier contributed by EACH matching tinted cell in a placement's group — two tinted cells in the same group stack to x4, three to x8, and so on.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group bonus when the group contains a multiplier-zone cell.</summary>
        public const int MultiplierZoneMultiplier = 2;
    }
}
