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
            return name + " " + System.Math.Min(growth, FertilizerGrowth.FruitAt) + "/" + FertilizerGrowth.FruitAt;
        }

        /// <summary>The growth tooltip's text: short, one idea per line; the effect is said in the unit the fertilizer counts in (Mult red, pts blue, Pollen gold).</summary>
        public static string TooltipLine(int growth, int harvests, string unit)
        {
            string line;
            switch (FertilizerGrowth.StageOf(growth))
            {
                case GrowthStage.Fruit:
                    line = DescriptionTextFormatter.Amount(FertilizerGrowth.FruitBonus, unit) + "\n"
                        + DescriptionTextFormatter.Colorize("Harvested at the end of the round: +" + FertilizerGrowth.HarvestPollen + " Pollen.", 14);
                    break;
                case GrowthStage.Bloom:
                    line = DescriptionTextFormatter.Amount(FertilizerGrowth.BloomBonus, unit) + "\nFruit after one more round it scores.";
                    break;
                default:
                    line = "Grows each round it scores.";
                    break;
            }
            if (harvests > 0)
            {
                line += "\nHarvested x" + harvests + ": " + DescriptionTextFormatter.Amount(harvests * FertilizerGrowth.BonusPerHarvest, unit) + " for good.";
            }
            return line;
        }

        /// <summary>The stage medallion (sprout / flower / fruit) at the foot of a badge, with its own tooltip when a <paramref name="tooltip"/> is given; a Fruit also glows behind the sack.</summary>
        public static void AttachStage(UnityEngine.UI.Image badge, int growth, int harvests, float medallionSize, float glowSize, TooltipView tooltip = null, string unit = "effect")
        {
            var stage = FertilizerGrowth.StageOf(growth);
            var medallion = UIFactory.CreatePanel(badge.transform, "StageMedallion", Color.white);
            // The growth has a tooltip of its own, apart from the fertilizer's (hover or touch on the medallion). Pointer-downs and drags it does not handle bubble up to the sack.
            medallion.raycastTarget = tooltip != null;
            if (tooltip != null)
            {
                medallion.gameObject.AddComponent<HoverTooltip>().Init(tooltip, StageName(stage, growth), TooltipLine(growth, harvests, unit));
            }
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
