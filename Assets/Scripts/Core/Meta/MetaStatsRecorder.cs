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
        /// <summary>
        /// Folds one run's outcome into the running totals. Every field not explicitly updated here must still be copied forward from
        /// <paramref name="current"/> (Copy does it), or recording a run would silently reset the unlocks. <paramref name="roundReached"/> is
        /// RunConfig.RoundCount on victory, CurrentRoundNumber (1-based) on defeat. A victory also records the difficulty level it was won at.
        /// </summary>
        public static MetaStats RecordRunOutcome(MetaStats current, int finalScore, int roundReached, bool victory, ChallengeId challenge = ChallengeId.Classic, int level = 1)
        {
            var updated = current.Copy();
            updated.TotalRunsPlayed = current.TotalRunsPlayed + 1;
            updated.TotalVictories = current.TotalVictories + (victory ? 1 : 0);
            updated.BestScore = System.Math.Max(current.BestScore, finalScore);
            updated.BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached);
            updated.ClassicVictory = current.ClassicVictory || (victory && challenge == ChallengeId.Classic);
            if (victory)
            {
                int index = (int)challenge;
                if (updated.BestLevelWon == null || updated.BestLevelWon.Length <= index)
                {
                    var grown = new int[System.Math.Max(8, index + 1)];
                    if (updated.BestLevelWon != null) { updated.BestLevelWon.CopyTo(grown, 0); }
                    updated.BestLevelWon = grown;
                }
                updated.BestLevelWon[index] = System.Math.Max(updated.BestLevelWon[index], level);
            }
            return updated;
        }

        /// <summary>Stores the unlock state in the stats (a pure fold: <paramref name="current"/> is not modified).</summary>
        public static MetaStats WithUnlocks(MetaStats current, UnlockState unlocks)
        {
            var updated = current.Copy();
            updated.UnlockData = unlocks.ToText();
            return updated;
        }

        /// <summary>Everything unlocked, records untouched — the dev/testing shortcut (see GameBootstrap's F10 and the picker's "Unlock all" button).</summary>
        public static MetaStats UnlockAll(MetaStats current)
        {
            var unlocks = current.ReadUnlocks();
            unlocks.UnlockEverything();
            var updated = WithUnlocks(current, unlocks);
            updated.ClassicVictory = true;
            return updated;
        }

        /// <summary>
        /// Endless: a run that already counted as a victory (see <see cref="RecordRunOutcome"/>) kept going past the final round and has now
        /// ended in round <paramref name="roundReached"/>. Refreshes the best score and round, but never counts the run (or its victory) a second time.
        /// </summary>
        public static MetaStats RecordEndlessEnd(MetaStats current, int finalScore, int roundReached)
        {
            var updated = current.Copy();
            updated.BestScore = System.Math.Max(current.BestScore, finalScore);
            updated.BestRoundReached = System.Math.Max(current.BestRoundReached, roundReached);
            return updated;
        }
    }
}
