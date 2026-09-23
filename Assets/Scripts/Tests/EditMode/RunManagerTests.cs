using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public class RunManagerTests
    {
        /// <summary>Group scoring is progressive (the Nth cell scored, 1-indexed, is worth N*GroupBonusPerCell — see GridManager.PlacePiece's group loop), so a full group of <paramref name="cellCount"/> cells earns the triangular number cellCount*(cellCount+1)/2 * GroupBonusPerCell, not a flat cellCount*GroupBonusPerCell.</summary>
        private static int ExpectedGroupBonus(int cellCount)
        {
            return cellCount * (cellCount + 1) / 2 * ScoringConstants.GroupBonusPerCell;
        }

        [Test]
        public void RunConfig_ArraysHaveEightEntries()
        {
            Assert.AreEqual(8, RunConfig.Quotas.Length);
            Assert.AreEqual(8, RunConfig.PieceBudgets.Length);
            Assert.AreEqual(7, RunConfig.BossRoundIndex);
        }

        [Test]
        public void InitialState_MatchesRoundOneConfig()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.AreEqual(1, run.CurrentRoundNumber);
            Assert.AreEqual(RunConfig.Quotas[0], run.CurrentQuota);
            Assert.AreEqual(RunConfig.PieceBudgets[0], run.CurrentBudget);
            Assert.AreEqual(RunConfig.PieceBudgets[0], run.PiecesRemainingThisRound);
            Assert.AreEqual(0, run.RoundScore);
            Assert.AreEqual(0, run.TotalScore);
            Assert.IsFalse(run.IsBossRound);
            Assert.AreEqual(RunState.InProgress, run.State);
            Assert.AreEqual(DeckManager.HandSize, run.Deck.Hand.Count);
        }

        [Test]
        public void DebugForceRoundComplete_SetsRoundScoreToQuota_AndAdvancesToAwaitingShop()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            var state = run.DebugForceRoundComplete();

            Assert.AreEqual(RunState.AwaitingShop, state);
            Assert.AreEqual(RunState.AwaitingShop, run.State);
            Assert.AreEqual(run.CurrentQuota, run.RoundScore);
        }

        [Test]
        public void DebugForceRoundComplete_NoOps_WhenNotInProgress()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugForceRoundComplete(); // now AwaitingShop
            int roundScoreBefore = run.RoundScore;

            var state = run.DebugForceRoundComplete();

            Assert.AreEqual(RunState.AwaitingShop, state);
            Assert.AreEqual(roundScoreBefore, run.RoundScore, "Calling it again while not InProgress should be a no-op");
        }

        [Test]
        public void PlacePiece_InvalidCoordinates_ReturnsFailureWithoutMutatingState()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            int piecesBefore = run.PiecesRemainingThisRound;
            int scoreBefore = run.RoundScore;

            var outcome = run.PlacePiece(0, -1, -1);

            Assert.IsFalse(outcome.Placement.Success);
            Assert.AreEqual(piecesBefore, run.PiecesRemainingThisRound);
            Assert.AreEqual(scoreBefore, run.RoundScore);
            Assert.AreEqual(RunState.InProgress, run.State);
        }

        [Test]
        public void PlacePiece_InvalidHandIndex_ReturnsFailure()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            var outcome = run.PlacePiece(99, 0, 0);

            Assert.IsFalse(outcome.Placement.Success);
        }

        [Test]
        public void ShopActions_Fail_WhenNotAwaitingShop()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            Assert.AreEqual(RunState.InProgress, run.State);

            Assert.IsFalse(run.BuyModifierSlot(0));
            Assert.IsFalse(run.BuyUpgradeSlot(0));
            Assert.IsFalse(run.RerollShop());
            Assert.IsFalse(run.LeaveShop());
            Assert.AreEqual(1, run.CurrentRoundNumber);
        }

        /// <summary>
        /// Greedy scan for the first free anchor (bottom row / smallest x first).
        /// Used only by tests that need to drive a full round to completion; not
        /// a general-purpose solver.
        /// </summary>
        private static Vector2Int? FindAnyValidAnchor(GridManager grid, PieceShape shape)
        {
            for (int y = 0; y < GridManager.Size; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    if (grid.CanPlace(shape, x, y))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            return null;
        }

        [Test]
        public void Round_EndsImmediately_WhenQuotaReached_RegardlessOfRemainingBudget()
        {
            var run = new RunManager(new SystemRandomProvider(42));

            // Every cell is golden so score accumulates fast regardless of shape,
            // color or adjacency luck — this test is about the round-end STATE
            // MACHINE (the quota ends the round the instant it's reached), not
            // about scoring itself (covered by GridManagerTests).
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            int budget = run.CurrentBudget;
            int piecesPlaced = 0;
            while (run.State == RunState.InProgress)
            {
                int slot = FirstOccupiedHandSlot(run);
                var token = run.Deck.Hand[slot].Value;
                var rotation = run.Deck.HandRotations[slot];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue, "Ran out of room on placement " + piecesPlaced);

                run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
                piecesPlaced++;

                Assert.Less(piecesPlaced, budget, "Should reach the quota well before exhausting the budget given every cell is golden");
            }

            Assert.AreEqual(RunState.AwaitingShop, run.State);
            Assert.GreaterOrEqual(run.RoundScore, run.CurrentQuota);
            Assert.Greater(run.PiecesRemainingThisRound, 0, "Round should end with budget still remaining once the quota is reached");

            Assert.AreEqual(EconomyConstants.ShopModifierSlotCount, run.ShopModifierSlots.Count);
            Assert.AreEqual(EconomyConstants.ShopUpgradeSlotCount, run.ShopUpgradeSlots.Count);
            Assert.AreEqual(1, run.CurrentRoundNumber, "Round shouldn't advance yet — the shop is still open");

            run.DebugGrantModifier(ModifierId.Prisme);
            bool left = run.LeaveShop();

            Assert.IsTrue(left);
            Assert.AreEqual(1, run.ActiveModifiers.Count);
            Assert.AreEqual(ModifierId.Prisme, run.ActiveModifiers[0]);
            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.AreEqual(0, run.RoundScore);
            Assert.AreEqual(RunConfig.PieceBudgets[1], run.PiecesRemainingThisRound);
            Assert.AreEqual(RunState.InProgress, run.State);
        }

        [Test]
        public void ActiveModifiers_StopsAcceptingMore_OnceAtTheCap()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            for (int i = 0; i < ModifierCatalog.All.Length && run.ActiveModifiers.Count < EconomyConstants.MaxActiveModifiers; i++)
            {
                bool granted = run.DebugGrantModifier(ModifierCatalog.All[i].Id);
                Assert.IsTrue(granted, "Granting should never require a removal step below the cap");
            }

            Assert.AreEqual(EconomyConstants.MaxActiveModifiers, run.ActiveModifiers.Count);

            // Any further modifier — including one not already held — is refused now that the cap is reached.
            var stillMissing = ModifierCatalog.All[EconomyConstants.MaxActiveModifiers].Id;
            CollectionAssert.DoesNotContain(run.ActiveModifiers, stillMissing);
            Assert.IsFalse(run.DebugGrantModifier(stillMissing));
            Assert.AreEqual(EconomyConstants.MaxActiveModifiers, run.ActiveModifiers.Count);
        }

        [Test]
        public void ShopModifierSlots_NeverOffersAModifierAlreadyActive()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            for (int i = 0; i < 5; i++)
            {
                PlayRoundToAwaitingShop(run);

                foreach (var slot in run.ShopModifierSlots)
                {
                    CollectionAssert.DoesNotContain(run.ActiveModifiers, slot.ModifierId,
                        "A modifier already held should never be offered again in the same run");
                }

                run.DebugGrantModifier(run.ShopModifierSlots[0].ModifierId);
                run.LeaveShop();
            }
        }

        [Test]
        public void BuyModifierSlot_Succeeds_DeductsLueurAndActivatesTheModifier()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            run.DebugSetLueur(0);
            var targetId = run.ShopModifierSlots[0].ModifierId;
            int price = run.GetModifierSlotPrice(0);
            run.DebugGrantLueur(price);

            bool bought = run.BuyModifierSlot(0);

            Assert.IsTrue(bought);
            Assert.AreEqual(0, run.Lueur);
            CollectionAssert.Contains(run.ActiveModifiers, targetId);
            Assert.IsTrue(run.ShopModifierSlots[0].Purchased);
        }

        [Test]
        public void BuyModifierSlot_Fails_WhenLueurIsInsufficient()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            run.DebugSetLueur(0);
            int price = run.GetModifierSlotPrice(0);
            run.DebugGrantLueur(price - 1);

            bool bought = run.BuyModifierSlot(0);

            Assert.IsFalse(bought);
            Assert.AreEqual(price - 1, run.Lueur);
            Assert.AreEqual(0, run.ActiveModifiers.Count);
        }

        [Test]
        public void BuyModifierSlot_Fails_OnceAlreadyPurchased()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            run.DebugGrantLueur(100000);
            Assert.IsTrue(run.BuyModifierSlot(0));

            bool boughtAgain = run.BuyModifierSlot(0);

            Assert.IsFalse(boughtAgain);
        }

        [Test]
        public void BuyModifierSlot_Fails_AtTheModifierCap()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            for (int i = 0; i < EconomyConstants.MaxActiveModifiers; i++)
            {
                run.DebugGrantModifier(ModifierCatalog.All[i].Id);
            }
            PlayRoundToAwaitingShop(run);
            run.DebugGrantLueur(100000);

            bool bought = run.BuyModifierSlot(0);

            Assert.IsFalse(bought);
            Assert.AreEqual(EconomyConstants.MaxActiveModifiers, run.ActiveModifiers.Count);
        }

        [Test]
        public void ModifierSlotPrice_EscalatesWithEachPurchaseThisVisit()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            int firstPrice = run.GetModifierSlotPrice(1);
            run.DebugGrantLueur(1000000);
            run.BuyModifierSlot(0);

            int secondPrice = run.GetModifierSlotPrice(1);

            Assert.Greater(secondPrice, firstPrice, "Every purchase this visit should raise the price of what's still on offer");
        }

        [Test]
        public void GetModifierSlotPrice_MatchesModifierPricing_BeforeAnyPurchaseThisVisit()
        {
            // No purchase yet this visit, so the usual escalation is a no-op
            // (see RunManager.ComputePrice) and the slot's price should be
            // exactly ModifierPricing's base price for whatever it's offering.
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            for (int i = 0; i < run.ShopModifierSlots.Count; i++)
            {
                int expected = ModifierPricing.GetPrice(run.ShopModifierSlots[i].ModifierId);
                Assert.AreEqual(expected, run.GetModifierSlotPrice(i), "Slot " + i);
            }
        }

        [Test]
        public void GetModifierSlotPrice_ReturnsZero_ForAnOutOfRangeIndex()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            Assert.AreEqual(0, run.GetModifierSlotPrice(-1));
            Assert.AreEqual(0, run.GetModifierSlotPrice(run.ShopModifierSlots.Count));
        }

        [Test]
        public void RerollShop_ReplacesEverySlot_IncludingAlreadyPurchasedOnes()
        {
            // Used to leave a purchased slot exactly as it was ("SOLD"
            // forever, for the rest of that shop visit) — changed on
            // explicit feedback that this read as reroll doing nothing:
            // "mes upgrades et modifiers que j'ai acheté sont encore
            // marqué sold, il faut que j'aie tout de disponible".
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            run.DebugGrantLueur(1000000);
            Assert.IsTrue(run.BuyModifierSlot(0));
            var purchasedId = run.ShopModifierSlots[0].ModifierId;

            bool rerolled = run.RerollShop();

            Assert.IsTrue(rerolled);
            Assert.IsFalse(run.ShopModifierSlots[0].Purchased, "A previously-sold slot should come back purchasable after a reroll");
            Assert.IsFalse(run.ShopModifierSlots[1].Purchased);
            // Buying it already permanently granted the modifier — rerolling
            // the SLOT later must not take that back.
            CollectionAssert.Contains(run.ActiveModifiers, purchasedId);
        }

        [Test]
        public void RerollShop_Fails_WhenLueurIsInsufficient()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            run.DebugSetLueur(0);

            bool rerolled = run.RerollShop();

            Assert.IsFalse(rerolled);
            Assert.AreEqual(0, run.Lueur);
        }

        [Test]
        public void LeaveShop_AdvancesToTheNextRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            bool left = run.LeaveShop();

            Assert.IsTrue(left);
            Assert.AreEqual(RunState.InProgress, run.State);
            Assert.AreEqual(2, run.CurrentRoundNumber);
        }

        [Test]
        public void PlacePiece_LueurCarriesOverAcrossRounds_LikeTotalScore()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugGrantLueur(42);
            PlayRoundToAwaitingShop(run);

            Assert.GreaterOrEqual(run.Lueur, 42, "Lueur earned mid-round should add on top of anything already held, never reset");

            run.LeaveShop();

            Assert.GreaterOrEqual(run.Lueur, 42, "Lueur should persist across the round boundary just like TotalScore, unlike RoundScore");
        }

        [Test]
        public void BuyUpgradeSlot_GridPool_SetsPendingUpgradeWithTileCandidates_ThenResolveApplies()
        {
            // Bounded seed search (same style as the stuck-board regression
            // test) for a shop whose slot 0 happens to roll a Grid-pool
            // upgrade — exercises the real roll instead of bypassing it.
            RunManager run = null;
            for (int seed = 0; seed < 500 && run == null; seed++)
            {
                var candidate = new RunManager(new SystemRandomProvider(seed));
                PlayRoundToAwaitingShop(candidate);
                if (candidate.ShopUpgradeSlots[0].Pool == UpgradePool.Grid)
                {
                    run = candidate;
                }
            }
            Assert.IsNotNull(run, "Should find a Grid-pool upgrade slot within 500 seeds");

            var hiddenUpgrade = run.ShopUpgradeSlots[0].HiddenUpgrade;
            run.DebugGrantLueur(1000000);

            bool bought = run.BuyUpgradeSlot(0);

            Assert.IsTrue(bought);
            Assert.IsTrue(run.ShopUpgradeSlots[0].Purchased);
            Assert.AreSame(hiddenUpgrade, run.PendingUpgrade);
            Assert.Greater(run.PendingUpgradeTileCandidates.Count, 0);
            // Nothing else in the shop can happen while a purchase is pending.
            Assert.IsFalse(run.BuyModifierSlot(1));
            Assert.IsFalse(run.RerollShop());
            Assert.IsFalse(run.LeaveShop());

            var chosen = new List<int> { run.PendingUpgradeTileCandidates[0] };
            bool resolved = run.ResolveUpgradeTileChoice(chosen);

            Assert.IsTrue(resolved);
            Assert.IsNull(run.PendingUpgrade);
            Assert.AreEqual(0, run.PendingUpgradeTileCandidates.Count);
            Assert.IsTrue(run.LeaveShop(), "The shop should be usable again once the pending upgrade is resolved");
        }

        [Test]
        public void BuyUpgradeSlot_BankPoolWithSubChoice_SetsPendingUpgrade_ThenResolveApplies()
        {
            // Narrowed to specifically Dupliquer (not Retirer, which can fail
            // at the deck floor, and not Recolorer, whose sub-choice needs a
            // real target color) so the resolve step below is unconditional.
            RunManager run = null;
            int foundSlot = -1;
            for (int seed = 0; seed < 500 && run == null; seed++)
            {
                var candidate = new RunManager(new SystemRandomProvider(seed));
                PlayRoundToAwaitingShop(candidate);
                for (int i = 0; i < candidate.ShopUpgradeSlots.Count; i++)
                {
                    if (candidate.ShopUpgradeSlots[i].HiddenUpgrade.Id == UpgradeId.DuplicatePiece)
                    {
                        run = candidate;
                        foundSlot = i;
                        break;
                    }
                }
            }
            Assert.IsNotNull(run, "Should find a Dupliquer upgrade slot within 500 seeds");

            var hiddenUpgrade = run.ShopUpgradeSlots[foundSlot].HiddenUpgrade;
            run.DebugGrantLueur(1000000);
            int deckCountBefore = run.Deck.DeckCount;

            bool bought = run.BuyUpgradeSlot(foundSlot);

            Assert.IsTrue(bought);
            Assert.AreSame(hiddenUpgrade, run.PendingUpgrade);
            Assert.AreEqual(0, run.PendingUpgradeTileCandidates.Count, "A Bank-pool upgrade never needs a tile choice");
            Assert.Greater(run.PendingUpgradeTypeCandidates.Count, 0, "A sub-choice Bank upgrade should offer at least one candidate type");
            Assert.LessOrEqual(run.PendingUpgradeTypeCandidates.Count, EconomyConstants.ShopTileCandidateCount, "The type picker should be capped, not list the whole deck composition");

            var firstType = run.PendingUpgradeTypeCandidates[0];
            var subChoice = new UpgradeSubChoice(firstType.Shape, firstType.Color);

            bool resolved = run.ResolveUpgradeSubChoice(subChoice);

            Assert.IsTrue(resolved);
            Assert.IsNull(run.PendingUpgrade);
            Assert.AreEqual(0, run.PendingUpgradeTypeCandidates.Count, "Candidates should be cleared once the sub-choice is resolved");
            Assert.AreEqual(deckCountBefore + 1, run.Deck.DeckCount);
        }

        [Test]
        public void BuyUpgradeSlot_Joker_AppliesImmediately_AndSurfacesTheShapeAdded()
        {
            RunManager run = null;
            int foundSlot = -1;
            for (int seed = 0; seed < 500 && run == null; seed++)
            {
                var candidate = new RunManager(new SystemRandomProvider(seed));
                PlayRoundToAwaitingShop(candidate);
                for (int i = 0; i < candidate.ShopUpgradeSlots.Count; i++)
                {
                    if (candidate.ShopUpgradeSlots[i].HiddenUpgrade.Id == UpgradeId.JokerPiece)
                    {
                        run = candidate;
                        foundSlot = i;
                        break;
                    }
                }
            }
            Assert.IsNotNull(run, "Should find a Joker upgrade slot within 500 seeds");

            run.DebugGrantLueur(1000000);
            int deckCountBefore = run.Deck.DeckCount;

            bool bought = run.BuyUpgradeSlot(foundSlot);

            Assert.IsTrue(bought);
            Assert.IsNull(run.PendingUpgrade, "Joker has no sub-choice, so it should apply immediately");
            Assert.AreEqual(deckCountBefore + 1, run.Deck.DeckCount);
            var addedToken = run.Deck.Deck[run.Deck.Deck.Count - 1];
            Assert.AreEqual(PieceColor.Joker, addedToken.Color);
            Assert.AreEqual(run.LastJokerShapeAdded, addedToken.Shape, "LastJokerShapeAdded should match the piece actually added, for UpgradeRevealView to preview");
        }

        [Test]
        public void BuyUpgradeSlot_ResolveWrongFollowUpKind_Fails()
        {
            RunManager run = null;
            for (int seed = 0; seed < 500 && run == null; seed++)
            {
                var candidate = new RunManager(new SystemRandomProvider(seed));
                PlayRoundToAwaitingShop(candidate);
                if (candidate.ShopUpgradeSlots[0].Pool == UpgradePool.Grid)
                {
                    run = candidate;
                }
            }
            Assert.IsNotNull(run, "Should find a Grid-pool upgrade slot within 500 seeds");
            run.DebugGrantLueur(1000000);
            run.BuyUpgradeSlot(0);

            // A Grid-pool pending upgrade needs a tile choice, not a sub-choice.
            bool resolved = run.ResolveUpgradeSubChoice(new UpgradeSubChoice(ShapeId.Single, PieceColor.Coral));

            Assert.IsFalse(resolved);
            Assert.IsNotNull(run.PendingUpgrade, "A failed resolve of the wrong kind should leave the pending upgrade untouched");
        }

        [Test]
        public void PlacePiece_DefersHandRefill_WhenTheEmptyingPlacementAlsoEndsTheRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            // Play 2 of the initial 3 hand slots (0 and 1) at whatever spots
            // are free, leaving exactly slot 2 occupied — slots no longer
            // shift down when an earlier one is played, so this leaves a
            // genuine hole at 0/1 rather than compacting slot 2 into 0.
            var tokenA = run.Deck.Hand[0].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[0]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            run.PlacePiece(0, anchorA.Value.x, anchorA.Value.y);

            var tokenB = run.Deck.Hand[1].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[1]);
            var anchorB = FindAnyValidAnchor(run.Grid, shapeB);
            Assert.IsTrue(anchorB.HasValue);
            run.PlacePiece(1, anchorB.Value.x, anchorB.Value.y);

            Assert.IsFalse(run.Deck.Hand[0].HasValue);
            Assert.IsFalse(run.Deck.Hand[1].HasValue);
            Assert.IsTrue(run.Deck.Hand[2].HasValue, "Slot 2 should still hold its original piece — playing 0 and 1 must not shift it down");
            Assert.AreEqual(RunState.InProgress, run.State);

            var lastToken = run.Deck.Hand[2].Value;
            var lastShape = PieceShapeCatalog.GetRotated(lastToken.Shape, run.Deck.HandRotations[2]);

            // Fill the whole board except a 3x3 pocket in the top-right
            // corner (far from where the first 2 pieces landed, near the
            // origin) with the last piece's own color, all golden — the last
            // piece's placement will merge into this huge group and score
            // well past round 1's quota (300) in one shot, guaranteeing this
            // single placement both empties the hand (its last piece) AND
            // ends the round.
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    bool inPocket = x >= 5 && y >= 5;
                    var cell = run.Grid.GetCell(x, y);
                    if (!inPocket && !cell.IsFilled)
                    {
                        cell.IsFilled = true;
                        cell.FilledColor = lastToken.Color;
                        cell.IsGolden = true;
                    }
                }
            }

            var lastAnchor = FindAnyValidAnchor(run.Grid, lastShape);
            Assert.IsTrue(lastAnchor.HasValue, "The 3x3 pocket should fit any single piece shape");

            var outcome = run.PlacePiece(2, lastAnchor.Value.x, lastAnchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(RunState.AwaitingShop, outcome.StateAfter);
            Assert.IsTrue(run.Deck.IsHandFullyEmpty(),
                "Hand should stay empty until the NEW round actually starts, not refill during this round's own ending placement");

            run.LeaveShop();

            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.IsTrue(AllHandSlotsFilled(run),
                "The deferred hand should only be drawn once the new round actually starts");
        }

        [Test]
        public void PlacePiece_DoesNotTriggerDefeat_WhenTheHandMerelyEmptiesMidRound()
        {
            // Regression test: EvaluateRoundEnd's stuck-board check used to
            // run against Deck.Hand as it stood right after PlayFromHand —
            // fine when refills were immediate, but once PlacePiece started
            // deferring the refill (see PlayFromHand_WithRefillIfEmptyFalse),
            // the 3rd placement of a batch left the hand at 0 pieces for that
            // one check, and HasAnyHandPlacement's empty-list loop trivially
            // returns false, so "no piece I don't have can be placed"
            // incorrectly read as "stuck" and ended the run in defeat.
            var run = new RunManager(new SystemRandomProvider(1));

            // Plays each of the 3 initial slots by its own index — they no
            // longer shift when an earlier one is played, so slot i always
            // still holds its original piece until this loop reaches it.
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                var token = run.Deck.Hand[i].Value;
                var rotation = run.Deck.HandRotations[i];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue);
                var outcome = run.PlacePiece(i, anchor.Value.x, anchor.Value.y);
                Assert.IsTrue(outcome.Placement.Success);
            }

            Assert.AreEqual(RunState.InProgress, run.State,
                "Emptying the hand mid-round should never by itself count as a stuck board");
            Assert.IsTrue(AllHandSlotsFilled(run),
                "The hand should refill immediately since the round is still in progress");
        }

        /// <summary>
        /// Places pieces from hand until the round's quota is reached. Marks
        /// every cell golden first so this converges quickly regardless of
        /// shape/color/adjacency luck — re-applied on every call (not just
        /// once by the caller) since GridManager.ResetForNewRound now clears
        /// modifier flags at the start of each round along with fill/lock
        /// state (a "Seeder"-left golden cell only lasting the rest of ITS
        /// round, not the whole run, meant every round needs its own reset).
        /// </summary>
        private static void PlayRoundToAwaitingShop(RunManager run)
        {
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            int guard = 0;
            while (run.State == RunState.InProgress)
            {
                int slot = FirstOccupiedHandSlot(run);
                var token = run.Deck.Hand[slot].Value;
                var rotation = run.Deck.HandRotations[slot];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue, "Ran out of room before reaching the quota");
                run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
                guard++;
                Assert.Less(guard, 100, "Round should reach its quota well within 100 placements given every cell is golden");
            }
            Assert.AreEqual(RunState.AwaitingShop, run.State);
        }


        [Test]
        public void PlacePiece_TriggersDefeat_WhenBoardBecomesFullyBlockedAfterThisPlacement()
        {
            var run = new RunManager(new SystemRandomProvider(7));
            var token = run.Deck.Hand[0].Value;
            var rotation = run.Deck.HandRotations[0];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            // Lock every cell except exactly the ones this piece will occupy at
            // (0,0) — using its actual DEALT rotation, since that's what
            // RunManager.PlacePiece will place. Note this placement will itself
            // complete (and clear) every row/column it touches, since the
            // footprint ends up being the only non-locked cells in each of them
            // — reopening a footprint-shaped hole right after the placement.
            // For seed 7 (verified by hand-tracing the deterministic draw, see
            // /tmp/dotnet_random_sim.py — re-run after DomV/TriIV were removed
            // from ShapeId, since that changed the deck and reshuffled this
            // exact draw), hand[0] is a straight tromino (TriIH, dealt rotated
            // 90° so its footprint is a 3-cell vertical line) and the two
            // pieces left in hand are a T-tetromino and a square (TTetro and
            // Sq2, both 4 cells — neither can ever fit a 3-cell hole, in any
            // rotation). That cell-count mismatch holds regardless of which
            // rotation each piece was actually dealt, so this test is robust
            // to random rotation. It's still tied to seed 7's exact hand
            // *composition* though — if InitialDeckFactory or the shuffle
            // ever changes, re-verify by hand or pick a new seed.
            var occupied = new HashSet<Vector2Int>();
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                occupied.Add(shape.Cells[i]);
            }
            foreach (var pos in GridManager.AllPositions())
            {
                if (!occupied.Contains(pos))
                {
                    run.Grid.GetCell(pos).IsLocked = true;
                }
            }

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(RunState.RunDefeat, run.State);
        }

        [Test]
        public void PlacePiece_DoesNotTriggerDefeat_WhenBoardStillHasRoom()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(RunState.InProgress, run.State);
        }

        [Test]
        public void PlacePiece_TriggersDefeat_WhenTheFreshHandDrawnAfterEmptyingItIsItselfUnplaceable()
        {
            // On explicit player report ("je ne peux pas jouer de tuile et
            // pourtant je n'ai pas perdu"): EvaluateRoundEnd deliberately
            // skips its stuck-check on a fully EMPTY hand (there's nothing
            // to evaluate yet) — but PlacePiece then immediately draws a
            // brand new 3-piece hand to replace it, and that redraw never
            // got checked at all. A board that had gone fully unplaceable
            // right as the hand ran out stayed InProgress forever, with the
            // player holding 3 pieces none of which could ever be placed.
            //
            // Picks whichever seed's starting hand has a non-tetromino
            // smallest piece — keeps the set of OTHER deck shapes that could
            // still fit inside the hole this piece's placement eventually
            // reopens (see below) small enough to fully strip from the deck
            // without hitting DeckManager.MinDeckSize. A fixed literal seed
            // would work just as well for the actual RNG implementation
            // this repo ships, but searching keeps the test correct even if
            // that implementation ever changes.
            RunManager run = null;
            int smallestSlot = -1;
            for (int seed = 1; seed <= 50; seed++)
            {
                var candidate = new RunManager(new SystemRandomProvider(seed));
                int bestCount = int.MaxValue;
                int bestSlot = -1;
                for (int i = 0; i < DeckManager.HandSize; i++)
                {
                    int count = PieceShapeCatalog.Get(candidate.Deck.Hand[i].Value.Shape).Cells.Count;
                    if (count < bestCount)
                    {
                        bestCount = count;
                        bestSlot = i;
                    }
                }
                if (bestCount < 4)
                {
                    run = candidate;
                    smallestSlot = bestSlot;
                    break;
                }
            }
            Assert.IsNotNull(run, "Could not find a seed whose starting hand isn't 3 tetrominoes within 50 tries");

            // Play every OTHER slot anywhere legal first, leaving only
            // smallestSlot occupied — so THIS test's placement is the one
            // that empties the hand and triggers the redraw.
            for (int slot = 0; slot < DeckManager.HandSize; slot++)
            {
                if (slot == smallestSlot)
                {
                    continue;
                }
                var token = run.Deck.Hand[slot].Value;
                var rotation = run.Deck.HandRotations[slot];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue);
                run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            }

            var lastToken = run.Deck.Hand[smallestSlot].Value;
            var lastRotation = run.Deck.HandRotations[smallestSlot];
            var lastShape = PieceShapeCatalog.GetRotated(lastToken.Shape, lastRotation);
            var lastAnchor = FindAnyValidAnchor(run.Grid, lastShape);
            Assert.IsTrue(lastAnchor.HasValue);

            var footprint = new HashSet<Vector2Int>();
            for (int i = 0; i < lastShape.Cells.Count; i++)
            {
                footprint.Add(lastAnchor.Value + lastShape.Cells[i]);
            }

            // Same trick as PlacePiece_TriggersDefeat_WhenBoardBecomesFullyBlockedAfterThisPlacement:
            // lock every OTHER cell (including ones slot0/1's placements
            // already filled — a locked cell is unusable regardless of fill
            // state, and it must also never complete/clear a DIFFERENT
            // line), so this placement's own footprint is the only thing
            // that can ever complete a line — reopening exactly (and only)
            // its own shape once cleared.
            foreach (var pos in GridManager.AllPositions())
            {
                if (!footprint.Contains(pos))
                {
                    run.Grid.GetCell(pos).IsLocked = true;
                }
            }

            // Strip every deck shape that could still fit inside that
            // reopened hole (any rotation, any offset) — otherwise the
            // random redraw might legitimately have somewhere to go, which
            // isn't the scenario under test. Verifies afterward that this
            // fully succeeded rather than silently stopping at MinDeckSize.
            var dangerousKeys = new List<(ShapeId Shape, PieceColor Color)>();
            foreach (var kvp in run.Deck.GetDeckComposition())
            {
                if (ShapeCanFitWithinCells(kvp.Key.Shape, footprint))
                {
                    dangerousKeys.Add(kvp.Key);
                }
            }
            foreach (var key in dangerousKeys)
            {
                while (run.Deck.RemoveOneOfType(key.Shape, key.Color)) { }
            }
            foreach (var kvp in run.Deck.GetDeckComposition())
            {
                Assert.IsFalse(ShapeCanFitWithinCells(kvp.Key.Shape, footprint),
                    kvp.Key.Shape + " still fits the reopened hole and couldn't be fully stripped without hitting DeckManager.MinDeckSize");
            }

            var outcome = run.PlacePiece(smallestSlot, lastAnchor.Value.x, lastAnchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.IsTrue(run.Deck.Hand[0].HasValue, "A fresh hand should have been drawn to replace the now-fully-empty one");
            Assert.AreEqual(RunState.RunDefeat, run.State,
                "The freshly-drawn hand has nowhere left to go on this fully locked board — this should be detected immediately, not left InProgress forever");
        }

        /// <summary>True if <paramref name="shapeId"/>, at any of its 4 rotations, has some translation where every one of its cells lands inside <paramref name="targetCells"/>.</summary>
        private static bool ShapeCanFitWithinCells(ShapeId shapeId, HashSet<Vector2Int> targetCells)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in targetCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            for (int r = 0; r < 4; r++)
            {
                var shape = PieceShapeCatalog.GetRotated(shapeId, (PieceRotation)r);
                for (int dx = minX; dx <= maxX; dx++)
                {
                    for (int dy = minY; dy <= maxY; dy++)
                    {
                        bool allFit = true;
                        for (int i = 0; i < shape.Cells.Count; i++)
                        {
                            var c = shape.Cells[i];
                            if (!targetCells.Contains(new Vector2Int(c.x + dx, c.y + dy)))
                            {
                                allFit = false;
                                break;
                            }
                        }
                        if (allFit)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        [Test]
        public void PlacePiece_AppliesAGoldenTraitedToken_AsAOneTimeCellBonus_ThenClearsTheCell()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.GoldenBonus, 0, "The token's own golden tile should have scored a golden bonus on this placement");

            var offset = shape.Cells[token.Trait.Value.LocalCellIndex];
            var landedCell = run.Grid.GetCell(anchor.Value.x + offset.x, anchor.Value.y + offset.y);
            Assert.IsFalse(landedCell.IsGolden, "Golden is a one-time enchantment on the token, not a permanent grid modifier — it should be cleared right after scoring");
            Assert.IsTrue(landedCell.OriginTrait.HasValue, "OriginTrait is a purely cosmetic marker and should survive even though IsGolden itself was cleared");
            Assert.AreEqual(PieceTraitKind.Golden, landedCell.OriginTrait.Value.Kind);
        }

        [Test]
        public void PlacePiece_StampsOriginTraitForEveryTraitKind_EvenOnesThatDontStampGoldenTintedOrMultiplier()
        {
            // Mirror/Catalyst/Twin/Detonator/Chameleon/Spark/Void never set
            // IsGolden/IsTinted/IsMultiplierZone (their effects are resolved
            // elsewhere), but OriginTrait should still be stamped uniformly
            // for all of them — it's meant to be the one reliable on-grid
            // trace of a trait pick regardless of kind.
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagCatalystTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            const int anchorX = 3;
            const int anchorY = 3;
            Assert.IsTrue(run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), anchorX, anchorY));

            var outcome = run.PlacePiece(slot, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            var landedCell = run.Grid.GetCell(anchorX, anchorY);
            Assert.IsFalse(landedCell.IsGolden);
            Assert.IsFalse(landedCell.IsTinted);
            Assert.IsFalse(landedCell.IsMultiplierZone);
            Assert.IsTrue(landedCell.OriginTrait.HasValue);
            Assert.AreEqual(PieceTraitKind.Catalyst, landedCell.OriginTrait.Value.Kind);
        }

        [Test]
        public void PlacePiece_TintedTrait_AlwaysMatchesTheTokensOwnColor_SoItAlwaysDoublesTheScore()
        {
            // On explicit player feedback ("les tinted tiles sont vraiment
            // chiantes, il se peut qu'elle serve a rien parfois"): the
            // target color used to be rolled independently of the token's
            // own (fixed) color, so a tinted tile usually could never
            // match. It's now always the token's own color, which never
            // changes on its own — so placing it should always double the
            // score, every single time, not just "sometimes".
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagTintedTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier, outcome.Placement.GroupMultiplier,
                "A tinted tile should always match its own piece's color and double the score now — it should never be a dead enchantment");
        }

        [Test]
        public void PlacePiece_BlastTrait_AlsoScoresFilledOrthogonalNeighborsAsGolden()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagBlastTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            const int anchorX = 3;
            const int anchorY = 3;
            Assert.IsTrue(run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), anchorX, anchorY));

            FillCell(run.Grid, anchorX - 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX + 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX, anchorY - 1, token.Color);
            FillCell(run.Grid, anchorX, anchorY + 1, token.Color);

            var outcome = run.PlacePiece(slot, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(5 * ScoringConstants.GoldenCellBonus, outcome.Placement.GoldenBonus,
                "The trait cell plus all 4 filled orthogonal neighbors should each score golden");
        }

        [Test]
        public void PlacePiece_BeaconTrait_StacksMultiplierWithFilledRowNeighbors()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagBeaconTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            const int anchorX = 3;
            const int anchorY = 3;
            Assert.IsTrue(run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), anchorX, anchorY));

            // Two same-color cells already filled in the trait cell's own
            // row, both orthogonally adjacent so they merge into one group
            // with it — Beacon marks the trait cell AND both of these as
            // multiplier zones, so the group's multiplier stacks x2 three
            // times (2^3 = 8) instead of the plain single-cell Multiplier
            // trait's flat x2.
            FillCell(run.Grid, anchorX - 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX + 1, anchorY, token.Color);

            var outcome = run.PlacePiece(slot, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            int expectedMultiplier = ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier;
            // GroupBonus itself stays the plain unmultiplied per-cell sum —
            // the x8 factor lives in GroupMultiplier and is applied once, at
            // the end, in TotalScore (see "apply the multiplier at the end",
            // explicit request).
            Assert.AreEqual(ExpectedGroupBonus(3), outcome.Placement.GroupBonus);
            Assert.AreEqual(expectedMultiplier, outcome.Placement.GroupMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(3) * expectedMultiplier, outcome.Placement.TotalScore);
        }

        [Test]
        public void PlacePiece_MirrorTrait_DuplicatesGroupBonusOntoTheOnlyOtherGroupCell()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagMirrorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;

            // Simplified mechanic (on explicit request — the old geometric
            // "symmetric partner" rule was too hard to reason about): Mirror
            // now duplicates onto a RANDOM other cell in the group. With
            // exactly one pre-existing neighbor, there's only one candidate,
            // so the target is deterministic regardless of the RNG draw.
            FillCell(run.Grid, 3, 3, token.Color);

            var outcome = run.PlacePiece(slot, 2, 3);

            Assert.IsTrue(outcome.Placement.Success);
            // Group scoring is progressive by scan order (see
            // ScoringConstants.GroupBonusPerCell), but the trait cell here
            // IS the piece's own only cell, which is always FindConnectedGroup's
            // flood-fill seed — always index 0, so its own share is always
            // exactly GroupBonusPerCell regardless of the group's total size.
            int perCellAmount = ScoringConstants.GroupBonusPerCell;
            Assert.AreEqual(perCellAmount, outcome.Placement.TraitBonus);

            bool foundMirrorEvent = false;
            foreach (var e in outcome.Placement.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Trait && e.Position == new Vector2Int(3, 3) && e.Amount == perCellAmount)
                {
                    foundMirrorEvent = true;
                }
            }
            Assert.IsTrue(foundMirrorEvent, "Mirror's duplicated bonus should appear as its own ScoreEvent at the other group cell");
        }

        [Test]
        public void PlacePiece_MirrorTrait_DoesNotFire_WhenTheGroupIsOnlyThisSingleCell()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagMirrorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_MirrorTrait_AlwaysPicksATargetFromWithinTheGroup()
        {
            var run = new RunManager(new SystemRandomProvider(3));
            run.Deck.TagMirrorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(4));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            FillCell(run.Grid, 3, 3, token.Color);
            FillCell(run.Grid, 4, 3, token.Color);
            FillCell(run.Grid, 5, 3, token.Color);

            var outcome = run.PlacePiece(slot, 2, 3);

            Assert.IsTrue(outcome.Placement.Success);
            var validTargets = new HashSet<Vector2Int> { new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3) };
            bool foundMirrorEvent = false;
            foreach (var e in outcome.Placement.ScoreEvents)
            {
                if (e.Type != ScoreEventType.Trait)
                {
                    continue;
                }
                Assert.IsTrue(validTargets.Contains(e.Position), "Mirror's target must be one of the other group cells, never outside the group");
                foundMirrorEvent = true;
            }
            Assert.IsTrue(foundMirrorEvent);
        }

        [Test]
        public void PlacePiece_SeederTrait_LeavesTheGridCellGoldenForTheRestOfTheRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSeederTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var traitPos = anchor.Value + shape.Cells[token.Trait.Value.LocalCellIndex];

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.IsTrue(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden,
                "Seeder's stamp should NOT be cleared after scoring — unlike every other trait, it stays golden for the rest of THIS round");
        }

        [Test]
        public void PlacePiece_SeederTrait_ClearsOnceTheRoundItWasSetInEnds()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSeederTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var traitPos = anchor.Value + shape.Cells[token.Trait.Value.LocalCellIndex];

            run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden);

            // Skip straight to the next round via the debug helper (same one
            // wired to the F9 editor shortcut) rather than grinding out real
            // placements — a permanent-for-the-whole-run golden cell was
            // judged too powerful, so it should be gone once round 2 starts.
            run.DebugForceRoundComplete();
            run.LeaveShop();

            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.IsFalse(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden,
                "Seeder's stamp should be cleared once the round it was set in ends");
            Assert.IsFalse(run.Grid.GetCell(traitPos.x, traitPos.y).OriginTrait.HasValue,
                "OriginTrait should reset at the round boundary just like Seeder's own IsGolden stamp (see Cell.ResetForNewRound)");
        }

        [Test]
        public void PlacePiece_CatalystTrait_ScoresPerPreExistingCellInTheGroup()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagCatalystTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            const int anchorX = 3;
            const int anchorY = 3;
            Assert.IsTrue(run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), anchorX, anchorY));

            // 2 pre-existing cells merge with the trait cell into a 3-cell group.
            FillCell(run.Grid, anchorX - 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX + 1, anchorY, token.Color);

            var outcome = run.PlacePiece(slot, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(2 * ScoringConstants.CatalystBonusPerExistingCell, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_CatalystTrait_DoesNotFire_WhenTheGroupIsOnlyThisPiece()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagCatalystTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_TwinTrait_DuplicatesGroupShareOntoEveryOtherCellInTheGroup()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagTwinTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            FillCell(run.Grid, 3, 3, token.Color);
            FillCell(run.Grid, 4, 3, token.Color);
            FillCell(run.Grid, 5, 3, token.Color);

            var outcome = run.PlacePiece(slot, 2, 3);

            Assert.IsTrue(outcome.Placement.Success);
            // Group is 4 cells (the trait cell + the 3 pre-filled ones). The
            // trait cell is the piece's own only cell, always
            // FindConnectedGroup's flood-fill seed (index 0), so its own
            // share is exactly GroupBonusPerCell regardless of group size —
            // Twin duplicates THAT specific share onto the OTHER 3 cells.
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_TwinTrait_DoesNotFire_WhenTheGroupIsOnlyThisSingleCell()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagTwinTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_DetonatorTrait_DoublesTheLineClearBonus()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagDetonatorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                FillCell(run.Grid, x, 0, token.Color);
            }

            var outcome = run.PlacePiece(slot, GridManager.Size - 1, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.LineClearScore, 0);
            Assert.AreEqual(outcome.Placement.LineClearScore, outcome.Placement.TraitBonus,
                "Detonator's extra copy of the line-clear bonus should exactly double it");
        }

        [Test]
        public void PlacePiece_DetonatorTrait_DoesNotFire_WhenNoLineClears()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagDetonatorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_ChameleonTrait_RecolorsWholePieceToMatchAFilledNeighbor()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagChameleonTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            var neighborColor = token.Color == PieceColor.Coral ? PieceColor.Teal : PieceColor.Coral;
            FillCell(run.Grid, 3, 4, neighborColor); // orthogonal ("up") neighbor of (3,3)

            var outcome = run.PlacePiece(slot, 3, 3);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(neighborColor, run.Grid.GetCell(3, 3).FilledColor);
        }

        [Test]
        public void PlacePiece_ChameleonTrait_PicksTheNeighborColorThatScoresTheMost()
        {
            // Explicit request: same treatment as Joker (see
            // GridManagerTests.PlacePiece_JokerJoinsWhicheverAdjacentGroupScoresTheMost)
            // — "il devrait être jumelé avec le groupe faisant le plus de
            // points" — instead of just the first neighbor color found in
            // the old fixed scan order (left, right, down, up).
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagChameleonTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            // Left ("first" in the old scan order) neighbor is a lone Coral
            // cell — joining it would only make a 2-cell group. The right
            // neighbor is part of a 3-cell Teal group — joining it makes a
            // 4-cell group, which scores more.
            FillCell(run.Grid, 2, 3, PieceColor.Coral);
            FillCell(run.Grid, 4, 3, PieceColor.Teal);
            FillCell(run.Grid, 5, 3, PieceColor.Teal);
            FillCell(run.Grid, 6, 3, PieceColor.Teal);

            var outcome = run.PlacePiece(slot, 3, 3);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(PieceColor.Teal, run.Grid.GetCell(3, 3).FilledColor);
            Assert.AreEqual(ExpectedGroupBonus(4), outcome.Placement.GroupBonus);
        }

        [Test]
        public void PlacePiece_ChameleonTrait_KeepsItsOwnColor_WhenNoNeighborIsFilled()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagChameleonTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(token.Color, run.Grid.GetCell(0, 0).FilledColor);
        }

        [Test]
        public void PlacePiece_ChameleonTrait_AlsoRecolorsItsOwnDeckEntry_KeepingItsTrait()
        {
            // Explicit request: "si une pièce est recolorée, elle est
            // recolorée dans le deck aussi (on garde l'upgrade sur la
            // pièce recolorée)" — so the next time this exact deck entry
            // is drawn with no filled neighbor to react to, it defaults to
            // the color it last actually took on instead of reverting to
            // whatever it started as.
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagChameleonTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            var neighborColor = token.Color == PieceColor.Coral ? PieceColor.Teal : PieceColor.Coral;
            FillCell(run.Grid, 3, 4, neighborColor); // orthogonal ("up") neighbor of (3,3)

            int originalColorCountBefore = CountChameleonSingles(run.Deck, token.Color);
            int neighborColorCountBefore = CountChameleonSingles(run.Deck, neighborColor);

            var outcome = run.PlacePiece(slot, 3, 3);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(neighborColor, run.Grid.GetCell(3, 3).FilledColor);

            // Exactly the one token that was actually placed and recolored
            // moved from the original-color bucket to the neighbor-color
            // bucket, trait intact — not a whole-type recolor like the
            // Bank-pool "Recolor a piece" upgrade.
            Assert.AreEqual(originalColorCountBefore - 1, CountChameleonSingles(run.Deck, token.Color),
                "The placed token's own deck entry should no longer show the old color");
            Assert.AreEqual(neighborColorCountBefore + 1, CountChameleonSingles(run.Deck, neighborColor),
                "The placed token's own deck entry should now show the neighbor's color, Chameleon trait intact");
        }

        private static int CountChameleonSingles(DeckManager deck, PieceColor color)
        {
            int count = 0;
            for (int i = 0; i < deck.DeckCount; i++)
            {
                var t = deck.Deck[i];
                if (t.Shape == ShapeId.Single && t.Color == color && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Chameleon)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void PlacePiece_SparkTrait_ScoresMoreTheLongerSinceTheLastClearThisRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSparkTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));

            // 2 ordinary placements first (neither clearing a line, using
            // whatever piece actually happens to occupy the first available
            // slot each time — tagging only updates the deck, not the
            // already-dealt hand), so the streak reaches 2 by the time the
            // Spark-tagged token lands.
            PlaceFirstAvailableHandPiece(run);
            PlaceFirstAvailableHandPiece(run);
            Assert.AreEqual(2, run.Grid.PlacementsSinceLastClear);

            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);
            var anchorC = FindAnyValidAnchor(run.Grid, PieceShapeCatalog.Get(ShapeId.Single));
            Assert.IsTrue(anchorC.HasValue);

            var outcome = run.PlacePiece(slot, anchorC.Value.x, anchorC.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(2 * ScoringConstants.SparkBonusPerPlacement, outcome.Placement.TraitBonus);
        }

        /// <summary>Plays whichever hand slot happens to be occupied first — used by tests that just need "any piece from hand," not a specific slot (slots no longer shift when played, so replaying slot 0 twice in a row would fail the 2nd time once it's empty).</summary>
        private static void PlaceFirstAvailableHandPiece(RunManager run)
        {
            int slot = FirstOccupiedHandSlot(run);
            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(outcome.Placement.Success);
        }

        [Test]
        public void PlacePiece_SparkTrait_DoesNotFire_OnTheFirstPlacementOfTheRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSparkTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, outcome.Placement.TraitBonus);
        }

        [Test]
        public void PlacePiece_VoidTrait_ClearsOneRandomFilledCellElsewhereOnTheGrid()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagVoidTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[slot].Value;
            // Only ONE eligible cell elsewhere on the grid, so Void's random
            // pick is deterministic regardless of the RNG seed.
            FillCell(run.Grid, 7, 7, token.Color);

            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.IsFalse(run.Grid.GetCell(7, 7).IsFilled, "Void should have cleared the only other filled cell on the grid");
        }

        [Test]
        public void PlacePiece_VoidTrait_NeverClearsThisPlacementsOwnCells()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagVoidTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            // Nothing else is filled on the grid, so Void has no eligible
            // target — this placement's own just-filled cell must survive.
            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.IsTrue(run.Grid.GetCell(0, 0).IsFilled);
        }

        [Test]
        public void PlacePiece_BastionTrait_LocksInPlaceAndSurvivesALaterLineClear()
        {
            // "Bastion Tile" (on explicit request — "Locked cell upgraded.
            // N'est pas cleared mais fait quand même les points cleared").
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagBastionTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int bastionSlot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var placeBastion = run.PlacePiece(bastionSlot, 3, 0);
            Assert.IsTrue(placeBastion.Placement.Success);

            var bastionCell = run.Grid.GetCell(3, 0);
            Assert.IsTrue(bastionCell.IsLocked, "Bastion cell should lock in place once placed");
            Assert.IsTrue(bastionCell.IsFilled);

            // Fill the rest of row 0 directly (not through the trait system) —
            // nothing re-checks for a completed line until the next real
            // placement anywhere on the board.
            for (int x = 0; x < GridManager.Size; x++)
            {
                if (x == 3) continue;
                FillCell(run.Grid, x, 0, PieceColor.Teal);
            }

            int otherSlot = FirstOccupiedHandSlot(run);
            var token = run.Deck.Hand[otherSlot].Value;
            var rotation = run.Deck.HandRotations[otherSlot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var outcome = run.PlacePiece(otherSlot, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.LineClearScore, 0, "Row 0 should now be recognized as complete");
            CollectionAssert.DoesNotContain(outcome.Placement.ClearedCells, new Vector2Int(3, 0));
            Assert.IsTrue(run.Grid.GetCell(3, 0).IsFilled, "Bastion cell should still be there after the clear");
            Assert.IsTrue(run.Grid.GetCell(3, 0).IsLocked);
        }

        [Test]
        public void PlacePiece_KamikazeTrait_DestroysItsEightSurroundingTilesAndScoresPerTileDestroyed()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagKamikazeTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            FillCell(run.Grid, 3, 3, PieceColor.Teal);
            FillCell(run.Grid, 4, 3, PieceColor.Teal);
            FillCell(run.Grid, 5, 3, PieceColor.Teal);
            FillCell(run.Grid, 3, 4, PieceColor.Teal);
            FillCell(run.Grid, 5, 4, PieceColor.Teal);
            FillCell(run.Grid, 3, 5, PieceColor.Teal);
            FillCell(run.Grid, 4, 5, PieceColor.Teal);
            FillCell(run.Grid, 5, 5, PieceColor.Teal);

            var outcome = run.PlacePiece(slot, 4, 4);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(8 * ScoringConstants.KamikazeBonusPerDestroyedCell, outcome.Placement.TraitBonus);
            Assert.IsFalse(run.Grid.GetCell(3, 3).IsFilled);
            Assert.IsFalse(run.Grid.GetCell(5, 5).IsFilled);
            Assert.IsTrue(run.Grid.GetCell(4, 4).IsFilled, "The Kamikaze tile itself should survive");
        }

        [Test]
        public void PlacePiece_KamikazeTrait_NeverDestroysThisPlacementsOwnCells()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagKamikazeTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            // A 2-cell piece so its OTHER cell sits right inside whichever
            // local cell got enchanted's own 8-neighbor blast radius —
            // survives regardless of which of the two cells got tagged.
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.DomH);

            // Every hand token draws a RANDOM initial rotation (see
            // DeckManager.RandomRotation) — this used to hardcode the
            // piece's landing cells as (4,4)/(5,4) as if it always placed
            // unrotated, which only held for 2 of DomH's 4 possible
            // rotations (the other 2 land it VERTICAL instead), making
            // this test flaky depending on the random hand state. Only
            // surfaced once CI actually ran it for the first time — same
            // "compute the real rotated shape" pattern as the other
            // ChurnUntilHandMatches tests in this file.
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(ShapeId.DomH, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            foreach (var offset in shape.Cells)
            {
                Assert.IsTrue(run.Grid.GetCell(anchor.Value.x + offset.x, anchor.Value.y + offset.y).IsFilled);
            }
        }

        [Test]
        public void PlacePiece_DuringBossRound_LocksTwoMoreFreeCellsEveryThreePiecesPlayed()
        {
            // Boss round rework (on explicit request — "le boss est beaucoup
            // trop difficile, on va faire autre chose"): no more upfront
            // lock, instead RunConfig.BossLockCellsPerInterval more empty
            // cells lock every RunConfig.BossLockPiecesInterval pieces played.
            var run = new RunManager(new SystemRandomProvider(5));
            AdvanceToRound(run, RunConfig.BossRoundIndex);
            Assert.IsTrue(run.IsBossRound);
            Assert.AreEqual(0, CountLockedCells(run.Grid), "Boss round should no longer lock cells upfront");

            PlaceFirstAvailableHandPiece(run);
            PlaceFirstAvailableHandPiece(run);
            Assert.AreEqual(0, CountLockedCells(run.Grid), "No lock tick yet after only 2 pieces");

            PlaceFirstAvailableHandPiece(run);
            Assert.AreEqual(RunConfig.BossLockCellsPerInterval, CountLockedCells(run.Grid));
        }

        [Test]
        public void PlacePiece_ChaosChallenge_LocksCellsFromRoundOne_AtItsOwnGentlerPace()
        {
            // Chaos (see ChallengeCatalog.Chaos): the boss cell-lock is
            // active every round instead of only the last one, at its own
            // (gentler) pace — 1 cell every 5 pieces here, not Classic's 2
            // every 3, so this must NOT reuse RunConfig's boss numbers.
            var run = new RunManager(new SystemRandomProvider(5), ChallengeCatalog.Chaos);
            Assert.IsTrue(run.IsBossRound, "Chaos' boss should already be active on round 1");
            Assert.AreEqual(0, CountLockedCells(run.Grid));

            for (int i = 0; i < 4; i++)
            {
                PlaceFirstAvailableHandPiece(run);
            }
            Assert.AreEqual(0, CountLockedCells(run.Grid), "No lock tick yet after only 4 of Chaos' 5-piece interval");

            PlaceFirstAvailableHandPiece(run);
            Assert.AreEqual(ChallengeCatalog.Chaos.BossLockCellsPerInterval, CountLockedCells(run.Grid));
        }

        [Test]
        public void RunManager_ClassicChallenge_NeverActivatesTheBossBeforeTheLastRound()
        {
            var run = new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Classic);

            Assert.IsFalse(run.IsBossRound);
        }

        [Test]
        public void RunManager_MarathonChallenge_StartsWithItsOwnSmallerDeckAndQuotas()
        {
            var run = new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Marathon);

            Assert.AreEqual(13, run.Deck.DeckCount, "Marathon's starting deck should be the smaller 13-token one, not the standard 32.");
            Assert.AreEqual(ChallengeCatalog.Marathon.Quotas[0], run.CurrentQuota);
            Assert.AreEqual(ChallengeCatalog.Marathon.PieceBudgets[0], run.CurrentBudget);
        }

        /// <summary>Drives a fresh run straight to the start of <paramref name="targetRoundIndex"/> via DebugForceRoundComplete, without needing to actually reach each round's real quota — the upgrade/modifier picked each round don't matter for these tests, only reaching the round does.</summary>
        private static void AdvanceToRound(RunManager run, int targetRoundIndex)
        {
            while (run.CurrentRoundIndex < targetRoundIndex)
            {
                run.DebugForceRoundComplete();
                Assert.AreEqual(RunState.AwaitingShop, run.State);
                run.LeaveShop();
            }
        }

        private static int CountLockedCells(GridManager grid)
        {
            int count = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (grid.GetCell(pos).IsLocked)
                {
                    count++;
                }
            }
            return count;
        }

        private static void FillCell(GridManager grid, int x, int y, PieceColor color)
        {
            var cell = grid.GetCell(x, y);
            cell.IsFilled = true;
            cell.FilledColor = color;
        }

        /// <summary>
        /// Tagging a deck token only updates the deck (the source of truth),
        /// not the hand/draw-pile copies already dealt at construction time,
        /// so this churns hand draws (without going through RunManager, which
        /// would touch the grid) — cycling through slots 0, 1, 2 in turn so
        /// every 3rd play triggers DeckManager's full-hand refill — until
        /// some slot matches <paramref name="predicate"/>. Returns that
        /// slot's index: never assume it's 0, slots no longer shift down
        /// when an earlier one is played (on explicit request).
        /// </summary>
        private static int ChurnUntilHandMatches(RunManager run, System.Func<PieceToken, bool> predicate)
        {
            int guard = 0;
            int nextSlotToPlay = 0;
            while (true)
            {
                int match = FirstMatchingHandSlot(run, predicate);
                if (match >= 0)
                {
                    return match;
                }
                run.Deck.PlayFromHand(nextSlotToPlay);
                nextSlotToPlay = (nextSlotToPlay + 1) % DeckManager.HandSize;
                guard++;
                Assert.Less(guard, 300, "A matching token should reach the hand well within a few reshuffle cycles");
            }
        }

        private static int FirstMatchingHandSlot(RunManager run, System.Func<PieceToken, bool> predicate)
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                var slot = run.Deck.Hand[i];
                if (slot.HasValue && predicate(slot.Value))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The index of the first occupied hand slot — used by tests that just need "any piece from hand," not a specific slot.</summary>
        private static int FirstOccupiedHandSlot(RunManager run)
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (run.Deck.Hand[i].HasValue)
                {
                    return i;
                }
            }
            Assert.Fail("Hand should have at least one occupied slot while the round is in progress");
            return -1;
        }

        private static bool AllHandSlotsFilled(RunManager run)
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (!run.Deck.Hand[i].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        // ---- Fourth batch: SlotUn/Deux/Trois (see GridManagerModifierTests
        // for GrandFormat/HorsNorme/Éclat*, which don't need handIndex and are
        // covered there directly against GridManager.PlacePiece) ----

        /// <summary>Plays a full round (quota met via all-golden cells, same trick as PlayRoundToAwaitingShop), then grants exactly <paramref name="id"/> via the DebugGrantModifier test-only bypass (skips the shop's random slot rolls and Lueur cost entirely) and closes the shop. Also forces a fresh full 3-card hand afterward, since the round-ending placement can leave a partial hand carried into the next round.</summary>
        private static void GiveActiveModifier(RunManager run, ModifierId id)
        {
            PlayRoundToAwaitingShop(run);
            run.DebugGrantModifier(id);
            run.LeaveShop();
            Assert.AreEqual(RunState.InProgress, run.State);
            CollectionAssert.Contains(run.ActiveModifiers, id);
            run.Deck.DrawNewHand();
        }

        [Test]
        public void SlotUn_MultipliesEntireScore_WhenPlacingFromHandSlotZero()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.SlotUn);

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(ScoringConstants.SlotLoyaltyMultiplier, outcome.Placement.ModifierMultiplier);
        }

        [Test]
        public void SlotModifiers_EachOnlyFiresForItsOwnHandIndex()
        {
            AssertSlotFiresOnlyForHandIndex(ModifierId.SlotUn, 0);
            AssertSlotFiresOnlyForHandIndex(ModifierId.SlotDeux, 1);
            AssertSlotFiresOnlyForHandIndex(ModifierId.SlotTrois, 2);
        }

        private static void AssertSlotFiresOnlyForHandIndex(ModifierId id, int matchingHandIndex)
        {
            for (int handIndex = 0; handIndex < DeckManager.HandSize; handIndex++)
            {
                var run = new RunManager(new SystemRandomProvider(1));
                GiveActiveModifier(run, id);

                var outcome = run.PlacePiece(handIndex, 0, 0);

                Assert.IsTrue(outcome.Placement.Success, id + " vs hand index " + handIndex);
                int expected = handIndex == matchingHandIndex ? ScoringConstants.SlotLoyaltyMultiplier : 1;
                Assert.AreEqual(expected, outcome.Placement.ModifierMultiplier, id + " vs hand index " + handIndex);
            }
        }

        [Test]
        public void GetModifierUsageCount_IncrementsEachTimeTheModifierScores()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.SlotUn);
            Assert.AreEqual(0, run.GetModifierUsageCount(ModifierId.SlotUn));

            var first = run.PlacePiece(0, 0, 0);
            Assert.IsTrue(first.Placement.Success);
            Assert.AreEqual(1, run.GetModifierUsageCount(ModifierId.SlotUn));

            run.Deck.DrawNewHand();
            // Far from (0,0) so the first placement's shape can never overlap
            // this one, whatever shape/rotation each hand draw happens to be.
            var second = run.PlacePiece(0, 5, 5);
            Assert.IsTrue(second.Placement.Success);
            Assert.AreEqual(2, run.GetModifierUsageCount(ModifierId.SlotUn));
        }

        [Test]
        public void GetModifierUsageCount_StaysZero_ForAModifierThatNeverFires()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.SlotUn);

            // Placing from hand slot 1 never triggers SlotUn (matches slot 0 only).
            var outcome = run.PlacePiece(1, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0, run.GetModifierUsageCount(ModifierId.SlotUn));
        }

        [Test]
        public void GetModifierUsageCount_AlsoIncrements_ForAModifierMultiplierEvent()
        {
            // Regression test: CountModifierUsage used to only tally
            // ScoreEventType.Modifier events, so a modifier converted from a
            // flat bonus to a multiplier (ScoreEventType.ModifierMultiplier —
            // see PlacementResult.ModifierMultiplier) would silently stop
            // ever incrementing this stat, even though it still fires every
            // placement it qualifies for.
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.Architecte);

            int slot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Sq2);
            var outcome = run.PlacePiece(slot, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.ModifierMultiplier, 1);
            Assert.AreEqual(1, run.GetModifierUsageCount(ModifierId.Architecte));
        }

        [Test]
        public void RepetitionLueur_CreditsTheRunsLueurAndCountsAsUsed()
        {
            // Same "does the generic wiring pick up a new event type" risk
            // as the ModifierMultiplier regression test above, here for
            // RunManager.Lueur (see PlacementResult.ModifierLueurBonus —
            // the first of the 5 Lueur-earning modifiers, spec extension)
            // and GetModifierUsageCount (ScoreEventType.LueurBonus).
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.RepetitionLueur);
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            int firstSlot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var firstAnchor = FindAnyValidAnchor(run.Grid, single);
            Assert.IsTrue(firstAnchor.HasValue);
            var first = run.PlacePiece(firstSlot, firstAnchor.Value.x, firstAnchor.Value.y);
            Assert.IsTrue(first.Placement.Success);
            Assert.AreEqual(0, first.Placement.ModifierLueurBonus, "Round's first placement starts no streak yet");
            int lueurAfterFirst = run.Lueur;

            int secondSlot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var secondAnchor = FindAnyValidAnchor(run.Grid, single);
            Assert.IsTrue(secondAnchor.HasValue);
            var second = run.PlacePiece(secondSlot, secondAnchor.Value.x, secondAnchor.Value.y);

            Assert.IsTrue(second.Placement.Success);
            Assert.AreEqual(EconomyConstants.RepetitionLueurBonus, second.Placement.ModifierLueurBonus, "2nd consecutive Single in a row");
            Assert.AreEqual(lueurAfterFirst + second.Placement.LueurEarned + EconomyConstants.RepetitionLueurBonus, run.Lueur, "RunManager.Lueur adds both LueurEarned and ModifierLueurBonus");
            Assert.AreEqual(1, run.GetModifierUsageCount(ModifierId.RepetitionLueur));
        }

        // ---- Ninth batch: Copieur (purchase-time mimic), MultCinqRisque
        // (round-end risk/reward), CartesEnchantees/Multitude (deck-state
        // dependent bonuses via RunManager.ApplyDeckStateModifierBonuses) —
        // see ModifierId's ninth batch. Devotion/Forme* conversion to a
        // genuine xN ModifierMultiplier and the 3 flat MultUn/Deux/Quatre +
        // 10 Forme*Points modifiers are covered in GridManagerModifierTests
        // instead, since GridManager.PlacePiece already exercises them
        // directly without needing a whole RunManager/shop around them.

        /// <summary>Always returns 0 — every roll it feeds picks index 0 of whatever range it's asked for (a valid choice for any maxExclusive &gt;= 1), so a "1-in-N" chance rolled via <c>rng.Next(N) == 0</c> always hits. Used to force MultCinqRisque's round-end loss roll deterministically.</summary>
        private sealed class AlwaysZeroRandomProvider : IRandomProvider
        {
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }

        /// <summary>The opposite of <see cref="AlwaysZeroRandomProvider"/> — returns 1 whenever the range allows it (falling back to the only valid choice, 0, when maxExclusive is 1), so a "1-in-N" chance rolled via <c>rng.Next(N) == 0</c> (N &gt;= 2) never hits.</summary>
        private sealed class NeverZeroRandomProvider : IRandomProvider
        {
            public int Next(int maxExclusive)
            {
                return maxExclusive > 1 ? 1 : 0;
            }
        }

        /// <summary>Finds <paramref name="target"/> among the open shop's modifier slots and buys it, rerolling (funded by a large Lueur grant) until it shows up — modifier slots are randomly rolled, so this is the only way to buy a SPECIFIC modifier without relying on <see cref="RunManager.DebugGrantModifier"/>, which can't exercise purchase-time behavior like Copieur's. The shop must already be open (see PlayRoundToAwaitingShop).</summary>
        private static void BuyModifierByIdViaShop(RunManager run, ModifierId target)
        {
            run.DebugGrantLueur(1000000);
            int guard = 0;
            while (true)
            {
                for (int i = 0; i < run.ShopModifierSlots.Count; i++)
                {
                    if (run.ShopModifierSlots[i].ModifierId == target && !run.ShopModifierSlots[i].Purchased)
                    {
                        Assert.IsTrue(run.BuyModifierSlot(i), "Buying " + target + " should succeed with ample Lueur granted");
                        return;
                    }
                }
                Assert.IsTrue(run.RerollShop(), "Reroll should always succeed with ample Lueur granted");
                guard++;
                Assert.Less(guard, 2000, target + " never appeared in the shop after many rerolls");
            }
        }

        private static int CountOccurrences(IReadOnlyList<ModifierId> modifiers, ModifierId id)
        {
            int count = 0;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == id)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void Copieur_AddsAnotherCopyOfThePreviouslyPurchasedModifier()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            BuyModifierByIdViaShop(run, ModifierId.MultUn);
            Assert.AreEqual(1, CountOccurrences(run.ActiveModifiers, ModifierId.MultUn));

            BuyModifierByIdViaShop(run, ModifierId.Copieur);

            Assert.AreEqual(2, CountOccurrences(run.ActiveModifiers, ModifierId.MultUn), "Copieur should add another copy of MultUn, the modifier bought immediately before it");
            CollectionAssert.DoesNotContain(run.ActiveModifiers, ModifierId.Copieur, "Copieur itself never becomes an active modifier");
        }

        [Test]
        public void Copieur_IsANoOp_WhenNothingHasBeenPurchasedYetThisRun()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            BuyModifierByIdViaShop(run, ModifierId.Copieur);

            Assert.AreEqual(0, run.ActiveModifiers.Count, "Buying Copieur first, before anything else, has nothing to copy yet");
        }

        [Test]
        public void Copieur_ChainedPurchases_AllCopyTheSameUnderlyingModifier_NotEachOther()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);

            BuyModifierByIdViaShop(run, ModifierId.MultDeux);
            BuyModifierByIdViaShop(run, ModifierId.Copieur);
            BuyModifierByIdViaShop(run, ModifierId.Copieur);

            Assert.AreEqual(3, CountOccurrences(run.ActiveModifiers, ModifierId.MultDeux), "Both Copieur purchases should copy MultDeux — _lastPurchasedModifierId only ever updates on a real purchase, never on Copieur itself");
        }

        [Test]
        public void MultCinqRisque_CanBeLost_AtRoundEnd_WhenTheLossRollHits()
        {
            var run = new RunManager(new AlwaysZeroRandomProvider());
            GiveActiveModifier(run, ModifierId.MultCinqRisque);

            PlayRoundToAwaitingShop(run);

            CollectionAssert.DoesNotContain(run.ActiveModifiers, ModifierId.MultCinqRisque, "A guaranteed-hit 1-in-5 roll should remove it at round end");
        }

        [Test]
        public void MultCinqRisque_Survives_AtRoundEnd_WhenTheLossRollMisses()
        {
            var run = new RunManager(new NeverZeroRandomProvider());
            GiveActiveModifier(run, ModifierId.MultCinqRisque);

            PlayRoundToAwaitingShop(run);

            CollectionAssert.Contains(run.ActiveModifiers, ModifierId.MultCinqRisque, "A guaranteed-miss roll should leave it held");
        }

        [Test]
        public void MultCinqRisque_GivesFlatAdditiveMultBonus_LikeMultUn()
        {
            var run = new RunManager(new NeverZeroRandomProvider());
            GiveActiveModifier(run, ModifierId.MultCinqRisque);

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(ScoringConstants.MultCinqRisqueBonus, outcome.Placement.AdditiveMultBonus);
        }

        [Test]
        public void CartesEnchantees_GivesTheTrueFractionalMult_NotFlooredToZero_BelowTenUpgradedCards()
        {
            // On explicit request: "on doit multiplier comme si c'était un
            // float au lieu d'arrondir a la baisse" — the OLD integer-step
            // behavior floored this to 0 (no bonus at all); the true value
            // is 0.9, and it now applies in full via ProgressiveAdditiveMult.
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.CartesEnchantees);
            run.Deck.TagGoldenTokensRandom(8, new SystemRandomProvider(2)); // 1 (baseline) + 8 = 9 -> 9/10 = 0.9

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0f, outcome.Placement.AdditiveMultBonus, "The plain integer pool stays untouched by CartesEnchantees now");
            Assert.AreEqual(0.9f, outcome.Placement.ProgressiveAdditiveMult, 0.0001f);
        }

        [Test]
        public void CartesEnchantees_GivesOneMult_AtTenUpgradedCards_CountingFromABaselineOfOne()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.CartesEnchantees);
            run.Deck.TagGoldenTokensRandom(9, new SystemRandomProvider(2)); // 1 (baseline) + 9 = 10 -> 10/10 = 1.0

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(1f, outcome.Placement.ProgressiveAdditiveMult, 0.0001f);
        }

        [Test]
        public void CartesEnchantees_GivesTwoMult_AtTwentyUpgradedCards()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.CartesEnchantees);
            run.Deck.TagGoldenTokensRandom(19, new SystemRandomProvider(2)); // 1 (baseline) + 19 = 20 -> 20/10 = 2.0

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(2f, outcome.Placement.ProgressiveAdditiveMult, 0.0001f);
        }

        [Test]
        public void CartesEnchantees_ScoreEventCarriesThePreciseFractionalAmount_NotJustTheRoundedInt()
        {
            // On explicit report: "le popup de score qui apparait est un
            // int et non un float donc au lieu de voir +1.3 je vois +1
            // malgré le fait que le mult est bien augmenté de 1.3" —
            // Amount stays a rounded int (chip/usage bookkeeping still
            // wants a whole number), but PreciseAmount now carries the
            // exact value for the popup to show instead.
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.CartesEnchantees);
            run.Deck.TagGoldenTokensRandom(11, new SystemRandomProvider(2)); // 1 (baseline) + 11 = 12 -> 12/10 = 1.2

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            ScoreEvent multEvent = null;
            foreach (var scoreEvent in outcome.Placement.ScoreEvents)
            {
                if (scoreEvent.Type == ScoreEventType.MultBonus && scoreEvent.TriggeringModifier == ModifierId.CartesEnchantees)
                {
                    multEvent = scoreEvent;
                    break;
                }
            }
            Assert.IsNotNull(multEvent, "CartesEnchantees should always fire a MultBonus event, even below the old int-rounding threshold");
            Assert.AreEqual(1, multEvent.Amount, "Amount stays a rounded int for chip/usage bookkeeping");
            Assert.AreEqual(1.2f, multEvent.PreciseAmount.Value, 0.0001f);
        }

        [Test]
        public void Multitude_GivesFlatPointsEqualToDeckSize()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.Multitude);

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(run.Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard, outcome.Placement.ModifierBonus);
        }

        [Test]
        public void Multitude_StacksWhenHeldTwice_ViaCopieur()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            PlayRoundToAwaitingShop(run);
            BuyModifierByIdViaShop(run, ModifierId.Multitude);
            BuyModifierByIdViaShop(run, ModifierId.Copieur);
            run.LeaveShop();
            run.Deck.DrawNewHand();

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(2 * run.Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard, outcome.Placement.ModifierBonus, "Holding Multitude twice (via Copieur) should stack additively");
        }

        // ---- Tenth batch: Experience (mult scaling with special pieces
        // PLAYED this run, CartesEnchantees' played-count counterpart) and
        // RunManager.GetProgressiveModifierStateText (the tooltip's
        // "Currently ..." line for every progressive/incremental modifier,
        // on explicit request).

        /// <summary>Plays one piece guaranteed to carry a trait (every deck token must already be tagged, e.g. via TagGoldenTokensRandom(Deck.DeckCount, ...)) and returns the outcome — used to drive Experience's _specialPiecesPlayedCount counter up by exactly 1 per call.</summary>
        private static PlacementOutcome PlaceOneSpecialPiece(RunManager run)
        {
            int slot = ChurnUntilHandMatches(run, t => t.Trait.HasValue);
            var token = run.Deck.Hand[slot].Value;
            var rotation = run.Deck.HandRotations[slot];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue, "Ran out of room while playing special pieces");
            return run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
        }

        [Test]
        public void Experience_GivesTheTrueFractionalMult_NotFlooredToZero_BelowTenSpecialPiecesPlayed()
        {
            // Same explicit request as CartesEnchantees above — the true
            // value (0.9) now applies in full via ProgressiveAdditiveMult,
            // instead of the old integer-step behavior flooring it to 0.
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.Experience);
            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));

            var outcome = PlaceOneSpecialPiece(run);
            for (int i = 1; i < 8; i++)
            {
                outcome = PlaceOneSpecialPiece(run);
            }

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(0f, outcome.Placement.AdditiveMultBonus, "The plain integer pool stays untouched by Experience now");
            Assert.AreEqual(0.9f, outcome.Placement.ProgressiveAdditiveMult, 0.0001f, "1 (baseline) + 8 played = 9 -> 9/10 = 0.9");
        }

        [Test]
        public void Experience_GivesOneMult_AtNineSpecialPiecesPlayed_CountingFromABaselineOfOne()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.Experience);
            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));

            var outcome = PlaceOneSpecialPiece(run);
            for (int i = 1; i < 9; i++)
            {
                outcome = PlaceOneSpecialPiece(run);
            }

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(1f, outcome.Placement.ProgressiveAdditiveMult, 0.0001f, "1 (baseline) + 9 played = 10 -> 10/10 = 1.0");
        }

        [Test]
        public void GetProgressiveModifierStateText_ReturnsNull_ForANonProgressiveModifier()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.IsNull(run.GetProgressiveModifierStateText(ModifierId.MultUn));
            Assert.IsNull(run.GetProgressiveModifierStateText(ModifierId.Prisme));
        }

        [Test]
        public void GetProgressiveModifierStateText_Gradient_MatchesGridsCurrentMultiplier()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.AreEqual("Currently x1", run.GetProgressiveModifierStateText(ModifierId.Gradient));

            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Gradient };
            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime,
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                run.Grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            run.Grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            Assert.AreEqual("Currently x" + run.Grid.GradientCurrentMultiplier, run.GetProgressiveModifierStateText(ModifierId.Gradient));
        }

        [Test]
        public void GetProgressiveModifierStateText_Solidarite_ReflectsTotalModifiersHeld()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugGrantModifier(ModifierId.Solidarite);
            run.DebugGrantModifier(ModifierId.MultUn);

            Assert.AreEqual("Currently +2 Mult", run.GetProgressiveModifierStateText(ModifierId.Solidarite));
        }

        [Test]
        public void GetProgressiveModifierStateText_Epuisement_MatchesGridsCurrentBonus()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.AreEqual("Currently +" + ScoringConstants.EpuisementStartingBonus + " pts", run.GetProgressiveModifierStateText(ModifierId.Epuisement));
        }

        [Test]
        public void GetProgressiveModifierStateText_Multitude_ReflectsCurrentDeckSize()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.AreEqual("Currently +" + (run.Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard) + " pts", run.GetProgressiveModifierStateText(ModifierId.Multitude));
        }

        [Test]
        public void GetProgressiveModifierStateText_CartesEnchantees_ShowsItsAdditiveContributionWithABaselineOfOne()
        {
            // Additive style ("+N Mult"), matching Solidarite, since this
            // modifier contributes to the SAME "+Mult" pool rather than
            // being a multiplier of its own — on explicit report: "tu as
            // oublié la baseline de 1 et non de 0" (showing this with an
            // "x" prefix made a below-1 value like 0.1 read as a NERF
            // instead of the bonus it actually is; the "baseline of 1" is
            // in the CARD COUNT ((1+count)/10), not a separate flat
            // addition on top).
            var run = new RunManager(new SystemRandomProvider(1));

            // No upgraded cards yet: still +0.1, never +0 ("counting from
            // a baseline of 1" card).
            Assert.AreEqual("Currently +0.1 Mult", run.GetProgressiveModifierStateText(ModifierId.CartesEnchantees));

            run.Deck.TagGoldenTokensRandom(22, new SystemRandomProvider(2));

            // (1 + 22) / 10 = 2.3.
            Assert.AreEqual("Currently +2.3 Mult", run.GetProgressiveModifierStateText(ModifierId.CartesEnchantees));
        }

        [Test]
        public void GetProgressiveModifierStateText_Experience_ShowsItsAdditiveContributionWithABaselineOfOne()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.Experience);

            Assert.AreEqual("Currently +0.1 Mult", run.GetProgressiveModifierStateText(ModifierId.Experience));

            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            PlaceOneSpecialPiece(run);
            PlaceOneSpecialPiece(run);
            PlaceOneSpecialPiece(run);

            // (1 + 3) / 10 = 0.4.
            Assert.AreEqual("Currently +0.4 Mult", run.GetProgressiveModifierStateText(ModifierId.Experience));
        }
    }
}
