namespace Contigu.Core
{
    /// <summary>
    /// Reference scoring values. Kept as a single tunable surface so balancing
    /// doesn't require touching game logic.
    /// </summary>
    public static class ScoringConstants
    {
        /// <summary>
        /// Step size for a placement's resulting connected same-color
        /// group's PROGRESSIVE per-cell scoring — the Nth cell scored in
        /// the group (1-indexed, in scan order) is worth N * this
        /// constant, so a full group of N cells scores the triangular
        /// number N*(N+1)/2 * GroupBonusPerCell in total, growing FASTER
        /// than group size instead of linearly with it (on explicit
        /// request: "plus tu fais un gros groupe, plus ça fait de points,
        /// la première tuile fait 1 point, la 2e fait 2 points, la 3e fait
        /// 3 points, etc." — rebalancing groups against line clears, which
        /// used to be far more lucrative). The WHOLE group is rescored in
        /// full every time a placement grows it — like replaying an
        /// extended Scrabble word — so a bigger connected blob is worth
        /// disproportionately more every time it's touched again, not
        /// just once.
        /// </summary>
        public const int GroupBonusPerCell = 1;

        /// <summary>Fixed bonus for filling a golden cell, independent of color. Computed separately and simply added to the total — never multiplied by group size or the group multiplier. Fires again every time the cell is part of a rescored group, not just when first placed.</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column — dropped from 12 (on explicit request: "on va descendre le nombre de points par tuile à 3 lorsqu'on clear une ligne", rebalancing against the new progressive group scoring above, which used to be far less lucrative than clearing lines).</summary>
        public const int LineClearBonusPerCell = 3;

        /// <summary>Multiplier contributed by EACH matching tinted cell in a placement's group — two tinted cells in the same group stack to x4, three to x8, and so on. Only reaches the group+golden bonus, never the line-clear bonus (see PlacementResult.LineClearMultiplier) — the one thing that still tells it apart from MultiplierZoneMultiplier now that Tinted's target color always matches its own piece.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group+golden bonus AND its line-clear bonus when the group contains a multiplier-zone cell — broader reach than TintedMatchMultiplier, matching its higher (Uncommon vs Common) rarity.</summary>
        public const int MultiplierZoneMultiplier = 2;

        // ---- Modifier bonuses (see ModifierCatalog) ----
        // A good number of the modifiers below were converted from a flat
        // "+X pts" bonus to a "xN" MULTIPLIER (feeding
        // PlacementResult.ModifierMultiplier instead of .ModifierBonus) on
        // explicit request — "j'aimerais qu'on utilise plus de multiplicateur
        // dans les modifiers... changer de point vers multiplicateur". Picked
        // for conversion: every ONE-SHOT conditional bonus (fires at most
        // once per placement, or once per cleared LINE, never scaling with
        // group/piece cell count) — kept as flat +pts: every bonus that
        // already scales per group cell or per placed cell (Forteresse,
        // Prisonnier, Couronne, Carrefour, Contraste, Emmitouflée, Jardinier,
        // Cercle Chromatique, Monochrome, Collectionneur, Diagonale, Nid,
        // Encerclement, Boucher, Éclat x4, Grand/Petit/Hors-Norme Format,
        // Precision, Surpopulation, Chaîne/Méga-chaîne) — multiplying THOSE
        // too would let one placement's multiplier scale unboundedly with
        // however big its group happens to be, which reads as a bug rather
        // than a feature. The converted ones use xN factors here instead of
        // pts values — mostly x2 (matching the existing Devotion/Forme/Slot
        // "doubles" convention), x3 for the rarer/harder-to-trigger ones
        // (Prisme, Rafale, Puriste). On explicit request, round score quotas
        // (see RunConfig) were raised to scale exponentially to match the
        // resulting much bigger placement totals once several of these stack.

        /// <summary>Prisme: xN multiplier when the placement (itself + its direct neighbors) touches 4 distinct non-joker colors (or 3 + a joker).</summary>
        public const int PrismeMultiplier = 3;
        public const int PrismeMinDistinctColors = 4;

        /// <summary>Chaîne: flat bonus once the group reaches this many cells.</summary>
        public const int ChaineBonus = 10;
        public const int ChaineMinGroupSize = 5;

        /// <summary>Méga-chaîne: base bonus at the size threshold, plus a per-cell bonus for every cell beyond it.</summary>
        public const int MegaChaineBaseBonus = 30;
        public const int MegaChaineMinGroupSize = 10;
        public const int MegaChaineBonusPerExtraCell = 5;

