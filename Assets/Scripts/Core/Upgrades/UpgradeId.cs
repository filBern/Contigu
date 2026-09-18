namespace Contigu.Core
{
    public enum UpgradePool
    {
        Bank,
        Grid
    }

    /// <summary>The 4 Bank upgrades + 14 Grid (piece-enchantment) upgrades from spec 5.3 / 5.4, in two batches.</summary>
    public enum UpgradeId
    {
        RemovePiece,
        DuplicatePiece,
        JokerPiece,
        RecolorPiece,
        GoldenCells,
        TintedCells,
        MultiplierZone,
        BlastTile,
        MultiplierBeacon,
        MirrorTile,
        Seeder,

        // ---- Second batch (7 more tile upgrades, on explicit request) ----
        CatalystTile,
        DrillerTile,
        TwinTile,
        DetonatorTile,
        ChameleonTile,
        SparkTile,
        VoidTile
    }
}
