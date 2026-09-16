namespace Contigu.Core
{
    /// <summary>
    /// A round-end draft offer: 3 distinct Bank (tile) options and 3 Grid
    /// options, presented independently — the player picks exactly one from
    /// each, ending up with two permanent upgrades per round cleared.
    /// </summary>
    public sealed class UpgradeDraft
    {
        public readonly UpgradeDefinition[] TileOptions;
        public readonly UpgradeDefinition[] GridOptions;

        public UpgradeDraft(UpgradeDefinition[] tileOptions, UpgradeDefinition[] gridOptions)
        {
            TileOptions = tileOptions;
            GridOptions = gridOptions;
        }
    }
}
