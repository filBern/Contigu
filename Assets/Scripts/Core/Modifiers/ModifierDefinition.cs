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
            "x3 multiplier if this placement touches 3 distinct colors.");

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
            "x2 multiplier every time a 2x2 square block is placed.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collector",
            "+8 pts per distinct color among cleared cells.");

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Tricolor",
            "x2 multiplier if this placement touches 2 other colors.");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Complementary",
            "x2 multiplier if this placement touches a complementary color pair (Red/Yellow or Blue/Green).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Islet",
            "x2 multiplier if the placement forms an isolated single-cell group.");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Crown",
            "+5 pts per group cell sitting on the grid's outer edge.");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+12 pts per group cell surrounded on all 4 sides by at least 2 different colors.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "x2 multiplier for every placement that completes no row/column.");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "x2 multiplier per row/column completed simultaneously by this placement, stacking. (Minimum 2)");

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
            "x2 multiplier whenever this placement's scored group is larger than the group scored by previous placement.");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+8 pts per group cell whose 4 diagonal neighbors are all filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+6 pts per group cell orthogonally adjacent to an upgraded cell.");

        public static readonly ModifierDefinition ArcEnCiel = new ModifierDefinition(
            ModifierId.ArcEnCiel, ModifierCategory.Couleurs, "Rainbow",
            "x2 multiplier per cleared row/column containing all 4 base colors, stacking.");

        public static readonly ModifierDefinition Alternance = new ModifierDefinition(
            ModifierId.Alternance, ModifierCategory.Couleurs, "Alternation",
            "x2 multiplier per cleared row/column whose colors strictly alternate between exactly 2 colors along its whole length (a joker anywhere breaks the pattern), stacking.");

        public static readonly ModifierDefinition Palindrome = new ModifierDefinition(
            ModifierId.Palindrome, ModifierCategory.Connexions, "Palindrome",
            "x2 multiplier per cleared row/column whose own color sequence reads the same forwards and backwards, stacking.");

        public static readonly ModifierDefinition Gradient = new ModifierDefinition(
            ModifierId.Gradient, ModifierCategory.Connexions, "Gradient",
            "PERMANENT: every cleared row/column where no two adjacent cells share the same color adds +1 to a multiplier that never resets, not even between rounds — applies as xn to every placement for the rest of the run.");

        public static readonly ModifierDefinition Bloc = new ModifierDefinition(
            ModifierId.Bloc, ModifierCategory.Connexions, "Block",
            "x2 multiplier per cleared row/column without isolated single cell of its own color, stacking.");

        public static readonly ModifierDefinition MonochromeLigne = new ModifierDefinition(
            ModifierId.MonochromeLigne, ModifierCategory.Couleurs, "Monochrome Line",
            "x2 multiplier per cleared row/column that is entirely a single color, stacking.");

        // Devotion/Forme* converted from "doubles this placement's group
        // bonus" (additive) to a genuine xN ModifierMultiplier, on explicit
        // request ("Tous les modifiers par rapport à la couleur de pièce ou
        // type de pièce doivent une version +pts et une version +mult") —
        // Éclat (below) already covers the +pts side for colors; the 10 new
        // Forme*Points modifiers (ninth batch, further down) now cover it
        // for shapes.

        public static readonly ModifierDefinition DevotionCoral = new ModifierDefinition(
            ModifierId.DevotionCoral, ModifierCategory.Couleurs, "Red Devotion",
            "x2 multiplier when placing a Red piece.");

        public static readonly ModifierDefinition DevotionTeal = new ModifierDefinition(
            ModifierId.DevotionTeal, ModifierCategory.Couleurs, "Blue Devotion",
            "x2 multiplier when placing a Blue piece.");

        public static readonly ModifierDefinition DevotionViolet = new ModifierDefinition(
            ModifierId.DevotionViolet, ModifierCategory.Couleurs, "Yellow Devotion",
            "x2 multiplier when placing a Yellow piece.");

        public static readonly ModifierDefinition DevotionLime = new ModifierDefinition(
            ModifierId.DevotionLime, ModifierCategory.Couleurs, "Green Devotion",
            "x2 multiplier when placing a Green piece.");

        // ---- Fourth batch: hand-slot, piece-size and per-color-tile bonuses (on explicit request) ----
        // The 3 slot modifiers can't be evaluated by GridManager at all — it has
        // no idea which of the 3 hand slots a piece came from, only RunManager's
        // PlacePiece(handIndex, x, y) does — so unlike every other modifier here,
        // they're resolved post-hoc in RunManager, the same pattern already used
        // for the second-batch PieceTrait kinds (see RunManager.ApplyHandSlotModifierBonus).

        public static readonly ModifierDefinition SlotUn = new ModifierDefinition(
            ModifierId.SlotUn, ModifierCategory.Roguelike, "Slot 1 Loyalty",
            "x2 multiplier on this placement's ENTIRE score when playing from hand slot 1.");

        public static readonly ModifierDefinition SlotDeux = new ModifierDefinition(
            ModifierId.SlotDeux, ModifierCategory.Roguelike, "Slot 2 Loyalty",
            "x2 multiplier on this placement's ENTIRE score when playing from hand slot 2.");

        public static readonly ModifierDefinition SlotTrois = new ModifierDefinition(
            ModifierId.SlotTrois, ModifierCategory.Roguelike, "Slot 3 Loyalty",
            "x2 multiplier on this placement's ENTIRE score when playing from hand slot 3.");

        public static readonly ModifierDefinition GrandFormat = new ModifierDefinition(
            ModifierId.GrandFormat, ModifierCategory.Roguelike, "Large Format",
            "+8 pts per placed cell when the piece has 3 or more cells.");

        public static readonly ModifierDefinition HorsNorme = new ModifierDefinition(
            ModifierId.HorsNorme, ModifierCategory.Roguelike, "Off-Size",
            "+12 pts when the piece does NOT have exactly 3 cells.");

        public static readonly ModifierDefinition EclatCoral = new ModifierDefinition(
            ModifierId.EclatCoral, ModifierCategory.Couleurs, "Red Glow",
            "+4 pts per group cell when placing a Red piece.");

        public static readonly ModifierDefinition EclatTeal = new ModifierDefinition(
            ModifierId.EclatTeal, ModifierCategory.Couleurs, "Blue Glow",
            "+4 pts per group cell when placing a Blue piece.");

        public static readonly ModifierDefinition EclatViolet = new ModifierDefinition(
            ModifierId.EclatViolet, ModifierCategory.Couleurs, "Yellow Glow",
            "+4 pts per group cell when placing a Yellow piece.");

        public static readonly ModifierDefinition EclatLime = new ModifierDefinition(
            ModifierId.EclatLime, ModifierCategory.Couleurs, "Green Glow",
            "+4 pts per group cell when placing a Green piece.");

        // ---- Fifth batch: 8 new ideas (on explicit request) ----

        public static readonly ModifierDefinition Diagonale = new ModifierDefinition(
            ModifierId.Diagonale, ModifierCategory.Voisinage, "Diagonal",
            "+5 pts per group cell sitting on either of the board's two main diagonals.");

        public static readonly ModifierDefinition Nid = new ModifierDefinition(
            ModifierId.Nid, ModifierCategory.Voisinage, "Nest",
            "+3 pts per group cell with exactly 3 of its 4 orthogonal neighbors filled.");

        public static readonly ModifierDefinition Solitaire = new ModifierDefinition(
            ModifierId.Solitaire, ModifierCategory.Connexions, "Solitaire",
            "x2 multiplier when this placement's group is entirely its own piece (more than 1 cell) — nothing pre-existing merged into it.");

        public static readonly ModifierDefinition EspaceLibre = new ModifierDefinition(
            ModifierId.EspaceLibre, ModifierCategory.Roguelike, "Open Space",
            "x2 multiplier whenever the board is at most 25% filled once this placement is fully resolved.");

        public static readonly ModifierDefinition Rafale = new ModifierDefinition(
            ModifierId.Rafale, ModifierCategory.Destruction, "Burst",
            "x3 multiplier when this placement clears a line AND the immediately previous one this round also did.");

        public static readonly ModifierDefinition PetitFormat = new ModifierDefinition(
            ModifierId.PetitFormat, ModifierCategory.Roguelike, "Small Format",
            "+5 pts per placed cell when the piece has at most 2 cells.");

        public static readonly ModifierDefinition Fraicheur = new ModifierDefinition(
            ModifierId.Fraicheur, ModifierCategory.Couleurs, "Freshness",
            "x2 multiplier when this placement's color isn't anywhere else on the board yet.");

        // ---- Sixth batch: 11 more, from a player-authored brainstorm list
        // (Équilibriste, Longue série and a second "Solitaire" idea were
        // dropped — see README) ----

        public static readonly ModifierDefinition Pont = new ModifierDefinition(
            ModifierId.Pont, ModifierCategory.Connexions, "Bridge",
            "x2 multiplier per pre-existing group this placement bridges together beyond the first one, stacking.");

        public static readonly ModifierDefinition Encerclement = new ModifierDefinition(
            ModifierId.Encerclement, ModifierCategory.Voisinage, "Encirclement",
            "+6 pts per group cell whose 8 surrounding tiles are all filled OR off the edge of the grid.");

        public static readonly ModifierDefinition Boucher = new ModifierDefinition(
            ModifierId.Boucher, ModifierCategory.Voisinage, "Sealer",
            "+10 pts per pre-existing tile that this placement itself causes to become encircled (see Encirclement).");

        public static readonly ModifierDefinition GrosseFamille = new ModifierDefinition(
            ModifierId.GrosseFamille, ModifierCategory.Couleurs, "Big Family",
            "x2 multiplier when this placement's color exists in exactly one connected group on the whole board.");

        public static readonly ModifierDefinition Repetition = new ModifierDefinition(
            ModifierId.Repetition, ModifierCategory.Roguelike, "Repetition",
            "xn multiplier where n is how many placements in a row share this piece's shape: x2 on the 2nd consecutive placement of the same shape, x3 on the 3rd, and so on (resets to x1 the moment a different shape is placed).");

        public static readonly ModifierDefinition AlternancePieces = new ModifierDefinition(
            ModifierId.AlternancePieces, ModifierCategory.Couleurs, "Color Switch",
            "x2 multiplier when this piece's color differs from the immediately previous placement's color this round.");

        public static readonly ModifierDefinition Combo = new ModifierDefinition(
            ModifierId.Combo, ModifierCategory.Destruction, "Combo",
            "x2 this placement's ENTIRE score when the immediately previous placement this round cleared a line.");

        public static readonly ModifierDefinition Precision = new ModifierDefinition(
            ModifierId.Precision, ModifierCategory.Voisinage, "Precision",
            "+5 pts per placed cell when EVERY cell of this piece touches at least one pre-existing filled tile.");

        public static readonly ModifierDefinition Surpopulation = new ModifierDefinition(
            ModifierId.Surpopulation, ModifierCategory.Voisinage, "Overcrowding",
            "+8 pts per placed cell when EVERY cell of this piece touches at least 2 pre-existing filled tiles.");

        public static readonly ModifierDefinition Minimaliste = new ModifierDefinition(
            ModifierId.Minimaliste, ModifierCategory.Voisinage, "Minimalist",
            "x2 multiplier when this piece's whole footprint touches EXACTLY one pre-existing filled tile, total.");

        public static readonly ModifierDefinition Joker = new ModifierDefinition(
            ModifierId.Joker, ModifierCategory.Roguelike, "Wildcard",
            "A placed Joker tile counts as whichever base color would score the most from your Devotion/Glow modifiers.");

        // ---- Seventh batch: progressive modifiers that scale with a
        // running counter instead of a fixed strength, on explicit request
        // ("+5 ou x1 pour chaque pièce d'un même type de suite, +10 ou x2
        // pour la 2e de suite, etc... (x1 par modifiers possédé) (x0.1 par
        // tuile sur la grille)") — Repetition (above) was adapted the same
        // way instead of being duplicated. ----

        public static readonly ModifierDefinition Densite = new ModifierDefinition(
            ModifierId.Densite, ModifierCategory.Roguelike, "Density",
            "xn multiplier where n is how many cells are filled on the board after this placement, divided by 10 (rounded down) — the fuller the board, the stronger this gets.");

        // ---- Eighth batch: Lueur-earning modifiers, each adapted from an
        // existing score modifier of the same shape instead of a new
        // condition (on explicit request: "il faut ajouter quelques
        // modifiers qui rapportent des lueur... tu peux t'inspirer des
        // modifiers qu'on a déjà et les adapter en version bonus lueur") —
        // same trigger condition as their inspiration, paying Lueur (the
        // shop currency) instead of points/a multiplier.

        public static readonly ModifierDefinition ArcEnCielLueur = new ModifierDefinition(
            ModifierId.ArcEnCielLueur, ModifierCategory.Couleurs, "Rainbow Glow",
            "+6 Lueur per cleared row/column containing all 4 base colors, stacking.");

        public static readonly ModifierDefinition AlternanceLueur = new ModifierDefinition(
            ModifierId.AlternanceLueur, ModifierCategory.Couleurs, "Glowing Alternation",
            "+4 Lueur per cleared row/column whose colors strictly alternate between exactly 2 colors along its whole length (a joker anywhere breaks the pattern), stacking.");

        public static readonly ModifierDefinition MonochromeLigneLueur = new ModifierDefinition(
            ModifierId.MonochromeLigneLueur, ModifierCategory.Couleurs, "Radiant Line",
            "+4 Lueur per cleared row/column that is entirely a single color, stacking.");

        public static readonly ModifierDefinition CollectionneurLueur = new ModifierDefinition(
            ModifierId.CollectionneurLueur, ModifierCategory.Roguelike, "Glowing Collector",
            "+2 Lueur per distinct color among this placement's cleared cells.");

        public static readonly ModifierDefinition RepetitionLueur = new ModifierDefinition(
            ModifierId.RepetitionLueur, ModifierCategory.Roguelike, "Golden Repetition",
            "+5 Lueur when this piece is the same shape as the immediately previous placement this round.");

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
            "+1 Mult per modifier held, this one included.");

        public static readonly ModifierDefinition Copieur = new ModifierDefinition(
            ModifierId.Copieur, ModifierCategory.Roguelike, "Mimic",
            "The instant you buy this, it's replaced by another copy of whichever modifier you bought immediately before it. Does nothing if it's the very first modifier you buy this run.");

        public static readonly ModifierDefinition MultCinqRisque = new ModifierDefinition(
            ModifierId.MultCinqRisque, ModifierCategory.Roguelike, "Risky Mult",
            "+5 Mult. No condition — but a 1-in-5 chance to lose this modifier at the end of every round.");

        public static readonly ModifierDefinition CartesEnchantees = new ModifierDefinition(
            ModifierId.CartesEnchantees, ModifierCategory.Roguelike, "Enchanted Cards",
            "+0.1 Mult per upgraded card in your deck, counting from a baseline of 1 (so it's never quite zero).");

        public static readonly ModifierDefinition Epuisement = new ModifierDefinition(
            ModifierId.Epuisement, ModifierCategory.Roguelike, "Dwindling",
            "+100 pts, dropping by 5 after every placement for the rest of the run (down to 0, permanent, never resets).");

        public static readonly ModifierDefinition Multitude = new ModifierDefinition(
            ModifierId.Multitude, ModifierCategory.Roguelike, "Multitude",
            "+1 pt per piece currently in your deck.");

        public static readonly ModifierDefinition Experience = new ModifierDefinition(
            ModifierId.Experience, ModifierCategory.Roguelike, "Experience",
            "+0.1 Mult per special (upgraded) piece you've PLAYED this run, counting from a baseline of 1 — Enchanted Cards' played-count counterpart.");

        // ---- Eleventh batch: curation pass (on explicit request — see
        // ModifierId's own doc comment on this batch) — replaces the 10
        // FormeX "Specialist" + 10 FormeXPoints "Glow" definitions removed
        // above with 3 per-size-tier pairs. Same multiplier/bonus values
        // (ScoringConstants.FormeSpecialistMultiplier/
        // FormeGlowBonusPerCell) as every one of the 20 they replace.

        public static readonly ModifierDefinition FormatPetitSpecialiste = new ModifierDefinition(
            ModifierId.FormatPetitSpecialiste, ModifierCategory.Formes, "Small Format Specialist",
            "x2 multiplier when the placed piece has 2 cells or fewer (Single, Domino H/V).");

        public static readonly ModifierDefinition FormatMoyenSpecialiste = new ModifierDefinition(
            ModifierId.FormatMoyenSpecialiste, ModifierCategory.Formes, "Medium Format Specialist",
            "x2 multiplier when the placed piece has exactly 3 cells (any of the 3 Trominoes).");

        public static readonly ModifierDefinition FormatGrandSpecialiste = new ModifierDefinition(
            ModifierId.FormatGrandSpecialiste, ModifierCategory.Formes, "Large Format Specialist",
            "x2 multiplier when the placed piece has 4 cells (Square or any Tetromino).");

        public static readonly ModifierDefinition FormatPetitGlow = new ModifierDefinition(
            ModifierId.FormatPetitGlow, ModifierCategory.Formes, "Small Format Glow",
            "+4 pts per scored group cell when the placed piece has 2 cells or fewer (Single, Domino H/V).");

        public static readonly ModifierDefinition FormatMoyenGlow = new ModifierDefinition(
            ModifierId.FormatMoyenGlow, ModifierCategory.Formes, "Medium Format Glow",
            "+4 pts per scored group cell when the placed piece has exactly 3 cells (any of the 3 Trominoes).");

        public static readonly ModifierDefinition FormatGrandGlow = new ModifierDefinition(
            ModifierId.FormatGrandGlow, ModifierCategory.Formes, "Large Format Glow",
            "+4 pts per scored group cell when the placed piece has 4 cells (Square or any Tetromino).");

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
            FormatPetitGlow, FormatMoyenGlow, FormatGrandGlow
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
