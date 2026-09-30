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
        /// <see cref="BossLockCellsPerInterval"/> was halved from 2 to 1 on
        /// explicit report that the boss had become too aggressive once the
        /// board shrank from 8x8 to <see cref="GridManager.Size"/> = 6x6
        /// ("Le boss ajoute trop de tuile maintenant qu'on est rendu en
        /// 6x6") — the old numbers (14 cells locked over a 22-piece boss
        /// round) ate ~22% of an 8x8 board but ~39% of the smaller 6x6 one;
        /// halving brought it down to ~19%. Still reported too hard after
        /// that ("Le boss est beaucoup trop difficile"), so
        /// <see cref="BossLockPiecesInterval"/> was widened 3 -> 5 (now
        /// matching Chaos' own pace — see ChallengeCatalog.Chaos) instead of
        /// dropping cells-per-interval below 1: only 4 ticks fit in a
        /// 22-piece boss round at this pace, ~4 cells locked (~11% of the
        /// board) instead of ~7 (~19%).
        /// </summary>
        public const int BossLockPiecesInterval = 5;
        public const int BossLockCellsPerInterval = 1;

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
        /// progression; steepened to an ACCELERATING curve (round-over-round
        /// ratio climbing from ~x1.83 to ~x2.15, instead of a constant
        /// ratio) on explicit report that the run was ending every single
        /// round with roughly half its piece budget still unused ("plus
        /// difficile, plus exponentiel peut être"). Round 1 deliberately
        /// left at the same 300 (explicit request: "on peut garder le 300
        /// points de base") — it's the only round with zero modifiers/
        /// upgrades yet, so it isn't where the game was reported to feel
        /// easy.
        ///
        /// Round 8's own ratio (37000/17200 ≈ x2.15) was the single steepest
        /// jump in this whole curve, landing on the one round that ALSO cuts
        /// the piece budget (see PieceBudgets below, 22 vs round 7's 28) and
        /// layers the boss's cell-lock on top (see BossLockPiecesInterval/
        /// BossLockCellsPerInterval) — on explicit report that the boss was
        /// still too hard even after those were eased ("Le boss est
        /// beaucoup trop difficile"), pulled back to ~x1.92 (33000), closer
        /// to round 7's own ratio (17200/8200 ≈ x2.10) instead of exceeding
        /// it, so the final round isn't ALSO the sharpest score-quota spike
        /// on top of its other two difficulty levers.
        ///
        /// NOTE for whoever tunes this next: PlayRoundToAwaitingShop (the
        /// EditMode helper backing most shop/modifier tests) does NOT need
        /// these numbers to be reachable by real placement — it fast-
        /// forwards via DebugForceRoundComplete once a round's last piece
        /// or last shuffle would otherwise risk RunManager.EvaluateRoundEnd's
        /// "ran out of budget"/stuck-hand defeat paths. An earlier version
        /// of this comment reasoned that a CI failure there meant a quota
        /// was genuinely unreachable and had to be dialed back — that
        /// turned out to be a fixed-seed RNG artifact of that one golden-
        /// cell/zero-modifier grind, not a real balance ceiling, so don't
        /// re-derive quota values from whether that specific test happens
        /// to pass.
        /// </summary>
        public static readonly int[] Quotas =
        {
            300, 550, 1050, 2000, 4000, 8200, 17200, 33000
        };

        public static readonly int[] PieceBudgets =
        {
            24, 24, 26, 26, 28, 28, 28, 22
        };
    }
}
