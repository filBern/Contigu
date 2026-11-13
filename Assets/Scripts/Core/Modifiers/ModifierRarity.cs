namespace Contigu.Core
{
    /// <summary>
    /// How often a fertilizer is offered, and how dear it is to its owner: Common, Uncommon, Rare and Legendary. Derived from the fertilizer's base price (see <see cref="ModifierPricing"/>), so rarity and price never disagree.
    /// </summary>
    public enum ModifierRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public static class ModifierRarityUtility
    {
        /// <summary>Base prices up to this are Common; above it up to <see cref="UncommonMaxPrice"/> Uncommon; dearer ones Rare.</summary>
        public const int CommonMaxPrice = 5;
        public const int UncommonMaxPrice = 7;
        /// <summary>Dearer than <see cref="UncommonMaxPrice"/> up to this is Rare; dearer still is Legendary.</summary>
        public const int RareMaxPrice = 9;

        public static ModifierRarity Of(ModifierId id)
        {
            int price = ModifierPricing.GetPrice(id);
            if (price <= CommonMaxPrice) { return ModifierRarity.Common; }
            if (price <= UncommonMaxPrice) { return ModifierRarity.Uncommon; }
            return price <= RareMaxPrice ? ModifierRarity.Rare : ModifierRarity.Legendary;
        }

        /// <summary>Share of the usual draw weight each tier gets (Blister and Fertilizer Pack).</summary>
        public static int DrawWeight(ModifierRarity rarity)
        {
            switch (rarity)
            {
                case ModifierRarity.Common: return EconomyConstants.BlisterModifierWeight;
                case ModifierRarity.Uncommon: return EconomyConstants.BlisterModifierWeight * 11 / 16;
                case ModifierRarity.Rare: return EconomyConstants.BlisterModifierWeight * 6 / 16;
                default: return EconomyConstants.BlisterModifierWeight / 4;
            }
        }
    }
}
