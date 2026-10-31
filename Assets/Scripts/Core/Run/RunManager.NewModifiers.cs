using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The run-state fertilizers of the 40-fertilizer batch (rounds, seeds left, enemy HP, Pollen, weather, bag size) and the two that pay Pollen when a round is cleared.</summary>
    public sealed partial class RunManager
    {
        private static readonly HashSet<ModifierId> NewRunStateModifiers = new HashSet<ModifierId>
        {
            ModifierId.Saisons, ModifierId.Outsider, ModifierId.CoupFinal, ModifierId.DerniereGraine, ModifierId.PremiereGraine,
            ModifierId.Tirelire, ModifierId.BanqueGraines, ModifierId.SousBois, ModifierId.DanseDePluie, ModifierId.DanseDuSoleil,
            ModifierId.Rustique, ModifierId.CavalierVent, ModifierId.Matinal, ModifierId.Marathonien, ModifierId.Pourfendeur
        };

        private bool HoldsAnyNewRunStateModifier()
        {
            foreach (var id in NewRunStateModifiers)
            {
                if (HasEffectiveModifier(id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Adds the placement's contribution of one of the new run-state fertilizers (no-op for any other id).</summary>
        private void ApplyNewRunStateModifier(PlacementResult placement, List<ScoreEvent> events, int index, ModifierId id)
        {
            float level = GetModifierLevelFactor(index);
            var weather = WeatherCatalog.Mechanic(CurrentWeather);
            switch (id)
            {
                case ModifierId.Saisons:
                    AddRunStateMultBonus(placement, events, id, index, CurrentRoundIndex * ScoringConstants.SeasonedMultPerRound * level, whole: false);
                    break;
                case ModifierId.Outsider:
                    if (HasActiveEncounter && EnemyHpFraction() > 0.5f)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.UnderdogMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.CoupFinal:
                    if (HasActiveEncounter && EnemyHpFraction() <= ScoringConstants.FinishingBlowHpFraction)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.FinishingBlowMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.DerniereGraine:
                    if (PiecesRemainingThisRound == 1)
                    {
                        AddRunStateMultiplier(placement, events, id, index, ScoringConstants.LastSeedMultiplier);
                    }
                    break;
                case ModifierId.PremiereGraine:
                    if (PiecesRemainingThisRound == CurrentBudget)
                    {
                        AddRunStateMultiplier(placement, events, id, index, ScoringConstants.FirstSeedMultiplier);
                    }
                    break;
                case ModifierId.Tirelire:
                    AddRunStateMultBonus(placement, events, id, index, Lueur * ScoringConstants.PiggyBankMultPerPollen * level, whole: false);
                    break;
                case ModifierId.BanqueGraines:
                    AddRunStateMultBonus(placement, events, id, index, Deck.DeckCount * ScoringConstants.SeedBankMultPerSeed * level, whole: false);
                    break;
                case ModifierId.SousBois:
                    AddRunStateMultBonus(placement, events, id, index, CountAliveEnemies() * ScoringConstants.UnderbrushMultPerEnemy * level, whole: true);
                    break;
                case ModifierId.DanseDePluie:
                    if (weather == WeatherId.Rain)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.WeatherDancerMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.DanseDuSoleil:
                    if (weather == WeatherId.Sunshine || weather == WeatherId.GoldenHour)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.WeatherDancerMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.Rustique:
                    if (weather == WeatherId.Frost)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.WeatherDancerMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.CavalierVent:
                    if (weather == WeatherId.Wind || weather == WeatherId.Hurricane)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.WeatherDancerMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.Matinal:
                    if (CurrentRoundIndex < ScoringConstants.EarlyBirdRounds)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.EarlyBirdMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.Marathonien:
                    if (CurrentRoundIndex >= ScoringConstants.MarathonerFirstRoundIndex)
                    {
                        AddRunStateMultBonus(placement, events, id, index, ScoringConstants.MarathonerMultBonus * level, whole: true);
                    }
                    break;
                case ModifierId.Pourfendeur:
                    if (IsBossRound)
                    {
                        AddRunStateMultiplier(placement, events, id, index, ScoringConstants.BossbaneMultiplier);
                    }
                    break;
            }
        }

        /// <summary>An xN contribution at <paramref name="index"/>'s place in the fold (scaled by that slot's level, never below x1).</summary>
        private void AddRunStateMultiplier(PlacementResult placement, List<ScoreEvent> events, ModifierId id, int index, int baseMultiplier)
        {
            int amount = Mathf.Max(1, Mathf.RoundToInt(baseMultiplier * GetModifierLevelFactor(index)));
            placement.ModifierMultiplier *= amount;
            var multEvent = new ScoreEvent(ScoreEventType.ModifierMultiplier, placement.PlacedCells[0], amount);
            multEvent.TriggeringModifier = id;
            multEvent.TriggeringModifierIndex = index;
            events.Add(multEvent);
        }

        /// <summary>Remaining enemy HP over their total (every enemy of the round counts toward the total; the dead have none left).</summary>
        private float EnemyHpFraction()
        {
            int current = 0;
            int max = 0;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                current += Mathf.Max(0, _currentEncounter[i].CurrentHp);
                max += _currentEncounter[i].CurrentMaxHp;
            }
            return max > 0 ? current / (float)max : 1f;
        }

        /// <summary>The tooltip's live line for the new run-state fertilizers (null for any other id).</summary>
        private string GetNewModifierStateText(ModifierId id, float level)
        {
            switch (id)
            {
                case ModifierId.Saisons:
                    return "Currently +" + FormatMultDisplay(CurrentRoundIndex * ScoringConstants.SeasonedMultPerRound * level) + " Mult";
                case ModifierId.Outsider:
                    return HasActiveEncounter && EnemyHpFraction() > 0.5f ? "Active: +" + Mathf.RoundToInt(ScoringConstants.UnderdogMultBonus * level) + " Mult" : "Inactive: the enemies are below half HP";
                case ModifierId.CoupFinal:
                    return HasActiveEncounter && EnemyHpFraction() <= ScoringConstants.FinishingBlowHpFraction ? "Active: +" + Mathf.RoundToInt(ScoringConstants.FinishingBlowMultBonus * level) + " Mult" : "Inactive: the enemies still have more than a quarter of their HP";
                case ModifierId.DerniereGraine:
                    return PiecesRemainingThisRound == 1 ? "Active: x" + ScoringConstants.LastSeedMultiplier : "Inactive: not the last seed";
                case ModifierId.PremiereGraine:
                    return PiecesRemainingThisRound == CurrentBudget ? "Active: x" + ScoringConstants.FirstSeedMultiplier : "Inactive: not the first seed";
                case ModifierId.Tirelire:
                    return "Currently +" + FormatMultDisplay(Lueur * ScoringConstants.PiggyBankMultPerPollen * level) + " Mult";
                case ModifierId.BanqueGraines:
                    return "Currently +" + FormatMultDisplay(Deck.DeckCount * ScoringConstants.SeedBankMultPerSeed * level) + " Mult";
                case ModifierId.SousBois:
                    return "Currently +" + Mathf.RoundToInt(CountAliveEnemies() * ScoringConstants.UnderbrushMultPerEnemy * level) + " Mult";
                case ModifierId.DanseDePluie:
                    return WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Rain ? "Active: +" + Mathf.RoundToInt(ScoringConstants.WeatherDancerMultBonus * level) + " Mult" : "Inactive: it isn't raining";
                case ModifierId.DanseDuSoleil:
                    return WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Sunshine || WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.GoldenHour ? "Active: +" + Mathf.RoundToInt(ScoringConstants.WeatherDancerMultBonus * level) + " Mult" : "Inactive: no sun today";
                case ModifierId.Rustique:
                    return WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Frost ? "Active: +" + Mathf.RoundToInt(ScoringConstants.WeatherDancerMultBonus * level) + " Mult" : "Inactive: no frost today";
                case ModifierId.CavalierVent:
                    return WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Wind || WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Hurricane ? "Active: +" + Mathf.RoundToInt(ScoringConstants.WeatherDancerMultBonus * level) + " Mult" : "Inactive: no wind today";
                case ModifierId.Matinal:
                    return CurrentRoundIndex < ScoringConstants.EarlyBirdRounds ? "Active: +" + Mathf.RoundToInt(ScoringConstants.EarlyBirdMultBonus * level) + " Mult" : "Inactive: past round " + ScoringConstants.EarlyBirdRounds;
                case ModifierId.Marathonien:
                    return CurrentRoundIndex >= ScoringConstants.MarathonerFirstRoundIndex ? "Active: +" + Mathf.RoundToInt(ScoringConstants.MarathonerMultBonus * level) + " Mult" : "Inactive: before round " + (ScoringConstants.MarathonerFirstRoundIndex + 1);
                case ModifierId.Pourfendeur:
                    return IsBossRound ? "Active: x" + ScoringConstants.BossbaneMultiplier : "Inactive: not a boss round";
                default:
                    return null;
            }
        }

        /// <summary>Rain Barrel and Granary: Pollen banked when a round is cleared.</summary>
        private int RoundEndFertilizerPollen()
        {
            int pollen = 0;
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_blightedModifiers.Contains(i))
                {
                    continue;
                }
                float level = GetModifierLevelFactor(i);
                if (_activeModifiers[i] == ModifierId.Tonneau)
                {
                    pollen += Mathf.RoundToInt(ShufflesRemaining * EconomyConstants.RainBarrelLueurPerShuffle * level);
                }
                else if (_activeModifiers[i] == ModifierId.Grange)
                {
                    pollen += Mathf.RoundToInt(_activeModifiers.Count * EconomyConstants.GranaryLueurPerFertilizer * level);
                }
            }
            return pollen;
        }

        /// <summary>False for a new fertilizer that could never fire in this challenge (needs enemies, or a weather the world never has).</summary>
        private bool IsNewModifierUsable(ModifierId id)
        {
            switch (id)
            {
                case ModifierId.Outsider:
                case ModifierId.CoupFinal:
                case ModifierId.SousBois:
                    return _challenge.UsesEncounters;
                case ModifierId.Rustique:
                    return WeatherCatalog.CanOccur(WeatherId.Frost, _challenge.WeatherProfile);
                default:
                    return true;
            }
        }
    }
}
