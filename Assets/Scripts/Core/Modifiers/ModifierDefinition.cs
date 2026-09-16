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

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Tricolore",
            "+14 pts si le groupe compte exactement 3 couleurs distinctes (jokers exclus).");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Complémentaire",
            "+16 pts si le groupe contient une paire de couleurs complémentaires (Corail/Violet ou Sarcelle/Citron vert).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Îlot",
            "+8 pts si la pose forme un groupe isolé d'une seule case (aucun voisin de couleur compatible).");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Couronne",
            "+5 pts par case du groupe située sur le pourtour de la grille (bord).");

        public static readonly ModifierDefinition TrouDansLaGrille = new ModifierDefinition(
            ModifierId.TrouDansLaGrille, ModifierCategory.Voisinage, "Trou dans la grille",
            "+10 pts par case du groupe adjacente à une case verrouillée (manche boss).");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Carrefour",
            "+12 pts par case du groupe encerclée sur ses 4 côtés par au moins 2 couleurs différentes.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Maçon",
            "+5 pts à chaque pose qui ne complète aucune ligne/colonne (bâtir sans détruire).");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Démolisseur",
            "+15 pts par ligne/colonne complétée simultanément par cette pose, à partir de 2 lignes à la fois.");

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
