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
        TrouDansLaGrille,
        Carrefour,
        Macon,
        Demolisseur,

        // ---- Second batch (16 more, from the same brainstorm list) ----
        CoeurDePierre,
        CercleChromatique,
        DiagonaleVerrouillee,
        Monochrome,
        Contraste,
        Degrade,
        Emmitouflee,
        Jardinier,
        ArcEnCiel,
        Alternance,
        Symetrie,
        Palindrome,
        Gradient,
        SansDoublon,
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
        FormeSTetro
    }
}
