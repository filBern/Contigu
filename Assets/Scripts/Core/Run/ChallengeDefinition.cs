namespace Contigu.Core
{
    /// <summary>Randomly selected special mechanic for a scheduled boss round.</summary>
    public enum BossEffect
    {
        None,
        ProgressiveCellLock,
        LockedHandSlot,
        CursedColor
    }

    /// <summary>How a challenge draws its round weather (see WeatherCatalog.RollForRun).</summary>
    public enum WeatherProfile
    {
        /// <summary>The usual mix (rarities in WeatherCatalog.GetWeight).</summary>
        Normal,

        /// <summary>Never calm: every round after the first has real weather, weighted toward the harsh and the dramatic.</summary>
        Extreme
    }

    /// <summary>
    /// Everything a challenge (see ChallengeId/ChallengeCatalog) overrides
    /// about a run's rules — RunManager reads these instead of RunConfig's
    /// own constants directly once a non-Classic challenge is chosen, so
    /// RunConfig itself stays exactly what it always was: Classic's own
    /// numbers, now also the source ChallengeCatalog.Classic is built
    /// from rather than a second copy of them.
    /// </summary>
    public sealed class ChallengeDefinition
    {
        public ChallengeId Id;
        public string Name;
        public string Description;

        /// <summary>Stars cost to unlock (see MetaStats.Stars) — 0 for Classic, which is always available.</summary>
        public int UnlockCost;

        public int RoundCount;
        public int[] Quotas;
        public int[] PieceBudgets;

        public int BossLockPiecesInterval;
        public int BossLockCellsPerInterval;

        /// <summary>False for Marathon (quota rounds with no authored enemies); every other challenge fights EncounterCatalog's enemies.</summary>
        public bool UsesEncounters = true;

        /// <summary>Side of the square grid (Greenhouse plays on a smaller one).</summary>
        public int GridSize = GridManager.DefaultSize;

        /// <summary>How many of the 4 base seed colors exist in this run (Orchard keeps only 2).</summary>
        public int ActiveColorCount = 4;

        public WeatherProfile WeatherProfile = WeatherProfile.Normal;

        /// <summary>Scales every weather bonus/penalty amount (Summer's weather hits harder).</summary>
        public float WeatherIntensity = 1f;

        /// <summary>Autumn: a fertilizer withers away once it has been held this many full rounds (0 = never).</summary>
        public int FertilizerLifespanRounds;

        /// <summary>Autumn: extra Pollen granted at the end of every cleared round.</summary>
        public int RoundEndPollenBonus;

        /// <summary>Multiplies every enemy's HP (balance lever for challenges that make the player stronger or weaker).</summary>
        public float EnemyHpMultiplier = 1f;

        /// <summary>Growth of the score target per round past the authored list (Endless on a quota challenge).</summary>
        private const double EndlessQuotaGrowth = 1.75;

        /// <summary>The score target of any round, authored or Endless (compounding past the last authored one).</summary>
        public int QuotaFor(int roundIndex)
        {
            if (roundIndex < Quotas.Length)
            {
                return Quotas[roundIndex];
            }
            double value = Quotas[Quotas.Length - 1] * System.Math.Pow(EndlessQuotaGrowth, roundIndex - (Quotas.Length - 1));
            return (int)System.Math.Min(value, 2000000000.0);
        }

        /// <summary>The seed budget of any round — Endless rounds keep the last authored budget.</summary>
        public int BudgetFor(int roundIndex)
        {
            return PieceBudgets[System.Math.Min(roundIndex, PieceBudgets.Length - 1)];
        }

        public int BossRoundIndex
        {
            get { return RoundCount - 1; }
        }
    }
}
