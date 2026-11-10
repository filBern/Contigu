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

        /// <summary>The line a fertilizer's tooltip shows about its growth: its stage and progress, and what its harvests gave for good.</summary>
        public static string TooltipLine(int growth, int harvests)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var stage = FertilizerGrowth.StageOf(growth);
            string line;
            switch (stage)
            {
                case GrowthStage.Fruit:
                    line = "FRUIT: +" + FertilizerGrowth.FruitBonus.ToString("0.##", inv) + " effect, harvested at the end of the round (+" + FertilizerGrowth.HarvestPollen + " Pollen).";
                    break;
                case GrowthStage.Bloom:
                    line = "Bloom: +" + FertilizerGrowth.BloomBonus.ToString("0.##", inv) + " effect. Grows each round it scores (" + growth + "/" + FertilizerGrowth.FruitAt + ").";
                    break;
                default:
                    line = "Bud. Grows each round it scores (" + growth + "/" + FertilizerGrowth.FruitAt + ").";
                    break;
            }
            if (harvests > 0)
            {
                line += "\nHarvested x" + harvests + ": +" + (harvests * FertilizerGrowth.BonusPerHarvest).ToString("0.##", inv) + " effect for good.";
            }
            return line;
        }

        /// <summary>The stage medallion (sprout / flower / fruit) at the foot of a badge; a Fruit also glows behind the sack.</summary>
        public static void AttachStage(UnityEngine.UI.Image badge, int growth, float medallionSize, float glowSize)
        {
            var stage = FertilizerGrowth.StageOf(growth);
            var medallion = UIFactory.CreatePanel(badge.transform, "StageMedallion", Color.white);
            medallion.raycastTarget = false;
            medallion.preserveAspect = true;
            medallion.sprite = StageSprite(stage);
            medallion.enabled = medallion.sprite != null;
            medallion.rectTransform.anchorMin = medallion.rectTransform.anchorMax = new Vector2(0f, 0f);
            medallion.rectTransform.pivot = new Vector2(0f, 0f);
            medallion.rectTransform.anchoredPosition = new Vector2(1f, 1f);
            medallion.rectTransform.sizeDelta = new Vector2(medallionSize, medallionSize);
            if (stage == GrowthStage.Fruit)
            {
                var glow = UIFactory.CreatePanel(badge.transform, "FruitGlow", new Color(1f, 1f, 1f, 0.8f));
                glow.raycastTarget = false;
                glow.sprite = FruitGlow;
                glow.enabled = glow.sprite != null;
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                glow.rectTransform.anchoredPosition = Vector2.zero;
                glow.rectTransform.sizeDelta = new Vector2(glowSize, glowSize);
                glow.transform.SetAsFirstSibling();
                glow.gameObject.AddComponent<GrowthGlowPulse>();
            }
        }

        public static Sprite FruitGlow
        {
            get { return Resources.Load<Sprite>("Icons/Growth/Fruit_Glow"); }
        }
    }
}
