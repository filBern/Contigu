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

        /// <summary>Fixed bonus for filling a golden cell, independent of color. Computed separately and simply added to the total — never multiplied by group size or the group multiplier.</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column.</summary>
        public const int LineClearBonusPerCell = 12;

        /// <summary>Multiplier applied to a placement's whole group bonus when the group contains a matching tinted cell.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group bonus when the group contains a multiplier-zone cell.</summary>
        public const int MultiplierZoneMultiplier = 2;
    }
}
