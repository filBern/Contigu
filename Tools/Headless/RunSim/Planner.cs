using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.RunSim
{
    /// <summary>
    /// The "hand planner": instead of choosing one placement at a time it searches the whole hand — every order of the pieces it holds and
    /// every spot for each — and plays the first move of the best sequence found (then re-plans after every placement, since the dealt hand
    /// and the real scoring move on). Each step is scored by the real grid rules on a copy of the board (<see cref="GridManager.CloneForSimulation"/>,
    /// held fertilizers included), plus the strategic bonus per completed line the greedy bot uses; a beam keeps the search small.
    /// </summary>
    public sealed class Planner
    {
        private static readonly int[] BeamWidths = { 8, 5, 3 };
        private static readonly int MaxDepth = int.TryParse(Environment.GetEnvironmentVariable("PLAN_DEPTH"), out var d) ? Math.Max(1, Math.Min(3, d)) : 3;
        private const double LineBonus = 30.0;

        private readonly struct Piece
        {
            public readonly int Slot;
            public readonly PieceShape Shape;
            public readonly PieceColor Color;

            public Piece(int slot, PieceShape shape, PieceColor color)
            {
                Slot = slot;
                Shape = shape;
                Color = color;
            }
        }

        private sealed class Option
        {
            public int PieceIndex;
            public int X;
            public int Y;
            public double Value;
            public GridManager Board;
        }

        private readonly Random _rng;
        private List<ModifierId> _modifiers;
        private List<int> _levels;

        public Planner(Random rng)
        {
            _rng = rng;
        }

        /// <summary>The first move of the best plan for the current hand, or false when nothing can be placed.</summary>
        public bool Choose(RunManager run, out int slot, out int x, out int y)
        {
            slot = -1;
            x = 0;
            y = 0;
            _modifiers = new List<ModifierId>(run.ActiveModifiers);
            _levels = new List<int>();
            for (int i = 0; i < _modifiers.Count; i++)
            {
                _levels.Add(run.GetModifierLevel(i));
            }

            var deck = run.Deck;
            var pieces = new List<Piece>();
            for (int i = 0; i < deck.Hand.Count; i++)
            {
                if (!deck.Hand[i].HasValue || run.IsHandSlotLocked(i))
                {
                    continue;
                }
                var token = deck.Hand[i].Value;
                pieces.Add(new Piece(i, PieceShapeCatalog.GetRotated(token.Shape, deck.HandRotations[i]), token.Color));
            }
            if (pieces.Count == 0)
            {
                return false;
            }

            var roots = Expand(run.Grid, pieces, 0);
            if (roots.Count == 0)
            {
                return false;
            }
            Option best = null;
            double bestTotal = double.NegativeInfinity;
            int width = Math.Min(roots.Count, BeamWidths[0]);
            for (int r = 0; r < width; r++)
            {
                double total = roots[r].Value + Continue(roots[r].Board, pieces, 1 << roots[r].PieceIndex, 1) + _rng.NextDouble() * 0.3;
                if (total > bestTotal)
                {
                    bestTotal = total;
                    best = roots[r];
                }
            }
            slot = pieces[best.PieceIndex].Slot;
            x = best.X;
            y = best.Y;
            return true;
        }

        /// <summary>Every legal placement of every unused piece, scored on a copy of <paramref name="board"/>, best first.</summary>
        private List<Option> Expand(GridManager board, List<Piece> pieces, int used)
        {
            var options = new List<Option>();
            for (int p = 0; p < pieces.Count; p++)
            {
                if ((used & (1 << p)) != 0)
                {
                    continue;
                }
                for (int ax = 0; ax < GridManager.Size; ax++)
                {
                    for (int ay = 0; ay < GridManager.Size; ay++)
                    {
                        if (!board.CanPlace(pieces[p].Shape, ax, ay))
                        {
                            continue;
                        }
                        var copy = board.CloneForSimulation();
                        var result = copy.PlacePiece(pieces[p].Shape, pieces[p].Color, ax, ay, _modifiers, _levels);
                        if (!result.Success)
                        {
                            continue;
                        }
                        double value = result.TotalScore + result.ClearedLineCount * LineBonus + Hug(board, pieces[p].Shape, ax, ay);
                        options.Add(new Option { PieceIndex = p, X = ax, Y = ay, Value = value, Board = copy });
                    }
                }
            }
            options.Sort((a, b) => b.Value.CompareTo(a.Value));
            return options;
        }

        /// <summary>Best total value obtainable from the pieces not yet used (bitmask).</summary>
        private double Continue(GridManager board, List<Piece> pieces, int used, int depth)
        {
            if (depth >= MaxDepth || used == (1 << pieces.Count) - 1)
            {
                return Tidiness(board);
            }
            var options = Expand(board, pieces, used);
            if (options.Count == 0)
            {
                return Tidiness(board);
            }
            double best = double.NegativeInfinity;
            int width = Math.Min(options.Count, BeamWidths[Math.Min(depth, BeamWidths.Length - 1)]);
            for (int i = 0; i < width; i++)
            {
                double value = options[i].Value + Continue(options[i].Board, pieces, used | (1 << options[i].PieceIndex), depth + 1);
                if (value > best)
                {
                    best = value;
                }
            }
            return best;
        }

        /// <summary>Small preference for hugging filled plots, as the greedy bot has.</summary>
        private static double Hug(GridManager board, PieceShape shape, int ax, int ay)
        {
            double score = 0;
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                int cx = ax + shape.Cells[i].x;
                int cy = ay + shape.Cells[i].y;
                score += (Wall(board, cx - 1, cy) ? 1 : 0) + (Wall(board, cx + 1, cy) ? 1 : 0) + (Wall(board, cx, cy - 1) ? 1 : 0) + (Wall(board, cx, cy + 1) ? 1 : 0);
            }
            return score * 0.4;
        }

        private static bool Wall(GridManager board, int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return true;
            }
            var cell = board.GetCell(x, y);
            return cell.IsFilled || cell.IsLocked;
        }

        /// <summary>Penalty for empty plots boxed in on every side (nothing can ever land there).</summary>
        private static double Tidiness(GridManager board)
        {
            int holes = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (!Wall(board, x, y) && Wall(board, x - 1, y) && Wall(board, x + 1, y) && Wall(board, x, y - 1) && Wall(board, x, y + 1))
                    {
                        holes++;
                    }
                }
            }
            return -3.0 * holes;
        }
    }
}
