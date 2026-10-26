using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The Workshop that replaced the Casino: packs (reveal a few options, keep one), Compost (thin the bag) and Forge (a visible tile upgrade on any seed).</summary>
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
                    PendingPackUpgrades = RollPackUpgrades(EconomyConstants.PackChoiceCount);
                    return true;
                case UpgradeId.FertilizerPack:
                    PendingUpgrade = upgrade;
                    PendingPackModifiers = RollPackModifiers(EconomyConstants.PackChoiceCount, preview: false);
                    return true;
                case UpgradeId.MasteryPack:
                    PendingUpgrade = upgrade;
                    PendingMasteryOptions = RollPackMasteryOptions(EconomyConstants.PackChoiceCount);
                    return true;
                case UpgradeId.Compost:
                    PendingUpgrade = upgrade;
                    PendingUpgradeTileCandidates = Deck.GetRandomTokenIndices(EconomyConstants.ShopTileCandidateCount, _rng);
                    _compostsBought++;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Forge: the shown tile upgrade goes to a seed the player picks among a wide choice of the bag (see <see cref="PendingUpgradeTileCandidates"/>).</summary>
        private bool ApplyForge(UpgradeDefinition payload)
        {
            PendingUpgrade = payload;
            PendingUpgradeTileCandidates = Deck.GetCandidateTokenIndices(EconomyConstants.ForgeSeedCandidateCount, _rng, null);
            return true;
        }

        /// <summary>Distinct tile upgrades (rarity-weighted like any draft) for a Sprout Pack.</summary>
        private List<UpgradeDefinition> RollPackUpgrades(int count)
        {
            var picked = new List<UpgradeDefinition>();
            for (int attempt = 0; attempt < 60 && picked.Count < count; attempt++)
            {
                var rolled = Upgrades.RollFromPool(UpgradePool.Grid);
                if (rolled.Id != UpgradeId.PermanentBonusSquare && UpgradeSystem.TraitKindFor(rolled.Id).HasValue && !picked.Contains(rolled))
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

        /// <summary>Sprout Pack: keeps tile upgrade <paramref name="index"/> of <see cref="PendingPackUpgrades"/>; the player then picks the seed it goes on (the usual tile choice).</summary>
        public bool ResolvePackUpgradeChoice(int index)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.SproutPack || index < 0 || index >= PendingPackUpgrades.Count)
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
            if (PendingUpgrade == null || (PendingUpgrade.Id != UpgradeId.SproutPack && PendingUpgrade.Id != UpgradeId.FertilizerPack))
            {
                return false;
            }
            PendingUpgrade = null;
            PendingPackUpgrades = System.Array.Empty<UpgradeDefinition>();
            PendingPackModifiers = System.Array.Empty<ModifierId>();
            return true;
        }

        /// <summary>
        /// Gives up whatever choice is pending (a pack's options, a seed to upgrade, a type to compost...) without taking anything.
        /// The Pollen already paid is not refunded. False when nothing was pending.
        /// </summary>
        public bool AbandonPending()
        {
            if (PendingUpgrade == null)
            {
                return false;
            }
            PendingUpgrade = null;
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
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            return removed;
        }
    }
}
