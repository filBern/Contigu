using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>Names, descriptions, rarities and the per-run roll of the round weather.</summary>
    public static class WeatherCatalog
    {
        /// <summary>Rain and Sunshine apply to one seed color, rolled per round (see RunManager.CurrentWeatherColor).</summary>
        public static bool UsesColor(WeatherId id)
        {
            var m = Mechanic(id);
            return m == WeatherId.Rain || m == WeatherId.Sunshine;
        }

        /// <summary>Whether a weather with this base mechanic can ever be rolled under a challenge's weather profile (a themed variant counts).</summary>
        public static bool CanOccur(WeatherId mechanic, WeatherProfile profile)
        {
            foreach (WeatherId id in System.Enum.GetValues(typeof(WeatherId)))
            {
                if (Mechanic(id) == mechanic && GetWeight(id, profile) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The base weather a themed variant plays like (a base weather is its own mechanic).</summary>
        public static WeatherId Mechanic(WeatherId id)
        {
            switch (id)
            {
                case WeatherId.Snowfall:
                case WeatherId.Sprinkler: return WeatherId.Rain;
                case WeatherId.GrowLamp: return WeatherId.Sunshine;
                case WeatherId.VentFan: return WeatherId.Wind;
                case WeatherId.Condensation: return WeatherId.Dew;
                case WeatherId.Blizzard:
                case WeatherId.RogueCat: return WeatherId.Hurricane;
                case WeatherId.Aurora:
                case WeatherId.Compost: return WeatherId.PollenShower;
                case WeatherId.IndianSummer:
                case WeatherId.Thermostat: return WeatherId.Heatwave;
                case WeatherId.FreezerLeak: return WeatherId.Frost;
                case WeatherId.DiscoLights: return WeatherId.GoldenHour;
                default: return id;
            }
        }

        /// <summary>
        /// Relative draw weight under a challenge's weather profile (0 = this world never has it: no snow in Summer, no rain in Winter,
        /// nothing outdoors in the Greenhouse). Summer (Extreme) is never calm.
        /// </summary>
        public static int GetWeight(WeatherId id, WeatherProfile profile)
        {
            switch (profile)
            {
                case WeatherProfile.Winter:
                    switch (id)
                    {
                        case WeatherId.Frost:
                        case WeatherId.Snowfall: return 3;
                        case WeatherId.Clear:
                        case WeatherId.Wind:
                        case WeatherId.Blizzard: return 2;
                        case WeatherId.Aurora:
                        case WeatherId.Sunshine:
                        case WeatherId.GoldenHour: return 1;
                        default: return 0;
                    }
                case WeatherProfile.Extreme:
                    switch (id)
                    {
                        case WeatherId.Rain:
                        case WeatherId.Sunshine:
                        case WeatherId.Heatwave: return 3;
                        case WeatherId.Wind:
                        case WeatherId.Hurricane: return 2;
                        case WeatherId.Dew:
                        case WeatherId.GoldenHour:
                        case WeatherId.PollenShower: return 1;
                        default: return 0;
                    }
                case WeatherProfile.Autumn:
                    switch (id)
                    {
                        case WeatherId.Rain:
                        case WeatherId.Wind:
                        case WeatherId.GoldenHour: return 3;
                        case WeatherId.Dew: return 2;
                        case WeatherId.Clear:
                        case WeatherId.Sunshine:
                        case WeatherId.Frost:
                        case WeatherId.Hurricane:
                        case WeatherId.IndianSummer: return 1;
                        default: return 0;
                    }
                case WeatherProfile.Orchard:
                    switch (id)
                    {
                        case WeatherId.Rain:
                        case WeatherId.Sunshine:
                        case WeatherId.PollenShower: return 3;
                        case WeatherId.Clear:
                        case WeatherId.Wind:
                        case WeatherId.Dew: return 2;
                        case WeatherId.GoldenHour:
                        case WeatherId.Frost:
                        case WeatherId.Heatwave: return 1;
                        default: return 0;
                    }
                case WeatherProfile.Greenhouse:
                    switch (id)
                    {
                        case WeatherId.Sprinkler:
                        case WeatherId.GrowLamp: return 3;
                        case WeatherId.VentFan:
                        case WeatherId.Condensation:
                        case WeatherId.Thermostat:
                        case WeatherId.RogueCat:
                        case WeatherId.FreezerLeak: return 2;
                        case WeatherId.Clear:
                        case WeatherId.Compost:
                        case WeatherId.DiscoLights: return 1;
                        default: return 0;
                    }
                default:
                    switch (id)
                    {
                        case WeatherId.Clear:
                        case WeatherId.Rain:
                        case WeatherId.Sunshine: return 3;
                        case WeatherId.Wind:
                        case WeatherId.Dew: return 2;
                        case WeatherId.GoldenHour:
                        case WeatherId.PollenShower:
                        case WeatherId.Frost:
                        case WeatherId.Hurricane: return 1;
                        default: return 0;
                    }
            }
        }

        /// <summary>A weather amount scaled by a challenge's intensity (never below 1 for a positive base).</summary>
        public static int Scale(int amount, float intensity)
        {
            if (amount <= 0 || System.Math.Abs(intensity - 1f) < 1e-4f)
            {
                return amount;
            }
            return System.Math.Max(1, (int)System.Math.Round(amount * (double)intensity, System.MidpointRounding.AwayFromZero));
        }

        /// <summary>Relative draw weight: 3 = common, 2 = uncommon, 1 = rare.</summary>
        // Harsh weathers (Heatwave, Hurricane) are a deliberate part of the mix, not just bonuses.
        public static int GetWeight(WeatherId id)
        {
            switch (Mechanic(id))
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
            var m = Mechanic(id);
            return m == WeatherId.Heatwave || m == WeatherId.Hurricane || m == WeatherId.Frost;
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
                case WeatherId.Snowfall: return "Snowfall" + withColor;
                case WeatherId.Blizzard: return "Blizzard";
                case WeatherId.Aurora: return "Aurora";
                case WeatherId.IndianSummer: return "Indian Summer";
                case WeatherId.Sprinkler: return "Sprinkler Shower" + withColor;
                case WeatherId.GrowLamp: return "Grow Lamp" + withColor;
                case WeatherId.VentFan: return "Ventilation Fan";
                case WeatherId.Condensation: return "Condensation";
                case WeatherId.Thermostat: return "Stuck Thermostat";
                case WeatherId.RogueCat: return "Cat in the Greenhouse";
                case WeatherId.FreezerLeak: return "Freezer Door Ajar";
                case WeatherId.Compost: return "Compost Delivery";
                case WeatherId.DiscoLights: return "Disco Grow Lights";
                default: return "Clear skies";
            }
        }

        public static string GetDescription(WeatherId id, string colorName = null, float intensity = 1f)
        {
            string flavor = GetFlavor(id);
            return flavor.Length > 0 ? flavor + " " + GetMechanicDescription(id, colorName, intensity) : GetMechanicDescription(id, colorName, intensity);
        }

        private static string GetFlavor(WeatherId id)
        {
            switch (id)
            {
                case WeatherId.Snowfall: return "Soft snow muffles the garden.";
                case WeatherId.Blizzard: return "A howling whiteout.";
                case WeatherId.Aurora: return "Green lights dance over the snow.";
                case WeatherId.IndianSummer: return "A last scorching week of summer.";
                case WeatherId.Sprinkler: return "The sprinklers come on right on schedule.";
                case WeatherId.GrowLamp: return "The grow lamps hum at full power.";
                case WeatherId.VentFan: return "The ventilation fan whirs up.";
                case WeatherId.Condensation: return "The glass is dripping.";
                case WeatherId.Thermostat: return "The thermostat is stuck on maximum.";
                case WeatherId.RogueCat: return "The cat got in again and is zooming around.";
                case WeatherId.FreezerLeak: return "Someone left the cold room open.";
                case WeatherId.Compost: return "A fresh load of compost is wheeled in.";
                case WeatherId.DiscoLights: return "The grow lights start strobing gold.";
                default: return "";
            }
        }

        private static string GetMechanicDescription(WeatherId id, string colorName, float intensity)
        {
            id = Mechanic(id);
            string color = string.IsNullOrEmpty(colorName) ? "of the weather's color" : colorName;
            switch (id)
            {
                case WeatherId.Rain: return "+" + Scale(ScoringConstants.RainPollenPerSeed, intensity) + " Pollen each time you plant a " + color + " seed.";
                case WeatherId.Sunshine: return "+" + Scale(ScoringConstants.SunshinePointsPerCell, intensity) + " pts per cell of every " + color + " seed you plant.";
                case WeatherId.Wind: return "A gust blows in a free Shuffle at the start of the round.";
                case WeatherId.Frost: return Scale(ScoringConstants.FrostFrozenPlots, intensity) + " empty plots start frozen (harvest their row or column to break the ice).";
                case WeatherId.Dew: return "+" + Scale(ScoringConstants.DewPollenPerLine, intensity) + " Pollen for every line you clear.";
                case WeatherId.GoldenHour: return Scale(ScoringConstants.GoldenHourPlots, intensity) + " empty plots turn golden for the round, and golden again each time their tile is harvested.";
                case WeatherId.PollenShower: return "Start the round with +" + Scale(ScoringConstants.PollenShowerPollen, intensity) + " Pollen.";
                case WeatherId.Heatwave: return "Each Shuffle you spend scorches one planted tile (it burns away, no points).";
                case WeatherId.Hurricane: return "Every " + ScoringConstants.HurricaneInterval + " seeds you plant, the storm blows " + Scale(ScoringConstants.HurricaneTilesMoved, intensity) + " planted tiles to other plots.";
                default: return "Calm skies. No effect.";
            }
        }

        /// <summary>
        /// One weather per round. Spring (the Normal profile) always opens on clear skies as the tutorial warm-up; every other challenge draws its first round like any other.
        /// Rounds draw from the profile's kinds by rarity weight (see <see cref="GetWeight(WeatherId, WeatherProfile)"/>), never repeating the previous round's weather.
        /// </summary>
        public static WeatherId[] RollForRun(IRandomProvider rng, int roundCount)
        {
            PieceColor[] colors;
            return RollForRun(rng, roundCount, out colors);
        }

        public static WeatherId[] RollForRun(IRandomProvider rng, int roundCount, out PieceColor[] colors, WeatherProfile profile = WeatherProfile.Normal)
        {
            var result = new WeatherId[roundCount];
            colors = new PieceColor[roundCount];
            for (int i = profile == WeatherProfile.Normal ? 1 : 0; i < roundCount; i++)
            {
                result[i] = RollNext(rng, i == 0 ? (WeatherId?)null : result[i - 1], profile, out colors[i]);
            }
            return result;
        }

        /// <summary>
        /// One round's weather after <paramref name="previous"/> (never the same twice running), also yielding its seed color for Rain/Sunshine
        /// (Coral, Teal, Violet or Lime — never Joker). The color comes out of the same single draw as the weather itself, so it consumes no
        /// extra randomness.
        /// </summary>
        public static WeatherId RollNext(IRandomProvider rng, WeatherId? previous, WeatherProfile profile, out PieceColor color)
        {
            var kinds = (WeatherId[])System.Enum.GetValues(typeof(WeatherId));
            var candidates = new List<WeatherId>();
            int total = 0;
            for (int k = 0; k < kinds.Length; k++)
            {
                int weight = GetWeight(kinds[k], profile);
                if (kinds[k] != previous && weight > 0)
                {
                    candidates.Add(kinds[k]);
                    total += weight;
                }
            }
            int draw = rng.Next(total * 4);
            // Same as the plain enum order with all 4 colors; with fewer (Orchard) it can only land on a color that exists.
            color = PieceColorUtility.BaseColors[(draw % 4) % PieceColorUtility.BaseColors.Count];
            int roll = draw / 4;
            for (int c = 0; c < candidates.Count; c++)
            {
                roll -= GetWeight(candidates[c], profile);
                if (roll < 0)
                {
                    return candidates[c];
                }
            }
            return candidates[candidates.Count - 1];
        }
    }
}
