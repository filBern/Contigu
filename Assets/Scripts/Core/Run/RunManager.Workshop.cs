using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The Workshop that replaced the Casino: packs (reveal a few options, keep one), Compost (thin the bag) and Forge (a choice of tile upgrades for any seed).</summary>
    public sealed partial class RunManager
    {
        /// <summary>Applies a just-paid Workshop pack or service: sets up whatever the player must now choose from.</summary>
        private bool ApplyWorkshopPurchase(UpgradeDefinition upgrade)
        {
            switch (upgrade.Id)
            {
                case UpgradeId.SeedPack:
                    PendingUpgrade = upgrade;
                    PendingUpgradePieceCandidates = Upgrades.GetCandidatePiecesFor(upgrade);
                    return true;
                case UpgradeId.SproutPack:
                    PendingUpgrade = upgrade;
                    PendingPackUpgrades = RollTileUpgradeOffers(EconomyConstants.SproutPackChoiceCount, null);
                    return true;
                case UpgradeId.FertilizerPack:
                    PendingUpgrade = upgrade;
                    PendingPackModifiers = RollPackModifiers(EconomyConstants.PackChoiceCount, preview: false);
                    return true;
                case UpgradeId.MasteryPack:
                    PendingUpgrade = upgrade;
                    PendingMasteryOptions = RollPackMasteryOptions(EconomyConstants.PackChoiceCount);
                    return true;
                case UpgradeId.Transplant:
                    PendingUpgrade = upgrade;
                    RollTransplantSources();
                    return true;
                case UpgradeId.Compost:
                    PendingUpgrade = upgrade;
                    PendingUpgradeTileCandidates = Deck.GetRandomTokenIndices(EconomyConstants.ShopTileCandidateCount, _rng);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>One movable upgrade of a seed: the seed's place in the bag and the upgrade's number on it.</summary>
        public struct TransplantSource
        {
            public int DeckIndex;
            public int TraitIndex;
        }

        /// <summary>Transplant, step 1: the upgrades that can be moved (null outside that step).</summary>
        public IReadOnlyList<TransplantSource> PendingTransplantSources { get; private set; } = System.Array.Empty<TransplantSource>();

        /// <summary>Transplant, step 2: the upgrade chosen in step 1 (-1 outside that step); the seeds that can receive it are in <see cref="PendingUpgradeTileCandidates"/>.</summary>
        public TransplantSource? PendingTransplantChosen { get; private set; }

        /// <summary>True when at least one seed of the bag carries an upgrade that can move.</summary>
        public bool HasTransplantableUpgrade()
        {
            var tokens = Deck.Deck;
            for (int i = 0; i < tokens.Count; i++)
            {
                for (int t = 0; t < tokens[i].TraitCount; t++)
                {
                    if (Deck.CanTransplantFrom(i, t)) { return true; }
                }
            }
            return false;
        }

        private void RollTransplantSources()
        {
            var all = new List<TransplantSource>();
            var tokens = Deck.Deck;
            for (int i = 0; i < tokens.Count; i++)
            {
                for (int t = 0; t < tokens[i].TraitCount; t++)
                {
                    if (Deck.CanTransplantFrom(i, t)) { all.Add(new TransplantSource { DeckIndex = i, TraitIndex = t }); }
                }
            }
            var picked = new List<TransplantSource>();
            while (picked.Count < EconomyConstants.TransplantChoiceCount && all.Count > 0)
            {
                int pick = _rng.Next(all.Count);
                picked.Add(all[pick]);
                all.RemoveAt(pick);
            }
            PendingTransplantSources = picked;
            PendingTransplantChosen = null;
        }

        /// <summary>Transplant, step 1: keeps upgrade number <paramref name="index"/> of <see cref="PendingTransplantSources"/>; the seeds that can receive it are then offered. False when no other seed has a free cell.</summary>
        public bool ResolveTransplantSource(int index)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.Transplant || PendingTransplantChosen.HasValue || index < 0 || index >= PendingTransplantSources.Count)
            {
                return false;
            }
            var chosen = PendingTransplantSources[index];
            var receivers = new List<int>();
            var tokens = Deck.Deck;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i != chosen.DeckIndex && DeckManager.FirstFreeCell(tokens[i]) >= 0) { receivers.Add(i); }
            }
            if (receivers.Count == 0)
            {
                return false;
            }
            var offered = new List<int>();
            while (offered.Count < EconomyConstants.TransplantChoiceCount && receivers.Count > 0)
            {
                int pick = _rng.Next(receivers.Count);
                offered.Add(receivers[pick]);
                receivers.RemoveAt(pick);
            }
            PendingTransplantChosen = chosen;
            PendingTransplantSources = System.Array.Empty<TransplantSource>();
            PendingUpgradeTileCandidates = offered;
            return true;
        }

        /// <summary>Transplant, step 2: moves the chosen upgrade onto the seed at deck position <paramref name="deckIndex"/> (one of <see cref="PendingUpgradeTileCandidates"/>).</summary>
        public bool ResolveTransplantTarget(int deckIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.Transplant || !PendingTransplantChosen.HasValue || !System.Linq.Enumerable.Contains(PendingUpgradeTileCandidates, deckIndex))
            {
                return false;
            }
            var source = PendingTransplantChosen.Value;
            if (!Deck.TransplantTrait(source.DeckIndex, source.TraitIndex, deckIndex))
            {
                return false;
            }
            ClearPendingTransplant();
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            return true;
        }

        private void ClearPendingTransplant()
        {
            PendingTransplantSources = System.Array.Empty<TransplantSource>();
            PendingTransplantChosen = null;
        }

        /// <summary>Forge: offers a few tile upgrades to pick from (see <see cref="PendingPackUpgrades"/>), then the seeds it goes on (a wide choice of the bag, see <see cref="PendingUpgradeTileCandidates"/>).</summary>
        private bool ApplyForge()
        {
            PendingUpgrade = UpgradeCatalog.Forge;
            PendingPackUpgrades = RollTileUpgradeOffers(EconomyConstants.ForgeChoiceCount, null);
            return true;
        }

        /// <summary>Distinct tile upgrades for a Sprout Pack or the Forge, rarity-weighted like any draft (nothing steers them toward the player's build). <paramref name="already"/> lists upgrades already on offer, which are not drawn again.</summary>
        private List<UpgradeDefinition> RollTileUpgradeOffers(int count, IReadOnlyList<UpgradeDefinition> already)
        {
            var picked = new List<UpgradeDefinition>();
            for (int attempt = 0; attempt < 120 && picked.Count < count; attempt++)
            {
                var rolled = Upgrades.RollFromPool(UpgradePool.Grid);
                if (rolled.Id != UpgradeId.PermanentBonusSquare && UpgradeSystem.TraitKindFor(rolled.Id).HasValue && IsUpgradeEligibleForOffer(rolled)
                    && !picked.Contains(rolled) && (already == null || !System.Linq.Enumerable.Contains(already, rolled)))
                {
                    picked.Add(rolled);
                }
            }
            return picked;
        }

        /// <summary>Distinct fertilizers the player does not hold and could use, for a Fertilizer Pack (uniform). With <paramref name="preview"/> it does not touch the run's random sequence's meaning: it only counts availability.</summary>
        private List<ModifierId> RollPackModifiers(int count, bool preview)
        {
            var available = new List<ModifierId>();
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var id = ModifierCatalog.All[i].Id;
                if (!_activeModifiers.Contains(id) && id != ModifierId.Copieur && IsModifierUsableThisRun(id))
                {
                    available.Add(id);
                }
            }
            if (preview)
            {
                return available.Count > 0 ? new List<ModifierId> { available[0] } : new List<ModifierId>();
            }
            var picked = new List<ModifierId>();
            while (picked.Count < count && available.Count > 0)
            {
                int pick = _rng.Next(available.Count);
                picked.Add(available[pick]);
                available.RemoveAt(pick);
            }
            return picked;
        }

        /// <summary>Distinct masteries (a seed shape or a seed color, each with its rolled level gain) for a Mastery Pack.</summary>
        private List<MasteryOption> RollPackMasteryOptions(int count)
        {
            var all = new List<MasteryOption>();
            foreach (ShapeId shape in System.Enum.GetValues(typeof(ShapeId)))
            {
                all.Add(new MasteryOption { Shape = shape, Gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain) });
            }
            foreach (var color in PieceColorUtility.BaseColors)
            {
                all.Add(new MasteryOption { Color = color, Gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain) });
            }
            var picked = new List<MasteryOption>();
            while (picked.Count < count && all.Count > 0)
            {
                int pick = _rng.Next(all.Count);
                picked.Add(all[pick]);
                all.RemoveAt(pick);
            }
            return picked;
        }

        /// <summary>Sprout Pack or Forge: keeps tile upgrade <paramref name="index"/> of <see cref="PendingPackUpgrades"/>; the player then picks the seed it goes on (the usual tile choice).</summary>
        public bool ResolvePackUpgradeChoice(int index)
        {
            if (PendingUpgrade == null || (PendingUpgrade.Id != UpgradeId.SproutPack && PendingUpgrade.Id != UpgradeId.Forge) || index < 0 || index >= PendingPackUpgrades.Count)
            {
                return false;
            }
            var chosen = PendingPackUpgrades[index];
            PendingPackUpgrades = System.Array.Empty<UpgradeDefinition>();
            PendingUpgrade = chosen;
            PendingUpgradeTileCandidates = Upgrades.GetCandidateTilesFor(chosen, Deck);
            return true;
        }

        /// <summary>Fertilizer Pack: keeps fertilizer <paramref name="index"/> of <see cref="PendingPackModifiers"/> for free.</summary>
        public bool ResolvePackModifierChoice(int index)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.FertilizerPack || index < 0 || index >= PendingPackModifiers.Count)
            {
                return false;
            }
            var id = PendingPackModifiers[index];
            PendingPackModifiers = System.Array.Empty<ModifierId>();
            PendingUpgrade = null;
            if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            AddActiveModifier(id);
            _lastPurchasedModifierId = id;
            return true;
        }

        /// <summary>Walks away from a Sprout or Fertilizer Pack without taking anything (the Pollen is not refunded).</summary>
        public bool SkipPack()
        {
            if (PendingUpgrade == null || (PendingUpgrade.Id != UpgradeId.SproutPack && PendingUpgrade.Id != UpgradeId.FertilizerPack && PendingUpgrade.Id != UpgradeId.Forge))
            {
                return false;
            }
            PendingUpgrade = null;
            PendingPackUpgrades = System.Array.Empty<UpgradeDefinition>();
            PendingPackModifiers = System.Array.Empty<ModifierId>();
            return true;
        }

        /// <summary>
        /// Skips whatever choice is pending (a pack's options, a seed to upgrade, a type to compost...) without taking anything.
        /// The Pollen already paid is not refunded, and the purchase counts as made (a skipped Compost still makes the next one dearer). False when nothing was pending.
        /// </summary>
        public bool AbandonPending()
        {
            if (PendingUpgrade == null)
            {
                return false;
            }
            // Skipping a Compost is as good as using it: the next one is dearer either way.
            if (PendingUpgrade.Id == UpgradeId.Compost)
            {
                _compostsBought++;
            }
            ClearPendingTransplant();
            PendingUpgrade = null;
            LastRandomModifierGranted = null; // never replay the reveal of an earlier purchase
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            PendingPackUpgrades = System.Array.Empty<UpgradeDefinition>();
            PendingPackModifiers = System.Array.Empty<ModifierId>();
            PendingMasteryOptions = System.Array.Empty<MasteryOption>();
            return true;
        }

        private bool IsPendingTileCandidate(int deckIndex)
        {
            for (int i = 0; i < PendingUpgradeTileCandidates.Count; i++)
            {
                if (PendingUpgradeTileCandidates[i] == deckIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Compost: removes the chosen seed (a deck position among <see cref="PendingUpgradeTileCandidates"/>) from the bag, never below the minimum bag size.</summary>
        public bool ResolveCompostChoice(int deckIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.Compost || !IsPendingTileCandidate(deckIndex))
            {
                return false;
            }
            bool removed = Deck.RemoveTokenAt(deckIndex);
            if (removed)
            {
                // Only a seed actually removed makes the next Compost dearer: abandoning the purchase changes nothing.
                _compostsBought++;
            }
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            return removed;
        }
    }
}
