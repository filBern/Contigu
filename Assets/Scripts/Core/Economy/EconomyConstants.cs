namespace Contigu.Core
{
    /// <summary>
    /// Tunable values for the "Lueur" currency and the between-round shop
    /// (spec extension, explicit request — a Balatro-style economy, but tied
    /// to Contigu's own color-diversity mechanic rather than a generic
    /// per-point cash-out). Kept as its own surface (parallel to
    /// ScoringConstants, which stays focused on in-round placement scoring)
    /// since this is a genuinely separate axis: Lueur rewards MIXING colors
    /// in a cleared line, while the score itself mostly rewards the opposite
    /// (big monochrome groups via Chaîne/Éclat/the group bonus).
    /// </summary>
    public static class EconomyConstants
    {
        /// <summary>
        /// Lueur earned per contiguous same-color group within a cleared
        /// line (see GridManager.ComputeLueurGroups/LueurGroup) — on
        /// explicit request ("chaque groupe d'une couleur sur la ligne = 2
        /// points"), replacing the old formula keyed on distinct color
        /// count. A fully monochrome line is one group (2 Lueur); a line
        /// that alternates color every cell is 8 groups (16 Lueur) — still
        /// rewarding mixing over monochrome, just linearly per group instead
        /// of a lookup table. Jokers never form an earning group.
        /// </summary>
        public const int LueurPerColorGroup = 2;

        /// <summary>How many modifier slots the shop offers per visit.</summary>
        public const int ShopModifierSlotCount = 3;

        /// <summary>How many upgrade slots (pool only shown, specific upgrade hidden) the shop offers per visit.</summary>
        public const int ShopUpgradeSlotCount = 2;

        /// <summary>Hard cap on how many modifiers the player can hold at once — the shop lets Lueur buy modifiers far more freely than the old one-per-round draft ever could, so unlike that system this one needs a ceiling.</summary>
        public const int MaxActiveModifiers = 10;

        public const int ModifierShopBasePrice = 8;
        public const int BankUpgradeShopBasePrice = 3;
        public const int GridUpgradeShopBasePrice = 3;

        /// <summary>Flat Lueur cost to refresh every still-unsold slot in the current shop visit (see RunManager.RerollShop) — escalates with every other purchase this visit exactly like a slot's own price does.</summary>
        public const int ShopRerollBasePrice = 5;

        /// <summary>Every purchase (a slot OR a reroll) made in the SAME shop visit raises the price of everything else still on offer by this fraction — on explicit request ("prix qui montent à chaque achat"), so a big Lueur balance can't just clear the whole shop at face value.</summary>
        public const float ShopPriceEscalationPerPurchase = 0.5f;

        /// <summary>How many candidate deck tokens the player gets to choose from once a Grid-pool shop upgrade is revealed (spec: "un choix de 5 tiles").</summary>
        public const int ShopTileCandidateCount = 5;

        /// <summary>How many of the ShopTileCandidateCount candidates the player actually picks — same count every Grid upgrade used to tag randomly, just player-chosen now instead of random.</summary>
        public const int ShopTileChoiceCount = 3;
    }
}
