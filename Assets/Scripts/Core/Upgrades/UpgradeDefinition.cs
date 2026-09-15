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
            UpgradeId.RemovePiece, UpgradePool.Bank, "Retirer une pièce",
            "Choisissez un type de pièce du deck ; une copie est retirée définitivement (plancher de 10).", true);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Dupliquer une pièce",
            "Choisissez un type de pièce du deck ; une copie supplémentaire est ajoutée.", true);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Pièce joker",
            "Ajoute une pièce (single, joker) au deck.", false);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolorer une pièce",
            "Choisissez un type de pièce et une couleur cible ; une copie change de couleur.", true);

        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Cases dorées",
            "Ajoute 3 cellules dorées (+18 pts fixes à la pose) à la grille.", false);

        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Cases teintées",
            "Ajoute 2 cellules teintées ; poser la bonne couleur double le bonus de voisinage.", false);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Zone multiplicatrice",
            "Ajoute 3 cellules qui doublent le bonus de voisinage à la pose.", false);

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
