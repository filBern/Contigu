using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Contigu.Core;

namespace Contigu.RunSim
{
    /// <summary>
    /// dotnet run -- [--runs N] [--seed S] [--policy greedy|random|both] [--csv path] [--fail-on-error] [--challenge name] [--endless]
    /// Plays N full Classic runs per policy against the real Core rules and prints balance statistics.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            int runs = 200;
            int seed = 1;
            string policyArg = "both";
            string csvPath = null;
            bool failOnError = false;
            bool endless = false;
            var challenge = ChallengeCatalog.Classic;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--runs" && i + 1 < args.Length) { runs = int.Parse(args[++i]); }
                else if (args[i] == "--seed" && i + 1 < args.Length) { seed = int.Parse(args[++i]); }
                else if (args[i] == "--policy" && i + 1 < args.Length) { policyArg = args[++i]; }
                else if (args[i] == "--csv" && i + 1 < args.Length) { csvPath = args[++i]; }
                else if (args[i] == "--fail-on-error") { failOnError = true; }
                else if (args[i] == "--endless") { endless = true; }
                else if (args[i] == "--challenge" && i + 1 < args.Length)
                {
                    string wanted = args[++i];
                    foreach (var candidate in ChallengeCatalog.All)
                    {
                        if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Id.ToString(), wanted, StringComparison.OrdinalIgnoreCase))
                        {
                            challenge = candidate;
                        }
                    }
                }
            }

            var policies = new List<BotPolicy>();
            if (policyArg == "random" || policyArg == "both") { policies.Add(BotPolicy.Random); }
            if (policyArg == "greedy" || policyArg == "both") { policies.Add(BotPolicy.Greedy); }

            int errorCount = 0;
            var csv = new StringBuilder("policy,seed,victory,rounds_cleared,total_score,placements,shuffles,pollen_spent,pollen_earned,stuck,death_enemies,modifiers\n");
            foreach (var policy in policies)
            {
                var watch = Stopwatch.StartNew();
                var results = new List<RunResult>();
                for (int r = 0; r < runs; r++)
                {
                    int runSeed = seed + r;
                    var bot = new Bot(runSeed, policy, challenge, endless);
                    RunResult res;
                    try
                    {
                        res = bot.PlayRun(runSeed);
                    }
                    catch (Exception e)
                    {
                        res = new RunResult { Stuck = true, StuckReason = "exception: " + e.GetType().Name + ": " + e.Message };
                        Console.Error.WriteLine("seed " + runSeed + " (" + policy + "): " + e);
                    }
                    results.Add(res);
                    if (res.Stuck) { errorCount++; }
                    csv.AppendLine(string.Join(",", policy, runSeed, res.Victory ? 1 : 0, res.RoundsCleared, res.TotalScore, res.PlacementsMade, res.ShufflesUsed,
                        res.PollenSpent, res.PollenEarnedEstimate, res.Stuck ? res.StuckReason.Replace(',', ';') : "", string.Join("|", res.DeathEncounter), string.Join("|", res.FinalModifiers)));
                }
                watch.Stop();
                Report(policy, results, watch.Elapsed);
            }
            if (csvPath != null)
            {
                System.IO.File.WriteAllText(csvPath, csv.ToString());
                Console.WriteLine("\nCSV written to " + csvPath);
            }
            return failOnError && errorCount > 0 ? 1 : 0;
        }

        private static void Report(BotPolicy policy, List<RunResult> results, TimeSpan elapsed)
        {
            int n = results.Count;
            int wins = results.Count(r => r.Victory);
            int stuck = results.Count(r => r.Stuck);
            Console.WriteLine();
            Console.WriteLine("=== Policy: " + policy + " — " + n + " runs in " + elapsed.TotalSeconds.ToString("0.0") + "s ===");
            Console.WriteLine("Victories: " + wins + " (" + (100.0 * wins / n).ToString("0.0") + "%)   Stuck/errors: " + stuck);
            Console.WriteLine("Avg rounds cleared: " + results.Average(r => r.RoundsCleared).ToString("0.00")
                + "   Avg total score: " + results.Average(r => (double)r.TotalScore).ToString("0")
                + "   Avg placements: " + results.Average(r => r.PlacementsMade).ToString("0")
                + "   Avg shuffles: " + results.Average(r => r.ShufflesUsed).ToString("0.0"));
            Console.WriteLine("Avg Pollen earned (placement gains): " + results.Average(r => r.PollenEarnedEstimate).ToString("0")
                + "   Avg Pollen spent: " + results.Average(r => r.PollenSpent).ToString("0"));

            Console.WriteLine("\nRound reached (runs ending there):");
            for (int round = 0; round <= RunConfig.RoundCount; round++)
            {
                int count = results.Count(r => !r.Victory && !r.Stuck && r.RoundsCleared == round);
                if (count > 0)
                {
                    var encounter = round < RunConfig.RoundCount ? string.Join("+", EncounterCatalog.GetEncounter(ChallengeId.Classic, round)) : "";
                    Console.WriteLine("  died in round " + (round + 1).ToString().PadLeft(2) + ": " + count.ToString().PadLeft(4) + "  " + new string('#', Math.Min(60, count * 60 / n)) + "  " + encounter);
                }
            }
            if (wins > 0)
            {
                Console.WriteLine("  victories     : " + wins.ToString().PadLeft(4));
            }

            var defeats = results.Where(r => !r.Victory && !r.Stuck).ToList();
            if (defeats.Count > 0)
            {
                Console.WriteLine("\nDefeats: " + defeats.Count(d => d.DeathCause == "out of seeds") + " out of seeds, " + defeats.Count(d => d.DeathCause == "board blocked")
                    + " board blocked; avg enemy HP left on defeat: " + defeats.Average(d => d.EnemyHpLeftPct).ToString("0") + "%");
            }

            Console.WriteLine("\nCleared-round rate (reached round N and cleared it):");
            for (int round = 1; round <= Math.Max(RunConfig.RoundCount, results.Max(r => r.RoundsCleared)); round++)
            {
                int reached = results.Count(r => r.RoundsCleared >= round - 1 && !r.Stuck);
                int cleared = results.Count(r => r.RoundsCleared >= round && !r.Stuck);
                if (reached == 0) { break; }
                Console.WriteLine("  round " + round.ToString().PadLeft(2) + ": " + (100.0 * cleared / reached).ToString("0").PadLeft(3) + "%  (" + cleared + "/" + reached + ")");
            }

            var modWins = new Dictionary<ModifierId, (int Held, int Won, double Rounds)>();
            foreach (var r in results.Where(r => !r.Stuck))
            {
                foreach (var m in r.FinalModifiers.Distinct())
                {
                    modWins.TryGetValue(m, out var cur);
                    modWins[m] = (cur.Held + 1, cur.Won + (r.Victory ? 1 : 0), cur.Rounds + r.RoundsCleared);
                }
            }
            double overall = results.Where(r => !r.Stuck).Average(r => (double)r.RoundsCleared);
            Console.WriteLine("\nFertilizers held at the end (min 8 runs), by avg rounds cleared vs overall " + overall.ToString("0.00") + ":");
            foreach (var kv in modWins.Where(k => k.Value.Held >= 8).OrderByDescending(k => k.Value.Rounds / k.Value.Held).Take(8))
            {
                Console.WriteLine("  + " + kv.Key.ToString().PadRight(24) + " held in " + kv.Value.Held.ToString().PadLeft(4) + " runs, avg rounds " + (kv.Value.Rounds / kv.Value.Held).ToString("0.00"));
            }
            foreach (var kv in modWins.Where(k => k.Value.Held >= 8).OrderBy(k => k.Value.Rounds / k.Value.Held).Take(5))
            {
                Console.WriteLine("  - " + kv.Key.ToString().PadRight(24) + " held in " + kv.Value.Held.ToString().PadLeft(4) + " runs, avg rounds " + (kv.Value.Rounds / kv.Value.Held).ToString("0.00"));
            }

            foreach (var group in results.Where(r => r.Stuck).GroupBy(r => r.StuckReason).OrderByDescending(g => g.Count()).Take(5))
            {
                Console.WriteLine("\nStuck/error (" + group.Count() + "x): " + group.Key);
            }
        }
    }
}
