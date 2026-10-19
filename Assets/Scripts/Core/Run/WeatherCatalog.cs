using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>Names, descriptions, rarities and the per-run roll of the round weather.</summary>
    public static class WeatherCatalog
    {
        /// <summary>Rain and Sunshine apply to one seed color, rolled per round (see RunManager.CurrentWeatherColor).</summary>
        public static bool UsesColor(WeatherId id)
        {
            return id == WeatherId.Rain || id == WeatherId.Sunshine;
        }

        /// <summary>Relative draw weight: 3 = common, 2 = uncommon, 1 = rare.</summary>
        // Harsh weathers (Heatwave, Hurricane) are a deliberate part of the mix, not just bonuses.
        public static int GetWeight(WeatherId id)
        {
            switch (id)
            {
                case WeatherId.Clear:
                case WeatherId.Rain:
                case WeatherId.Sunshine:
                    return 3;
                case WeatherId.Wind:
                case WeatherId.Frost:
                case WeatherId.Dew:
                case WeatherId.Heatwave:
                    return 2;
                default:
                    return 1;
            }
        }

        /// <summary>Weathers that hurt rather than help (shown with a warning tone).</summary>
        public static bool IsHarsh(WeatherId id)
        {
            return id == WeatherId.Heatwave || id == WeatherId.Hurricane || id == WeatherId.Frost;
        }

        public static string GetRarityLabel(WeatherId id)
        {
            int w = GetWeight(id);
            return w >= 3 ? "Common" : w == 2 ? "Uncommon" : "Rare";
        }

        /// <summary>Display name; <paramref name="colorName"/> is the round's color for Rain/Sunshine (ignored by the others).</summary>
        public static string GetName(WeatherId id, string colorName = null)
        {
            string withColor = UsesColor(id) && !string.IsNullOrEmpty(colorName) ? " (" + colorName + ")" : "";
            switch (id)
            {
                case WeatherId.Rain: return "Rain" + withColor;
                case WeatherId.Sunshine: return "Sunshine" + withColor;
                case WeatherId.Wind: return "Wind";
                case WeatherId.Frost: return "Frost";
                case WeatherId.Dew: return "Morning Dew";
                case WeatherId.GoldenHour: return "Golden Hour";
                case WeatherId.PollenShower: return "Pollen Shower";
                case WeatherId.Heatwave: return "Heatwave";
                case WeatherId.Hurricane: return "Hurricane";
                default: return "Clear skies";
            }
        }

        public static string GetDescription(WeatherId id, string colorName = null)
        {
            string color = string.IsNullOrEmpty(colorName) ? "of the weather's color" : colorName;
            switch (id)
            {
                case WeatherId.Rain: return "+" + ScoringConstants.RainPollenPerSeed + " Pollen each time you plant a " + color + " seed.";
                case WeatherId.Sunshine: return "+" + ScoringConstants.SunshinePointsPerCell + " pts per cell of every " + color + " seed you plant.";
                case WeatherId.Wind: return "A gust blows in a free Shuffle at the start of the round.";
                case WeatherId.Frost: return ScoringConstants.FrostFrozenPlots + " empty plots start frozen (harvest their row or column to break the ice).";
                case WeatherId.Dew: return "+" + ScoringConstants.DewPollenPerLine + " Pollen for every line you clear.";
                case WeatherId.GoldenHour: return ScoringConstants.GoldenHourPlots + " empty plots turn golden for the round, and golden again each time their tile is harvested.";
                case WeatherId.PollenShower: return "Start the round with +" + ScoringConstants.PollenShowerPollen + " Pollen.";
                case WeatherId.Heatwave: return "Each Shuffle you spend scorches one planted tile (it burns away, no points).";
                case WeatherId.Hurricane: return "Every " + ScoringConstants.HurricaneInterval + " seeds you plant, the storm blows " + ScoringConstants.HurricaneTilesMoved + " planted tiles to other plots.";
                default: return "Calm skies. No effect.";
            }
        }

        /// <summary>
        /// One weather per round. Round 1 is always clear (the warm-up); later rounds draw from every kind by rarity weight
        /// (see <see cref="GetWeight"/>), never repeating the previous round's weather so the run keeps changing.
        /// </summary>
        public static WeatherId[] RollForRun(IRandomProvider rng, int roundCount)
        {
            PieceColor[] colors;
            return RollForRun(rng, roundCount, out colors);
        }

        /// <summary>
        /// Same roll, also yielding each round's seed color for Rain/Sunshine (Coral, Teal, Violet or Lime — never Joker). The color comes out
        /// of the same single draw per round as the weather itself, so adding it consumed no extra randomness.
        /// </summary>
        public static WeatherId[] RollForRun(IRandomProvider rng, int roundCount, out PieceColor[] colors)
        {
            var result = new WeatherId[roundCount];
            colors = new PieceColor[roundCount];
            var kinds = (WeatherId[])System.Enum.GetValues(typeof(WeatherId));
            for (int i = 1; i < roundCount; i++)
            {
                var candidates = new List<WeatherId>();
                int total = 0;
                for (int k = 0; k < kinds.Length; k++)
                {
                    if (kinds[k] != result[i - 1])
                    {
                        candidates.Add(kinds[k]);
                        total += GetWeight(kinds[k]);
                    }
                }
                int draw = rng.Next(total * 4);
                colors[i] = (PieceColor)(draw % 4);
                int roll = draw / 4;
                for (int c = 0; c < candidates.Count; c++)
                {
                    roll -= GetWeight(candidates[c]);
                    if (roll < 0)
                    {
                        result[i] = candidates[c];
                        break;
                    }
                }
            }
            return result;
        }
    }
}
