using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contigu.Core;

namespace Contigu.RunSim
{
    /// <summary>
    /// Balance study: how much does ONE item change a run? Every fertilizer (held from the start) and every sprout upgrade (on three seeds
    /// of the bag) is played over the same seeds as a baseline run; the paired difference in rounds cleared and in victories ranks the items.
    /// Usage: --study [--runs N per item] [--seed S] [--policy greedy|planner] [--csv path]
    /// It measures what a bot gets out of an item, not what a person would: use it to spot items far above or below the rest, never as advice to players.
    /// </summary>
    public static class Study
    {
        private sealed class Row
        {
            public string Kind;
            public string Id;
            public string Name;
            public double MeanDeltaRounds;
            public double StdErr;
            public double WinRate;
            public double DeltaWins;
            public double MeanRounds;
        }

        public static int Run(int runs, int seed, BotPolicy policy, string csvPath)
        {
            var challenge = ChallengeCatalog.Classic;
            var baseRounds = new double[runs];
            int baseWins = 0;
            for (int r = 0; r < runs; r++)
            {
                var res = Play(seed + r, policy, challenge, null);
                baseRounds[r] = res.Item1;
                if (res.Item2) { baseWins++; }
            }
            double baseMean = baseRounds.Average();
            Console.WriteLine("Baseline (" + policy + ", " + runs + " runs, seed " + seed + "): " + baseMean.ToString("0.00") + " rounds cleared, " + (100.0 * baseWins / runs).ToString("0.0") + "% wins\n");

            var rows = new List<Row>();
            foreach (var def in ModifierCatalog.All)
            {
                var id = def.Id;
                rows.Add(Measure("fertilizer", id.ToString(), def.Name, runs, seed, policy, challenge, baseRounds, baseWins, r => r.DebugGrantModifier(id)));
            }
            foreach (var up in UpgradeCatalog.GridPool)
            {
                var kind = UpgradeSystem.TraitKindFor(up.Id);
                if (!kind.HasValue) { continue; }
                var k = kind.Value;
                rows.Add(Measure("sprout", up.Id.ToString(), up.Name, runs, seed, policy, challenge, baseRounds, baseWins, r =>
                {
                    var picks = new List<int>();
                    var rng = new Random(seed ^ 0x2468);
                    int count = Math.Min(3, r.Deck.DeckCount);
                    while (picks.Count < count)
                    {
                        int pick = rng.Next(r.Deck.DeckCount);
                        if (!picks.Contains(pick)) { picks.Add(pick); }
                    }
                    r.Deck.TagSpecificTokens(picks, k, new SystemRandomProvider(seed));
                }));
            }

            rows = rows.OrderByDescending(x => x.MeanDeltaRounds).ToList();
            var sb = new StringBuilder("kind,id,name,mean_delta_rounds,std_err,z,win_rate,delta_wins_pts,mean_rounds\n");
            Console.WriteLine("Strongest items (rounds cleared vs baseline, paired by seed; z = delta / standard error):");
            Print(rows.Take(15));
            Console.WriteLine("\nWeakest items:");
            Print(Enumerable.Reverse(rows).Take(15));
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Kind, row.Id, row.Name.Replace(',', ';'), row.MeanDeltaRounds.ToString("0.000"), row.StdErr.ToString("0.000"),
                    (row.StdErr > 0 ? row.MeanDeltaRounds / row.StdErr : 0).ToString("0.0"), row.WinRate.ToString("0.0"), row.DeltaWins.ToString("0.0"), row.MeanRounds.ToString("0.00")));
            }
            int significantlyHigh = rows.Count(x => x.StdErr > 0 && x.MeanDeltaRounds / x.StdErr > 2.5);
            int significantlyLow = rows.Count(x => x.StdErr > 0 && x.MeanDeltaRounds / x.StdErr < -2.5);
            Console.WriteLine("\n" + rows.Count + " items; " + significantlyHigh + " clearly above baseline, " + significantlyLow + " clearly below (|z| > 2.5; with this many items a few are chance).");
            if (csvPath != null)
            {
                System.IO.File.WriteAllText(csvPath, sb.ToString());
                Console.WriteLine("CSV written to " + csvPath);
            }
            return 0;
        }

        private static void Print(IEnumerable<Row> rows)
        {
            foreach (var row in rows)
            {
                double z = row.StdErr > 0 ? row.MeanDeltaRounds / row.StdErr : 0;
                Console.WriteLine("  " + (row.Kind + " " + row.Name).PadRight(44) + " " + row.MeanDeltaRounds.ToString("+0.00;-0.00").PadLeft(6) + " rounds   z " + z.ToString("+0.0;-0.0").PadLeft(5) + "   wins " + row.WinRate.ToString("0.0") + "% (" + row.DeltaWins.ToString("+0.0;-0.0") + " pts)");
            }
        }

        private static Row Measure(string kind, string id, string name, int runs, int seed, BotPolicy policy, ChallengeDefinition challenge, double[] baseRounds, int baseWins, Action<RunManager> setup)
        {
            var diffs = new double[runs];
            double total = 0;
            int wins = 0;
            for (int r = 0; r < runs; r++)
            {
                var res = Play(seed + r, policy, challenge, setup);
                diffs[r] = res.Item1 - baseRounds[r];
                total += res.Item1;
                if (res.Item2) { wins++; }
            }
            double mean = diffs.Average();
            double variance = diffs.Sum(d => (d - mean) * (d - mean)) / Math.Max(1, runs - 1);
            return new Row
            {
                Kind = kind,
                Id = id,
                Name = name,
                MeanDeltaRounds = mean,
                StdErr = Math.Sqrt(variance / runs),
                WinRate = 100.0 * wins / runs,
                DeltaWins = 100.0 * (wins - baseWins) / runs,
                MeanRounds = total / runs
            };
        }

        private static Tuple<int, bool> Play(int runSeed, BotPolicy policy, ChallengeDefinition challenge, Action<RunManager> setup)
        {
            var bot = new Bot(runSeed, policy, challenge) { Setup = setup };
            try
            {
                var res = bot.PlayRun(runSeed);
                return Tuple.Create(res.RoundsCleared, res.Victory);
            }
            catch (Exception)
            {
                return Tuple.Create(0, false);
            }
        }
    }
}
