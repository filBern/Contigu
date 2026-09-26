namespace Contigu.Core
{
    public enum ShopSlotKind
    {
        Modifier,
        Upgrade
    }

    /// <summary>
    /// One purchasable slot in the between-round shop (see
    /// RunManager.ShopModifierSlots/ShopUpgradeSlots). A Modifier slot shows
    /// its exact <see cref="ModifierId"/> plainly — on explicit request,
    /// modifiers are never a mystery ("les modifiers sont précis, pas de
    /// type"). An Upgrade slot only reveals which <see cref="Core.UpgradePool"/>
    /// it comes from until purchased — the specific <see cref="UpgradeDefinition"/>
    /// underneath (<see cref="HiddenUpgrade"/>) stays hidden from the
    /// presentation layer on purpose (spec: "tout ce que tu sais c'est
    /// l'upgrade se situe dans quel UpgradePool").
    /// </summary>
    public sealed class ShopSlot
    {
        public readonly ShopSlotKind Kind;

        /// <summary>Meaningful only when <see cref="Kind"/> is Modifier.</summary>
        public readonly ModifierId ModifierId;

        /// <summary>Meaningful only when <see cref="Kind"/> is Upgrade — the one thing about it the player is allowed to see before buying.</summary>
        public readonly UpgradePool Pool;

        /// <summary>Meaningful only when <see cref="Kind"/> is Upgrade — the actual upgrade this slot grants, resolved the moment the slot was rolled but not exposed anywhere the presentation layer could read it before purchase.</summary>
        public readonly UpgradeDefinition HiddenUpgrade;

        public bool Purchased;

        private ShopSlot(ShopSlotKind kind, ModifierId modifierId, UpgradePool pool, UpgradeDefinition hiddenUpgrade)
        {
            Kind = kind;
            ModifierId = modifierId;
            Pool = pool;
            HiddenUpgrade = hiddenUpgrade;
        }

        public static ShopSlot ForModifier(ModifierId id)
        {
            return new ShopSlot(ShopSlotKind.Modifier, id, default(UpgradePool), null);
        }

        public static ShopSlot ForUpgrade(UpgradeDefinition hiddenUpgrade)
        {
            return new ShopSlot(ShopSlotKind.Upgrade, default(ModifierId), hiddenUpgrade.Pool, hiddenUpgrade);
        }
    }
}
