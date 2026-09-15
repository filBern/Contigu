namespace Contigu.Core
{
    public enum UpgradePool
    {
        Bank,
        Grid
    }

    /// <summary>The 4 Bank upgrades + 3 Grid upgrades from spec 5.3 / 5.4.</summary>
    public enum UpgradeId
    {
        RemovePiece,
        DuplicatePiece,
        JokerPiece,
        RecolorPiece,
        GoldenCells,
        TintedCells,
        MultiplierZone
    }
}
