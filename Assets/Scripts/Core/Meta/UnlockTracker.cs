using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Watches a run and unlocks what its conditions ask for (see <see cref="UnlockCatalog"/>). RunManager feeds it observations; each one is
    /// compared with the conditions of every still-locked entry watching that metric. Pure logic, no I/O.
    /// </summary>
    public sealed class UnlockTracker
    {
        /// <summary>Calibration (RunSim --calibrate): nothing is unlocked or removed from the watch lists, and the best value each condition reached is recorded instead.</summary>
        public static bool CalibrationMode;

        /// <summary>Calibration only: per catalog entry key, the per-run best of the compared value (the maximum, or the minimum for an at-most condition).</summary>
        public readonly Dictionary<string, int> Best = new Dictionary<string, int>();

        /// <summary>How many things one run can unlock; further conditions met in that run are queued and arrive at the start of the next runs (so one great run never unlocks half the game).</summary>
        public const int MaxUnlocksPerRun = 8;

        private int _unlockedThisRun;

        private readonly UnlockState _state;
        private readonly int _metricCount = System.Enum.GetValues(typeof(UnlockMetric)).Length;
        private readonly int[] _round;
        private readonly int[] _run;
        private readonly bool[] _keepTotal;
        private readonly List<UnlockEntry>[] _watch;
        private readonly List<UnlockEntry> _fresh = new List<UnlockEntry>();

        // Streaks across the placements of one round.
        private ShapeId? _prevShape;
        private PieceColor? _prevColor;
        private int _sameShape, _sameColor, _differentColor, _harvest, _bigger;
        private int _prevGroup = -1;

        public UnlockTracker(UnlockState state)
        {
            _state = state;
            _round = new int[_metricCount];
            _run = new int[_metricCount];
            _keepTotal = new bool[_metricCount];
            _watch = new List<UnlockEntry>[_metricCount];
            foreach (var entry in UnlockCatalog.All)
            {
                if (_state.IsEarned(entry.Key))
                {
                    continue;
                }
                int m = (int)entry.Condition.Metric;
                if (_watch[m] == null)
                {
                    _watch[m] = new List<UnlockEntry>();
                }
                _watch[m].Add(entry);
            }
            foreach (var entry in UnlockCatalog.All)
            {
                if (entry.Condition.Scope == UnlockScope.Total)
                {
                    _keepTotal[(int)entry.Condition.Metric] = true;
                }
            }
        }

        public UnlockState State
        {
            get { return _state; }
        }

        /// <summary>What got unlocked since the last call (the caller saves it and announces it), oldest first.</summary>
        public List<UnlockEntry> DrainNewlyUnlocked()
        {
            var copy = new List<UnlockEntry>(_fresh);
            _fresh.Clear();
            return copy;
        }

        public bool HasNewlyUnlocked
        {
            get { return _fresh.Count > 0; }
        }

        public void StartRun()
        {
            System.Array.Clear(_run, 0, _run.Length);
            _unlockedThisRun = 0;
            // What the last runs earned beyond the limit arrives now.
            while (_unlockedThisRun < MaxUnlocksPerRun && !CalibrationMode)
            {
                string key = _state.ReleaseNextQueued();
                if (key == null)
                {
                    break;
                }
                var entry = UnlockCatalog.FindByKey(key);
                if (entry != null)
                {
                    _fresh.Add(entry);
                    _unlockedThisRun++;
                }
            }
            StartRound();
        }

        public void StartRound()
        {
            System.Array.Clear(_round, 0, _round.Length);
            _prevShape = null;
            _prevColor = null;
            _sameShape = _sameColor = _differentColor = _harvest = _bigger = 0;
            _prevGroup = -1;
        }

        /// <summary>One observation of a metric. How it counts depends on each watching condition's scope.</summary>
        public void Observe(UnlockMetric metric, int value)
        {
            int m = (int)metric;
            _round[m] += value;
            _run[m] += value;
            if (_keepTotal[m])
            {
                _state.AddTotal(metric, value);
            }
            var watchers = _watch[m];
            if (watchers == null)
            {
                return;
            }
            for (int i = watchers.Count - 1; i >= 0; i--)
            {
                var entry = watchers[i];
                int compared;
                switch (entry.Condition.Scope)
                {
                    case UnlockScope.Round: compared = _round[m]; break;
                    case UnlockScope.Run: compared = _run[m]; break;
                    case UnlockScope.Total: compared = CalibrationMode ? _run[m] : _state.Total(metric); break;
                    default: compared = value; break;
                }
                if (CalibrationMode)
                {
                    int bestSoFar;
                    if (!Best.TryGetValue(entry.Key, out bestSoFar)
                        || (entry.Condition.AtMost ? compared < bestSoFar : compared > bestSoFar))
                    {
                        Best[entry.Key] = compared;
                    }
                    continue;
                }
                if (entry.Condition.IsMet(compared))
                {
                    watchers.RemoveAt(i);
                    if (_unlockedThisRun >= MaxUnlocksPerRun)
                    {
                        _state.Enqueue(entry.Key);
                    }
                    else if (_state.Unlock(entry.Key))
                    {
                        _fresh.Add(entry);
                        _unlockedThisRun++;
                    }
                }
            }
        }

        /// <summary>Feeds the sequence metrics (streaks, shape changes) of one more placement in the round.</summary>
        public void ObserveSequence(ShapeId shape, PieceColor color, int lines, int groupSize)
        {
            if (_prevShape.HasValue && _prevShape.Value != shape)
            {
                Observe(UnlockMetric.ShapeChanges, 1);
            }
            _sameShape = _prevShape.HasValue && _prevShape.Value == shape ? _sameShape + 1 : 1;
            _sameColor = _prevColor.HasValue && _prevColor.Value == color ? _sameColor + 1 : 1;
            _differentColor = _prevColor.HasValue && _prevColor.Value != color ? _differentColor + 1 : 1;
            _harvest = lines > 0 ? _harvest + 1 : 0;
            _bigger = _prevGroup >= 0 && groupSize > _prevGroup ? _bigger + 1 : 1;
            _prevShape = shape;
            _prevColor = color;
            _prevGroup = groupSize;
            Observe(UnlockMetric.SameShapeStreak, _sameShape);
            Observe(UnlockMetric.SameColorStreak, _sameColor);
            Observe(UnlockMetric.DifferentColorStreak, _differentColor);
            Observe(UnlockMetric.HarvestStreak, _harvest);
            Observe(UnlockMetric.BiggerGroupStreak, _bigger);
        }
    }
}
