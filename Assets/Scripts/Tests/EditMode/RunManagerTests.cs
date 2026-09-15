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
        public void Round_EndsOnlyWhenBudgetExhausted_EvenIfQuotaReachedEarly()
        {
            var run = new RunManager(new SystemRandomProvider(42));

            // Every cell is golden so score accumulates fast regardless of shape,
            // color or adjacency luck — this test is about the round-end STATE
            // MACHINE (spec 3.4: evaluated only once the budget is exhausted),
            // not about scoring itself (covered by GridManagerTests).
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            int budget = run.CurrentBudget;
            for (int i = 0; i < budget; i++)
            {
                var token = run.Deck.Hand[0];
                var shape = PieceShapeCatalog.Get(token.Shape);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue, "Ran out of room on placement " + i + " of " + budget);

                var outcome = run.PlacePiece(0, anchor.Value.x, anchor.Value.y);
                Assert.IsTrue(outcome.Placement.Success);

                if (i < budget - 1)
                {
                    Assert.AreEqual(RunState.InProgress, outcome.StateAfter,
                        "Round must not end before its piece budget is exhausted, even if the quota was already reached.");
                }
            }

            Assert.AreEqual(0, run.PiecesRemainingThisRound);
            Assert.GreaterOrEqual(run.RoundScore, run.CurrentQuota);
            Assert.AreEqual(RunState.AwaitingDraft, run.State);

            var draft = run.RollDraftOptions();
            Assert.AreEqual(3, draft.Options.Length);

            bool applied = run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));

            Assert.IsTrue(applied);
            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.AreEqual(0, run.RoundScore);
            Assert.AreEqual(RunConfig.PieceBudgets[1], run.PiecesRemainingThisRound);
            Assert.AreEqual(RunState.InProgress, run.State);
        }
    }
}
