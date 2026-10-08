using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for a piece's enchanted-tile badge (see
    /// Presentation.HandView/GridCellView). Badges are too small to carry more
    /// than a color, so traits that behave similarly share a badge color
    /// (Golden/Blast/Seeder; Multiplier/Beacon) and the hover tooltip is what
    /// distinguishes them.
    /// </summary>
    public static class PieceTraitVisualDefaults
    {
        // Kept as its own literal set rather than referencing Presentation.UITheme —
        // Contigu.Data must not depend on Contigu.Presentation (see README).
        private static readonly Color GoldenBadgeColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d
        private static readonly Color MultiplierBadgeColor = new Color(0.396f, 0.682f, 0.839f); // #65aed6
        private static readonly Color TintedFallbackColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color MirrorBadgeColor = new Color(0.373f, 0.412f, 0.612f); // #5f699c
        private static readonly Color SeederBadgeColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color VoidBadgeColor = new Color(0.216f, 0.180f, 0.302f); // #372e4d
        private static readonly Color DetonatorBadgeColor = new Color(0.710f, 0.427f, 0.498f); // #b56d7f
        private static readonly Color ChameleonBadgeColor = new Color(0.643f, 0.922f, 0.800f); // #a4ebcc
        private static readonly Color BastionBadgeColor = new Color(0.380f, 0.263f, 0.388f); // #614363
        private static readonly Color KamikazeBadgeColor = new Color(0.710f, 0.427f, 0.498f); // #b56d7f (same family as Detonator/destruction)

        // Joker-exclusive combat traits all share Joker's own color
        // (VisualDefaults.ColorMap[PieceColor.Joker]); the hover tooltip
        // is what distinguishes them from each other.
        private static readonly Color JokerCombatBadgeColor = new Color(0.608f, 0.349f, 0.714f); // #9b59b6

        public static string GetName(PieceTraitKind kind)
        {
            switch (kind)
            {
                case PieceTraitKind.Golden: return "Golden Tile";
                case PieceTraitKind.Tinted: return "Tinted Tile";
                case PieceTraitKind.Multiplier: return "Multiplier Tile";
                case PieceTraitKind.Blast: return "Blast Tile";
                case PieceTraitKind.Beacon: return "Multiplier Beacon";
                case PieceTraitKind.Mirror: return "Mirror Tile";
                case PieceTraitKind.Seeder: return "Seeder";
                case PieceTraitKind.Catalyst: return "Catalyst Tile";
                case PieceTraitKind.Twin: return "Twin Tile";
                case PieceTraitKind.Detonator: return "Detonator Tile";
                case PieceTraitKind.Chameleon: return "Chameleon Tile";
                case PieceTraitKind.Spark: return "Spark Tile";
                case PieceTraitKind.Void: return "Void Tile";
                case PieceTraitKind.Bastion: return "Bastion Tile";
                case PieceTraitKind.Kamikaze: return "Kamikaze Tile";
                case PieceTraitKind.Bombe: return "Bombe";
                case PieceTraitKind.Range: return "Range";
                case PieceTraitKind.Eclat: return "Éclat";
                case PieceTraitKind.Precision: return "Précision";
                case PieceTraitKind.Sangsue: return "Sangsue";
                default: return kind.ToString();
            }
        }

        public static string GetDescription(PieceTrait trait)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Golden:
                    return "Scores a flat golden bonus when placed.";
                case PieceTraitKind.Tinted:
                    return "Doubles this placement's group and golden bonus (not line-clear) — tinted to "
                        + (trait.TintedColor.HasValue ? VisualDefaults.GetColorName(trait.TintedColor.Value) : "its target color") + ", so it always fires.";
                case PieceTraitKind.Multiplier:
                    return "Doubles the placement's ENTIRE score (group, golden, and line-clear) — broader than Tinted Tile, which skips line-clear.";
                case PieceTraitKind.Blast:
                    return "This tile and its 4 orthogonal neighbors score golden if they land in the same scored group.";
                case PieceTraitKind.Beacon:
                    return "Every filled tile in this tile's row and column becomes a multiplier for this placement.";
                case PieceTraitKind.Mirror:
                    return "Copies this tile's group bonus onto one random other tile in the scored group.";
                case PieceTraitKind.Seeder:
                    return "Turns golden for the rest of the round, rescoring every time its group is scored again.";
                case PieceTraitKind.Catalyst:
                    return "Scores extra per pre-existing cell merged into its group — the bigger the merge, the bigger the bonus.";
                case PieceTraitKind.Twin:
                    return "Copies this tile's group bonus onto EVERY other tile in the group, not just one like Mirror Tile.";
                case PieceTraitKind.Detonator:
                    return "Doubles this placement's whole line-clear bonus, if it clears at least one row or column.";
                case PieceTraitKind.Chameleon:
                    return "If this tile has a filled neighbor, the WHOLE piece recolors to match it before scoring, merging into that group.";
                case PieceTraitKind.Spark:
                    return "Scores more the longer since the last row/column clear this round — resets once a clear happens.";
                case PieceTraitKind.Void:
                    return "Also clears one random filled tile elsewhere on the grid, scoring +" + ScoringConstants.VoidBonusPerDestroyedCell + " for the tile broken.";
                case PieceTraitKind.Bastion:
                    return "Locks in place instead of clearing, but keeps scoring every line it completes for the rest of the round.";
                case PieceTraitKind.Kamikaze:
                    return "Destroys itself and its 8 surrounding tiles (including this piece's own other tiles), scoring +" + ScoringConstants.KamikazeBonusPerDestroyedCell + " per tile destroyed.";
                case PieceTraitKind.Bombe:
                    return "Splits this placement's damage equally across every enemy currently alive, instead of just the front one.";
                case PieceTraitKind.Range:
                    return "Damages the LAST alive enemy in the encounter order instead of the front one.";
                case PieceTraitKind.Eclat:
                    return "Damages the front alive enemy, but any overkill beyond its remaining HP cascades onto the next alive enemy, and so on.";
                case PieceTraitKind.Precision:
                    return "Always damages whichever alive enemy currently has the lowest HP, instead of the front one — a finishing blow.";
                case PieceTraitKind.Sangsue:
                    return "Damages the front alive enemy like usual, and also converts " + (int)(ScoringConstants.SangsueLueurFraction * 100) + "% of the damage dealt into bonus Lueur.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>How often this trait's upgrade shows up in a draft — mirrors the corresponding UpgradeDefinition.Rarity in UpgradeCatalog (kept separately since a PieceTrait doesn't carry a back-reference to the UpgradeDefinition that created it).</summary>
        public static UpgradeRarity GetRarity(PieceTraitKind kind)
        {
            switch (kind)
            {
                case PieceTraitKind.Golden: return UpgradeRarity.Common;
                case PieceTraitKind.Tinted: return UpgradeRarity.Common;
                case PieceTraitKind.Multiplier: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Blast: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Beacon: return UpgradeRarity.Rare;
                case PieceTraitKind.Mirror: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Seeder: return UpgradeRarity.Rare;
                case PieceTraitKind.Catalyst: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Twin: return UpgradeRarity.Rare;
                case PieceTraitKind.Detonator: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Chameleon: return UpgradeRarity.Common;
                case PieceTraitKind.Spark: return UpgradeRarity.Common;
                case PieceTraitKind.Void: return UpgradeRarity.Rare;
                case PieceTraitKind.Bastion: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Kamikaze: return UpgradeRarity.Rare;
                default: return UpgradeRarity.Common;
            }
        }

        public static Color GetBadgeColor(PieceTrait trait)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Golden:
                case PieceTraitKind.Blast:
                    return GoldenBadgeColor;
                case PieceTraitKind.Multiplier:
                case PieceTraitKind.Beacon:
                    return MultiplierBadgeColor;
                case PieceTraitKind.Tinted:
                    return trait.TintedColor.HasValue ? VisualDefaults.GetColor(trait.TintedColor.Value) : TintedFallbackColor;
                case PieceTraitKind.Mirror:
                case PieceTraitKind.Twin:
                    return MirrorBadgeColor;
                case PieceTraitKind.Seeder:
                    return SeederBadgeColor;
                case PieceTraitKind.Catalyst:
                case PieceTraitKind.Void:
                    return VoidBadgeColor;
                case PieceTraitKind.Detonator:
                    return DetonatorBadgeColor;
                case PieceTraitKind.Chameleon:
                    return ChameleonBadgeColor;
                case PieceTraitKind.Spark:
                    return GoldenBadgeColor;
                case PieceTraitKind.Bastion:
                    return BastionBadgeColor;
                case PieceTraitKind.Kamikaze:
                    return KamikazeBadgeColor;
                case PieceTraitKind.Bombe:
                case PieceTraitKind.Range:
                case PieceTraitKind.Eclat:
                case PieceTraitKind.Precision:
                case PieceTraitKind.Sangsue:
                    return JokerCombatBadgeColor;
                default:
                    return Color.gray;
            }
        }
    }
}
