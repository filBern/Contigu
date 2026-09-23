namespace Contigu.Core
{
    /// <summary>
    /// Per-modifier Lueur price in the shop, on explicit request ("à force de
    /// jouer, il faudrait que les modifiers ne soient pas tous le même prix,
    /// en fonction de leur rareté et de leur puissance (entre 4 et 10)") — up
    /// to now every modifier slot cost the same flat base price (formerly
    /// EconomyConstants.ModifierShopBasePrice, removed now that it's fully
    /// superseded by this per-modifier table) regardless of what it offered.
    /// Every price returned is in [4, 10] and is a BASE price — RunManager.
    /// GetModifierSlotPrice still applies this visit's usual escalation on
    /// top (see EconomyConstants.ShopPriceEscalationPerPurchase), same as
    /// GetUpgradeSlotPrice already varies its own base price by upgrade pool.
    ///
    /// Kept as a lookup table separate from ModifierDefinition (rather than a
    /// 5th constructor argument on all ~95 existing definitions) so pricing
    /// stays a standalone, easily re-tunable concern. The switch below is
    /// meant to be EXHAUSTIVE — every entry in ModifierCatalog.All must have
    /// a case here — enforced by
    /// ModifierPricingTests.GetPrice_CoversEveryCatalogEntry_WithAValidPrice,
    /// not the compiler, since C# can't require switch exhaustiveness over an
    /// enum. A price never actually falls through to the `default` case in
    /// practice; it exists only so a modifier missing its own case fails that
    /// test loudly (price -1, outside [4, 10]) instead of silently reusing
    /// some other modifier's price.
    ///
    /// Rough rubric used throughout (a design judgment call, not a strict
    /// formula, since this is a single-player game with no matchmaking
    /// balance at stake): 4-5 for a common/easy trigger with a modest payout;
    /// 6-7 for a solid x2 under a moderately common condition, or a bigger
    /// flat bonus under a harder one; 8 for x3 multipliers, stacking
    /// per-line multipliers, or a strong unconditional effect; 9-10 for the
    /// rarest/most powerful — permanent effects (Gradient), extremely hard
    /// triggers with a huge payout (Cercle Chromatique), and the strongest
    /// run-long engine pieces (Copieur, Mult +4).
    /// </summary>
    public static class ModifierPricing
    {
        public static int GetPrice(ModifierId id)
        {
            switch (id)
            {
                // ---- First batch (Couleurs/Voisinage/Connexions/Destruction/Roguelike) ----
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

                // ---- Line-pattern batch (6) ----
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

                // ---- Fourth batch: hand-slot, piece-size, per-color-tile ----
                case ModifierId.SlotUn: return 7; // xN on the ENTIRE score, ~1/3 of placements
                case ModifierId.SlotDeux: return 7;
                case ModifierId.SlotTrois: return 7;
                case ModifierId.GrandFormat: return 5;
                case ModifierId.HorsNorme: return 5;
                case ModifierId.EclatCoral: return 5;
                case ModifierId.EclatTeal: return 5;
                case ModifierId.EclatViolet: return 5;
                case ModifierId.EclatLime: return 5;

                // ---- Fifth batch ----
                case ModifierId.Diagonale: return 5;
                case ModifierId.Nid: return 4;
                case ModifierId.Solitaire: return 5;
                case ModifierId.EspaceLibre: return 5;
                case ModifierId.Rafale: return 8; // x3, needs 2 clears in a row
                case ModifierId.PetitFormat: return 4;
                case ModifierId.Fraicheur: return 5;

                // ---- Sixth batch ----
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

                // ---- Seventh batch: progressive engine pieces ----
                case ModifierId.Densite: return 8;

                // ---- Eighth batch: Lueur-earning (economy, not score) ----
                case ModifierId.ArcEnCielLueur: return 5;
                case ModifierId.AlternanceLueur: return 5;
                case ModifierId.MonochromeLigneLueur: return 5;
                case ModifierId.CollectionneurLueur: return 4;
                case ModifierId.RepetitionLueur: return 4;

                // ---- Ninth batch ----
                case ModifierId.MultUn: return 5; // +1 Mult, unconditional
                case ModifierId.MultDeux: return 7; // +2 Mult, unconditional
                case ModifierId.MultQuatre: return 10; // +4 Mult, unconditional — the strongest flat effect in the game
                case ModifierId.Solidarite: return 8; // scales with total modifiers held
                case ModifierId.Copieur: return 9; // copies any modifier already bought — huge flexibility
                case ModifierId.MultCinqRisque: return 7; // +5 Mult, but can be lost
                case ModifierId.CartesEnchantees: return 8; // scales with upgraded deck cards
                case ModifierId.Epuisement: return 7; // strong early burst, decays away
                case ModifierId.Multitude: return 6;

                // ---- Tenth batch ----
                case ModifierId.Experience: return 8; // scales with special pieces played

                // ---- Eleventh batch: Format* size tiers (curation pass,
                // replaces the 10 FormeX Specialist / 10 FormeXPoints Glow
                // entries above) — priced higher than the old per-shape
                // ones despite the exact same effect strength, because a
                // size TIER covers several shapes at once and so fires
                // several times more often: Petit/Moyen each cover 3 of the
                // 10 catalog shapes (~30% of placements under a uniform
                // draw, in Devotion's own "~1 in 4" bracket, so priced the
                // same as Devotion/Éclat); Grand covers 4 shapes (~40%,
                // the most frequent of the three, priced one step above).
                case ModifierId.FormatPetitSpecialiste: return 6;
                case ModifierId.FormatMoyenSpecialiste: return 6;
                case ModifierId.FormatGrandSpecialiste: return 7;
                case ModifierId.FormatPetitGlow: return 5;
                case ModifierId.FormatMoyenGlow: return 5;
                case ModifierId.FormatGrandGlow: return 6;

                default:
                    return -1;
            }
        }
    }
}
