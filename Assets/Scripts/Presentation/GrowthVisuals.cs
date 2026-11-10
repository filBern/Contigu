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

        public static Sprite FruitGlow
        {
            get { return Resources.Load<Sprite>("Icons/Growth/Fruit_Glow"); }
        }
    }
}
