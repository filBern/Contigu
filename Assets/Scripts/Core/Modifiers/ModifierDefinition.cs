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

        /// <summary>The per-shape modifiers (Formes.*).</summary>
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
    /// Line-level modifiers (Arc-en-ciel, Alternance, Palindrome, Gradient,
    /// Bloc, Monochrome-ligne) rely on <see cref="GridManager.CheckAndClearLines"/>
    /// exposing each cleared row/column's ordered color sequence before it's
    /// wiped. Descriptions use "Mult" for "multiplier" throughout — still
    /// colorized red by DescriptionTextFormatter, which matches "mult"
    /// case-insensitively.
    /// </summary>
    public static class ModifierCatalog
    {
        public static readonly ModifierDefinition Prisme = new ModifierDefinition(
            ModifierId.Prisme, ModifierCategory.Couleurs, "Prism",
            "x3 Mult if 3 different colors are touched other than the one placed.");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chain",
            "+15 pts if the group has 5+ cells.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Mega Chain",
            "+45 pts if the group has 10+ cells, +8 pts per cell past that.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Fortress",
            "+9 pts per fully surrounded group cell.");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisoner",
            "+6 pts per group cell with all 4 sides filled.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architect",
            "x2 Mult when a 2x2 block is placed.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Collector",
            "+12 pts per distinct color in cleared cells.");

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
            "+8 pts per group cell on the grid's edge.");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+18 pts per group cell surrounded by 2+ colors.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "+2 Mult when no row/column is completed.");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "x2 Mult per row/column cleared at once, stacking (2+ only).");

        // Requires 4 filled cardinal neighbors showing all 4 base colors at once.
        public static readonly ModifierDefinition CercleChromatique = new ModifierDefinition(
            ModifierId.CercleChromatique, ModifierCategory.Voisinage, "Color Wheel",
            "+52 pts per group cell surrounded by all 4 colors.");

        public static readonly ModifierDefinition Monochrome = new ModifierDefinition(
            ModifierId.Monochrome, ModifierCategory.Roguelike, "Monochrome",
            "+5 pts per group cell if the group has no jokers.");

        public static readonly ModifierDefinition Contraste = new ModifierDefinition(
            ModifierId.Contraste, ModifierCategory.Couleurs, "Contrast",
            "+9 pts per placed cell next to a different color.");

        public static readonly ModifierDefinition Degrade = new ModifierDefinition(
            ModifierId.Degrade, ModifierCategory.Roguelike, "Momentum",
            "x2 Mult when this group is bigger than the last one scored.");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+12 pts per group cell with all 4 diagonals filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+9 pts per group cell next to an upgraded tile.");

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

        // The 3 slot modifiers can't be evaluated by GridManager — it doesn't
        // know which hand slot a piece came from, only RunManager's
        // PlacePiece(handIndex, x, y) does — so they're resolved post-hoc in
        // RunManager (see RunManager.ApplyHandSlotModifierBonus).

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
            "+12 pts per cell for pieces with 3+ cells.");

        public static readonly ModifierDefinition HorsNorme = new ModifierDefinition(
            ModifierId.HorsNorme, ModifierCategory.Roguelike, "Off-Size",
            "+18 pts if the piece isn't exactly 3 cells.");

        public static readonly ModifierDefinition EclatCoral = new ModifierDefinition(
            ModifierId.EclatCoral, ModifierCategory.Couleurs, "Red Glow",
            "+6 pts per group cell on Red pieces.");

        public static readonly ModifierDefinition EclatTeal = new ModifierDefinition(
            ModifierId.EclatTeal, ModifierCategory.Couleurs, "Blue Glow",
            "+6 pts per group cell on Blue pieces.");

        public static readonly ModifierDefinition EclatViolet = new ModifierDefinition(
            ModifierId.EclatViolet, ModifierCategory.Couleurs, "Yellow Glow",
            "+6 pts per group cell on Yellow pieces.");

        public static readonly ModifierDefinition EclatLime = new ModifierDefinition(
            ModifierId.EclatLime, ModifierCategory.Couleurs, "Green Glow",
            "+6 pts per group cell on Green pieces.");

        public static readonly ModifierDefinition Diagonale = new ModifierDefinition(
            ModifierId.Diagonale, ModifierCategory.Voisinage, "Diagonal",
            "+8 pts per group cell on either main diagonal.");

        public static readonly ModifierDefinition Nid = new ModifierDefinition(
            ModifierId.Nid, ModifierCategory.Voisinage, "Nest",
            "+5 pts per group cell with exactly 3 sides filled.");

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
            "+8 pts per cell for pieces with 2 or fewer cells.");

        public static readonly ModifierDefinition Fraicheur = new ModifierDefinition(
            ModifierId.Fraicheur, ModifierCategory.Couleurs, "Freshness",
            "x2 Mult when this color is new to the board.");

        public static readonly ModifierDefinition Pont = new ModifierDefinition(
            ModifierId.Pont, ModifierCategory.Connexions, "Bridge",
            "x2 Mult per extra pre-existing group bridged together, stacking.");

        public static readonly ModifierDefinition Encerclement = new ModifierDefinition(
            ModifierId.Encerclement, ModifierCategory.Voisinage, "Encirclement",
            "+9 pts per group cell boxed in on all 8 sides (filled or edge).");

        public static readonly ModifierDefinition Boucher = new ModifierDefinition(
            ModifierId.Boucher, ModifierCategory.Voisinage, "Sealer",
            "+15 pts per pre-existing tile this placement newly encircles.");

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
            "+8 pts per cell if every cell of the piece touches a filled tile.");

        public static readonly ModifierDefinition Surpopulation = new ModifierDefinition(
            ModifierId.Surpopulation, ModifierCategory.Voisinage, "Overcrowding",
            "+12 pts per cell if every cell of the piece touches 2+ filled tiles.");

        public static readonly ModifierDefinition Minimaliste = new ModifierDefinition(
            ModifierId.Minimaliste, ModifierCategory.Voisinage, "Minimalist",
            "x2 Mult when the piece touches exactly 1 filled tile, total.");

        public static readonly ModifierDefinition Joker = new ModifierDefinition(
            ModifierId.Joker, ModifierCategory.Roguelike, "Wildcard",
            "Joker tiles count as whichever color scores best with your Devotion/Glow modifiers.");

        public static readonly ModifierDefinition Densite = new ModifierDefinition(
            ModifierId.Densite, ModifierCategory.Roguelike, "Density",
            "+n Mult, n = filled cells on the board ÷ 10 — scales with how full the board is.");

        // Lueur-earning modifiers, each using the same trigger condition as
        // an existing score modifier but paying Lueur (shop currency)
        // instead of points or a multiplier.

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
            "+150 pts, -5 per placement for the rest of the run — destroyed once it hits 0.");

        public static readonly ModifierDefinition Multitude = new ModifierDefinition(
            ModifierId.Multitude, ModifierCategory.Roguelike, "Multitude",
            "+2 pts per piece currently in your deck.");

        public static readonly ModifierDefinition Experience = new ModifierDefinition(
            ModifierId.Experience, ModifierCategory.Roguelike, "Experience",
            "+0.1 Mult per upgraded piece you've PLAYED this run, starting from a baseline of 1.");

        // Per-size-tier pairs; values from ScoringConstants.FormeSpecialistMultiplier/FormeGlowBonusPerCell.

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
            "+6 pts per group cell on pieces with 2 or fewer cells (Single, Domino).");

        public static readonly ModifierDefinition FormatMoyenGlow = new ModifierDefinition(
            ModifierId.FormatMoyenGlow, ModifierCategory.Formes, "Medium Format Glow",
            "+6 pts per group cell on 3-cell pieces (any Tromino).");

        public static readonly ModifierDefinition FormatGrandGlow = new ModifierDefinition(
            ModifierId.FormatGrandGlow, ModifierCategory.Formes, "Large Format Glow",
            "+6 pts per group cell on 4-cell pieces (Square or any Tetromino).");

        public static readonly ModifierDefinition Pair = new ModifierDefinition(
            ModifierId.Pair, ModifierCategory.Roguelike, "Even",
            "x2 Mult when the scored group has an even number of cells.");

        public static readonly ModifierDefinition Impair = new ModifierDefinition(
            ModifierId.Impair, ModifierCategory.Roguelike, "Odd",
            "x2 Mult when the scored group has an odd number of cells.");

        // The Devotion/Éclat same-color pairing bonus needs no definition
        // here — see GridManager.ApplyDevotionEclatPairBonus.

        public static readonly ModifierDefinition Polyvalence = new ModifierDefinition(
            ModifierId.Polyvalence, ModifierCategory.Roguelike, "Polyvalence",
            "+1 Mult per distinct modifier category you hold at least one of.");

        public static readonly ModifierDefinition RenfortJoker = new ModifierDefinition(
            ModifierId.RenfortJoker, ModifierCategory.Roguelike, "Combat Boost",
            "+25% damage on placements that trigger a Joker combat trait (Bombe/Range/Éclat/Précision/Sangsue). No effect otherwise.");

        public static readonly ModifierDefinition Arsenal = new ModifierDefinition(
            ModifierId.Arsenal, ModifierCategory.Roguelike, "Arsenal",
            "+1 Mult per distinct Joker combat trait kind currently in your deck.");

        public static readonly ModifierDefinition CollectionChromatique = new ModifierDefinition(
            ModifierId.CollectionChromatique, ModifierCategory.Couleurs, "Color Collection",
            "+2 Mult per color for which you hold BOTH its Devotion and Éclat modifier.");

        public static readonly ModifierDefinition Cadence = new ModifierDefinition(
            ModifierId.Cadence, ModifierCategory.Roguelike, "Cadence",
            "x2 Mult when this placement matches BOTH a held Pair/Impair condition AND a held Format Specialist tier at once.");

        public static readonly ModifierDefinition Echo = new ModifierDefinition(
            ModifierId.Echo, ModifierCategory.Roguelike, "Echo",
            "Replays whatever modifier sits immediately to its left in your order, as if you held a second copy of it. No effect if that neighbor is also Echo.");

        public static readonly ModifierDefinition Siphon = new ModifierDefinition(
            ModifierId.Siphon, ModifierCategory.Roguelike, "Siphon",
            "Converts 15% of the damage from any Joker combat trait into bonus Lueur. Stacks with Sangsue.");

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
            Pair, Impair,
            Polyvalence, RenfortJoker, Arsenal,
            CollectionChromatique, Cadence, Echo, Siphon
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
