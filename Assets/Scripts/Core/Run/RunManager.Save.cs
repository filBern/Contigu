using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Saving and resuming a run: a checkpoint is taken at the start of every round, and a run resumes from the start of its last checkpoint's round.</summary>
    public sealed partial class RunManager
    {
        /// <summary>The run as it stood when the current round started. Never null.</summary>
        public RunSnapshot RoundStartSnapshot { get; private set; }

        /// <summary>The latest checkpoint — what "Save &amp; Quit" writes: the start of the current round, or, once the shop has opened, that shop as it opened (nothing bought yet). Never null.</summary>
        public RunSnapshot CheckpointSnapshot { get; private set; }

        private void CaptureShopCheckpoint()
        {
            var s = CaptureSnapshot();
            s.InShop = true;
            for (int i = 0; i < _blisterSlots.Length; i++) { s.ShopBlister.Add(ToSlotData(_blisterSlots[i])); }
            for (int i = 0; i < _upgradeSlots.Length; i++) { s.ShopWorkshop.Add(ToSlotData(_upgradeSlots[i])); }
            CheckpointSnapshot = s;
        }

        private static RunSnapshot.ShopSlotData ToSlotData(ShopSlot slot)
        {
            if (slot == null)
            {
                return new RunSnapshot.ShopSlotData { Empty = true };
            }
            if (slot.Kind == ShopSlotKind.Modifier)
            {
                return new RunSnapshot.ShopSlotData { IsModifier = true, Id = (int)slot.ModifierId, PayloadId = -1 };
            }
            return new RunSnapshot.ShopSlotData { Id = (int)slot.HiddenUpgrade.Id, PayloadId = slot.Payload != null ? (int)slot.Payload.Id : -1 };
        }

        private static ShopSlot FromSlotData(RunSnapshot.ShopSlotData d)
        {
            if (d.Empty)
            {
                return null;
            }
            if (d.IsModifier)
            {
                return ShopSlot.ForModifier((ModifierId)d.Id);
            }
            var upgrade = UpgradeCatalog.Get((UpgradeId)d.Id);
            return d.PayloadId >= 0 ? ShopSlot.ForForge(upgrade, UpgradeCatalog.Get((UpgradeId)d.PayloadId)) : ShopSlot.ForUpgrade(upgrade);
        }

        private RunSnapshot CaptureSnapshot()
        {
            var s = new RunSnapshot();
            s.Challenge = _challenge.Id;
            s.RoundIndex = CurrentRoundIndex;
            s.TotalScore = TotalScore;
            s.Lueur = Lueur;
            s.ShufflesRemaining = ShufflesRemaining;
            s.IsEndless = IsEndless;
            s.ActiveColors.AddRange(PieceColorUtility.BaseColors);
            s.Deck.AddRange(Deck.Deck);
            s.Modifiers.AddRange(_activeModifiers);
            s.ModifierLevels.AddRange(_modifierLevels);
            s.ModifierAges.AddRange(_modifierAges);
            foreach (var kv in _modifierUsageCounts) { s.ModifierUsage[kv.Key] = kv.Value; }
            foreach (var kv in _shapeMasteryLevels) { s.ShapeMastery[kv.Key] = kv.Value; }
            foreach (var kv in _colorMasteryLevels) { s.ColorMastery[kv.Key] = kv.Value; }
            s.SpecialPiecesPlayed = _specialPiecesPlayedCount;
            s.CompostsBought = _compostsBought;
            s.PerennialScores = _perennialScores;
            s.GradientBonus = Grid.GradientPermanentBonus;
            s.EpuisementValue = Grid.EpuisementCurrentBonus;
            s.LastPurchasedModifier = _lastPurchasedModifierId;
            s.Weather.AddRange(_weatherByRound);
            s.WeatherColors.AddRange(_weatherColorByRound);
            s.WeatherDisabled = _weatherDisabled;
            foreach (var pos in GridManager.AllPositions())
            {
                if (Grid.GetCell(pos).IsPermanentMultiplier)
                {
                    s.PermanentBonusCells.Add(pos);
                }
            }
            return s;
        }

        /// <summary>Rebuilds a run at the start of the round <paramref name="snapshot"/> was taken in. Null when the snapshot is unusable.</summary>
        public static RunManager Restore(RunSnapshot snapshot, IRandomProvider rng)
        {
            if (snapshot == null)
            {
                return null;
            }
            var run = new RunManager(rng, ChallengeCatalog.Get(snapshot.Challenge));
            run.ApplySnapshot(snapshot);
            return run;
        }

        private void ApplySnapshot(RunSnapshot s)
        {
            if (s.ActiveColors.Count > 0 && s.ActiveColors.Count < PieceColorUtility.AllBaseColors.Count)
            {
                PieceColorUtility.SetActiveBaseColors(s.ActiveColors);
            }
            else
            {
                PieceColorUtility.ResetBaseColors();
            }
            Deck.RestoreDeck(s.Deck);

            _activeModifiers.Clear();
            _modifierLevels.Clear();
            _modifierAges.Clear();
            int kept = 0;
            for (int i = 0; i < s.Modifiers.Count; i++)
            {
                var id = s.Modifiers[i];
                // A retired fertilizer (the color Glows) becomes its Devotion; dropped when that Devotion is already held.
                if (ModifierCatalog.RetiredReplacement(id) is ModifierId replacement)
                {
                    id = replacement;
                    if (s.Modifiers.Contains(id) || _activeModifiers.Contains(id))
                    {
                        continue;
                    }
                }
                AddActiveModifier(id);
                _modifierLevels[kept] = s.ModifierLevels[i];
                _modifierAges[kept] = s.ModifierAges[i];
                kept++;
            }
            _modifierUsageCounts.Clear();
            foreach (var kv in s.ModifierUsage) { _modifierUsageCounts[kv.Key] = kv.Value; }
            _shapeMasteryLevels.Clear();
            foreach (var kv in s.ShapeMastery) { _shapeMasteryLevels[kv.Key] = kv.Value; }
            _colorMasteryLevels.Clear();
            foreach (var kv in s.ColorMastery) { _colorMasteryLevels[kv.Key] = kv.Value; }

            _specialPiecesPlayedCount = s.SpecialPiecesPlayed;
            _compostsBought = s.CompostsBought;
            _perennialScores = s.PerennialScores;
            Grid.RestoreRunCounters(s.GradientBonus, s.EpuisementValue);
            _lastPurchasedModifierId = s.LastPurchasedModifier;
            Lueur = s.Lueur;
            TotalScore = s.TotalScore;
            IsEndless = s.IsEndless;

            _weatherByRound.Clear();
            _weatherByRound.AddRange(s.Weather);
            _weatherColorByRound.Clear();
            _weatherColorByRound.AddRange(s.WeatherColors);
            _weatherDisabled = s.WeatherDisabled;

            for (int i = 0; i < s.PermanentBonusCells.Count; i++)
            {
                var pos = s.PermanentBonusCells[i];
                if (GridManager.InBounds(pos.x, pos.y))
                {
                    Grid.GetCell(pos).IsPermanentMultiplier = true;
                }
            }

            CurrentRoundIndex = s.RoundIndex;
            StartRound();
            // The saved count already includes this round's own start bonus (Wind's free Shuffle), which StartRound just granted again.
            ShufflesRemaining = s.ShufflesRemaining;
            // Same for Pollen: a Pollen Shower pays out at round start, and the saved amount already holds it.
            Lueur = s.Lueur;
            RoundStartSnapshot = CaptureSnapshot();
            CheckpointSnapshot = RoundStartSnapshot;

            if (s.InShop)
            {
                // Back in the shop exactly as it opened: the round just cleared, the same offers, the Pollen from before any purchase.
                Lueur = s.Lueur;
                State = RunState.AwaitingShop;
                LastRoundEndLueurBonus = 0;
                _purchasesThisVisit = 0;
                _rerollsThisVisit = 0;
                for (int i = 0; i < _blisterSlots.Length; i++)
                {
                    _blisterSlots[i] = i < s.ShopBlister.Count ? FromSlotData(s.ShopBlister[i]) : null;
                }
                for (int i = 0; i < _upgradeSlots.Length; i++)
                {
                    _upgradeSlots[i] = i < s.ShopWorkshop.Count ? FromSlotData(s.ShopWorkshop[i]) : null;
                }
                CaptureShopCheckpoint();
            }
        }
    }
}
