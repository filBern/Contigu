namespace Contigu.Core
{
    /// <summary>
    /// Pure update logic for folding one run's outcome into the running
    /// MetaStats totals — kept free of any file I/O (see IMetaStatsStore)
    /// so it's unit-testable without touching disk, the same reason
    /// SystemRandomProvider stays plain System.Random rather than
    /// something UnityEngine-specific.
    /// </summary>
    public static class MetaStatsRecorder
    {
        /// <summary>+1 Star per round actually cleared, on top of a flat bonus for a true victory — on explicit request ("Meta progression avec différents challenge..."): Stars are what unlocks Marathon/Chaos (see ChallengeCatalog/TryUnlockChallenge below), earned by playing rather than bought.</summary>
        private const int VictoryBonusStars = 2;

        /// <summary>
        /// Folds one run's outcome into the running totals, INCLUDING the
        /// Stars/unlock fields added for challenge unlocks — every field
        /// not explicitly updated here must still be copied forward from
        /// <paramref name="current"/>, or recording a run would silently
        /// reset a player's Stars balance and unlocked challenges back to
        /// zero/locked. <paramref name="roundReached"/> follows the same
        /// convention the caller (GameBootstrap) already used before Stars
        /// existed: RunConfig.RoundCount (8) on victory, CurrentRoundNumber
        /// (1-based) on defeat — so "rounds actually cleared" is
        /// <paramref name="roundReached"/> itself on victory (every round
        /// including the boss was cleared) or <paramref name="roundReached"/>
        /// - 1 on defeat (the round you died in was never cleared).
        /// </summary>
        public static MetaStats RecordRunOutcome(MetaStats current, int finalScore, int roundReached, bool victory)
        {
            int roundsCleared = victory ? roundReached : roundReached - 1;
            int starsEarned = roundsCleared + (victory ? VictoryBonusStars : 0);

            return new MetaStats
            {
                TotalRunsPlayed = current.TotalRunsPlayed + 1,
                TotalVictories = current.TotalVictories + (victory ? 1 : 0),
                BestScore = System.Math.Max(current.BestScore, finalScore),
                BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached),
                Stars = current.Stars + starsEarned,
                MarathonUnlocked = current.MarathonUnlocked,
                ChaosUnlocked = current.ChaosUnlocked
            };
        }

        /// <summary>
        /// Spends Stars to unlock <paramref name="challenge"/> — a no-op
        /// success (nothing charged) if it's already unlocked, a no-op
        /// failure if the player can't afford it. Never mutates <paramref
        /// name="current"/>, same pure-fold convention as RecordRunOutcome.
        /// </summary>
        public static (MetaStats Updated, bool Success) TryUnlockChallenge(MetaStats current, ChallengeDefinition challenge)
        {
            if (current.IsUnlocked(challenge.Id))
            {
                return (current, true);
            }
            if (current.Stars < challenge.UnlockCost)
            {
                return (current, false);
            }

            var updated = new MetaStats
            {
                TotalRunsPlayed = current.TotalRunsPlayed,
                TotalVictories = current.TotalVictories,
                BestScore = current.BestScore,
                BestRoundReached = current.BestRoundReached,
                Stars = current.Stars - challenge.UnlockCost,
                MarathonUnlocked = current.MarathonUnlocked,
                ChaosUnlocked = current.ChaosUnlocked
            };

            switch (challenge.Id)
            {
                case ChallengeId.Marathon:
                    updated.MarathonUnlocked = true;
                    break;
                case ChallengeId.Chaos:
                    updated.ChaosUnlocked = true;
                    break;
            }

            return (updated, true);
        }
    }
}
