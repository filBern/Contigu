using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>Names, descriptions and the per-run roll of the round weather.</summary>
    public static class WeatherCatalog
    {
        public static string GetName(WeatherId id)
        {
            switch (id)
            {
                case WeatherId.Rain: return "Rain";
                case WeatherId.Sunshine: return "Sunshine";
                case WeatherId.Wind: return "Wind";
                case WeatherId.Frost: return "Frost";
                default: return "Clear skies";
            }
        }

        public static string GetDescription(WeatherId id)
        {
            switch (id)
            {
                case WeatherId.Rain: return "+" + ScoringConstants.RainPollenPerIrisSeed + " Pollen each time you plant an Iris seed.";
                case WeatherId.Sunshine: return "+" + ScoringConstants.SunshinePointsPerSunflowerCell + " pts per cell of every Sunflower seed you plant.";
                case WeatherId.Wind: return "A gust blows in a free Shuffle at the start of the round.";
                case WeatherId.Frost: return ScoringConstants.FrostFrozenPlots + " empty plots start frozen (harvest their row or column to break the ice).";
                default: return "Calm skies. No effect.";
            }
        }

        /// <summary>
        /// One weather per round. Round 1 is always clear (the warm-up); later rounds draw from every kind, never repeating the previous
        /// round's weather so the run keeps changing.
        /// </summary>
        public static WeatherId[] RollForRun(IRandomProvider rng, int roundCount)
        {
            var result = new WeatherId[roundCount];
            var kinds = (WeatherId[])System.Enum.GetValues(typeof(WeatherId));
            for (int i = 1; i < roundCount; i++)
            {
                var candidates = new List<WeatherId>();
                for (int k = 0; k < kinds.Length; k++)
                {
                    if (kinds[k] != result[i - 1])
                    {
                        candidates.Add(kinds[k]);
                    }
                }
                result[i] = candidates[rng.Next(candidates.Count)];
            }
            return result;
        }
    }
}
