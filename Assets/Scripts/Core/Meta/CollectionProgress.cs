using System.Collections.Generic;
using System.Text;

namespace Contigu.Core
{
    public enum CollectionCategory
    {
        Fertilizer,
        Sprout,
        Enemy,
        Weather
    }

    /// <summary>What the player has discovered so far across every run (the Collection screen): one set of ids per category, saved as plain text. Pure data, no I/O.</summary>
    public sealed class CollectionProgress
    {
        private readonly HashSet<int>[] _found;

        public CollectionProgress()
        {
            var categories = System.Enum.GetValues(typeof(CollectionCategory));
            _found = new HashSet<int>[categories.Length];
            for (int i = 0; i < _found.Length; i++)
            {
                _found[i] = new HashSet<int>();
            }
        }

        /// <summary>Marks an entry as discovered; true only the first time (so the caller knows what is new and whether to save).</summary>
        public bool Discover(CollectionCategory category, int id)
        {
            return _found[(int)category].Add(id);
        }

        public bool IsDiscovered(CollectionCategory category, int id)
        {
            return _found[(int)category].Contains(id);
        }

        public int Count(CollectionCategory category)
        {
            return _found[(int)category].Count;
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            for (int c = 0; c < _found.Length; c++)
            {
                var ids = new List<int>(_found[c]);
                ids.Sort();
                sb.Append(c).Append('=').Append(string.Join(",", ids.ConvertAll(i => i.ToString()).ToArray())).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Parses <see cref="ToText"/>; an empty or damaged text just gives an empty collection (a lost save must never block the game).</summary>
        public static CollectionProgress FromText(string text)
        {
            var progress = new CollectionProgress();
            if (string.IsNullOrEmpty(text))
            {
                return progress;
            }
            foreach (var line in text.Split('\n'))
            {
                int eq = line.IndexOf('=');
                int category;
                if (eq <= 0 || !int.TryParse(line.Substring(0, eq), out category) || category < 0 || category >= progress._found.Length)
                {
                    continue;
                }
                foreach (var part in line.Substring(eq + 1).Split(','))
                {
                    int id;
                    if (int.TryParse(part, out id))
                    {
                        progress._found[category].Add(id);
                    }
                }
            }
            return progress;
        }
    }
}
