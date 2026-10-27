using System;
using System.Collections.Generic;
using Contigu.Core;

namespace Contigu.RunSim
{
    /// <summary>
    /// QA: plays short runs where every run holds ONE specific fertilizer, then ONE specific sprout upgrade on every seed of the bag,
    /// with the invariants checked after each action — so a crash or an impossible state inside a single piece of content shows up
    /// by name instead of being lost in the noise of random full runs. Usage: --each [--runs N per item] [--seed S]
    /// </summary>
    public static class Smoke
    {
        public static int Run(int runsPerItem, int seed)
        {
            Invariants.Enabled = true;
            if (runsPerItem > 40) { runsPerItem = 12; }
            int problems = 0;
            foreach (var def in ModifierCatalog.All)
            {
                var id = def.Id;
                problems += Play("fertilizer " + id + " (" + def.Name + ")", runsPerItem, seed, r => r.DebugGrantModifier(id));
            }
            foreach (var up in UpgradeCatalog.GridPool)
            {
                var kind = UpgradeSystem.TraitKindFor(up.Id);
                if (!kind.HasValue) { continue; }
                var k = kind.Value;
                problems += Play("sprout " + up.Id + " (" + up.Name + ")", runsPerItem, seed, r =>
                {
                    var all = new List<int>();
                    for (int i = 0; i < r.Deck.DeckCount; i++) { all.Add(i); }
                    r.Deck.TagSpecificTokens(all, k, new SystemRandomProvider(seed));
                });
            }
            Console.WriteLine(problems == 0 ? "\nEvery fertilizer and sprout played cleanly." : "\n" + problems + " item(s) with problems.");
            return problems == 0 ? 0 : 1;
        }

        private static int Play(string label, int runs, int seed, Action<RunManager> setup)
        {
            var reasons = new Dictionary<string, int>();
            foreach (var challenge in new[] { ChallengeCatalog.Classic, ChallengeCatalog.Orchard })
            {
                for (int r = 0; r < runs; r++)
                {
                    int runSeed = seed + r;
                    var bot = new Bot(runSeed, r % 2 == 0 ? BotPolicy.Greedy : BotPolicy.Random, challenge) { Setup = setup };
                    string reason = null;
                    try
                    {
                        var res = bot.PlayRun(runSeed);
                        if (res.Stuck) { reason = res.StuckReason; }
                    }
                    catch (Exception e)
                    {
                        reason = "exception: " + e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n')[0].Trim();
                    }
                    if (reason != null)
                    {
                        string key = challenge.Id + ": " + reason;
                        int n;
                        reasons.TryGetValue(key, out n);
                        reasons[key] = n + 1;
                    }
                }
            }
            if (reasons.Count == 0) { return 0; }
            Console.WriteLine(label);
            foreach (var kv in reasons) { Console.WriteLine("   " + kv.Value + "x " + kv.Key); }
            return 1;
        }
    }
}