        /// <summary>Forteresse: bonus per group cell whose 8 surrounding neighbors are all filled.</summary>
        public const int ForteresseBonusPerCell = 6;

        /// <summary>Prisonnier: bonus per group cell whose 4 cardinal neighbors are all filled.</summary>
        public const int PrisonnierBonusPerCell = 4;

        /// <summary>Architecte: xN multiplier for placing a 2x2 square piece.</summary>
        public const int ArchitecteMultiplier = 2;

        /// <summary>Collectionneur: bonus per distinct color among this placement's cleared cells.</summary>
        public const int CollectionneurBonusPerColor = 8;

        /// <summary>Tricolore: xN multiplier when the placement (itself + its direct neighbors) touches exactly this many distinct non-joker colors.</summary>
        public const int TricoloreMultiplier = 2;
        public const int TricoloreExactDistinctColors = 3;

        /// <summary>Complémentaire: xN multiplier when the placement (itself + its direct neighbors) touches both colors of a complementary pair.</summary>
        public const int ComplementaireMultiplier = 2;

        /// <summary>Îlot: xN multiplier when the placement's resulting group is a single isolated cell.</summary>
        public const int IlotMultiplier = 2;

        /// <summary>Couronne: bonus per group cell sitting on the grid's outer border.</summary>
        public const int CouronneBonusPerCell = 5;

        /// <summary>Carrefour: bonus per group cell whose 4 cardinal neighbors are filled with at least 2 colors different from BOTH each other and the cell's own color.</summary>
        public const int CarrefourBonusPerCell = 12;

        /// <summary>Maçon: xN multiplier for a placement that clears no line/column at all.</summary>
        public const int MaconMultiplier = 2;

        /// <summary>Démolisseur: xN multiplier PER LINE, only once at least this many rows/columns clear simultaneously (stacks — 3 lines at once is xN*xN*xN).</summary>
        public const int DemolisseurMultiplierPerLine = 2;
        public const int DemolisseurMinLines = 2;

        // ---- Second batch of modifier bonuses (see ModifierCatalog) ----

        /// <summary>Cercle Chromatique: bonus per group cell whose 4 filled cardinal neighbors together show all 4 base colors. Raised 18->35 (explicit request) — very hard to actually land.</summary>
        public const int CercleChromatiqueBonusPerCell = 35;

        /// <summary>Monochrome: bonus per group cell when the whole group is a single real color with zero jokers (stricter than Puriste).</summary>
        public const int MonochromeBonusPerCell = 3;

        /// <summary>Contraste: bonus per placed cell with at least one filled orthogonal neighbor of a different color.</summary>
        public const int ContrasteBonusPerCell = 6;

        /// <summary>Dégradé: xN multiplier whenever this placement's scored group is strictly larger than the previous placement's this round.</summary>
        public const int DegradeMultiplier = 2;

        /// <summary>Emmitouflée: bonus per group cell whose 4 diagonal neighbors are all filled.</summary>
        public const int EmmitoufleeBonusPerCell = 8;

        /// <summary>Jardinier: bonus per group cell orthogonally adjacent to a golden/tinted/multiplier-zone cell.</summary>
        public const int JardinierBonusPerCell = 6;

        /// <summary>Arc-en-ciel: xN multiplier PER cleared row/column containing all 4 base colors (stacks across simultaneous lines).</summary>
        public const int ArcEnCielMultiplierPerLine = 2;

        /// <summary>Alternance: xN multiplier PER cleared row/column whose colors strictly alternate between exactly 2 colors.</summary>
        public const int AlternanceMultiplierPerLine = 2;

        /// <summary>Palindrome: xN multiplier PER cleared row/column whose color sequence reads the same forwards and backwards.</summary>
        public const int PalindromeMultiplierPerLine = 2;

        /// <summary>Bloc: xN multiplier PER cleared row/column made only of contiguous same-color runs of at least 2.</summary>
        public const int BlocMultiplierPerLine = 2;

        /// <summary>Monochrome Ligne: xN multiplier PER cleared row/column that is entirely a single color (jokers ignored).</summary>
        public const int MonochromeLigneMultiplierPerLine = 2;

        // ---- Second batch of tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Catalyst Tile: bonus per pre-existing cell merged into this placement's scored group (group size minus the piece's own cell count).</summary>
        public const int CatalystBonusPerExistingCell = 3;

