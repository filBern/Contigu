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
        /// Round score targets. Was a flat ~x1.7-per-round geometric
        /// progression; steepened to an ACCELERATING curve — round-over-
        /// round ratio climbing from ~x1.83 to ~x2.15 rather than a constant
        /// ratio — on explicit report that the run was ending every single
        /// round with roughly half its piece budget still unused, i.e. the
        /// old curve simply wasn't keeping pace with how much placement
        /// totals grow as the player stacks modifiers ("plus difficile,
        /// plus exponentiel peut être"). Round 1 deliberately left at the
        /// same 300 (explicit request: "on peut garder le 300 points de
        /// base") — it's the only round with zero modifiers/upgrades yet,
        /// so it isn't where the game was reported to feel easy; every
        /// round after it climbs meaningfully harder than before.
        /// </summary>
        public static readonly int[] Quotas =
        {
            300, 550, 1050, 2000, 4000, 8200, 17200, 37000
        };

        public static readonly int[] PieceBudgets =
        {
            24, 24, 26, 26, 28, 28, 28, 22
        };
    }
}
