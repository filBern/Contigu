namespace Contigu.Core
{
    /// <summary>
    /// Reference scoring values from spec sections 3.2 and 3.3. Kept as a single
    /// tunable surface so balancing doesn't require touching game logic.
    /// </summary>
    public static class ScoringConstants
    {
        /// <summary>Points awarded per matching-color neighbor pair (spec 3.2).</summary>
        public const int NeighborBonusPerPair = 6;

        /// <summary>Fixed bonus for filling a golden cell, independent of color (spec 3.2).</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column (spec 3.3).</summary>
        public const int LineClearBonusPerCell = 12;

        /// <summary>Multiplier applied to a placed cell's neighbor bonus when tinted-color matches.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placed cell's neighbor bonus when inside a multiplier zone.</summary>
        public const int MultiplierZoneMultiplier = 2;
    }
}