        /// <summary>Spark Tile: bonus per consecutive placement since the last line/column clear this round.</summary>
        public const int SparkBonusPerPlacement = 5;

        // ---- Fourth batch of modifier bonuses (see ModifierCatalog) ----

        /// <summary>Slot N Loyalty: xN multiplier on this placement's ENTIRE score (see RunManager.ApplyHandSlotModifierBonus) when played from the matching hand slot — was "doubles just the group bonus", changed to double everything on explicit request ("au lieu de double group placement, on va tout doubler").</summary>
        public const int SlotLoyaltyMultiplier = 2;

        /// <summary>Grand Format: bonus per placed cell when the piece being placed has at least this many cells.</summary>
        public const int GrandFormatBonusPerCell = 8;
        public const int GrandFormatMinPieceSize = 3;

        /// <summary>Hors Norme: flat bonus when the piece being placed does NOT have exactly this many cells.</summary>
        public const int HorsNormeBonus = 12;
        public const int HorsNormeExactPieceSize = 3;

        /// <summary>Éclat (per-color): bonus per scored group cell of the matching color — unlike Devotion's full double, a flat per-tile amount.</summary>
        public const int EclatBonusPerCell = 4;

        // ---- Fifth batch of modifier bonuses (see ModifierCatalog) ----

        /// <summary>Diagonale: bonus per group cell sitting on either of the grid's two main diagonals.</summary>
        public const int DiagonaleBonusPerCell = 5;

        /// <summary>Nid: bonus per group cell with exactly 3 of its 4 cardinal neighbors filled.</summary>
        public const int NidBonusPerCell = 3;

        /// <summary>Solitaire: xN multiplier when this placement's group is entirely its own piece (more than 1 cell), nothing pre-existing merged in.</summary>
        public const int SolitaireMultiplier = 2;

        /// <summary>Petit Format: bonus per placed cell when the piece has at most this many cells.</summary>
        public const int PetitFormatBonusPerCell = 5;
        public const int PetitFormatMaxPieceSize = 2;

        /// <summary>Fraîcheur: xN multiplier when this placement's fill color is nowhere else on the board yet.</summary>
        public const int FraicheurMultiplier = 2;

        /// <summary>Espace Libre: xN multiplier once at most this many cells on the whole board are still filled.</summary>
        public const int EspaceLibreMultiplier = 2;
        public const int EspaceLibreMaxFilledCells = 16; // 25% of the 64-cell board

        /// <summary>Rafale: xN multiplier when this placement clears a line AND the immediately previous one this round also did.</summary>
        public const int RafaleMultiplier = 3;

        // ---- Third batch of tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Kamikaze Tile: bonus per surrounding tile actually destroyed (this placement's own cells excluded).</summary>
        public const int KamikazeBonusPerDestroyedCell = 6;

        // ---- Sixth batch of modifier bonuses (player-authored brainstorm, see ModifierCatalog) ----

        /// <summary>Bridge (Pont): xN multiplier PER pre-existing group bridged together by this placement beyond the first (bridging 2 groups applies once, 3 groups twice, stacking multiplicatively).</summary>
        public const int PontMultiplierPerBridge = 2;

        /// <summary>Encirclement (Encerclement): bonus per group cell whose 8 surrounding tiles are all filled OR off the edge of the grid — softer than Fortress, which never gives edge/corner cells any credit.</summary>
        public const int EncerclementBonusPerCell = 6;

        /// <summary>Sealer (Boucher): bonus per pre-existing tile that this placement itself causes to become "encircled" (see Encerclement) — i.e. this placement fills the one missing neighbor that was keeping it from qualifying.</summary>
        public const int BoucherBonusPerCell = 10;

        /// <summary>Big Family (Grosse Famille): xN multiplier when this placement's color exists in exactly one connected group on the whole board — no other same-color cell anywhere else.</summary>
        public const int GrosseFamilleMultiplier = 2;

        /// <summary>Color Switch (Alternance des pièces): xN multiplier when this piece's color differs from the immediately previous placement's color this round — the piece-to-piece sibling of the existing line-level "Alternation" modifier.</summary>
        public const int AlternancePiecesMultiplier = 2;

        /// <summary>Combo: multiplies this placement's ENTIRE total score (see PlacementResult.ComboMultiplier) when the immediately previous placement this round cleared a line — the only modifier that's a true multiplier rather than a flat/per-cell bonus, on explicit request.</summary>
        public const int ComboMultiplierFactor = 2;

