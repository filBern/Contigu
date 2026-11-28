using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Contigu.Core
{
    /// <summary>What an event can do to the run (see <see cref="EventEffect"/>).</summary>
    public enum EventEffectKind
    {
        Pollen,            // Amount > 0 gains, < 0 loses (never below 0 Pollen)
        GrantFertilizer,   // a random fertilizer the player does not hold yet
        LoseFertilizer,    // a random held fertilizer
        LevelUpFertilizer, // a random held fertilizer gains one level
        AddJoker,          // a Joker seed joins the bag
        RemoveSeeds,       // Amount seeds leave the bag (the bag never goes under its minimum size)
        NextRoundSeeds,    // Amount more (or fewer) seeds in the NEXT round's budget
        NextRoundEnemyHp   // the next round's enemies have Amount percent more (or less) HP
    }

    /// <summary>One thing an outcome does.</summary>
    public struct EventEffect
    {
        public EventEffectKind Kind;
        public int Amount;

        public EventEffect(EventEffectKind kind, int amount = 0)
        {
            Kind = kind;
            Amount = amount;
        }

        /// <summary>The effect as the player reads it (what the card shows, so what it says is exactly what it does).</summary>
        public string Describe()
        {
            switch (Kind)
            {
                case EventEffectKind.Pollen: return Amount >= 0 ? "gain " + Amount + " Pollen" : "lose " + (-Amount) + " Pollen";
                case EventEffectKind.GrantFertilizer: return "gain a random fertilizer";
                case EventEffectKind.LoseFertilizer: return "lose a random fertilizer";
                case EventEffectKind.LevelUpFertilizer: return "a random fertilizer gains a level";
                case EventEffectKind.AddJoker: return "a random Joker seed joins your bag";
                case EventEffectKind.RemoveSeeds: return "lose " + Amount + " random" + (Amount == 1 ? " seed" : " seeds") + " from your bag";
                case EventEffectKind.NextRoundSeeds: return Amount >= 0 ? "next round: +" + Amount + (Amount == 1 ? " seed" : " seeds") : "next round: " + Amount + (Amount == -1 ? " seed" : " seeds");
                default: return Amount >= 0 ? "next round: enemies have " + Amount + "% more HP" : "next round: enemies have " + (-Amount) + "% less HP";
            }
        }
    }

    /// <summary>One possible result of choosing an option, with its weight among the option's outcomes.</summary>
    public sealed class EventOutcome
    {
        public readonly int Weight;
        public readonly EventEffect[] Effects;

        public EventOutcome(int weight, params EventEffect[] effects)
        {
            Weight = weight;
            Effects = effects ?? new EventEffect[0];
        }
    }

    /// <summary>A choice on an event card. Every cost and every risk is shown, with its real probability; nothing hints which option is the good one.</summary>
    public sealed class EventOption
    {
        public readonly string Label;
        /// <summary>Pollen the player must have to pick this option (0 when it asks for none).</summary>
        public readonly int RequiredPollen;
        public readonly EventOutcome[] Outcomes;

        public EventOption(string label, int requiredPollen, params EventOutcome[] outcomes)
        {
            Label = label;
            RequiredPollen = requiredPollen;
            Outcomes = outcomes;
        }

        public int TotalWeight
        {
            get
            {
                int total = 0;
                foreach (var o in Outcomes) { total += o.Weight; }
                return total;
            }
        }

        /// <summary>"gain 14 Pollen, lose a random fertilizer" or "50%: a random fertilizer gains a level   |   50%: lose 6 Pollen".</summary>
        public string Describe()
        {
            return Describe(Outcomes);
        }

        /// <summary>The same card text for only the <paramref name="outcomes"/> that can really happen right now (the percentages are those among them).</summary>
        public string Describe(IList<EventOutcome> outcomes)
        {
            var parts = new List<string>();
            int total = 0;
            foreach (var o in outcomes) { total += o.Weight; }
            var percents = Percentages(outcomes, total);
            for (int i = 0; i < outcomes.Count; i++)
            {
                var effects = new List<string>();
                foreach (var e in outcomes[i].Effects) { effects.Add(e.Describe()); }
                string text = effects.Count == 0 ? "nothing happens" : string.Join(", ", effects.ToArray());
                if (outcomes.Count > 1)
                {
                    text = percents[i].ToString(CultureInfo.InvariantCulture) + "%: " + text;
                }
                parts.Add(text);
            }
            string joined = string.Join("   |   ", parts.ToArray());
            return joined.Length > 0 ? char.ToUpperInvariant(joined[0]) + joined.Substring(1) : joined;
        }

        /// <summary>Whole percentages that add up to exactly 100 (largest remainder), so three equal outcomes read 34, 33, 33 and not 33, 33, 33.</summary>
        public static int[] Percentages(IList<EventOutcome> outcomes, int total)
        {
            var result = new int[outcomes.Count];
            if (total <= 0) { return result; }
            var remainder = new int[outcomes.Count];
            int sum = 0;
            for (int i = 0; i < outcomes.Count; i++)
            {
                result[i] = 100 * outcomes[i].Weight / total;
                remainder[i] = 100 * outcomes[i].Weight % total;
                sum += result[i];
            }
            while (sum < 100)
            {
                int best = 0;
                for (int i = 1; i < outcomes.Count; i++) { if (remainder[i] > remainder[best]) { best = i; } }
                result[best]++;
                remainder[best] = -1;
                sum++;
            }
            return result;
        }
    }

    /// <summary>"Something happens": a short story from the garden with 2 or 3 options and a free way out.</summary>
    public sealed class EventDefinition
    {
        public readonly int Id;
        public readonly string Title;
        public readonly string Story;
        /// <summary>The last option is always the free "walk on", which may still carry a small side effect.</summary>
        public readonly EventOption[] Options;

        public EventDefinition(int id, string title, string story, params EventOption[] options)
        {
            Id = id;
            Title = title;
            Story = story;
            Options = options;
        }
    }
}
