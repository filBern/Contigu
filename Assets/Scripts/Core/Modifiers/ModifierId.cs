namespace Contigu.Core
{
    /// <summary>
    /// Identifies one persistent "modifier" (Joker-like passive, spec extension
    /// from the modifier brainstorm list) that a player can hold. See
    /// <see cref="ModifierCatalog"/> for the effect of each.
    /// </summary>
    public enum ModifierId
    {
        Prisme,
        Chaine,
        MegaChaine,
        Forteresse,
        Prisonnier,
        Architecte,
        Collectionneur,
        Tricolore,
        Complementaire,
        Ilot,
        Couronne,
        Carrefour,
        Macon,
        Demolisseur,

        // ---- Second batch (16 more, from the same brainstorm list) ----
        // CoeurDePierre, DiagonaleVerrouillee and SansDoublon removed (on
        // explicit request — locked-cell/boss-round mechanics read as too
        // abstract for too long before a player could act on them). Symetrie
        // removed separately (on explicit request — unclear, hard to trigger).
        CercleChromatique,
        Monochrome,
        Contraste,
        Degrade,
        Emmitouflee,
        Jardinier,
        ArcEnCiel,
        Alternance,
        Palindrome,
        Gradient,
        Bloc,
        MonochromeLigne,

        // ---- Third batch (basic per-color / per-shape modifiers, on explicit request) ----
        DevotionCoral,
        DevotionTeal,
        DevotionViolet,
        DevotionLime,

        // ---- Fourth batch: hand-slot, piece-size and per-color-tile bonuses (on explicit request) ----
        SlotUn,
        SlotDeux,
        SlotTrois,
        GrandFormat,
        HorsNorme,
        EclatCoral,
        EclatTeal,
        EclatViolet,
        EclatLime,

        // ---- Fifth batch (7 more, on explicit request — originally 8, Imminent later removed) ----
        Diagonale,
        Nid,
        Solitaire,
        EspaceLibre,
        Rafale,
        PetitFormat,
        Fraicheur,

        // ---- Sixth batch (11 more, from a player-authored brainstorm list —
        // Équilibriste, Longue série and a second "Solitaire" idea were
        // dropped, see README) ----
        Pont,
        Encerclement,
        Boucher,
        GrosseFamille,
        Repetition,
        AlternancePieces,
        Combo,
        Precision,
        Surpopulation,
        Minimaliste,
        Joker,

        // ---- Seventh batch: progressive modifiers that scale with a
        // running counter instead of firing at a fixed strength (on
        // explicit request) ----
        Densite,

        // ---- Eighth batch: Lueur-earning modifiers, adapted from 5
        // existing score modifiers (on explicit request: "il faut ajouter
        // quelques modifiers qui rapportent des lueur... adapter [les
        // modifiers qu'on a déjà] en version bonus lueur") ----
        ArcEnCielLueur,
        AlternanceLueur,
        MonochromeLigneLueur,
        CollectionneurLueur,
        RepetitionLueur,

        // ---- Ninth batch (on explicit request) — 3 flat, unconditional
        // "+Mult" modifiers (a genuine ADDITIVE mult pool, see
        // PlacementResult.AdditiveMultBonus, distinct from every "xN"
        // ModifierMultiplier modifier above); a new "+pts" counterpart for
        // each of the 10 Forme* shapes (Devotion/Éclat already covered
        // this split for colors, Forme* only had one version — see the
        // curation-pass note below, these 10 no longer exist as such);
        // Solidarite (+N Mult scaling with total modifiers held); Copieur
        // (copies whichever modifier was bought immediately before it); a
        // risk/reward +5 mult with a chance to be lost at round end; a
        // mult bonus scaling with upgraded cards in the deck; a decaying
        // flat points bonus; and a flat points bonus scaling with total
        // deck size. See ModifierCatalog for each one's exact effect.
        MultUn,
        MultDeux,
        MultQuatre,
        Solidarite,
        Copieur,
        MultCinqRisque,
        CartesEnchantees,
        Epuisement,
        Multitude,

        // ---- Tenth batch (on explicit request) — a mult bonus scaling
        // with how many special (trait-carrying) pieces have been PLAYED
        // this run, CartesEnchantees' "played" counterpart to its own
        // "currently in deck" count.
        Experience,

        // ---- Eleventh batch: curation pass (on explicit request — "que
        // me propose tu pour faire passer le jeu à un state supérieur" ->
        // "attaquons celui la") — replaces the 10 per-SHAPE FormeX
        // Specialist ids (third batch, above) and their 10 FormeXPoints
        // "+pts" siblings (ninth batch) with 3 per-SIZE-TIER pairs, grouped
        // by the placed piece's own cell count: Petit (<=2 cells: Single,
        // Domino H/V), Moyen (exactly 3: the 3 Trominoes), Grand (>=4
        // cells: Square, L/T/S-Tetromino). 20 near-identical "xN/+pts if
        // you play THIS EXACT shape" modifiers diluted the shop pool for a
        // fairly minor axis (which of 10 shapes you happen to place)
        // compared to color (Devotion/Éclat, only 4 values, kept as-is).
        // Same ScoringConstants.FormeSpecialistMultiplier/
        // FormeGlowBonusPerCell values as every one of the 20 modifiers
        // this replaces — a pure catalog-size reduction, not a numeric
        // rebalance.
        FormatPetitSpecialiste,
        FormatMoyenSpecialiste,
        FormatGrandSpecialiste,
        FormatPetitGlow,
        FormatMoyenGlow,
        FormatGrandGlow
    }
}
