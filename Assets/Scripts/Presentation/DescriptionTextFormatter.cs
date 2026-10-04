namespace Contigu.Presentation
{
    /// <summary>
    /// Wraps every mention of points (pts/point/points) in blue and every
    /// mention of a multiplier (multiplier/multipliers/mult/xN — "mult"
    /// added once the genuine additive "+Mult" modifiers arrived, ninth
    /// batch) in red, across an upgrade/modifier/piece-trait
    /// description — on explicit request ("à chaque fois que le mot point
    /// apparait dans les description, que le mot soit bleu et idem pour le
    /// rouge et le multiplicateur"), matching the same blue=score/red=
    /// multiplier convention as the Balatro-style chips/mult pills (see
    /// ComboView). Relies on Unity's legacy Text component rich-text support
    /// (&lt;color=#RRGGBB&gt;...&lt;/color&gt;), on by default and never
    /// disabled anywhere in this project's UIFactory. Only recognizes the
    /// literal words themselves, not every synonym ("doubles", "+18 flat")
    /// — a handful of upgrade descriptions that phrase things differently
    /// simply stay uncolored.
    /// Descriptions that earn Lueur (the shop currency) no longer spell out
    /// the word at all — on explicit request ("changer le mot lueur dans
    /// les description de modifiers et upgrades pour le symbole losange
    /// jaune"), the literal Lueur-diamond glyph (◆, U+25C6) is embedded
    /// directly in those description strings instead, and THIS formatter
    /// colors it gold (same as the word used to be) and, on a further
    /// explicit request ("Grossis de 50% le symbole lueur dans les
    /// descriptions"), sizes it 50% larger than the surrounding text —
    /// hence <see cref="Colorize"/> now needs the caller's base font size,
    /// since the glyph's target size is relative to whatever context is
    /// displaying the description (tooltip, shop card, upgrade card, ...
    /// each uses a different base size).
    /// </summary>
    public static class DescriptionTextFormatter
    {
        private const string PointsColorHex = "65AED6"; // UITheme.ButtonSelected
        private const string MultiplierColorHex = "B56D7F"; // UITheme.Danger
        private const string DiamondColorHex = "F0B38D"; // VisualDefaults.GoldenColor
        private const string DiamondGlyph = "◆";
        private const float DiamondSizeMultiplier = 1.5f;

        public static string Colorize(string description, int fontSize)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            var words = description.Split(' ');
            string result = null;
            for (int i = 0; i < words.Length; i++)
            {
                string colored = ColorizeWord(words[i], fontSize);
                result = result == null ? colored : result + " " + colored;
            }
            return result;
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
            if (core == DiamondGlyph)
            {
                int diamondSize = (int)(fontSize * DiamondSizeMultiplier + 0.5f);
                return "<size=" + diamondSize + ">" + Wrap(core, DiamondColorHex) + "</size>" + trailing;
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
        /// Densite), on explicit request: "X et N collé ne devrait
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
