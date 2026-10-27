using System;
using System.Collections.Generic;
using System.IO;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// The player's discoveries across every run, kept in a small text file. A fertilizer, a sprout, an enemy or a weather counts as
    /// discovered the first time it shows up in a run (owned, offered in a shop or pack, on a seed, fought, or falling on the garden).
    /// </summary>
    public static class CollectionTracker
    {
        private static CollectionProgress _progress;
        private static Dictionary<PieceTraitKind, UpgradeId> _upgradeByTrait;

        private static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "collection.txt"); }
        }

        public static CollectionProgress Progress
        {
            get
            {
                if (_progress == null)
                {
                    try
                    {
                        _progress = CollectionProgress.FromText(File.Exists(FilePath) ? File.ReadAllText(FilePath) : null);
                    }
                    catch (Exception)
                    {
                        _progress = new CollectionProgress();
                    }
                }
                return _progress;
            }
        }

        /// <summary>Forgets every discovery (Settings → Reset data).</summary>
        public static void Reset()
        {
            _progress = new CollectionProgress();
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    WebFileSync.Flush();
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>Records everything the run currently shows: call whenever the run's visible state changes. Saves only when something is new.</summary>
        public static void Scan(RunManager run)
        {
            if (run == null)
            {
                return;
            }
            bool changed = false;
            var active = run.ActiveModifiers;
            for (int i = 0; i < active.Count; i++) { changed |= Progress.Discover(CollectionCategory.Fertilizer, (int)active[i]); }
            for (int i = 0; i < run.PendingPackModifiers.Count; i++) { changed |= Progress.Discover(CollectionCategory.Fertilizer, (int)run.PendingPackModifiers[i]); }
            for (int i = 0; i < run.PendingPackUpgrades.Count; i++) { changed |= DiscoverUpgrade(run.PendingPackUpgrades[i]); }
            if (run.State == RunState.AwaitingShop)
            {
                changed |= ScanSlots(run.ShopBlisterSlots);
                changed |= ScanSlots(run.ShopUpgradeSlots);
            }
            foreach (var token in run.Deck.Deck)
            {
                var traits = token.Traits;
                for (int t = 0; t < traits.Count; t++)
                {
                    UpgradeId id;
                    if (UpgradeByTrait.TryGetValue(traits[t].Kind, out id))
                    {
                        changed |= Progress.Discover(CollectionCategory.Sprout, (int)id);
                    }
                }
            }
            if (run.HasActiveEncounter)
            {
                var encounter = run.CurrentEncounter;
                for (int i = 0; i < encounter.Count; i++) { changed |= Progress.Discover(CollectionCategory.Enemy, (int)encounter[i].Definition.Id); }
            }
            if (run.CurrentWeather != WeatherId.Clear)
            {
                changed |= Progress.Discover(CollectionCategory.Weather, (int)run.CurrentWeather);
            }
            if (changed)
            {
                Save();
            }
        }

        private static bool ScanSlots(IReadOnlyList<ShopSlot> slots)
        {
            bool changed = false;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    continue;
                }
                if (slot.Kind == ShopSlotKind.Modifier)
                {
                    changed |= Progress.Discover(CollectionCategory.Fertilizer, (int)slot.ModifierId);
                }
                else
                {
                    changed |= DiscoverUpgrade(slot.HiddenUpgrade);
                    changed |= DiscoverUpgrade(slot.Payload);
                }
            }
            return changed;
        }

        private static bool DiscoverUpgrade(UpgradeDefinition upgrade)
        {
            return upgrade != null && UpgradeSystem.TraitKindFor(upgrade.Id).HasValue && Progress.Discover(CollectionCategory.Sprout, (int)upgrade.Id);
        }

        private static Dictionary<PieceTraitKind, UpgradeId> UpgradeByTrait
        {
            get
            {
                if (_upgradeByTrait == null)
                {
                    _upgradeByTrait = new Dictionary<PieceTraitKind, UpgradeId>();
                    foreach (var def in UpgradeCatalog.GridPool)
                    {
                        var kind = UpgradeSystem.TraitKindFor(def.Id);
                        if (kind.HasValue && !_upgradeByTrait.ContainsKey(kind.Value))
                        {
                            _upgradeByTrait[kind.Value] = def.Id;
                        }
                    }
                }
                return _upgradeByTrait;
            }
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, Progress.ToText());
                WebFileSync.Flush();
            }
            catch (Exception)
            {
            }
        }
    }
}
