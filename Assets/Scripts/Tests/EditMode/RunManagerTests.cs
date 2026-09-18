using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public class RunManagerTests
    {
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
        public void ApplyUpgradeAndAdvance_Fails_WhenNotAwaitingDraft()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            Assert.AreEqual(RunState.InProgress, run.State);

            bool applied = run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));

            Assert.IsFalse(applied);
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
                var token = run.Deck.Hand[0];
                var rotation = run.Deck.HandRotations[0];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue, "Ran out of room on placement " + piecesPlaced);

                run.PlacePiece(0, anchor.Value.x, anchor.Value.y);
                piecesPlaced++;

                Assert.Less(piecesPlaced, budget, "Should reach the quota well before exhausting the budget given every cell is golden");
            }

            Assert.AreEqual(RunState.AwaitingDraft, run.State);
            Assert.GreaterOrEqual(run.RoundScore, run.CurrentQuota);
            Assert.Greater(run.PiecesRemainingThisRound, 0, "Round should end with budget still remaining once the quota is reached");

            var draft = run.RollDraftOptions();
            Assert.AreEqual(3, draft.Options.Length);

            bool applied = run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            Assert.IsTrue(applied);
            Assert.AreEqual(RunState.AwaitingModifierPick, run.State);
            Assert.AreEqual(1, run.CurrentRoundNumber, "Round shouldn't advance yet — a modifier pick is still pending");

            var modifierOptions = run.RollModifierDraftOptions();
            Assert.AreEqual(3, modifierOptions.Length);

            bool modifierApplied = run.ApplyModifierPick(modifierOptions[0].Id);

            Assert.IsTrue(modifierApplied);
            Assert.AreEqual(1, run.ActiveModifiers.Count);
            Assert.AreEqual(modifierOptions[0].Id, run.ActiveModifiers[0]);
            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.AreEqual(0, run.RoundScore);
            Assert.AreEqual(RunConfig.PieceBudgets[1], run.PiecesRemainingThisRound);
            Assert.AreEqual(RunState.InProgress, run.State);
        }

        [Test]
        public void ApplyModifierPick_RequiresRemoval_WhenPushingPastTheFiveSlotCap()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            // Every cell golden so each round's quota is reached almost
            // instantly, regardless of shape/color/adjacency luck — this test
            // is about the modifier-slot STATE MACHINE, not scoring.
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            for (int i = 0; i < RunManager.MaxActiveModifiers; i++)
            {
                PlayRoundToAwaitingDraft(run);
                run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
                Assert.AreEqual(RunState.AwaitingModifierPick, run.State);
                var options = run.RollModifierDraftOptions();
                bool applied = run.ApplyModifierPick(options[0].Id);
                Assert.IsTrue(applied);
            }

            Assert.AreEqual(RunManager.MaxActiveModifiers, run.ActiveModifiers.Count);
            Assert.AreEqual(RunState.InProgress, run.State);

            PlayRoundToAwaitingDraft(run);
            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            var sixthOptions = run.RollModifierDraftOptions();
            run.ApplyModifierPick(sixthOptions[0].Id);

            Assert.AreEqual(RunState.AwaitingModifierRemoval, run.State);
            Assert.AreEqual(RunManager.MaxActiveModifiers + 1, run.ActiveModifiers.Count);

            var toRemove = run.ActiveModifiers[0];
            bool removed = run.RemoveModifierAndAdvance(toRemove);

            Assert.IsTrue(removed);
            Assert.AreEqual(RunManager.MaxActiveModifiers, run.ActiveModifiers.Count);
            Assert.AreEqual(RunState.InProgress, run.State);
        }

        [Test]
        public void RollModifierDraftOptions_NeverOffersAModifierAlreadyActive()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            for (int i = 0; i < RunManager.MaxActiveModifiers; i++)
            {
                PlayRoundToAwaitingDraft(run);
                run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
                var options = run.RollModifierDraftOptions();

                foreach (var option in options)
                {
                    CollectionAssert.DoesNotContain(run.ActiveModifiers, option.Id,
                        "A modifier already held should never be offered again in the same run");
                }

                run.ApplyModifierPick(options[0].Id);
            }
        }

        /// <summary>Places pieces from hand until the round's quota is reached (assumes every cell is already golden, as set up by the caller, so this converges quickly).</summary>
        private static void PlayRoundToAwaitingDraft(RunManager run)
        {
            int guard = 0;
            while (run.State == RunState.InProgress)
            {
                var token = run.Deck.Hand[0];
                var rotation = run.Deck.HandRotations[0];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue, "Ran out of room before reaching the quota");
                run.PlacePiece(0, anchor.Value.x, anchor.Value.y);
                guard++;
                Assert.Less(guard, 100, "Round should reach its quota well within 100 placements given every cell is golden");
            }
            Assert.AreEqual(RunState.AwaitingDraft, run.State);
        }

        [Test]
        public void PlacePiece_TriggersDefeat_WhenBoardBecomesFullyBlockedAfterThisPlacement()
        {
            var run = new RunManager(new SystemRandomProvider(7));
            var token = run.Deck.Hand[0];
            var rotation = run.Deck.HandRotations[0];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            // Lock every cell except exactly the ones this piece will occupy at
            // (0,0) — using its actual DEALT rotation, since that's what
            // RunManager.PlacePiece will place. Note this placement will itself
            // complete (and clear) every row/column it touches, since the
            // footprint ends up being the only non-locked cells in each of them
            // — reopening a footprint-shaped hole right after the placement.
            // For seed 7 (verified by hand-tracing the deterministic draw, see
            // /tmp/dotnet_random_sim.py), hand[0] is an L-tromino (TriL) and the
            // two pieces left in hand are a T-tetromino (TTetro, 4 cells — can
            // never fit a 3-cell hole, in any rotation) and a straight tromino
            // (TriIV — can never fit an L-shaped hole, in any of its 2 distinct
            // rotations). Both mismatches (cell count, and straight-vs-bent
            // shape) hold regardless of which rotation each piece was actually
            // dealt, so this test is robust to random rotation. It's still tied
            // to seed 7's exact hand *composition* though — if InitialDeckFactory
            // or the shuffle ever changes, re-verify by hand or pick a new seed.
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
        public void PlacePiece_AppliesAGoldenTraitedToken_AsAOneTimeCellBonus_ThenClearsTheCell()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            // Tag every deck token golden — this only updates the deck (the
            // source of truth), not the hand/draw-pile copies already dealt at
            // construction time, so churn hand draws (without going through
            // RunManager, which would touch the grid) until the draw pile
            // reshuffles from the now-fully-golden deck and a tagged token
            // actually reaches the hand.
            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            int guard = 0;
            while (!run.Deck.Hand[0].Trait.HasValue)
            {
                run.Deck.PlayFromHand(0);
                guard++;
                Assert.Less(guard, 200, "A golden-tagged token should reach the hand well within a few reshuffle cycles");
            }

            var token = run.Deck.Hand[0];
            var rotation = run.Deck.HandRotations[0];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);

            var outcome = run.PlacePiece(0, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.GoldenBonus, 0, "The token's own golden tile should have scored a golden bonus on this placement");

            var offset = shape.Cells[token.Trait.Value.LocalCellIndex];
            var landedCell = run.Grid.GetCell(anchor.Value.x + offset.x, anchor.Value.y + offset.y);
            Assert.IsFalse(landedCell.IsGolden, "Golden is a one-time enchantment on the token, not a permanent grid modifier — it should be cleared right after scoring");
        }
    }
}
