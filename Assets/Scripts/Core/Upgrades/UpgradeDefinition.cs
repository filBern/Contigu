namespace Contigu.Core
{
    /// <summary>
    /// Static description of one upgrade. <see cref="RequiresSubChoice"/> flags
    /// upgrades that need the player to pick a piece type (and/or target color)
    /// before they can be applied (Retirer/Dupliquer/Recolorer, spec 5.3).
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public readonly UpgradeId Id;
        public readonly UpgradePool Pool;
        public readonly string Name;
        public readonly string Description;
        public readonly bool RequiresSubChoice;

        public UpgradeDefinition(UpgradeId id, UpgradePool pool, string name, string description, bool requiresSubChoice)
        {
            Id = id;
            Pool = pool;
            Name = name;
            Description = description;
            RequiresSubChoice = requiresSubChoice;
        }
    }

    public static class UpgradeCatalog
    {
        public static readonly UpgradeDefinition RemovePiece = new UpgradeDefinition(
            UpgradeId.RemovePiece, UpgradePool.Bank, "Remove a piece",
            "Choose a piece type from the deck; one copy is permanently removed (floor of 10).", true);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Duplicate a piece",
            "Choose a piece type from the deck; one extra copy is added.", true);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Joker piece",
            "Adds a piece (single, joker) to the deck.", false);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolor a piece",
            "Choose a piece type and a target color; one copy changes color.", true);

        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Golden Cells",
            "Enchants 3 random pieces in the deck: one tile on each lands golden (+18 flat points) when that piece is placed.", false);

        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Tinted Cells",
            "Enchants 3 random pieces in the deck: one tile on each lands tinted, doubling that placement's group bonus if the piece's own color matches.", false);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "Enchants 3 random pieces in the deck: one tile on each lands as a multiplier zone, doubling that placement's group bonus.", false);

        public static readonly UpgradeDefinition[] All =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece,
            GoldenCells, TintedCells, MultiplierZone
        };

        public static readonly UpgradeDefinition[] BankPool =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece
        };

        public static readonly UpgradeDefinition[] GridPool =
        {
            GoldenCells, TintedCells, MultiplierZone
        };
    }
}
