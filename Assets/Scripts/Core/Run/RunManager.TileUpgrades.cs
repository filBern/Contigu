using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The tile upgrades that score flat placement points (Pip, Lucky, Companion, Colony, Border, Heart, Corner, Hermit, Cuddly, Dawn, Dusk, Miser, Stormy, Elder, Harvester, Breeze, Harmony, Prism, Gardener).</summary>
    public sealed partial class RunManager
    {
        private static readonly Vector2Int[] TileUpgradeSteps = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        /// <summary>The tiles that do not count as "neighbors" of the tile upgrade being scored: the placed seed's own tiles for a fresh upgrade, none for an older one being counted again.</summary>
        private IReadOnlyList<Vector2Int> _tileUpgradeOwnCells;

        /// <summary>The upgraded tiles already on the garden, with their upgrade, as they stand right before a placement.</summary>
        private List<(Vector2Int Pos, PieceTrait Trait)> CollectPersistentTraits()
        {
            var list = new List<(Vector2Int, PieceTrait)>();
            foreach (var pos in GridManager.AllPositions())
            {
                var cell = Grid.GetCell(pos);
                if (cell.IsFilled && cell.OriginTrait.HasValue && !PieceTrait.IsJokerCombatKind(cell.OriginTrait.Value.Kind))
                {
                    list.Add((pos, cell.OriginTrait.Value));
                }
            }
            return list;
        }

        /// <summary>Re-stamps the golden/tinted/multiplier effects of the older upgraded tiles so GridManager's own scoring counts them again when they are part of the scored group.</summary>
        private void StampPersistentTraits(List<(Vector2Int Pos, PieceTrait Trait)> traits, PieceShape shape, int x, int y, List<Cell> transientCells)
        {
            for (int i = 0; i < traits.Count; i++)
            {
                var kind = traits[i].Trait.Kind;
                if (kind == PieceTraitKind.Golden || kind == PieceTraitKind.Tinted || kind == PieceTraitKind.Multiplier || kind == PieceTraitKind.Blast || kind == PieceTraitKind.Beacon)
                {
                    transientCells.AddRange(ApplyTokenTrait(traits[i].Trait, traits[i].Pos, shape, x, y));
                }
            }
        }

        /// <summary>The post-placement upgrades (Mirror, Twin, Spark, Void, Venom and every flat-point one) of the older tiles that this placement scored again.</summary>
        private void ApplyPersistentTraitBonuses(List<(Vector2Int Pos, PieceTrait Trait)> traits, int streakBeforePlacement, PlacementResult placement)
        {
            if (traits.Count == 0 || placement.GroupCells.Count == 0)
            {
                return;
            }
            var scored = new HashSet<Vector2Int>(placement.GroupCells);
            var fresh = new HashSet<Vector2Int>(placement.PlacedCells);
            _tileUpgradeOwnCells = System.Array.Empty<Vector2Int>();
            try
            {
                for (int i = 0; i < traits.Count; i++)
                {
                    if (scored.Contains(traits[i].Pos) && !fresh.Contains(traits[i].Pos))
                    {
                        ApplyPostPlacementTraitBonus(traits[i].Trait, traits[i].Pos, streakBeforePlacement, placement);
                    }
                }
            }
            finally
            {
                _tileUpgradeOwnCells = null;
            }
        }

        /// <summary>The color of the tile at <paramref name="pos"/> as this placement saw it — a tile its own line clear just harvested still counts.</summary>
        private PieceColor? ColorAsPlaced(Vector2Int pos, PlacementResult placement)
        {
            if (!GridManager.InBounds(pos.x, pos.y))
            {
                return null;
            }
            var cell = Grid.GetCell(pos);
            if (cell.IsFilled && cell.FilledColor.HasValue)
            {
                return cell.FilledColor;
            }
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                if (placement.ClearedCells[i] == pos && !placement.ClearedCellFrozen[i])
                {
                    return placement.ClearedCellColors[i];
                }
            }
            return null;
        }

        /// <summary>The colors of the tiles touching <paramref name="pos"/> that are not part of the placed piece itself.</summary>
        private List<PieceColor> OutsideNeighborColors(Vector2Int pos, PlacementResult placement)
        {
            var colors = new List<PieceColor>();
            for (int s = 0; s < TileUpgradeSteps.Length; s++)
            {
                var n = pos + TileUpgradeSteps[s];
                if (new List<Vector2Int>(_tileUpgradeOwnCells ?? placement.PlacedCells).Contains(n))
                {
                    continue;
                }
                var color = ColorAsPlaced(n, placement);
                if (color.HasValue)
                {
                    colors.Add(color.Value);
                }
            }
            return colors;
        }

        /// <summary>Scores the flat points of the placement-point tile upgrades (a Trait event on the enchanted cell).</summary>
        private void ApplyTileUpgradeBonus(PieceTrait trait, Vector2Int pos, PlacementResult placement)
        {
            int bonus = 0;
            int last = GridManager.Size - 1;
            switch (trait.Kind)
            {
                case PieceTraitKind.Pip:
                    bonus = ScoringConstants.PipBonus;
                    break;
                case PieceTraitKind.Lucky:
                    bonus = _rng.Next(ScoringConstants.LuckyMaxBonus + 1);
                    break;
                case PieceTraitKind.Companion:
                    if (trait.TintedColor.HasValue)
                    {
                        foreach (var color in OutsideNeighborColors(pos, placement))
                        {
                            if (color == trait.TintedColor.Value)
                            {
                                bonus += ScoringConstants.CompanionBonusPerTile;
                            }
                        }
                    }
                    break;
                case PieceTraitKind.Colony:
                    GetGroupShare(placement, pos, out int groupSize, out _);
                    if (groupSize > ScoringConstants.ColonyMinGroupExclusive)
                    {
                        bonus = ScoringConstants.ColonyBonus;
                    }
                    break;
                case PieceTraitKind.Border:
                    if (pos.x == 0 || pos.y == 0 || pos.x == last || pos.y == last)
                    {
                        bonus = ScoringConstants.BorderBonus;
                    }
                    break;
                case PieceTraitKind.Heart:
                    if (pos.x >= (GridManager.Size - 1) / 2 && pos.x <= GridManager.Size / 2 && pos.y >= (GridManager.Size - 1) / 2 && pos.y <= GridManager.Size / 2)
                    {
                        bonus = ScoringConstants.HeartBonus;
                    }
                    break;
                case PieceTraitKind.Corner:
                    if ((pos.x == 0 || pos.x == last) && (pos.y == 0 || pos.y == last))
                    {
                        bonus = ScoringConstants.CornerBonus;
                    }
                    break;
                case PieceTraitKind.Hermit:
                    if (OutsideNeighborColors(pos, placement).Count == 0)
                    {
                        bonus = ScoringConstants.HermitBonus;
                    }
                    break;
                case PieceTraitKind.Cuddly:
                    // Every planted tile around it counts: the 4 diagonals too, and the other tiles of its own seed.
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if ((dx != 0 || dy != 0) && ColorAsPlaced(new Vector2Int(pos.x + dx, pos.y + dy), placement).HasValue)
                            {
                                bonus += ScoringConstants.CuddlyBonusPerNeighbor;
                            }
                        }
                    }
                    break;
                case PieceTraitKind.Dawn:
                    // PiecesRemainingThisRound still counts this very seed (it is spent right after scoring).
                    if (CurrentBudget - PiecesRemainingThisRound + 1 <= ScoringConstants.DawnSeeds)
                    {
                        bonus = ScoringConstants.DawnBonus;
                    }
                    break;
                case PieceTraitKind.Dusk:
                    // The last seed of the current hand: this one is still in its slot (it is spent right after scoring), every other slot is empty.
                    int seedsInHand = 0;
                    for (int slot = 0; slot < Deck.Hand.Count; slot++)
                    {
                        if (Deck.Hand[slot].HasValue)
                        {
                            seedsInHand++;
                        }
                    }
                    if (seedsInHand == 1)
                    {
                        bonus = ScoringConstants.DuskBonus;
                    }
                    break;
                case PieceTraitKind.Miser:
                    bonus = Mathf.Min(Lueur * ScoringConstants.MiserBonusPerPollen, ScoringConstants.MiserMaxBonus);
                    break;
                case PieceTraitKind.Stormy:
                    if (CurrentWeather != WeatherId.Clear)
                    {
                        bonus = ScoringConstants.StormyBonus;
                    }
                    break;
                case PieceTraitKind.Elder:
                    bonus = CurrentRoundIndex * ScoringConstants.ElderBonusPerRound;
                    break;
                case PieceTraitKind.Harvester:
                    bonus = placement.ClearedLineCount * ScoringConstants.HarvesterBonusPerLine;
                    break;
                case PieceTraitKind.Breeze:
                    bonus = ShufflesRemaining * ScoringConstants.BreezeBonusPerShuffle;
                    break;
                case PieceTraitKind.Harmony:
                    var neighbors = OutsideNeighborColors(pos, placement);
                    var own = ColorAsPlaced(pos, placement);
                    if (neighbors.Count > 0 && own.HasValue && neighbors.TrueForAll(c => c == own.Value))
                    {
                        bonus = ScoringConstants.HarmonyBonus;
                    }
                    break;
                case PieceTraitKind.Prism:
                    if (new HashSet<PieceColor>(OutsideNeighborColors(pos, placement)).Count >= ScoringConstants.PrismMinColors)
                    {
                        bonus = ScoringConstants.PrismBonus;
                    }
                    break;
                case PieceTraitKind.Gardener:
                    bonus = _activeModifiers.Count * ScoringConstants.GardenerBonusPerFertilizer;
                    break;
            }
            if (bonus > 0)
            {
                AddTraitBonus(placement, pos, bonus);
            }
        }
    }
}
