using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for an upgrade's rarity + pool ("type"), shown
    /// together as a small colored subtitle line under the name — on draft
    /// cards (see Presentation.DraftView.BuildCard) and, for a piece trait's
    /// tile badge, in its hover tooltip (see Presentation.TraitBadgeView).
    /// </summary>
    public static class UpgradeVisualDefaults
    {
        // Same v1 8-color palette as VisualDefaults/UITheme/ModifierVisualDefaults.
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

        /// <summary>Player-facing label for an UpgradePool — "Bank"/"Grid" are internal holdovers from the pre-5.4 fixed-grid-cell design (see README); the pool itself is unchanged, only how it reads here.</summary>
        public static string GetPoolLabel(UpgradePool pool)
        {
            switch (pool)
            {
                case UpgradePool.Bank: return "Piece Upgrade";
                case UpgradePool.Grid: return "Tile Upgrade";
                default: return pool.ToString();
            }
        }
    }
}
