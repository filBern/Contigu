using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Fertilizer rarity: derived from the base price, and the dearer a fertilizer, the less often it is drawn.</summary>
    public class ModifierRarityTests
    {
        [Test]
        public void EveryFertilizer_HasARarityMatchingItsPrice()
        {
            var all = ModifierCatalog.All;
            for (int i = 0; i < all.Length; i++)
            {
                int price = ModifierPricing.GetPrice(all[i].Id);
                var rarity = ModifierRarityUtility.Of(all[i].Id);
                if (price <= ModifierRarityUtility.CommonMaxPrice) { Assert.AreEqual(ModifierRarity.Common, rarity, all[i].Id.ToString()); }
                else if (price <= ModifierRarityUtility.UncommonMaxPrice) { Assert.AreEqual(ModifierRarity.Uncommon, rarity, all[i].Id.ToString()); }
                else if (price <= ModifierRarityUtility.RareMaxPrice) { Assert.AreEqual(ModifierRarity.Rare, rarity, all[i].Id.ToString()); }
                else { Assert.AreEqual(ModifierRarity.Legendary, rarity, all[i].Id.ToString()); }
            }
        }

        [Test]
        public void EveryTierExists_AndRarerMeansLessOftenDrawn()
        {
            var seen = new System.Collections.Generic.HashSet<ModifierRarity>();
            foreach (var def in ModifierCatalog.All) { seen.Add(ModifierRarityUtility.Of(def.Id)); }
            Assert.AreEqual(4, seen.Count, "all four tiers are used");
            Assert.Greater(ModifierRarityUtility.DrawWeight(ModifierRarity.Common), ModifierRarityUtility.DrawWeight(ModifierRarity.Uncommon));
            Assert.Greater(ModifierRarityUtility.DrawWeight(ModifierRarity.Uncommon), ModifierRarityUtility.DrawWeight(ModifierRarity.Rare));
            Assert.Greater(ModifierRarityUtility.DrawWeight(ModifierRarity.Rare), ModifierRarityUtility.DrawWeight(ModifierRarity.Legendary));
            Assert.Greater(ModifierRarityUtility.DrawWeight(ModifierRarity.Legendary), 0);
            Assert.AreEqual(ModifierRarity.Legendary, ModifierRarityUtility.Of(ModifierId.Tirelire));
        }
    }
}
