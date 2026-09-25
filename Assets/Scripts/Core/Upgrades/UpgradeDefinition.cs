namespace Contigu.Core
{
    /// <summary>
    /// Static description of one upgrade. <see cref="RequiresSubChoice"/> flags
    /// upgrades that need the player to pick a piece type (and/or target color)
    /// before they can be applied (Retirer/Dupliquer/Recolorer, spec 5.3).
    /// <see cref="Rarity"/> (spec extension, explicit request) weights how
    /// often it shows up in a draft — see UpgradeSystem.PickWeighted — and is
    /// shown to the player alongside <see cref="Pool"/> ("type") on the draft
    /// card / tile-badge tooltip.
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
        // Rarity dropped Common -> Rare (on explicit report: "L'upgrade
        // 'remove a piece' est beaucoup trop fréquente et surtout chiante en
        // début de partie") — a 4x cut in its draft weight (8 -> 2, see
        // UpgradeRarityUtility.GetDraftWeight), same tier as the most
        // situational Grid-pool upgrades, since permanently shrinking the
        // deck is the one Bank-pool pick that can backfire rather than just
        // being weaker than another option.
        public static readonly UpgradeDefinition RemovePiece = new UpgradeDefinition(
            UpgradeId.RemovePiece, UpgradePool.Bank, "Remove a piece",
            "Choose a piece type from the deck; one copy is permanently removed.", true, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Duplicate a piece",
            "Choose a piece type from the deck; one extra copy is added.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Joker piece",
            "Adds a joker piece (random shape) to the deck.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolor a piece",
            "Choose a piece type and a target color; one copy changes color.", true, UpgradeRarity.Uncommon);

        /// <summary>
        /// A gamble (spec extension, explicit request: "j'aimerais qu'on
        /// rajoute random modifier dans la liste de possibilité d'apparaitre.
        /// 3 lueurs de base pareil, c'est un gamble") — Bank pool so it
        /// automatically shares the same base price as every other Bank
        /// upgrade (EconomyConstants.BankUpgradeShopBasePrice) with no
        /// special-casing needed, and no sub-choice (applies immediately on
        /// purchase, like Joker — see RunManager.BuyUpgradeSlot/
        /// GrantRandomModifier) since there's nothing for the player to pick:
        /// the whole point is not knowing which modifier they'll get.
        /// </summary>
        public static readonly UpgradeDefinition RandomModifier = new UpgradeDefinition(
            UpgradeId.RandomModifier, UpgradePool.Bank, "Random Modifier",
            "Grants one random modifier you don't already have. A gamble — you don't get to pick which.", false, UpgradeRarity.Uncommon);

        // Descriptions below all follow the same short "A tile that ..."
        // pattern, describing the trait itself rather than how many pieces
        // get it — that count is a shop mechanic (see
        // EconomyConstants.ShopTileChoiceCount, picked by the player in
        // TileChoiceView), not a property of the upgrade, so it doesn't
        // belong baked into the text (on explicit request — it used to say
        // "3 pieces get a tile that ...").
        public static readonly UpgradeDefinition GoldenCells = new UpgradeDefinition(
            UpgradeId.GoldenCells, UpgradePool.Grid, "Golden Cells",
            "A tile that scores +18 flat when placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition TintedCells = new UpgradeDefinition(
            UpgradeId.TintedCells, UpgradePool.Grid, "Tinted Cells",
            "A tile that always doubles the placement's group and golden score (not the line-clear bonus) — tinted to match the piece's own color.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "A tile that doubles the placement's ENTIRE score, line-clear bonus included.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition BlastTile = new UpgradeDefinition(
            UpgradeId.BlastTile, UpgradePool.Grid, "Blast Tile",
            "A tile that also makes its 4 neighbors score golden (+18 each).", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MultiplierBeacon = new UpgradeDefinition(
            UpgradeId.MultiplierBeacon, UpgradePool.Grid, "Multiplier Beacon",
            "A tile that turns every filled tile in its row/column into a multiplier for that placement.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MirrorTile = new UpgradeDefinition(
            UpgradeId.MirrorTile, UpgradePool.Grid, "Mirror Tile",
            "A tile that duplicates its group-bonus share onto one random other tile in the group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition Seeder = new UpgradeDefinition(
            UpgradeId.Seeder, UpgradePool.Grid, "Seeder",
            "A tile that stays golden on the grid for the rest of the round instead of scoring once.", false, UpgradeRarity.Rare);

        // ---- Second batch (7 more tile upgrades, on explicit request) ----

        public static readonly UpgradeDefinition CatalystTile = new UpgradeDefinition(
            UpgradeId.CatalystTile, UpgradePool.Grid, "Catalyst Tile",
            "A tile that scores extra for every pre-existing cell merged into its group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition TwinTile = new UpgradeDefinition(
            UpgradeId.TwinTile, UpgradePool.Grid, "Twin Tile",
            "A tile that duplicates its group-bonus share onto EVERY other tile in the group.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DetonatorTile = new UpgradeDefinition(
            UpgradeId.DetonatorTile, UpgradePool.Grid, "Detonator Tile",
            "A tile that doubles the line-clear bonus if it clears a row or column.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition ChameleonTile = new UpgradeDefinition(
            UpgradeId.ChameleonTile, UpgradePool.Grid, "Chameleon Tile",
            "A tile that recolors the WHOLE piece to match a filled neighbor when placed.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition SparkTile = new UpgradeDefinition(
            UpgradeId.SparkTile, UpgradePool.Grid, "Spark Tile",
            "A tile that scores more the longer since the last line clear this round.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition VoidTile = new UpgradeDefinition(
            UpgradeId.VoidTile, UpgradePool.Grid, "Void Tile",
            "A tile that also clears one random filled tile elsewhere on the grid.", false, UpgradeRarity.Rare);

        // ---- Third batch (2 more tile upgrades, on explicit request) ----

        public static readonly UpgradeDefinition BastionTile = new UpgradeDefinition(
            UpgradeId.BastionTile, UpgradePool.Grid, "Bastion Tile",
            "A tile that locks in place for the rest of the round instead of being cleared, but keeps scoring the line-clear bonus every time its row/column completes.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition KamikazeTile = new UpgradeDefinition(
            UpgradeId.KamikazeTile, UpgradePool.Grid, "Kamikaze Tile",
            "A tile that also destroys its 8 surrounding tiles when placed (+6 per tile destroyed).", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition[] All =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier,
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile
        };

        public static readonly UpgradeDefinition[] BankPool =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier
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
