namespace Contigu.Core
{
    /// <summary>
    /// Reference scoring values. Kept as a single tunable surface so balancing
    /// doesn't require touching game logic.
    /// </summary>
    public static class ScoringConstants
    {
        /// <summary>
        /// Points per cell in a placement's resulting connected same-color group
        /// (see <see cref="GridManager"/> group scoring). The WHOLE group is
        /// rescored in full every time a placement grows it — like replaying an
        /// extended Scrabble word — so a bigger connected blob is worth more
        /// every time it's touched again, not just once.
        /// </summary>
        public const int GroupBonusPerCell = 1;

        /// <summary>Fixed bonus for filling a golden cell, independent of color. Computed separately and simply added to the total — never multiplied by group size or the group multiplier. Fires again every time the cell is part of a rescored group, not just when first placed.</summary>
        public const int GoldenCellBonus = 18;

        /// <summary>Points per cell cleared by a completed line/column.</summary>
        public const int LineClearBonusPerCell = 12;

        /// <summary>Multiplier contributed by EACH matching tinted cell in a placement's group — two tinted cells in the same group stack to x4, three to x8, and so on. Only reaches the group+golden bonus, never the line-clear bonus (see PlacementResult.LineClearMultiplier) — the one thing that still tells it apart from MultiplierZoneMultiplier now that Tinted's target color always matches its own piece.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group+golden bonus AND its line-clear bonus when the group contains a multiplier-zone cell — broader reach than TintedMatchMultiplier, matching its higher (Uncommon vs Common) rarity.</summary>
        public const int MultiplierZoneMultiplier = 2;

        // ---- Modifier bonuses (see ModifierCatalog) ----

        /// <summary>Prisme: flat bonus when the placement (itself + its direct neighbors) touches 4 distinct non-joker colors (or 3 + a joker).</summary>
        public const int PrismeBonus = 20;
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

        /// <summary>Architecte: flat bonus for placing a 2x2 square piece.</summary>
        public const int ArchitecteBonus = 15;

        /// <summary>Collectionneur: bonus per distinct color among this placement's cleared cells.</summary>
        public const int CollectionneurBonusPerColor = 8;

        /// <summary>Tricolore: flat bonus when the placement (itself + its direct neighbors) touches exactly this many distinct non-joker colors.</summary>
        public const int TricoloreBonus = 14;
        public const int TricoloreExactDistinctColors = 3;

        /// <summary>Complémentaire: flat bonus when the placement (itself + its direct neighbors) touches both colors of a complementary pair.</summary>
        public const int ComplementaireBonus = 16;

        /// <summary>Îlot: flat bonus when the placement's resulting group is a single isolated cell.</summary>
        public const int IlotBonus = 8;

        /// <summary>Couronne: bonus per group cell sitting on the grid's outer border.</summary>
        public const int CouronneBonusPerCell = 5;

        /// <summary>Carrefour: bonus per group cell whose 4 cardinal neighbors are filled with at least 2 colors different from BOTH each other and the cell's own color.</summary>
        public const int CarrefourBonusPerCell = 12;

        /// <summary>Maçon: flat bonus for a placement that clears no line/column at all.</summary>
        public const int MaconBonus = 5;

        /// <summary>Démolisseur: bonus per line, only once at least this many rows/columns clear simultaneously.</summary>
        public const int DemolisseurBonusPerLine = 15;
        public const int DemolisseurMinLines = 2;

        // ---- Second batch of modifier bonuses (see ModifierCatalog) ----

        /// <summary>Cercle Chromatique: bonus per group cell whose 4 filled cardinal neighbors together show all 4 base colors. Raised 18->35 (explicit request) — very hard to actually land.</summary>
        public const int CercleChromatiqueBonusPerCell = 35;

        /// <summary>Monochrome: bonus per group cell when the whole group is a single real color with zero jokers (stricter than Puriste).</summary>
        public const int MonochromeBonusPerCell = 3;

        /// <summary>Contraste: bonus per placed cell with at least one filled orthogonal neighbor of a different color.</summary>
        public const int ContrasteBonusPerCell = 6;

        /// <summary>Dégradé: flat bonus whenever this placement's scored group is strictly larger than the previous placement's this round.</summary>
        public const int DegradeBonus = 10;

        /// <summary>Emmitouflée: bonus per group cell whose 4 diagonal neighbors are all filled.</summary>
        public const int EmmitoufleeBonusPerCell = 8;

        /// <summary>Jardinier: bonus per group cell orthogonally adjacent to a golden/tinted/multiplier-zone cell.</summary>
        public const int JardinierBonusPerCell = 6;

        /// <summary>Arc-en-ciel: bonus per cleared row/column containing all 4 base colors.</summary>
        public const int ArcEnCielBonusPerLine = 25;

        /// <summary>Alternance: bonus per cleared row/column whose colors strictly alternate between exactly 2 colors.</summary>
        public const int AlternanceBonusPerLine = 16;

