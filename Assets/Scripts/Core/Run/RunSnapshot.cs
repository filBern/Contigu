using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Everything that defines a run at the START of a round (the checkpoint a saved run resumes from): the bag with its upgrades,
    /// fertilizers, masteries, Pollen, the weather forecast and the rest of the run-long counters. The grid, the hand and the enemies
    /// are rebuilt fresh when a round starts, so they are not part of it. Plain text, one "key=value" per line.
    /// </summary>
    public sealed class RunSnapshot
    {
        public const int CurrentVersion = 1;

        public ChallengeId Challenge;
        public int RoundIndex;
        public int TotalScore;
        public int Lueur;
        public int ShufflesRemaining;
        public bool IsEndless;
        public List<PieceColor> ActiveColors = new List<PieceColor>();
        public List<PieceToken> Deck = new List<PieceToken>();
        public List<ModifierId> Modifiers = new List<ModifierId>();
        public List<int> ModifierLevels = new List<int>();
        public List<int> ModifierAges = new List<int>();
        public Dictionary<ModifierId, int> ModifierUsage = new Dictionary<ModifierId, int>();
        public Dictionary<ShapeId, int> ShapeMastery = new Dictionary<ShapeId, int>();
        public Dictionary<PieceColor, int> ColorMastery = new Dictionary<PieceColor, int>();
        public int SpecialPiecesPlayed;
        public int CompostsBought;
        public int PerennialScores;
        public int GradientBonus;
        public int EpuisementValue = ScoringConstants.EpuisementStartingBonus;
        public ModifierId? LastPurchasedModifier;
        public List<WeatherId> Weather = new List<WeatherId>();
        public List<PieceColor> WeatherColors = new List<PieceColor>();
        public bool WeatherDisabled;
        public List<Vector2Int> PermanentBonusCells = new List<Vector2Int>();

        /// <summary>True for a checkpoint taken the moment the shop opened (RoundIndex is then the round just cleared): the run resumes in that shop, with these exact offers.</summary>
        public bool InShop;
        public List<ShopSlotData> ShopBlister = new List<ShopSlotData>();
        public List<ShopSlotData> ShopWorkshop = new List<ShopSlotData>();

        /// <summary>One shop offer as saved: a fertilizer, or an upgrade (with its Forge payload when it is the Forge). Null Slot means an empty position.</summary>
        public struct ShopSlotData
        {
            public bool Empty;
            public bool IsModifier;
            public int Id;
            public int PayloadId;
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            Line(sb, "version", CurrentVersion);
            Line(sb, "challenge", (int)Challenge);
            Line(sb, "round", RoundIndex);
            Line(sb, "score", TotalScore);
            Line(sb, "lueur", Lueur);
            Line(sb, "shuffles", ShufflesRemaining);
            Line(sb, "endless", IsEndless ? 1 : 0);
            Line(sb, "colors", Join(ActiveColors, c => ((int)c).ToString(CultureInfo.InvariantCulture)));
            Line(sb, "deck", Join(Deck, TokenToText, ";"));
            Line(sb, "mods", Join(Modifiers, m => ((int)m).ToString(CultureInfo.InvariantCulture)));
            Line(sb, "levels", Join(ModifierLevels, i => i.ToString(CultureInfo.InvariantCulture)));
            Line(sb, "ages", Join(ModifierAges, i => i.ToString(CultureInfo.InvariantCulture)));
            Line(sb, "usage", JoinPairs(ModifierUsage, k => (int)k));
            Line(sb, "shapemastery", JoinPairs(ShapeMastery, k => (int)k));
            Line(sb, "colormastery", JoinPairs(ColorMastery, k => (int)k));
            Line(sb, "special", SpecialPiecesPlayed);
            Line(sb, "composts", CompostsBought);
            Line(sb, "perennial", PerennialScores);
            Line(sb, "gradient", GradientBonus);
            Line(sb, "dwindling", EpuisementValue);
            Line(sb, "lastmod", LastPurchasedModifier.HasValue ? (int)LastPurchasedModifier.Value : -1);
            Line(sb, "weather", Join(Weather, w => ((int)w).ToString(CultureInfo.InvariantCulture)));
            Line(sb, "weathercolors", Join(WeatherColors, c => ((int)c).ToString(CultureInfo.InvariantCulture)));
            Line(sb, "weatheroff", WeatherDisabled ? 1 : 0);
            Line(sb, "permanent", Join(PermanentBonusCells, p => p.x.ToString(CultureInfo.InvariantCulture) + ":" + p.y.ToString(CultureInfo.InvariantCulture)));
            Line(sb, "inshop", InShop ? 1 : 0);
            Line(sb, "blister", Join(ShopBlister, SlotToText, ";"));
            Line(sb, "workshop", Join(ShopWorkshop, SlotToText, ";"));
            return sb.ToString();
        }

        private static string SlotToText(ShopSlotData d)
        {
            return d.Empty ? "-" : (d.IsModifier ? "m" : "u") + "," + d.Id.ToString(CultureInfo.InvariantCulture) + "," + d.PayloadId.ToString(CultureInfo.InvariantCulture);
        }

        private static ShopSlotData SlotFromText(string text)
        {
            if (text == "-")
            {
                return new ShopSlotData { Empty = true };
            }
            var f = text.Split(',');
            return new ShopSlotData { IsModifier = f[0] == "m", Id = ParseInt(f[1]), PayloadId = ParseInt(f[2]) };
        }

        /// <summary>Parses <see cref="ToText"/>'s output; null when the text is empty, from another version or damaged.</summary>
        public static RunSnapshot FromText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            try
            {
                var values = new Dictionary<string, string>();
                foreach (var raw in text.Split('\n'))
                {
                    int eq = raw.IndexOf('=');
                    if (eq > 0)
                    {
                        values[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
                    }
                }
                if (Int(values, "version") != CurrentVersion)
                {
                    return null;
                }
                var s = new RunSnapshot();
                s.Challenge = (ChallengeId)Int(values, "challenge");
                s.RoundIndex = Int(values, "round");
                s.TotalScore = Int(values, "score");
                s.Lueur = Int(values, "lueur");
                s.ShufflesRemaining = Int(values, "shuffles");
                s.IsEndless = Int(values, "endless") == 1;
                foreach (var v in Split(values, "colors", ',')) { s.ActiveColors.Add((PieceColor)ParseInt(v)); }
                foreach (var v in Split(values, "deck", ';')) { s.Deck.Add(TokenFromText(v)); }
                foreach (var v in Split(values, "mods", ',')) { s.Modifiers.Add((ModifierId)ParseInt(v)); }
                foreach (var v in Split(values, "levels", ',')) { s.ModifierLevels.Add(ParseInt(v)); }
                foreach (var v in Split(values, "ages", ',')) { s.ModifierAges.Add(ParseInt(v)); }
                foreach (var p in Pairs(values, "usage")) { s.ModifierUsage[(ModifierId)p.Key] = p.Value; }
                foreach (var p in Pairs(values, "shapemastery")) { s.ShapeMastery[(ShapeId)p.Key] = p.Value; }
                foreach (var p in Pairs(values, "colormastery")) { s.ColorMastery[(PieceColor)p.Key] = p.Value; }
                s.SpecialPiecesPlayed = Int(values, "special");
                s.CompostsBought = Int(values, "composts");
                s.PerennialScores = Int(values, "perennial");
                s.GradientBonus = values.ContainsKey("gradient") ? Int(values, "gradient") : 0;
                s.EpuisementValue = values.ContainsKey("dwindling") ? Int(values, "dwindling") : ScoringConstants.EpuisementStartingBonus;
                int lastMod = Int(values, "lastmod");
                s.LastPurchasedModifier = lastMod >= 0 ? (ModifierId?)(ModifierId)lastMod : null;
                foreach (var v in Split(values, "weather", ',')) { s.Weather.Add((WeatherId)ParseInt(v)); }
                foreach (var v in Split(values, "weathercolors", ',')) { s.WeatherColors.Add((PieceColor)ParseInt(v)); }
                s.WeatherDisabled = Int(values, "weatheroff") == 1;
                foreach (var v in Split(values, "permanent", ','))
                {
                    var xy = v.Split(':');
                    s.PermanentBonusCells.Add(new Vector2Int(ParseInt(xy[0]), ParseInt(xy[1])));
                }
                s.InShop = values.ContainsKey("inshop") && Int(values, "inshop") == 1;
                foreach (var v in Split(values, "blister", ';')) { s.ShopBlister.Add(SlotFromText(v)); }
                foreach (var v in Split(values, "workshop", ';')) { s.ShopWorkshop.Add(SlotFromText(v)); }
                if (s.Deck.Count == 0 || s.Modifiers.Count != s.ModifierLevels.Count || s.Modifiers.Count != s.ModifierAges.Count)
                {
                    return null;
                }
                return s;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static string TokenToText(PieceToken t)
        {
            var sb = new StringBuilder();
            sb.Append((int)t.Shape).Append(',').Append((int)t.Color);
            var traits = t.Traits;
            for (int i = 0; i < traits.Count; i++)
            {
                sb.Append(',').Append((int)traits[i].Kind).Append('.').Append(traits[i].LocalCellIndex).Append('.').Append(traits[i].TintedColor.HasValue ? (int)traits[i].TintedColor.Value : -1);
            }
            return sb.ToString();
        }

        private static PieceToken TokenFromText(string text)
        {
            var parts = text.Split(',');
            var shape = (ShapeId)ParseInt(parts[0]);
            var color = (PieceColor)ParseInt(parts[1]);
            var traits = new List<PieceTrait>();
            for (int i = 2; i < parts.Length; i++)
            {
                var f = parts[i].Split('.');
                int tint = ParseInt(f[2]);
                traits.Add(new PieceTrait((PieceTraitKind)ParseInt(f[0]), ParseInt(f[1]), tint >= 0 ? (PieceColor?)(PieceColor)tint : null));
            }
            if (traits.Count == 0)
            {
                return new PieceToken(shape, color);
            }
            var extras = traits.GetRange(1, traits.Count - 1).ToArray();
            return new PieceToken(shape, color, traits[0], extras);
        }

        private static void Line(StringBuilder sb, string key, int value)
        {
            Line(sb, key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Line(StringBuilder sb, string key, string value)
        {
            sb.Append(key).Append('=').Append(value).Append('\n');
        }

        private static string Join<T>(IList<T> items, System.Func<T, string> format, string separator = ",")
        {
            var sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }
                sb.Append(format(items[i]));
            }
            return sb.ToString();
        }

        private static string JoinPairs<TKey>(Dictionary<TKey, int> map, System.Func<TKey, int> key)
        {
            var parts = new List<string>();
            foreach (var kv in map)
            {
                parts.Add(key(kv.Key).ToString(CultureInfo.InvariantCulture) + ":" + kv.Value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join(",", parts.ToArray());
        }

        private static int ParseInt(string s)
        {
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        private static int Int(Dictionary<string, string> values, string key)
        {
            return ParseInt(values[key]);
        }

        private static IEnumerable<string> Split(Dictionary<string, string> values, string key, char separator)
        {
            string raw;
            if (!values.TryGetValue(key, out raw) || raw.Length == 0)
            {
                return new string[0];
            }
            return raw.Split(separator);
        }

        private static IEnumerable<KeyValuePair<int, int>> Pairs(Dictionary<string, string> values, string key)
        {
            var list = new List<KeyValuePair<int, int>>();
            foreach (var part in Split(values, key, ','))
            {
                var kv = part.Split(':');
                list.Add(new KeyValuePair<int, int>(ParseInt(kv[0]), ParseInt(kv[1])));
            }
            return list;
        }
    }
}
