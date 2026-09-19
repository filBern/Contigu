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

        /// <summary>"Mirror Tile" — this cell's own group-bonus contribution is also duplicated onto one random OTHER cell in the scored group.</summary>
        Mirror,

        /// <summary>"Seeder" — like <see cref="Golden"/>, but the golden flag isn't cleared right after scoring like every other trait: the grid cell stays golden for the rest of the CURRENT ROUND (cleared at the next round's reset — permanent for the whole run was too powerful).</summary>
        Seeder,

        // ---- Second batch (7 more, on explicit request — see README) ----

        /// <summary>"Catalyst Tile" — scores extra points for every cell in this placement's scored group that was already on the grid before this placement (group size minus this piece's own cell count).</summary>
        Catalyst,

        /// <summary>"Twin Tile" — like <see cref="Mirror"/>, but duplicates the enchanted cell's own group-bonus share onto EVERY other cell in the scored group, not just one at random.</summary>
        Twin,

        /// <summary>"Detonator Tile" — if this placement clears at least one row/column, doubles the WHOLE placement's line-clear bonus.</summary>
        Detonator,

        /// <summary>"Chameleon Tile" — if the enchanted cell has an already-filled orthogonal neighbor, the WHOLE piece is recolored to match it before scoring, merging into an existing group instead of keeping its own color.</summary>
        Chameleon,

        /// <summary>"Spark Tile" — scores more points the longer it's been (in placements) since the last row/column clear this round; resets once a clear happens.</summary>
        Spark,

        /// <summary>"Void Tile" — also clears one random already-filled, unlocked cell elsewhere on the grid (excluding this placement's own cells) when placed. No score of its own.</summary>
        Void
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
