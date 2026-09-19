namespace Contigu.Core
{
    /// <summary>Broad theme a modifier belongs to, matching the categories from the brainstorm list — display grouping only, no gameplay effect.</summary>
    public enum ModifierCategory
    {
        Couleurs,
        Voisinage,
        Connexions,
        Destruction,
        Roguelike,

        /// <summary>Third batch only — the 10 per-shape modifiers (Formes.*), none of the first two batches needed their own bucket for this.</summary>
        Formes
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
    /// Voisinage / Lignes / Connexions / Destruction / "plus roguelike"), in
    /// two batches (16 + 16). The first batch (Prisme..Démolisseur) is
    /// computable directly from state <see cref="GridManager.PlacePiece"/>
    /// already had. The second batch's 8 line-level modifiers (Arc-en-ciel,
    /// Alternance, Symétrie, Palindrome, Gradient, Sans doublon, Bloc,
    /// Monochrome-ligne) needed <see cref="GridManager.CheckAndClearLines"/>
    /// reworked to expose each cleared row/column's ordered color sequence
    /// BEFORE it's wiped — see README. Several names (Complémentaire's exact
    /// color pairing, Maçon, Démolisseur, and the whole second batch) had only
    /// a name + category to go on, not original detailed rule text, so their
    /// exact trigger condition is this project's best-effort interpretation of
    /// the theme — documented per-modifier below and in the README. Still not
    /// delivered: the destruction modifiers needing a placement/clear history
    /// across a round (Overkill, Cascade, Réaction en chaîne, Combo parfait,
    /// Nettoyage, Récolte) and "Dernier espace" (structurally unreachable —
    /// see README).
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

        // ---- Second batch (16 more) — see README for the interpretation notes
        // on the 8 line-level modifiers below, which needed CheckAndClearLines
        // reworked to expose each cleared line's ordered color sequence before
        // it clears. Bonuses only ever apply to a cleared line's UNLOCKED cells
        // (boss-round locked cells are simply absent from the sequence, so a
        // line can qualify with fewer than 8 colors when locks shrink it). ----

        public static readonly ModifierDefinition CoeurDePierre = new ModifierDefinition(
            ModifierId.CoeurDePierre, ModifierCategory.Voisinage, "Stone Heart",
            "+7 pts per group cell whose 8 surrounding neighbors are each either filled, locked, or off the grid (no open gap around it at all).");

        public static readonly ModifierDefinition CercleChromatique = new ModifierDefinition(
            ModifierId.CercleChromatique, ModifierCategory.Voisinage, "Color Wheel",
            "+18 pts per group cell whose 4 cardinal neighbors are filled and together show all 4 base colors.");

        public static readonly ModifierDefinition DiagonaleVerrouillee = new ModifierDefinition(
            ModifierId.DiagonaleVerrouillee, ModifierCategory.Voisinage, "Locked Diagonal",
            "+9 pts per group cell diagonally adjacent to a locked cell (boss round).");

        public static readonly ModifierDefinition Monochrome = new ModifierDefinition(
            ModifierId.Monochrome, ModifierCategory.Roguelike, "Monochrome",
            "+3 pts per group cell when the whole group is a single real color with ZERO jokers anywhere in it (stricter than Puriste, which still tolerates jokers).");

        public static readonly ModifierDefinition Contraste = new ModifierDefinition(
            ModifierId.Contraste, ModifierCategory.Couleurs, "Contrast",
            "+6 pts per placed cell that has at least one filled orthogonal neighbor of a different color.");

        public static readonly ModifierDefinition Degrade = new ModifierDefinition(
            ModifierId.Degrade, ModifierCategory.Roguelike, "Momentum",
            "+10 pts whenever this placement's scored group is strictly larger than the group scored by this round's PREVIOUS placement (tracked per round, resets at round start).");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+8 pts per group cell whose 4 diagonal neighbors are all filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+6 pts per group cell orthogonally adjacent to a golden/tinted/multiplier-zone cell.");

        public static readonly ModifierDefinition ArcEnCiel = new ModifierDefinition(
            ModifierId.ArcEnCiel, ModifierCategory.Couleurs, "Rainbow",
            "+25 pts per cleared row/column containing all 4 base colors.");

        public static readonly ModifierDefinition Alternance = new ModifierDefinition(
            ModifierId.Alternance, ModifierCategory.Couleurs, "Alternation",
            "+16 pts per cleared row/column whose colors strictly alternate between exactly 2 colors along its whole length (a joker anywhere breaks the pattern).");

        public static readonly ModifierDefinition Symetrie = new ModifierDefinition(
            ModifierId.Symetrie, ModifierCategory.Connexions, "Symmetry",
            "+20 pts per cleared row/column whose mirror line across the grid's center (row y <-> row 7-y, column x <-> column 7-x) ALSO cleared this same placement with an identical color pattern.");

        public static readonly ModifierDefinition Palindrome = new ModifierDefinition(
            ModifierId.Palindrome, ModifierCategory.Connexions, "Palindrome",
            "+18 pts per cleared row/column whose own color sequence reads the same forwards and backwards.");

        public static readonly ModifierDefinition Gradient = new ModifierDefinition(
            ModifierId.Gradient, ModifierCategory.Connexions, "Gradient",
            "+10 pts per cleared row/column where no two adjacent cells share the same color.");

        public static readonly ModifierDefinition SansDoublon = new ModifierDefinition(
            ModifierId.SansDoublon, ModifierCategory.Couleurs, "No Duplicate",
            "+22 pts per cleared row/column where every color appears at most once (only reachable when locked cells shrink the line below 6 cells, since there are just 5 possible colors including Joker).");

        public static readonly ModifierDefinition Bloc = new ModifierDefinition(
            ModifierId.Bloc, ModifierCategory.Connexions, "Block",
            "+9 pts per cleared row/column made only of contiguous same-color runs of at least 2 cells (no isolated single cell of its own color).");

        public static readonly ModifierDefinition MonochromeLigne = new ModifierDefinition(
            ModifierId.MonochromeLigne, ModifierCategory.Couleurs, "Monochrome Line",
            "+24 pts per cleared row/column that is entirely a single color (jokers ignored).");

        // ---- Third batch (14 more) — basic per-color / per-shape modifiers,
        // on explicit request ("il manque beaucoup de modifiers basique:
        // points doublé pour une couleur, un upgrade par couleur. Idem pour
        // les formes de tuiles"). Each fully doubles this placement's group
        // bonus (100%, not Puriste's 50%) when the placed piece's own color/
        // shape matches — computable directly from GridManager.PlacePiece's
        // existing shape/placedCells parameters, no refactor needed. ----

        public static readonly ModifierDefinition DevotionCoral = new ModifierDefinition(
            ModifierId.DevotionCoral, ModifierCategory.Couleurs, "Coral Devotion",
            "Doubles this placement's group bonus when the piece's own color is Coral.");

        public static readonly ModifierDefinition DevotionTeal = new ModifierDefinition(
            ModifierId.DevotionTeal, ModifierCategory.Couleurs, "Teal Devotion",
            "Doubles this placement's group bonus when the piece's own color is Teal.");

        public static readonly ModifierDefinition DevotionViolet = new ModifierDefinition(
            ModifierId.DevotionViolet, ModifierCategory.Couleurs, "Violet Devotion",
            "Doubles this placement's group bonus when the piece's own color is Violet.");

        public static readonly ModifierDefinition DevotionLime = new ModifierDefinition(
            ModifierId.DevotionLime, ModifierCategory.Couleurs, "Lime Devotion",
            "Doubles this placement's group bonus when the piece's own color is Lime.");

        public static readonly ModifierDefinition FormeSingle = new ModifierDefinition(
            ModifierId.FormeSingle, ModifierCategory.Formes, "Single Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is Single.");

        public static readonly ModifierDefinition FormeDomH = new ModifierDefinition(
            ModifierId.FormeDomH, ModifierCategory.Formes, "Domino H Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is Domino H.");

        public static readonly ModifierDefinition FormeDomV = new ModifierDefinition(
            ModifierId.FormeDomV, ModifierCategory.Formes, "Domino V Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is Domino V.");

        public static readonly ModifierDefinition FormeTriL = new ModifierDefinition(
            ModifierId.FormeTriL, ModifierCategory.Formes, "L-Tromino Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is L-Tromino.");

        public static readonly ModifierDefinition FormeTriIH = new ModifierDefinition(
            ModifierId.FormeTriIH, ModifierCategory.Formes, "I-Tromino H Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is I-Tromino H.");

        public static readonly ModifierDefinition FormeTriIV = new ModifierDefinition(
            ModifierId.FormeTriIV, ModifierCategory.Formes, "I-Tromino V Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is I-Tromino V.");

        public static readonly ModifierDefinition FormeSq2 = new ModifierDefinition(
            ModifierId.FormeSq2, ModifierCategory.Formes, "Square Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is the 2x2 Square.");

        public static readonly ModifierDefinition FormeLTetro = new ModifierDefinition(
            ModifierId.FormeLTetro, ModifierCategory.Formes, "L-Tetromino Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is L-Tetromino.");

        public static readonly ModifierDefinition FormeTTetro = new ModifierDefinition(
            ModifierId.FormeTTetro, ModifierCategory.Formes, "T-Tetromino Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is T-Tetromino.");

        public static readonly ModifierDefinition FormeSTetro = new ModifierDefinition(
            ModifierId.FormeSTetro, ModifierCategory.Formes, "S-Tetromino Specialist",
            "Doubles this placement's group bonus when the placed piece's shape is S-Tetromino.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Puriste, Collectionneur,
            Tricolore, Complementaire, Ilot, Couronne, TrouDansLaGrille, Carrefour, Macon, Demolisseur,
            CoeurDePierre, CercleChromatique, DiagonaleVerrouillee, Monochrome, Contraste, Degrade, Emmitouflee, Jardinier,
            ArcEnCiel, Alternance, Symetrie, Palindrome, Gradient, SansDoublon, Bloc, MonochromeLigne,
            DevotionCoral, DevotionTeal, DevotionViolet, DevotionLime,
            FormeSingle, FormeDomH, FormeDomV, FormeTriL, FormeTriIH, FormeTriIV, FormeSq2, FormeLTetro, FormeTTetro, FormeSTetro
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
