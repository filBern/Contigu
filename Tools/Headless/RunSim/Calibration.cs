using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contigu.Core;

namespace Contigu.RunSim
{
    /// <summary>
    /// --calibrate: how far each unlock condition gets in one run, over many runs, so thresholds can be set from how a player actually plays.
    /// For each condition the report gives the value reached or beaten in 65 / 35 / 20 / 10 / 5 / 3 / 2 / 1 % of the runs.
    /// </summary>
    public static class Calibration
    {
        private static readonly Dictionary<string, List<int>> Values = new Dictionary<string, List<int>>();
        private static int _runs;

        public static void Add(UnlockTracker tracker)
        {
            _runs++;
            foreach (var entry in UnlockCatalog.All)
            {
                if (!Values.TryGetValue(entry.Key, out var list)) { list = new List<int>(); Values[entry.Key] = list; }
                list.Add(tracker.Best.TryGetValue(entry.Key, out int v) ? v : 0);
            }
        }

        public static void Write(string path)
        {
            var sb = new StringBuilder("key,kind,metric,scope,threshold,at_most,p65,p35,p20,p10,p05,p03,p02,p01,mean,current_share,text\n");
            foreach (var entry in UnlockCatalog.All)
            {
                var values = Values[entry.Key];
                var sorted = values.OrderByDescending(v => v).ToList();
                if (entry.Condition.AtMost) { sorted = values.OrderBy(v => v).ToList(); }
                int At(double share) { return sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(share * sorted.Count))]; }
                int meet = values.Count(v => entry.Condition.IsMet(v));
                sb.AppendLine(string.Join(",", entry.Key, entry.Kind, entry.Condition.Metric, entry.Condition.Scope, entry.Condition.Threshold, entry.Condition.AtMost ? 1 : 0,
                    At(0.65), At(0.35), At(0.20), At(0.10), At(0.05), At(0.03), At(0.02), At(0.01), values.Average().ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                    (100.0 * meet / values.Count).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%", entry.Condition.Text.Replace(',', ';')));
            }
            string target = path ?? "calibration.csv";
            System.IO.File.WriteAllText(target, sb.ToString());
            Console.WriteLine("\nCalibration of " + _runs + " runs written to " + target);
        }
    }
}
