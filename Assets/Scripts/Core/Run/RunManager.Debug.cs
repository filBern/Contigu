using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Editor/debug-only helpers (compiled only in the editor or with CONTIGU_DEBUG).</summary>
    public sealed partial class RunManager
    {
#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: makes every round clear-skied, so a test's exact expectations (frozen/locked cell counts, shuffle bank) do not depend on the rolled weather. Call before advancing rounds.</summary>
        public void DebugDisableWeather()
        {
            _weatherDisabled = true;
        }


        /// <summary>Debug-only helper: forces this round's weather (no effect on a round that already applied its start-of-round part).</summary>
        public void DebugSetWeather(WeatherId weather)
        {
            _weatherDisabled = false;
            WeatherOf(CurrentRoundIndex);
            _weatherByRound[CurrentRoundIndex] = weather;
        }
#endif
#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>
        /// Debug-only helper: instantly completes the current round as if its
        /// quota had just been reached. No-op if the run isn't InProgress.
        /// Pure core logic (no UnityEditor dependency), so the method ships
        /// in real builds; only its call site is gated behind #if
        /// UNITY_EDITOR. Still sets RoundScore to CurrentQuota
        /// unconditionally so every caller/test reading it back sees the same
        /// value - encounter rounds additionally need every enemy actually
        /// dead (see DebugDefeatAllEnemies), since EvaluateRoundEnd ignores
        /// RoundScore once HasActiveEncounter is true.
        /// </summary>
        public RunState DebugForceRoundComplete()
        {
            if (State != RunState.InProgress)
            {
                return State;
            }
            RoundScore = CurrentQuota;
            if (HasActiveEncounter)
            {
                DebugDefeatAllEnemies();
            }
            EvaluateRoundEnd();
            return State;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: kills every enemy in <see cref="CurrentEncounter"/> outright (same cleanup as a real kill - see CleanUpDefeatedEnemy).</summary>
        private void DebugDefeatAllEnemies()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                // A shielded enemy (Reclaimer) would absorb this whole hit; a debug kill must really kill it.
                enemy.HasShield = false;
                enemy.ApplyDamage(enemy.CurrentMaxHp);
                CleanUpDefeatedEnemy(enemy);
            }
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>
        /// Debug-only helper: adds <paramref name="modifierId"/> straight to
        /// the player's active set, bypassing the shop entirely. Used by
        /// EditMode tests that need a specific modifier active without
        /// fighting shop RNG. Still respects
        /// EconomyConstants.MaxActiveModifiers and never duplicates a
        /// modifier already held.
        /// </summary>
        public bool DebugGrantModifier(ModifierId modifierId)
        {
            if (_activeModifiers.Contains(modifierId) || _activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            AddActiveModifier(modifierId);
            return true;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: replaces the current encounter outright with brand-new instances of exactly these ids, bypassing EncounterCatalog.</summary>
        public void DebugSetEncounter(params EnemyId[] ids)
        {
            var instances = new List<EnemyInstance>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                instances.Add(new EnemyInstance(EnemyCatalog.Get(ids[i])));
            }
            _currentEncounter = instances;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: adds Lueur directly, bypassing gameplay.</summary>
        public void DebugGrantLueur(int amount)
        {
            Lueur += amount;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: pins Lueur to an exact value, bypassing gameplay - lets EditMode tests establish a known baseline before asserting purchase/reroll outcomes.</summary>
        public void DebugSetLueur(int amount)
        {
            Lueur = amount;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: pins ShufflesRemaining to an exact value, bypassing gameplay - avoids re-dealing the hand via ShuffleHand for a deliberately set-up deterministic scenario.</summary>
        public void DebugSetShufflesRemaining(int amount)
        {
            ShufflesRemaining = amount;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>
        /// Debug-only helper: exercises the exact same effect + reveal-
        /// surfacing (LastRandomModifierGranted) that buying the "Random
        /// Modifier" upgrade for real would, without needing to
        /// find/afford/click it in the shop first.
        /// </summary>
        /// </summary>
        public ModifierId? DebugTriggerRandomModifierGrant()
        {
            LastRandomModifierGranted = GrantRandomModifier();
            return LastRandomModifierGranted;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: exercises "Piece Mastery"'s exact grant (GrantShapeMastery), bypassing the shop entirely.</summary>
        public ShapeId DebugTriggerShapeMasteryGrant()
        {
            LastShapeMasteryGranted = GrantShapeMastery();
            return LastShapeMasteryGranted.Value;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>Debug-only helper: exercises "Color Mastery"'s exact grant (GrantColorMastery), bypassing the shop entirely.</summary>
        public PieceColor DebugTriggerColorMasteryGrant()
        {
            LastColorMasteryGranted = GrantColorMastery();
            return LastColorMasteryGranted.Value;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>
        /// Debug-only helper: overwrites upgrade slot <paramref
        /// name="index"/> (default 0) to be "Random Modifier" outright,
        /// bypassing the shop's own roll - unlike
        /// DebugTriggerRandomModifierGrant above, this goes through the real
        /// purchase button (BuyUpgradeSlot), exercising its exact branching
        /// instead of skipping straight to GrantRandomModifier. No-op
        /// (false) if the shop isn't currently open.
        /// </summary>
        public bool DebugForceUpgradeSlotToRandomModifier(int index = 0)
        {
            if (State != RunState.AwaitingShop || index < 0 || index >= _upgradeSlots.Length)
            {
                return false;
            }
            _upgradeSlots[index] = ShopSlot.ForUpgrade(UpgradeCatalog.RandomModifier);
            return true;
        }
#endif

#if UNITY_EDITOR || CONTIGU_DEBUG
        /// <summary>
        /// Debug-only helper: overwrites Blister slot <paramref
        /// name="index"/> (default 0) to hold <paramref
        /// name="modifierId"/> outright, bypassing the shop's own roll -
        /// lets a test buy a specific modifier through the real purchase
        /// path (BuyBlisterSlot), which RerollShop can't reach into since it
        /// only ever touches Casino. No-op (false) if the shop isn't
        /// currently open.
        /// </summary>
        public bool DebugForceBlisterSlotToModifier(ModifierId modifierId, int index = 0)
        {
            if (State != RunState.AwaitingShop || index < 0 || index >= _blisterSlots.Length)
            {
                return false;
            }
            _blisterSlots[index] = ShopSlot.ForModifier(modifierId);
            return true;
        }
#endif
    }
}
