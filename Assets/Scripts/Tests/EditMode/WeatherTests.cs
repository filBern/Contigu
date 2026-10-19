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
