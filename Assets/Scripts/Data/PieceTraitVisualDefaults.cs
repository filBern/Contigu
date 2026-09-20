using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for a piece's enchanted-tile badge (see
    /// Presentation.HandView/GridCellView) — same "no bespoke art yet, colored
    /// chip + hover tooltip" approach as ModifierVisualDefaults, since the
    /// badge itself is too small to carry more than a color. Golden/Blast/
    /// Seeder share one color family (all score as golden), Multiplier/Beacon
    /// share another (both double the group bonus) — the badge alone can't
    /// tell those apart, which is exactly what the hover tooltip is for.
    /// </summary>
    public static class PieceTraitVisualDefaults
    {
        // Same v1 8-color palette as VisualDefaults/UITheme, kept as its own
        // literal set here rather than referencing Presentation.UITheme —
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
                default: return kind.ToString();
            }
        }

        public static string GetDescription(PieceTrait trait)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Golden:
                    return "When this piece is placed, this tile scores a flat golden bonus.";
                case PieceTraitKind.Tinted:
                    return "When this piece is placed, this tile doubles the placement's group and golden bonuses (not the line-clear bonus) — it's tinted to this piece's own color ("
                        + (trait.TintedColor.HasValue ? VisualDefaults.GetColorName(trait.TintedColor.Value) : "its target color") + "), so it always fires.";
                case PieceTraitKind.Multiplier:
                    return "When this piece is placed, this tile doubles the placement's ENTIRE score (group, golden, and line-clear bonuses together) — broader than Tinted Tile, which skips the line-clear bonus.";
                case PieceTraitKind.Blast:
                    return "When this piece is placed, this tile AND its 4 orthogonal neighbors score golden if they end up in the same scored group.";
                case PieceTraitKind.Beacon:
                    return "When this piece is placed, every already-filled tile in this tile's row and column also becomes a multiplier for that one placement's scoring.";
                case PieceTraitKind.Mirror:
                    return "When this piece is placed, this tile's own group bonus is also duplicated onto one random OTHER tile in the scored group (if there's more than one cell in it).";
                case PieceTraitKind.Seeder:
                    return "When this piece is placed, this tile turns golden on the grid for the rest of the round — it keeps scoring every time its group is rescored, until the round ends.";
                case PieceTraitKind.Catalyst:
                    return "When this piece is placed, this tile scores extra points for every cell in the resulting group that was already on the grid before this placement — the bigger the group it reacts with, the bigger the bonus.";
                case PieceTraitKind.Twin:
                    return "When this piece is placed, this tile's own group-bonus share is duplicated onto EVERY other tile in the scored group, not just one at random like Mirror Tile.";
                case PieceTraitKind.Detonator:
                    return "When this piece is placed, if it clears at least one row or column, this tile doubles that placement's whole line-clear bonus.";
                case PieceTraitKind.Chameleon:
                    return "When this piece is placed, if this tile has an already-filled neighbor, the WHOLE piece recolors to match it before scoring — merging into an existing group instead of keeping its own color.";
                case PieceTraitKind.Spark:
                    return "When this piece is placed, this tile scores more points the longer it's been since the last row/column clear this round — the bonus resets once a clear happens.";
                case PieceTraitKind.Void:
                    return "When this piece is placed, this tile also clears one random already-filled tile elsewhere on the grid — free space, at the risk of undoing a setup you were building.";
                case PieceTraitKind.Bastion:
                    return "Once placed, this tile locks in place for the rest of the round instead of being cleared — it still scores the line-clear bonus every time its row/column completes, forever, for as long as the round lasts.";
                case PieceTraitKind.Kamikaze:
                    return "When this piece is placed, this tile also destroys its 8 surrounding tiles (this placement's own cells excluded), scoring +" + ScoringConstants.KamikazeBonusPerDestroyedCell + " per tile actually destroyed.";
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

        /// <summary>True for the trait kinds that reuse the same golden badge look (Golden, Blast, Seeder) as the actual golden grid-cell badge (see GridCellView) — false for the flat color-chip kinds.</summary>
        public static bool UsesGoldenSprite(PieceTraitKind kind)
        {
            return kind == PieceTraitKind.Golden || kind == PieceTraitKind.Blast || kind == PieceTraitKind.Seeder;
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
                default:
                    return Color.gray;
            }
        }
    }
}
