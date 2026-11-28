using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>"Something happens": when events come, what they show, and what every effect really does.</summary>
    public sealed class EventTests
    {
        private static RunManager NewRun(int seed = 5)
        {
            var run = new RunManager(new SystemRandomProvider(seed));
            run.DebugDisableWeather();
            run.EventsEnabled = true;
            return run;
        }

        private static void ClearRoundAndLeaveShop(RunManager run, bool resolveEvent = true)
        {
            run.DebugForceRoundComplete();
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            if (resolveEvent && run.PendingEvent != null)
            {
                Assert.IsTrue(run.ResolveEvent(run.PendingEvent.Options.Length - 1), "walking on is always possible");
            }
            Assert.IsTrue(run.LeaveShop());
        }

        [Test]
        public void EventsComeAfterEveryThirdRound_AndTheShopWaitsForThem()
        {
            var run = NewRun();
            for (int round = 1; round <= 5; round++)
            {
                run.DebugForceRoundComplete();
                Assert.AreEqual(round % RunConfig.EventRoundInterval == 0, run.PendingEvent != null, "round " + round);
                if (run.PendingEvent != null)
                {
                    Assert.IsFalse(run.LeaveShop(), "the shop is locked until the event is resolved");
                    Assert.IsFalse(run.RerollShop());
                    Assert.IsTrue(run.ResolveEvent(run.PendingEvent.Options.Length - 1));
                    Assert.IsNull(run.PendingEvent);
                }
                Assert.IsTrue(run.LeaveShop());
            }
        }

        [Test]
        public void EveryEvent_IsWellFormed_AndItsLastOptionIsAFreeWayOut()
        {
            var ids = new HashSet<int>();
            foreach (var e in EventCatalog.All)
            {
                Assert.IsTrue(ids.Add(e.Id), "unique id " + e.Id);
                Assert.IsFalse(string.IsNullOrEmpty(e.Title + e.Story));
                Assert.GreaterOrEqual(e.Options.Length, 3, e.Title);
                Assert.LessOrEqual(e.Options.Length, 4, e.Title);
                foreach (var o in e.Options)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(o.Describe()), e.Title + "/" + o.Label);
                    foreach (var outcome in o.Outcomes) { Assert.Greater(outcome.Weight, 0, e.Title + "/" + o.Label); }
                }
                var walkOn = e.Options[e.Options.Length - 1];
                Assert.AreEqual(0, walkOn.RequiredPollen, e.Title + ": walking on is free");
                foreach (var outcome in walkOn.Outcomes)
                {
                    foreach (var effect in outcome.Effects)
                    {
                        Assert.IsTrue(effect.Kind == EventEffectKind.Pollen || effect.Kind == EventEffectKind.NextRoundSeeds || effect.Kind == EventEffectKind.NextRoundEnemyHp,
                            e.Title + ": a side effect of walking on stays small (Pollen, seeds or enemy HP)");
                    }
                }
            }
        }

        [Test]
        public void TheDescriptionStatesEveryChanceAndEffect()
        {
            var option = new EventOption("Test", 0,
                new EventOutcome(1, new EventEffect(EventEffectKind.LevelUpFertilizer)),
                new EventOutcome(3, new EventEffect(EventEffectKind.Pollen, -6)));
            Assert.AreEqual("25%: a random fertilizer gains a level   |   75%: lose 6 Pollen", option.Describe());
            Assert.AreEqual("Gain 14 Pollen, lose a random fertilizer", new EventOption("T", 0, new EventOutcome(1, new EventEffect(EventEffectKind.Pollen, 14), new EventEffect(EventEffectKind.LoseFertilizer))).Describe());
        }

        [Test]
        public void SellingOneOfYours_LosesAFertilizerAndGivesPollen()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            run.DebugGrantModifier(ModifierId.Macon);
            run.DebugSetLueur(10);
            run.DebugSetPendingEvent(1);
            int before = run.ActiveModifiers.Count;
            Assert.IsTrue(run.ResolveEvent(1));
            Assert.AreEqual(before - 1, run.ActiveModifiers.Count);
            Assert.AreEqual(24, run.Lueur);
        }

        [Test]
        public void AnOptionYouCannotPayForIsUnavailable_ButWalkingOnNever_IsNot()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            run.DebugSetLueur(3);
            run.DebugSetPendingEvent(1);
            Assert.IsFalse(run.IsEventOptionAvailable(0), "8 Pollen needed");
            Assert.IsFalse(run.ResolveEvent(0));
            Assert.IsNotNull(run.PendingEvent, "an unavailable choice resolves nothing");
            Assert.IsTrue(run.IsEventOptionAvailable(2));
        }

        [Test]
        public void NextRoundEffects_ApplyToThatRoundOnly()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            run.DebugSetPendingEvent(8);           // The Old Well, "Drink": +2 seeds (3 in 4) or -2 seeds
            int nextIndex = run.CurrentRoundIndex + 1;
            int baseBudget = run.Challenge.BudgetFor(nextIndex);
            Assert.IsTrue(run.ResolveEvent(1));
            run.LeaveShop();
            Assert.AreNotEqual(baseBudget, run.CurrentBudget, "the next round's seeds changed");
            Assert.AreEqual(run.CurrentBudget, run.PiecesRemainingThisRound);
            run.DebugForceRoundComplete();
            if (run.PendingEvent != null) { run.ResolveEvent(run.PendingEvent.Options.Length - 1); }
            run.LeaveShop();
            Assert.AreEqual(run.Challenge.BudgetFor(run.CurrentRoundIndex), run.CurrentBudget, "the round after is back to normal");
        }

        [Test]
        public void ThePendingEventAndItsEffectsSurviveSaveAndResume()
        {
            var run = NewRun();
            for (int i = 0; i < RunConfig.EventRoundInterval - 1; i++) { ClearRoundAndLeaveShop(run); }
            run.DebugForceRoundComplete();
            Assert.IsNotNull(run.PendingEvent, "the third cleared round opens on an event");
            var snapshot = RunSnapshot.FromText(run.CheckpointSnapshot.ToText());
            Assert.IsNotNull(snapshot);
            var resumed = RunManager.Restore(snapshot, new SystemRandomProvider(9));
            Assert.AreEqual(run.PendingEvent.Id, resumed.PendingEvent.Id, "back on the same event");
        }

        [Test]
        public void ARunNeverSeesTheSameEventTwiceBeforeSeeingThemAll()
        {
            var seen = new List<int>();
            var rng = new SystemRandomProvider(3);
            var picked = new HashSet<int>();
            for (int i = 0; i < EventCatalog.All.Length; i++)
            {
                var e = EventCatalog.Pick(rng, seen);
                Assert.IsTrue(picked.Add(e.Id), "no repeat before all are seen");
                seen.Add(e.Id);
            }
            Assert.AreEqual(EventCatalog.All.Length, picked.Count);
        }

        [Test]
        public void ThePercentagesAlwaysAddUpTo100()
        {
            var option = new EventOption("T", 0,
                new EventOutcome(1, new EventEffect(EventEffectKind.Pollen, 1)),
                new EventOutcome(1, new EventEffect(EventEffectKind.Pollen, 2)),
                new EventOutcome(1, new EventEffect(EventEffectKind.Pollen, 3)));
            Assert.AreEqual("34%: gain 1 Pollen   |   33%: gain 2 Pollen   |   33%: gain 3 Pollen", option.Describe());
        }

        [Test]
        public void AnOutcomeThatCannotHappen_IsNeitherShownNorDrawn()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            run.DebugSetLueur(50);
            for (int i = 0; run.ActiveModifiers.Count < EconomyConstants.MaxActiveModifiers; i++) { run.DebugGrantModifier(ModifierCatalog.All[i].Id); }
            foreach (var e in EventCatalog.All)
            {
                run.DebugSetPendingEvent(e.Id);
                for (int o = 0; o < e.Options.Length; o++)
                {
                    if (e.Options[o].Outcomes.Length < 2) { continue; }
                    foreach (var outcome in run.EventOptionOutcomes(o))
                    {
                        bool grants = false, loses = false;
                        foreach (var fx in outcome.Effects) { grants |= fx.Kind == EventEffectKind.GrantFertilizer; loses |= fx.Kind == EventEffectKind.LoseFertilizer; }
                        Assert.IsFalse(grants && !loses, e.Title + "/" + e.Options[o].Label + ": cannot gain a fertilizer with every slot full");
                    }
                }
            }
        }

        [Test]
        public void WithoutAnyFertilizer_NoOutcomeLevelsOneUpOrTakesOneAway()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            foreach (var e in EventCatalog.All)
            {
                run.DebugSetPendingEvent(e.Id);
                for (int o = 0; o < e.Options.Length; o++)
                {
                    foreach (var outcome in run.EventOptionOutcomes(o))
                    {
                        foreach (var fx in outcome.Effects)
                        {
                            Assert.AreNotEqual(EventEffectKind.LevelUpFertilizer, fx.Kind, e.Title + "/" + e.Options[o].Label);
                            Assert.AreNotEqual(EventEffectKind.LoseFertilizer, fx.Kind, e.Title + "/" + e.Options[o].Label);
                        }
                    }
                }
            }
        }

        [Test]
        public void ASwapNeverHandsBackTheFertilizerJustLost()
        {
            int swapEvent = -1, swapOption = -1;
            foreach (var e in EventCatalog.All)
            {
                for (int o = 0; o < e.Options.Length; o++)
                {
                    foreach (var outcome in e.Options[o].Outcomes)
                    {
                        bool grants = false, loses = false;
                        foreach (var fx in outcome.Effects) { grants |= fx.Kind == EventEffectKind.GrantFertilizer; loses |= fx.Kind == EventEffectKind.LoseFertilizer; }
                        if (grants && loses) { swapEvent = e.Id; swapOption = o; }
                    }
                }
            }
            Assert.GreaterOrEqual(swapEvent, 0, "a swap event exists");
            for (int seed = 1; seed <= 300; seed++)
            {
                var run = NewRun(seed);
                run.DebugForceRoundComplete();
                for (int i = 0; run.ActiveModifiers.Count < EconomyConstants.MaxActiveModifiers; i++) { run.DebugGrantModifier(ModifierCatalog.All[i].Id); }
                run.DebugSetPendingEvent(swapEvent);
                Assert.IsTrue(run.ResolveEvent(swapOption));
                string lost = null, gained = null;
                foreach (var line in run.LastEventResult)
                {
                    if (line.StartsWith("You lose ") && !line.Contains("Pollen") && !line.Contains("seed")) { lost = line.Substring(9).TrimEnd('.'); }
                    if (line.StartsWith("You gain ") && !line.Contains("Pollen")) { gained = line.Substring(9).TrimEnd('.'); }
                }
                if (lost != null && gained != null) { Assert.AreNotEqual(lost, gained, "seed " + seed); }
            }
        }

        [Test]
        public void LosingNothing_IsNotReportedAsLosingZero()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            run.DebugSetLueur(0);
            run.DebugSetPendingEvent(2);               // A Strange Bloom
            for (int o = 0; o < 3; o++)
            {
                var fresh = NewRun();
                fresh.DebugForceRoundComplete();
                fresh.DebugSetLueur(0);
                fresh.DebugSetPendingEvent(2);
                if (!fresh.IsEventOptionAvailable(o)) { continue; }
                fresh.ResolveEvent(o);
                foreach (var line in fresh.LastEventResult) { Assert.AreNotEqual("You lose 0 Pollen.", line); }
            }
        }

        [Test]
        public void ATilesPlusMult_CountsBeforeEveryFertilizer_WhateverTheirSlot()
        {
            foreach (bool tileFirst in new[] { true, false })
            {
                var placement = new PlacementResult();
                var tile = new ScoreEvent(ScoreEventType.MultBonus, new UnityEngine.Vector2Int(0, 0), 5);
                tile.TriggeringModifierIndex = -1;
                var fert = new ScoreEvent(ScoreEventType.ModifierMultiplier, new UnityEngine.Vector2Int(0, 0), 2);
                fert.TriggeringModifierIndex = 0;
                placement.ScoreEvents = tileFirst ? new List<ScoreEvent> { tile, fert } : new List<ScoreEvent> { fert, tile };
                Assert.AreEqual(12f, placement.Mult, 0.001f, "(1 + 5) x 2, emitted " + (tileFirst ? "tile first" : "fertilizer first"));
            }
        }

        [Test]
        public void LevelFour_AlsoTakesOneSeedOffTheShortFirstRound()
        {
            var normal = ChallengeCatalog.Classic;
            Assert.AreEqual(RunConfig.FirstRoundBudget, normal.BudgetFor(0));
            Assert.AreEqual(RunConfig.FirstRoundBudget - 1, normal.AtLevel(4).BudgetFor(0));
            Assert.AreEqual(normal.BudgetFor(1) - 1, normal.AtLevel(4).BudgetFor(1));
        }

        [Test]
        public void ASavedShopOfferingARemovedUpgrade_StillLoads()
        {
            var run = NewRun();
            run.DebugForceRoundComplete();
            string text = run.CheckpointSnapshot.ToText();
            var snapshot = RunSnapshot.FromText(text);
            Assert.IsNotNull(snapshot);
            snapshot.ShopWorkshop.Clear();
            snapshot.ShopWorkshop.Add(new RunSnapshot.ShopSlotData { IsModifier = false, Id = (int)UpgradeId.TintedCells });
            RunManager resumed = null;
            Assert.DoesNotThrow(() => resumed = RunManager.Restore(RunSnapshot.FromText(snapshot.ToText()), new SystemRandomProvider(2)));
            Assert.IsNotNull(resumed);
        }
    }
}
