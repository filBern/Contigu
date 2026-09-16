namespace Contigu.Core
{
    /// <summary>Broad theme a modifier belongs to, matching the categories from the brainstorm list — display grouping only, no gameplay effect.</summary>
    public enum ModifierCategory
    {
        Couleurs,
        Voisinage,
        Connexions,
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
    /// First batch of modifiers implemented from the much larger brainstorm list
    /// (Couleurs / Voisinage / Lignes / Connexions / Destruction / "plus
    /// roguelike"): 8 chosen because each is computable from data already
    /// flowing through <see cref="GridManager.PlacePiece"/> without a deeper
    /// refactor (line-level and destruction-streak modifiers need
    /// <see cref="GridManager"/>'s clear pipeline reworked first and are left as
    /// a documented future batch — see README).
    /// </summary>
    public static class ModifierCatalog
    {
        public static readonly ModifierDefinition Prisme = new ModifierDefinition(
            ModifierId.Prisme, ModifierCategory.Couleurs, "Prisme",
            "+20 pts si le groupe compte 4 couleurs distinctes (ou 3 + un joker).");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chaîne",
            "+10 pts si le groupe connecté compte au moins 5 cases.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Méga-chaîne",
            "+30 pts si le groupe compte au moins 10 cases, +5 pts par case au-delà.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Forteresse",
            "+6 pts par case du groupe entièrement entourée (8 voisins remplis).");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisonnier",
            "+4 pts par case du groupe entourée sur ses 4 côtés orthogonaux.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architecte",
            "+15 pts à chaque pose d'un bloc carré 2x2.");

        public static readonly ModifierDefinition Puriste = new ModifierDefinition(
            ModifierId.Puriste, ModifierCategory.Roguelike, "Puriste",
            "+50% du bonus de groupe si le groupe est entièrement d'une seule couleur (jokers exclus).");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collectionneur",
            "+8 pts par couleur distincte parmi les cases effacées par cette pose.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Puriste, Collectionneur
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
