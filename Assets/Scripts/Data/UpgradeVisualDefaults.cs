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
        // Tuned for the DARK backgrounds these colors were designed against
        // (tooltip panel, trait badge) — see GetRarityColorOnLight below for
        // the same rarities recolored for a light background.
        private static readonly Color CommonColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color UncommonColor = new Color(0.396f, 0.682f, 0.839f); // #65aed6
        private static readonly Color RareColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d

        // Same three hues, re-lightened/darkened for legibility on a LIGHT
        // background (the pale-lavender draft-card art, UISprites.
        // UpgradeCardBackground) — the dark-background set above reads as
        // near-invisible pale-on-pale there (this is what made the rarity
        // line on draft cards hard to read). Each keeps its source color's
        // hue so a rarity is still recognizable at a glance between the two
        // contexts, just pushed to a shade dark/saturated enough to contrast
        // against a light card instead of a dark panel.
        private static readonly Color CommonColorOnLight = new Color(0.235f, 0.22f, 0.318f); // #3c3851 (dark slate, Panel's hue family)
        private static readonly Color UncommonColorOnLight = new Color(0.106f, 0.373f, 0.525f); // #1b5f86 (dark cyan-blue, UncommonColor's hue darkened)
        private static readonly Color RareColorOnLight = new Color(0.616f, 0.365f, 0.106f); // #9d5d1b (burnt orange, RareColor's hue darkened)

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

        /// <summary>Same rarities as GetRarityColor, recolored for legibility on a light background — see DraftView.BuildCard.</summary>
        public static Color GetRarityColorOnLight(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common: return CommonColorOnLight;
                case UpgradeRarity.Uncommon: return UncommonColorOnLight;
                case UpgradeRarity.Rare: return RareColorOnLight;
                default: return Color.black;
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
