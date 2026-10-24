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

        private static readonly System.Collections.Generic.Dictionary<UpgradeId, Sprite> SignIcons = new System.Collections.Generic.Dictionary<UpgradeId, Sprite>();

        /// <summary>The upgrade's wooden garden-sign icon (Resources/Icons/Upgrades/&lt;UpgradeId&gt;.png) — a tall signpost silhouette, instantly different from a fertilizer's round sack. Null if missing.</summary>
        public static Sprite GetIcon(UpgradeId id)
        {
            if (!SignIcons.TryGetValue(id, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Icons/Upgrades/" + id);
                SignIcons[id] = sprite;
            }
            return sprite;
        }

        public static string GetRarityLabel(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common: return "Wild";
                case UpgradeRarity.Uncommon: return "Cultivated";
                case UpgradeRarity.Rare: return "Heirloom";
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
                case UpgradePool.Bank: return "Seed Upgrade";
                case UpgradePool.Grid: return "Sprout Upgrade";
                case UpgradePool.Mastery: return "Mastery Upgrade";
                case UpgradePool.Modifier: return "Fertilizer Upgrade";
                case UpgradePool.Workshop: return "Workshop";
                default: return pool.ToString();
            }
        }
    }
}
