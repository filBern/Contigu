namespace Contigu.Core
{
    /// <summary>
    /// How often an upgrade shows up in a draft (spec extension, explicit
    /// request). Display-only beyond the draft weight below — its label/color
    /// live in Data.UpgradeVisualDefaults, shown alongside the upgrade's
    /// <see cref="UpgradePool"/> ("type") in the draft card and, for a piece
    /// trait, its tile-badge tooltip.
    /// </summary>
    public enum UpgradeRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public static class UpgradeRarityUtility
    {
        /// <summary>
        /// Relative draft weight — higher rolls more often (see
        /// UpgradeSystem.PickWeighted). Common is 4x as likely to be rolled as
        /// Rare, Uncommon 2x.
        /// </summary>
        public static int GetDraftWeight(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common: return 8;
                case UpgradeRarity.Uncommon: return 4;
                case UpgradeRarity.Rare: return 2;
                default: return 1;
            }
        }
    }
}
