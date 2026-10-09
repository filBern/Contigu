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
            UpgradeId.ReplacePiece, UpgradePool.Bank, "Replace a piece",
            "Choose 1 piece to replace with a duplicate of another.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Duplicate a piece",
            "Choose 1 piece to duplicate in your deck.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Joker piece",
            "Adds a joker piece (random shape) to the deck.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolor a piece",
            "Choose a piece type and color; recolor one copy.", true, UpgradeRarity.Uncommon);

        /// <summary>
        /// A gamble with no sub-choice (applies immediately on purchase, like
        /// Joker — see RunManager.BuyUpgradeSlot/GrantRandomModifier): grants
        /// a random modifier with no way to know which in advance.
        /// </summary>
        public static readonly UpgradeDefinition RandomModifier = new UpgradeDefinition(
            UpgradeId.RandomModifier, UpgradePool.Modifier, "Random Modifier",
            "Grants one random modifier you don't already have.", false, UpgradeRarity.Uncommon);

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
            UpgradeId.RandomPiece, UpgradePool.Bank, "Random Piece",
            "Choose 1 piece to add to your deck.", true, UpgradeRarity.Common);

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
            UpgradeId.ModifierUpgrade, UpgradePool.Modifier, "Modifier Upgrade",
            "Choose one of your active modifiers to level up — its effect gets stronger.", true, UpgradeRarity.Uncommon);

        // Descriptions below describe the trait itself rather than how many
        // pieces get it — that count is a shop mechanic (see
        // EconomyConstants.ShopTileChoiceCount, picked by the player in
        // TileChoiceView), not a property of the upgrade.
        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Golden Cells",
            "Scores +18 flat when placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Tinted Cells",
            "Doubles group and golden score (not line-clear), tinted to the piece's color.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "Doubles the whole placement's score, line-clear included.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition BlastTile = new UpgradeDefinition(
            UpgradeId.BlastTile, UpgradePool.Grid, "Blast Tile",
            "Makes itself and its 4 neighbors score golden if same color.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MultiplierBeacon = new UpgradeDefinition(
            UpgradeId.MultiplierBeacon, UpgradePool.Grid, "Multiplier Beacon",
            "Turns its whole row/column into multipliers for that placement.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MirrorTile = new UpgradeDefinition(
            UpgradeId.MirrorTile, UpgradePool.Grid, "Mirror Tile",
            "Copies its group bonus onto one random other tile in the group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition Seeder = new UpgradeDefinition(
            UpgradeId.Seeder, UpgradePool.Grid, "Seeder",
            "Stays golden for the rest of the round instead of scoring once.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition CatalystTile = new UpgradeDefinition(
            UpgradeId.CatalystTile, UpgradePool.Grid, "Catalyst Tile",
            "Scores extra for every pre-existing cell merged into its group.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition TwinTile = new UpgradeDefinition(
            UpgradeId.TwinTile, UpgradePool.Grid, "Twin Tile",
            "Copies its group bonus onto EVERY other tile in the group.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DetonatorTile = new UpgradeDefinition(
            UpgradeId.DetonatorTile, UpgradePool.Grid, "Detonator Tile",
            "Doubles the line-clear bonus if it clears a row or column.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ChameleonTile = new UpgradeDefinition(
            UpgradeId.ChameleonTile, UpgradePool.Grid, "Chameleon Tile",
            "Recolors the WHOLE piece to match a filled neighbor when placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition SparkTile = new UpgradeDefinition(
            UpgradeId.SparkTile, UpgradePool.Grid, "Spark Tile",
            "Scores more the longer since the last line clear this round.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition VoidTile = new UpgradeDefinition(
            UpgradeId.VoidTile, UpgradePool.Grid, "Void Tile",
            "Clears one random filled tile elsewhere on the grid (+10 for the tile broken).", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition BastionTile = new UpgradeDefinition(
            UpgradeId.BastionTile, UpgradePool.Grid, "Bastion Tile",
            "Won't be removed when line a line is cleared.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition KamikazeTile = new UpgradeDefinition(
            UpgradeId.KamikazeTile, UpgradePool.Grid, "Kamikaze Tile",
            "Destroys itself and its 8 surrounding tiles (+6 per tile destroyed).", false, UpgradeRarity.Rare);

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
            UpgradeId.PieceMastery, UpgradePool.Mastery, "Piece Mastery",
            "Levels up one random piece shape — its tiles score +1 pts per level.", false, UpgradeRarity.Common);

        /// <summary>Piece Mastery's exact sibling, keyed by PieceColor instead of ShapeId (see RunManager.GrantColorMastery/LastColorMasteryGranted, ColorCarouselView). Same no-sub-choice/no-cap/level-(N-1)-flat-points mechanics.</summary>
        public static readonly UpgradeDefinition ColorMastery = new UpgradeDefinition(
            UpgradeId.ColorMastery, UpgradePool.Mastery, "Color Mastery",
            "Levels up one random piece color — its tiles score +1 pts per level.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition[] All =
        {
            ReplacePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier, RandomPiece, ModifierUpgrade,
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile, PieceMastery, ColorMastery
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
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile
        };
    }
}
