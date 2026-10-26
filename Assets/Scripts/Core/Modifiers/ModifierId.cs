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

        /// <summary>Replays whatever modifier sits immediately before it in the player's held order, for this placement only — "as if holding a second copy of it" (see GridManager.ApplyEcho). Never chains into another Écho.</summary>
        Echo,

        /// <summary>Lueur siphon that fires alongside ANY Joker combat trait kind (see RunManager.ApplyJokerCombatOrDefaultDamage).</summary>
        Siphon,

        /// <summary>Grows the hand by one extra slot (see DeckManager.SetEffectiveHandSize/MaxHandSize), applied/reverted from RunManager.AddActiveModifier/RemoveActiveModifierAt rather than scored on placement like every other modifier.</summary>
        QuatriemeEmplacement,

        /// <summary>+Mult whenever this round's weather isn't calm (see RunManager.ApplyDeckStateModifierBonuses).</summary>
        Barometre,

        /// <summary>xN Mult while the weather is harsh (Frost, Heatwave or Hurricane).</summary>
        ChasseurTempete,

        /// <summary>Frost, Heatwave and Hurricane have no effect on the player.</summary>
        Impermeable,

        /// <summary>+Mult while any alive enemy has a lock or blight on the garden.</summary>
        Epines,

        /// <summary>+Mult per enemy already defeated this round.</summary>
        Exterminateur,

        /// <summary>+Mult per seed still left to play this round.</summary>
        Hate,

        /// <summary>+Mult per Shuffle still in the bank.</summary>
        Tourbillon,

        /// <summary>xN Mult when the placed piece covers one of the 4 corner plots.</summary>
        PierreAngulaire,

        /// <summary>+4 pts per tile of the seed that sits on the edge of the garden.</summary>
        Haie,

        /// <summary>x2 Mult when the seed puts a tile in the middle of the garden.</summary>
        Coeur,

        /// <summary>+3 Mult when the seed is a straight vertical piece (1 wide, 2 or more tall).</summary>
        Treillis,

        /// <summary>+3 Mult when the seed is a straight horizontal piece (1 tall, 2 or more wide).</summary>
        Sillon,

        /// <summary>+12 pts when the seed is a straight piece of 3 or more tiles.</summary>
        Haricot,

        /// <summary>+4 pts for each tile of another color touching the seed.</summary>
        Patchwork,

        /// <summary>+1 Mult per 3 cells in the scored group.</summary>
        Racines,

        /// <summary>+5 Mult when every plot touching the seed is already planted (or the edge).</summary>
        Emboite,

        /// <summary>+3 Mult when the seed touches 2 or more tiles and none of them shares its color.</summary>
        Mosaique,

        /// <summary>x2 Mult while two thirds of the garden or more is planted.</summary>
        Foule,

        /// <summary>+4 Mult when every color of the run is growing in the garden.</summary>
        Prairie,

        /// <summary>+8 pts per tile of the seed touching 3 or more planted plots.</summary>
        Guirlande,

        /// <summary>+2 Pollen when the seed touches 2 or more different colors.</summary>
        Pollinisateur,

        /// <summary>+1 Pollen per 3 cells in the scored group.</summary>
        Miel,

        /// <summary>+1 Pollen every time you plant a seed.</summary>
        Nectar,

        /// <summary>+6 Mult when one seed harvests a row and a column at once.</summary>
        MoissonCroisee,

        /// <summary>+3 Mult per line harvested.</summary>
        Faucheur,

        /// <summary>x3 Mult when the seed harvests the last planted tile in the garden.</summary>
        TableRase,

        /// <summary>+2 Pollen per line harvested.</summary>
        Dime,

        /// <summary>x2 Mult when the seed harvests at least one row.</summary>
        CultureRang,

        /// <summary>x2 Mult when the seed harvests at least one column.</summary>
        CultureColonne,

        /// <summary>+3 pts per tile harvested.</summary>
        Prime,

        /// <summary>+6 Mult when the seed breaks a frozen plot free.</summary>
        BriseGlace,

        /// <summary>+0.5 Mult per round already cleared this run.</summary>
        Saisons,

        /// <summary>+3 Mult while the enemies still have more than half of their total HP.</summary>
        Outsider,

        /// <summary>+6 Mult while the enemies are down to a quarter of their total HP or less.</summary>
        CoupFinal,

        /// <summary>x3 Mult on the last seed of the round.</summary>
        DerniereGraine,

        /// <summary>x2 Mult on the first seed of the round.</summary>
        PremiereGraine,

        /// <summary>+0.1 Mult per Pollen you hold.</summary>
        Tirelire,

        /// <summary>+0.05 Mult per seed in your bag.</summary>
        BanqueGraines,

        /// <summary>+1 Mult per enemy still alive.</summary>
        SousBois,

        /// <summary>+4 Mult while it rains (Rain, Snowfall, Sprinkler Malfunction).</summary>
        DanseDePluie,

        /// <summary>+4 Mult on Sunshine and Golden Hour days (and their indoor cousins).</summary>
        DanseDuSoleil,

        /// <summary>+4 Mult while the weather is Frost (or the Freezer Door Ajar).</summary>
        Rustique,

        /// <summary>+4 Mult in Wind and Hurricane weather (and their indoor cousins).</summary>
        CavalierVent,

        /// <summary>+6 Mult during the first 5 rounds.</summary>
        Matinal,

        /// <summary>+4 Mult from round 9 onwards.</summary>
        Marathonien,

        /// <summary>x2 Mult during boss rounds.</summary>
        Pourfendeur,

        /// <summary>+1 Pollen per Shuffle left when you clear a round.</summary>
        Tonneau,

        /// <summary>+1 Pollen per fertilizer held when you clear a round.</summary>
        Grange,

        /// <summary>+1 Mult per upgraded tile in the scored group.</summary>
        CharmedPatch,

        /// <summary>+3 Mult when the seed you plant carries an upgrade.</summary>
        Grafter,

        /// <summary>+5 Mult when the seed you plant carries 2 or more upgrades.</summary>
        TwinGraft,

        /// <summary>+1 Mult per different kind of upgrade in the garden.</summary>
        Botanist,

        /// <summary>+1 Mult per 2 upgraded tiles in the garden.</summary>
        Hothouse,

        /// <summary>+2 Mult per upgraded tile touching the seed you plant.</summary>
        GoodNeighbors,

        /// <summary>+2 Mult per upgraded tile harvested by this placement.</summary>
        Gleaner,

        /// <summary>Scoring-only placeholder for a modifier slot currently blighted by Blightmoth (see RunManager.EffectiveModifiers): it has no effect, never appears in the catalog or the player's own list.</summary>
        Blighted
    }
}
