using System.Collections.Generic;
using System.Text.RegularExpressions;
using Contigu.Core;

namespace Contigu.Presentation
{
    /// <summary>
    /// The few game words a tooltip can use that a new player may not know (Joker, Shuffle, Blighted...): when the text of a tooltip contains one, its one-line
    /// definition is added at the foot of the tooltip, so nothing has to be looked up elsewhere. Very common words (Mult, group, seed) are left to the tutorial.
    /// </summary>
    public static class TooltipGlossary
    {
        private struct Term
        {
            public Regex Pattern;
            public string Name;
            public string Definition;
        }

        private static readonly Regex Tags = new Regex("<[^>]+>", RegexOptions.Compiled);

        private static readonly Term[] Terms =
        {
            Make(@"\bjokers?\b", "Joker", "takes whichever color helps and carries ordinary sprout upgrades."),
            Make(@"\bshuffles?\b", "Shuffle", "renews your hand. You only get a few."),
            Make(@"\bsprouts?\b", "Sprout", "a seed tile that carries an upgrade."),
            Make(@"\bblighted\b", "Blighted", "a fertilizer with no effect until the next Shuffle."),
            Make(@"\bfrozen\b", "Frozen", "a locked plot. Harvest its line to break the ice."),
            Make(@"\bgolden (plots?|cells?|tiles?)\b", "Golden plot", "+" + ScoringConstants.GoldenCellBonus + " pts when it joins the scored group."),
        };

        private static Term Make(string pattern, string name, string definition)
        {
            return new Term { Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), Name = name, Definition = definition };
        }

        /// <summary>The definitions (at most <paramref name="max"/>, in the order the words first appear) of the glossary words found in <paramref name="text"/>; empty when none.</summary>
        public static List<(string Name, string Definition)> Lookup(string text, int max = 2)
        {
            var found = new List<(int At, string Name, string Definition)>();
            if (!string.IsNullOrEmpty(text))
            {
                string plain = Tags.Replace(text, string.Empty);
                for (int i = 0; i < Terms.Length; i++)
                {
                    var match = Terms[i].Pattern.Match(plain);
                    if (match.Success) { found.Add((match.Index, Terms[i].Name, Terms[i].Definition)); }
                }
            }
            found.Sort((a, b) => a.At.CompareTo(b.At));
            var result = new List<(string, string)>();
            for (int i = 0; i < found.Count && result.Count < max; i++)
            {
                result.Add((found[i].Name, found[i].Definition));
            }
            return result;
        }
    }
}
