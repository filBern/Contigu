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
                    return "When this piece is placed, this tile doubles the whole placement's group bonus if it lands as "
                        + (trait.TintedColor.HasValue ? VisualDefaults.GetColorName(trait.TintedColor.Value) : "its target color") + ".";
                case PieceTraitKind.Multiplier:
                    return "When this piece is placed, this tile doubles the whole placement's group bonus.";
                case PieceTraitKind.Blast:
                    return "When this piece is placed, this tile AND its 4 orthogonal neighbors score golden if they end up in the same scored group.";
                case PieceTraitKind.Beacon:
                    return "When this piece is placed, every already-filled tile in this tile's row and column also becomes a multiplier for that one placement's scoring.";
                case PieceTraitKind.Mirror:
                    return "When this piece is placed, this tile's own group bonus is duplicated onto the tile symmetrically opposite it in the scored group, if one exists there.";
                case PieceTraitKind.Seeder:
                    return "When this piece is placed, this tile turns golden on the grid for the rest of the round — it keeps scoring every time its group is rescored, until the round ends.";
                default:
                    return string.Empty;
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
                    return MirrorBadgeColor;
                case PieceTraitKind.Seeder:
                    return SeederBadgeColor;
                default:
                    return Color.gray;
            }
        }
    }
}
