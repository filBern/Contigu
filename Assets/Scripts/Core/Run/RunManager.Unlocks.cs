using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Progression: feeds the <see cref="UnlockTracker"/> with what happens in a run, and hides what is still locked from every pool (docs/PROGRESSION.md).</summary>
    public sealed partial class RunManager
    {
        private UnlockTracker _unlocks;
        private int _shufflesAtRoundStart;

        /// <summary>The progression tracker of this run, or null when none is attached (tests and tools that do not care about unlocks).</summary>
        public UnlockTracker Unlocks
        {
            get { return _unlocks; }
        }

        /// <summary>
        /// Attaches the player's unlock state to this run: from now on locked sprouts and fertilizers are left out of every pool, and
        /// what the run does can unlock more (read <see cref="UnlockTracker.DrainNewlyUnlocked"/>). Call it right after creating the run.
        /// </summary>
        public void AttachUnlocks(UnlockState state)
        {
            _unlocks = new UnlockTracker(state);
            Upgrades.Filter = definition => IsUpgradeAvailable(definition.Id);
            _unlocks.StartRun();
            _shufflesAtRoundStart = Stats.ShufflesUsed;
            _unlocks.Observe(UnlockMetric.RoundReached, CurrentRoundNumber);
            ObserveSnapshot();
        }

        /// <summary>False for a tile upgrade (sprout) the player has not unlocked yet; everything else is always available.</summary>
        public bool IsUpgradeAvailable(UpgradeId id)
        {
            return _unlocks == null || _unlocks.State.IsUnlocked(UnlockKind.Sprout, (int)id);
        }

        /// <summary>False for a fertilizer the player has not unlocked yet.</summary>
        public bool IsFertilizerAvailable(ModifierId id)
        {
            return _unlocks == null || _unlocks.State.IsUnlocked(UnlockKind.Fertilizer, (int)id);
        }

        private void UnlocksStartRound()
        {
            if (_unlocks == null)
            {
                return;
            }
            _unlocks.StartRound();
            _shufflesAtRoundStart = Stats.ShufflesUsed;
            _unlocks.Observe(UnlockMetric.RoundReached, CurrentRoundNumber);
            ObserveSnapshot();
        }

        /// <summary>What the seed touches, read on the garden as it stands BEFORE the seed is planted.</summary>
        private struct PlantingFacts
        {
            public int ColorsTouched;
            public int NeighborsTouched;
            public int MergedGroups;
            public int Corners;
            public bool Middle;
        }

        private PlantingFacts ReadPlantingFacts(PieceShape shape, int x, int y, PieceColor color)
        {
            var facts = new PlantingFacts();
            if (_unlocks == null)
            {
                return facts;
            }
            var own = new HashSet<Vector2Int>();
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                own.Add(new Vector2Int(x, y) + shape.Cells[i]);
            }
            int last = GridManager.Size - 1;
            int middle = GridManager.Size / 2;
            var neighbors = new HashSet<Vector2Int>();
            var otherColors = new HashSet<PieceColor>();
            foreach (var pos in own)
            {
                if ((pos.x == 0 || pos.x == last) && (pos.y == 0 || pos.y == last))
                {
                    facts.Corners++;
                }
                if (pos.x == middle && pos.y == middle)
                {
                    facts.Middle = true;
                }
                for (int s = 0; s < TileUpgradeSteps.Length; s++)
                {
                    var n = pos + TileUpgradeSteps[s];
                    if (own.Contains(n) || !GridManager.InBounds(n.x, n.y))
                    {
                        continue;
                    }
                    var cell = Grid.GetCell(n);
                    if (cell.IsFilled && cell.FilledColor.HasValue)
                    {
                        neighbors.Add(n);
                        if (cell.FilledColor.Value != color && cell.FilledColor.Value != PieceColor.Joker)
                        {
                            otherColors.Add(cell.FilledColor.Value);
                        }
                    }
                }
            }
            facts.NeighborsTouched = neighbors.Count;
            facts.ColorsTouched = otherColors.Count;
            facts.MergedGroups = CountSameColorGroups(neighbors, color);
            return facts;
        }

        /// <summary>How many separate groups of <paramref name="color"/> the given touching tiles belong to (same-color tiles connected through the garden).</summary>
        private int CountSameColorGroups(HashSet<Vector2Int> touching, PieceColor color)
        {
            var seen = new HashSet<Vector2Int>();
            int groups = 0;
            foreach (var start in touching)
            {
                if (seen.Contains(start) || Grid.GetCell(start).FilledColor != color)
                {
                    continue;
                }
                groups++;
                var stack = new Stack<Vector2Int>();
                stack.Push(start);
                seen.Add(start);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    for (int s = 0; s < TileUpgradeSteps.Length; s++)
                    {
                        var n = current + TileUpgradeSteps[s];
                        if (GridManager.InBounds(n.x, n.y) && !seen.Contains(n) && Grid.GetCell(n).IsFilled && Grid.GetCell(n).FilledColor == color)
                        {
                            seen.Add(n);
                            stack.Push(n);
                        }
                    }
                }
            }
            return groups;
        }

        private void ObservePlacementForUnlocks(PlacementResult placement, PlantingFacts facts, PieceToken token, PieceShape shape, int handIndex, bool blightScored)
        {
            if (_unlocks == null)
            {
                return;
            }
            var u = _unlocks;
            u.Observe(UnlockMetric.Score, placement.TotalScore);
            u.Observe(UnlockMetric.Lines, placement.ClearedLineCount);
            u.Observe(UnlockMetric.GroupSize, placement.GroupCells.Count);
            u.Observe(UnlockMetric.TraitPoints, placement.TraitBonus);
            u.Observe(UnlockMetric.Destroyed, placement.DestroyedCells.Count);
            u.Observe(UnlockMetric.Uprooted, placement.UprootedTiles.Count);

            var traitCells = new HashSet<Vector2Int>();
            int golden = 0;
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var e = placement.ScoreEvents[i];
                if (e.Type == ScoreEventType.Trait && e.Amount > 0) { traitCells.Add(e.Position); }
                if (e.Type == ScoreEventType.Golden && e.Amount > 0) { golden++; }
            }
            u.Observe(UnlockMetric.TraitCells, traitCells.Count);
            u.Observe(UnlockMetric.GoldenScored, golden);
            int frozen = 0;
            for (int i = 0; i < placement.ClearedCellFrozen.Count; i++)
            {
                if (placement.ClearedCellFrozen[i]) { frozen++; }
            }
            u.Observe(UnlockMetric.FrozenBroken, frozen);
            u.Observe(UnlockMetric.GardenCleared, Grid.FilledCellCount == 0 && placement.ClearedCells.Count > 0 ? 1 : 0);

            u.Observe(UnlockMetric.ColorsTouched, facts.ColorsTouched);
            u.Observe(UnlockMetric.NeighborsTouched, facts.NeighborsTouched);
            u.Observe(UnlockMetric.MergedGroups, facts.MergedGroups);
            u.Observe(UnlockMetric.Corners, facts.Corners);
            u.Observe(UnlockMetric.Middle, facts.Middle ? 1 : 0);
            u.Observe(UnlockMetric.Isolated, facts.NeighborsTouched == 0 ? 1 : 0);
            u.Observe(UnlockMetric.BlightScored, blightScored ? 1 : 0);

            u.Observe(UnlockMetric.Seeds, 1);
            int cells = shape.Cells.Count;
            u.Observe(cells <= 2 ? UnlockMetric.SeedSize12 : cells == 3 ? UnlockMetric.SeedSize3 : UnlockMetric.SeedSize4, 1);
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int i = 0; i < cells; i++)
            {
                minX = Mathf.Min(minX, shape.Cells[i].x); maxX = Mathf.Max(maxX, shape.Cells[i].x);
                minY = Mathf.Min(minY, shape.Cells[i].y); maxY = Mathf.Max(maxY, shape.Cells[i].y);
            }
            int width = maxX - minX + 1, height = maxY - minY + 1;
            if (token.Shape == ShapeId.Sq2) { u.Observe(UnlockMetric.SquarePlayed, 1); }
            if (width == 1 && height >= 2) { u.Observe(UnlockMetric.VerticalSeed, 1); }
            if (height == 1 && width >= 2) { u.Observe(UnlockMetric.HorizontalSeed, 1); }
            if ((width == 1 || height == 1) && cells >= 3) { u.Observe(UnlockMetric.Straight3, 1); }
            if (handIndex == 0) { u.Observe(UnlockMetric.SlotPlayed1, 1); }
            else if (handIndex == 1) { u.Observe(UnlockMetric.SlotPlayed2, 1); }
            else if (handIndex == 2) { u.Observe(UnlockMetric.SlotPlayed3, 1); }
            int traitCount = token.Traits.Count;
            if (traitCount >= 1) { u.Observe(UnlockMetric.SeedUpgraded, 1); }
            if (traitCount >= 2) { u.Observe(UnlockMetric.SeedTwoUpgrades, 1); }
            if (placement.ClearedLineCount > 0 && _roundPlacements == 1) { u.Observe(UnlockMetric.HarvestOnFirstSeedOfRound, 1); }

            u.ObserveSequence(token.Shape, token.Color, placement.ClearedLineCount, placement.GroupCells.Count);
            ObserveSnapshot();
        }

        /// <summary>The hand was just emptied by this placement.</summary>
        private void ObserveHandEmptied(PlacementResult placement)
        {
            if (_unlocks == null)
            {
                return;
            }
            _unlocks.Observe(UnlockMetric.HandEmptied, 1);
            if (placement.ClearedLineCount > 0)
            {
                _unlocks.Observe(UnlockMetric.HarvestOnLastSeedOfHand, 1);
            }
        }

        /// <summary>The run as it stands: Pollen, fertilizers, bag and garden (observed after every placement and at each round start).</summary>
        private void ObserveSnapshot()
        {
            if (_unlocks == null)
            {
                return;
            }
            var u = _unlocks;
            u.Observe(UnlockMetric.Pollen, Lueur);
            u.Observe(UnlockMetric.FertilizersHeld, _activeModifiers.Count);
            var categories = new HashSet<ModifierCategory>();
            int devotions = 0;
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                var id = _activeModifiers[i];
                var definition = ModifierCatalog.Get(id);
                if (definition != null) { categories.Add(definition.Category); }
                if (id == ModifierId.DevotionCoral || id == ModifierId.DevotionTeal || id == ModifierId.DevotionViolet || id == ModifierId.DevotionLime) { devotions++; }
            }
            u.Observe(UnlockMetric.FertilizerCategories, categories.Count);
            u.Observe(UnlockMetric.DevotionsHeld, devotions);

            var tokens = Deck.Deck;
            var shapes = new HashSet<ShapeId>();
            var bagKinds = new HashSet<PieceTraitKind>();
            var perColor = new Dictionary<PieceColor, int>();
            int upgraded = 0, jokers = 0, mostOfOneColor = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                shapes.Add(tokens[i].Shape);
                var traits = tokens[i].Traits;
                if (traits.Count > 0) { upgraded++; }
                for (int t = 0; t < traits.Count; t++) { bagKinds.Add(traits[t].Kind); }
                if (tokens[i].Color == PieceColor.Joker) { jokers++; continue; }
                int soFar;
                perColor.TryGetValue(tokens[i].Color, out soFar);
                perColor[tokens[i].Color] = ++soFar;
                mostOfOneColor = Mathf.Max(mostOfOneColor, soFar);
            }
            u.Observe(UnlockMetric.BagSize, tokens.Count);
            u.Observe(UnlockMetric.BagUpgraded, upgraded);
            u.Observe(UnlockMetric.BagUpgradeKinds, bagKinds.Count);
            u.Observe(UnlockMetric.BagShapes, shapes.Count);
            u.Observe(UnlockMetric.BagJokers, jokers);
            u.Observe(UnlockMetric.BagOneColor, mostOfOneColor);

            int filled = 0, gardenUpgraded = 0, goldenPlots = 0, cornersFilled = 0, afflicted = 0;
            var gardenKinds = new HashSet<PieceTraitKind>();
            var gardenColors = new HashSet<PieceColor>();
            int last = GridManager.Size - 1;
            foreach (var pos in GridManager.AllPositions())
            {
                var cell = Grid.GetCell(pos);
                if (cell.IsFilled)
                {
                    filled++;
                    if (cell.FilledColor.HasValue && cell.FilledColor.Value != PieceColor.Joker) { gardenColors.Add(cell.FilledColor.Value); }
                    if (cell.OriginTrait.HasValue) { gardenUpgraded++; gardenKinds.Add(cell.OriginTrait.Value.Kind); }
                    if ((pos.x == 0 || pos.x == last) && (pos.y == 0 || pos.y == last)) { cornersFilled++; }
                }
                if (cell.IsGolden) { goldenPlots++; }
                if ((cell.IsLocked && !cell.IsBastion) || cell.IsFrozen || cell.IsPoisoned) { afflicted++; }
            }
            u.Observe(UnlockMetric.GardenTiles, filled);
            u.Observe(UnlockMetric.GardenUpgraded, gardenUpgraded);
            u.Observe(UnlockMetric.GardenUpgradeKinds, gardenKinds.Count);
            u.Observe(UnlockMetric.GardenColors, gardenColors.Count);
            u.Observe(UnlockMetric.GoldenPlots, goldenPlots);
            u.Observe(UnlockMetric.CornersFilled, cornersFilled);
            u.Observe(UnlockMetric.AfflictedPlots, afflicted);
            u.Observe(UnlockMetric.GardenFillPercent, filled * 100 / (GridManager.Size * GridManager.Size));
            u.Observe(UnlockMetric.MasteryLevels, CountMasteryLevelsEarned());
        }

        /// <summary>After a Shuffle: how many plots the enemies are holding hostage right now.</summary>
        private void ObserveShuffleForUnlocks()
        {
            if (_unlocks == null)
            {
                return;
            }
            int afflicted = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                var cell = Grid.GetCell(pos);
                if ((cell.IsLocked && !cell.IsBastion) || cell.IsFrozen || cell.IsPoisoned) { afflicted++; }
            }
            _unlocks.Observe(UnlockMetric.PlotsAfflicted, afflicted);
            _unlocks.Observe(UnlockMetric.AfflictedPlots, afflicted);
        }

        private void ObserveRoundClearedForUnlocks()
        {
            if (_unlocks == null)
            {
                return;
            }
            var u = _unlocks;
            var weather = WeatherCatalog.Mechanic(CurrentWeather);
            if (CurrentWeather != WeatherId.Clear) { u.Observe(UnlockMetric.RoundClearedWeather, 1); }
            if (weather == WeatherId.Sunshine || weather == WeatherId.GoldenHour) { u.Observe(UnlockMetric.RoundClearedSunny, 1); }
            if (WeatherCatalog.IsHarsh(CurrentWeather)) { u.Observe(UnlockMetric.RoundClearedHarshWeather, 1); }
            if (weather == WeatherId.Frost) { u.Observe(UnlockMetric.RoundClearedFrost, 1); }
            if (weather == WeatherId.Wind || weather == WeatherId.Hurricane) { u.Observe(UnlockMetric.RoundClearedWind, 1); }
            if (Stats.ShufflesUsed == _shufflesAtRoundStart) { u.Observe(UnlockMetric.RoundClearedNoShuffle, 1); }
            if (_roundLines == 0) { u.Observe(UnlockMetric.RoundClearedNoHarvest, 1); }
            if (PiecesRemainingThisRound == 0) { u.Observe(UnlockMetric.RoundClearedOnLastSeed, 1); }
            u.Observe(UnlockMetric.SeedsLeftAtClear, PiecesRemainingThisRound);
            u.Observe(UnlockMetric.GardenFillPercentAtClear, Grid.FilledCellCount * 100 / (GridManager.Size * GridManager.Size));
            if (CurrentRoundIndex >= 2)
            {
                bool clean = true;
                foreach (var pos in GridManager.AllPositions())
                {
                    var cell = Grid.GetCell(pos);
                    if ((cell.IsLocked && !cell.IsBastion) || cell.IsFrozen || cell.IsPoisoned) { clean = false; break; }
                }
                if (clean) { u.Observe(UnlockMetric.RoundClearedClean, 1); }
            }
            if (_challenge.Id == ChallengeId.Marathon) { u.Observe(UnlockMetric.ClearedWinterRound, 1); }
            if (_challenge.Id == ChallengeId.Greenhouse) { u.Observe(UnlockMetric.ClearedGreenhouseRound, 1); }
        }

        private void ObserveRunWonForUnlocks()
        {
            if (_unlocks == null)
            {
                return;
            }
            _unlocks.Observe(UnlockMetric.RunWon, 1);
            if (_challenge.Id == ChallengeId.Classic)
            {
                _unlocks.Observe(UnlockMetric.RunWonSpring, 1);
            }
        }

        private void ObserveEnemyDefeatedForUnlocks(EnemyInstance enemy)
        {
            if (_unlocks == null)
            {
                return;
            }
            var u = _unlocks;
            u.Observe(UnlockMetric.EnemiesDefeated, 1);
            u.Observe(UnlockMetric.EnemyDefeatedSeedsLeft, Mathf.Max(0, PiecesRemainingThisRound - 1));
            switch (enemy.Definition.Id)
            {
                case EnemyId.Locker: u.Observe(UnlockMetric.DefeatedBramble, 1); break;
                case EnemyId.Poisoner: u.Observe(UnlockMetric.DefeatedBlightspore, 1); break;
                case EnemyId.ColorHater:
                case EnemyId.ShapeHater: u.Observe(UnlockMetric.DefeatedHaterBoss, 1); break;
                case EnemyId.Leech:
                case EnemyId.Reclaimer: u.Observe(UnlockMetric.DefeatedLeechOrVine, 1); break;
                case EnemyId.Hoarfrost:
                case EnemyId.Locust:
                case EnemyId.Rotgrub:
                case EnemyId.Fruitworm:
                case EnemyId.Whitefly: u.Observe(UnlockMetric.DefeatedResident, 1); break;
            }
            if (enemy.Definition.Role == EnemyRole.Boss)
            {
                u.Observe(UnlockMetric.DefeatedBoss, 1);
            }
        }
    }
}
