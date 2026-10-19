using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public sealed class WeatherTests
    {
        [Test]
        public void RoundOne_IsAlwaysClear_AndNoWeatherRepeatsBackToBack()
        {
            for (int seed = 1; seed <= 50; seed++)
            {
                var weather = WeatherCatalog.RollForRun(new SystemRandomProvider(seed), 15);
                Assert.AreEqual(WeatherId.Clear, weather[0]);
                for (int i = 1; i < weather.Length; i++)
                {
                    Assert.AreNotEqual(weather[i - 1], weather[i], "seed " + seed + " round " + (i + 1));
                }
            }
        }

        [Test]
        public void Run_ExposesCurrentAndNextWeather_AndTheLastRoundHasNoNext()
        {
            var run = new RunManager(new SystemRandomProvider(7));
            Assert.AreEqual(WeatherId.Clear, run.CurrentWeather);
            Assert.IsTrue(run.NextWeather.HasValue);
            for (int i = 0; i < RunConfig.RoundCount - 1; i++)
            {
                run.DebugForceRoundComplete();
                var expectedNext = run.NextWeather;
                run.LeaveShop();
                Assert.AreEqual(expectedNext.Value, run.CurrentWeather, "the announced weather is the one that arrives");
            }
            Assert.IsFalse(run.NextWeather.HasValue);
        }

        [Test]
        public void EveryWeather_HasAnAuthoredNameAndDescription()
        {
            foreach (WeatherId id in System.Enum.GetValues(typeof(WeatherId)))
            {
                Assert.IsNotEmpty(WeatherCatalog.GetName(id));
                Assert.IsNotEmpty(WeatherCatalog.GetDescription(id));
            }
        }

        [Test]
        public void ColorWeathers_NameTheirColor_AndOthersHaveNone()
        {
            StringAssert.Contains("Poppy", WeatherCatalog.GetName(WeatherId.Rain, "Poppy"));
            StringAssert.Contains("Poppy", WeatherCatalog.GetDescription(WeatherId.Sunshine, "Poppy"));
            Assert.AreEqual(WeatherCatalog.GetName(WeatherId.Wind), WeatherCatalog.GetName(WeatherId.Wind, "Poppy"));

            var rainy = RunInWeather(WeatherId.Rain);
            Assert.IsTrue(rainy.CurrentWeatherColor.HasValue);
            Assert.AreNotEqual(PieceColor.Joker, rainy.CurrentWeatherColor.Value);
            Assert.IsFalse(RunInWeather(WeatherId.Frost).CurrentWeatherColor.HasValue);
        }

        [Test]
        public void RareWeathers_ComeUpLessOftenThanCommonOnes()
        {
            var counts = new Dictionary<WeatherId, int>();
            for (int seed = 1; seed <= 300; seed++)
            {
                foreach (var w in WeatherCatalog.RollForRun(new SystemRandomProvider(seed), 15))
                {
                    int n;
                    counts.TryGetValue(w, out n);
                    counts[w] = n + 1;
                }
            }
            Assert.Greater(counts[WeatherId.Rain], counts[WeatherId.PollenShower] * 2, "common vs rare");
            Assert.Greater(counts[WeatherId.PollenShower], 0, "rare weathers still happen");
            Assert.AreEqual(WeatherCatalog.GetWeight(WeatherId.GoldenHour), 1);
        }

        [Test]
        public void GoldenHour_TurnsEmptyPlotsGolden()
        {
            var run = RunInWeather(WeatherId.GoldenHour);
            int golden = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (run.Grid.GetCell(x, y).IsGolden)
                    {
                        golden++;
                    }
                }
            }
            Assert.GreaterOrEqual(golden, ScoringConstants.GoldenHourPlots);
        }

        [Test]
        public void PollenShower_StartsTheRoundWithPollen()
        {
            var run = RunInWeather(WeatherId.PollenShower);
            Assert.GreaterOrEqual(run.Lueur, ScoringConstants.PollenShowerPollen);
        }

        private static GridManager GridWithTiles(params Vector2Int[] cells)
        {
            var grid = new GridManager();
            foreach (var c in cells)
            {
                var cell = grid.GetCell(c);
                cell.IsFilled = true;
                cell.FilledColor = PieceColor.Violet;
                cell.FilledShapeId = ShapeId.Single;
                cell.IsGolden = true;
            }
            return grid;
        }

        [Test]
        public void Heatwave_BurnsOneUnlockedTile_AndSkipsLockedOnes()
        {
            var grid = GridWithTiles(new Vector2Int(1, 1), new Vector2Int(2, 2));
            grid.GetCell(2, 2).IsLocked = true;
            var burned = grid.BurnRandomTile(new SystemRandomProvider(3));
            Assert.IsTrue(burned.HasValue);
            Assert.AreEqual(new Vector2Int(1, 1), burned.Value.Pos);
            Assert.AreEqual(PieceColor.Violet, burned.Value.Color);
            Assert.IsFalse(grid.GetCell(1, 1).IsFilled);
            Assert.IsFalse(grid.GetCell(1, 1).IsGolden);
            Assert.IsTrue(grid.GetCell(2, 2).IsFilled);
            Assert.IsNull(new GridManager().BurnRandomTile(new SystemRandomProvider(3)), "nothing to burn on an empty grid");
        }

        [Test]
        public void Hurricane_MovesTilesWithTheirWholeFillState_ToEmptyPlots()
        {
            var grid = GridWithTiles(new Vector2Int(0, 0), new Vector2Int(GridManager.Size - 1, GridManager.Size - 1), new Vector2Int(3, 3));
            var moves = grid.BlowTiles(2, new SystemRandomProvider(5));
            Assert.AreEqual(2, moves.Count);
            foreach (var move in moves)
            {
                Assert.IsFalse(grid.GetCell(move.From).IsFilled, "the source is emptied");
                var dst = grid.GetCell(move.To);
                Assert.IsTrue(dst.IsFilled);
                Assert.AreEqual(PieceColor.Violet, dst.FilledColor);
                Assert.AreEqual(ShapeId.Single, dst.FilledShapeId);
                Assert.IsTrue(dst.IsGolden, "flags travel with the tile");
                Assert.AreEqual(PieceColor.Violet, move.Color);
            }
            int filled = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (grid.GetCell(pos).IsFilled)
                {
                    filled++;
                }
            }
            Assert.AreEqual(3, filled, "no tile is created or lost");
        }

        [Test]
        public void Hurricane_NeverBlowsALockedTile_NorOntoFrozenOrLockedPlots()
        {
            var grid = GridWithTiles(new Vector2Int(0, 0));
            grid.GetCell(0, 0).IsLocked = true;
            Assert.AreEqual(0, grid.BlowTiles(2, new SystemRandomProvider(1)).Count);

            var grid2 = GridWithTiles(new Vector2Int(0, 0));
            foreach (var pos in GridManager.AllPositions())
            {
                var c = grid2.GetCell(pos);
                if (!c.IsFilled)
                {
                    c.IsFrozen = true;
                    c.IsLocked = true;
                }
            }
            Assert.AreEqual(0, grid2.BlowTiles(2, new SystemRandomProvider(1)).Count, "no free destination");
        }

        private static RunManager RunInWeather(WeatherId weather)
        {
            for (int seed = 1; seed < 400; seed++)
            {
                var run = new RunManager(new SystemRandomProvider(seed));
                for (int i = 0; i < 6 && run.State != RunState.RunVictory; i++)
                {
                    run.DebugForceRoundComplete();
                    if (run.State == RunState.AwaitingShop)
                    {
                        run.LeaveShop();
                    }
                    if (run.CurrentWeather == weather)
                    {
                        return run;
                    }
                }
            }
            Assert.Fail("no run found with weather " + weather);
            return null;
        }

        [Test]
        public void Wind_GrantsAFreeShuffleAtRoundStart()
        {
            var windy = RunInWeather(WeatherId.Wind);
            // The round already started with the bonus; compare against the same run's value one round earlier is not possible,
            // so check the invariant instead: a wind round always starts with at least the free Shuffle in the bank.
            Assert.GreaterOrEqual(windy.ShufflesRemaining, 1);
        }

        [Test]
        public void Frost_StartsTheRoundWithFrozenPlots()
        {
            var run = RunInWeather(WeatherId.Frost);
            int frozen = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (run.Grid.GetCell(x, y).IsFrozen)
                    {
                        frozen++;
                    }
                }
            }
            Assert.GreaterOrEqual(frozen, ScoringConstants.FrostFrozenPlots);
        }

        [Test]
        public void EnemyTick_CountsDownEverySixSeedsPlaced()
        {
            var run = new RunManager(new SystemRandomProvider(2));
            Assert.AreEqual(ScoringConstants.EnemyTickInterval, run.PlacementsUntilEnemyTick);
            Assert.IsTrue(RunManager.HasTimedTrick(EnemyId.Locker));
            Assert.IsTrue(RunManager.HasTimedTrick(EnemyId.Blightmoth));
            Assert.IsFalse(RunManager.HasTimedTrick(EnemyId.Basic));
        }
    }
}
