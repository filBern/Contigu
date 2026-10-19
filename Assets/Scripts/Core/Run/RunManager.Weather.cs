using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Round weather: a per-round effect rolled at the start of the run, announced one round ahead in the shop.</summary>
    public sealed partial class RunManager
    {
        private WeatherId[] _weatherByRound;

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
            _weatherByRound = WeatherCatalog.RollForRun(_rng, _challenge.RoundCount);
        }

        /// <summary>Round-start part of the weather (Wind: a free Shuffle; Frost: frozen plots). Round 1 is always clear.</summary>
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
            }
        }

        /// <summary>Placement part of the weather: Rain pays Pollen for an Iris seed, Sunshine pays points per cell of a Sunflower seed.</summary>
        private void ApplyWeatherPlacementBonus(PlacementResult placement, PieceColor color)
        {
            switch (CurrentWeather)
            {
                case WeatherId.Rain:
                    if (color == PieceColor.Teal)
                    {
                        Lueur += ScoringConstants.RainPollenPerIrisSeed;
                    }
                    break;
                case WeatherId.Sunshine:
                    if (color == PieceColor.Violet && placement.PlacedCells.Count > 0)
                    {
                        int bonus = placement.PlacedCells.Count * ScoringConstants.SunshinePointsPerSunflowerCell;
                        placement.ModifierBonus += bonus;
                        var events = new List<ScoreEvent>(placement.ScoreEvents);
                        events.Add(new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus));
                        placement.ScoreEvents = events;
                    }
                    break;
            }
        }
    }
}
