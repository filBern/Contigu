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
        /// Lueur earned per DISTINCT non-Joker color within a cleared line
        /// (see GridManager.ComputeLueurGroups/LueurGroup) — "2 points par
        /// couleur", on explicit request. Was briefly "2 points par groupe"
        /// (per contiguous same-color RUN instead, so a color split across
        /// several non-adjacent runs in the same line paid more than once);
        /// changed back to a flat rate per color regardless of how many runs
        /// it's split into. A fully monochrome line is 1 color (2 Lueur); a
        /// line touching all 4 base colors is 4 (8 Lueur) — still rewarding
        /// mixing over monochrome, just linearly per color instead of the
        /// original escalating lookup table. Jokers never count.
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

        // ---- Lueur-earning modifiers (see ModifierId's eighth batch) —
        // each adapted from an existing SCORE modifier of the same shape,
        // paying Lueur instead (on explicit request). Values are roughly
        // scaled against LueurPerColorGroup (2) — a per-line one is worth
        // more than a single color group since it needs a specific
        // pattern, not just any clear.

        /// <summary>Rainbow Glow (adapted from Arc-en-ciel): Lueur PER cleared row/column containing all 4 base colors, stacking.</summary>
        public const int ArcEnCielLueurPerLine = 6;

        /// <summary>Glowing Alternation (adapted from Alternance): Lueur PER cleared row/column whose colors strictly alternate between exactly 2 colors, stacking.</summary>
        public const int AlternanceLueurPerLine = 4;

        /// <summary>Radiant Line (adapted from Monochrome Ligne): Lueur PER cleared row/column that is entirely a single color, stacking — the odd one out among these, rewarding the opposite of what Lueur normally favors (a monochrome line is worth the least base Lueur), as a deliberate counter-play option.</summary>
        public const int MonochromeLigneLueurPerLine = 4;

        /// <summary>Glowing Collector (adapted from Collectionneur): Lueur per distinct color among this placement's cleared cells.</summary>
        public const int CollectionneurLueurPerColor = 2;

        /// <summary>Golden Repetition (adapted from Repetition): flat Lueur when this piece is the same shape as the immediately previous placement this round — unlike its score counterpart, not progressive.</summary>
        public const int RepetitionLueurBonus = 5;

        /// <summary>Risky Mult: 1-in-this-many chance, rolled once per round at round end (see RunManager.EvaluateRoundEnd), to lose ONE held copy of the modifier — on explicit request ("chance sur 5 de perdre le modifier a la fin de la round").</summary>
        public const int MultCinqRisqueLossChanceDenominator = 5;
    }
}
