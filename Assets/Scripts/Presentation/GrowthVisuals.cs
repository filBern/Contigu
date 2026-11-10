using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>The Maturation medallions (sprout / flower / fruit) and the Fruit's halo.</summary>
    public static class GrowthVisuals
    {
        public static Sprite StageSprite(GrowthStage stage)
        {
            string name = stage == GrowthStage.Fruit ? "Stage_Fruit" : stage == GrowthStage.Bloom ? "Stage_Bloom" : "Stage_Bud";
            return Resources.Load<Sprite>("Icons/Growth/" + name);
        }

        public static string StageName(GrowthStage stage, int growth)
        {
            string name = stage == GrowthStage.Fruit ? "Fruit" : stage == GrowthStage.Bloom ? "Bloom" : "Bud";
            return name + " (" + System.Math.Min(growth, FertilizerGrowth.FruitAt) + "/" + FertilizerGrowth.FruitAt + ")";
        }

        /// <summary>What the growth icon means: how a fertilizer grows, what each stage gives, and what a harvest pays.</summary>
        public static string StageBody()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return "A fertilizer grows one step at the end of each round it scored in.\n"
                + "Bloom (" + FertilizerGrowth.BloomAt + " steps): +" + FertilizerGrowth.BloomBonus.ToString("0.##", inv) + " effect.\n"
                + "Fruit (" + FertilizerGrowth.FruitAt + " steps): +" + FertilizerGrowth.FruitBonus.ToString("0.##", inv) + " effect, harvested at the end of the next round: +"
                + FertilizerGrowth.HarvestPollen + " Pollen, back to a Bud, and +" + FertilizerGrowth.BonusPerHarvest.ToString("0.##", inv) + " effect for good.";
        }

        public static Sprite FruitGlow
        {
            get { return Resources.Load<Sprite>("Icons/Growth/Fruit_Glow"); }
        }
    }
}
