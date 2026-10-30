namespace Contigu.Core
{
    /// <summary>
    /// Tunable values for the "Lueur" currency and the between-round shop.
    /// Kept as its own surface (parallel to ScoringConstants, which stays
    /// focused on in-round placement scoring) since this is a genuinely
    /// separate axis: Lueur rewards mixing colors in a cleared line, while
    /// the score itself mostly rewards the opposite (big monochrome groups
    /// via Chaîne/Éclat/the group bonus).
    /// </summary>
    public static class EconomyConstants
    {
        /// <summary>
        /// Lueur earned per distinct non-Joker color within a cleared line
        /// (see GridManager.ComputeLueurGroups/LueurGroup) — a flat rate per
        /// color regardless of how many contiguous runs it's split into. A
        /// fully monochrome line is 1 color (2 Lueur); a line touching all 4
        /// base colors is 4 (8 Lueur). Jokers never count.
        /// </summary>
        public const int LueurPerColorGroup = 2;

        /// <summary>
        /// How many "Blister" slots the shop offers per visit — a modifier
        /// or upgrade drawn from one shared bag, its exact identity always
        /// shown up front (never a mystery) — see RunManager.RollBlisterSlot.
        /// Never touched by RerollShop (see ShopUpgradeSlotCount below).
        /// </summary>
        public const int ShopBlisterSlotCount = 3;

        /// <summary>
        /// Extra multiplier applied to each upgrade's own rarity weight
        /// (never the modifier weight) when rolling a Blister slot — with
        /// far more modifiers than upgrades in the catalog, a flat weighted
        /// bag would make upgrades too rare, so this biases toward them.
        /// Never affects Casino's own separate weighting (see
        /// RunManager.RollUpgradeSlot/UpgradeSystem.RollFromPool).
        /// </summary>
        public const int BlisterUpgradeWeightMultiplier = 3;

        /// <summary>
        /// Weight of every fertilizer in a Blister roll (an upgrade weighs its rarity weight times
        /// <see cref="BlisterUpgradeWeightMultiplier"/>: 24 common, 12 uncommon, 6 rare). Raised from 8 to 16 so a Blister slot is a fertilizer
        /// about two times out of three instead of one in two.
        /// </summary>
        public const int BlisterModifierWeight = 16;

        /// <summary>
        /// How many "Casino" slots the shop offers per visit — only the
        /// UpgradePool is shown, the specific UpgradeDefinition underneath
        /// stays hidden until purchase. The only section RerollShop still
        /// touches.
        /// </summary>
        public const int ShopUpgradeSlotCount = 4;

        // ---- Workshop prices and sizes (see RunManager.RollWorkshopSlots).
        public const int SeedPackPrice = 4;
        public const int SproutPackPrice = 5;
        public const int FertilizerPackPrice = 6;
        public const int MasteryPackPrice = 4;
        public const int CompostBasePrice = 3;
        public const int CompostPriceIncreasePerUse = 2;

        /// <summary>A Workshop item is never sold out: buying it again in the same visit costs this much more each time (a fraction of its price, per earlier purchase).</summary>
        public const float WorkshopRepeatPriceIncrease = 0.5f;
        public const int ForgePrice = 4;
        public const int PackChoiceCount = 3;
        /// <summary>How many tile upgrades a Sprout Pack reveals (pick 1).</summary>
        public const int SproutPackChoiceCount = 5;
        /// <summary>How many tile upgrades the Forge offers when bought: the one shown on its shelf plus this many minus one more (pick 1).</summary>
        public const int ForgeChoiceCount = 3;
        public const int ForgeSeedCandidateCount = 8;

        /// <summary>Chance that a Casino upgrade slot rolls from each of the 4 pools — must sum to 100 (see RunManager.RollUpgradeSlot).</summary>
        public const int ModifierUpgradePoolChancePercent = 10;

        /// <summary>See <see cref="ModifierUpgradePoolChancePercent"/>.</summary>
        public const int MasteryUpgradePoolChancePercent = 25;

        /// <summary>See <see cref="ModifierUpgradePoolChancePercent"/>.</summary>
        public const int BankUpgradePoolChancePercent = 25;

        /// <summary>See <see cref="ModifierUpgradePoolChancePercent"/>.</summary>
        public const int GridUpgradePoolChancePercent = 40;

        /// <summary>"Piece Mastery"/"Color Mastery" (see RunManager.GrantShapeMastery/GrantColorMastery): each purchase grants a random amount of levels from 1 up to this many (inclusive).</summary>
        public const int MasteryUpgradeMaxLevelGain = 3;

        /// <summary>Hard cap on how many modifiers the player can hold at once.</summary>
        public const int MaxActiveModifiers = 4;

        public const int BankUpgradeShopBasePrice = 3;
        public const int GridUpgradeShopBasePrice = 3;

        /// <summary>Flat Lueur cost to refresh every still-unsold slot in the current shop visit (see RunManager.RerollShop) — escalates with every other purchase this visit exactly like a slot's own price does.</summary>
        public const int ShopRerollBasePrice = 5;

        /// <summary>Every purchase (a slot or a reroll) made in the same shop visit raises the price of everything else still on offer by this fraction, so a big Lueur balance can't just clear the whole shop at face value.</summary>
        public const float ShopPriceEscalationPerPurchase = 0.5f;

        /// <summary>How many candidate deck tokens the player gets to choose from once a Grid-pool shop upgrade is revealed.</summary>
        public const int ShopTileCandidateCount = 5;

        /// <summary>How many of the ShopTileCandidateCount candidates the player actually picks — same count every Grid upgrade used to tag randomly, just player-chosen now instead of random.</summary>
        public const int ShopTileChoiceCount = 3;

        /// <summary>
        /// Per-candidate percent chance, rolled via IRandomProvider.Next(100)
        /// &lt; this (IRandomProvider has no float roll — see
        /// UpgradeSystem.GetCandidatePiecesFor), that one of the "Random
        /// Piece" upgrade's ShopTileCandidateCount (5) freshly-rolled piece
        /// candidates already carries a Grid-pool tile trait before the
        /// player even picks one. Independent per candidate.
        /// </summary>
        public const int RandomPieceTraitChancePercent = 100;

        /// <summary>Chance, for a candidate piece of a Seed Pack / Random Piece that already carries an upgrade, to carry one more on another tile (rolled again for a third, and so on).</summary>
        public const int ExtraUpgradeChancePercent = 12;

        // Lueur-earning modifiers, each adapted from an existing score
        // modifier of the same shape, paying Lueur instead. Values are
        // roughly scaled against LueurPerColorGroup (2) — a per-line one is
        // worth more than a single color group since it needs a specific
        // pattern, not just any clear.

        /// <summary>Rainbow Glow (adapted from Arc-en-ciel): Lueur PER cleared row/column containing all 4 base colors, stacking.</summary>
        public const int ArcEnCielLueurPerLine = 6;

        // ---- the 40 fertilizers added in one batch: the Pollen ones ----
        public const int PollinatorLueur = 2;
        public const int HoneyCellsPerLueur = 3;
        public const int NectarLueurPerSeed = 1;
        public const int TitheLueurPerLine = 2;
        public const int RainBarrelLueurPerShuffle = 1;
        public const int GranaryLueurPerFertilizer = 1;

        /// <summary>Glowing Alternation (adapted from Alternance): Lueur PER cleared row/column whose colors strictly alternate between exactly 2 colors, stacking.</summary>
        public const int AlternanceLueurPerLine = 4;

        /// <summary>Radiant Line (adapted from Monochrome Ligne): Lueur PER cleared row/column that is entirely a single color, stacking — the odd one out among these, rewarding the opposite of what Lueur normally favors (a monochrome line is worth the least base Lueur), as a deliberate counter-play option.</summary>
        public const int MonochromeLigneLueurPerLine = 4;

        /// <summary>Glowing Collector (adapted from Collectionneur): Lueur per distinct color among this placement's cleared cells.</summary>
        public const int CollectionneurLueurPerColor = 2;

        /// <summary>Golden Repetition (adapted from Repetition): flat Lueur when this piece is the same shape as the immediately previous placement this round — unlike its score counterpart, not progressive.</summary>
        public const int RepetitionLueurBonus = 5;

        /// <summary>Risky Mult: 1-in-this-many chance, rolled once per round at round end (see RunManager.EvaluateRoundEnd), to lose one held copy of the modifier.</summary>
        public const int MultCinqRisqueLossChanceDenominator = 5;
    }
}
