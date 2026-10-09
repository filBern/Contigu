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
        /// group's progressive per-cell scoring — the Nth cell scored in
        /// the group (1-indexed, in scan order) is worth N * this constant,
        /// so a full group of N cells scores the triangular number
        /// N*(N+1)/2 * GroupBonusPerCell in total, growing faster than
        /// group size instead of linearly with it. The whole group is
        /// rescored in full every time a placement grows it.
        /// </summary>
        public const int GroupBonusPerCell = 1;

        /// <summary>Fixed bonus for filling a golden cell, independent of color. Computed separately and simply added to the total — never multiplied by group size or the group multiplier. Fires again every time the cell is part of a rescored group, not just when first placed.</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column — dropped from 12.</summary>
        public const int LineClearBonusPerCell = 3;

        /// <summary>Multiplier contributed by EACH matching tinted cell in a placement's group — two tinted cells in the same group stack to x4, three to x8, and so on. Only reaches the group+golden bonus, never the line-clear bonus (see PlacementResult.LineClearMultiplier) — the one thing that still tells it apart from MultiplierZoneMultiplier now that Tinted's target color always matches its own piece.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group+golden bonus AND its line-clear bonus when the group contains a multiplier-zone cell — broader reach than TintedMatchMultiplier, matching its higher (Uncommon vs Common) rarity.</summary>
        public const int MultiplierZoneMultiplier = 2;

        /// <summary>"Leech" enemy mechanic: HP healed per row/column cleared, not per cell (see PlacementResult.ClearedLineCount/RunManager.HealLeech).</summary>
        public const int LeechHealPerLineClear = 250;

        // ---- Modifier bonuses (see ModifierCatalog) ----
        // Multiplier-based modifiers (xN) are kept to one-shot conditional
        // bonuses (fire at most once per placement or per cleared line).
        // Any bonus that scales per group cell or per placed cell (Forteresse,
        // Prisonnier, Couronne, Carrefour, Contraste, Emmitouflée, Jardinier,
        // Cercle Chromatique, Monochrome, Collectionneur, Diagonale, Nid,
        // Encerclement, Boucher, Éclat x4, Grand/Petit/Hors-Norme Format,
        // Precision, Surpopulation, Chaîne/Méga-chaîne) stays a flat +pts
        // bonus instead, so it can't scale unboundedly with group size.

        /// <summary>Prisme: xN multiplier when the placement (itself + its direct neighbors) touches 4 distinct non-joker colors (or 3 + a joker).</summary>
        public const int PrismeMultiplier = 3;
        public const int PrismeMinDistinctColors = 4;

        /// <summary>Chaîne: flat bonus once the group reaches this many cells.</summary>
        public const int ChaineBonus = 15;
        public const int ChaineMinGroupSize = 5;

        /// <summary>Méga-chaîne: base bonus at the size threshold, plus a per-cell bonus for every cell beyond it.</summary>
        public const int MegaChaineBaseBonus = 45;
        public const int MegaChaineMinGroupSize = 10;
        public const int MegaChaineBonusPerExtraCell = 8;

        /// <summary>Forteresse: bonus per group cell whose 8 surrounding neighbors are all filled.</summary>
        public const int ForteresseBonusPerCell = 9;

        /// <summary>Prisonnier: bonus per group cell whose 4 cardinal neighbors are all filled.</summary>
        public const int PrisonnierBonusPerCell = 6;

        /// <summary>Architecte: xN multiplier for placing a 2x2 square piece.</summary>
        public const int ArchitecteMultiplier = 2;

        /// <summary>Collectionneur: bonus per distinct color among this placement's cleared cells.</summary>
        public const int CollectionneurBonusPerColor = 12;

        /// <summary>Tricolore: xN multiplier when the placement (itself + its direct neighbors) touches exactly this many distinct non-joker colors.</summary>
        public const int TricoloreMultiplier = 2;
        public const int TricoloreExactDistinctColors = 3;

        /// <summary>Complémentaire: xN multiplier when the placement (itself + its direct neighbors) touches both colors of a complementary pair.</summary>
        public const int ComplementaireMultiplier = 2;

        /// <summary>Îlot: xN multiplier when the placement's resulting group is a single isolated cell.</summary>
        public const int IlotMultiplier = 2;

        /// <summary>Couronne: bonus per group cell sitting on the grid's outer border.</summary>
        public const int CouronneBonusPerCell = 8;

        /// <summary>Carrefour: bonus per group cell whose 4 cardinal neighbors are filled with at least 2 colors different from BOTH each other and the cell's own color.</summary>
        public const int CarrefourBonusPerCell = 18;

        /// <summary>Maçon: flat +Mult (additive, see PlacementResult.AdditiveMultBonus) for a placement that clears no line/column at all.</summary>
        public const int MaconBonus = 2;

        /// <summary>Démolisseur: xN multiplier PER LINE, only once at least this many rows/columns clear simultaneously (stacks — 3 lines at once is xN*xN*xN).</summary>
        public const int DemolisseurMultiplierPerLine = 2;
        public const int DemolisseurMinLines = 2;

        // ---- More modifier bonuses (see ModifierCatalog) ----

        /// <summary>Cercle Chromatique: bonus per group cell whose 4 filled cardinal neighbors together show all 4 base colors.</summary>
        public const int CercleChromatiqueBonusPerCell = 52;

        /// <summary>Monochrome: bonus per group cell when the whole group is a single real color with zero jokers.</summary>
        public const int MonochromeBonusPerCell = 5;

        /// <summary>Contraste: bonus per placed cell with at least one filled orthogonal neighbor of a different color.</summary>
        public const int ContrasteBonusPerCell = 9;

        /// <summary>Dégradé: xN multiplier whenever this placement's scored group is strictly larger than the previous placement's this round.</summary>
        public const int DegradeMultiplier = 2;

        /// <summary>Emmitouflée: bonus per group cell whose 4 diagonal neighbors are all filled.</summary>
        public const int EmmitoufleeBonusPerCell = 12;

        /// <summary>Jardinier: bonus per group cell orthogonally adjacent to a golden/tinted/multiplier-zone cell.</summary>
        public const int JardinierBonusPerCell = 9;

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

        // ---- Tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Catalyst Tile: bonus per pre-existing cell merged into this placement's scored group (group size minus the piece's own cell count).</summary>
        public const int CatalystBonusPerExistingCell = 3;

        /// <summary>Spark Tile: bonus per consecutive placement since the last line/column clear this round.</summary>
        public const int SparkBonusPerPlacement = 5;

        // ---- More modifier bonuses (see ModifierCatalog) ----

        /// <summary>Slot N Loyalty: flat +Mult (additive, see PlacementResult.AdditiveMultBonus) when played from the matching hand slot (see RunManager.ApplyHandSlotModifierBonus).</summary>
        public const int SlotLoyaltyBonus = 3;

        /// <summary>Grand Format: bonus per placed cell when the piece being placed has at least this many cells.</summary>
        public const int GrandFormatBonusPerCell = 12;
        public const int GrandFormatMinPieceSize = 3;

        /// <summary>Hors Norme: flat bonus when the piece being placed does NOT have exactly this many cells.</summary>
        public const int HorsNormeBonus = 18;
        public const int HorsNormeExactPieceSize = 3;

        /// <summary>Éclat (per-color): bonus per scored group cell of the matching color — unlike Devotion's full double, a flat per-tile amount.</summary>
        public const int EclatBonusPerCell = 6;

        // ---- More modifier bonuses (see ModifierCatalog) ----

        /// <summary>Diagonale: bonus per group cell sitting on either of the grid's two main diagonals.</summary>
        public const int DiagonaleBonusPerCell = 8;

        /// <summary>Nid: bonus per group cell with exactly 3 of its 4 cardinal neighbors filled.</summary>
        public const int NidBonusPerCell = 5;

        /// <summary>Solitaire: xN multiplier when this placement's group is entirely its own piece (more than 1 cell), nothing pre-existing merged in.</summary>
        public const int SolitaireMultiplier = 2;

        /// <summary>Petit Format: bonus per placed cell when the piece has at most this many cells.</summary>
        public const int PetitFormatBonusPerCell = 8;
        public const int PetitFormatMaxPieceSize = 2;

        /// <summary>Fraîcheur: xN multiplier when this placement's fill color is nowhere else on the board yet.</summary>
        public const int FraicheurMultiplier = 2;

        /// <summary>Espace Libre: xN multiplier once at most this many cells on the whole board are still filled.</summary>
        public const int EspaceLibreMultiplier = 2;
        public const int EspaceLibreMaxFilledCells = 16; // 25% of the 64-cell board

        /// <summary>Rafale: xN multiplier when this placement clears a line AND the immediately previous one this round also did.</summary>
        public const int RafaleMultiplier = 3;

        // ---- Tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Kamikaze Tile: bonus per tile actually destroyed, including this same placement's own other cells and the trait cell itself.</summary>
        public const int KamikazeBonusPerDestroyedCell = 6;

        /// <summary>Void Tile: bonus for the one already-filled cell elsewhere on the grid it destroys. A no-op (no bonus) when nothing else on the grid is eligible to be destroyed.</summary>
        public const int VoidBonusPerDestroyedCell = 10;

        // ---- More modifier bonuses (see ModifierCatalog) ----

        /// <summary>Bridge (Pont): xN multiplier PER pre-existing group bridged together by this placement beyond the first (bridging 2 groups applies once, 3 groups twice, stacking multiplicatively).</summary>
        public const int PontMultiplierPerBridge = 2;

        /// <summary>Encirclement (Encerclement): bonus per group cell whose 8 surrounding tiles are all filled OR off the edge of the grid — softer than Fortress, which never gives edge/corner cells any credit.</summary>
        public const int EncerclementBonusPerCell = 9;

        /// <summary>Sealer (Boucher): bonus per pre-existing tile that this placement itself causes to become "encircled" (see Encerclement) — i.e. this placement fills the one missing neighbor that was keeping it from qualifying.</summary>
        public const int BoucherBonusPerCell = 15;

        /// <summary>Big Family (Grosse Famille): xN multiplier when this placement's color exists in exactly one connected group on the whole board — no other same-color cell anywhere else.</summary>
        public const int GrosseFamilleMultiplier = 2;

        /// <summary>Color Switch (Alternance des pièces): flat +Mult (additive, see PlacementResult.AdditiveMultBonus) when this piece's color differs from the immediately previous placement's color this round — the piece-to-piece sibling of the existing line-level "Alternation" modifier.</summary>
        public const int AlternancePiecesBonus = 2;

        /// <summary>Combo: multiplies this placement's entire total score (see PlacementResult.ComboMultiplier) when the immediately previous placement this round cleared a line — the only modifier that's a true multiplier rather than a flat/per-cell bonus.</summary>
        public const int ComboMultiplierFactor = 2;

        /// <summary>Precision: bonus per placed cell when EVERY one of this placement's own cells has at least one pre-existing filled orthogonal neighbor.</summary>
        public const int PrecisionBonusPerCell = 8;

        /// <summary>Overcrowding (Surpopulation): bonus per placed cell when EVERY one of this placement's own cells has at least 2 pre-existing filled orthogonal neighbors — a stricter sibling of Precision.</summary>
        public const int SurpopulationBonusPerCell = 12;

        /// <summary>Minimalist (Minimaliste): xN multiplier when this placement's whole footprint touches EXACTLY one distinct pre-existing filled cell, total — the "just barely touching" middle ground between Îlot (zero) and Precision (one or more, per cell).</summary>
        public const int MinimalisteMultiplier = 2;

        /// <summary>Density (Densité): filled cells on the board (after this placement's own clears), divided by this many as a true float (never floored), to get the +Mult contributed — i.e. +0.1 Mult per filled tile. No "start at 1" baseline here (unlike CartesEnchantees/Experience below): an empty board correctly contributes +0 Mult.</summary>
        public const int DensiteFilledCellsPerMultStep = 10;

        /// <summary>The 3 flat, unconditional "+Mult" modifiers (see PlacementResult.AdditiveMultBonus) — always fire, no condition to satisfy.</summary>
        public const int MultUnBonus = 1;
        public const int MultDeuxBonus = 2;
        public const int MultQuatreBonus = 4;

        /// <summary>Devotion (per-color): flat +Mult (additive, see PlacementResult.AdditiveMultBonus) when placing a piece of the matching color.</summary>
        public const int DevotionBonus = 3;

        /// <summary>Format* "Specialist" (per-piece-size tier, not per-exact-shape): xN multiplier when the placed piece's cell count falls in the modifier's tier.</summary>
        public const int FormeSpecialistMultiplier = 2;

        /// <summary>The "+pts" sibling of each Format* "Specialist" tier above — bonus per scored group cell when the placed piece's cell count falls in the tier, same pattern as Éclat's per-color bonus.</summary>
        public const int FormeGlowBonusPerCell = 6;

        /// <summary>Risky Mult: flat +Mult (see PlacementResult.AdditiveMultBonus), always fires — the risk is EconomyConstants.MultCinqRisqueLossChanceDenominator's chance to lose the modifier itself at round end, not a scoring condition.</summary>
        public const int MultCinqRisqueBonus = 5;

        /// <summary>Solidarity (Solidarite): total modifiers currently held (this one included, every duplicate copy counting separately) divided by this many, floored, to get the +Mult contributed.</summary>
        public const int SolidariteModifierCountDivisor = 2;

        /// <summary>Enchanted Cards: upgraded (enchanted) cards currently in the deck, PLUS 1 (so it's never literally zero — "starting at one"), divided by this many and floored, to get the +Mult contributed — i.e. +0.1 Mult per upgraded card, counting from a baseline of 1, kept as a clean integer step instead of introducing fractional Mult (same reasoning as Density above).</summary>
        public const int CartesEnchanteesUpgradedCardsPerMultStep = 10;

        /// <summary>Dwindling: starting flat points bonus — permanent for the whole run, never reset back to this once it starts decaying.</summary>
        public const int EpuisementStartingBonus = 150;

        /// <summary>Dwindling: how much the current bonus drops after EACH placement it's held for (floored at 0, never negative).</summary>
        public const int EpuisementDecayPerPlacement = 5;

        /// <summary>Multitude: points per piece currently in the deck (see DeckManager.DeckCount) — counts every token regardless of pile (hand/draw pile/discard), the deck's true total size.</summary>
        public const int MultitudeBonusPerDeckCard = 2;

        /// <summary>Experience: special (trait-carrying) pieces PLAYED this run so far, PLUS 1 (same "starting at one" baseline as Enchanted Cards), divided by this many and floored, to get the +Mult contributed — i.e. +0.1 Mult per special piece played, counting from a baseline of 1, same integer-step trick as CartesEnchanteesUpgradedCardsPerMultStep, just keyed on PLAYED count instead of current deck count.</summary>
        public const int ExperienceSpecialPiecesPlayedPerMultStep = 10;

        // Format* size-tier boundaries. A piece's cell count
        // (PieceShape.Cells.Count) determines its tier: Petit is at most
        // this many cells; Moyen is exactly this many (the 3 Trominoes are
        // the only 3-cell shapes in the catalog); Grand is at least this
        // many, phrased as "at least" so a future bigger piece would fall
        // into Grand automatically instead of matching nothing.

        public const int FormatPetitMaxCells = 2;
        public const int FormatMoyenCells = 3;
        public const int FormatGrandMinCells = 4;

        /// <summary>"Sangsue" (Joker-exclusive combat trait, see PieceTrait.JokerCombatKinds) — fraction of the damage it deals to its target that also converts into bonus Lueur (floored), see RunManager.ApplySangsueDamage.</summary>
        public const float SangsueLueurFraction = 0.25f;

        /// <summary>Pair: xN multiplier (see GridManager.ApplyParitePaire) when the placement's scored group has an EVEN total cell count.</summary>
        public const int PairMultiplier = 2;

        /// <summary>Impair: xN multiplier (see GridManager.ApplyPariteImpaire) when the placement's scored group has an ODD total cell count — Pair's exact mirror.</summary>
        public const int ImpairMultiplier = 2;

        /// <summary>Devotion/Éclat pairing bonus: extra flat +Mult (additive, see PlacementResult.AdditiveMultBonus) when BOTH a color's Devotion and Éclat modifier are held, on top of Devotion's own bonus — see GridManager.ApplyDevotionEclatPairBonus, checked only from the Devotion side so the pair is never double-counted.</summary>
        public const int DevotionEclatPairBonus = 2;

        /// <summary>Polyvalence: +N Mult (additive), N = the number of DISTINCT ModifierCategory values among currently held modifiers (this one's own Roguelike category included) — see GridManager.ApplyPolyvalence. Capped at 6 in practice (ModifierCategory's own value count).</summary>
        public const int PolyvalenceMultPerCategory = 1;

        /// <summary>Renfort Joker: percentage damage bonus applied only to a placement whose scored group carries at least one Joker-exclusive combat trait (see RunManager.ApplyJokerCombatOrDefaultDamage) — no effect otherwise.</summary>
        public const int RenfortJokerDamageBonusPercent = 25;

        /// <summary>Arsenal: +N Mult (additive), N = the number of DISTINCT Joker combat trait kinds (see PieceTrait.JokerCombatKinds) currently anywhere in the deck — see RunManager.CountDistinctCombatKindsInDeck. Capped at 5 in practice (JokerCombatKinds' own length); two tokens sharing the same kind count once.</summary>
        public const int ArsenalMultPerDistinctCombatKind = 1;

        /// <summary>Collection Chromatique: +N Mult (additive), N = the number of the 4 base colors for which BOTH Devotion and Éclat are currently held — see GridManager.ApplyCollectionChromatique. Capped at 4 in practice.</summary>
        public const int CollectionChromatiqueMultPerCompletePair = 2;

        /// <summary>Cadence: xN multiplier (see GridManager.ApplyCadence) when this placement's group parity matches a held Pair/Impair modifier AND its piece size matches a held Format* Specialist tier, both at once.</summary>
        public const int CadenceMultiplier = 2;

        /// <summary>Siphon: fraction of Joker-combat damage (see RunManager.ApplyJokerCombatOrDefaultDamage) converted into bonus Lueur (floored) — Sangsue's own fraction generalized to fire alongside every combat kind, not just Sangsue. Set lower than Sangsue's own (0.25f) since it fires on strictly more placements.</summary>
        public const float SiphonLueurFraction = 0.15f;
    }
}
