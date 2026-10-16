using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for an upgrade's rarity + pool ("type"), shown together
    /// as a subtitle line (see Presentation.UpgradeCardFactory, Presentation.TraitBadgeView).
    /// </summary>
    public static class UpgradeVisualDefaults
    {
        // Tuned for the dark backgrounds these colors are shown against (tooltip panel, trait badge).
        private static readonly Color CommonColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color UncommonColor = new Color(0.396f, 0.682f, 0.839f); // #65aed6
        private static readonly Color RareColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d

        public static string GetRarityLabel(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common: return "Common";
                case UpgradeRarity.Uncommon: return "Uncommon";
                case UpgradeRarity.Rare: return "Rare";
                default: return rarity.ToString();
            }
        }

        public static Color GetRarityColor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common: return CommonColor;
                case UpgradeRarity.Uncommon: return UncommonColor;
                case UpgradeRarity.Rare: return RareColor;
                default: return Color.gray;
            }
        }

        /// <summary>Player-facing label for an UpgradePool. "Bank"/"Grid" are internal names from an earlier design (see README); the pool itself is unchanged, only how it's labeled here.</summary>
        public static string GetPoolLabel(UpgradePool pool)
        {
            switch (pool)
            {
                case UpgradePool.Bank: return "Piece Upgrade";
                case UpgradePool.Grid: return "Tile Upgrade";
                case UpgradePool.Mastery: return "Mastery Upgrade";
                case UpgradePool.Modifier: return "Modifier Upgrade";
                default: return pool.ToString();
            }
        }
    }
}
