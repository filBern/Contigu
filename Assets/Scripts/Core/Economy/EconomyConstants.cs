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

        /// <summary>
        /// How many "Blister" slots the shop offers per visit — a modifier
        /// or upgrade drawn from one shared bag, its exact identity always
        /// shown up front (never a mystery), on explicit request: "au lieu
        /// d'une section modifiers et d'une section upgrade, j'aimerais
        /// qu'on ait une section 'blister'... mélanger modifiers et
        /// upgrades dans un sac et en tirer 3 au hasard". Replaces the old,
        /// separate modifier-only section — see RunManager.RollBlisterSlot.
        /// Never touched by RerollShop (see ShopUpgradeSlotCount below).
        /// </summary>
        public const int ShopBlisterSlotCount = 3;

        /// <summary>
        /// Extra multiplier applied to each upgrade's own rarity weight
        /// (never the modifier weight) when rolling a Blister slot — on
        /// explicit report, right after the flat-bag design shipped:
        /// "Il faudrait qu'on ajoute des upgrades ou qu'on bump up un peu
        /// les odds parce qu'ils n'apparaissent vraiment pas assez souvent
        /// dans la section blister". With ~70 modifiers to ~21 upgrades in
        /// the catalog, a truly flat weight-8-per-item bag put an upgrade's
        /// odds per slot around 15-16%; x3 brings that to roughly 35-40%
        /// without needing to invent new upgrade content. A single tunable
        /// knob if that still doesn't feel frequent enough. Never affects
        /// Casino's own separate weighting (see RunManager.RollUpgradeSlot/
        /// UpgradeSystem.RollFromPool), which this constant doesn't touch.
        /// </summary>
        public const int BlisterUpgradeWeightMultiplier = 3;

        /// <summary>
        /// How many "Casino" slots the shop offers per visit — only the
        /// UpgradePool is shown, the specific UpgradeDefinition underneath
        /// stays hidden until purchase (explicit request: "la section
        /// 'casino' avec ce que l'on a déjà comme section upgrade, ce sont
        /// des choses que le joueur découvre sans préalablement savoir
        /// exactement ce qu'il obtientra" — this is exactly the pre-
        /// existing upgrade-slot mystery-box mechanic, unchanged, just
        /// renamed/reorganized as its own section). The ONLY section
        /// RerollShop still touches.
        /// </summary>
        public const int ShopUpgradeSlotCount = 2;

        /// <summary>
        /// Chance that a Casino upgrade slot rolls from each of the 3 pools
        /// — must sum to 100 (see RunManager.RollUpgradeSlot). Mastery used
        /// to just be 2 more entries inside Bank's own weighted pool; once
        /// it became its own UpgradePool (explicit request: "séparer les
        /// mastery upgrades des pieces upgrades pour qu'elles soient leur
        /// propre type"), its odds needed an explicit top-level cut of what
        /// used to be Bank's whole 75% share — 40/40/20 (explicit choice,
        /// over keeping Grid's old 25% untouched or splitting all 3 evenly)
        /// keeps Grid close to its old share while giving Mastery and the
        /// rest of Bank (Piece Upgrade) equal billing.
        /// </summary>
        public const int MasteryUpgradePoolChancePercent = 40;

        /// <summary>See <see cref="MasteryUpgradePoolChancePercent"/>.</summary>
        public const int BankUpgradePoolChancePercent = 40;

        /// <summary>See <see cref="MasteryUpgradePoolChancePercent"/>.</summary>
        public const int GridUpgradePoolChancePercent = 20;

        /// <summary>Hard cap on how many modifiers the player can hold at once — the shop lets Lueur buy modifiers far more freely than the old one-per-round draft ever could, so unlike that system this one needs a ceiling. Lowered from 10 (explicit request: "réduire à 8 la quantité de modifiers"), alongside the steeper Quotas curve (see RunConfig) — on explicit report the run was ending every round with ~50% of its piece budget still unused, so both the difficulty curve and the modifier-stacking ceiling that was outpacing it needed to come down/up together.</summary>
        public const int MaxActiveModifiers = 8;

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

        /// <summary>
        /// Per-candidate percent chance (spec extension, explicit request:
        /// "Chaque pièce a un pourcentage de chance d'être upgradé avec une
        /// tuile spéciale"), rolled via IRandomProvider.Next(100) &lt; this
        /// (IRandomProvider has no float roll — see UpgradeSystem.
        /// GetCandidatePiecesFor), that one of the "Random Piece" upgrade's
        /// ShopTileCandidateCount (5) freshly-rolled piece candidates
        /// already carries a Grid-pool tile trait (golden/mult/void/etc)
        /// before the player even picks one. Independent per candidate, so
        /// on average 1-2 of the 5 will be enchanted. Chosen (over 40%) to
        /// keep it a rare bonus rather than making this upgrade obviously
        /// superior to Joker Piece (which never enchants).
        /// </summary>
        public const int RandomPieceTraitChancePercent = 25;

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
