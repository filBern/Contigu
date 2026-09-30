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
            "Choose a piece type; remove one copy from the deck.", true, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition DuplicatePiece = new UpgradeDefinition(
            UpgradeId.DuplicatePiece, UpgradePool.Bank, "Duplicate a piece",
            "Choose a piece type; add one extra copy to the deck.", true, UpgradeRarity.Common);

        public static readonly UpgradeDefinition JokerPiece = new UpgradeDefinition(
            UpgradeId.JokerPiece, UpgradePool.Bank, "Joker piece",
            "Adds a joker piece (random shape) to the deck.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition RecolorPiece = new UpgradeDefinition(
            UpgradeId.RecolorPiece, UpgradePool.Bank, "Recolor a piece",
            "Choose a piece type and color; recolor one copy.", true, UpgradeRarity.Uncommon);

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
        /// Rarity bumped Uncommon -> Common (on explicit report: "Après plus
        /// d'une dizaine de reroll je n'ai jamais eu de random modifier" —
        /// the math checked out as unlucky-but-plausible rather than a bug,
        /// ~15% chance of a 0-sighting streak across 12 rerolls at Uncommon,
        /// given the shop's own 50/50 Bank/Grid pool split on top of the
        /// weighted pick within Bank's now-5-entry pool — but the player
        /// confirmed they wanted it more frequent regardless). Now matches
        /// Duplicate/Joker's weight, roughly doubling its odds per upgrade
        /// slot (~7.7% -> ~13.3%) and cutting the 12-reroll never-seen-it
        /// chance from ~15% to ~4%.
        /// </summary>
        public static readonly UpgradeDefinition RandomModifier = new UpgradeDefinition(
            UpgradeId.RandomModifier, UpgradePool.Bank, "Random Modifier",
            "Grants one random modifier you don't already have.", false, UpgradeRarity.Common);

        // ---- Fourth batch (Bank pool, on explicit request) ----

        /// <summary>
        /// Spec extension, explicit request: "j'aimerais rajouter un type
        /// d'upgrade dans le shop: random piece. Propose 5 choix de pièces
        /// et le joueur en sélectionne une. Chaque pièce a un pourcentage
        /// de chance d'être upgradé avec une tuile spéciale". Bank pool
        /// (adds to the deck, like Joker/Duplicate) but WITH a sub-choice —
        /// unlike every other Bank sub-choice (Retirer/Dupliquer/Recolorer),
        /// which picks a TYPE already in the deck (see UpgradeSystem.
        /// GetCandidateTypesFor), this one's candidates are freshly rolled
        /// pieces that don't exist in the deck yet, each independently
        /// possibly pre-enchanted with a Grid-pool tile trait (see
        /// EconomyConstants.RandomPieceTraitChancePercent,
        /// UpgradeSystem.GetCandidatePiecesFor) — so it gets its own
        /// RunManager.PendingUpgradePieceCandidates/ResolveUpgradePieceChoice
        /// pair and Presentation.PieceChoiceView instead of reusing
        /// DraftView (which only knows how to show plain (Shape, Color)
        /// types, never a trait preview).
        /// </summary>
        public static readonly UpgradeDefinition RandomPiece = new UpgradeDefinition(
            UpgradeId.RandomPiece, UpgradePool.Bank, "Random Piece",
            "Choose 1 of 5 random pieces to add to your deck; each may already carry a special tile.", true, UpgradeRarity.Common);

        /// <summary>
        /// Spec extension, explicit request: "J'aimerais rajouter un type
        /// d'upgrade dans le shop: Modifier upgrade, ce serait pour
        /// upgrader un modifier que le joueur possède." After clarifying
        /// what "upgrade" should concretely do (a generic level system
        /// that amplifies whatever the chosen modifier already does — see
        /// ModifierLevelUtility — rather than swapping to a named next
        /// tier, which only a handful of modifiers even have), landed on a
        /// third Bank sub-choice shape distinct from both existing ones:
        /// unlike Retirer/Dupliquer/Recolorer (pick a TYPE from the deck)
        /// and Random Piece (pick from freshly-rolled candidates), this
        /// picks a SLOT the player already owns — RunManager.ActiveModifiers
        /// itself is the full candidate list (every owned modifier is
        /// eligible), so there's no Pending*Candidates list to populate at
        /// all, just RunManager.ResolveModifierUpgradeChoice(slotIndex)
        /// once Presentation.ModifierUpgradeChoiceView shows the picker.
        /// Refused outright (see RunManager.BuyUpgradeSlot) if the player
        /// owns no modifiers yet — same "don't sell an upgrade with
        /// nothing to apply to" precedent as Random Modifier respecting
        /// the modifier cap.
        /// </summary>
        public static readonly UpgradeDefinition ModifierUpgrade = new UpgradeDefinition(
            UpgradeId.ModifierUpgrade, UpgradePool.Bank, "Modifier Upgrade",
            "Choose one of your active modifiers to level up — its effect gets stronger.", true, UpgradeRarity.Uncommon);

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
            "A tile that doubles group and golden score (not line-clear), tinted to the piece's color.", false, UpgradeRarity.Common);

        public static readonly UpgradeDefinition MultiplierZone = new UpgradeDefinition(
            UpgradeId.MultiplierZone, UpgradePool.Grid, "Multiplier Zone",
            "A tile that doubles the whole placement's score, line-clear included.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition BlastTile = new UpgradeDefinition(
            UpgradeId.BlastTile, UpgradePool.Grid, "Blast Tile",
            "A tile that also makes its 4 neighbors score golden (+18 each).", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition MultiplierBeacon = new UpgradeDefinition(
            UpgradeId.MultiplierBeacon, UpgradePool.Grid, "Multiplier Beacon",
            "A tile that turns its whole row/column into multipliers for that placement.", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition MirrorTile = new UpgradeDefinition(
            UpgradeId.MirrorTile, UpgradePool.Grid, "Mirror Tile",
            "A tile that copies its group bonus onto one random other tile in the group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition Seeder = new UpgradeDefinition(
            UpgradeId.Seeder, UpgradePool.Grid, "Seeder",
            "A tile that stays golden for the rest of the round instead of scoring once.", false, UpgradeRarity.Rare);

        // ---- Second batch (7 more tile upgrades, on explicit request) ----

        public static readonly UpgradeDefinition CatalystTile = new UpgradeDefinition(
            UpgradeId.CatalystTile, UpgradePool.Grid, "Catalyst Tile",
            "A tile that scores extra for every pre-existing cell merged into its group.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition TwinTile = new UpgradeDefinition(
            UpgradeId.TwinTile, UpgradePool.Grid, "Twin Tile",
            "A tile that copies its group bonus onto EVERY other tile in the group.", false, UpgradeRarity.Rare);

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
            "A tile that also clears one random filled tile elsewhere on the grid (+10 for the tile broken).", false, UpgradeRarity.Rare);

        // ---- Third batch (2 more tile upgrades, on explicit request) ----

        public static readonly UpgradeDefinition BastionTile = new UpgradeDefinition(
            UpgradeId.BastionTile, UpgradePool.Grid, "Bastion Tile",
            "A tile that locks in place instead of clearing, but keeps scoring every line it completes.", false, UpgradeRarity.Uncommon);

        public static readonly UpgradeDefinition KamikazeTile = new UpgradeDefinition(
            UpgradeId.KamikazeTile, UpgradePool.Grid, "Kamikaze Tile",
            "A tile that destroys itself and its 8 surrounding tiles when placed (+6 per tile destroyed).", false, UpgradeRarity.Rare);

        public static readonly UpgradeDefinition[] All =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier, RandomPiece, ModifierUpgrade,
            GoldenCells, TintedCells, MultiplierZone,
            BlastTile, MultiplierBeacon, MirrorTile, Seeder,
            CatalystTile, TwinTile, DetonatorTile, ChameleonTile, SparkTile, VoidTile,
            BastionTile, KamikazeTile
        };

        public static readonly UpgradeDefinition[] BankPool =
        {
            RemovePiece, DuplicatePiece, JokerPiece, RecolorPiece, RandomModifier, RandomPiece, ModifierUpgrade
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
