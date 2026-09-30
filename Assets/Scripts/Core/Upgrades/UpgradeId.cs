namespace Contigu.Core
{
    public enum UpgradePool
    {
        Bank,
        Grid
    }

    /// <summary>The 5 Bank upgrades + 13 Grid (piece-enchantment) upgrades from spec 5.3 / 5.4, in two batches.</summary>
    public enum UpgradeId
    {
        RemovePiece,
        DuplicatePiece,
        JokerPiece,
        RecolorPiece,
        RandomModifier,

        // ---- Fourth batch (Bank pool, on explicit request) ----
        RandomPiece,
        ModifierUpgrade,

        GoldenCells,
        TintedCells,
        MultiplierZone,
        BlastTile,
        MultiplierBeacon,
        MirrorTile,
        Seeder,

        // ---- Second batch (7 more tile upgrades, on explicit request) ----
        // DrillerTile removed (on explicit request — boss-round locked-cell
        // upgrades read as too abstract for too long before a player could
        // act on them).
        CatalystTile,
        TwinTile,
        DetonatorTile,
        ChameleonTile,
        SparkTile,
        VoidTile,

        // ---- Third batch (2 more, on explicit request) ----
        BastionTile,
        KamikazeTile,

        // ---- Fifth batch (Bank pool, on explicit request — a corrected
        // redo of an earlier attempt that wrongly built this as a
        // persistent Modifier instead: "les modifiers mastery que tu as
        // créé devaient être des upgrades, pas des modifiers") ----
        PieceMastery,

        // ---- Sixth batch (Bank pool, on explicit request: "Il faudrait
        // faire la même chose avec les couleurs" — the same Mastery
        // mechanic, keyed by PieceColor instead of ShapeId) ----
        ColorMastery
    }
}
