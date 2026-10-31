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
        /// <summary>+1 Star per round actually cleared, on top of a flat bonus for a true victory — Stars are what unlocks the other challenges (see ChallengeCatalog/TryUnlockChallenge below), earned by playing rather than bought.</summary>
        private const int VictoryBonusStars = 2;

        /// <summary>
        /// Folds one run's outcome into the running totals, INCLUDING the
        /// Stars/unlock fields added for challenge unlocks — every field
        /// not explicitly updated here must still be copied forward from
        /// <paramref name="current"/>, or recording a run would silently
        /// reset a player's Stars balance and unlocked challenges back to
        /// zero/locked. <paramref name="roundReached"/> follows the same
        /// convention the caller (GameBootstrap) already used before Stars
        /// existed: RunConfig.RoundCount on victory, CurrentRoundNumber
        /// (1-based) on defeat — so "rounds actually cleared" is
        /// <paramref name="roundReached"/> itself on victory (every round
        /// including the boss was cleared) or <paramref name="roundReached"/>
        /// - 1 on defeat (the round you died in was never cleared).
        /// </summary>
        public static MetaStats RecordRunOutcome(MetaStats current, int finalScore, int roundReached, bool victory)
        {
            int roundsCleared = victory ? roundReached : roundReached - 1;
            int starsEarned = roundsCleared + (victory ? VictoryBonusStars : 0);

            var updated = current.Copy();
            updated.TotalRunsPlayed = current.TotalRunsPlayed + 1;
            updated.TotalVictories = current.TotalVictories + (victory ? 1 : 0);
            updated.BestScore = System.Math.Max(current.BestScore, finalScore);
            updated.BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached);
            updated.Stars = current.Stars + starsEarned;
            return updated;
        }

        /// <summary>Every challenge unlocked, Stars untouched — the dev/testing shortcut (see GameBootstrap's F10 and the picker's "Unlock all" button).</summary>
        public static MetaStats UnlockAllChallenges(MetaStats current)
        {
            var updated = current.Copy();
            updated.MarathonUnlocked = true;
            updated.SummerUnlocked = true;
            updated.AutumnUnlocked = true;
            updated.OrchardUnlocked = true;
            updated.GreenhouseUnlocked = true;
            return updated;
        }

        /// <summary>
        /// Endless: a run that already counted as a victory (see <see cref="RecordRunOutcome"/>) kept going past the final round and has now
        /// ended in round <paramref name="roundReached"/>. Adds one Star per extra round cleared and refreshes the best score and round, but
        /// never counts the run (or its victory) a second time.
        /// </summary>
        public static MetaStats RecordEndlessEnd(MetaStats current, int finalScore, int roundReached)
        {
            int extraRoundsCleared = System.Math.Max(0, roundReached - 1 - RunConfig.RoundCount);
            var updated = current.Copy();
            updated.BestScore = System.Math.Max(current.BestScore, finalScore);
            updated.BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached);
            updated.Stars = current.Stars + extraRoundsCleared;
            return updated;
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

            var updated = current.Copy();
            updated.Stars = current.Stars - challenge.UnlockCost;

            switch (challenge.Id)
            {
                case ChallengeId.Marathon:
                    updated.MarathonUnlocked = true;
                    break;
                case ChallengeId.Summer:
                    updated.SummerUnlocked = true;
                    break;
                case ChallengeId.Autumn:
                    updated.AutumnUnlocked = true;
                    break;
                case ChallengeId.Orchard:
                    updated.OrchardUnlocked = true;
                    break;
                case ChallengeId.Greenhouse:
                    updated.GreenhouseUnlocked = true;
                    break;
            }

            return (updated, true);
        }
    }
}
