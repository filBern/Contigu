namespace Contigu.Core
{
    /// <summary>Which effect a tagged piece's enchanted cell applies on placement.</summary>
    public enum PieceTraitKind
    {
        /// <summary>The cell scores a flat golden bonus (mirrors <see cref="Cell.IsGolden"/>).</summary>
        Golden,

        /// <summary>The cell doubles the group bonus if its own fill color matches (mirrors <see cref="Cell.IsTinted"/>).</summary>
        Tinted,

        /// <summary>The cell doubles the group bonus (mirrors <see cref="Cell.IsMultiplierZone"/>).</summary>
        Multiplier,

        /// <summary>"Blast Tile" — the cell AND its 4 orthogonal neighbors all score golden for this one placement.</summary>
        Blast,

        /// <summary>"Multiplier Beacon" — the cell, plus every already-filled cell in its row and column, become multiplier zones for this one placement.</summary>
        Beacon,

        /// <summary>"Mirror Tile" — this cell's own group-bonus contribution is duplicated onto the cell symmetrically opposite it in the scored group, if one exists there.</summary>
        Mirror,

        /// <summary>"Seeder" — like <see cref="Golden"/>, but the golden flag is never cleared after scoring: the grid cell stays golden for the rest of the run.</summary>
        Seeder
    }

    /// <summary>
    /// A one-time scoring enchantment tagged onto a single cell of a specific
    /// <see cref="PieceToken"/> (spec 5.4 redesign: the upgrade marks a piece in
    /// the deck rather than a fixed grid cell). <see cref="LocalCellIndex"/>
    /// indexes into the piece's BASE (Deg0) shape's cell list — since
    /// <see cref="PieceShapeCatalog.GetRotated"/> maps that list 1:1 across every
    /// rotation, the same index still identifies the correct cell once the piece
    /// is placed at whatever rotation it was actually dealt.
    /// </summary>
    public readonly struct PieceTrait
    {
        public readonly PieceTraitKind Kind;
        public readonly int LocalCellIndex;

        /// <summary>Only meaningful for <see cref="PieceTraitKind.Tinted"/>; null for the others.</summary>
        public readonly PieceColor? TintedColor;

        public PieceTrait(PieceTraitKind kind, int localCellIndex, PieceColor? tintedColor = null)
        {
            Kind = kind;
            LocalCellIndex = localCellIndex;
            TintedColor = tintedColor;
        }
    }
}
