namespace Contigu.Presentation
{
    /// <summary>
    /// Wraps every mention of points (pts/point/points) in blue, every
    /// mention of a multiplier (multiplier/multipliers/mult/xN — "mult"
    /// added once the genuine additive "+Mult" modifiers arrived, ninth
    /// batch) in red, and every mention of Lueur in gold, across an
    /// upgrade/modifier/piece-trait
    /// description — on explicit request ("à chaque fois que le mot point
    /// apparait dans les description, que le mot soit bleu et idem pour le
    /// rouge et le multiplicateur"), matching the same blue=score/red=
    /// multiplier convention as the Balatro-style chips/mult pills (see
    /// ComboView); Lueur added the same way once the currency got its own
    /// modifiers to describe (see ModifierId's eighth batch), gold matching
    /// VisualDefaults.GoldenColor/the HUD's own Lueur label. Relies on
    /// Unity's legacy Text component rich-text support
    /// (&lt;color=#RRGGBB&gt;...&lt;/color&gt;), on by default and never
    /// disabled anywhere in this project's UIFactory. Only recognizes the
    /// literal words themselves, not every synonym ("doubles", "+18 flat")
    /// — a handful of upgrade descriptions that phrase things differently
    /// simply stay uncolored.
    /// </summary>
    public static class DescriptionTextFormatter
    {
        private const string PointsColorHex = "65AED6"; // UITheme.ButtonSelected
        private const string MultiplierColorHex = "B56D7F"; // UITheme.Danger
        private const string LueurColorHex = "F0B38D"; // VisualDefaults.GoldenColor

        public static string Colorize(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            var words = description.Split(' ');
            string result = null;
            for (int i = 0; i < words.Length; i++)
            {
                string colored = ColorizeWord(words[i]);
                result = result == null ? colored : result + " " + colored;
            }
            return result;
        }

        private static string ColorizeWord(string word)
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
            if (lower == "lueur")
            {
                return Wrap(core, LueurColorHex) + trailing;
            }
            return word;
        }

        private static bool IsTrailingPunctuation(char c)
        {
            return c == '.' || c == ',' || c == ')' || c == ';' || c == ':';
        }

        /// <summary>
        /// "x2", "x3", ... — the literal multiplier factor token itself, e.g.
        /// in "x3 multiplier if...". Also catches the placeholder form "xn"
        /// (e.g. "xn multiplier where n is..." — Repetition, Gradient,
        /// Synergie, Densite), on explicit request: "X et N collé ne devrait
        /// pas arriver dans un mot normal" — "x" immediately followed by a
        /// single "n" and nothing else never occurs in normal English text,
        /// so it's safe to always treat it as this same token. Also catches
        /// a decimal factor like "x2.3" (a single "." with at least one
        /// digit on each side) — the progressive-modifier tooltip's
        /// "Currently xY.Z" line (see RunManager.GetProgressiveModifierStateText)
        /// uses exactly this format for CartesEnchantees/Experience.
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
