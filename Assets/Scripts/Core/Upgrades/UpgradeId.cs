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
        KamikazeTile
    }
}
