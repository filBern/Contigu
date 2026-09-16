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

        /// <summary>Multiplier contributed by EACH matching tinted cell in a placement's group — two tinted cells in the same group stack to x4, three to x8, and so on.</summary>
        public const int TintedMatchMultiplier = 2;

        /// <summary>Multiplier applied to a placement's whole group bonus when the group contains a multiplier-zone cell.</summary>
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

        /// <summary>Trou dans la grille: bonus per group cell orthogonally adjacent to a locked cell.</summary>
        public const int TrouBonusPerCell = 10;

        /// <summary>Carrefour: bonus per group cell whose 4 cardinal neighbors are filled with at least 2 colors different from BOTH each other and the cell's own color.</summary>
        public const int CarrefourBonusPerCell = 12;

        /// <summary>Maçon: flat bonus for a placement that clears no line/column at all.</summary>
        public const int MaconBonus = 5;

        /// <summary>Démolisseur: bonus per line, only once at least this many rows/columns clear simultaneously.</summary>
        public const int DemolisseurBonusPerLine = 15;
        public const int DemolisseurMinLines = 2;
    }
}
