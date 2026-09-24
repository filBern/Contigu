namespace Contigu.Core
{
    /// <summary>Reference run-structure values from spec section 6.</summary>
    public static class RunConfig
    {
        public const int RoundCount = 8;
        public const int BossRoundIndex = RoundCount - 1; // round 8 (0-based index 7)

        /// <summary>
        /// Boss round mechanic (replaces the old upfront 14-cell lock at
        /// round start — judged too hard on explicit request: "le boss est
        /// beaucoup trop difficile, on va faire autre chose"): every
        /// <see cref="BossLockPiecesInterval"/> pieces played this round,
        /// the boss locks <see cref="BossLockCellsPerInterval"/> more random
        /// still-empty cells (see RunManager.ApplyBossLockTick /
        /// GridManager.LockFreeCellsAndCheckClears) — the board tightens up
        /// gradually across the whole round instead of all at once.
        /// </summary>
        public const int BossLockPiecesInterval = 3;
        public const int BossLockCellsPerInterval = 2;

        /// <summary>
        /// How many times the player can re-roll all 3 hand slots at once
        /// for the whole run (spec extension, explicit request: "un bouton
        /// shuffle qui permet de shuffle les 3 slots de pièce au hasard. Le
        /// joueur a droit à 10 shuffle") — a shared per-run pool like
        /// Lueur, not reset between rounds (see RunManager.ShufflesRemaining
        /// / ShuffleHand). Also factors into the "stuck hand" defeat check
        /// (see RunManager.EvaluateRoundEnd): a hand with no legal placement
        /// is only a loss once shuffles are ALSO exhausted, since a shuffle
        /// might turn up a playable hand.
        /// </summary>
        public const int StartingShuffleCount = 10;

        /// <summary>
        /// Round score targets — a geometric progression (~x1.7 per round)
        /// rather than the previous roughly-quadratic one, on explicit
        /// request to match how much bigger placement totals can now get
        /// once several of the 24 modifiers converted to "xN multiplier"
        /// (see ScoringConstants/PlacementResult.ModifierMultiplier) stack
        /// together on the same placement — a couple of those compounding
        /// multiplicatively with GroupMultiplier/ComboMultiplier can already
        /// dwarf the old late-round targets, so those needed to climb
        /// exponentially too or the back half of a run would stop being any
        /// kind of challenge.
        /// </summary>
        public static readonly int[] Quotas =
        {
            300, 500, 850, 1450, 2450, 4150, 7050, 12000
        };

        public static readonly int[] PieceBudgets =
        {
            24, 24, 26, 26, 28, 28, 28, 22
        };
    }
}
