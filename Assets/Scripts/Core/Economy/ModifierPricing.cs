namespace Contigu.Core
{
    /// <summary>
    /// Per-modifier Lueur price in the shop. Every price returned is in
    /// [4, 10] and is a base price — RunManager.GetBlisterSlotPrice still
    /// applies this visit's usual escalation on top (see
    /// EconomyConstants.ShopPriceEscalationPerPurchase), same as
    /// GetUpgradeSlotPrice already varies its own base price by upgrade pool.
    ///
    /// Kept as a lookup table separate from ModifierDefinition so pricing
    /// stays a standalone, easily re-tunable concern. The switch below is
    /// meant to be exhaustive — every entry in ModifierCatalog.All must have
    /// a case here — enforced by
    /// ModifierPricingTests.GetPrice_CoversEveryCatalogEntry_WithAValidPrice,
    /// not the compiler, since C# can't require switch exhaustiveness over an
    /// enum. A price never actually falls through to the `default` case in
    /// practice; it exists only so a modifier missing its own case fails that
    /// test loudly (price -1, outside [4, 10]) instead of silently reusing
    /// some other modifier's price.
    ///
    /// Rough rubric used throughout: 4-5 for a common/easy trigger with a
    /// modest payout; 6-7 for a solid x2 under a moderately common
    /// condition, or a bigger flat bonus under a harder one; 8 for x3
    /// multipliers, stacking per-line multipliers, or a strong unconditional
    /// effect; 9-10 for the rarest/most powerful — permanent effects
    /// (Gradient), extremely hard triggers with a huge payout (Cercle
    /// Chromatique), and the strongest run-long engine pieces (Copieur,
    /// Mult +4).
    /// </summary>
    public static class ModifierPricing
    {
        public static int GetPrice(ModifierId id)
        {
            switch (id)
            {
                case ModifierId.Prisme: return 7;
                case ModifierId.Chaine: return 5;
                case ModifierId.MegaChaine: return 8;
                case ModifierId.Forteresse: return 6;
                case ModifierId.Prisonnier: return 5;
                case ModifierId.Architecte: return 5;
                case ModifierId.Collectionneur: return 6;
                case ModifierId.Tricolore: return 5;
                case ModifierId.Complementaire: return 6;
                case ModifierId.Ilot: return 6;
                case ModifierId.Couronne: return 4;
                case ModifierId.Carrefour: return 7;
                case ModifierId.Macon: return 4;
                case ModifierId.Demolisseur: return 8;
                case ModifierId.CercleChromatique: return 9;
                case ModifierId.Monochrome: return 4;
                case ModifierId.Contraste: return 5;
                case ModifierId.Degrade: return 5;
                case ModifierId.Emmitouflee: return 6;
                case ModifierId.Jardinier: return 5;

                case ModifierId.ArcEnCiel: return 7;
                case ModifierId.Alternance: return 7;
                case ModifierId.Palindrome: return 7;
                case ModifierId.Gradient: return 10; // permanent, never resets
                case ModifierId.Bloc: return 6;
                case ModifierId.MonochromeLigne: return 7;

                // ---- Devotion (per-color xN, fires ~1 in 4 placements) ----
                case ModifierId.DevotionCoral: return 6;
                case ModifierId.DevotionTeal: return 6;
                case ModifierId.DevotionViolet: return 6;
                case ModifierId.DevotionLime: return 6;

                case ModifierId.SlotUn: return 7; // xN on the ENTIRE score, ~1/3 of placements
                case ModifierId.SlotDeux: return 7;
                case ModifierId.SlotTrois: return 7;
                case ModifierId.GrandFormat: return 5;
                case ModifierId.HorsNorme: return 5;
                case ModifierId.EclatCoral: return 5;
                case ModifierId.EclatTeal: return 5;
                case ModifierId.EclatViolet: return 5;
                case ModifierId.EclatLime: return 5;

                case ModifierId.Diagonale: return 5;
                case ModifierId.Nid: return 4;
                case ModifierId.Solitaire: return 5;
                case ModifierId.EspaceLibre: return 5;
                case ModifierId.Rafale: return 8; // x3, needs 2 clears in a row
                case ModifierId.PetitFormat: return 4;
                case ModifierId.Fraicheur: return 5;

                case ModifierId.Pont: return 7;
                case ModifierId.Encerclement: return 6;
                case ModifierId.Boucher: return 7;
                case ModifierId.GrosseFamille: return 5;
                case ModifierId.Repetition: return 8; // progressive, unbounded
                case ModifierId.AlternancePieces: return 5;
                case ModifierId.Combo: return 7; // xN on the ENTIRE score
                case ModifierId.Precision: return 5;
                case ModifierId.Surpopulation: return 6;
                case ModifierId.Minimaliste: return 6;
                case ModifierId.Joker: return 7;

                case ModifierId.Densite: return 8;

                case ModifierId.ArcEnCielLueur: return 5;
                case ModifierId.AlternanceLueur: return 5;
                case ModifierId.MonochromeLigneLueur: return 5;
                case ModifierId.CollectionneurLueur: return 4;
                case ModifierId.RepetitionLueur: return 4;

                case ModifierId.MultUn: return 5; // +1 Mult, unconditional
                case ModifierId.MultDeux: return 7; // +2 Mult, unconditional
                case ModifierId.MultQuatre: return 10; // +4 Mult, unconditional — the strongest flat effect in the game
                case ModifierId.Solidarite: return 8; // scales with total modifiers held
                case ModifierId.Copieur: return 9; // copies any modifier already bought — huge flexibility
                case ModifierId.MultCinqRisque: return 7; // +5 Mult, but can be lost
                case ModifierId.CartesEnchantees: return 8; // scales with upgraded deck cards
                case ModifierId.Epuisement: return 7; // strong early burst, decays away
                case ModifierId.Multitude: return 6;

                case ModifierId.Experience: return 8; // scales with special pieces played

                // Format* size tiers: Petit/Moyen each cover 3 of the 10
                // catalog shapes (~30% of placements, Devotion's own "~1 in
                // 4" bracket, priced the same); Grand covers 4 shapes
                // (~40%, priced one step above).
                case ModifierId.FormatPetitSpecialiste: return 6;
                case ModifierId.FormatMoyenSpecialiste: return 6;
                case ModifierId.FormatGrandSpecialiste: return 7;
                case ModifierId.FormatPetitGlow: return 5;
                case ModifierId.FormatMoyenGlow: return 5;
                case ModifierId.FormatGrandGlow: return 6;

                case ModifierId.Pair: return 5; // x2, fires on ~half of placements
                case ModifierId.Impair: return 5; // x2, fires on ~half of placements

                case ModifierId.Polyvalence: return 8; // scales with modifier category spread, up to +6 Mult at a full build
                case ModifierId.RenfortJoker: return 7; // strong but does nothing without Joker combat pieces already owned
                case ModifierId.Arsenal: return 8; // scales with distinct Joker combat kinds in deck, up to +5 Mult

                case ModifierId.CollectionChromatique: return 8; // needs 2 other modifiers held PER color to scale at all
                case ModifierId.Cadence: return 6; // x2, but needs 2 other specific modifiers held to ever fire
                case ModifierId.Echo: return 9; // doubles whatever sits to its left — build-order dependent, potentially huge
                case ModifierId.Siphon: return 6; // smaller fraction than Sangsue, but fires alongside every combat kind

                default:
                    return -1;
            }
        }
    }
}
