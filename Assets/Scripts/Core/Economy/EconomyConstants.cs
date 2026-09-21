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
        /// Lueur earned per cleared line, indexed by how many DISTINCT colors
        /// (jokers excluded, same convention as every other line-diversity
        /// modifier) it contained — index 0 is unused (a line always has at
        /// least 1 color once it's complete), 1 distinct color pays the
        /// least, 4 (the max, one of each base color) pays disproportionately
        /// more than linear to make a genuine rainbow line feel like a jackpot.
        /// </summary>
        public static readonly int[] LueurByDistinctColors = { 0, 1, 3, 6, 10 };

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
        public const int ShopRerollBasePrice = 15;

        /// <summary>Every purchase (a slot OR a reroll) made in the SAME shop visit raises the price of everything else still on offer by this fraction — on explicit request ("prix qui montent à chaque achat"), so a big Lueur balance can't just clear the whole shop at face value.</summary>
        public const float ShopPriceEscalationPerPurchase = 0.5f;

        /// <summary>How many candidate deck tokens the player gets to choose from once a Grid-pool shop upgrade is revealed (spec: "un choix de 5 tiles").</summary>
        public const int ShopTileCandidateCount = 5;

        /// <summary>How many of the ShopTileCandidateCount candidates the player actually picks — same count every Grid upgrade used to tag randomly, just player-chosen now instead of random.</summary>
        public const int ShopTileChoiceCount = 3;
    }
}
