namespace Contigu.Core
{
    /// <summary>
    /// Static description of one upgrade. <see cref="RequiresSubChoice"/> flags
    /// upgrades that need the player to pick a piece type (and/or target color)
    /// before they can be applied (Retirer/Dupliquer/Recolorer, spec 5.3).
    /// <see cref="Rarity"/> (spec extension, explicit request) weights how
    /// often it shows up in a draft — see UpgradeSystem.PickWeighted — and is
    /// shown to the player alongside <see cref="Pool"/> ("type") on the draft
    /// card / tile-badge tooltip.
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public readonly UpgradeId Id;
        public readonly UpgradePool Pool;
        public readonly string Name;
        public readonly string Description;
        public readonly bool RequiresSubChoice;
        public readonly UpgradeRarity Rarity;

        public UpgradeDefinition(UpgradeId id, UpgradePool pool, string name, string description, bool requiresSubChoice, UpgradeRarity rarity)
        {
            Id = id;
            Pool = pool;
            Name = name;
            Description = description;
            RequiresSubChoice = requiresSubChoice;
            Rarity = rarity;
        }
    }

    public static class UpgradeCatalog
    {
        public static readonly UpgradeDefinition RemovePiece = new UpgradeDefinition(
            UpgradeId.RemovePiece, UpgradePool.Bank, "Remove a piece",
            "Choose a piece type from the deck; one copy is permanently removed (floor of 10).", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Duplicate a piece",
            "Choose a piece type from the deck; one extra copy is added.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Joker piece",
            "Adds a piece (single, joker) to the deck.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolor a piece",
            "Choose a piece type and a target color; one copy changes color.", true, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Golden Cells",
            "Enchants 3 random pieces in the deck: one tile on each lands golden (+18 flat points) when that piece is placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Tinted Cells",
            "Enchants 3 random pieces in the deck: one tile on each lands tinted, doubling that placement's group bonus if the piece's own color matches.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "Enchants 3 random pieces in the deck: one tile on each lands as a multiplier zone, doubling that placement's group bonus.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition BlastTile = new UpgradeDefinition(
            UpgradeId.BlastTile, UpgradePool.Grid, "Blast Tile",
            "Enchants 3 random pieces in the deck: one tile on each also makes its 4 orthogonal neighbors score golden (+18 each) when that piece is placed.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MultiplierBeacon = new UpgradeDefinition(
            UpgradeId.MultiplierBeacon, UpgradePool.Grid, "Multiplier Beacon",
            "Enchants 3 random pieces in the deck: one tile on each turns every already-filled tile in its row and column into a multiplier too, for that one placement.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MirrorTile = new UpgradeDefinition(
            UpgradeId.MirrorTile, UpgradePool.Grid, "Mirror Tile",
            "Enchants 3 random pieces in the deck: one tile on each also duplicates its own group-bonus share onto one random OTHER tile in the scored group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition Seeder = new UpgradeDefinition(
            UpgradeId.Seeder, UpgradePool.Grid, "Seeder",
            "Enchants 3 random pieces in the deck: one tile on each turns golden on the grid for the rest of the round when that piece is placed, instead of just scoring once.", false, UpgradeRarity.Rare);

        // ---- Second batch (7 more tile upgrades, on explicit request) ----

        public static readonly UpgradeDefinition CatalystTile = new UpgradeDefinition(
            UpgradeId.CatalystTile, UpgradePool.Grid, "Catalyst Tile",
            "Enchants 3 random pieces in the deck: one tile on each scores extra points for every cell in the resulting group that was already on the grid before this placement.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition DrillerTile = new UpgradeDefinition(
            UpgradeId.DrillerTile, UpgradePool.Grid, "Driller Tile",
            "Enchants 3 random pieces in the deck: one tile on each scores a big flat bonus, but only when placed next to a locked cell (boss rounds).", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition TwinTile = new UpgradeDefinition(
            UpgradeId.TwinTile, UpgradePool.Grid, "Twin Tile",
            "Enchants 3 random pieces in the deck: one tile on each duplicates its own group-bonus share onto EVERY other tile in the scored group, not just one at random like Mirror Tile.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DetonatorTile = new UpgradeDefinition(
            UpgradeId.DetonatorTile, UpgradePool.Grid, "Detonator Tile",
            "Enchants 3 random pieces in the deck: one tile on each doubles this placement's line-clear bonus, if it clears at least one row or column.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ChameleonTile = new UpgradeDefinition(
            UpgradeId.ChameleonTile, UpgradePool.Grid, "Chameleon Tile",
            "Enchants 3 random pieces in the deck: one tile on each recolors the WHOLE piece to match a filled neighbor when placed, merging it into an existing group instead of keeping its own color.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition SparkTile = new UpgradeDefinition(
            UpgradeId.SparkTile, UpgradePool.Grid, "Spark Tile",
            "Enchants 3 random pieces in the deck: one tile on each scores more points the longer it's been since the last line/column clear this round.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition VoidTile = new UpgradeDefinition(
            UpgradeId.VoidTile, UpgradePool.Grid, "Void Tile",
            "Enchants 3 random pieces in the deck: one tile on each also clears one random already-filled tile elsewhere on the grid when placed — free space, at a risk.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition[] All =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece,
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, DrillerTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile
        };

        public static readonly UpgradeDefinition[] BankPool =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece
        };

        public static readonly UpgradeDefinition[] GridPool =
        {
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, DrillerTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile
        };
    }
}
