using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Covers ModifierPricing (on explicit request: "les modifiers ne soient pas tous le même prix, en fonction de leur rareté et de leur puissance (entre 4 et 10)").</summary>
    public class ModifierPricingTests
    {
        [Test]
        public void GetPrice_CoversEveryCatalogEntry_WithAValidPrice()
        {
            var all = ModifierCatalog.All;
            for (int i = 0; i < all.Length; i++)
            {
                int price = ModifierPricing.GetPrice(all[i].Id);
                Assert.GreaterOrEqual(price, 4, all[i].Id + " should have a price of at least 4");
                int ceiling = EconomyConstants.ModifierDrawWeight(all[i].Id) < EconomyConstants.BlisterModifierWeight ? 12 : 10; // the rare ones are dearer
                Assert.LessOrEqual(price, ceiling, all[i].Id + " should have a price of at most " + ceiling);
            }
        }

        [Test]
        public void PiggyBank_IsDearAndDrawnAQuarterAsOften()
        {
            Assert.AreEqual(12, ModifierPricing.GetPrice(ModifierId.Tirelire));
            Assert.AreEqual(EconomyConstants.BlisterModifierWeight / 4, EconomyConstants.ModifierDrawWeight(ModifierId.Tirelire));
            Assert.AreEqual(EconomyConstants.BlisterModifierWeight, EconomyConstants.ModifierDrawWeight(ModifierId.Chaine));
            // Offered about a quarter as often as an ordinary fertilizer in the Blister.
            int piggy = 0;
            int plain = 0;
            for (int seed = 1; seed <= 4000; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed * 7919));
                run.DebugForceRoundComplete();
                foreach (var slot in run.ShopBlisterSlots)
                {
                    if (slot != null && slot.Kind == ShopSlotKind.Modifier)
                    {
                        if (slot.ModifierId == ModifierId.Tirelire) { piggy++; }
                        else if (slot.ModifierId == ModifierId.Chaine) { plain++; }
                    }
                }
            }
            Assert.Greater(plain, 0);
            Assert.Less(piggy, plain, "rarer than an ordinary fertilizer");
        }

        [Test]
        public void GetPrice_IsNotFlat_AcrossTheCatalog()
        {
            var all = ModifierCatalog.All;
            int firstPrice = ModifierPricing.GetPrice(all[0].Id);
            bool foundDifferentPrice = false;
            for (int i = 1; i < all.Length; i++)
            {
                if (ModifierPricing.GetPrice(all[i].Id) != firstPrice)
                {
                    foundDifferentPrice = true;
                    break;
                }
            }
            Assert.IsTrue(foundDifferentPrice, "Modifiers should no longer all share the same flat price");
        }

        [Test]
        public void GetPrice_TheStrongestUnconditionalEffectsAreThePriciest()
        {
            // Mult +4 (unconditional) should cost more than Mult +1 (same
            // effect, weaker) — a basic sanity check that price tracks
            // power within a clearly-ordered family.
            Assert.Less(ModifierPricing.GetPrice(ModifierId.MultUn), ModifierPricing.GetPrice(ModifierId.MultDeux));
            Assert.Less(ModifierPricing.GetPrice(ModifierId.MultDeux), ModifierPricing.GetPrice(ModifierId.MultQuatre));
        }
    }
}
