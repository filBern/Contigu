namespace Contigu.Core
{
    /// <summary>Broad theme a modifier belongs to, matching the categories from the brainstorm list — display grouping only, no gameplay effect.</summary>
    public enum ModifierCategory
    {
        Couleurs,
        Voisinage,
        Connexions,
        Destruction,
        Roguelike
    }

    /// <summary>
    /// Static description of one modifier. Unlike <see cref="UpgradeDefinition"/>,
    /// modifiers are never consumed by application — they're held persistently
    /// (up to <see cref="RunManager.MaxActiveModifiers"/> at once) and re-evaluate
    /// every placement via <see cref="GridManager.PlacePiece"/>.
    /// </summary>
    public sealed class ModifierDefinition
    {
        public readonly ModifierId Id;
        public readonly ModifierCategory Category;
        public readonly string Name;
        public readonly string Description;

        public ModifierDefinition(ModifierId id, ModifierCategory category, string name, string description)
        {
            Id = id;
            Category = category;
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Modifiers implemented from the much larger brainstorm list (Couleurs /
    /// Voisinage / Lignes / Connexions / Destruction / "plus roguelike"): each
    /// one here is computable from data already flowing through
    /// <see cref="GridManager.PlacePiece"/> without a deeper refactor
    /// (line-level modifiers — row/column-shape rules like Alternance,
    /// Symétrie, Palindrome, Gradient — need <see cref="GridManager"/>'s clear
    /// pipeline reworked first and are left as a documented future batch — see
    /// README). Several names below (Complémentaire's exact color pairing,
    /// Maçon, Démolisseur) had only a name + category to go on when this batch
    /// was implemented, not the original detailed rule text, so their exact
    /// trigger condition is this project's best-effort interpretation of the
    /// theme — documented per-modifier below and in the README.
    /// </summary>
    public static class ModifierCatalog
    {
        public static readonly ModifierDefinition Prisme = new ModifierDefinition(
            ModifierId.Prisme, ModifierCategory.Couleurs, "Prism",
            "+20 pts if this placement touches (itself or its direct neighbors) 4 distinct colors (or 3 + a joker).");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chain",
            "+10 pts if the connected group has at least 5 cells.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Mega Chain",
            "+30 pts if the group has at least 10 cells, +5 pts per cell beyond that.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Fortress",
            "+6 pts per group cell fully surrounded (8 filled neighbors).");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisoner",
            "+4 pts per group cell surrounded on its 4 orthogonal sides.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architect",
            "+15 pts every time a 2x2 square block is placed.");

        public static readonly ModifierDefinition Puriste = new ModifierDefinition(
            ModifierId.Puriste, ModifierCategory.Roguelike, "Purist",
            "+50% of the group's points if the ENTIRE group is the same color (jokers ignored). A single cell of a different color anywhere in the group cancels the bonus.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collector",
            "When this placement clears at least one row/column: +8 pts per distinct color among the cleared cells (0 if nothing is cleared).");

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Tricolor",
            "+14 pts if this placement touches (itself or its direct neighbors) exactly 3 distinct colors (jokers excluded).");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Complementary",
            "+16 pts if this placement touches (itself or its direct neighbors) a complementary color pair (Coral/Violet or Teal/Lime).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Islet",
            "+8 pts if the placement forms an isolated single-cell group (no compatible-colored neighbor).");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Crown",
            "+5 pts per group cell sitting on the grid's outer edge.");

        public static readonly ModifierDefinition TrouDansLaGrille = new ModifierDefinition(
            ModifierId.TrouDansLaGrille, ModifierCategory.Voisinage, "Hole in the Grid",
            "+10 pts per group cell adjacent to a locked cell (boss round).");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+12 pts per group cell surrounded on all 4 sides by at least 2 different colors, themselves different from its own color.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "+5 pts for every placement that completes no row/column (building without destroying).");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "+15 pts per row/column completed simultaneously by this placement, starting at 2 lines at once.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Puriste, Collectionneur,
            Tricolore, Complementaire, Ilot, Couronne, TrouDansLaGrille, Carrefour, Macon, Demolisseur
        };

        public static ModifierDefinition Get(ModifierId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }
            return null;
        }
    }
}
