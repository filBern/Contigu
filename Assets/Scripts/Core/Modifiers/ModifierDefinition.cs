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
    ///
    /// Every description below was shortened on explicit request ("Toutes
    /// les descriptions d'upgrades et modifiers sont beaucoup trop longues,
    /// ça devient chiant a lire a la longue, peux-tu les réduires") — same
    /// mechanic and numbers, fewer words, "multiplier" contracted to "Mult"
    /// throughout (still colorized red by DescriptionTextFormatter, which
    /// matches "mult" case-insensitively) to match the newer +Mult
    /// modifiers' own phrasing.
    /// </summary>
    public static class ModifierCatalog
    {
        public static readonly ModifierDefinition Prisme = new ModifierDefinition(
            ModifierId.Prisme, ModifierCategory.Couleurs, "Prism",
            "x3 Mult if 3 different colors are touched.");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chain",
            "+10 pts if the group has 5+ cells.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Mega Chain",
            "+30 pts if the group has 10+ cells, +5 pts per cell past that.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Fortress",
            "+6 pts per fully surrounded group cell.");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisoner",
            "+4 pts per group cell with all 4 sides filled.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architect",
            "x2 Mult when a 2x2 block is placed.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collector",
            "+8 pts per distinct color in cleared cells.");

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Tricolor",
            "x2 Mult if 2 other colors are touched.");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Complementary",
            "x2 Mult if a complementary pair is touched (Red/Yellow or Blue/Green).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Islet",
            "x2 Mult if the piece lands as a lone single cell.");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Crown",
            "+5 pts per group cell on the grid's edge.");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+12 pts per group cell surrounded by 2+ colors.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "+2 Mult when no row/column is completed.");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "x2 Mult per row/column cleared at once, stacking (2+ only).");

        // Very hard to actually trigger (needs 4 filled cardinal neighbors
        // showing all 4 base colors at once) — bonus raised 18->35 on
        // explicit request to make it worth chasing.
        public static readonly ModifierDefinition CercleChromatique = new ModifierDefinition(
            ModifierId.CercleChromatique, ModifierCategory.Voisinage, "Color Wheel",
            "+35 pts per group cell surrounded by all 4 colors.");

        public static readonly ModifierDefinition Monochrome = new ModifierDefinition(
            ModifierId.Monochrome, ModifierCategory.Roguelike, "Monochrome",
            "+3 pts per group cell if the group has no jokers.");

        public static readonly ModifierDefinition Contraste = new ModifierDefinition(
            ModifierId.Contraste, ModifierCategory.Couleurs, "Contrast",
            "+6 pts per placed cell next to a different color.");

        public static readonly ModifierDefinition Degrade = new ModifierDefinition(
            ModifierId.Degrade, ModifierCategory.Roguelike, "Momentum",
            "x2 Mult when this group is bigger than the last one scored.");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+8 pts per group cell with all 4 diagonals filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+6 pts per group cell next to an upgraded tile.");

        public static readonly ModifierDefinition ArcEnCiel = new ModifierDefinition(
            ModifierId.ArcEnCiel, ModifierCategory.Couleurs, "Rainbow",
            "x2 Mult per cleared line with all 4 colors, stacking.");

        public static readonly ModifierDefinition Alternance = new ModifierDefinition(
            ModifierId.Alternance, ModifierCategory.Couleurs, "Alternation",
            "x2 Mult per cleared line alternating between 2 colors, stacking (a joker breaks it).");

        public static readonly ModifierDefinition Palindrome = new ModifierDefinition(
            ModifierId.Palindrome, ModifierCategory.Connexions, "Palindrome",
            "x2 Mult per cleared line whose colors read the same both ways, stacking.");

        public static readonly ModifierDefinition Gradient = new ModifierDefinition(
            ModifierId.Gradient, ModifierCategory.Connexions, "Gradient",
            "PERMANENT: +1 Mult (never resets) per cleared line with no two adjacent same-color cells.");

        public static readonly ModifierDefinition Bloc = new ModifierDefinition(
            ModifierId.Bloc, ModifierCategory.Connexions, "Block",
            "x2 Mult per cleared line with no isolated single-color cell, stacking.");

        public static readonly ModifierDefinition MonochromeLigne = new ModifierDefinition(
            ModifierId.MonochromeLigne, ModifierCategory.Couleurs, "Monochrome Line",
            "x2 Mult per cleared line that's a single color, stacking.");

        // Devotion/Forme* converted from "doubles this placement's group
        // bonus" (additive) to a genuine xN ModifierMultiplier, on explicit
        // request ("Tous les modifiers par rapport à la couleur de pièce ou
        // type de pièce doivent une version +pts et une version +mult") —
        // Éclat (below) already covers the +pts side for colors; the 10 new
        // Forme*Points modifiers (ninth batch, further down) now cover it
        // for shapes. Devotion (only — Forme* stays a genuine xN) was later
        // converted BACK to additive, at a higher flat value, on explicit
        // request ("converting some multiplicative sources to additive") —
        // its per-color condition fires reliably enough (~1-in-4 placements)
        // that the old xN was compounding too easily with the game's other
        // "always-on" multiplicative modifiers.

        public static readonly ModifierDefinition DevotionCoral = new ModifierDefinition(
            ModifierId.DevotionCoral, ModifierCategory.Couleurs, "Red Devotion",
            "+3 Mult on Red pieces.");

        public static readonly ModifierDefinition DevotionTeal = new ModifierDefinition(
            ModifierId.DevotionTeal, ModifierCategory.Couleurs, "Blue Devotion",
            "+3 Mult on Blue pieces.");

        public static readonly ModifierDefinition DevotionViolet = new ModifierDefinition(
            ModifierId.DevotionViolet, ModifierCategory.Couleurs, "Yellow Devotion",
            "+3 Mult on Yellow pieces.");

        public static readonly ModifierDefinition DevotionLime = new ModifierDefinition(
            ModifierId.DevotionLime, ModifierCategory.Couleurs, "Green Devotion",
            "+3 Mult on Green pieces.");

        // ---- Fourth batch: hand-slot, piece-size and per-color-tile bonuses (on explicit request) ----
        // The 3 slot modifiers can't be evaluated by GridManager at all — it has
        // no idea which of the 3 hand slots a piece came from, only RunManager's
        // PlacePiece(handIndex, x, y) does — so unlike every other modifier here,
        // they're resolved post-hoc in RunManager, the same pattern already used
        // for the second-batch PieceTrait kinds (see RunManager.ApplyHandSlotModifierBonus).

        public static readonly ModifierDefinition SlotUn = new ModifierDefinition(
            ModifierId.SlotUn, ModifierCategory.Roguelike, "Slot 1 Loyalty",
            "+3 Mult when played from hand slot 1.");

        public static readonly ModifierDefinition SlotDeux = new ModifierDefinition(
            ModifierId.SlotDeux, ModifierCategory.Roguelike, "Slot 2 Loyalty",
            "+3 Mult when played from hand slot 2.");

        public static readonly ModifierDefinition SlotTrois = new ModifierDefinition(
            ModifierId.SlotTrois, ModifierCategory.Roguelike, "Slot 3 Loyalty",
            "+3 Mult when played from hand slot 3.");

        public static readonly ModifierDefinition GrandFormat = new ModifierDefinition(
            ModifierId.GrandFormat, ModifierCategory.Roguelike, "Large Format",
            "+8 pts per cell for pieces with 3+ cells.");

        public static readonly ModifierDefinition HorsNorme = new ModifierDefinition(
            ModifierId.HorsNorme, ModifierCategory.Roguelike, "Off-Size",
            "+12 pts if the piece isn't exactly 3 cells.");

        public static readonly ModifierDefinition EclatCoral = new ModifierDefinition(
            ModifierId.EclatCoral, ModifierCategory.Couleurs, "Red Glow",
            "+4 pts per group cell on Red pieces.");

        public static readonly ModifierDefinition EclatTeal = new ModifierDefinition(
            ModifierId.EclatTeal, ModifierCategory.Couleurs, "Blue Glow",
            "+4 pts per group cell on Blue pieces.");

        public static readonly ModifierDefinition EclatViolet = new ModifierDefinition(
            ModifierId.EclatViolet, ModifierCategory.Couleurs, "Yellow Glow",
            "+4 pts per group cell on Yellow pieces.");

        public static readonly ModifierDefinition EclatLime = new ModifierDefinition(
            ModifierId.EclatLime, ModifierCategory.Couleurs, "Green Glow",
            "+4 pts per group cell on Green pieces.");

        // ---- Fifth batch: 8 new ideas (on explicit request) ----

        public static readonly ModifierDefinition Diagonale = new ModifierDefinition(
            ModifierId.Diagonale, ModifierCategory.Voisinage, "Diagonal",
            "+5 pts per group cell on either main diagonal.");

        public static readonly ModifierDefinition Nid = new ModifierDefinition(
            ModifierId.Nid, ModifierCategory.Voisinage, "Nest",
            "+3 pts per group cell with exactly 3 sides filled.");

        public static readonly ModifierDefinition Solitaire = new ModifierDefinition(
            ModifierId.Solitaire, ModifierCategory.Connexions, "Solitaire",
            "x2 Mult when the group is only this piece (2+ cells) — nothing merged in.");

        public static readonly ModifierDefinition EspaceLibre = new ModifierDefinition(
            ModifierId.EspaceLibre, ModifierCategory.Roguelike, "Open Space",
            "x2 Mult when the board ends up 25% filled or less.");

        public static readonly ModifierDefinition Rafale = new ModifierDefinition(
            ModifierId.Rafale, ModifierCategory.Destruction, "Burst",
            "x3 Mult when this AND the last placement both cleared a line.");

        public static readonly ModifierDefinition PetitFormat = new ModifierDefinition(
            ModifierId.PetitFormat, ModifierCategory.Roguelike, "Small Format",
            "+5 pts per cell for pieces with 2 or fewer cells.");

        public static readonly ModifierDefinition Fraicheur = new ModifierDefinition(
            ModifierId.Fraicheur, ModifierCategory.Couleurs, "Freshness",
            "x2 Mult when this color is new to the board.");

        // ---- Sixth batch: 11 more, from a player-authored brainstorm list
        // (Équilibriste, Longue série and a second "Solitaire" idea were
        // dropped — see README) ----

        public static readonly ModifierDefinition Pont = new ModifierDefinition(
            ModifierId.Pont, ModifierCategory.Connexions, "Bridge",
            "x2 Mult per extra pre-existing group bridged together, stacking.");

        public static readonly ModifierDefinition Encerclement = new ModifierDefinition(
            ModifierId.Encerclement, ModifierCategory.Voisinage, "Encirclement",
            "+6 pts per group cell boxed in on all 8 sides (filled or edge).");

        public static readonly ModifierDefinition Boucher = new ModifierDefinition(
            ModifierId.Boucher, ModifierCategory.Voisinage, "Sealer",
            "+10 pts per pre-existing tile this placement newly encircles.");

        public static readonly ModifierDefinition GrosseFamille = new ModifierDefinition(
            ModifierId.GrosseFamille, ModifierCategory.Couleurs, "Big Family",
            "x2 Mult when this color forms only one connected group on the board.");

        public static readonly ModifierDefinition Repetition = new ModifierDefinition(
            ModifierId.Repetition, ModifierCategory.Roguelike, "Repetition",
            "xn Mult for n consecutive same-shape placements (x2 on the 2nd, x3 on the 3rd...); resets on a different shape.");

        public static readonly ModifierDefinition AlternancePieces = new ModifierDefinition(
            ModifierId.AlternancePieces, ModifierCategory.Couleurs, "Color Switch",
            "+2 Mult when this piece's color differs from the last one played.");

        public static readonly ModifierDefinition Combo = new ModifierDefinition(
            ModifierId.Combo, ModifierCategory.Destruction, "Combo",
            "x2 the whole score if the last placement cleared a line.");

        public static readonly ModifierDefinition Precision = new ModifierDefinition(
            ModifierId.Precision, ModifierCategory.Voisinage, "Precision",
            "+5 pts per cell if every cell of the piece touches a filled tile.");

        public static readonly ModifierDefinition Surpopulation = new ModifierDefinition(
            ModifierId.Surpopulation, ModifierCategory.Voisinage, "Overcrowding",
            "+8 pts per cell if every cell of the piece touches 2+ filled tiles.");

        public static readonly ModifierDefinition Minimaliste = new ModifierDefinition(
            ModifierId.Minimaliste, ModifierCategory.Voisinage, "Minimalist",
            "x2 Mult when the piece touches exactly 1 filled tile, total.");

        public static readonly ModifierDefinition Joker = new ModifierDefinition(
            ModifierId.Joker, ModifierCategory.Roguelike, "Wildcard",
            "Joker tiles count as whichever color scores best with your Devotion/Glow modifiers.");

        // ---- Seventh batch: progressive modifiers that scale with a
        // running counter instead of a fixed strength, on explicit request
        // ("+5 ou x1 pour chaque pièce d'un même type de suite, +10 ou x2
        // pour la 2e de suite, etc... (x1 par modifiers possédé) (x0.1 par
        // tuile sur la grille)") — Repetition (above) was adapted the same
        // way instead of being duplicated. ----

        public static readonly ModifierDefinition Densite = new ModifierDefinition(
            ModifierId.Densite, ModifierCategory.Roguelike, "Density",
            "+n Mult, n = filled cells on the board ÷ 10 — scales with how full the board is.");

        // ---- Eighth batch: Lueur-earning modifiers, each adapted from an
        // existing score modifier of the same shape instead of a new
        // condition (on explicit request: "il faut ajouter quelques
        // modifiers qui rapportent des lueur... tu peux t'inspirer des
        // modifiers qu'on a déjà et les adapter en version bonus lueur") —
        // same trigger condition as their inspiration, paying Lueur (the
        // shop currency) instead of points/a multiplier.

        public static readonly ModifierDefinition ArcEnCielLueur = new ModifierDefinition(
            ModifierId.ArcEnCielLueur, ModifierCategory.Couleurs, "Rainbow Glow",
            "+6 ◆ per cleared line with all 4 colors, stacking.");

        public static readonly ModifierDefinition AlternanceLueur = new ModifierDefinition(
            ModifierId.AlternanceLueur, ModifierCategory.Couleurs, "Glowing Alternation",
            "+4 ◆ per cleared line alternating between 2 colors, stacking (a joker breaks it).");

        public static readonly ModifierDefinition MonochromeLigneLueur = new ModifierDefinition(
            ModifierId.MonochromeLigneLueur, ModifierCategory.Couleurs, "Radiant Line",
            "+4 ◆ per cleared line that's a single color, stacking.");

        public static readonly ModifierDefinition CollectionneurLueur = new ModifierDefinition(
            ModifierId.CollectionneurLueur, ModifierCategory.Roguelike, "Glowing Collector",
            "+2 ◆ per distinct color in cleared cells.");

        public static readonly ModifierDefinition RepetitionLueur = new ModifierDefinition(
            ModifierId.RepetitionLueur, ModifierCategory.Roguelike, "Golden Repetition",
            "+5 ◆ when this piece matches the last one's shape.");

        // ---- Ninth batch, on explicit request ----

        public static readonly ModifierDefinition MultUn = new ModifierDefinition(
            ModifierId.MultUn, ModifierCategory.Roguelike, "Mult +1",
            "+1 Mult. No condition.");

        public static readonly ModifierDefinition MultDeux = new ModifierDefinition(
            ModifierId.MultDeux, ModifierCategory.Roguelike, "Mult +2",
            "+2 Mult. No condition.");

        public static readonly ModifierDefinition MultQuatre = new ModifierDefinition(
            ModifierId.MultQuatre, ModifierCategory.Roguelike, "Mult +4",
            "+4 Mult. No condition.");

        public static readonly ModifierDefinition Solidarite = new ModifierDefinition(
            ModifierId.Solidarite, ModifierCategory.Roguelike, "Solidarity",
            "+1 Mult per 2 modifiers held, this one included.");

        public static readonly ModifierDefinition Copieur = new ModifierDefinition(
            ModifierId.Copieur, ModifierCategory.Roguelike, "Mimic",
            "Instantly copies whatever modifier you bought right before it. Does nothing on your first purchase.");

        public static readonly ModifierDefinition MultCinqRisque = new ModifierDefinition(
            ModifierId.MultCinqRisque, ModifierCategory.Roguelike, "Risky Mult",
            "+5 Mult, but 1-in-5 chance to lose it at the end of every round.");

        public static readonly ModifierDefinition CartesEnchantees = new ModifierDefinition(
            ModifierId.CartesEnchantees, ModifierCategory.Roguelike, "Enchanted Cards",
            "+0.1 Mult per upgraded card in your deck, starting from a baseline of 1.");

        public static readonly ModifierDefinition Epuisement = new ModifierDefinition(
            ModifierId.Epuisement, ModifierCategory.Roguelike, "Dwindling",
            "+100 pts, -5 per placement for the rest of the run — destroyed once it hits 0.");

        public static readonly ModifierDefinition Multitude = new ModifierDefinition(
            ModifierId.Multitude, ModifierCategory.Roguelike, "Multitude",
            "+1 pt per piece currently in your deck.");

        public static readonly ModifierDefinition Experience = new ModifierDefinition(
            ModifierId.Experience, ModifierCategory.Roguelike, "Experience",
            "+0.1 Mult per upgraded piece you've PLAYED this run, starting from a baseline of 1.");

        // ---- Eleventh batch: curation pass (on explicit request — see
        // ModifierId's own doc comment on this batch) — replaces the 10
        // FormeX "Specialist" + 10 FormeXPoints "Glow" definitions removed
        // above with 3 per-size-tier pairs. Same multiplier/bonus values
        // (ScoringConstants.FormeSpecialistMultiplier/
        // FormeGlowBonusPerCell) as every one of the 20 they replace.

        public static readonly ModifierDefinition FormatPetitSpecialiste = new ModifierDefinition(
            ModifierId.FormatPetitSpecialiste, ModifierCategory.Formes, "Small Format Specialist",
            "x2 Mult on pieces with 2 or fewer cells (Single, Domino).");

        public static readonly ModifierDefinition FormatMoyenSpecialiste = new ModifierDefinition(
            ModifierId.FormatMoyenSpecialiste, ModifierCategory.Formes, "Medium Format Specialist",
            "x2 Mult on 3-cell pieces (any Tromino).");

        public static readonly ModifierDefinition FormatGrandSpecialiste = new ModifierDefinition(
            ModifierId.FormatGrandSpecialiste, ModifierCategory.Formes, "Large Format Specialist",
            "x2 Mult on 4-cell pieces (Square or any Tetromino).");

        public static readonly ModifierDefinition FormatPetitGlow = new ModifierDefinition(
            ModifierId.FormatPetitGlow, ModifierCategory.Formes, "Small Format Glow",
            "+4 pts per group cell on pieces with 2 or fewer cells (Single, Domino).");

        public static readonly ModifierDefinition FormatMoyenGlow = new ModifierDefinition(
            ModifierId.FormatMoyenGlow, ModifierCategory.Formes, "Medium Format Glow",
            "+4 pts per group cell on 3-cell pieces (any Tromino).");

        public static readonly ModifierDefinition FormatGrandGlow = new ModifierDefinition(
            ModifierId.FormatGrandGlow, ModifierCategory.Formes, "Large Format Glow",
            "+4 pts per group cell on 4-cell pieces (Square or any Tetromino).");

        // ---- Twelfth batch: per-exact-shape "Mastery" modifiers (on
        // explicit request) — a flat +1 pts every time you place this
        // exact shape. Each is its own catalog entry so the player picks
        // which shape to specialize in; holding N copies of the same one
        // stacks to +N automatically (the existing unlimited-duplicate-
        // modifiers system already sums every copy's own event), no
        // separate per-shape counter needed.

        public static readonly ModifierDefinition MasterySingle = new ModifierDefinition(
            ModifierId.MasterySingle, ModifierCategory.Formes, "Single Mastery",
            "+1 pts when you place a Single. Stacks with every copy held.");

        public static readonly ModifierDefinition MasteryDomH = new ModifierDefinition(
            ModifierId.MasteryDomH, ModifierCategory.Formes, "Domino Mastery",
            "+1 pts when you place a Domino. Stacks with every copy held.");

        public static readonly ModifierDefinition MasteryTriL = new ModifierDefinition(
            ModifierId.MasteryTriL, ModifierCategory.Formes, "L-Tromino Mastery",
            "+1 pts when you place an L-Tromino. Stacks with every copy held.");

        public static readonly ModifierDefinition MasteryTriIH = new ModifierDefinition(
            ModifierId.MasteryTriIH, ModifierCategory.Formes, "I-Tromino Mastery",
            "+1 pts when you place an I-Tromino. Stacks with every copy held.");

        public static readonly ModifierDefinition MasterySq2 = new ModifierDefinition(
            ModifierId.MasterySq2, ModifierCategory.Formes, "Square Mastery",
            "+1 pts when you place a Square. Stacks with every copy held.");

        public static readonly ModifierDefinition MasteryLTetro = new ModifierDefinition(
            ModifierId.MasteryLTetro, ModifierCategory.Formes, "L-Tetromino Mastery",
            "+1 pts when you place an L-Tetromino. Stacks with every copy held.");

        public static readonly ModifierDefinition MasteryTTetro = new ModifierDefinition(
            ModifierId.MasteryTTetro, ModifierCategory.Formes, "T-Tetromino Mastery",
            "+1 pts when you place a T-Tetromino. Stacks with every copy held.");

        public static readonly ModifierDefinition MasterySTetro = new ModifierDefinition(
            ModifierId.MasterySTetro, ModifierCategory.Formes, "S-Tetromino Mastery",
            "+1 pts when you place an S-Tetromino. Stacks with every copy held.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Collectionneur,
            Tricolore, Complementaire, Ilot, Couronne, Carrefour, Macon, Demolisseur,
            CercleChromatique, Monochrome, Contraste, Degrade, Emmitouflee, Jardinier,
            ArcEnCiel, Alternance, Palindrome, Gradient, Bloc, MonochromeLigne,
            DevotionCoral, DevotionTeal, DevotionViolet, DevotionLime,
            SlotUn, SlotDeux, SlotTrois, GrandFormat, HorsNorme, EclatCoral, EclatTeal, EclatViolet, EclatLime,
            Diagonale, Nid, Solitaire, EspaceLibre, Rafale, PetitFormat, Fraicheur,
            Pont, Encerclement, Boucher, GrosseFamille, Repetition, AlternancePieces, Combo, Precision, Surpopulation, Minimaliste, Joker,
            Densite,
            ArcEnCielLueur, AlternanceLueur, MonochromeLigneLueur, CollectionneurLueur, RepetitionLueur,
            MultUn, MultDeux, MultQuatre,
            Solidarite, Copieur, MultCinqRisque, CartesEnchantees, Epuisement, Multitude,
            Experience,
            FormatPetitSpecialiste, FormatMoyenSpecialiste, FormatGrandSpecialiste,
            FormatPetitGlow, FormatMoyenGlow, FormatGrandGlow,
            MasterySingle, MasteryDomH, MasteryTriL, MasteryTriIH, MasterySq2, MasteryLTetro, MasteryTTetro, MasterySTetro
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
