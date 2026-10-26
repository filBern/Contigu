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

        /// <summary>Perennial Sprout's run-long counter: how many times any Perennial has scored this run.</summary>
        private int _perennialScores;

        /// <summary>
        /// Amplifier (the points of every upgraded tile touching it count twice) and Echo (copies the biggest points bonus among them): they read
        /// what the other upgrades just scored, so they resolve last. Only upgrades that scored points count, and an amplified or copied bonus is
        /// never amplified again.
        /// </summary>
        private void ApplyNeighborCopyUpgrades(PlacementResult placement)
        {
            var scoredAt = new Dictionary<Vector2Int, int>();
            foreach (var scoreEvent in placement.ScoreEvents)
            {
                if (scoreEvent.Type == ScoreEventType.Trait && scoreEvent.Amount > 0)
                {
                    int already;
                    scoredAt.TryGetValue(scoreEvent.Position, out already);
                    scoredAt[scoreEvent.Position] = already + scoreEvent.Amount;
                }
            }
            if (scoredAt.Count == 0)
            {
                return;
            }
            foreach (var pos in placement.GroupCells)
            {
                var trait = TraitAsPlaced(pos, placement);
                if (!trait.HasValue || (trait.Value.Kind != PieceTraitKind.Amplifier && trait.Value.Kind != PieceTraitKind.Echo))
                {
                    continue;
                }
                int sum = 0;
                int best = 0;
                for (int s = 0; s < TileUpgradeSteps.Length; s++)
                {
                    if (scoredAt.TryGetValue(pos + TileUpgradeSteps[s], out int amount))
                    {
                        sum += amount;
                        best = Mathf.Max(best, amount);
                    }
                }
                int copied = trait.Value.Kind == PieceTraitKind.Amplifier ? sum : best;
                if (copied > 0)
                {
                    AddTraitBonus(placement, pos, copied);
                }
            }
        }

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
                    bonus = Lueur * ScoringConstants.MiserBonusPerPollen;
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
                case PieceTraitKind.Cluster:
                    for (int g = 0; g < placement.GroupCells.Count; g++)
                    {
                        if (placement.GroupCells[g] != pos && TraitAsPlaced(placement.GroupCells[g], placement).HasValue)
                        {
                            bonus += ScoringConstants.ClusterBonusPerUpgrade;
                        }
                    }
                    break;
                case PieceTraitKind.Crossfire:
                    for (int k = 0; k < GridManager.Size; k++)
                    {
                        if (k != pos.x && ColorAsPlaced(new Vector2Int(k, pos.y), placement).HasValue) { bonus += ScoringConstants.CrossfireBonusPerPlot; }
                        if (k != pos.y && ColorAsPlaced(new Vector2Int(pos.x, k), placement).HasValue) { bonus += ScoringConstants.CrossfireBonusPerPlot; }
                    }
                    break;
                case PieceTraitKind.Even:
                    GetGroupShare(placement, pos, out int evenGroup, out _);
                    if (evenGroup > 0 && evenGroup % 2 == 0)
                    {
                        bonus = ScoringConstants.EvenBonus;
                    }
                    break;
                case PieceTraitKind.RainbowRow:
                    var rowColors = new HashSet<PieceColor>();
                    for (int k = 0; k < GridManager.Size; k++)
                    {
                        var rowColor = ColorAsPlaced(new Vector2Int(k, pos.y), placement);
                        if (rowColor.HasValue)
                        {
                            rowColors.Add(rowColor.Value);
                        }
                    }
                    bool allColors = true;
                    foreach (var needed in PieceColorUtility.BaseColors)
                    {
                        allColors &= rowColors.Contains(needed);
                    }
                    if (allColors)
                    {
                        bonus = ScoringConstants.RainbowRowBonus;
                    }
                    break;
                case PieceTraitKind.Sunseeker:
                    var sky = WeatherCatalog.Mechanic(CurrentWeather);
                    if (sky == WeatherId.Sunshine || sky == WeatherId.GoldenHour)
                    {
                        bonus = ScoringConstants.SunseekerBonus;
                    }
                    break;
                case PieceTraitKind.Hunter:
                    bonus = HasActiveEncounter ? CountAliveEnemies() * ScoringConstants.HunterBonusPerEnemy : 0;
                    break;
                case PieceTraitKind.Executioner:
                    foreach (var enemy in _currentEncounter)
                    {
                        if (!enemy.IsDead)
                        {
                            if (enemy.CurrentHp <= enemy.CurrentMaxHp * ScoringConstants.ExecutionerHpFraction)
                            {
                                bonus = ScoringConstants.ExecutionerBonus;
                            }
                            break;
                        }
                    }
                    break;
                case PieceTraitKind.Spillover:
                    for (int c = 0; c < placement.ClearedCells.Count; c++)
                    {
                        if (placement.ClearedCells[c] == pos && !placement.ClearedCellFrozen[c])
                        {
                            bonus = ScoringConstants.SpilloverBonus;
                        }
                    }
                    break;
                case PieceTraitKind.Honeycomb:
                    AddTilePollen(placement, pos, ScoringConstants.HoneycombPollen);
                    break;
                case PieceTraitKind.Dewdrop:
                    AddTilePollen(placement, pos, placement.ClearedLineCount * ScoringConstants.DewdropPollenPerLine);
                    break;
                case PieceTraitKind.Keystone:
                    int mid0 = (GridManager.Size - 1) / 2;
                    int mid1 = GridManager.Size / 2;
                    bool corner = (pos.x == 0 || pos.x == last) && (pos.y == 0 || pos.y == last);
                    bool middle = pos.x >= mid0 && pos.x <= mid1 && pos.y >= mid0 && pos.y <= mid1;
                    if (corner || middle)
                    {
                        AddTileMult(placement, pos, ScoringConstants.KeystoneMultBonus);
                    }
                    break;
                case PieceTraitKind.Growth:
                    var growthCell = Grid.GetCell(pos);
                    bonus = 1 + growthCell.GrowthCount;
                    growthCell.GrowthCount++;
                    break;
                case PieceTraitKind.Gambler:
                    if (_rng.Next(2) == 0)
                    {
                        bonus = ScoringConstants.GamblerWinBonus;
                    }
                    else
                    {
                        AddTraitBonus(placement, pos, -ScoringConstants.GamblerLossPenalty);
                    }
                    break;
                case PieceTraitKind.Warden:
                    for (int s = 0; s < TileUpgradeSteps.Length; s++)
                    {
                        CleanseAndShield(pos + TileUpgradeSteps[s], placement);
                    }
                    break;
                case PieceTraitKind.Aegis:
                    CleanseAndShield(pos, placement);
                    break;
                case PieceTraitKind.Kin:
                    for (int s2 = 0; s2 < TileUpgradeSteps.Length; s2++)
                    {
                        if (TraitAsPlaced(pos + TileUpgradeSteps[s2], placement).HasValue)
                        {
                            bonus += ScoringConstants.KinBonusPerNeighbor;
                        }
                    }
                    break;
                case PieceTraitKind.Amplifier:
                case PieceTraitKind.Echo:
                    // Resolved once every other upgrade has scored (see ApplyNeighborCopyUpgrades).
                    break;
                case PieceTraitKind.Collector:
                    var kinds = new HashSet<PieceTraitKind>();
                    foreach (var any in GridManager.AllPositions())
                    {
                        var seen = TraitAsPlaced(any, placement);
                        if (seen.HasValue)
                        {
                            kinds.Add(seen.Value.Kind);
                        }
                    }
                    bonus = kinds.Count * ScoringConstants.CollectorBonusPerKind;
                    break;
                case PieceTraitKind.Resonance:
                    foreach (var any in GridManager.AllPositions())
                    {
                        if (TraitAsPlaced(any, placement).HasValue)
                        {
                            bonus += ScoringConstants.ResonanceBonusPerTile;
                        }
                    }
                    break;
                case PieceTraitKind.Pouch:
                    bonus = CountUpgradedDeckCards() * ScoringConstants.PouchBonusPerUpgradedSeed;
                    break;
                case PieceTraitKind.Perennial:
                    bonus = 1 + _perennialScores;
                    _perennialScores++;
                    break;
                case PieceTraitKind.Crescendo:
                    // Seeds planted before this one this round (PiecesRemainingThisRound still counts this very seed).
                    bonus = Mathf.Max(0, CurrentBudget - PiecesRemainingThisRound) * ScoringConstants.CrescendoBonusPerSeed;
                    break;
                case PieceTraitKind.Scholar:
                    int levels = 0;
                    foreach (ShapeId shapeId in System.Enum.GetValues(typeof(ShapeId)))
                    {
                        levels += GetShapeMasteryLevel(shapeId) - 1;
                    }
                    foreach (var baseColor in PieceColorUtility.BaseColors)
                    {
                        levels += GetColorMasteryLevel(baseColor) - 1;
                    }
                    bonus = Mathf.Max(0, levels) * ScoringConstants.ScholarBonusPerLevel;
                    break;
                case PieceTraitKind.FullHouse:
                    int rowFilled = 0;
                    int columnFilled = 0;
                    for (int k = 0; k < GridManager.Size; k++)
                    {
                        if (ColorAsPlaced(new Vector2Int(k, pos.y), placement).HasValue) { rowFilled++; }
                        if (ColorAsPlaced(new Vector2Int(pos.x, k), placement).HasValue) { columnFilled++; }
                    }
                    if (rowFilled >= ScoringConstants.FullHouseMinFilled) { bonus += ScoringConstants.FullHouseBonus; }
                    if (columnFilled >= ScoringConstants.FullHouseMinFilled) { bonus += ScoringConstants.FullHouseBonus; }
                    break;
                case PieceTraitKind.Compound:
                    AddTilePollen(placement, pos, Lueur * ScoringConstants.CompoundPollenPercent / 100);
                    break;
                case PieceTraitKind.Dye:
                    var dyeColor = ColorAsPlaced(pos, placement);
                    if (dyeColor.HasValue && dyeColor.Value != PieceColor.Joker && Grid.GetCell(pos).IsFilled)
                    {
                        for (int s3 = 0; s3 < TileUpgradeSteps.Length; s3++)
                        {
                            var around = pos + TileUpgradeSteps[s3];
                            if (GridManager.InBounds(around.x, around.y) && Grid.RecolorFilledTile(around, dyeColor.Value))
                            {
                                var painted = new List<(Vector2Int, PieceColor)>(placement.RecoloredTiles);
                                painted.Add((around, dyeColor.Value));
                                placement.RecoloredTiles = painted;
                            }
                        }
                    }
                    break;
                case PieceTraitKind.Pruner:
                    var keep = ColorAsPlaced(pos, placement);
                    if (keep.HasValue && Grid.GetCell(pos).IsFilled)
                    {
                        for (int s4 = 0; s4 < TileUpgradeSteps.Length; s4++)
                        {
                            var touching = pos + TileUpgradeSteps[s4];
                            if (GridManager.InBounds(touching.x, touching.y) && Grid.UprootTile(touching, keep.Value))
                            {
                                bonus += ScoringConstants.PrunerBonusPerTile;
                                var uprooted = new List<Vector2Int>(placement.UprootedTiles);
                                uprooted.Add(touching);
                                placement.UprootedTiles = uprooted;
                            }
                        }
                    }
                    break;
                case PieceTraitKind.Glow:
                    for (int s5 = 0; s5 < TileUpgradeSteps.Length; s5++)
                    {
                        var plot = pos + TileUpgradeSteps[s5];
                        if (GridManager.InBounds(plot.x, plot.y) && Grid.GildEmptyPlot(plot))
                        {
                            var gilded = new List<Vector2Int>(placement.FertilizedPlots);
                            gilded.Add(plot);
                            placement.FertilizedPlots = gilded;
                        }
                    }
                    break;
                case PieceTraitKind.Seedpod:
                    for (int c3 = 0; c3 < placement.ClearedCells.Count; c3++)
                    {
                        if (placement.ClearedCells[c3] == pos && !placement.ClearedCellFrozen[c3])
                        {
                            var podColor = placement.ClearedCellColors[c3];
                            if (podColor == PieceColor.Joker)
                            {
                                podColor = PieceColorUtility.BaseColors[_rng.Next(PieceColorUtility.BaseColors.Count)];
                            }
                            for (int pod = 0; pod < 2; pod++)
                            {
                                var landed2 = Grid.PlantFreeTileNear(pos, podColor, _rng);
                                if (landed2.HasValue)
                                {
                                    var sown2 = new List<(Vector2Int, PieceColor)>(placement.SownTiles);
                                    sown2.Add((landed2.Value, podColor));
                                    placement.SownTiles = sown2;
                                }
                            }
                        }
                    }
                    break;
                case PieceTraitKind.Locksmith:
                    var afflicted = new List<Vector2Int>();
                    foreach (var any2 in GridManager.AllPositions())
                    {
                        var plotCell = Grid.GetCell(any2);
                        if ((plotCell.IsLocked && !plotCell.IsBastion) || plotCell.IsFrozen || plotCell.IsPoisoned)
                        {
                            afflicted.Add(any2);
                        }
                    }
                    if (afflicted.Count > 0)
                    {
                        var freed = afflicted[_rng.Next(afflicted.Count)];
                        FreePlot(freed);
                        var freedList = new List<Vector2Int>(placement.FreedPlots);
                        freedList.Add(freed);
                        placement.FreedPlots = freedList;
                    }
                    break;
                case PieceTraitKind.Fertile:
                    for (int c2 = 0; c2 < placement.ClearedCells.Count; c2++)
                    {
                        if (placement.ClearedCells[c2] == pos && !placement.ClearedCellFrozen[c2])
                        {
                            Grid.GetCell(pos).IsGolden = true;
                            var fertilized = new List<Vector2Int>(placement.FertilizedPlots);
                            fertilized.Add(pos);
                            placement.FertilizedPlots = fertilized;
                        }
                    }
                    break;
                case PieceTraitKind.Sower:
                    var sownColor = ColorAsPlaced(pos, placement);
                    var tileColor = sownColor.HasValue && sownColor.Value != PieceColor.Joker ? sownColor.Value : PieceColorUtility.BaseColors[_rng.Next(PieceColorUtility.BaseColors.Count)];
                    var landed = Grid.PlantFreeTileNear(pos, tileColor, _rng);
                    if (landed.HasValue)
                    {
                        var sown = new List<(Vector2Int, PieceColor)>(placement.SownTiles);
                        sown.Add((landed.Value, tileColor));
                        placement.SownTiles = sown;
                    }
                    break;
            }
            if (bonus > 0)
            {
                AddTraitBonus(placement, pos, bonus);
            }
        }

        /// <summary>The upgrade of the tile at <paramref name="pos"/> as this placement saw it (a tile harvested by this very placement still has it).</summary>
        private PieceTrait? TraitAsPlaced(Vector2Int pos, PlacementResult placement)
        {
            if (!GridManager.InBounds(pos.x, pos.y))
            {
                return null;
            }
            var cell = Grid.GetCell(pos);
            if (cell.IsFilled && cell.OriginTrait.HasValue)
            {
                return cell.OriginTrait;
            }
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                if (placement.ClearedCells[i] == pos)
                {
                    return placement.ClearedCellTraits[i];
                }
            }
            return null;
        }

        /// <summary>Pollen from a tile upgrade, banked with the placement's other Pollen.</summary>
        private static void AddTilePollen(PlacementResult placement, Vector2Int pos, int pollen)
        {
            if (pollen <= 0)
            {
                return;
            }
            placement.ModifierLueurBonus += pollen;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, pos, pollen));
            placement.ScoreEvents = events;
        }

        /// <summary>A flat +Mult from a tile upgrade (adds to the same pool as Solidarity-style fertilizers).</summary>
        private static void AddTileMult(PlacementResult placement, Vector2Int pos, int mult)
        {
            placement.AdditiveMultBonus += mult;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, pos, mult));
            placement.ScoreEvents = events;
        }

        /// <summary>Warden / Aegis: lifts every enemy effect (lock, ice, poison) from one plot, forgets it in the enemies that own it, and makes the plot immune for the round.</summary>
        /// <summary>Frees one plot from every enemy effect (lock, ice, poison) without making it immune — Locksmith Sprout.</summary>
        private void FreePlot(Vector2Int pos)
        {
            var cell = Grid.GetCell(pos);
            if (!cell.IsBastion)
            {
                cell.IsLocked = false;
                cell.IsFrozen = false;
                cell.IsLineClearObstacle = false;
            }
            cell.IsPoisoned = false;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.LockedCell == pos) { enemy.LockedCell = null; }
                if (enemy.ExtraLockedCell == pos) { enemy.ExtraLockedCell = null; }
                enemy.RemovePoisonedCell(pos);
                enemy.FrozenCells.Remove(pos);
            }
        }

        private void CleanseAndShield(Vector2Int pos, PlacementResult placement)
        {
            if (!GridManager.InBounds(pos.x, pos.y))
            {
                return;
            }
            var cell = Grid.GetCell(pos);
            // A Bastion tile locks itself on purpose: that lock is not an enemy effect.
            if (!cell.IsBastion)
            {
                cell.IsLocked = false;
                cell.IsFrozen = false;
                cell.IsLineClearObstacle = false;
            }
            cell.IsPoisoned = false;
            cell.IsImmune = true;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.LockedCell == pos) { enemy.LockedCell = null; }
                if (enemy.ExtraLockedCell == pos) { enemy.ExtraLockedCell = null; }
                enemy.RemovePoisonedCell(pos);
                enemy.FrozenCells.Remove(pos);
            }
            var cleansed = new List<Vector2Int>(placement.CleansedCells);
            if (!cleansed.Contains(pos))
            {
                cleansed.Add(pos);
            }
            placement.CleansedCells = cleansed;
        }
    }
}
