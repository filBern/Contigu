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
        /// <summary>Spring: mild weather, a rare late frost or storm.</summary>
        Normal,

        /// <summary>Winter: frost, snow and blizzards; no rain, heat or pollen.</summary>
        Winter,

        /// <summary>Autumn: rain, wind and low golden light, the odd first frost or Indian summer.</summary>
        Autumn,

        /// <summary>Orchard: blossom showers and mild days, a risky late frost.</summary>
        Orchard,

        /// <summary>Greenhouse: indoors, so the weather is sprinklers, grow lamps and a cat.</summary>
        Greenhouse,

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

        public int RoundCount;
        public int[] Quotas;
        public int[] PieceBudgets;

        public int BossLockPiecesInterval;
        public int BossLockCellsPerInterval;

        /// <summary>Whether the rounds are fought against EncounterCatalog's enemies (true for every real challenge); false only for quota-only rules some tests build with <see cref="Clone"/>.</summary>
        public bool UsesEncounters = true;

        /// <summary>Side of the square grid (Greenhouse plays on a smaller one).</summary>
        public int GridSize = GridManager.DefaultSize;

        /// <summary>How many of the 4 base seed colors exist in this run (Orchard keeps only 2).</summary>
        public int ActiveColorCount = 4;

        /// <summary>This challenge's own resident enemy (null for Spring): it joins every third round's roster (see EncounterCatalog.GetEncounter).</summary>
        public EnemyId? WorldEnemy;

        /// <summary>
        /// Which pests this challenge swaps for others in the authored rosters (the leading Basic is never touched): it is what makes each
        /// challenge meet its own enemies instead of the same ones as Spring. Empty for Spring.
        /// </summary>
        public EnemySwap[] EnemySwaps = System.Array.Empty<EnemySwap>();

        public WeatherProfile WeatherProfile = WeatherProfile.Normal;

        /// <summary>Scales every weather bonus/penalty amount (Summer's weather hits harder).</summary>
        public float WeatherIntensity = 1f;

        /// <summary>Autumn: a fertilizer withers away once it has been held this many full rounds (0 = never).</summary>
        public int FertilizerLifespanRounds;

        /// <summary>Autumn: extra Pollen granted at the end of every cleared round.</summary>
        public int RoundEndPollenBonus;

        /// <summary>Multiplies every enemy's HP (balance lever for challenges that make the player stronger or weaker).</summary>
        public float EnemyHpMultiplier = 1f;

        /// <summary>Extra seeds added to every round's budget.</summary>
        public int PieceBudgetBonus;

        /// <summary>The difficulty level this definition was built for (1 = Normal, see <see cref="DifficultyLevels"/>).</summary>
        public int Level = 1;

        /// <summary>Shuffles added to (negative: taken from) the run's starting Shuffles.</summary>
        public int ShuffleDelta;

        /// <summary>Pollen added to every fertilizer's price in the shop.</summary>
        public int FertilizerPriceDelta;

        /// <summary>Multiplies only Act 1's enemies (rounds 1-8), on top of <see cref="EnemyHpMultiplier"/> — eases the early filter without softening the finale.</summary>
        public float ActOneHpMultiplier = 1f;

        /// <summary>Multiplies the enemies from round <see cref="ScoringConstants.LateHpFirstRoundIndex"/> + 1 on, on top of <see cref="EnemyHpMultiplier"/>: the fertilizers' Maturation and Roots build up over the rounds, so the answer to their power belongs to the mid-game, not to the opening filter.</summary>
        public float LateHpMultiplier = 1f;

        /// <summary>Multiplies every round's score target (quota challenges: Winter).</summary>
        public float QuotaMultiplier = 1f;

        /// <summary>Growth of the score target per round past the authored list (Endless on a quota challenge).</summary>
        private const double EndlessQuotaGrowth = 1.75;

        /// <summary>The score target of any round, authored or Endless (compounding past the last authored one).</summary>
        public int QuotaFor(int roundIndex)
        {
            double value = roundIndex < Quotas.Length
                ? Quotas[roundIndex]
                : Quotas[Quotas.Length - 1] * System.Math.Pow(EndlessQuotaGrowth, roundIndex - (Quotas.Length - 1));
            return (int)System.Math.Min(value * QuotaMultiplier, 2000000000.0);
        }

        /// <summary>The seed budget of any round — Endless rounds keep the last authored budget.</summary>
        public int BudgetFor(int roundIndex)
        {
            return PieceBudgets[System.Math.Min(roundIndex, PieceBudgets.Length - 1)] + PieceBudgetBonus;
        }

        /// <summary>A shallow copy (arrays shared) — lets a test tweak one rule of a catalog challenge without touching the catalog.</summary>
        public ChallengeDefinition Clone()
        {
            return (ChallengeDefinition)MemberwiseClone();
        }

        /// <summary>
        /// The same challenge at difficulty <paramref name="level"/> (1 to <see cref="DifficultyLevels.Max"/>): each level adds a rule to the
        /// previous ones (see <see cref="DifficultyLevels"/>). Level 1 returns the definition itself.
        /// </summary>
        public ChallengeDefinition AtLevel(int level)
        {
            level = System.Math.Max(1, System.Math.Min(DifficultyLevels.Max, level));
            if (level == 1)
            {
                return this;
            }
            var harder = Clone();
            harder.Level = level;
            harder.EnemyHpMultiplier = EnemyHpMultiplier * 1.15f;
            if (level >= 3) { harder.ShuffleDelta = ShuffleDelta - 1; }
            if (level >= 4)
            {
                harder.EnemyHpMultiplier *= 1.15f;
                harder.PieceBudgetBonus = PieceBudgetBonus - 1;
            }
            if (level >= 5) { harder.FertilizerPriceDelta = FertilizerPriceDelta + 1; }
            return harder;
        }

        public int BossRoundIndex
        {
            get { return RoundCount - 1; }
        }
    }

    /// <summary>One substitution of a pest for another in a challenge's rosters (see <see cref="ChallengeDefinition.EnemySwaps"/>).</summary>
    public struct EnemySwap
    {
        public readonly EnemyId From;
        public readonly EnemyId To;

        public EnemySwap(EnemyId from, EnemyId to)
        {
            From = from;
            To = to;
        }
    }
}