        /// <summary>Palindrome: bonus per cleared row/column whose color sequence reads the same forwards and backwards.</summary>
        public const int PalindromeBonusPerLine = 18;

        /// <summary>Gradient: bonus per cleared row/column where no two adjacent cells share the same color.</summary>
        public const int GradientBonusPerLine = 10;

        /// <summary>Bloc: bonus per cleared row/column made only of contiguous same-color runs of at least 2.</summary>
        public const int BlocBonusPerLine = 9;

        /// <summary>Monochrome Ligne: bonus per cleared row/column that is entirely a single color (jokers ignored).</summary>
        public const int MonochromeLigneBonusPerLine = 24;

        // ---- Second batch of tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Catalyst Tile: bonus per pre-existing cell merged into this placement's scored group (group size minus the piece's own cell count).</summary>
        public const int CatalystBonusPerExistingCell = 3;

        /// <summary>Spark Tile: bonus per consecutive placement since the last line/column clear this round.</summary>
        public const int SparkBonusPerPlacement = 5;

        // ---- Fourth batch of modifier bonuses (see ModifierCatalog) ----

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

        /// <summary>Solitaire: flat bonus when this placement's group is entirely its own piece (more than 1 cell), nothing pre-existing merged in.</summary>
        public const int SolitaireBonus = 12;

        /// <summary>Petit Format: bonus per placed cell when the piece has at most this many cells.</summary>
        public const int PetitFormatBonusPerCell = 5;
        public const int PetitFormatMaxPieceSize = 2;

        /// <summary>Fraîcheur: flat bonus when this placement's fill color is nowhere else on the board yet.</summary>
        public const int FraicheurBonus = 10;

        /// <summary>Imminent: bonus per row/column left with exactly one empty unlocked cell once this placement has fully resolved.</summary>
        public const int ImminentBonusPerLine = 10;

        /// <summary>Espace Libre: flat bonus once at most this many cells on the whole board are still filled.</summary>
        public const int EspaceLibreBonus = 15;
        public const int EspaceLibreMaxFilledCells = 16; // 25% of the 64-cell board

        /// <summary>Rafale: flat bonus when this placement clears a line AND the immediately previous one this round also did.</summary>
        public const int RafaleBonus = 20;

        // ---- Third batch of tile-upgrade (PieceTrait) bonuses ----

        /// <summary>Kamikaze Tile: bonus per surrounding tile actually destroyed (this placement's own cells excluded).</summary>
        public const int KamikazeBonusPerDestroyedCell = 6;

        // ---- Sixth batch of modifier bonuses (player-authored brainstorm, see ModifierCatalog) ----

        /// <summary>Bridge (Pont): bonus per pre-existing group bridged together by this placement beyond the first (bridging 2 groups scores once, 3 groups twice, ...).</summary>
        public const int PontBonusPerBridge = 15;

        /// <summary>Encirclement (Encerclement): bonus per group cell whose 8 surrounding tiles are all filled OR off the edge of the grid — softer than Fortress, which never gives edge/corner cells any credit.</summary>
        public const int EncerclementBonusPerCell = 6;

        /// <summary>Sealer (Boucher): bonus per pre-existing tile that this placement itself causes to become "encircled" (see Encerclement) — i.e. this placement fills the one missing neighbor that was keeping it from qualifying.</summary>
        public const int BoucherBonusPerCell = 10;

        /// <summary>Big Family (Grosse Famille): flat bonus when this placement's color exists in exactly one connected group on the whole board — no other same-color cell anywhere else.</summary>
        public const int GrosseFamilleBonus = 15;

        /// <summary>Repetition: flat bonus when this piece is the same shape as the immediately previous placement this round.</summary>
        public const int RepetitionBonus = 10;

        /// <summary>Color Switch (Alternance des pièces): flat bonus when this piece's color differs from the immediately previous placement's color this round — the piece-to-piece sibling of the existing line-level "Alternation" modifier.</summary>
        public const int AlternancePiecesBonus = 10;

        /// <summary>Combo: multiplies this placement's ENTIRE total score (see PlacementResult.ComboMultiplier) when the immediately previous placement this round cleared a line — the only modifier that's a true multiplier rather than a flat/per-cell bonus, on explicit request.</summary>
        public const int ComboMultiplierFactor = 2;

        /// <summary>Precision: bonus per placed cell when EVERY one of this placement's own cells has at least one pre-existing filled orthogonal neighbor.</summary>
        public const int PrecisionBonusPerCell = 5;

        /// <summary>Overcrowding (Surpopulation): bonus per placed cell when EVERY one of this placement's own cells has at least 2 pre-existing filled orthogonal neighbors — a stricter sibling of Precision.</summary>
        public const int SurpopulationBonusPerCell = 8;

        /// <summary>Minimalist (Minimaliste): flat bonus when this placement's whole footprint touches EXACTLY one distinct pre-existing filled cell, total — the "just barely touching" middle ground between Îlot (zero) and Precision (one or more, per cell).</summary>
        public const int MinimalisteBonus = 12;
    }
}
