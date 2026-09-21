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
        Puriste,
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
        FormeSingle,
        FormeDomH,
        FormeDomV,
        FormeTriL,
        FormeTriIH,
        FormeTriIV,
        FormeSq2,
        FormeLTetro,
        FormeTTetro,
        FormeSTetro,

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
        Joker
    }
}
