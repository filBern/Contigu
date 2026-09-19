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
    /// (no cap on how many can be active at once) and re-evaluate every
    /// placement via <see cref="GridManager.PlacePiece"/>.
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
    /// already had. The second batch's line-level modifiers (Arc-en-ciel,
    /// Alternance, Palindrome, Gradient, Bloc, Monochrome-ligne) needed
    /// <see cref="GridManager.CheckAndClearLines"/> reworked to expose each
    /// cleared row/column's ordered color sequence BEFORE it's wiped — see
    /// README. Several names (Complémentaire's exact color pairing, Maçon,
    /// Démolisseur, and the whole second batch) had only a name + category to
    /// go on, not original detailed rule text, so their exact trigger
    /// condition is this project's best-effort interpretation of the theme —
    /// documented per-modifier below and in the README. Still not delivered:
    /// the destruction modifiers needing a placement/clear history across a
    /// round (Overkill, Cascade, Réaction en chaîne, Combo parfait, Nettoyage,
    /// Récolte) and "Dernier espace" (structurally unreachable — see README).
    /// Trou dans la Grille, Cœur de Pierre, Diagonale Verrouillée and Sans
    /// Doublon (all tied to boss-round locked cells) and Symétrie (unclear,
    /// hard to trigger) were removed on explicit request — see README.
    /// </summary>
    public static class ModifierCatalog
    {
        public static readonly ModifierDefinition Prisme = new ModifierDefinition(
            ModifierId.Prisme, ModifierCategory.Couleurs, "Prism",
            "+20 pts if this placement touches 3 distinct colors.");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chain",
            "+10 pts if the connected group has at least 5 cells.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Mega Chain",
            "+30 pts if the group has at least 10 cells, +5 pts per cell beyond that.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Fortress",
            "+6 pts per group cell fully surrounded.");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisoner",
            "+4 pts per group cell surrounded on its 4 orthogonal sides.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architect",
            "+15 pts every time a 2x2 square block is placed.");

        public static readonly ModifierDefinition Puriste = new ModifierDefinition(
            ModifierId.Puriste, ModifierCategory.Roguelike, "Purist",
            "+50% of the group's points before bonus.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collector",
            "+8 pts per distinct color among cleared cells.");

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Tricolor",
            "+14 pts if this placement touches 2 other colors.");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Complementary",
            "+16 pts if this placement touches a complementary color pair (Coral/Violet or Teal/Lime).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Islet",
            "+8 pts if the placement forms an isolated single-cell group.");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Crown",
            "+5 pts per group cell sitting on the grid's outer edge.");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+12 pts per group cell surrounded on all 4 sides by at least 2 different colors.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "+5 pts for every placement that completes no row/column.");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "+15 pts per row/column completed simultaneously by this placement. (Minimum 2)");

        // Very hard to actually trigger (needs 4 filled cardinal neighbors
        // showing all 4 base colors at once) — bonus raised 18->35 on
        // explicit request to make it worth chasing.
        public static readonly ModifierDefinition CercleChromatique = new ModifierDefinition(
            ModifierId.CercleChromatique, ModifierCategory.Voisinage, "Color Wheel",
            "+35 pts per group cell whose 4 cardinal neighbors are filled by all 4 base colors.");

        public static readonly ModifierDefinition Monochrome = new ModifierDefinition(
            ModifierId.Monochrome, ModifierCategory.Roguelike, "Monochrome",
            "+3 pts per group cell when the whole group has ZERO jokers anywhere in it.");

        public static readonly ModifierDefinition Contraste = new ModifierDefinition(
            ModifierId.Contraste, ModifierCategory.Couleurs, "Contrast",
            "+6 pts per placed cell that has at least one filled orthogonal neighbor of a different color.");

        public static readonly ModifierDefinition Degrade = new ModifierDefinition(
            ModifierId.Degrade, ModifierCategory.Roguelike, "Momentum",
            "+10 pts whenever this placement's scored group is larger than the group scored by previous placement.");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+8 pts per group cell whose 4 diagonal neighbors are all filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+6 pts per group cell orthogonally adjacent to an upgraded cell.");

        public static readonly ModifierDefinition ArcEnCiel = new ModifierDefinition(
            ModifierId.ArcEnCiel, ModifierCategory.Couleurs, "Rainbow",
            "+25 pts per cleared row/column containing all 4 base colors.");

        public static readonly ModifierDefinition Alternance = new ModifierDefinition(
            ModifierId.Alternance, ModifierCategory.Couleurs, "Alternation",
            "+16 pts per cleared row/column whose colors strictly alternate between exactly 2 colors along its whole length (a joker anywhere breaks the pattern).");

        public static readonly ModifierDefinition Palindrome = new ModifierDefinition(
            ModifierId.Palindrome, ModifierCategory.Connexions, "Palindrome",
            "+18 pts per cleared row/column whose own color sequence reads the same forwards and backwards.");

        public static readonly ModifierDefinition Gradient = new ModifierDefinition(
            ModifierId.Gradient, ModifierCategory.Connexions, "Gradient",
            "+10 pts per cleared row/column where no two adjacent cells share the same color.");

        public static readonly ModifierDefinition Bloc = new ModifierDefinition(
            ModifierId.Bloc, ModifierCategory.Connexions, "Block",
            "+9 pts per cleared row/column without isolated single cell of its own color.");

        public static readonly ModifierDefinition MonochromeLigne = new ModifierDefinition(
            ModifierId.MonochromeLigne, ModifierCategory.Couleurs, "Monochrome Line",
            "+24 pts per cleared row/column that is entirely a single color.");

        public static readonly ModifierDefinition DevotionCoral = new ModifierDefinition(
            ModifierId.DevotionCoral, ModifierCategory.Couleurs, "Coral Devotion",
            "Doubles this placement's group bonus when placing a Coral piece.");

        public static readonly ModifierDefinition DevotionTeal = new ModifierDefinition(
            ModifierId.DevotionTeal, ModifierCategory.Couleurs, "Teal Devotion",
            "Doubles this placement's group bonus when placing a Teal piece.");

        public static readonly ModifierDefinition DevotionViolet = new ModifierDefinition(
            ModifierId.DevotionViolet, ModifierCategory.Couleurs, "Violet Devotion",
            "Doubles this placement's group bonus when placing a Violet piece.");

        public static readonly ModifierDefinition DevotionLime = new ModifierDefinition(
            ModifierId.DevotionLime, ModifierCategory.Couleurs, "Lime Devotion",
            "Doubles this placement's group bonus when placing a Lime piece.");

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

        // ---- Fourth batch: hand-slot, piece-size and per-color-tile bonuses (on explicit request) ----
        // The 3 slot modifiers can't be evaluated by GridManager at all — it has
        // no idea which of the 3 hand slots a piece came from, only RunManager's
        // PlacePiece(handIndex, x, y) does — so unlike every other modifier here,
        // they're resolved post-hoc in RunManager, the same pattern already used
        // for the second-batch PieceTrait kinds (see RunManager.ApplyHandSlotModifierBonus).

        public static readonly ModifierDefinition SlotUn = new ModifierDefinition(
            ModifierId.SlotUn, ModifierCategory.Roguelike, "Slot 1 Loyalty",
            "Doubles this placement's group bonus when playing from hand slot 1.");

        public static readonly ModifierDefinition SlotDeux = new ModifierDefinition(
            ModifierId.SlotDeux, ModifierCategory.Roguelike, "Slot 2 Loyalty",
            "Doubles this placement's group bonus when playing from hand slot 2.");

        public static readonly ModifierDefinition SlotTrois = new ModifierDefinition(
            ModifierId.SlotTrois, ModifierCategory.Roguelike, "Slot 3 Loyalty",
            "Doubles this placement's group bonus when playing from hand slot 3.");

        public static readonly ModifierDefinition GrandFormat = new ModifierDefinition(
            ModifierId.GrandFormat, ModifierCategory.Roguelike, "Large Format",
            "+8 pts per placed cell when the piece has 3 or more cells.");

        public static readonly ModifierDefinition HorsNorme = new ModifierDefinition(
            ModifierId.HorsNorme, ModifierCategory.Roguelike, "Off-Size",
            "+12 pts when the piece does NOT have exactly 3 cells.");

        public static readonly ModifierDefinition EclatCoral = new ModifierDefinition(
            ModifierId.EclatCoral, ModifierCategory.Couleurs, "Coral Glow",
            "+4 pts per group cell when placing a Coral piece.");

        public static readonly ModifierDefinition EclatTeal = new ModifierDefinition(
            ModifierId.EclatTeal, ModifierCategory.Couleurs, "Teal Glow",
            "+4 pts per group cell when placing a Teal piece.");

        public static readonly ModifierDefinition EclatViolet = new ModifierDefinition(
            ModifierId.EclatViolet, ModifierCategory.Couleurs, "Violet Glow",
            "+4 pts per group cell when placing a Violet piece.");

        public static readonly ModifierDefinition EclatLime = new ModifierDefinition(
            ModifierId.EclatLime, ModifierCategory.Couleurs, "Lime Glow",
            "+4 pts per group cell when placing a Lime piece.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Puriste, Collectionneur,
            Tricolore, Complementaire, Ilot, Couronne, Carrefour, Macon, Demolisseur,
            CercleChromatique, Monochrome, Contraste, Degrade, Emmitouflee, Jardinier,
            ArcEnCiel, Alternance, Palindrome, Gradient, Bloc, MonochromeLigne,
            DevotionCoral, DevotionTeal, DevotionViolet, DevotionLime,
            FormeSingle, FormeDomH, FormeDomV, FormeTriL, FormeTriIH, FormeTriIV, FormeSq2, FormeLTetro, FormeTTetro, FormeSTetro,
            SlotUn, SlotDeux, SlotTrois, GrandFormat, HorsNorme, EclatCoral, EclatTeal, EclatViolet, EclatLime
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
