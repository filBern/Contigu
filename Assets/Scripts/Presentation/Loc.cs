using System.Collections.Generic;

namespace Contigu.Presentation
{
    /// <summary>
    /// Player-visible strings of the shop and its choices, looked up by key so a translation can replace them without touching layout code.
    /// English is the built-in table; <see cref="Override"/> swaps in another language's entries (missing keys fall back to English).
    /// Placeholders are {0}, {1}...; a label's width is never assumed (badges and rows size to their text).
    /// </summary>
    public static class Loc
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "shop.header", "The Pollen Shop" },
            { "shop.blister", "Blister" },
            { "shop.workshop", "Workshop" },
            { "shop.reroll", "Reroll ({0})" },
            { "shop.nextRound", "Next round" },
            { "shop.seedBag", "Show seed bag (Tab)" },
            { "shop.sold", "Sold" },
            { "shop.full", "Full ({0})" },
            { "shop.minBag", "Min bag" },
            { "shop.tapAgain", "Tap again to buy" },
            { "shop.needPollen", "Not enough Pollen: {0} more needed" },
            { "shop.priceNote", "Every purchase makes the other prices rise for this visit." },
            { "shop.unavailable", "N/A" },
            { "shop.unavailableWhy", "Nothing to use it on right now" },
            { "shop.fullWhy", "Your garden already holds {0} fertilizers" },
            { "shop.minBagWhy", "Your seed bag is already at its minimum size" },
            { "shop.workshopPack", "Workshop · pack" },
            { "shop.workshopService", "Workshop · service" },
            { "shop.fertilizer", "Fertilizer · {0}" },
            { "shop.upgrade", "Upgrade · {0} · {1}" },
            { "shop.forgeTitle", "Forge: {0}" },
            { "shop.forgeBody", "{0}: {1}\nAdd it to 3 seeds of your bag, of your choice." },
            { "choice.selectSeeds", "Select {0} seeds for {1}" },
            { "choice.selectSeed", "Select {0} seed for {1}" },
            { "choice.sproutPack", "Sprout Pack" },
            { "choice.sproutPackHint", "Pick one, then choose the seed that gets it" },
            { "choice.fertilizerPack", "Fertilizer Pack" },
            { "choice.fertilizerPackHint", "Pick one fertilizer to keep" },
            { "choice.skipPack", "Skip (no refund)" },
            { "choice.abandon", "Skip (no refund)" },
            { "choice.abandonConfirm", "Sure? Click again (no refund)" },
            { "choice.sproutTag", "Sprout · {0}" },
            { "choice.fertilizerTag", "Fertilizer · {0}" },
            { "choice.pickSeed", "Choose 1 seed to add" },
            { "choice.pickMastery", "Choose a mastery" },
            { "choice.pickCompost", "Choose 1 seed to remove" },
            { "choice.masteryBody", "Level {0} to {1} (+{2})\n+{3} pts per tile" },
            { "choice.masteryTag", "Mastery" },
            { "settings.motionOff", "Reduced motion: Off" },
            { "settings.motionOn", "Reduced motion: On" },
        };

        private static Dictionary<string, string> _override;

        /// <summary>Replaces entries (typically a whole language); pass null to go back to English.</summary>
        public static void Override(Dictionary<string, string> entries)
        {
            _override = entries;
        }

        public static string T(string key, params object[] args)
        {
            string text;
            if ((_override == null || !_override.TryGetValue(key, out text)) && !English.TryGetValue(key, out text))
            {
                text = key;
            }
            return args != null && args.Length > 0 ? string.Format(text, args) : text;
        }
    }
}
