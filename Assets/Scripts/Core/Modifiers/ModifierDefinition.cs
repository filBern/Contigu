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
            "x3 Mult if the seed you plant touches all 3 other colors (side by side with any of its cells). A Joker counts as any one color.");

        public static readonly ModifierDefinition Chaine = new ModifierDefinition(
            ModifierId.Chaine, ModifierCategory.Connexions, "Chain",
            "+1 Mult if the group has 5+ cells.");

        public static readonly ModifierDefinition MegaChaine = new ModifierDefinition(
            ModifierId.MegaChaine, ModifierCategory.Connexions, "Mega Chain",
            "+4 Mult if the group has 10+ cells, +1 Mult per cell past that.");

        public static readonly ModifierDefinition Forteresse = new ModifierDefinition(
            ModifierId.Forteresse, ModifierCategory.Voisinage, "Fortress",
            "+1 Mult per fully surrounded group cell.");

        public static readonly ModifierDefinition Prisonnier = new ModifierDefinition(
            ModifierId.Prisonnier, ModifierCategory.Voisinage, "Prisoner",
            "+1 Mult per group cell with all 4 sides filled.");

        public static readonly ModifierDefinition Architecte = new ModifierDefinition(
            ModifierId.Architecte, ModifierCategory.Roguelike, "Architect",
            "x2 Mult when a 2x2 block is placed.");

        public static readonly ModifierDefinition Collectionneur = new ModifierDefinition(
            ModifierId.Collectionneur, ModifierCategory.Roguelike, "Hoarder",
            "+1 Mult per distinct color in harvested cells.");

        public static readonly ModifierDefinition Tricolore = new ModifierDefinition(
            ModifierId.Tricolore, ModifierCategory.Couleurs, "Triad Bloom",
            "x2 Mult if 2 other colors are touched.");

        public static readonly ModifierDefinition Complementaire = new ModifierDefinition(
            ModifierId.Complementaire, ModifierCategory.Couleurs, "Companion Bloom",
            "x2 Mult if a complementary pair is touched (Poppy/Sunflower or Iris/Fern).");

        public static readonly ModifierDefinition Ilot = new ModifierDefinition(
            ModifierId.Ilot, ModifierCategory.Voisinage, "Islet",
            "x2 Mult if the seed lands as a lone single cell.");

        public static readonly ModifierDefinition Couronne = new ModifierDefinition(
            ModifierId.Couronne, ModifierCategory.Voisinage, "Crown",
            "+1 Mult per group cell on the grid's edge.");

        public static readonly ModifierDefinition Carrefour = new ModifierDefinition(
            ModifierId.Carrefour, ModifierCategory.Voisinage, "Crossroads",
            "+2 Mult per group cell surrounded by 2+ colors.");

        public static readonly ModifierDefinition Macon = new ModifierDefinition(
            ModifierId.Macon, ModifierCategory.Destruction, "Mason",
            "+2 Mult when no row/column is completed.");

        public static readonly ModifierDefinition Demolisseur = new ModifierDefinition(
            ModifierId.Demolisseur, ModifierCategory.Destruction, "Demolisher",
            "x2 Mult per row/column harvested at once, stacking (2+ only).");

        // Requires 4 filled cardinal neighbors showing all 4 base colors at once.
        public static readonly ModifierDefinition CercleChromatique = new ModifierDefinition(
            ModifierId.CercleChromatique, ModifierCategory.Voisinage, "Alchemist's Wheel",
            "+4 Mult per group cell surrounded by all 4 colors.");

        public static readonly ModifierDefinition Monochrome = new ModifierDefinition(
            ModifierId.Monochrome, ModifierCategory.Roguelike, "Ashbound",
            "+0.5 Mult per group cell if the group has no jokers.");

        public static readonly ModifierDefinition Contraste = new ModifierDefinition(
            ModifierId.Contraste, ModifierCategory.Couleurs, "Dissonant Bloom",
            "+1 Mult per placed cell next to a different color.");

        public static readonly ModifierDefinition Degrade = new ModifierDefinition(
            ModifierId.Degrade, ModifierCategory.Roguelike, "Momentum",
            "x2 Mult when this group is bigger than the last one scored.");

        public static readonly ModifierDefinition Emmitouflee = new ModifierDefinition(
            ModifierId.Emmitouflee, ModifierCategory.Voisinage, "Cocooned",
            "+1 Mult per group cell with all 4 diagonals filled.");

        public static readonly ModifierDefinition Jardinier = new ModifierDefinition(
            ModifierId.Jardinier, ModifierCategory.Roguelike, "Gardener",
            "+1 Mult per group cell next to an upgraded sprout.");

        public static readonly ModifierDefinition ArcEnCiel = new ModifierDefinition(
            ModifierId.ArcEnCiel, ModifierCategory.Couleurs, "Rainbow",
            "x2 Mult per harvested line with every color of the run, stacking.");

        public static readonly ModifierDefinition Alternance = new ModifierDefinition(
            ModifierId.Alternance, ModifierCategory.Couleurs, "Alternation",
            "x2 Mult per harvested line alternating between 2 colors, stacking (a joker breaks it).");

        public static readonly ModifierDefinition Palindrome = new ModifierDefinition(
            ModifierId.Palindrome, ModifierCategory.Connexions, "Palindrome",
            "x2 Mult per harvested line whose colors read the same both ways, stacking.");

        public static readonly ModifierDefinition Gradient = new ModifierDefinition(
            ModifierId.Gradient, ModifierCategory.Connexions, "Gradient",
            "PERMANENT: +1 Mult (never resets) per harvested line with no two adjacent same-color cells.");

        public static readonly ModifierDefinition Bloc = new ModifierDefinition(
            ModifierId.Bloc, ModifierCategory.Connexions, "Block",
            "x2 Mult per harvested line with no isolated single-color cell, stacking.");

        public static readonly ModifierDefinition MonochromeLigne = new ModifierDefinition(
            ModifierId.MonochromeLigne, ModifierCategory.Couleurs, "Ashbound Line",
            "x2 Mult per harvested line that's a single color, stacking.");

        public static readonly ModifierDefinition DevotionCoral = new ModifierDefinition(
            ModifierId.DevotionCoral, ModifierCategory.Couleurs, "Poppy Devotion",
            "+3 Mult on Poppy seeds.");

        public static readonly ModifierDefinition DevotionTeal = new ModifierDefinition(
            ModifierId.DevotionTeal, ModifierCategory.Couleurs, "Iris Devotion",
            "+3 Mult on Iris seeds.");

        public static readonly ModifierDefinition DevotionViolet = new ModifierDefinition(
            ModifierId.DevotionViolet, ModifierCategory.Couleurs, "Sunflower Devotion",
            "+3 Mult on Sunflower seeds.");

        public static readonly ModifierDefinition DevotionLime = new ModifierDefinition(
            ModifierId.DevotionLime, ModifierCategory.Couleurs, "Fern Devotion",
            "+3 Mult on Fern seeds.");

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
            ModifierId.GrandFormat, ModifierCategory.Roguelike, "Overgrowth",
            "+1 Mult per cell for seeds with 3+ cells.");

        public static readonly ModifierDefinition HorsNorme = new ModifierDefinition(
            ModifierId.HorsNorme, ModifierCategory.Roguelike, "Mutant Growth",
            "+2 Mult if the seed isn't exactly 3 cells.");

        // Retired: the four color "Glow" fertilizers (EclatCoral/Teal/Violet/Lime) did what Devotion already does. They stay defined so an old
        // save that still holds one loads, but they are no longer in All (never offered, never scored).
        public static readonly ModifierDefinition EclatCoral = new ModifierDefinition(
            ModifierId.EclatCoral, ModifierCategory.Couleurs, "Poppy Glow",
            "+1 Mult per group cell on Poppy seeds.");

        public static readonly ModifierDefinition EclatTeal = new ModifierDefinition(
            ModifierId.EclatTeal, ModifierCategory.Couleurs, "Iris Glow",
            "+1 Mult per group cell on Iris seeds.");

        public static readonly ModifierDefinition EclatViolet = new ModifierDefinition(
            ModifierId.EclatViolet, ModifierCategory.Couleurs, "Sunflower Glow",
            "+1 Mult per group cell on Sunflower seeds.");

        public static readonly ModifierDefinition EclatLime = new ModifierDefinition(
            ModifierId.EclatLime, ModifierCategory.Couleurs, "Fern Glow",
            "+1 Mult per group cell on Fern seeds.");

        public static readonly ModifierDefinition Diagonale = new ModifierDefinition(
            ModifierId.Diagonale, ModifierCategory.Voisinage, "Diagonal",
            "+1 Mult per group cell on either main diagonal.");

        public static readonly ModifierDefinition Nid = new ModifierDefinition(
            ModifierId.Nid, ModifierCategory.Voisinage, "Nest",
            "+1 Mult per group cell with exactly 3 sides filled.");

        public static readonly ModifierDefinition Solitaire = new ModifierDefinition(
            ModifierId.Solitaire, ModifierCategory.Connexions, "Solitaire",
            "x2 Mult when the group is only this seed (2+ cells) - nothing merged in.");

        public static readonly ModifierDefinition EspaceLibre = new ModifierDefinition(
            ModifierId.EspaceLibre, ModifierCategory.Roguelike, "Fallow Ground",
            "x2 Mult when the board ends up with 16 plots filled or fewer.");

        public static readonly ModifierDefinition Rafale = new ModifierDefinition(
            ModifierId.Rafale, ModifierCategory.Destruction, "Burst",
            "x3 Mult when this AND the last placement both harvested a line.");

        public static readonly ModifierDefinition PetitFormat = new ModifierDefinition(
            ModifierId.PetitFormat, ModifierCategory.Roguelike, "Seedling",
            "+1 Mult per cell for seeds with 2 or fewer cells.");

        public static readonly ModifierDefinition Fraicheur = new ModifierDefinition(
            ModifierId.Fraicheur, ModifierCategory.Couleurs, "New Growth",
            "x2 Mult when this color is new to the board.");

        public static readonly ModifierDefinition Pont = new ModifierDefinition(
            ModifierId.Pont, ModifierCategory.Connexions, "Bridge",
            "x2 Mult per extra pre-existing group bridged together, stacking.");

        public static readonly ModifierDefinition Encerclement = new ModifierDefinition(
            ModifierId.Encerclement, ModifierCategory.Voisinage, "Encirclement",
            "+1 Mult per group cell boxed in on all 8 sides (filled or edge).");

        public static readonly ModifierDefinition Boucher = new ModifierDefinition(
            ModifierId.Boucher, ModifierCategory.Voisinage, "Sealer",
            "+1 Mult per pre-existing sprout this placement newly encircles.");

        public static readonly ModifierDefinition GrosseFamille = new ModifierDefinition(
            ModifierId.GrosseFamille, ModifierCategory.Couleurs, "Thriving Colony",
            "x2 Mult when this color forms only one connected group on the board.");

        public static readonly ModifierDefinition Repetition = new ModifierDefinition(
            ModifierId.Repetition, ModifierCategory.Roguelike, "Repetition",
            "xn Mult for n consecutive same-shape placements (x2 on the 2nd, x3 on the 3rd...); resets on a different shape.");

        public static readonly ModifierDefinition AlternancePieces = new ModifierDefinition(
            ModifierId.AlternancePieces, ModifierCategory.Couleurs, "Changeling Bloom",
            "+2 Mult when this seed's color differs from the last one played.");

        public static readonly ModifierDefinition Combo = new ModifierDefinition(
            ModifierId.Combo, ModifierCategory.Destruction, "Combo",
            "x2 the whole score if the last placement harvested a line.");

        public static readonly ModifierDefinition Precision = new ModifierDefinition(
            ModifierId.Precision, ModifierCategory.Voisinage, "Precision",
            "+1 Mult per cell if every cell of the seed touches a filled sprout.");

        public static readonly ModifierDefinition Surpopulation = new ModifierDefinition(
            ModifierId.Surpopulation, ModifierCategory.Voisinage, "Thicket",
            "+1 Mult per cell if every cell of the seed touches 2+ filled sprouts.");

        public static readonly ModifierDefinition Minimaliste = new ModifierDefinition(
            ModifierId.Minimaliste, ModifierCategory.Voisinage, "Lone Bloom",
            "x2 Mult when the seed touches exactly 1 filled sprout, total.");

        public static readonly ModifierDefinition Joker = new ModifierDefinition(
            ModifierId.Joker, ModifierCategory.Roguelike, "Wildcard",
            "Joker sprouts count as the color of one of your Devotion fertilizers.");

        public static readonly ModifierDefinition Densite = new ModifierDefinition(
            ModifierId.Densite, ModifierCategory.Roguelike, "Root Density",
            "+n Mult, n = filled cells on the board ÷ 10 - scales with how full the board is.");

        // Lueur-earning modifiers, each using the same trigger condition as
        // an existing score modifier but paying Lueur (shop currency)
        // instead of points or a multiplier.

        public static readonly ModifierDefinition ArcEnCielLueur = new ModifierDefinition(
            ModifierId.ArcEnCielLueur, ModifierCategory.Couleurs, "Rainbow Glow",
            "+6 Pollen per harvested line with every color of the run, stacking.");

        public static readonly ModifierDefinition AlternanceLueur = new ModifierDefinition(
            ModifierId.AlternanceLueur, ModifierCategory.Couleurs, "Glowing Alternation",
            "+4 Pollen per harvested line alternating between 2 colors, stacking (a joker breaks it).");

        public static readonly ModifierDefinition MonochromeLigneLueur = new ModifierDefinition(
            ModifierId.MonochromeLigneLueur, ModifierCategory.Couleurs, "Radiant Line",
            "+4 Pollen per harvested line that's a single color, stacking.");

        public static readonly ModifierDefinition CollectionneurLueur = new ModifierDefinition(
            ModifierId.CollectionneurLueur, ModifierCategory.Roguelike, "Glowing Hoarder",
            "+2 Pollen per distinct color in harvested cells.");

        public static readonly ModifierDefinition RepetitionLueur = new ModifierDefinition(
            ModifierId.RepetitionLueur, ModifierCategory.Roguelike, "Golden Repetition",
            "+5 Pollen when this seed matches the last one's shape.");

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
            "+1 Mult per 2 fertilizers held, this one included.");

        public static readonly ModifierDefinition Copieur = new ModifierDefinition(
            ModifierId.Copieur, ModifierCategory.Roguelike, "Mimic",
            "Instantly copies whatever fertilizer you bought right before it. Does nothing on your first purchase.");

        public static readonly ModifierDefinition MultCinqRisque = new ModifierDefinition(
            ModifierId.MultCinqRisque, ModifierCategory.Roguelike, "Volatile Brew",
            "+5 Mult, but 1-in-5 chance to lose it at the end of every round.");

        public static readonly ModifierDefinition CartesEnchantees = new ModifierDefinition(
            ModifierId.CartesEnchantees, ModifierCategory.Roguelike, "Enchanted Seeds",
            "+0.1 Mult per upgraded seed in your seed bag, starting from a baseline of 1.");

        public static readonly ModifierDefinition Epuisement = new ModifierDefinition(
            ModifierId.Epuisement, ModifierCategory.Roguelike, "Dwindling",
            "+13 Mult, shrinking a little with every placement for the rest of the run - destroyed once it hits 0.");

        public static readonly ModifierDefinition Multitude = new ModifierDefinition(
            ModifierId.Multitude, ModifierCategory.Roguelike, "Multitude",
            "+1 Mult per 10 seeds currently in your seed bag.");

        public static readonly ModifierDefinition Experience = new ModifierDefinition(
            ModifierId.Experience, ModifierCategory.Roguelike, "Seasoned Growth",
            "+0.1 Mult per upgraded seed you've PLAYED this run, starting from a baseline of 1.");

        // Per-size-tier pairs; values from ScoringConstants.FormeSpecialistMultiplier/FormeGlowBonusPerCell.

        public static readonly ModifierDefinition FormatPetitSpecialiste = new ModifierDefinition(
            ModifierId.FormatPetitSpecialiste, ModifierCategory.Formes, "Seedling Specialist",
            "x2 Mult on seeds with 2 or fewer cells (Single, Domino).");

        public static readonly ModifierDefinition FormatMoyenSpecialiste = new ModifierDefinition(
            ModifierId.FormatMoyenSpecialiste, ModifierCategory.Formes, "Sapling Specialist",
            "x2 Mult on 3-cell seeds (any Tromino).");

        public static readonly ModifierDefinition FormatGrandSpecialiste = new ModifierDefinition(
            ModifierId.FormatGrandSpecialiste, ModifierCategory.Formes, "Overgrowth Specialist",
            "x2 Mult on 4-cell seeds (Square or any Tetromino).");

        public static readonly ModifierDefinition FormatPetitGlow = new ModifierDefinition(
            ModifierId.FormatPetitGlow, ModifierCategory.Formes, "Seedling Glow",
            "+1 Mult per group cell on seeds with 2 or fewer cells (Single, Domino).");

        public static readonly ModifierDefinition FormatMoyenGlow = new ModifierDefinition(
            ModifierId.FormatMoyenGlow, ModifierCategory.Formes, "Sapling Glow",
            "+1 Mult per group cell on 3-cell seeds (any Tromino).");

        public static readonly ModifierDefinition FormatGrandGlow = new ModifierDefinition(
            ModifierId.FormatGrandGlow, ModifierCategory.Formes, "Overgrowth Glow",
            "+1 Mult per group cell on 4-cell seeds (Square or any Tetromino).");

        public static readonly ModifierDefinition Pair = new ModifierDefinition(
            ModifierId.Pair, ModifierCategory.Roguelike, "Waxing",
            "x2 Mult when the scored group has an even number of cells.");

        public static readonly ModifierDefinition Impair = new ModifierDefinition(
            ModifierId.Impair, ModifierCategory.Roguelike, "Waning",
            "x2 Mult when the scored group has an odd number of cells.");

        public static readonly ModifierDefinition Polyvalence = new ModifierDefinition(
            ModifierId.Polyvalence, ModifierCategory.Roguelike, "Hardy Growth",
            "+1 Mult per distinct fertilizer category you hold at least one of.");

        public static readonly ModifierDefinition RenfortJoker = new ModifierDefinition(
            ModifierId.RenfortJoker, ModifierCategory.Roguelike, "Battle Tonic",
            "+25% damage on placements that trigger a Joker combat trait (Spore Burst/Long Reach/Overflow/Pruning). No effect otherwise.");

        public static readonly ModifierDefinition Arsenal = new ModifierDefinition(
            ModifierId.Arsenal, ModifierCategory.Roguelike, "Arsenal",
            "+1 Mult per distinct Joker combat trait kind currently in your seed bag.");

        public static readonly ModifierDefinition CollectionChromatique = new ModifierDefinition(
            ModifierId.CollectionChromatique, ModifierCategory.Couleurs, "Chromatic Hoard",
            "+2 Mult per color for which you hold its Devotion fertilizer.");

        public static readonly ModifierDefinition Cadence = new ModifierDefinition(
            ModifierId.Cadence, ModifierCategory.Roguelike, "Cadence",
            "x2 Mult when this placement matches BOTH a held Pair/Impair condition AND a held Format Specialist tier at once.");

        public static readonly ModifierDefinition Echo = new ModifierDefinition(
            ModifierId.Echo, ModifierCategory.Roguelike, "Echo",
            "Replays the fertilizer right before it in your order, as if you held a second copy of it. No effect if that fertilizer is also Echo.");

        public static readonly ModifierDefinition Siphon = new ModifierDefinition(
            ModifierId.Siphon, ModifierCategory.Roguelike, "Siphon",
            "Converts 4% of the damage from any Joker combat trait into bonus Pollen (max 10).");

        /// <summary>Playtest request: "Ce serait cool un modifier qui rajoute une un 4e slot" — applied/reverted from RunManager.AddActiveModifier/RemoveActiveModifierAt (see DeckManager.SetEffectiveHandSize), unlike every other modifier above which only ever reads state, never grants a standing structural change.</summary>
        public static readonly ModifierDefinition QuatriemeEmplacement = new ModifierDefinition(
            ModifierId.QuatriemeEmplacement, ModifierCategory.Roguelike, "Fourth Slot",
            "Adds a 4th hand slot.");

        public static readonly ModifierDefinition Barometre = new ModifierDefinition(
            ModifierId.Barometre, ModifierCategory.Roguelike, "Barometer",
            "+3 Mult whenever the round's weather isn't Clear skies.");

        public static readonly ModifierDefinition ChasseurTempete = new ModifierDefinition(
            ModifierId.ChasseurTempete, ModifierCategory.Roguelike, "Storm Chaser",
            "x2 Mult while the weather is Frost, Heatwave or Hurricane.");

        public static readonly ModifierDefinition Impermeable = new ModifierDefinition(
            ModifierId.Impermeable, ModifierCategory.Roguelike, "Weatherproof",
            "Frost, Heatwave and Hurricane have no effect on your garden.");

        public static readonly ModifierDefinition Epines = new ModifierDefinition(
            ModifierId.Epines, ModifierCategory.Roguelike, "Thorns",
            "+4 Mult while any enemy has a lock or blight on your plots.");

        public static readonly ModifierDefinition Exterminateur = new ModifierDefinition(
            ModifierId.Exterminateur, ModifierCategory.Roguelike, "Pest Control",
            "+1 Mult per enemy already defeated this round.");

        public static readonly ModifierDefinition Hate = new ModifierDefinition(
            ModifierId.Hate, ModifierCategory.Roguelike, "Haste",
            "+0.1 Mult per seed still left to play this round.");

        public static readonly ModifierDefinition Tourbillon = new ModifierDefinition(
            ModifierId.Tourbillon, ModifierCategory.Roguelike, "Whirlwind",
            "+0.25 Mult per Shuffle you still have.");

        public static readonly ModifierDefinition PierreAngulaire = new ModifierDefinition(
            ModifierId.PierreAngulaire, ModifierCategory.Voisinage, "Cornerstone",
            "x2 Mult when the seed puts a tile on one of the 4 corners.");

        public static readonly ModifierDefinition Haie = new ModifierDefinition(
            ModifierId.Haie, ModifierCategory.Voisinage, "Hedgerow",
            "+1 Mult per tile of the seed that sits on the edge of the garden.");

        public static readonly ModifierDefinition Coeur = new ModifierDefinition(
            ModifierId.Coeur, ModifierCategory.Voisinage, "Heartwood",
            "x2 Mult when the seed puts a tile in the middle of the garden.");

        public static readonly ModifierDefinition Treillis = new ModifierDefinition(
            ModifierId.Treillis, ModifierCategory.Formes, "Trellis",
            "+3 Mult when the seed is a straight vertical piece (1 wide, 2 or more tall).");

        public static readonly ModifierDefinition Sillon = new ModifierDefinition(
            ModifierId.Sillon, ModifierCategory.Formes, "Furrow",
            "+3 Mult when the seed is a straight horizontal piece (1 tall, 2 or more wide).");

        public static readonly ModifierDefinition Haricot = new ModifierDefinition(
            ModifierId.Haricot, ModifierCategory.Formes, "Beanstalk",
            "+1 Mult when the seed is a straight piece of 3 or more tiles.");

        public static readonly ModifierDefinition Patchwork = new ModifierDefinition(
            ModifierId.Patchwork, ModifierCategory.Couleurs, "Patchwork",
            "+1 Mult for each tile of another color touching the seed.");

        public static readonly ModifierDefinition Racines = new ModifierDefinition(
            ModifierId.Racines, ModifierCategory.Connexions, "Taproot",
            "+1 Mult per 3 cells in the scored group.");

        public static readonly ModifierDefinition Emboite = new ModifierDefinition(
            ModifierId.Emboite, ModifierCategory.Voisinage, "Snug Fit",
            "+5 Mult when every plot touching the seed is already planted (or the edge).");

        public static readonly ModifierDefinition Mosaique = new ModifierDefinition(
            ModifierId.Mosaique, ModifierCategory.Couleurs, "Mosaic",
            "+3 Mult when the seed touches 2 or more tiles and none of them shares its color.");

        public static readonly ModifierDefinition Foule = new ModifierDefinition(
            ModifierId.Foule, ModifierCategory.Voisinage, "Crowd",
            "x2 Mult while two thirds of the garden or more is planted.");

        public static readonly ModifierDefinition Prairie = new ModifierDefinition(
            ModifierId.Prairie, ModifierCategory.Couleurs, "Wildflower Meadow",
            "+4 Mult when every color of the run is growing in the garden.");

        public static readonly ModifierDefinition Guirlande = new ModifierDefinition(
            ModifierId.Guirlande, ModifierCategory.Voisinage, "Wreath",
            "+1 Mult per tile of the seed touching 3 or more planted plots.");

        public static readonly ModifierDefinition Pollinisateur = new ModifierDefinition(
            ModifierId.Pollinisateur, ModifierCategory.Couleurs, "Pollinator",
            "+2 Pollen when the seed touches 2 or more different colors.");

        public static readonly ModifierDefinition Miel = new ModifierDefinition(
            ModifierId.Miel, ModifierCategory.Connexions, "Honey",
            "+1 Pollen per 3 cells in the scored group.");

        public static readonly ModifierDefinition Nectar = new ModifierDefinition(
            ModifierId.Nectar, ModifierCategory.Roguelike, "Nectar",
            "+1 Pollen every time you plant a seed.");

        public static readonly ModifierDefinition MoissonCroisee = new ModifierDefinition(
            ModifierId.MoissonCroisee, ModifierCategory.Destruction, "Cross Harvest",
            "+6 Mult when one seed harvests a row and a column at once.");

        public static readonly ModifierDefinition Faucheur = new ModifierDefinition(
            ModifierId.Faucheur, ModifierCategory.Destruction, "Reaper",
            "+3 Mult per line harvested.");

        public static readonly ModifierDefinition TableRase = new ModifierDefinition(
            ModifierId.TableRase, ModifierCategory.Destruction, "Clean Slate",
            "x3 Mult when the seed harvests the last planted tile in the garden.");

        public static readonly ModifierDefinition Dime = new ModifierDefinition(
            ModifierId.Dime, ModifierCategory.Destruction, "Tithe",
            "+2 Pollen per line harvested.");

        public static readonly ModifierDefinition CultureRang = new ModifierDefinition(
            ModifierId.CultureRang, ModifierCategory.Destruction, "Row Crop",
            "x2 Mult when the seed harvests at least one row.");

        public static readonly ModifierDefinition CultureColonne = new ModifierDefinition(
            ModifierId.CultureColonne, ModifierCategory.Destruction, "Column Crop",
            "x2 Mult when the seed harvests at least one column.");

        public static readonly ModifierDefinition Prime = new ModifierDefinition(
            ModifierId.Prime, ModifierCategory.Destruction, "Bounty",
            "+1 Mult per tile harvested.");

        public static readonly ModifierDefinition BriseGlace = new ModifierDefinition(
            ModifierId.BriseGlace, ModifierCategory.Destruction, "Frostbreaker",
            "+6 Mult when the seed breaks a frozen plot free.");

        public static readonly ModifierDefinition Saisons = new ModifierDefinition(
            ModifierId.Saisons, ModifierCategory.Roguelike, "Seasoned",
            "+0.5 Mult per round already cleared this run.");

        public static readonly ModifierDefinition Outsider = new ModifierDefinition(
            ModifierId.Outsider, ModifierCategory.Roguelike, "Underdog",
            "+3 Mult while the enemies still have more than half of their total HP.");

        public static readonly ModifierDefinition CoupFinal = new ModifierDefinition(
            ModifierId.CoupFinal, ModifierCategory.Roguelike, "Finishing Blow",
            "+6 Mult while the enemies are down to a quarter of their total HP or less.");

        public static readonly ModifierDefinition DerniereGraine = new ModifierDefinition(
            ModifierId.DerniereGraine, ModifierCategory.Roguelike, "Last Seed",
            "x3 Mult on the last seed of the round.");

        public static readonly ModifierDefinition PremiereGraine = new ModifierDefinition(
            ModifierId.PremiereGraine, ModifierCategory.Roguelike, "First Seed",
            "x2 Mult on the first seed of the round.");

        public static readonly ModifierDefinition Tirelire = new ModifierDefinition(
            ModifierId.Tirelire, ModifierCategory.Roguelike, "Piggy Bank",
            "+0.1 Mult per Pollen you hold.");

        public static readonly ModifierDefinition BanqueGraines = new ModifierDefinition(
            ModifierId.BanqueGraines, ModifierCategory.Roguelike, "Seed Bank",
            "+0.05 Mult per seed in your bag.");

        public static readonly ModifierDefinition SousBois = new ModifierDefinition(
            ModifierId.SousBois, ModifierCategory.Roguelike, "Underbrush",
            "+1 Mult per enemy still alive.");

        public static readonly ModifierDefinition DanseDePluie = new ModifierDefinition(
            ModifierId.DanseDePluie, ModifierCategory.Roguelike, "Rain Dancer",
            "+4 Mult while it rains (Rain, Snowfall, Sprinkler Shower).");

        public static readonly ModifierDefinition DanseDuSoleil = new ModifierDefinition(
            ModifierId.DanseDuSoleil, ModifierCategory.Roguelike, "Sun Dancer",
            "+4 Mult on Sunshine and Golden Hour days (and their indoor variants).");

        public static readonly ModifierDefinition Rustique = new ModifierDefinition(
            ModifierId.Rustique, ModifierCategory.Roguelike, "Frost Hardy",
            "+4 Mult while the weather is Frost (or the Freezer Door Ajar).");

        public static readonly ModifierDefinition CavalierVent = new ModifierDefinition(
            ModifierId.CavalierVent, ModifierCategory.Roguelike, "Windrider",
            "+4 Mult in Wind and Hurricane weather (and their indoor variants).");

        public static readonly ModifierDefinition Matinal = new ModifierDefinition(
            ModifierId.Matinal, ModifierCategory.Roguelike, "Early Bird",
            "+6 Mult during the first 5 rounds.");

        public static readonly ModifierDefinition Marathonien = new ModifierDefinition(
            ModifierId.Marathonien, ModifierCategory.Roguelike, "Marathoner",
            "+4 Mult from round 9 onwards.");

        public static readonly ModifierDefinition Pourfendeur = new ModifierDefinition(
            ModifierId.Pourfendeur, ModifierCategory.Roguelike, "Bossbane",
            "x2 Mult during boss rounds.");

        public static readonly ModifierDefinition Tonneau = new ModifierDefinition(
            ModifierId.Tonneau, ModifierCategory.Roguelike, "Rain Barrel",
            "+1 Pollen per Shuffle left when you clear a round.");

        public static readonly ModifierDefinition Grange = new ModifierDefinition(
            ModifierId.Grange, ModifierCategory.Roguelike, "Granary",
            "+1 Pollen per fertilizer held when you clear a round.");

        public static readonly ModifierDefinition CharmedPatch = new ModifierDefinition(
            ModifierId.CharmedPatch, ModifierCategory.Roguelike, "Charmed Patch",
            "+1 Mult per upgraded tile in the scored group.");

        public static readonly ModifierDefinition Grafter = new ModifierDefinition(
            ModifierId.Grafter, ModifierCategory.Roguelike, "Grafter",
            "+3 Mult when the seed you plant carries an upgrade.");

        public static readonly ModifierDefinition TwinGraft = new ModifierDefinition(
            ModifierId.TwinGraft, ModifierCategory.Roguelike, "Twin Graft",
            "+5 Mult when the seed you plant carries 2 or more upgrades.");

        public static readonly ModifierDefinition Botanist = new ModifierDefinition(
            ModifierId.Botanist, ModifierCategory.Roguelike, "Botanist",
            "+1 Mult per different kind of upgrade in the garden.");

        public static readonly ModifierDefinition Hothouse = new ModifierDefinition(
            ModifierId.Hothouse, ModifierCategory.Roguelike, "Hothouse",
            "+1 Mult per 2 upgraded tiles in the garden.");

        public static readonly ModifierDefinition GoodNeighbors = new ModifierDefinition(
            ModifierId.GoodNeighbors, ModifierCategory.Voisinage, "Good Neighbors",
            "+2 Mult per upgraded tile touching the seed you plant.");

        public static readonly ModifierDefinition Gleaner = new ModifierDefinition(
            ModifierId.Gleaner, ModifierCategory.Destruction, "Gleaner",
            "+2 Mult per upgraded tile harvested by this placement.");

        public static readonly ModifierDefinition[] All =
        {
            Prisme, Chaine, MegaChaine, Forteresse, Prisonnier, Architecte, Collectionneur,
            Tricolore, Complementaire, Ilot, Couronne, Carrefour, Macon, Demolisseur,
            CercleChromatique, Monochrome, Contraste, Degrade, Emmitouflee, Jardinier,
            ArcEnCiel, Alternance, Palindrome, Gradient, Bloc, MonochromeLigne,
            DevotionCoral, DevotionTeal, DevotionViolet, DevotionLime,
            SlotUn, SlotDeux, SlotTrois, GrandFormat, HorsNorme,
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
            CollectionChromatique, Cadence, Echo, Siphon, QuatriemeEmplacement,
            Barometre, ChasseurTempete, Impermeable, Epines, Exterminateur, Hate, Tourbillon, PierreAngulaire,
            Haie, Coeur, Treillis, Sillon, Haricot, Patchwork, Racines, Emboite,
            Mosaique, Foule, Prairie, Guirlande, Pollinisateur, Miel, Nectar, MoissonCroisee,
            Faucheur, TableRase, Dime, CultureRang, CultureColonne, Prime, BriseGlace, Saisons,
            Outsider, CoupFinal, DerniereGraine, PremiereGraine, Tirelire, BanqueGraines, SousBois, DanseDePluie,
            DanseDuSoleil, Rustique, CavalierVent, Matinal, Marathonien, Pourfendeur, Tonneau, Grange,
            CharmedPatch, Grafter, TwinGraft, Botanist, Hothouse, GoodNeighbors, Gleaner
        };

        /// <summary>The fertilizer a retired id turns into when an old save is loaded (the color Glows become their Devotion); null for every other id.</summary>
        public static ModifierId? RetiredReplacement(ModifierId id)
        {
            switch (id)
            {
                case ModifierId.EclatCoral: return ModifierId.DevotionCoral;
                case ModifierId.EclatTeal: return ModifierId.DevotionTeal;
                case ModifierId.EclatViolet: return ModifierId.DevotionViolet;
                case ModifierId.EclatLime: return ModifierId.DevotionLime;
                default: return null;
            }
        }

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
