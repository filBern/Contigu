namespace Contigu.Core
{
    /// <summary>Which grid-cell scoring modifier a tagged piece's cell applies on placement (mirrors <see cref="Cell"/>'s golden/tinted/multiplier flags).</summary>
    public enum PieceTraitKind
    {
        Golden,
        Tinted,
        Multiplier
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
