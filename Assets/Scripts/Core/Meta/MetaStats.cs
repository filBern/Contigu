namespace Contigu.Core
{
    /// <summary>
    /// Persistent, cross-run stats: records and the progression state (what is unlocked, see <see cref="UnlockState"/>, and how hard each
    /// challenge has been won). Plain public-field data so Unity's JsonUtility can (de)serialize it directly; see <see cref="IMetaStatsStore"/>
    /// for how it's actually persisted.
    /// </summary>
    [System.Serializable]
    public sealed class MetaStats
    {
        /// <summary>The save format. A save of another version is dropped (the game is not out yet, nothing is migrated).</summary>
        public const int CurrentVersion = 2;

        public int SaveVersion;
        public int TotalRunsPlayed;
        public int TotalVictories;
        public int BestScore;

        /// <summary>Highest round NUMBER (1-based, see RunManager.CurrentRoundNumber) ever reached across every run — at least RunConfig.RoundCount on any past victory, since clearing the final round means reaching it.</summary>
        public int BestRoundReached;

        /// <summary>The unlocked sprouts, fertilizers and challenges, and the totals their conditions count (see <see cref="UnlockState.ToText"/>).</summary>
        public string UnlockData = string.Empty;

        /// <summary>Per challenge (indexed by <see cref="ChallengeId"/>), the highest difficulty level (1 to <see cref="DifficultyLevels.Max"/>) won so far; 0 when never won.</summary>
        public int[] BestLevelWon = new int[8];

        /// <summary>The default challenge (Spring) has been won at least once: unlocks the optional pledges.</summary>
        public bool ClassicVictory;

        /// <summary>A field-by-field copy, so a pure update never forgets a field it didn't touch.</summary>
        public MetaStats Copy()
        {
            var copy = (MetaStats)MemberwiseClone();
            copy.BestLevelWon = BestLevelWon == null ? new int[8] : (int[])BestLevelWon.Clone();
            return copy;
        }

        /// <summary>The unlock state saved here (a fresh one when nothing was saved yet).</summary>
        public UnlockState ReadUnlocks()
        {
            return UnlockState.FromText(UnlockData);
        }

        /// <summary>Classic is always open; every other challenge needs its own condition (see <see cref="UnlockCatalog"/>).</summary>
        public bool IsUnlocked(ChallengeId id)
        {
            return ReadUnlocks().IsUnlocked(UnlockKind.Challenge, (int)id);
        }

        /// <summary>The highest difficulty level won on <paramref name="id"/>; 0 when it was never won.</summary>
        public int BestLevel(ChallengeId id)
        {
            int index = (int)id;
            return BestLevelWon != null && index >= 0 && index < BestLevelWon.Length ? BestLevelWon[index] : 0;
        }

        /// <summary>The highest level the player may pick on <paramref name="id"/>: one more than the best level won, up to the maximum.</summary>
        public int HighestPlayableLevel(ChallengeId id)
        {
            return System.Math.Min(DifficultyLevels.Max, BestLevel(id) + 1);
        }
    }
}
