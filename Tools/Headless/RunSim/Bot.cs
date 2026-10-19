using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.RunSim
{
    /// <summary>The bot's way of choosing placements and shop purchases.</summary>
    public enum BotPolicy
    {
        /// <summary>Every legal placement is equally likely; buys whatever it can afford at random.</summary>
        Random,

        /// <summary>Takes the best-scoring placement (group score, completed lines, tidy edges); buys modifiers first.</summary>
        Greedy
    }

    /// <summary>Everything one simulated run reports back.</summary>
    public sealed class RunResult
    {
        public bool Victory;
        public int RoundsCleared;
        public int TotalScore;
        public int PlacementsMade;
        public int ShufflesUsed;
        public int PollenSpent;
        public int PollenEarnedEstimate;
        public string DeathCause;
        public double EnemyHpLeftPct;
        public bool Stuck;
        public string StuckReason;
        public List<EnemyId> DeathEncounter = new List<EnemyId>();
        public List<ModifierId> FinalModifiers = new List<ModifierId>();
        public List<int> PlacementsPerRound = new List<int>();
    }

    public sealed class Bot
    {
        private readonly Random _rng;
        private readonly BotPolicy _policy;

        public Bot(int seed, BotPolicy policy)
        {
            _rng = new Random(seed ^ 0x5bd1e995);
            _policy = policy;
        }

        public RunResult PlayRun(int seed)
        {
            var run = new RunManager(new SystemRandomProvider(seed));
            var result = new RunResult();
            int placementsThisRound = 0;
            int guard = 0;

            while (run.State != RunState.RunVictory && run.State != RunState.RunDefeat)
            {
                if (++guard > 200000)
                {
                    result.Stuck = true;
                    result.StuckReason = "step guard";
                    break;
                }

                if (run.State == RunState.AwaitingShop)
                {
                    result.PlacementsPerRound.Add(placementsThisRound);
                    placementsThisRound = 0;
                    result.RoundsCleared = run.CurrentRoundIndex + 1;
                    if (!ShopPhase(run, result))
                    {
                        result.Stuck = true;
                        result.StuckReason = "shop";
                        break;
                    }
                    continue;
                }

                if (!TryPlaceBest(run, out int lueurBefore))
                {
                    if (run.ShufflesRemaining > 0 && run.ShuffleHand())
                    {
                        result.ShufflesUsed++;
                        continue;
                    }
                    // Nothing playable and no shuffle left: the engine ends the run on its own; if it did not, bail out.
                    if (run.State == RunState.InProgress)
                    {
                        result.Stuck = true;
                        result.StuckReason = "no move";
                        break;
                    }
                    continue;
                }
                placementsThisRound++;
                result.PlacementsMade++;
                result.PollenEarnedEstimate += Math.Max(0, run.Lueur - lueurBefore);
            }

            if (run.State == RunState.RunVictory)
            {
                result.Victory = true;
                result.RoundsCleared = RunConfig.RoundCount;
            }
            else if (run.State == RunState.RunDefeat)
            {
                var ids = EncounterCatalog.GetEncounter(run.Challenge.Id, run.CurrentRoundIndex);
                result.DeathEncounter.AddRange(ids);
                result.DeathCause = run.PiecesRemainingThisRound <= 0 ? "out of seeds" : "board blocked";
                if (run.HasActiveEncounter)
                {
                    int hp = 0;
                    int max = 0;
                    foreach (var enemy in run.CurrentEncounter)
                    {
                        hp += Math.Max(0, enemy.CurrentHp);
                        max += enemy.CurrentMaxHp;
                    }
                    result.EnemyHpLeftPct = max > 0 ? 100.0 * hp / max : 0;
                }
            }
            result.TotalScore = run.TotalScore;
            result.FinalModifiers.AddRange(run.ActiveModifiers);
            return result;
        }

        // ---------------------------------------------------------------- placing

        private bool TryPlaceBest(RunManager run, out int lueurBefore)
        {
            lueurBefore = run.Lueur;
            var deck = run.Deck;
            double bestScore = double.NegativeInfinity;
            int bestSlot = -1;
            int bestX = 0;
            int bestY = 0;
            int ties = 0;

            for (int i = 0; i < deck.Hand.Count; i++)
            {
                if (!deck.Hand[i].HasValue || run.IsHandSlotLocked(i))
                {
                    continue;
                }
                var token = deck.Hand[i].Value;
                var shape = PieceShapeCatalog.GetRotated(token.Shape, deck.HandRotations[i]);
                for (int x = 0; x < GridManager.Size; x++)
                {
                    for (int y = 0; y < GridManager.Size; y++)
                    {
                        if (!run.Grid.CanPlace(shape, x, y))
                        {
                            continue;
                        }
                        double score = _policy == BotPolicy.Random ? _rng.NextDouble() : Evaluate(run, shape, token, x, y) + _rng.NextDouble() * 0.5;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSlot = i;
                            bestX = x;
                            bestY = y;
                            ties = 1;
                        }
                    }
                }
            }

            if (bestSlot < 0)
            {
                return false;
            }
            var outcome = run.PlacePiece(bestSlot, bestX, bestY);
            return outcome.Placement.Success;
        }

        /// <summary>Group score plus a bonus per row/column this placement would complete and a small tidiness term.</summary>
        private double Evaluate(RunManager run, PieceShape shape, PieceToken token, int ax, int ay)
        {
            double score = run.Grid.PreviewGroupScore(shape, token.Color, ax, ay);
            var cells = shape.Cells;
            var rowAdd = new int[GridManager.Size];
            var colAdd = new int[GridManager.Size];
            for (int i = 0; i < cells.Count; i++)
            {
                rowAdd[ay + cells[i].y]++;
                colAdd[ax + cells[i].x]++;
            }
            for (int k = 0; k < GridManager.Size; k++)
            {
                if (rowAdd[k] > 0 && FilledInRow(run.Grid, k) + rowAdd[k] >= UnlockedInRow(run.Grid, k))
                {
                    score += 30;
                }
                if (colAdd[k] > 0 && FilledInColumn(run.Grid, k) + colAdd[k] >= UnlockedInColumn(run.Grid, k))
                {
                    score += 30;
                }
            }
            // Slight preference for hugging the filled area (keeps free space contiguous).
            for (int i = 0; i < cells.Count; i++)
            {
                int cx = ax + cells[i].x;
                int cy = ay + cells[i].y;
                if (IsFilledOrWall(run.Grid, cx - 1, cy)) { score += 0.4; }
                if (IsFilledOrWall(run.Grid, cx + 1, cy)) { score += 0.4; }
                if (IsFilledOrWall(run.Grid, cx, cy - 1)) { score += 0.4; }
                if (IsFilledOrWall(run.Grid, cx, cy + 1)) { score += 0.4; }
            }
            return score;
        }

        private static bool IsFilledOrWall(GridManager grid, int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return true;
            }
            var cell = grid.GetCell(x, y);
            return cell.IsFilled || cell.IsLocked;
        }

        private static int FilledInRow(GridManager grid, int y)
        {
            int n = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                if (grid.GetCell(x, y).IsFilled) { n++; }
            }
            return n;
        }

        private static int UnlockedInRow(GridManager grid, int y)
        {
            int n = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                if (!grid.GetCell(x, y).IsLocked) { n++; }
            }
            return n;
        }

        private static int FilledInColumn(GridManager grid, int x)
        {
            int n = 0;
            for (int y = 0; y < GridManager.Size; y++)
            {
                if (grid.GetCell(x, y).IsFilled) { n++; }
            }
            return n;
        }

        private static int UnlockedInColumn(GridManager grid, int x)
        {
            int n = 0;
            for (int y = 0; y < GridManager.Size; y++)
            {
                if (!grid.GetCell(x, y).IsLocked) { n++; }
            }
            return n;
        }

        // ---------------------------------------------------------------- shopping

        /// <summary>Spends Pollen, resolves every pending choice, leaves the shop. False if a choice could not be resolved.</summary>
        private bool ShopPhase(RunManager run, RunResult result)
        {
            for (int guard = 0; guard < 40; guard++)
            {
                if (!TryBuyOne(run, result))
                {
                    break;
                }
                if (!ResolvePending(run))
                {
                    return false;
                }
            }
            return run.LeaveShop();
        }

        private bool TryBuyOne(RunManager run, RunResult result)
        {
            var options = new List<(bool Modifier, int Index, int Price)>();
            var blister = run.ShopBlisterSlots;
            for (int i = 0; i < blister.Count; i++)
            {
                if (blister[i] != null && !blister[i].Purchased)
                {
                    options.Add((blister[i].Kind == ShopSlotKind.Modifier, i, run.GetBlisterSlotPrice(i)));
                }
            }
            var upgrades = run.ShopUpgradeSlots;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && !upgrades[i].Purchased)
                {
                    options.Add((false, 1000 + i, run.GetUpgradeSlotPrice(i)));
                }
            }
            // Greedy: modifiers (blister) first, cheapest first. Random: shuffled.
            if (_policy == BotPolicy.Greedy)
            {
                options.Sort((a, b) =>
                {
                    int byKind = (b.Modifier ? 1 : 0).CompareTo(a.Modifier ? 1 : 0);
                    return byKind != 0 ? byKind : a.Price.CompareTo(b.Price);
                });
            }
            else
            {
                for (int i = options.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    var tmp = options[i]; options[i] = options[j]; options[j] = tmp;
                }
            }

            for (int i = 0; i < options.Count; i++)
            {
                int lueurBefore = run.Lueur;
                bool bought = options[i].Index >= 1000 ? run.BuyUpgradeSlot(options[i].Index - 1000) : run.BuyBlisterSlot(options[i].Index);
                if (bought)
                {
                    result.PollenSpent += lueurBefore - run.Lueur;
                    return true;
                }
            }
            return false;
        }

        private bool ResolvePending(RunManager run)
        {
            for (int guard = 0; guard < 10 && run.PendingUpgrade != null; guard++)
            {
                var pending = run.PendingUpgrade;
                bool ok;
                if (run.PendingMasteryOptions.Count > 0)
                {
                    ok = run.ResolveMasteryChoice(_rng.Next(run.PendingMasteryOptions.Count));
                }
                else if (pending.Id == UpgradeId.RandomPiece)
                {
                    ok = run.ResolveUpgradePieceChoice(_rng.Next(run.PendingUpgradePieceCandidates.Count));
                }
                else if (pending.Id == UpgradeId.ModifierUpgrade)
                {
                    ok = run.ActiveModifiers.Count > 0 && run.ResolveModifierUpgradeChoice(_rng.Next(run.ActiveModifiers.Count));
                }
                else if (pending.Pool == UpgradePool.Grid)
                {
                    var chosen = new List<int>();
                    var candidates = run.PendingUpgradeTileCandidates;
                    int take = Math.Min(EconomyConstants.ShopTileChoiceCount, candidates.Count);
                    var pool = new List<int>(candidates);
                    for (int i = 0; i < take; i++)
                    {
                        int pick = _rng.Next(pool.Count);
                        chosen.Add(pool[pick]);
                        pool.RemoveAt(pick);
                    }
                    ok = run.ResolveUpgradeTileChoice(chosen);
                }
                else if (pending.RequiresSubChoice && run.PendingUpgradeTypeCandidates.Count > 0)
                {
                    var type = run.PendingUpgradeTypeCandidates[_rng.Next(run.PendingUpgradeTypeCandidates.Count)];
                    PieceColor target = PieceColor.Coral;
                    foreach (var c in PieceColorUtility.BaseColors)
                    {
                        if (c != type.Color) { target = c; break; }
                    }
                    ShapeId addShape = ShapeId.Single;
                    PieceColor addColor = PieceColor.Coral;
                    if (pending.Id == UpgradeId.ReplacePiece)
                    {
                        var repl = run.Upgrades.GetReplacementCandidateTypesFor(run.Deck, type.Shape, type.Color);
                        if (repl.Count > 0)
                        {
                            addShape = repl[0].Shape;
                            addColor = repl[0].Color;
                        }
                    }
                    ok = run.ResolveUpgradeSubChoice(new UpgradeSubChoice(type.Shape, type.Color, target, addShape, addColor));
                }
                else
                {
                    return false;
                }
                if (!ok && run.PendingUpgrade == pending)
                {
                    return false;
                }
            }
            return run.PendingUpgrade == null;
        }
    }
}
