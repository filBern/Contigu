using System.Collections.Generic;
using System.Text;

namespace Contigu.Core
{
    /// <summary>
    /// What the player has unlocked so far across every run, plus the running totals the "in total" conditions need. Pure data saved as plain
    /// text (see <see cref="ToText"/>). Anything not in <see cref="UnlockCatalog"/> is always available.
    /// </summary>
    public sealed class UnlockState
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly Dictionary<UnlockMetric, int> _totals = new Dictionary<UnlockMetric, int>();
        private readonly List<string> _queued = new List<string>();

        /// <summary>When true everything counts as available (tests, simulator, the dev shortcut) while unlocks are still recorded, so coverage can be measured.</summary>
        public bool EverythingAvailable { get; private set; }

        public UnlockState(bool everythingAvailable = false)
        {
            EverythingAvailable = everythingAvailable;
        }

        public bool IsUnlocked(UnlockKind kind, int id)
        {
            if (EverythingAvailable)
            {
                return true;
            }
            return UnlockCatalog.Find(kind, id) == null || _unlocked.Contains(UnlockCatalog.KeyOf(kind, id));
        }

        /// <summary>Whether the unlock was actually earned (recorded), regardless of the everything-available switch.</summary>
        public bool IsEarned(string key)
        {
            return _unlocked.Contains(key);
        }

        /// <summary>Records an unlock; true only the first time.</summary>
        public bool Unlock(string key)
        {
            return _unlocked.Add(key);
        }

        /// <summary>Conditions already met whose unlock is held back (a run only releases so many, see <see cref="UnlockTracker.MaxUnlocksPerRun"/>); they arrive at the start of the next runs, oldest first.</summary>
        public IReadOnlyList<string> Queued
        {
            get { return _queued; }
        }

        public void Enqueue(string key)
        {
            if (!_unlocked.Contains(key) && !_queued.Contains(key))
            {
                _queued.Add(key);
            }
        }

        /// <summary>Takes the oldest held-back unlock out of the queue and unlocks it; null when the queue is empty.</summary>
        public string ReleaseNextQueued()
        {
            while (_queued.Count > 0)
            {
                string key = _queued[0];
                _queued.RemoveAt(0);
                if (_unlocked.Add(key))
                {
                    return key;
                }
            }
            return null;
        }

        public int EarnedCount
        {
            get { return _unlocked.Count; }
        }

        public int Total(UnlockMetric metric)
        {
            int value;
            return _totals.TryGetValue(metric, out value) ? value : 0;
        }

        public void AddTotal(UnlockMetric metric, int amount)
        {
            _totals[metric] = Total(metric) + amount;
        }

        /// <summary>Unlocks every challenge, sprout and fertilizer for good (the dev shortcut).</summary>
        public void UnlockEverything()
        {
            foreach (var entry in UnlockCatalog.All)
            {
                _unlocked.Add(entry.Key);
            }
            _queued.Clear();
        }

        public string ToText()
        {
            var keys = new List<string>(_unlocked);
            keys.Sort(System.StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append("U=").Append(string.Join(",", keys.ToArray())).Append('\n');
            sb.Append("Q=").Append(string.Join(",", _queued.ToArray())).Append('\n');
            sb.Append("T=");
            bool first = true;
            foreach (var pair in _totals)
            {
                if (!first) { sb.Append(','); }
                sb.Append((int)pair.Key).Append(':').Append(pair.Value);
                first = false;
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>Parses <see cref="ToText"/>; an empty or damaged text just gives a fresh state (a lost save must never block the game).</summary>
        public static UnlockState FromText(string text)
        {
            var state = new UnlockState();
            if (string.IsNullOrEmpty(text))
            {
                return state;
            }
            foreach (var line in text.Split('\n'))
            {
                if (line.StartsWith("U="))
                {
                    foreach (var part in line.Substring(2).Split(','))
                    {
                        if (UnlockCatalog.FindByKey(part) != null)
                        {
                            state._unlocked.Add(part);
                        }
                    }
                }
                else if (line.StartsWith("Q="))
                {
                    foreach (var part in line.Substring(2).Split(','))
                    {
                        if (UnlockCatalog.FindByKey(part) != null && !state._queued.Contains(part))
                        {
                            state._queued.Add(part);
                        }
                    }
                }
                else if (line.StartsWith("T="))
                {
                    foreach (var part in line.Substring(2).Split(','))
                    {
                        int colon = part.IndexOf(':');
                        int metric, value;
                        if (colon > 0 && int.TryParse(part.Substring(0, colon), out metric) && int.TryParse(part.Substring(colon + 1), out value)
                            && System.Enum.IsDefined(typeof(UnlockMetric), metric))
                        {
                            state._totals[(UnlockMetric)metric] = value;
                        }
                    }
                }
            }
            return state;
        }
    }
}
