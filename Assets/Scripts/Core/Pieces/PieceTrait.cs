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

        /// <summary>"Void Tile" — also clears one random already-filled, unlocked cell elsewhere on the grid (excluding this placement's own cells) when placed, scoring ScoringConstants.VoidBonusPerDestroyedCell for the broken tile.</summary>
        Void,

        /// <summary>"Bastion Tile" — once placed, this tile locks in place for the rest of the round: it's never cleared by a completed row/column, but it still scores the line-clear bonus every time one of those completes, as if it actually had been.</summary>
        Bastion,

        /// <summary>"Kamikaze Tile" — when placed, destroys itself and its 8 surrounding tiles (including this same placement's own other cells), scoring a flat bonus per tile actually destroyed.</summary>
        Kamikaze,

        // Joker-exclusive combat traits — unlike every trait above, these
        // never score anything on their own: they only change which enemy
        // (or enemies) this placement's damage lands on during an active
        // encounter (see RunManager.ApplyJokerCombatDamage), and are never
        // offered through the ordinary tile-upgrade shop — every Joker
        // piece is tagged with exactly one, rolled at random the moment
        // it's added to the deck (see DeckManager.AddJoker). Visually,
        // every cell of the piece shows the badge (not just one, like the
        // scoring traits above — see ShapePreviewFactory.Build/IsJokerCombatKind),
        // but the effect itself still only fires once per placement
        // regardless of the piece's cell count.

        /// <summary>"Bombe" — splits this placement's damage EQUALLY across every alive enemy instead of just the front one.</summary>
        Bombe,

        /// <summary>"Range" — damages the LAST alive enemy in encounter order instead of the front one.</summary>
        Range,

        /// <summary>"Éclat" — damages the front alive enemy same as the default rule, but any OVERKILL (damage beyond its remaining HP) cascades onto the next alive enemy, and so on down the line.</summary>
        Eclat,

        /// <summary>"Précision" — always damages whichever ALIVE enemy currently has the LOWEST HP, ignoring the usual front-to-back order — a finishing blow instead of chipping at the front.</summary>
        Precision,

        /// <summary>"Sangsue" — damages the front alive enemy exactly like the default rule, but also converts a fraction of the damage dealt into bonus Lueur (see ScoringConstants.SangsueLueurFraction).</summary>
        Sangsue
    }

    /// <summary>
    /// A one-time scoring enchantment tagged onto a single cell of a specific
    /// <see cref="PieceToken"/> — the upgrade marks a piece in the deck rather
    /// than a fixed grid cell. <see cref="LocalCellIndex"/> indexes into the
    /// piece's base (Deg0) shape's cell list — since <see
    /// cref="PieceShapeCatalog.GetRotated"/> maps that list 1:1 across every
    /// rotation, the same index still identifies the correct cell once the
    /// piece is placed at whatever rotation it was actually dealt.
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

        /// <summary>Every Joker-exclusive combat kind (see their own doc comments on <see cref="PieceTraitKind"/>) — DeckManager.AddJoker rolls uniformly from this array, and ShapePreviewFactory/RunManager.ApplyTokenTrait check membership in it to badge every cell instead of just <see cref="LocalCellIndex"/>.</summary>
        public static readonly PieceTraitKind[] JokerCombatKinds =
        {
            PieceTraitKind.Bombe, PieceTraitKind.Range, PieceTraitKind.Eclat, PieceTraitKind.Precision, PieceTraitKind.Sangsue
        };

        public static bool IsJokerCombatKind(PieceTraitKind kind)
        {
            return kind == PieceTraitKind.Bombe || kind == PieceTraitKind.Range || kind == PieceTraitKind.Eclat
                || kind == PieceTraitKind.Precision || kind == PieceTraitKind.Sangsue;
        }
    }
}
