namespace Contigu.Core
{
    /// <summary>Reference run-structure values.</summary>
    public static class RunConfig
    {
        public const int RoundCount = 15;
        public const int BossRoundIndex = RoundCount - 1; // final scheduled boss, round 15 (0-based index 14)
        public const int BossRoundInterval = 4;

        /// <summary>
        /// Boss round mechanic: every <see cref="BossLockPiecesInterval"/>
        /// pieces played this round, the boss locks <see
        /// cref="BossLockCellsPerInterval"/> more random still-empty cells
        /// (see RunManager.ApplyBossLockTick/GridManager.LockFreeCellsAndCheckClears)
        /// — the board tightens up gradually across the whole round instead
        /// of all at once.
        /// </summary>
        public const int BossLockPiecesInterval = 5;
        public const int BossLockCellsPerInterval = 1;

        /// <summary>
        /// How many times the player can re-roll all 3 hand slots at once
        /// for the whole run — a shared per-run pool like Lueur, not reset
        /// between rounds (see RunManager.ShufflesRemaining/ShuffleHand).
        /// Also factors into the "stuck hand" defeat check (see
        /// RunManager.EvaluateRoundEnd): a hand with no legal placement is
        /// only a loss once shuffles are also exhausted.
        /// </summary>
        public const int StartingShuffleCount = 10;

        /// <summary>
        /// Round score targets, on an accelerating curve (round-over-round
        /// ratio climbing rather than a constant ratio).
        ///
        /// Note for whoever tunes this next: PlayRoundToAwaitingShop (the
        /// EditMode helper backing most shop/modifier tests) does not need
        /// these numbers to be reachable by real placement — it fast-
        /// forwards via DebugForceRoundComplete once a round's last piece
        /// or last shuffle would otherwise risk RunManager.EvaluateRoundEnd's
        /// "ran out of budget"/stuck-hand defeat paths.
        ///
        /// Classic's own win condition no longer reads these at all past
        /// round 8 — EncounterCatalog now authors every one of its 15
        /// rounds, so <see cref="RunManager.HasActiveEncounter"/> is true
        /// throughout and <see cref="RunManager.EvaluateRoundEnd"/> checks
        /// "every enemy dead" instead of "quota reached" — but Marathon/
        /// Chaos (no authored encounters) still race these numbers
        /// directly, and Classic/Chaos still read them for display/
        /// DebugForceRoundComplete.
        /// </summary>
        public static readonly int[] Quotas =
        {
            300, 550, 1050, 2000, 4000, 8200, 17200, 33000,
            60000, 115000, 225000, 440000, 850000, 1600000, 2900000
        };

        public static readonly int[] PieceBudgets =
        {
            24, 24, 26, 26, 28, 28, 28, 22,
            28, 28, 28, 30, 30, 32, 34
        };
    }
}
