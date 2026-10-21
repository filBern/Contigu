using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Round weather: a per-round effect rolled at the start of the run, announced one round ahead in the shop.</summary>
    public sealed partial class RunManager
    {
        // Rolled lazily: the first RoundCount entries at the start of the run, the rest only once Endless asks for them.
        private readonly List<WeatherId> _weatherByRound = new List<WeatherId>();
        private readonly List<PieceColor> _weatherColorByRound = new List<PieceColor>();
        private bool _weatherDisabled;

        /// <summary>This round's weather (Spring always opens clear).</summary>
        public WeatherId CurrentWeather
        {
            get { return WeatherOf(CurrentRoundIndex); }
        }

        /// <summary>The next round's weather, or null on the last round.</summary>
        public WeatherId? NextWeather
        {
            get { return HasNextRound ? WeatherOf(CurrentRoundIndex + 1) : (WeatherId?)null; }
        }

        /// <summary>The seed color this round's Rain/Sunshine applies to, or null for any other weather.</summary>
        public PieceColor? CurrentWeatherColor
        {
            get { return WeatherColorOf(CurrentRoundIndex); }
        }

        /// <summary>The next round's weather color (null when it has none, or on the last round).</summary>
        public PieceColor? NextWeatherColor
        {
            get { return HasNextRound ? WeatherColorOf(CurrentRoundIndex + 1) : null; }
        }

        /// <summary>How hard this challenge's weather hits (Summer: more).</summary>
        public float WeatherIntensity
        {
            get { return _challenge.WeatherIntensity; }
        }

        private int ScaledWeather(int amount)
        {
            return WeatherCatalog.Scale(amount, _challenge.WeatherIntensity);
        }

        private PieceColor? WeatherColorOf(int roundIndex)
        {
            if (!WeatherCatalog.UsesColor(WeatherOf(roundIndex)))
            {
                return null;
            }
            return _weatherColorByRound[roundIndex];
        }

        private WeatherId WeatherOf(int roundIndex)
        {
            if (_weatherDisabled || roundIndex < 0 || _weatherByRound.Count == 0)
            {
                return WeatherId.Clear;
            }
            // Endless: keep rolling as far as the run goes, each round never repeating the one before.
            while (_weatherByRound.Count <= roundIndex)
            {
                PieceColor color;
                var next = WeatherCatalog.RollNext(_rng, _weatherByRound[_weatherByRound.Count - 1], _challenge.WeatherProfile, out color);
                _weatherByRound.Add(next);
                _weatherColorByRound.Add(color);
            }
            return _weatherByRound[roundIndex];
        }

        /// <summary>Called once, right after the first round started, so the roll never shifts the deck/hand draws that came before it.</summary>
        private void RollWeatherForRun()
        {
            PieceColor[] colors;
            var weathers = WeatherCatalog.RollForRun(_rng, _challenge.RoundCount, out colors, _challenge.WeatherProfile);
            _weatherByRound.Clear();
            _weatherByRound.AddRange(weathers);
            _weatherColorByRound.Clear();
            _weatherColorByRound.AddRange(colors);
        }

        /// <summary>Round-start part of the weather (Wind: a free Shuffle; Frost: frozen plots; Golden Hour: golden plots; Pollen Shower: Pollen). Spring's round 1 is always clear.</summary>
        private void ApplyWeatherRoundStart()
        {
            switch (WeatherCatalog.Mechanic(CurrentWeather))
            {
                case WeatherId.Wind:
                    ShufflesRemaining++;
                    break;
                case WeatherId.Frost:
                    if (HasEffectiveModifier(ModifierId.Impermeable))
                    {
                        break;
                    }
                    Grid.FreezeRandomCells(ScaledWeather(ScoringConstants.FrostFrozenPlots), _rng);
                    break;
                case WeatherId.GoldenHour:
                    Grid.MakeRandomCellsGolden(ScaledWeather(ScoringConstants.GoldenHourPlots), _rng);
                    break;
                case WeatherId.PollenShower:
                    Lueur += ScaledWeather(ScoringConstants.PollenShowerPollen);
                    break;
            }
        }

        /// <summary>Placement part of the weather: Rain pays Pollen for a seed of the round's color, Sunshine pays points per cell of one, Dew pays Pollen per cleared line.</summary>
        private void ApplyWeatherPlacementBonus(PlacementResult placement, PieceColor color)
        {
            switch (WeatherCatalog.Mechanic(CurrentWeather))
            {
                case WeatherId.Rain:
                    if (color == CurrentWeatherColor)
                    {
                        Lueur += ScaledWeather(ScoringConstants.RainPollenPerSeed);
                        WeatherBonusTriggerCount++;
                    }
                    break;
                case WeatherId.Sunshine:
                    if (color == CurrentWeatherColor && placement.PlacedCells.Count > 0)
                    {
                        int bonus = placement.PlacedCells.Count * ScaledWeather(ScoringConstants.SunshinePointsPerCell);
                        placement.ModifierBonus += bonus;
                        var events = new List<ScoreEvent>(placement.ScoreEvents);
                        events.Add(new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus));
                        placement.ScoreEvents = events;
                        WeatherBonusTriggerCount++;
                    }
                    break;
                case WeatherId.Dew:
                    if (placement.ClearedLineCount > 0)
                    {
                        Lueur += placement.ClearedLineCount * ScaledWeather(ScoringConstants.DewPollenPerLine);
                        WeatherBonusTriggerCount++;
                    }
                    break;
            }
        }

        /// <summary>Counts every time a placement weather bonus (Rain, Sunshine, Dew) paid out — Presentation compares it before/after a placement to pulse the weather icon.</summary>
        public int WeatherBonusTriggerCount { get; private set; }

        /// <summary>The tile the Heatwave scorched on the most recent manual Shuffle (null otherwise) — Presentation plays the burn before refreshing the grid.</summary>
        public (Vector2Int Pos, PieceColor Color, PieceTrait? Trait)? LastBurnedTile { get; private set; }

        private void ApplyHeatwaveShuffleBurn()
        {
            LastBurnedTile = WeatherCatalog.Mechanic(CurrentWeather) == WeatherId.Heatwave && !HasEffectiveModifier(ModifierId.Impermeable) ? Grid.BurnRandomTile(_rng) : null;
        }

        /// <summary>Hurricane: every few seeds placed, blows some tiles elsewhere. Returns the moves (empty when none) for the placement's outcome.</summary>
        private IReadOnlyList<(Vector2Int From, Vector2Int To, PieceColor Color)> ApplyHurricaneTick()
        {
            int played = CurrentBudget - PiecesRemainingThisRound;
            if (WeatherCatalog.Mechanic(CurrentWeather) != WeatherId.Hurricane || HasEffectiveModifier(ModifierId.Impermeable) || played <= 0 || played % ScoringConstants.HurricaneInterval != 0)
            {
                return System.Array.Empty<(Vector2Int, Vector2Int, PieceColor)>();
            }
            return Grid.BlowTiles(ScaledWeather(ScoringConstants.HurricaneTilesMoved), _rng);
        }
    }
}
