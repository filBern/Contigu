namespace Contigu.Core
{
    public enum ShopSlotKind
    {
        Modifier,
        Upgrade
    }

    /// <summary>
    /// One purchasable slot in the between-round shop (see
    /// RunManager.ShopBlisterSlots/ShopUpgradeSlots). A Modifier slot shows
    /// its exact <see cref="ModifierId"/> plainly — on explicit request,
    /// modifiers are never a mystery ("les modifiers sont précis, pas de
    /// type"). An Upgrade slot's visibility depends on WHICH section it's
    /// in, not on the slot itself: <see cref="HiddenUpgrade"/> is always
    /// fully resolved the moment the slot is rolled (both sections), but a
    /// "Blister" slot (RunManager.ShopBlisterSlots) shows it plainly same
    /// as a modifier — on explicit request, "on aperçoit 3 modifiers ou
    /// upgrades" — while a "Casino" slot (RunManager.ShopUpgradeSlots)
    /// still only reveals its <see cref="Core.UpgradePool"/> until
    /// purchased, the original mystery-box behavior (spec: "tout ce que tu
    /// sais c'est l'upgrade se situe dans quel UpgradePool"). The
    /// presentation layer decides which treatment applies purely by which
    /// of RunManager's two slot arrays a given ShopSlot came from.
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
