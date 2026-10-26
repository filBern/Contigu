using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Wraps every mention of points (pts/point/points) in blue and every
    /// mention of a multiplier (multiplier/multipliers/mult/xN) in red,
    /// across an upgrade/modifier/piece-trait description, matching the
    /// same blue=score/red=multiplier convention as the Balatro-style
    /// chips/mult pills (see ComboView). Relies on Unity's legacy Text
    /// component rich-text support (&lt;color=#RRGGBB&gt;...&lt;/color&gt;),
    /// on by default. Only recognizes the literal words themselves, not
    /// every synonym, so a handful of descriptions that phrase things
    /// differently stay uncolored.
    /// The literal Lueur-diamond glyph (◆, U+25C6) embedded in descriptions
    /// is colored gold and sized 50% larger than the surrounding text, hence
    /// <see cref="Colorize"/> needs the caller's base font size.
    /// </summary>
    public static class DescriptionTextFormatter
    {
        private const string PointsColorHex = "65AED6"; // UITheme.ButtonSelected
        private const string MultiplierColorHex = "E84D45"; // UITheme.ScoreMult
        private const string DiamondColorHex = "F0B38D"; // VisualDefaults.GoldenColor
        private const string PollenColorHex = "E8A82E"; // the Pollen gold (matches the counter's icon)
        private const string DiamondGlyph = "◆";
        private const float DiamondSizeMultiplier = 1.5f;

        private static Dictionary<string, string> _flowerHex;

        /// <summary>Flower name (and its plural) to the hex of the color it stands for: Poppy, Iris, Sunflower, Fern.</summary>
        private static Dictionary<string, string> FlowerHexByName()
        {
            if (_flowerHex == null)
            {
                _flowerHex = new Dictionary<string, string>();
                foreach (var color in PieceColorUtility.BaseColors)
                {
                    string name = VisualDefaults.GetColorName(color).ToLowerInvariant();
                    // Slightly deepened so a pale flower (Sunflower) still reads on a cream tooltip.
                    var tint = VisualDefaults.GetColor(color);
                    string hex = ColorUtility.ToHtmlStringRGB(new Color(tint.r * 0.88f, tint.g * 0.88f, tint.b * 0.88f));
                    _flowerHex[name] = hex;
                    _flowerHex[name.EndsWith("y") ? name.Substring(0, name.Length - 1) + "ies" : name.EndsWith("s") ? name + "es" : name + "s"] = hex;
                }
            }
            return _flowerHex;
        }

        /// <summary>Colors only the flower names (Poppy, Iris, Sunflower, Fern) in <paramref name="text"/> — for titles and chips that are not scored descriptions.</summary>
        public static string ColorizeFlowers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return Rebuild(text, word => ColorizeFlowerWord(word, null));
        }

        public static string Colorize(string description, int fontSize)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            return Rebuild(description, word => ColorizeWord(word, fontSize));
        }

        private static string Rebuild(string text, System.Func<string, string> colorizeWord)
        {
            // Words are separated by spaces or line breaks, both kept as they are.
            var result = new System.Text.StringBuilder(text.Length + 32);
            int start = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                bool atSeparator = i == text.Length || text[i] == ' ' || text[i] == '\n';
                if (!atSeparator)
                {
                    continue;
                }
                result.Append(colorizeWord(text.Substring(start, i - start)));
                if (i < text.Length)
                {
                    result.Append(text[i]);
                }
                start = i + 1;
            }
            return result.ToString();
        }

        private static string ColorizeWord(string word, int fontSize)
        {
            int end = word.Length;
            while (end > 0 && IsTrailingPunctuation(word[end - 1]))
            {
                end--;
            }
            string core = word.Substring(0, end);
            string trailing = word.Substring(end);
            string lower = core.ToLowerInvariant();

            if (lower == "pts" || lower == "pt" || lower == "points" || lower == "point")
            {
                return Wrap(core, PointsColorHex) + trailing;
            }
            if (lower == "multiplier" || lower == "multipliers" || lower == "mult" || IsMultiplierFactor(lower))
            {
                return Wrap(core, MultiplierColorHex) + trailing;
            }
            if (lower == "pollen" || lower == "lueur")
            {
                return Wrap(core, PollenColorHex) + trailing;
            }
            if (core == DiamondGlyph)
            {
                int diamondSize = (int)(fontSize * DiamondSizeMultiplier + 0.5f);
                return "<size=" + diamondSize + ">" + Wrap(core, DiamondColorHex) + "</size>" + trailing;
            }
            return ColorizeFlowerWord(word, fontSize);
        }

        /// <summary>A flower name, with or without a plural, a possessive or surrounding brackets, in the color it stands for.</summary>
        private static string ColorizeFlowerWord(string word, int? fontSize)
        {
            int begin = 0;
            int end = word.Length;
            while (begin < end && (word[begin] == '(' || word[begin] == '"'))
            {
                begin++;
            }
            while (end > begin && (IsTrailingPunctuation(word[end - 1]) || word[end - 1] == '"'))
            {
                end--;
            }
            string core = word.Substring(begin, end - begin);
            string key = core.ToLowerInvariant();
            if (key.EndsWith("'s"))
            {
                key = key.Substring(0, key.Length - 2);
            }
            if (FlowerHexByName().TryGetValue(key, out var hex))
            {
                return word.Substring(0, begin) + Wrap(core, hex) + word.Substring(end);
            }
            return word;
        }

        private static bool IsTrailingPunctuation(char c)
        {
            return c == '.' || c == ',' || c == ')' || c == ';' || c == ':';
        }

        /// <summary>
        /// "x2", "x3", ... — the literal multiplier factor token itself.
        /// Also catches the placeholder form "xn" (used by some modifiers'
        /// descriptions) and a decimal factor like "x2.3" (a single "."
        /// with at least one digit on each side, used by progressive-
        /// modifier tooltips, see RunManager.GetProgressiveModifierStateText).
        /// </summary>
        private static bool IsMultiplierFactor(string lower)
        {
            if (lower.Length < 2 || lower[0] != 'x')
            {
                return false;
            }
            if (lower.Length == 2 && lower[1] == 'n')
            {
                return true;
            }
            bool seenDot = false;
            for (int i = 1; i < lower.Length; i++)
            {
                char c = lower[i];
                if (c == '.' && !seenDot && i > 1 && i < lower.Length - 1)
                {
                    seenDot = true;
                    continue;
                }
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static string Wrap(string text, string colorHex)
        {
            return "<color=#" + colorHex + ">" + text + "</color>";
        }
    }
}
