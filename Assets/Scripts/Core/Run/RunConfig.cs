namespace Contigu.Core
{
    /// <summary>Reference run-structure values from spec section 6.</summary>
    public static class RunConfig
    {
        public const int RoundCount = 8;
        public const int BossRoundIndex = RoundCount - 1; // round 8 (0-based index 7)
        public const int BossLockedCellCount = 14;

        public static readonly int[] Quotas =
        {
            300, 450, 650, 900, 1200, 1550, 1950, 2500
        };

        public static readonly int[] PieceBudgets =
        {
            24, 24, 26, 26, 28, 28, 28, 22
        };
    }
}
