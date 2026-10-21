using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The shop: offers, prices, purchases, upgrade resolution and moving to the next round.</summary>
    public sealed partial class RunManager
    {
        /// <summary>Rolls every slot fresh — called once, the instant the shop opens (see EvaluateRoundEnd).</summary>
        private void OpenShop()
        {
            _purchasesThisVisit = 0;
            _rerollsThisVisit = 0;
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            PendingMasteryOptions = System.Array.Empty<MasteryOption>();
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                _blisterSlots[i] = RollBlisterSlot();
            }
            for (int i = 0; i < _upgradeSlots.Length; i++)
            {
                _upgradeSlots[i] = RollUpgradeSlot();
            }
        }

        /// <summary>
        /// False for a fertilizer that could never fire in this run: with only 2 seed colors (Orchard), the 4-color Prism and the exactly-3-color
        /// Triad Bloom are impossible, a color fertilizer of a missing color has nothing to read, and Companion Bloom needs its own pair.
        /// </summary>
        public bool IsModifierUsableThisRun(ModifierId id)
        {
            if (!IsNewModifierUsable(id))
            {
                return false;
            }
            var colors = PieceColorUtility.BaseColors;
            if (colors.Count >= PieceColorUtility.AllBaseColors.Count)
            {
                return true;
            }
            switch (id)
            {
                case ModifierId.Prisme:
                case ModifierId.Tricolore:
                    return false;
                case ModifierId.Complementaire:
                    return GridManager.HasComplementaryPair(colors);
                case ModifierId.DevotionCoral:
                case ModifierId.EclatCoral:
                    return PieceColorUtility.IsActive(PieceColor.Coral);
                case ModifierId.DevotionTeal:
                case ModifierId.EclatTeal:
                    return PieceColorUtility.IsActive(PieceColor.Teal);
                case ModifierId.DevotionViolet:
                case ModifierId.EclatViolet:
                    return PieceColorUtility.IsActive(PieceColor.Violet);
                case ModifierId.DevotionLime:
                case ModifierId.EclatLime:
                    return PieceColorUtility.IsActive(PieceColor.Lime);
                default:
                    return true;
            }
        }

        /// <summary>
        /// One "Blister" slot's roll - a single flat weighted bag holding
        /// every not-yet-held/not-already-offered-this-visit modifier
        /// (uniform weight, matching the Common upgrade-rarity weight)
        /// alongside every eligible upgrade (its own rarity weight, see
        /// UpgradeRarityUtility, multiplied by
        /// EconomyConstants.BlisterUpgradeWeightMultiplier). An upgrade
        /// needing a prerequisite the player doesn't meet yet (Modifier
        /// Upgrade with no modifiers owned, Random Modifier already at the
        /// cap) is excluded from the bag entirely - unlike a Casino slot, a
        /// Blister slot's exact identity is always visible, so an
        /// obviously-dead card would just read as broken.
        /// </summary>
        private ShopSlot RollBlisterSlot()
        {
            var modifierCandidates = new List<ModifierDefinition>();
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var candidate = ModifierCatalog.All[i];
                if (_activeModifiers.Contains(candidate.Id) || IsModifierAlreadyOfferedInBlister(candidate.Id) || !IsModifierUsableThisRun(candidate.Id))
                {
                    continue;
                }
                modifierCandidates.Add(candidate);
            }
            var upgradeCandidates = new List<UpgradeDefinition>();
            for (int i = 0; i < UpgradeCatalog.All.Length; i++)
            {
                var candidate = UpgradeCatalog.All[i];
                if (!IsUpgradeEligibleForOffer(candidate) || IsUpgradeAlreadyOfferedInBlister(candidate.Id))
                {
                    continue;
                }
                upgradeCandidates.Add(candidate);
            }

            const int ModifierBlisterWeight = 8; // matches UpgradeRarityUtility.GetDraftWeight(Common)
            int totalWeight = modifierCandidates.Count * ModifierBlisterWeight;
            for (int i = 0; i < upgradeCandidates.Count; i++)
            {
                totalWeight += UpgradeRarityUtility.GetDraftWeight(upgradeCandidates[i].Rarity) * EconomyConstants.BlisterUpgradeWeightMultiplier;
            }
            if (totalWeight <= 0)
            {
                // Only possible once held+offered modifiers AND every
                // eligible upgrade together cover both whole catalogs —
                // extremely unlikely with 70+/21+ entries, but stay
                // defensive rather than throw.
                for (int i = 0; i < ModifierCatalog.All.Length; i++)
                {
                    if (!_activeModifiers.Contains(ModifierCatalog.All[i].Id) && IsModifierUsableThisRun(ModifierCatalog.All[i].Id))
                    {
                        return ShopSlot.ForModifier(ModifierCatalog.All[i].Id);
                    }
                }
                return ShopSlot.ForModifier(ModifierCatalog.All[0].Id);
            }

            int roll = _rng.Next(totalWeight);
            int cumulative = 0;
            for (int i = 0; i < modifierCandidates.Count; i++)
            {
                cumulative += ModifierBlisterWeight;
                if (roll < cumulative)
                {
                    return ShopSlot.ForModifier(modifierCandidates[i].Id);
                }
            }
            for (int i = 0; i < upgradeCandidates.Count; i++)
            {
                cumulative += UpgradeRarityUtility.GetDraftWeight(upgradeCandidates[i].Rarity) * EconomyConstants.BlisterUpgradeWeightMultiplier;
                if (roll < cumulative)
                {
                    return ShopSlot.ForUpgrade(upgradeCandidates[i]);
                }
            }
            // Shouldn't happen given roll < totalWeight computed the same
            // way, but stay defensive rather than fall off the end.
            return ShopSlot.ForUpgrade(upgradeCandidates[upgradeCandidates.Count - 1]);
        }

        private bool IsModifierAlreadyOfferedInBlister(ModifierId id)
        {
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                if (_blisterSlots[i] != null && _blisterSlots[i].Kind == ShopSlotKind.Modifier && _blisterSlots[i].ModifierId == id)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsUpgradeAlreadyOfferedInBlister(UpgradeId id)
        {
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                if (_blisterSlots[i] != null && _blisterSlots[i].Kind == ShopSlotKind.Upgrade && _blisterSlots[i].HiddenUpgrade.Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether <paramref name="candidate"/> could actually be applied right now - see RollBlisterSlot's own doc comment for why this only matters for Blister, not Casino.</summary>
        private bool IsUpgradeEligibleForOffer(UpgradeDefinition candidate)
        {
            if (candidate.Id == UpgradeId.RandomModifier && _activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            if (candidate.Id == UpgradeId.ModifierUpgrade && _activeModifiers.Count == 0)
            {
                return false;
            }
            return true;
        }

        private ShopSlot RollUpgradeSlot()
        {
            int roll = _rng.Next(100);
            UpgradePool pool;
            int modifierCutoff = EconomyConstants.ModifierUpgradePoolChancePercent;
            int masteryCutoff = modifierCutoff + EconomyConstants.MasteryUpgradePoolChancePercent;
            int bankCutoff = masteryCutoff + EconomyConstants.BankUpgradePoolChancePercent;
            if (roll < modifierCutoff)
            {
                pool = UpgradePool.Modifier;
            }
            else if (roll < masteryCutoff)
            {
                pool = UpgradePool.Mastery;
            }
            else if (roll < bankCutoff)
            {
                pool = UpgradePool.Bank;
            }
            else
            {
                pool = UpgradePool.Grid;
            }
            var upgrade = Upgrades.RollFromPool(pool);
            return ShopSlot.ForUpgrade(upgrade);
        }

        /// <summary>Current Lueur price of Blister slot <paramref name="index"/> - a Modifier-kind slot varies per modifier (see ModifierPricing); an Upgrade-kind slot uses the same Bank/Grid base price as a Casino slot (see GetUpgradeSlotPrice). Includes this visit's escalation (see EconomyConstants.ShopPriceEscalationPerPurchase).</summary>
        public int GetBlisterSlotPrice(int index)
        {
            if (index < 0 || index >= _blisterSlots.Length || _blisterSlots[index] == null)
            {
                return 0;
            }
            var slot = _blisterSlots[index];
            int basePrice = slot.Kind == ShopSlotKind.Modifier
                ? ModifierPricing.GetPrice(slot.ModifierId)
                : (slot.Pool == UpgradePool.Grid ? EconomyConstants.GridUpgradeShopBasePrice : EconomyConstants.BankUpgradeShopBasePrice);
            return ComputePrice(basePrice);
        }

        /// <summary>Current Lueur price of upgrade slot <paramref name="index"/> - Grid-pool slots cost more than Bank-pool ones, including this visit's escalation. Mastery-pool slots share Bank's (lower) price.</summary>
        public int GetUpgradeSlotPrice(int index)
        {
            if (index < 0 || index >= _upgradeSlots.Length || _upgradeSlots[index] == null)
            {
                return 0;
            }
            int basePrice = _upgradeSlots[index].Pool == UpgradePool.Grid
                ? EconomyConstants.GridUpgradeShopBasePrice
                : EconomyConstants.BankUpgradeShopBasePrice;
            return ComputePrice(basePrice);
        }

        public int GetRerollPrice()
        {
            return Mathf.RoundToInt(EconomyConstants.ShopRerollBasePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _rerollsThisVisit));
        }

        private int ComputePrice(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _purchasesThisVisit));
        }

        /// <summary>
        /// Buys Blister slot <paramref name="index"/> outright - a
        /// Modifier-kind slot applies immediately; an Upgrade-kind slot goes
        /// through the exact same resolution as a Casino purchase (see
        /// ApplyPurchasedUpgrade/BuyUpgradeSlot). Fails (no charge, no state
        /// change) if the shop isn't open, the slot is invalid/already
        /// bought, the player can't afford it, or (Modifier-kind only)
        /// they're already at EconomyConstants.MaxActiveModifiers.
        /// </summary>
        /// </summary>
        public bool BuyBlisterSlot(int index)
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            if (index < 0 || index >= _blisterSlots.Length || _blisterSlots[index] == null || _blisterSlots[index].Purchased)
            {
                return false;
            }
            var slot = _blisterSlots[index];

            if (slot.Kind == ShopSlotKind.Modifier)
            {
                if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
                {
                    return false;
                }
                int price = GetBlisterSlotPrice(index);
                if (Lueur < price)
                {
                    return false;
                }

                Lueur -= price;
                _purchasesThisVisit++;
                slot.Purchased = true;

                // "Mimic" (Copieur) isn't a real modifier of its own - buying it
                // adds another copy of whichever modifier was purchased immediately
                // before it instead. A no-op (still costs Lueur, still marks the
                // slot sold) if nothing has been purchased yet this run.
                // _lastPurchasedModifierId only ever tracks a real purchase, never
                // Copieur itself, so buying several Mimics in a row all copy the
                // same underlying modifier rather than chaining off each other.
                if (slot.ModifierId == ModifierId.Copieur)
                {
                    if (_lastPurchasedModifierId.HasValue)
                    {
                        AddActiveModifier(_lastPurchasedModifierId.Value);
                    }
                }
                else
                {
                    AddActiveModifier(slot.ModifierId);
                    _lastPurchasedModifierId = slot.ModifierId;
                }
                return true;
            }

            var hiddenUpgrade = slot.HiddenUpgrade;
            if (!IsUpgradeEligibleForOffer(hiddenUpgrade))
            {
                return false;
            }
            int upgradePrice = GetBlisterSlotPrice(index);
            if (Lueur < upgradePrice)
            {
                return false;
            }
            Lueur -= upgradePrice;
            _purchasesThisVisit++;
            slot.Purchased = true;
            return ApplyPurchasedUpgrade(hiddenUpgrade);
        }

        /// <summary>
        /// Buys Casino upgrade slot <paramref name="index"/> - the specific
        /// upgrade underneath (only its <see cref="UpgradePool"/> was ever
        /// shown) gets revealed as <see cref="PendingUpgrade"/> (see
        /// ApplyPurchasedUpgrade). Fails (no charge) under the same
        /// conditions as <see cref="BuyBlisterSlot"/> (minus the modifier
        /// cap, which doesn't apply to upgrades in general) except for
        /// "Random Modifier" specifically, which does respect the cap:
        /// buying it while already at
        /// EconomyConstants.MaxActiveModifiers still charges Lueur and
        /// grants nothing (see GrantRandomModifier).
        /// </summary>
        public bool BuyUpgradeSlot(int index)
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            if (index < 0 || index >= _upgradeSlots.Length || _upgradeSlots[index] == null || _upgradeSlots[index].Purchased)
            {
                return false;
            }
            var hiddenUpgrade = _upgradeSlots[index].HiddenUpgrade;
            if (!IsUpgradeEligibleForOffer(hiddenUpgrade))
            {
                return false;
            }
            int price = GetUpgradeSlotPrice(index);
            if (Lueur < price)
            {
                return false;
            }

            Lueur -= price;
            _purchasesThisVisit++;
            var slot = _upgradeSlots[index];
            slot.Purchased = true;
            return ApplyPurchasedUpgrade(slot.HiddenUpgrade);
        }

        /// <summary>
        /// Shared resolution for a just-paid-for upgrade purchase, whichever
        /// section it came from (Blister or Casino). A Bank-pool upgrade
        /// with no sub-choice (Joker) applies immediately and leaves
        /// PendingUpgrade null; one that needs a sub-choice
        /// (Replace/Dupliquer/Recolorer) or a Grid-pool upgrade leaves
        /// PendingUpgrade set until <see cref="ResolveUpgradeSubChoice"/>/
        /// <see cref="ResolveUpgradeTileChoice"/> finishes it.
        /// </summary>
        private bool ApplyPurchasedUpgrade(UpgradeDefinition upgrade)
        {
            // Special-cased ahead of the generic Grid-pool routing below: unlike every other Grid-pool
            // upgrade, this one has no deck tile to choose — see UpgradeDefinition.PermanentBonusSquare's
            // own doc comment.
            if (upgrade.Id == UpgradeId.PermanentBonusSquare)
            {
                ApplyPermanentBonusSquare();
                return true;
            }
            if (upgrade.Pool == UpgradePool.Grid)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTileCandidates = Upgrades.GetCandidateTilesFor(upgrade, Deck);
                return true;
            }

            // Random Piece is Bank pool with a sub-choice, like Replace/
            // Dupliquer/Recolorer below, but its candidates are freshly rolled
            // pieces rather than existing deck types - handled by its own
            // Pending*/Resolve* pair (see PendingUpgradePieceCandidates/
            // ResolveUpgradePieceChoice).
            if (upgrade.Id == UpgradeId.RandomPiece)
            {
                PendingUpgrade = upgrade;
                PendingUpgradePieceCandidates = Upgrades.GetCandidatePiecesFor(upgrade);
                return true;
            }

            // Modifier Upgrade is a third distinct Bank sub-choice shape - its
            // candidates are simply every slot the player already owns
            // (ActiveModifiers itself), so there's no Pending*Candidates list
            // to populate here (see ResolveModifierUpgradeChoice).
            if (upgrade.Id == UpgradeId.ModifierUpgrade)
            {
                PendingUpgrade = upgrade;
                return true;
            }

            // Piece/Color Mastery offer TWO random levelings and let the player pick one, rather than a
            // fully random result (see ResolveMasteryChoice).
            if (upgrade.Id == UpgradeId.PieceMastery || upgrade.Id == UpgradeId.ColorMastery)
            {
                PendingUpgrade = upgrade;
                PendingMasteryOptions = RollMasteryOptions(upgrade.Id == UpgradeId.PieceMastery);
                return true;
            }

            if (upgrade.RequiresSubChoice)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTypeCandidates = Upgrades.GetCandidateTypesFor(upgrade, Deck);
                return true;
            }

            // The remaining upgrades with RequiresSubChoice false apply through
            // their own dedicated path (ApplyJoker / GrantRandomModifier /
            // GrantShapeMastery / GrantColorMastery, not a generic Apply) so
            // the actual thing rolled can be surfaced for the reveal to show
            // (see UpgradeRevealView / ShapeCarouselView / ColorCarouselView)
            // instead of just naming the upgrade.
            if (upgrade.Id == UpgradeId.JokerPiece)
            {
                LastJokerShapeAdded = Upgrades.ApplyJoker(Deck, out var combatKind);
                LastJokerCombatKindAdded = combatKind;
            }
            else if (upgrade.Id == UpgradeId.PieceMastery)
            {
                LastShapeMasteryGranted = GrantShapeMastery();
            }
            else if (upgrade.Id == UpgradeId.ColorMastery)
            {
                LastColorMasteryGranted = GrantColorMastery();
            }
            else
            {
                LastRandomModifierGranted = GrantRandomModifier();
            }
            return true;
        }

        /// <summary>
        /// "Random Modifier" upgrade's actual effect - grants one uniformly
        /// random modifier the player doesn't already hold, from the same
        /// catalog RollBlisterSlot draws from, minus its "already offered
        /// this shop visit" exclusion. Updates _lastPurchasedModifierId the
        /// same way a direct modifier-slot purchase does. Returns null -
        /// grants nothing, though the Lueur already spent is not refunded -
        /// if the player is already at EconomyConstants.MaxActiveModifiers
        /// or already holds every modifier in the catalog.
        /// </summary>
        private ModifierId? GrantRandomModifier()
        {
            if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return null;
            }
            var available = new List<ModifierDefinition>(ModifierCatalog.All.Length);
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                if (!_activeModifiers.Contains(ModifierCatalog.All[i].Id) && IsModifierUsableThisRun(ModifierCatalog.All[i].Id))
                {
                    available.Add(ModifierCatalog.All[i]);
                }
            }
            if (available.Count == 0)
            {
                return null;
            }

            var picked = available[_rng.Next(available.Count)].Id;
            AddActiveModifier(picked);
            _lastPurchasedModifierId = picked;
            return picked;
        }

        /// <summary>
        /// "Piece Mastery": picks a random exact shape (uniform over all 8 -
        /// no exclusion, since a shape can always be leveled up further) and
        /// levels it up by a random amount from 1 to <see
        /// cref="EconomyConstants.MasteryUpgradeMaxLevelGain"/>. Always
        /// succeeds - there's no cap to hit.
        /// </summary>
        private ShapeId GrantShapeMastery()
        {
            var shapes = (ShapeId[])System.Enum.GetValues(typeof(ShapeId));
            var picked = shapes[_rng.Next(shapes.Length)];
            int gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain);
            _shapeMasteryLevels[picked] = GetShapeMasteryLevel(picked) + gain;
            return picked;
        }

        /// <summary>"Color Mastery": Piece Mastery's exact sibling - picks a random base color (PieceColorUtility.BaseColors, never Joker) and levels it up by the same random amount. Always succeeds.</summary>
        private PieceColor GrantColorMastery()
        {
            var colors = PieceColorUtility.BaseColors;
            var picked = colors[_rng.Next(colors.Count)];
            int gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain);
            _colorMasteryLevels[picked] = GetColorMasteryLevel(picked) + gain;
            return picked;
        }

        /// <summary>Two distinct random shapes (or base colors), each with its own rolled level gain, for Piece/Color Mastery's choice.</summary>
        private List<MasteryOption> RollMasteryOptions(bool shapes)
        {
            var options = new List<MasteryOption>();
            if (shapes)
            {
                var all = new List<ShapeId>((ShapeId[])System.Enum.GetValues(typeof(ShapeId)));
                for (int i = 0; i < 2 && all.Count > 0; i++)
                {
                    int pick = _rng.Next(all.Count);
                    options.Add(new MasteryOption { Shape = all[pick], Gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain) });
                    all.RemoveAt(pick);
                }
            }
            else
            {
                var all = new List<PieceColor>(PieceColorUtility.BaseColors);
                for (int i = 0; i < 2 && all.Count > 0; i++)
                {
                    int pick = _rng.Next(all.Count);
                    options.Add(new MasteryOption { Color = all[pick], Gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain) });
                    all.RemoveAt(pick);
                }
            }
            return options;
        }

        /// <summary>Resolves Piece/Color Mastery's pending choice: levels up the chosen option's shape/color by its offered gain and surfaces it through <see cref="LastShapeMasteryGranted"/>/<see cref="LastColorMasteryGranted"/>. No-op (false) if nothing is pending or the index is out of range.</summary>
        public bool ResolveMasteryChoice(int optionIndex)
        {
            if (PendingUpgrade == null || (PendingUpgrade.Id != UpgradeId.PieceMastery && PendingUpgrade.Id != UpgradeId.ColorMastery))
            {
                return false;
            }
            if (optionIndex < 0 || optionIndex >= PendingMasteryOptions.Count)
            {
                return false;
            }

            var option = PendingMasteryOptions[optionIndex];
            if (option.Shape.HasValue)
            {
                _shapeMasteryLevels[option.Shape.Value] = GetShapeMasteryLevel(option.Shape.Value) + option.Gain;
                LastShapeMasteryGranted = option.Shape;
            }
            else if (option.Color.HasValue)
            {
                _colorMasteryLevels[option.Color.Value] = GetColorMasteryLevel(option.Color.Value) + option.Gain;
                LastColorMasteryGranted = option.Color;
            }
            PendingUpgrade = null;
            PendingMasteryOptions = System.Array.Empty<MasteryOption>();
            return true;
        }

        /// <summary>Resolves a Bank-pool <see cref="PendingUpgrade"/> that needed a sub-choice (which piece type, and for Recolorer which target color, or for Replace which SECOND piece type to duplicate in its place). No-op (false) if nothing is pending or it's actually a Grid-pool upgrade.</summary>
        public bool ResolveUpgradeSubChoice(UpgradeSubChoice subChoice)
        {
            if (PendingUpgrade == null || PendingUpgrade.Pool != UpgradePool.Bank)
            {
                return false;
            }

            bool applied = Upgrades.Apply(PendingUpgrade, subChoice, Deck);
            PendingUpgrade = null;
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            return applied;
        }

        /// <summary>Resolves "Random Piece"'s sub-choice — <paramref name="candidateIndex"/> must index into <see cref="PendingUpgradePieceCandidates"/> (not validated beyond range here; the presentation layer only ever offers those). No-op (false) if nothing is pending or it's actually a different upgrade.</summary>
        public bool ResolveUpgradePieceChoice(int candidateIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.RandomPiece)
            {
                return false;
            }
            if (candidateIndex < 0 || candidateIndex >= PendingUpgradePieceCandidates.Count)
            {
                return false;
            }

            Upgrades.ApplyChosenPiece(PendingUpgradePieceCandidates[candidateIndex], Deck);
            PendingUpgrade = null;
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            return true;
        }

        /// <summary>Resolves "Modifier Upgrade"'s sub-choice — <paramref name="slotIndex"/> indexes directly into <see cref="ActiveModifiers"/> (not a separate Pending*Candidates list, since every owned modifier is eligible — see BuyUpgradeSlot). Raises that slot's level by one (see GetModifierLevel/ModifierLevelUtility); no-op (false) if nothing is pending, it's actually a different upgrade, or the index is out of range.</summary>
        public bool ResolveModifierUpgradeChoice(int slotIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.ModifierUpgrade)
            {
                return false;
            }
            if (!IsValidModifierIndex(slotIndex))
            {
                return false;
            }

            _modifierLevels[slotIndex]++;
            PendingUpgrade = null;
            return true;
        }

        /// <summary>Resolves a Grid-pool <see cref="PendingUpgrade"/> — <paramref name="chosenDeckIndices"/> must come from <see cref="PendingUpgradeTileCandidates"/> (not validated beyond that here; the presentation layer only ever offers those). No-op (false) if nothing is pending or it's actually a Bank-pool upgrade.</summary>
        public bool ResolveUpgradeTileChoice(IReadOnlyList<int> chosenDeckIndices)
        {
            if (PendingUpgrade == null || PendingUpgrade.Pool != UpgradePool.Grid)
            {
                return false;
            }

            bool applied = Upgrades.ApplyToChosenTiles(PendingUpgrade, chosenDeckIndices, Deck);
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            return applied;
        }

        /// <summary>
        /// Refreshes every Casino slot - purchased or not - with a new
        /// random offer. Never touches the Blister section: it already
        /// shows its exact contents up front, so a reroll there would just
        /// be "pay Lueur to see 3 different exact items," a very different
        /// kind of purchase from Casino's "pay Lueur to try your luck at the
        /// mystery box again." Buying a slot still permanently grants
        /// whatever it held (the upgrade is already applied by then) - this
        /// only replaces the slot offer itself, giving the player a fresh
        /// purchasable pick where a spent one used to sit. Costs Lueur (see
        /// GetRerollPrice), escalating only from further rerolls this same
        /// visit (see _rerollsThisVisit), not from slot purchases.
        /// </summary>
        public bool RerollShop()
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            int price = GetRerollPrice();
            if (Lueur < price)
            {
                return false;
            }

            Lueur -= price;
            _rerollsThisVisit++;
            for (int i = 0; i < _upgradeSlots.Length; i++)
            {
                _upgradeSlots[i] = RollUpgradeSlot();
            }
            return true;
        }

        /// <summary>Closes the shop and starts the next round. Only valid while the shop is open and nothing is pending a follow-up.</summary>
        public bool LeaveShop()
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            AdvanceRound();
            return true;
        }

        private void AdvanceRound()
        {
            CurrentRoundIndex++;
            StartRound();
        }
    }
}
