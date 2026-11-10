using System;

namespace Contigu.Core
{
    /// <summary>The growth stage of a held fertilizer (see <see cref="FertilizerGrowth"/>).</summary>
    public enum GrowthStage
    {
        Bud = 0,
        Bloom = 1,
        Fruit = 2
    }

    /// <summary>
    /// Maturation: a held fertilizer grows one step each round it contributed to (it scored at least once), Bud → Bloom → Fruit.
    /// Each stage adds to the fertilizer's effect factor (on top of its level, see <see cref="ModifierLevelUtility"/>). At the end of the round a
    /// Fruit is held, it is harvested: Pollen, back to Bud, and a permanent bonus that stacks with no ceiling.
    /// </summary>
    public static class FertilizerGrowth
    {
        /// <summary>Growth steps at which the fertilizer is a Bloom / a Fruit.</summary>
        public const int BloomAt = 2;
        public const int FruitAt = 3;
        public const float BloomBonus = 0.25f;
        public const float FruitBonus = 0.5f;
        /// <summary>Permanent effect bonus per harvest (no ceiling).</summary>
        public const float BonusPerHarvest = 0.10f;
        public const int HarvestPollen = 8;

        public static GrowthStage StageOf(int growth)
        {
            if (growth >= FruitAt) { return GrowthStage.Fruit; }
            if (growth >= BloomAt) { return GrowthStage.Bloom; }
            return GrowthStage.Bud;
        }

        public static float StageBonus(int growth)
        {
            switch (StageOf(growth))
            {
                case GrowthStage.Fruit: return FruitBonus;
                case GrowthStage.Bloom: return BloomBonus;
                default: return 0f;
            }
        }

        /// <summary>The extra effect factor a fertilizer earns from its growth (current stage + all its past harvests).</summary>
        public static float Bonus(int growth, int harvests)
        {
            return StageBonus(growth) + Math.Max(0, harvests) * BonusPerHarvest;
        }
    }
}
