using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Round weather: a per-round effect rolled at the start of the run, announced one round ahead in the shop.</summary>
    public sealed partial class RunManager
    {
        private WeatherId[] _weatherByRound;
        private PieceColor[] _weatherColorByRound;

        /// <summary>This round's weather (always Clear on round 1).</summary>
        public WeatherId CurrentWeather
        {
            get { return WeatherOf(CurrentRoundIndex); }
        }

        /// <summary>The next round's weather, or null on the last round.</summary>
        public WeatherId? NextWeather
        {
            get { return CurrentRoundIndex + 1 < _challenge.RoundCount ? WeatherOf(CurrentRoundIndex + 1) : (WeatherId?)null; }
        }

        /// <summary>The seed color this round's Rain/Sunshine applies to, or null for any other weather.</summary>
        public PieceColor? CurrentWeatherColor
        {
            get { return WeatherColorOf(CurrentRoundIndex); }
        }

        /// <summary>The next round's weather color (null when it has none, or on the last round).</summary>
        public PieceColor? NextWeatherColor
        {
            get { return CurrentRoundIndex + 1 < _challenge.RoundCount ? WeatherColorOf(CurrentRoundIndex + 1) : null; }
        }

        private PieceColor? WeatherColorOf(int roundIndex)
        {
            if (_weatherColorByRound == null || roundIndex < 0 || roundIndex >= _weatherColorByRound.Length || !WeatherCatalog.UsesColor(WeatherOf(roundIndex)))
            {
                return null;
            }
            return _weatherColorByRound[roundIndex];
        }

        private WeatherId WeatherOf(int roundIndex)
        {
            if (_weatherByRound == null || roundIndex < 0 || roundIndex >= _weatherByRound.Length)
            {
                return WeatherId.Clear;
            }
            return _weatherByRound[roundIndex];
        }

        /// <summary>Called once, right after the first round started, so the roll never shifts the deck/hand draws that came before it.</summary>
        private void RollWeatherForRun()
        {
            _weatherByRound = WeatherCatalog.RollForRun(_rng, _challenge.RoundCount, out _weatherColorByRound);
        }

        /// <summary>Round-start part of the weather (Wind: a free Shuffle; Frost: frozen plots; Golden Hour: golden plots; Pollen Shower: Pollen). Round 1 is always clear.</summary>
        private void ApplyWeatherRoundStart()
        {
            switch (CurrentWeather)
            {
                case WeatherId.Wind:
                    ShufflesRemaining++;
                    break;
                case WeatherId.Frost:
                    Grid.FreezeRandomCells(ScoringConstants.FrostFrozenPlots, _rng);
                    break;
                case WeatherId.GoldenHour:
                    Grid.MakeRandomCellsGolden(ScoringConstants.GoldenHourPlots, _rng);
                    break;
                case WeatherId.PollenShower:
                    Lueur += ScoringConstants.PollenShowerPollen;
                    break;
            }
        }

        /// <summary>Placement part of the weather: Rain pays Pollen for a seed of the round's color, Sunshine pays points per cell of one, Dew pays Pollen per cleared line.</summary>
        private void ApplyWeatherPlacementBonus(PlacementResult placement, PieceColor color)
        {
            switch (CurrentWeather)
            {
                case WeatherId.Rain:
                    if (color == CurrentWeatherColor)
                    {
                        Lueur += ScoringConstants.RainPollenPerSeed;
                    }
                    break;
                case WeatherId.Sunshine:
                    if (color == CurrentWeatherColor && placement.PlacedCells.Count > 0)
                    {
                        int bonus = placement.PlacedCells.Count * ScoringConstants.SunshinePointsPerCell;
                        placement.ModifierBonus += bonus;
                        var events = new List<ScoreEvent>(placement.ScoreEvents);
                        events.Add(new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus));
                        placement.ScoreEvents = events;
                    }
                    break;
                case WeatherId.Dew:
                    Lueur += placement.ClearedLineCount * ScoringConstants.DewPollenPerLine;
                    break;
            }
        }
    }
}
