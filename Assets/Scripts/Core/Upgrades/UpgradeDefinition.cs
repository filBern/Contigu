namespace Contigu.Core
{
    /// <summary>
    /// Static description of one upgrade. <see cref="RequiresSubChoice"/> flags
    /// upgrades that need the player to pick a piece type (and/or target color)
    /// before they can be applied (Retirer/Dupliquer/Recolorer).
    /// <see cref="Rarity"/> weights how often it shows up in a draft — see
    /// UpgradeSystem.PickWeighted — and is shown to the player alongside
    /// <see cref="Pool"/> ("type") on the draft card / tile-badge tooltip.
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public readonly UpgradeId Id;
        public readonly UpgradePool Pool;
        public readonly string Name;
        public readonly string Description;
        public readonly bool RequiresSubChoice;
        public readonly UpgradeRarity Rarity;

        public UpgradeDefinition(UpgradeId id, UpgradePool pool, string name, string description, bool requiresSubChoice, UpgradeRarity rarity)
        {
            Id = id;
            Pool = pool;
            Name = name;
            Description = description;
            RequiresSubChoice = requiresSubChoice;
            Rarity = rarity;
        }
    }

    public static class UpgradeCatalog
    {
        public static readonly UpgradeDefinition ReplacePiece = new UpgradeDefinition(
            UpgradeId.ReplacePiece, UpgradePool.Bank, "Transmute a Seed",
            "Choose 1 seed to replace with a duplicate of another.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Propagate a Cutting",
            "Choose 1 seed to duplicate in your seed bag.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Wild Seed",
            "Adds a joker seed (random shape) to the seed bag.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Rebloom a Seed",
            "Choose a seed type and color; recolor one copy.", true, UpgradeRarity.Uncommon);

        /// <summary>
        /// A gamble with no sub-choice (applies immediately on purchase, like
        /// Joker — see RunManager.BuyUpgradeSlot/GrantRandomModifier): grants
        /// a random modifier with no way to know which in advance.
        /// </summary>
        public static readonly UpgradeDefinition RandomModifier = new UpgradeDefinition(
            UpgradeId.RandomModifier, UpgradePool.Modifier, "Alchemical Boon",
            "Grants one random fertilizer you don't already have.", false, UpgradeRarity.Uncommon);

        /// <summary>
        /// Bank pool (adds to the deck, like Joker/Duplicate) but with a
        /// sub-choice — unlike every other Bank sub-choice (Retirer/
        /// Dupliquer/Recolorer), which picks a type already in the deck (see
        /// UpgradeSystem.GetCandidateTypesFor), this one's candidates are
        /// freshly rolled pieces that don't exist in the deck yet, each
        /// independently possibly pre-enchanted with a Grid-pool tile trait
        /// (see EconomyConstants.RandomPieceTraitChancePercent,
        /// UpgradeSystem.GetCandidatePiecesFor) — so it gets its own
        /// RunManager.PendingUpgradePieceCandidates/ResolveUpgradePieceChoice
        /// pair and Presentation.PieceChoiceView instead of reusing DraftView
        /// (which only knows how to show plain (Shape, Color) types, never
        /// a trait preview).
        /// </summary>
        public static readonly UpgradeDefinition RandomPiece = new UpgradeDefinition(
            UpgradeId.RandomPiece, UpgradePool.Bank, "Fresh Sprout",
            "Choose 1 of 5 fresh seeds, each with an upgraded tile, to add to your seed bag.", true, UpgradeRarity.Common);

        /// <summary>
        /// A level system that amplifies whatever the chosen modifier
        /// already does (see ModifierLevelUtility), rather than swapping to
        /// a named next tier. A third Bank sub-choice shape distinct from
        /// the others: unlike Retirer/Dupliquer/Recolorer (pick a type from
        /// the deck) and Random Piece (pick from freshly-rolled candidates),
        /// this picks a slot the player already owns — RunManager.ActiveModifiers
        /// itself is the full candidate list, so there's no Pending*Candidates
        /// list to populate, just RunManager.ResolveModifierUpgradeChoice(slotIndex)
        /// once Presentation.ModifierUpgradeChoiceView shows the picker.
        /// Refused outright (see RunManager.BuyUpgradeSlot) if the player
        /// owns no modifiers yet.
        /// </summary>
        public static readonly UpgradeDefinition ModifierUpgrade = new UpgradeDefinition(
            UpgradeId.ModifierUpgrade, UpgradePool.Modifier, "Fertilizer Upgrade",
            "Choose one of your active fertilizers to level up — its effect gets stronger.", true, UpgradeRarity.Uncommon);

        // Descriptions below describe the trait itself rather than how many
        // pieces get it — that count is a shop mechanic (see
        // EconomyConstants.ShopTileChoiceCount, picked by the player in
        // TileChoiceView), not a property of the upgrade.
        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Golden Cells",
            "Scores +18 flat when placed.", false, UpgradeRarity.Common);

        /// <summary>
        /// Retired from the shop/draft pools (see All/GridPool below) — playtesting found it read as nearly
        /// identical to MultiplierZone ("Tinted vs multiplier tile, c'est pas clair"), both being a flat
        /// "doubles score" tile that differed only in exactly which score counted. The definition itself
        /// (and PieceTraitKind.Tinted/Cell.IsTinted it maps to, see UpgradeSystem.TraitKindFor) stays in
        /// place rather than being torn out, since it's still exercised directly by name in
        /// UpgradeSystemTests/GridManagerTests.
        /// </summary>
        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Tinted Cells",
            "Doubles group and golden score (not harvest), tinted to the seed's color.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "Doubles the whole placement's score, harvest included.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition BlastTile = new UpgradeDefinition(
            UpgradeId.BlastTile, UpgradePool.Grid, "Blast Sprout",
            "Makes itself and its 4 neighbors score golden if same color.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MultiplierBeacon = new UpgradeDefinition(
            UpgradeId.MultiplierBeacon, UpgradePool.Grid, "Multiplier Beacon",
            "Turns its whole row/column into multipliers for that placement.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MirrorTile = new UpgradeDefinition(
            UpgradeId.MirrorTile, UpgradePool.Grid, "Mirror Sprout",
            "Copies its group bonus onto one random other sprout in the group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition Seeder = new UpgradeDefinition(
            UpgradeId.Seeder, UpgradePool.Grid, "Seeder",
            "Stays golden for the rest of the round instead of scoring once.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition CatalystTile = new UpgradeDefinition(
            UpgradeId.CatalystTile, UpgradePool.Grid, "Catalyst Sprout",
            "Scores extra for every pre-existing cell merged into its group.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition TwinTile = new UpgradeDefinition(
            UpgradeId.TwinTile, UpgradePool.Grid, "Twin Sprout",
            "Copies its group bonus onto EVERY other sprout in the group.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DetonatorTile = new UpgradeDefinition(
            UpgradeId.DetonatorTile, UpgradePool.Grid, "Detonator Sprout",
            "Doubles the harvest bonus if it harvests a row or column.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ChameleonTile = new UpgradeDefinition(
            UpgradeId.ChameleonTile, UpgradePool.Grid, "Chameleon Sprout",
            "Recolors the WHOLE seed to match a filled neighbor when placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition SparkTile = new UpgradeDefinition(
            UpgradeId.SparkTile, UpgradePool.Grid, "Spark Sprout",
            "Scores more the longer since the last harvest this round.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition VoidTile = new UpgradeDefinition(
            UpgradeId.VoidTile, UpgradePool.Grid, "Void Sprout",
            "Uproots one random filled sprout elsewhere on the grid (+10 for the sprout broken).", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition BastionTile = new UpgradeDefinition(
            UpgradeId.BastionTile, UpgradePool.Grid, "Bastion Sprout",
            "Won't be removed when a line is harvested.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition KamikazeTile = new UpgradeDefinition(
            UpgradeId.KamikazeTile, UpgradePool.Grid, "Kamikaze Sprout",
            "Destroys itself and its 8 surrounding sprouts (+6 per sprout destroyed).", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition VenomTile = new UpgradeDefinition(
            UpgradeId.VenomTile, UpgradePool.Grid, "Venom Sprout",
            "Poisons the front enemy for 10% of its max HP when placed, no scoring required.", false, UpgradeRarity.Uncommon);

        /// <summary>
        /// Playtest request, cited textually: "Permanent debuff or buff on tile comme au scrabble. Grid
        /// upgrade rare" — unlike every other Grid-pool entry, this doesn't enchant a deck PIECE (no
        /// PieceTraitKind maps to it, see UpgradeSystem.TraitKindFor), it instantly marks one random board
        /// POSITION on purchase (see RunManager.ApplyPermanentBonusSquare), so it's special-cased in
        /// RunManager.ApplyPurchasedUpgrade to apply immediately instead of routing through the ordinary
        /// deck-tile-choice flow every other Grid-pool upgrade uses.
        /// </summary>
        public static readonly UpgradeDefinition PermanentBonusSquare = new UpgradeDefinition(
            UpgradeId.PermanentBonusSquare, UpgradePool.Grid, "Permanent Bonus Square",
            "Marks one random empty board square to always double score through it, for the rest of the run.", false, UpgradeRarity.Rare);

        /// <summary>
        /// No sub-choice — like Joker/Random Modifier, it applies
        /// immediately on purchase: a carousel spins through all 8 shapes
        /// (grayed out, color irrelevant) and lands on one at random (see
        /// RunManager.GrantShapeMastery/LastShapeMasteryGranted,
        /// ShapeCarouselView), leveling up every piece of that exact shape
        /// in the deck. Level N gives +(N-1) flat points per placement of
        /// that shape (level 2 = +1, level 3 = +2, ...) — buying it again,
        /// whether it lands on the same shape or a different one, keeps
        /// leveling up, no cap.
        /// </summary>
        public static readonly UpgradeDefinition PieceMastery = new UpgradeDefinition(
            UpgradeId.PieceMastery, UpgradePool.Mastery, "Seed Mastery",
            "Levels up one random seed shape — its sprouts score +1 pts per level.", false, UpgradeRarity.Common);

        /// <summary>Piece Mastery's exact sibling, keyed by PieceColor instead of ShapeId (see RunManager.GrantColorMastery/LastColorMasteryGranted, ColorCarouselView). Same no-sub-choice/no-cap/level-(N-1)-flat-points mechanics.</summary>
        public static readonly UpgradeDefinition ColorMastery = new UpgradeDefinition(
            UpgradeId.ColorMastery, UpgradePool.Mastery, "Color Mastery",
            "Levels up one random seed color — its sprouts score +1 pts per level.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition PipTile = new UpgradeDefinition(
            UpgradeId.PipTile, UpgradePool.Grid, "Pip Sprout",
            "+1 pt when planted.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition LuckyTile = new UpgradeDefinition(
            UpgradeId.LuckyTile, UpgradePool.Grid, "Lucky Sprout",
            "+0 to +20 pts at random when planted.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition CompanionTile = new UpgradeDefinition(
            UpgradeId.CompanionTile, UpgradePool.Grid, "Companion Sprout",
            "+8 pts per touching tile of another chosen color.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ColonyTile = new UpgradeDefinition(
            UpgradeId.ColonyTile, UpgradePool.Grid, "Colony Sprout",
            "+20 pts when its group has more than 10 cells.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition BorderTile = new UpgradeDefinition(
            UpgradeId.BorderTile, UpgradePool.Grid, "Border Sprout",
            "+8 pts when planted on the garden's edge.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition HeartTile = new UpgradeDefinition(
            UpgradeId.HeartTile, UpgradePool.Grid, "Heart Sprout",
            "+12 pts when planted in the middle of the garden.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition CornerTile = new UpgradeDefinition(
            UpgradeId.CornerTile, UpgradePool.Grid, "Corner Sprout",
            "+30 pts when planted on a corner.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition HermitTile = new UpgradeDefinition(
            UpgradeId.HermitTile, UpgradePool.Grid, "Hermit Sprout",
            "+12 pts when no other tile touches it.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition CuddlyTile = new UpgradeDefinition(
            UpgradeId.CuddlyTile, UpgradePool.Grid, "Cuddly Sprout",
            "+3 pts per planted tile around it (diagonals and its own seed included).", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DawnTile = new UpgradeDefinition(
            UpgradeId.DawnTile, UpgradePool.Grid, "Dawn Sprout",
            "+12 pts if planted among the first 3 seeds of the round.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DuskTile = new UpgradeDefinition(
            UpgradeId.DuskTile, UpgradePool.Grid, "Dusk Sprout",
            "+20 pts if planted as the last seed of your hand.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MiserTile = new UpgradeDefinition(
            UpgradeId.MiserTile, UpgradePool.Grid, "Miser Sprout",
            "+1 pt per Pollen held (up to +15).", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition StormyTile = new UpgradeDefinition(
            UpgradeId.StormyTile, UpgradePool.Grid, "Stormy Sprout",
            "+12 pts when the weather isn't Clear.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ElderTile = new UpgradeDefinition(
            UpgradeId.ElderTile, UpgradePool.Grid, "Elder Sprout",
            "+2 pts per round already cleared this run.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition HarvesterTile = new UpgradeDefinition(
            UpgradeId.HarvesterTile, UpgradePool.Grid, "Harvester Sprout",
            "+10 pts per line harvested by its seed.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition BreezeTile = new UpgradeDefinition(
            UpgradeId.BreezeTile, UpgradePool.Grid, "Breeze Sprout",
            "+3 pts per Shuffle you still have.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition HarmonyTile = new UpgradeDefinition(
            UpgradeId.HarmonyTile, UpgradePool.Grid, "Harmony Sprout",
            "+10 pts when every touching tile shares its color.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition PrismTile = new UpgradeDefinition(
            UpgradeId.PrismTile, UpgradePool.Grid, "Prism Sprout",
            "+20 pts when it touches 3 or more different colors.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition GardenerTile = new UpgradeDefinition(
            UpgradeId.GardenerTile, UpgradePool.Grid, "Gardener Sprout",
            "+3 pts per fertilizer held.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ClusterTile = new UpgradeDefinition(
            UpgradeId.ClusterTile, UpgradePool.Grid, "Cluster Sprout",
            "+6 pts per other upgraded tile in its group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition CrossfireTile = new UpgradeDefinition(
            UpgradeId.CrossfireTile, UpgradePool.Grid, "Crossfire Sprout",
            "+1 pt per planted tile in its row and column.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition EvenTile = new UpgradeDefinition(
            UpgradeId.EvenTile, UpgradePool.Grid, "Even Sprout",
            "+12 pts when its group has an even number of cells.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RainbowRowTile = new UpgradeDefinition(
            UpgradeId.RainbowRowTile, UpgradePool.Grid, "Rainbow Row Sprout",
            "+15 pts when its row holds every color of the run.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition SunseekerTile = new UpgradeDefinition(
            UpgradeId.SunseekerTile, UpgradePool.Grid, "Sunseeker Sprout",
            "+10 pts on Sunshine and Golden Hour days (and their indoor cousins).", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition HunterTile = new UpgradeDefinition(
            UpgradeId.HunterTile, UpgradePool.Grid, "Hunter Sprout",
            "+3 pts per enemy still alive.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition ExecutionerTile = new UpgradeDefinition(
            UpgradeId.ExecutionerTile, UpgradePool.Grid, "Executioner Sprout",
            "+30 pts when the front enemy is down to a quarter of its HP or less.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition SpilloverTile = new UpgradeDefinition(
            UpgradeId.SpilloverTile, UpgradePool.Grid, "Spillover Sprout",
            "+25 pts when its tile is harvested by a line.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition HoneycombTile = new UpgradeDefinition(
            UpgradeId.HoneycombTile, UpgradePool.Grid, "Honeycomb Sprout",
            "+1 Pollen every time it scores.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DewdropTile = new UpgradeDefinition(
            UpgradeId.DewdropTile, UpgradePool.Grid, "Dewdrop Sprout",
            "+2 Pollen per line harvested by its seed.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition KeystoneTile = new UpgradeDefinition(
            UpgradeId.KeystoneTile, UpgradePool.Grid, "Keystone Sprout",
            "+5 Mult when it sits on a corner or in the middle.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition GrowthTile = new UpgradeDefinition(
            UpgradeId.GrowthTile, UpgradePool.Grid, "Growth Sprout",
            "+1 pt, then +1 more every time it scores this round.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition GamblerTile = new UpgradeDefinition(
            UpgradeId.GamblerTile, UpgradePool.Grid, "Gambler Sprout",
            "50%: +40 pts. 50%: -10 pts.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition WardenTile = new UpgradeDefinition(
            UpgradeId.WardenTile, UpgradePool.Grid, "Warden Sprout",
            "Frees the 4 plots around it from enemy effects and makes them immune for the round.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition AegisTile = new UpgradeDefinition(
            UpgradeId.AegisTile, UpgradePool.Grid, "Aegis Sprout",
            "Frees its own plot from enemy effects and makes it immune for the round.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition SowerTile = new UpgradeDefinition(
            UpgradeId.SowerTile, UpgradePool.Grid, "Sower Sprout",
            "Plants a free tile of its color on a neighboring empty plot.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition[] All =
        {
            ReplacePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier, RandomPiece, ModifierUpgrade,
            GoldenCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile, VenomTile, PermanentBonusSquare, PieceMastery, ColorMastery,
            PipTile, LuckyTile, CompanionTile, ColonyTile, BorderTile, HeartTile, CornerTile, HermitTile, CuddlyTile, DawnTile, DuskTile, MiserTile, StormyTile, ElderTile, HarvesterTile, BreezeTile, HarmonyTile, PrismTile, GardenerTile,
            ClusterTile, CrossfireTile, EvenTile, RainbowRowTile, SunseekerTile, HunterTile, ExecutionerTile, SpilloverTile, HoneycombTile, DewdropTile, KeystoneTile, GrowthTile, GamblerTile, WardenTile, AegisTile, SowerTile
        };

        public static readonly UpgradeDefinition[] BankPool =
        {
            ReplacePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomPiece
        };

        /// <summary>Its own pool, with its own "Mastery Upgrade" label (see UpgradeVisualDefaults.GetPoolLabel) and independent shop-roll odds (see RunManager.RollUpgradeSlot/EconomyConstants.MasteryUpgradePoolChancePercent).</summary>
        public static readonly UpgradeDefinition[] MasteryPool =
        {
            PieceMastery, ColorMastery
        };

        /// <summary>Same pattern as MasteryPool: its own "Modifier Upgrade" label and its own shop-roll odds, see EconomyConstants.ModifierUpgradePoolChancePercent.</summary>
        public static readonly UpgradeDefinition[] ModifierPool =
        {
            RandomModifier, ModifierUpgrade
        };

        public static readonly UpgradeDefinition[] GridPool =
        {
            GoldenCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile, VenomTile, PermanentBonusSquare,
            PipTile, LuckyTile, CompanionTile, ColonyTile, BorderTile, HeartTile, CornerTile, HermitTile, CuddlyTile, DawnTile, DuskTile, MiserTile, StormyTile, ElderTile, HarvesterTile, BreezeTile, HarmonyTile, PrismTile, GardenerTile,
            ClusterTile, CrossfireTile, EvenTile, RainbowRowTile, SunseekerTile, HunterTile, ExecutionerTile, SpilloverTile, HoneycombTile, DewdropTile, KeystoneTile, GrowthTile, GamblerTile, WardenTile, AegisTile, SowerTile
        };
    }
}
