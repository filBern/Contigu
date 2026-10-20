using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Random locking/freezing of cells, permanent bonus squares and clearing random filled cells.</summary>
    public sealed partial class GridManager
    {
        /// <summary>Locks up to <paramref name="count"/> random cells for the boss round, avoiding cells that already carry a golden/tinted/multiplier modifier when possible.</summary>
        public IReadOnlyList<Vector2Int> LockRandomCells(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled && !cell.HasAnyModifier)
                {
                    candidates.Add(pos);
                }
            }

            if (candidates.Count < count)
            {
                // Fallback: not enough plain cells, allow modified (but still
                // unlocked and unfilled) ones too rather than under-delivering
                // the boss effect.
                foreach (var pos in AllPositions())
                {
                    var cell = GetCell(pos);
                    if (!cell.IsLocked && !cell.IsFilled && cell.HasAnyModifier)
                    {
                        candidates.Add(pos);
                    }
                }
            }

            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsLocked = true;
            }
            return chosen;
        }

        /// <summary>Heatwave: scorches one random planted, unlocked tile away (no score). Returns where it was, its color and trait, or null when nothing could burn.</summary>
        public (Vector2Int Pos, PieceColor Color, PieceTrait? Trait)? BurnRandomTile(IRandomProvider rng)
        {
            var candidates = MovableFilledCells();
            if (candidates.Count == 0)
            {
                return null;
            }
            var pos = candidates[rng.Next(candidates.Count)];
            var cell = GetCell(pos);
            var burned = (pos, cell.FilledColor ?? default(PieceColor), cell.OriginTrait);
            cell.ClearFill();
            return burned;
        }

        /// <summary>
        /// Hurricane: blows up to <paramref name="count"/> random planted, unlocked tiles onto other empty, unlocked, unfrozen plots, carrying their whole
        /// fill state (color, shape, golden/tinted/multiplier flags, trait, mastery stamps). Hazards tied to a position (blight, locks) stay put.
        /// </summary>
        public List<(Vector2Int From, Vector2Int To, PieceColor Color)> BlowTiles(int count, IRandomProvider rng)
        {
            var moves = new List<(Vector2Int, Vector2Int, PieceColor)>();
            var sources = MovableFilledCells();
            var destinations = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsFilled && !cell.IsLocked && !cell.IsFrozen)
                {
                    destinations.Add(pos);
                }
            }
            for (int n = 0; n < count && sources.Count > 0 && destinations.Count > 0; n++)
            {
                int si = rng.Next(sources.Count);
                int di = rng.Next(destinations.Count);
                var from = sources[si];
                var to = destinations[di];
                sources.RemoveAt(si);
                destinations.RemoveAt(di);
                var src = GetCell(from);
                var dst = GetCell(to);
                dst.IsFilled = src.IsFilled;
                dst.FilledColor = src.FilledColor;
                dst.FilledShapeId = src.FilledShapeId;
                // A weather-golden plot stays golden where it is; only a tile's own golden stamp travels.
                dst.IsGolden = dst.IsWeatherGolden || (src.IsGolden && !src.IsWeatherGolden);
                dst.IsTinted = src.IsTinted;
                dst.TintedColor = src.TintedColor;
                dst.IsMultiplierZone = src.IsMultiplierZone;
                dst.OriginTrait = src.OriginTrait;
                dst.ShapeMasteryBonus = src.ShapeMasteryBonus;
                dst.ColorMasteryBonus = src.ColorMasteryBonus;
                moves.Add((from, to, dst.FilledColor ?? default(PieceColor)));
                src.ClearFill();
            }
            return moves;
        }

        /// <summary>Fruitworm: turns one random planted, unlocked tile of a base color into a different one of the run's colors. Null when there is nothing to recolor.</summary>
        public (Vector2Int Pos, PieceColor From, PieceColor To)? RecolorRandomTile(IRandomProvider rng)
        {
            var colors = PieceColorUtility.BaseColors;
            if (colors.Count < 2)
            {
                return null;
            }
            var candidates = new List<Vector2Int>();
            foreach (var pos in MovableFilledCells())
            {
                if (PieceColorUtility.IsActive(GetCell(pos).FilledColor.Value) && GetCell(pos).FilledColor.Value != PieceColor.Joker)
                {
                    candidates.Add(pos);
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            var chosen = candidates[rng.Next(candidates.Count)];
            var cell = GetCell(chosen);
            var from = cell.FilledColor.Value;
            var others = new List<PieceColor>();
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] != from)
                {
                    others.Add(colors[i]);
                }
            }
            var to = others[rng.Next(others.Count)];
            cell.FilledColor = to;
            return (chosen, from, to);
        }

        /// <summary>Whitefly: swaps the whole fill state of two random planted, unlocked tiles (preferring two of different colors). Null with fewer than two tiles.</summary>
        public (Vector2Int A, Vector2Int B)? SwapRandomTiles(IRandomProvider rng)
        {
            var candidates = MovableFilledCells();
            if (candidates.Count < 2)
            {
                return null;
            }
            var a = candidates[rng.Next(candidates.Count)];
            var different = new List<Vector2Int>();
            var any = new List<Vector2Int>();
            foreach (var pos in candidates)
            {
                if (pos == a)
                {
                    continue;
                }
                any.Add(pos);
                if (GetCell(pos).FilledColor != GetCell(a).FilledColor)
                {
                    different.Add(pos);
                }
            }
            var pool = different.Count > 0 ? different : any;
            var b = pool[rng.Next(pool.Count)];
            var first = GetCell(a);
            var second = GetCell(b);
            var temp = new Cell();
            CopyFill(first, temp);
            CopyFill(second, first);
            CopyFill(temp, second);
            return (a, b);
        }

        private static void CopyFill(Cell src, Cell dst)
        {
            dst.IsFilled = src.IsFilled;
            dst.FilledColor = src.FilledColor;
            dst.FilledShapeId = src.FilledShapeId;
            dst.IsGolden = src.IsGolden;
            dst.IsTinted = src.IsTinted;
            dst.TintedColor = src.TintedColor;
            dst.IsMultiplierZone = src.IsMultiplierZone;
            dst.OriginTrait = src.OriginTrait;
            dst.ShapeMasteryBonus = src.ShapeMasteryBonus;
            dst.ColorMasteryBonus = src.ColorMasteryBonus;
        }

        private List<Vector2Int> MovableFilledCells()
        {
            var cells = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (cell.IsFilled && cell.FilledColor.HasValue && !cell.IsLocked && !cell.IsBastion)
                {
                    cells.Add(pos);
                }
            }
            return cells;
        }

        /// <summary>Turns up to <paramref name="count"/> random empty, unlocked, non-golden plots golden (Golden Hour weather). Returns the chosen positions.</summary>
        public IReadOnlyList<Vector2Int> MakeRandomCellsGolden(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled && !cell.IsGolden)
                {
                    candidates.Add(pos);
                }
            }
            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsGolden = true;
                GetCell(chosen[i]).IsWeatherGolden = true;
            }
            return chosen;
        }

        /// <summary>
        /// "Frozen" cells (see Cell.IsFrozen) — HeavyLocker's round-start effect. Picks up to <paramref
        /// name="count"/> random still-empty, unlocked cells and locks them WITHOUT IsLineClearObstacle, so
        /// — unlike LockRandomCells's own boss-round lock — the row/column they sit in can still complete
        /// around them; CollectLineCell then actually clears and unlocks a Frozen cell the moment that
        /// happens, its only removal path. No fallback onto already-modified cells (unlike LockRandomCells):
        /// a brand-new round's board is empty enough that this should essentially never run short.
        /// </summary>
        public IReadOnlyList<Vector2Int> FreezeRandomCells(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled)
                {
                    candidates.Add(pos);
                }
            }

            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                var cell = GetCell(chosen[i]);
                cell.IsLocked = true;
                cell.IsFrozen = true;
            }
            return chosen;
        }

        /// <summary>
        /// "Permanent Bonus Square" (see Cell.IsPermanentMultiplier/UpgradeDefinition.PermanentBonusSquare):
        /// marks one random currently-empty, unlocked cell that doesn't already carry this flag. Returns
        /// null (a no-op) only in the essentially impossible case the whole board already qualifies.
        /// </summary>
        public Vector2Int? MarkRandomPermanentBonusSquare(IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled && !cell.IsPermanentMultiplier)
                {
                    candidates.Add(pos);
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            var chosen = candidates[rng.Next(candidates.Count)];
            GetCell(chosen).IsPermanentMultiplier = true;
            return chosen;
        }

        /// <summary>
        /// Boss round mechanic: locks up to <paramref name="count"/> random
        /// still-empty, unlocked cells, ratcheting the board's playable area
        /// down gradually. Locking a cell can itself complete a row/column
        /// (if every other cell in it was already filled or locked) — that's
        /// re-validated here via the same <see cref="CheckAndClearLines"/>
        /// a real placement uses, so it scores and clears the same way.
        /// </summary>
        public BossLockOutcome LockFreeCellsAndCheckClears(int count, IRandomProvider rng)
        {
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (!cell.IsLocked && !cell.IsFilled)
                {
                    candidates.Add(pos);
                }
            }

            var chosen = PickN(candidates, count, rng);
            for (int i = 0; i < chosen.Count; i++)
            {
                GetCell(chosen[i]).IsLocked = true;
            }

            var outcome = new BossLockOutcome();
            outcome.LockedCells = chosen;
            if (chosen.Count == 0)
            {
                return outcome;
            }

            var clearInfo = CheckAndClearLines();
            outcome.ClearedCells = clearInfo.ClearedCells;
            outcome.ClearedCellColors = clearInfo.ClearedCellColors;
            outcome.LineClearScore = SumLineAmounts(clearInfo);
            outcome.LueurEarned = SumLueur(ComputeLueurGroups(clearInfo.ClearedLines));

            if (clearInfo.ClearedCells.Count > 0)
            {
                // Same streak this placement's own clear would update — a boss
                // lock completing a line is still a line clear as far as
                // "Spark Tile"/"Rafale" are concerned.
                _placementsSinceLastClear = 0;
            }

            var events = new List<ScoreEvent>(clearInfo.ClearedCells.Count + clearInfo.BastionBonusCells.Count);
            for (int i = 0; i < clearInfo.ClearedCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.LineClear, clearInfo.ClearedCells[i], clearInfo.LineAmounts[clearInfo.ClearedCells[i]]));
            }
            for (int i = 0; i < clearInfo.BastionBonusCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Bastion, clearInfo.BastionBonusCells[i], clearInfo.LineAmounts[clearInfo.BastionBonusCells[i]]));
            }
            outcome.ScoreEvents = events;

            return outcome;
        }

        /// <summary>
        /// Clears one random already-filled, unlocked cell not in
        /// <paramref name="exclude"/> — used by the "Void Tile" piece trait
        /// (RunManager applies it AFTER this placement's own PlacePiece call
        /// returns, excluding that placement's own cells, so Void never
        /// erases the very cells it just scored). Returns the cleared
        /// position, or null if nothing else on the grid was eligible.
        /// </summary>
        public Vector2Int? ClearRandomFilledCell(IRandomProvider rng, IReadOnlyList<Vector2Int> exclude)
        {
            return ClearRandomFilledCell(rng, exclude, out _, out _);
        }

        /// <summary>Same as the 2-argument overload, but also hands back the cleared cell's color and <see cref="Cell.FilledShapeId"/> (before it was cleared) via <paramref name="clearedColor"/>/<paramref name="clearedShape"/> — both null when nothing was eligible — so RunManager.ApplyVoidEffect can pass them on to <see cref="PlacementResult.DestroyedCellColors"/>/<see cref="PlacementResult.DestroyedCellShapes"/> for the presentation layer's destroy VFX and Color/Shape Hater's own score-cancelling rule.</summary>
        public Vector2Int? ClearRandomFilledCell(IRandomProvider rng, IReadOnlyList<Vector2Int> exclude, out PieceColor? clearedColor, out ShapeId? clearedShape)
        {
            clearedColor = null;
            clearedShape = null;
            var excludeSet = new HashSet<Vector2Int>(exclude);
            var candidates = new List<Vector2Int>();
            foreach (var pos in AllPositions())
            {
                var cell = GetCell(pos);
                if (cell.IsFilled && !cell.IsLocked && !excludeSet.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var chosen = candidates[rng.Next(candidates.Count)];
            var chosenCell = GetCell(chosen);
            clearedColor = chosenCell.FilledColor;
            clearedShape = chosenCell.FilledShapeId;
            chosenCell.ClearFill();
            return chosen;
        }

        private static List<Vector2Int> PickN(List<Vector2Int> candidates, int n, IRandomProvider rng)
        {
            var pool = new List<Vector2Int>(candidates);
            var result = new List<Vector2Int>();
            int take = Mathf.Min(n, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
