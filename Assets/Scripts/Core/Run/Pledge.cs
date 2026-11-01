using System;

namespace Contigu.Core
{
    /// <summary>
    /// Optional self-imposed rules chosen before a run: each one still kept when the run is won pays one extra Star.
    /// They change nothing else about the run.
    /// </summary>
    [Flags]
    public enum Pledge
    {
        None = 0,
        NoShuffle = 1,
        NoReroll = 2,
        LeanBag = 4,
        Minimalist = 8
    }

    public static class PledgeCatalog
    {
        /// <summary>LeanBag: the Seed Bag holds at most this many seeds when the run is won.</summary>
        public const int LeanBagMaxSeeds = 14;

        /// <summary>Minimalist: never more than this many fertilizers held at once.</summary>
        public const int MinimalistMaxFertilizers = 2;

        public static readonly Pledge[] All = { Pledge.NoShuffle, Pledge.NoReroll, Pledge.LeanBag, Pledge.Minimalist };

        public static string Name(Pledge pledge)
        {
            switch (pledge)
            {
                case Pledge.NoShuffle: return "No Shuffle";
                case Pledge.NoReroll: return "No Reroll";
                case Pledge.LeanBag: return "Lean Bag";
                case Pledge.Minimalist: return "Minimalist";
                default: return "";
            }
        }

        public static string Description(Pledge pledge)
        {
            switch (pledge)
            {
                case Pledge.NoShuffle: return "Never shuffle your hand.";
                case Pledge.NoReroll: return "Never reroll the Blister.";
                case Pledge.LeanBag: return "Win with at most " + LeanBagMaxSeeds + " seeds in your bag.";
                case Pledge.Minimalist: return "Never hold more than " + MinimalistMaxFertilizers + " fertilizers at once.";
                default: return "";
            }
        }
    }
}
