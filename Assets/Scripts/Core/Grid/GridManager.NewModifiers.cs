using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The grid-side fertilizers of the 40-fertilizer batch: placement geometry, neighbors and colors (pre-clear) and line-clear results (post-clear).</summary>
    public sealed partial class GridManager
    {
        private static readonly Vector2Int[] OrthogonalSteps = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        private static bool IsOnEdge(Vector2Int p)
        {
            return p.x == 0 || p.y == 0 || p.x == Size - 1 || p.y == Size - 1;
        }

        private void AddMult(List<ScoreEvent> events, Vector2Int at, int amount)
        {
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, at, amount));
        }

        private int Multiplied(List<ScoreEvent> events, Vector2Int at, int multiplier)
        {
            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, at, multiplier));
            return multiplier;
        }

        /// <summary>Hedgerow: points per placed tile on the garden's outer edge.</summary>
        private int ApplyHaie(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int edge = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                if (IsOnEdge(placed[i]))
                {
                    edge++;
                }
            }
            if (edge == 0)
            {
                return 0;
            }
            int bonus = edge * ScoringConstants.HedgerowBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placed[0], bonus));
            return bonus;
        }

        /// <summary>Heartwood: xN when a placed tile is in the middle plots (the 4 central ones on an even grid, the single centre on an odd one).</summary>
        private int ApplyCoeur(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int low = (Size - 1) / 2;
            int high = Size / 2;
            for (int i = 0; i < placed.Count; i++)
            {
                var p = placed[i];
                if (p.x >= low && p.x <= high && p.y >= low && p.y <= high)
                {
                    return Multiplied(events, placed[0], ScoringConstants.HeartwoodMultiplier);
                }
            }
            return 1;
        }

        private static bool IsStraight(List<Vector2Int> placed, out bool vertical)
        {
            bool sameX = true;
            bool sameY = true;
            for (int i = 1; i < placed.Count; i++)
            {
                sameX &= placed[i].x == placed[0].x;
                sameY &= placed[i].y == placed[0].y;
            }
            vertical = sameX && !sameY;
            return (sameX || sameY) && placed.Count >= 2;
        }

        /// <summary>Trellis (vertical) / Furrow (horizontal): +Mult for a straight piece of 2 or more tiles lying in that direction.</summary>
        private int ApplyDirectionalPiece(List<Vector2Int> placed, bool wantVertical, int bonusMult, List<ScoreEvent> events)
        {
            if (!IsStraight(placed, out bool vertical) || vertical != wantVertical)
            {
                return 0;
            }
            AddMult(events, placed[0], bonusMult);
            return bonusMult;
        }

        /// <summary>Beanstalk: flat points for a straight piece of 3 or more tiles.</summary>
        private int ApplyHaricot(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            if (placed.Count < ScoringConstants.BeanstalkMinCells || !IsStraight(placed, out _))
            {
                return 0;
            }
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placed[0], ScoringConstants.BeanstalkBonus));
            return ScoringConstants.BeanstalkBonus;
        }

        /// <summary>The planted tiles touching the placed piece (orthogonally, not part of it), each with the color of the placed tile it touches.</summary>
        private List<(Vector2Int Pos, PieceColor Own, PieceColor Other)> TouchingTiles(List<Vector2Int> placed)
        {
            var own = new HashSet<Vector2Int>(placed);
            var seen = new HashSet<Vector2Int>();
            var result = new List<(Vector2Int, PieceColor, PieceColor)>();
            for (int i = 0; i < placed.Count; i++)
            {
                for (int s = 0; s < OrthogonalSteps.Length; s++)
                {
                    var n = placed[i] + OrthogonalSteps[s];
                    if (!InBounds(n.x, n.y) || own.Contains(n) || !_cells[n.x, n.y].IsFilled || !_cells[n.x, n.y].FilledColor.HasValue || !seen.Add(n))
                    {
                        continue;
                    }
                    result.Add((n, _cells[placed[i].x, placed[i].y].FilledColor.Value, _cells[n.x, n.y].FilledColor.Value));
                }
            }
            return result;
        }

        /// <summary>Patchwork: points per touching tile of another color.</summary>
        private int ApplyPatchwork(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int count = 0;
            foreach (var t in TouchingTiles(placed))
            {
                if (t.Other != t.Own)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return 0;
            }
            int bonus = count * ScoringConstants.PatchworkBonusPerTile;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placed[0], bonus));
            return bonus;
        }

        /// <summary>Taproot: +1 Mult per 3 cells in the scored group.</summary>
        private int ApplyRacines(List<Vector2Int> group, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int bonus = group.Count / ScoringConstants.TaprootCellsPerMult;
            if (bonus <= 0)
            {
                return 0;
            }
            AddMult(events, placed[0], bonus);
            return bonus;
        }

        /// <summary>Snug Fit: +Mult when every plot beside the placed piece is already planted (or off the edge), with at least one such neighbor.</summary>
        private int ApplyEmboite(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            var own = new HashSet<Vector2Int>(placed);
            int neighbors = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                for (int s = 0; s < OrthogonalSteps.Length; s++)
                {
                    var n = placed[i] + OrthogonalSteps[s];
                    if (!InBounds(n.x, n.y) || own.Contains(n))
                    {
                        continue;
                    }
                    if (!_cells[n.x, n.y].IsFilled)
                    {
                        return 0;
                    }
                    neighbors++;
                }
            }
            if (neighbors == 0)
            {
                return 0;
            }
            AddMult(events, placed[0], ScoringConstants.SnugFitMultBonus);
            return ScoringConstants.SnugFitMultBonus;
        }

        /// <summary>Mosaic: +Mult when the piece touches 2+ tiles and none shares the color of the tile it touches.</summary>
        private int ApplyMosaique(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            var touching = TouchingTiles(placed);
            if (touching.Count < ScoringConstants.MosaicMinNeighbors)
            {
                return 0;
            }
            foreach (var t in touching)
            {
                if (t.Other == t.Own)
                {
                    return 0;
                }
            }
            AddMult(events, placed[0], ScoringConstants.MosaicMultBonus);
            return ScoringConstants.MosaicMultBonus;
        }

        /// <summary>Crowd: xN while at least two thirds of the garden is planted.</summary>
        private int ApplyFoule(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            if (FilledCellCount * 3 < Size * Size * 2)
            {
                return 1;
            }
            return Multiplied(events, placed[0], ScoringConstants.CrowdMultiplier);
        }

        /// <summary>Wildflower Meadow: +Mult when every active seed color has at least one tile in the garden.</summary>
        private int ApplyPrairie(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            var present = new HashSet<PieceColor>();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    if (_cells[x, y].IsFilled && _cells[x, y].FilledColor.HasValue)
                    {
                        present.Add(_cells[x, y].FilledColor.Value);
                    }
                }
            }
            var colors = PieceColorUtility.BaseColors;
            for (int i = 0; i < colors.Count; i++)
            {
                if (!present.Contains(colors[i]))
                {
                    return 0;
                }
            }
            AddMult(events, placed[0], ScoringConstants.MeadowMultBonus);
            return ScoringConstants.MeadowMultBonus;
        }

        /// <summary>Wreath: points per placed tile touching 3+ planted plots.</summary>
        private int ApplyGuirlande(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                int filled = 0;
                for (int s = 0; s < OrthogonalSteps.Length; s++)
                {
                    var n = placed[i] + OrthogonalSteps[s];
                    if (InBounds(n.x, n.y) && _cells[n.x, n.y].IsFilled)
                    {
                        filled++;
                    }
                }
                if (filled >= ScoringConstants.WreathMinNeighbors)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return 0;
            }
            int bonus = count * ScoringConstants.WreathBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placed[0], bonus));
            return bonus;
        }

        /// <summary>Pollinator: Pollen when the placement touches 2+ different colors.</summary>
        private int ApplyPollinisateur(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            var colors = CollectTouchingColors(placed);
            colors.Remove(PieceColor.Joker);
            if (colors.Count < 2)
            {
                return 0;
            }
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placed[0], EconomyConstants.PollinatorLueur));
            return EconomyConstants.PollinatorLueur;
        }

        /// <summary>Honey: Pollen per 3 cells in the scored group.</summary>
        private int ApplyMiel(List<Vector2Int> group, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int lueur = group.Count / EconomyConstants.HoneyCellsPerLueur;
            if (lueur <= 0)
            {
                return 0;
            }
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placed[0], lueur));
            return lueur;
        }

        /// <summary>Nectar: Pollen on every seed.</summary>
        private int ApplyNectar(List<Vector2Int> placed, List<ScoreEvent> events)
        {
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placed[0], EconomyConstants.NectarLueurPerSeed));
            return EconomyConstants.NectarLueurPerSeed;
        }

        // ------------------------------------------------------------------ post-clear

        /// <summary>Cross Harvest: +Mult when one seed completes a row and a column at once.</summary>
        private int ApplyMoissonCroisee(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            bool row = false;
            bool column = false;
            for (int i = 0; i < info.ClearedLines.Count; i++)
            {
                row |= info.ClearedLines[i].IsRow;
                column |= !info.ClearedLines[i].IsRow;
            }
            if (!row || !column)
            {
                return 0;
            }
            AddMult(events, placed[0], ScoringConstants.CrossHarvestMultBonus);
            return ScoringConstants.CrossHarvestMultBonus;
        }

        /// <summary>Reaper: +Mult per line harvested.</summary>
        private int ApplyFaucheur(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int bonus = info.ClearedLineCount * ScoringConstants.ReaperMultPerLine;
            if (bonus <= 0)
            {
                return 0;
            }
            AddMult(events, placed[0], bonus);
            return bonus;
        }

        /// <summary>Clean Slate: xN when the clear leaves the garden empty.</summary>
        private int ApplyTableRase(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            return info.GridEmptied ? Multiplied(events, placed[0], ScoringConstants.CleanSlateMultiplier) : 1;
        }

        /// <summary>Tithe: Pollen per line harvested.</summary>
        private int ApplyDime(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int lueur = info.ClearedLineCount * EconomyConstants.TitheLueurPerLine;
            if (lueur <= 0)
            {
                return 0;
            }
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placed[0], lueur));
            return lueur;
        }

        /// <summary>Row Crop / Column Crop: xN when at least one harvested line is a row / a column.</summary>
        private int ApplyCrop(ClearInfo info, bool rows, int multiplier, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            for (int i = 0; i < info.ClearedLines.Count; i++)
            {
                if (info.ClearedLines[i].IsRow == rows)
                {
                    return Multiplied(events, placed[0], multiplier);
                }
            }
            return 1;
        }

        /// <summary>Bounty: points per harvested tile (a frozen plot broken free had no tile).</summary>
        private int ApplyPrime(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            int tiles = 0;
            for (int i = 0; i < info.ClearedCells.Count; i++)
            {
                if (!info.ClearedCellFrozen[i])
                {
                    tiles++;
                }
            }
            if (tiles == 0)
            {
                return 0;
            }
            int bonus = tiles * ScoringConstants.BountyBonusPerTile;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placed[0], bonus));
            return bonus;
        }

        /// <summary>Frostbreaker: +Mult when the clear breaks a frozen plot free.</summary>
        private int ApplyBriseGlace(ClearInfo info, List<Vector2Int> placed, List<ScoreEvent> events)
        {
            for (int i = 0; i < info.ClearedCellFrozen.Count; i++)
            {
                if (info.ClearedCellFrozen[i])
                {
                    AddMult(events, placed[0], ScoringConstants.FrostbreakerMultBonus);
                    return ScoringConstants.FrostbreakerMultBonus;
                }
            }
            return 0;
        }
    }
}