        /// <summary>Precision: bonus per placed cell when EVERY one of this placement's own cells has at least one pre-existing filled orthogonal neighbor.</summary>
        public const int PrecisionBonusPerCell = 5;

        /// <summary>Overcrowding (Surpopulation): bonus per placed cell when EVERY one of this placement's own cells has at least 2 pre-existing filled orthogonal neighbors — a stricter sibling of Precision.</summary>
        public const int SurpopulationBonusPerCell = 8;

        /// <summary>Minimalist (Minimaliste): xN multiplier when this placement's whole footprint touches EXACTLY one distinct pre-existing filled cell, total — the "just barely touching" middle ground between Îlot (zero) and Precision (one or more, per cell).</summary>
        public const int MinimalisteMultiplier = 2;

        /// <summary>Puriste: xN multiplier when the placement's scored group is monochrome (jokers ignored) — was "+50% of the group's points" (a de facto x1.5), now a clean xN like every other converted modifier.</summary>
        public const int PuristeMultiplier = 3;

        // ---- Seventh batch: progressive modifiers (on explicit request) ----

        /// <summary>Density (Densité): filled cells on the board (after this placement's own clears) are divided by this many, floored, to get the xN multiplier — i.e. +0.1x per filled tile, kept as a clean integer step instead of introducing fractional multipliers.</summary>
        public const int DensiteFilledCellsPerMultiplierStep = 10;

        // ---- Ninth batch (on explicit request) ----

        /// <summary>The 3 flat, unconditional "+Mult" modifiers (see PlacementResult.AdditiveMultBonus) — always fire, no condition to satisfy.</summary>
        public const int MultUnBonus = 1;
        public const int MultDeuxBonus = 2;
        public const int MultQuatreBonus = 4;

        /// <summary>Devotion (per-color): xN multiplier when placing a piece of the matching color — was "doubles this placement's group bonus" (additive), converted to a genuine multiplier like every other "+mult"-style modifier, on explicit request that every color/shape modifier have both a +pts version (Éclat/the new Forme*Points siblings) and a +mult version.</summary>
        public const int DevotionMultiplier = 2;

        /// <summary>Forme* "Specialist" (per-shape): xN multiplier when the placed piece's own shape matches — same conversion, and for the same reason, as Devotion above.</summary>
        public const int FormeSpecialistMultiplier = 2;

        /// <summary>The new "+pts" sibling of each Forme* "Specialist" modifier — bonus per scored group cell when the placed piece's shape matches, same pattern as Éclat's per-color bonus.</summary>
        public const int FormeGlowBonusPerCell = 4;

        /// <summary>Risky Mult: flat +Mult (see PlacementResult.AdditiveMultBonus), always fires — the risk is EconomyConstants.MultCinqRisqueLossChanceDenominator's chance to lose the modifier itself at round end, not a scoring condition.</summary>
        public const int MultCinqRisqueBonus = 5;

        /// <summary>Enchanted Cards: upgraded (enchanted) cards currently in the deck, PLUS 1 (so it's never literally zero — "starting at one"), divided by this many and floored, to get the +Mult contributed — i.e. +0.1 Mult per upgraded card, counting from a baseline of 1, kept as a clean integer step instead of introducing fractional Mult (same reasoning as Density above).</summary>
        public const int CartesEnchanteesUpgradedCardsPerMultStep = 10;

        /// <summary>Dwindling: starting flat points bonus — permanent for the whole run, never reset back to this once it starts decaying.</summary>
        public const int EpuisementStartingBonus = 100;

        /// <summary>Dwindling: how much the current bonus drops after EACH placement it's held for (floored at 0, never negative).</summary>
        public const int EpuisementDecayPerPlacement = 5;

        /// <summary>Multitude: points per piece currently in the deck (see DeckManager.DeckCount) — counts every token regardless of pile (hand/draw pile/discard), the deck's true total size.</summary>
        public const int MultitudeBonusPerDeckCard = 1;

        /// <summary>Experience: special (trait-carrying) pieces PLAYED this run so far, PLUS 1 (same "starting at one" baseline as Enchanted Cards), divided by this many and floored, to get the +Mult contributed — i.e. +0.1 Mult per special piece played, counting from a baseline of 1, same integer-step trick as CartesEnchanteesUpgradedCardsPerMultStep, just keyed on PLAYED count instead of current deck count.</summary>
        public const int ExperienceSpecialPiecesPlayedPerMultStep = 10;
    }
}
