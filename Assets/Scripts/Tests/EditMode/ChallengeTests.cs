using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>The seasonal challenges (Summer, Autumn, Orchard, Greenhouse) and Endless mode.</summary>
    public sealed class ChallengeTests
    {
        // GridManager.Size and PieceColorUtility.BaseColors are process-wide, set per run: leave them as every other test expects.
        [TearDown]
        public void RestoreProcessWideRules()
        {
            GridManager.ResetSize();
            PieceColorUtility.ResetBaseColors();
        }

        private static RunManager NewRun(ChallengeDefinition challenge, int seed = 3)
        {
            var run = new RunManager(new SystemRandomProvider(seed), challenge);
            run.DebugDisableWeather();
            return run;
        }

        private static void ClearRoundAndLeaveShop(RunManager run)
        {
            run.DebugForceRoundComplete();
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            Assert.IsTrue(run.LeaveShop());
        }

        private static RunManager RunAtVictory(ChallengeDefinition challenge, int seed = 3)
        {
            var run = NewRun(challenge, seed);
            for (int i = 0; i < challenge.RoundCount - 1; i++)
            {
                ClearRoundAndLeaveShop(run);
            }
            run.DebugForceRoundComplete();
            Assert.AreEqual(RunState.RunVictory, run.State);
            return run;
        }

        [Test]
        public void EveryChallenge_HasAUniqueIdAndAUnlockPath()
        {
            var seen = new HashSet<ChallengeId>();
            foreach (var challenge in ChallengeCatalog.All)
            {
                Assert.IsTrue(seen.Add(challenge.Id), "duplicate " + challenge.Id);
                Assert.IsNotEmpty(challenge.Name);
                Assert.IsNotEmpty(challenge.Description);
                Assert.AreSame(challenge, ChallengeCatalog.Get(challenge.Id));
                Assert.IsTrue(new MetaStats().IsUnlocked(challenge.Id) == (challenge.UnlockCost == 0));
                var unlocked = MetaStatsRecorder.TryUnlockChallenge(new MetaStats { Stars = 99 }, challenge);
                Assert.IsTrue(unlocked.Success);
                Assert.IsTrue(unlocked.Updated.IsUnlocked(challenge.Id), challenge.Name);
            }
            Assert.AreEqual(6, ChallengeCatalog.All.Length);
        }

        [Test]
        public void Summer_NeverHasCalmSkiesAfterRoundOne_AndItsWeatherHitsHarder()
        {
            var weather = WeatherCatalog.RollForRun(new SystemRandomProvider(5), 15, out var colors, WeatherProfile.Extreme);
            Assert.AreEqual(WeatherId.Clear, weather[0]);
            for (int i = 1; i < weather.Length; i++)
            {
                Assert.AreNotEqual(WeatherId.Clear, weather[i], "round " + (i + 1));
            }
            Assert.AreEqual(3, WeatherCatalog.Scale(2, 1.5f));
            Assert.AreEqual(5, WeatherCatalog.Scale(3, 1.5f), "4.5 rounds up");
            Assert.AreEqual(2, WeatherCatalog.Scale(2, 1f));
            StringAssert.Contains("+" + WeatherCatalog.Scale(ScoringConstants.RainPollenPerSeed, 1.5f), WeatherCatalog.GetDescription(WeatherId.Rain, "Poppy", 1.5f));
            Assert.AreEqual(1.5f, new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Summer).WeatherIntensity);
        }

        [Test]
        public void Autumn_FertilizersWitherAfterTheirLifespan_AndEveryRoundPaysExtraPollen()
        {
            var run = NewRun(ChallengeCatalog.Autumn);
            run.DebugGrantModifier(ModifierId.MultDeux);
            Assert.AreEqual(ChallengeCatalog.Autumn.FertilizerLifespanRounds, run.GetModifierRoundsLeft(0));

            int pollenBefore = run.Lueur;
            run.DebugForceRoundComplete();
            Assert.GreaterOrEqual(run.Lueur - pollenBefore, ChallengeCatalog.Autumn.RoundEndPollenBonus, "the round-end bonus");
            Assert.AreEqual(1, run.GetModifierAge(0));
            Assert.AreEqual(0, run.LastWitheredModifiers.Count);
            run.LeaveShop();

            for (int i = 1; i < ChallengeCatalog.Autumn.FertilizerLifespanRounds - 1; i++)
            {
                ClearRoundAndLeaveShop(run);
            }
            Assert.AreEqual(ChallengeCatalog.Autumn.FertilizerLifespanRounds - 1, run.GetModifierAge(0));
            CollectionAssert.Contains(run.ActiveModifiers, ModifierId.MultDeux);

            run.DebugForceRoundComplete();
            CollectionAssert.DoesNotContain(run.ActiveModifiers, ModifierId.MultDeux, "withered at the end of its last round");
            CollectionAssert.AreEqual(new[] { ModifierId.MultDeux }, run.LastWitheredModifiers);
            Assert.AreEqual(1, run.LastSproutedModifiers.Count, "a new fertilizer sprouts in its place");
            Assert.AreEqual(1, run.ActiveModifiers.Count);
            Assert.AreEqual(run.LastSproutedModifiers[0], run.ActiveModifiers[0]);
            Assert.AreNotEqual(ModifierId.MultDeux, run.ActiveModifiers[0]);
            Assert.AreEqual(0, run.GetModifierAge(0), "the sprout starts young");
        }

        [Test]
        public void Autumn_AgeTravelsWithTheFertilizer_WhenItIsMoved()
        {
            var run = NewRun(ChallengeCatalog.Autumn);
            run.DebugGrantModifier(ModifierId.MultUn);
            run.DebugForceRoundComplete();
            run.LeaveShop();
            run.DebugGrantModifier(ModifierId.MultDeux);
            Assert.AreEqual(1, run.GetModifierAge(0));
            Assert.AreEqual(0, run.GetModifierAge(1));

            run.SwapModifiers(0, 1);
            Assert.AreEqual(ModifierId.MultDeux, run.ActiveModifiers[0]);
            Assert.AreEqual(0, run.GetModifierAge(0));
            Assert.AreEqual(1, run.GetModifierAge(1));

            run.MoveModifier(1, 0);
            Assert.AreEqual(ModifierId.MultUn, run.ActiveModifiers[0]);
            Assert.AreEqual(1, run.GetModifierAge(0));
        }

        [Test]
        public void ClassicFertilizers_NeverWither()
        {
            var run = NewRun(ChallengeCatalog.Classic);
            run.DebugGrantModifier(ModifierId.MultDeux);
            for (int i = 0; i < 5; i++)
            {
                ClearRoundAndLeaveShop(run);
            }
            CollectionAssert.Contains(run.ActiveModifiers, ModifierId.MultDeux);
            Assert.AreEqual(0, run.GetModifierRoundsLeft(0));
        }

        [Test]
        public void Orchard_KeepsTwoColors_InTheBagAndEverywhere_AndRestoresThemAfter()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = NewRun(ChallengeCatalog.Orchard, seed);
                Assert.AreEqual(2, PieceColorUtility.BaseColors.Count);
                Assert.AreEqual(32, run.Deck.DeckCount, "same bag size, doubled copies");
                foreach (var key in run.Deck.GetDeckComposition().Keys)
                {
                    Assert.IsTrue(PieceColorUtility.IsActive(key.Color), "a missing color in the bag: " + key.Color);
                }
                Assert.IsFalse(run.IsModifierUsableThisRun(ModifierId.Prisme));
                Assert.IsFalse(run.IsModifierUsableThisRun(ModifierId.Tricolore));
                var missing = PieceColor.Coral;
                foreach (var color in PieceColorUtility.AllBaseColors)
                {
                    if (!PieceColorUtility.IsActive(color))
                    {
                        missing = color;
                    }
                }
                var devotionOfMissing = (ModifierId)System.Enum.Parse(typeof(ModifierId), "Devotion" + missing);
                Assert.IsFalse(run.IsModifierUsableThisRun(devotionOfMissing));
            }

            new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Classic);
            Assert.AreEqual(4, PieceColorUtility.BaseColors.Count, "a normal run brings every color back");
        }

        [Test]
        public void Orchard_ShopNeverOffersAFertilizerThatCouldNeverFire()
        {
            for (int seed = 1; seed <= 15; seed++)
            {
                var run = NewRun(ChallengeCatalog.Orchard, seed);
                run.DebugForceRoundComplete();
                for (int i = 0; i < run.ShopBlisterSlots.Count; i++)
                {
                    var slot = run.ShopBlisterSlots[i];
                    if (slot.Kind == ShopSlotKind.Modifier)
                    {
                        Assert.IsTrue(run.IsModifierUsableThisRun(slot.ModifierId), slot.ModifierId.ToString());
                    }
                }
            }
        }

        [Test]
        public void Greenhouse_PlaysOnASmallerPlot_AndNormalRunsGoBackToTheFullSize()
        {
            var run = NewRun(ChallengeCatalog.Greenhouse);
            Assert.AreEqual(5, GridManager.Size);
            Assert.IsFalse(GridManager.InBounds(5, 0));
            Assert.IsTrue(GridManager.InBounds(4, 4));
            int cells = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                cells++;
            }
            Assert.AreEqual(25, cells);

            new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Classic);
            Assert.AreEqual(GridManager.DefaultSize, GridManager.Size);
        }

        [Test]
        public void EveryChallenge_CanBePlayedThroughPlacementsWithoutErrors()
        {
            foreach (var challenge in ChallengeCatalog.All)
            {
                var run = new RunManager(new SystemRandomProvider(9), challenge);
                for (int i = 0; i < 12 && run.State == RunState.InProgress; i++)
                {
                    int slot = -1;
                    for (int h = 0; h < run.Deck.Hand.Count && slot < 0; h++)
                    {
                        if (run.Deck.Hand[h].HasValue && !run.IsHandSlotLocked(h))
                        {
                            slot = h;
                        }
                    }
                    if (slot < 0)
                    {
                        run.ShuffleHand();
                        continue;
                    }
                    var token = run.Deck.Hand[slot].Value;
                    var shape = PieceShapeCatalog.GetRotated(token.Shape, run.Deck.HandRotations[slot]);
                    bool placed = false;
                    foreach (var pos in GridManager.AllPositions())
                    {
                        if (run.Grid.CanPlace(shape, pos.x, pos.y))
                        {
                            Assert.IsTrue(run.PlacePiece(slot, pos.x, pos.y).Placement.Success, challenge.Name);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        run.ShuffleHand();
                    }
                }
            }
        }

        [Test]
        public void Endless_AfterTheFinalRoundTheRunKeepsGoing_WithCompoundingHp()
        {
            var run = RunAtVictory(ChallengeCatalog.Classic);
            Assert.IsFalse(run.IsEndless);
            Assert.IsFalse(run.HasNextRound, "a normal run ends here");

            Assert.IsTrue(run.ContinueEndless());
            Assert.IsTrue(run.IsEndless);
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            Assert.IsTrue(run.HasNextRound);
            Assert.IsFalse(run.ContinueEndless(), "only once, and only from a victory");

            Assert.IsTrue(run.LeaveShop());
            Assert.AreEqual(16, run.CurrentRoundNumber);
            Assert.AreEqual(RunState.InProgress, run.State);
            Assert.IsTrue(run.HasActiveEncounter);
            // Round 16 replays round 9's roster, far sturdier.
            var roster = EncounterCatalog.GetEncounter(ChallengeId.Classic, 8);
            Assert.AreEqual(roster.Count, run.CurrentEncounter.Count);
            int round9Hp = EncounterCatalog.ScaleHp(run.CurrentEncounter[1].Definition.MaxHp, EncounterCatalog.GetHpScale(8));
            Assert.Greater(run.CurrentEncounter[1].CurrentMaxHp, round9Hp);
        }

        [Test]
        public void Endless_HpGrowsEveryRound_WithoutCeilingAndWithoutOverflow()
        {
            float previous = EncounterCatalog.GetHpScale(14);
            for (int round = 15; round < 120; round++)
            {
                float scale = EncounterCatalog.GetHpScale(round);
                Assert.GreaterOrEqual(scale, previous, "round " + (round + 1));
                Assert.IsFalse(float.IsInfinity(scale) || float.IsNaN(scale));
                Assert.GreaterOrEqual(EncounterCatalog.ScaleHp(3550, scale), 3550);
                previous = scale;
            }
            Assert.Greater(EncounterCatalog.GetHpScale(20), EncounterCatalog.GetHpScale(14) * 2f);
        }

        [Test]
        public void Endless_WorksForAQuotaOnlyRuleToo_WithGrowingQuotas()
        {
            var run = RunAtVictory(TestChallenges.QuotaOnlyMarathon);
            Assert.IsTrue(run.ContinueEndless());
            Assert.IsTrue(run.LeaveShop());
            Assert.AreEqual(16, run.CurrentRoundNumber);
            Assert.IsFalse(run.HasActiveEncounter, "a quota-only challenge has no authored enemies");
            Assert.Greater(run.CurrentQuota, TestChallenges.QuotaOnlyMarathon.QuotaFor(14));
            Assert.AreEqual(TestChallenges.QuotaOnlyMarathon.BudgetFor(14), run.CurrentBudget);
            Assert.Greater(TestChallenges.QuotaOnlyMarathon.QuotaFor(40), TestChallenges.QuotaOnlyMarathon.QuotaFor(20));
            Assert.LessOrEqual(TestChallenges.QuotaOnlyMarathon.QuotaFor(500), 2000000000);
        }

        [Test]
        public void Endless_KeepsRollingWeather_AndAnnouncesTheNextOne()
        {
            var run = new RunManager(new SystemRandomProvider(4), ChallengeCatalog.Classic);
            for (int i = 0; i < 14; i++)
            {
                ClearRoundAndLeaveShop(run);
            }
            run.DebugForceRoundComplete();
            run.ContinueEndless();
            Assert.IsTrue(run.NextWeather.HasValue, "the forecast for round 16 exists");
            var announced = run.NextWeather.Value;
            run.LeaveShop();
            Assert.AreEqual(announced, run.CurrentWeather);
        }

        [Test]
        public void UnlockAllChallenges_OpensEveryChallenge_WithoutTouchingStarsOrRecords()
        {
            var before = new MetaStats { Stars = 3, BestScore = 777, TotalVictories = 2 };
            var after = MetaStatsRecorder.UnlockAllChallenges(before);
            foreach (var challenge in ChallengeCatalog.All)
            {
                Assert.IsTrue(after.IsUnlocked(challenge.Id), challenge.Name);
            }
            Assert.AreEqual(3, after.Stars);
            Assert.AreEqual(777, after.BestScore);
            Assert.AreEqual(2, after.TotalVictories);
            Assert.IsFalse(before.MarathonUnlocked, "the original is left alone");
        }

        [Test]
        public void EndlessEnd_PaysOneStarPerExtraRound_WithoutCountingTheRunTwice()
        {
            var afterVictory = MetaStatsRecorder.RecordRunOutcome(new MetaStats(), 5000, RunConfig.RoundCount, victory: true);
            var afterEndless = MetaStatsRecorder.RecordEndlessEnd(afterVictory, 90000, RunConfig.RoundCount + 4);

            Assert.AreEqual(afterVictory.TotalRunsPlayed, afterEndless.TotalRunsPlayed);
            Assert.AreEqual(afterVictory.TotalVictories, afterEndless.TotalVictories);
            Assert.AreEqual(afterVictory.Stars + 3, afterEndless.Stars, "died in round 19: rounds 16, 17 and 18 were cleared");
            Assert.AreEqual(90000, afterEndless.BestScore);
            Assert.AreEqual(RunConfig.RoundCount + 4, afterEndless.BestRoundReached);
        }
    }
}
