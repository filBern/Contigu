namespace Contigu.Core
{
    /// <summary>
    /// Identifies one persistent passive modifier that a player can hold. See
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

        DevotionCoral,
        DevotionTeal,
        DevotionViolet,
        DevotionLime,

        SlotUn,
        SlotDeux,
        SlotTrois,
        GrandFormat,
        HorsNorme,
        EclatCoral,
        EclatTeal,
        EclatViolet,
        EclatLime,

        Diagonale,
        Nid,
        Solitaire,
        EspaceLibre,
        Rafale,
        PetitFormat,
        Fraicheur,

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

        // Progressive modifiers that scale with a running counter instead
        // of firing at a fixed strength.
        Densite,

        // Lueur-earning modifiers, each adapted from an existing score
        // modifier, paying Lueur instead of points/a multiplier.
        ArcEnCielLueur,
        AlternanceLueur,
        MonochromeLigneLueur,
        CollectionneurLueur,
        RepetitionLueur,

        // 3 flat, unconditional "+Mult" modifiers (a genuine additive mult
        // pool, see PlacementResult.AdditiveMultBonus, distinct from every
        // "xN" ModifierMultiplier modifier above). See ModifierCatalog for
        // each one's exact effect.
        MultUn,
        MultDeux,
        MultQuatre,
        Solidarite,
        Copieur,
        MultCinqRisque,
        CartesEnchantees,
        Epuisement,
        Multitude,

        // A mult bonus scaling with how many special (trait-carrying)
        // pieces have been played this run — CartesEnchantees' "played"
        // counterpart to its own "currently in deck" count.
        Experience,

        // 3 per-size-tier pairs, grouped by the placed piece's own cell
        // count: Petit (<=2 cells: Single, Domino H/V), Moyen (exactly 3:
        // the 3 Trominoes), Grand (>=4 cells: Square, L/T/S-Tetromino).
        FormatPetitSpecialiste,
        FormatMoyenSpecialiste,
        FormatGrandSpecialiste,
        FormatPetitGlow,
        FormatMoyenGlow,
        FormatGrandGlow,

        // A pair keyed on the scored group's total cell count being even
        // or odd, same xN-multiplier shape as Architecte/Îlot (a one-shot
        // boolean condition, not a per-cell scaling bonus).
        Pair,
        Impair,

        // Modifiers that reward owning other specific things (a matching
        // Devotion/Éclat pair, a spread of modifier categories, a spread
        // of Joker combat trait kinds) instead of each one scoring in
        // isolation off its own fixed condition. The Devotion/Éclat
        // pairing bonus needed no new ModifierId at all — see
        // GridManager.ApplyDevotionEclatPairBonus.
        Polyvalence,
        RenfortJoker,
        Arsenal,

        /// <summary>+Mult per color for which BOTH Devotion and Éclat are held — the "trio" idea extended practically, since there's no natural 3rd per-color modifier to chase yet (see ModifierDefinition.CollectionChromatique).</summary>
        CollectionChromatique,

        /// <summary>Ties the group-parity pair (Pair/Impair) to the Format* size tiers — xN Mult when THIS placement satisfies a held parity condition AND a held Format tier condition at once (see GridManager.ApplyCadence).</summary>
        Cadence,

        /// <summary>Replays whatever modifier sits immediately to its own left in the player's held order, for this placement only — "as if holding a second copy of it" (see GridManager.ApplyEcho). Never chains into another Écho.</summary>
        Echo,

        /// <summary>Sangsue's own Lueur siphon, generalized to fire alongside ANY Joker combat trait kind, not just Sangsue itself (see RunManager.ApplyJokerCombatOrDefaultDamage) — stacks independently if Sangsue is also held and also fires.</summary>
        Siphon
    }
}
