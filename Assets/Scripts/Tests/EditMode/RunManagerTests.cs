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
        public void DebugForceRoundComplete_SetsRoundScoreToQuota_AndAdvancesToAwaitingDraft()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            var state = run.DebugForceRoundComplete();

            Assert.AreEqual(RunState.AwaitingDraft, state);
            Assert.AreEqual(RunState.AwaitingDraft, run.State);
            Assert.AreEqual(run.CurrentQuota, run.RoundScore);
        }

        [Test]
        public void DebugForceRoundComplete_NoOps_WhenNotInProgress()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugForceRoundComplete(); // now AwaitingDraft
            int roundScoreBefore = run.RoundScore;

            var state = run.DebugForceRoundComplete();

            Assert.AreEqual(RunState.AwaitingDraft, state);
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

        [Test]
        public void PlacePiece_DefersHandRefill_WhenTheEmptyingPlacementAlsoEndsTheRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            // Play 2 of the initial 3 hand pieces at whatever spots are free,
            // leaving exactly 1.
            var tokenA = run.Deck.Hand[0];
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[0]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            run.PlacePiece(0, anchorA.Value.x, anchorA.Value.y);

            var tokenB = run.Deck.Hand[0];
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[0]);
            var anchorB = FindAnyValidAnchor(run.Grid, shapeB);
            Assert.IsTrue(anchorB.HasValue);
            run.PlacePiece(0, anchorB.Value.x, anchorB.Value.y);

            Assert.AreEqual(1, run.Deck.Hand.Count, "Exactly 1 piece should remain after playing 2 of the initial 3");
            Assert.AreEqual(RunState.InProgress, run.State);

            var lastToken = run.Deck.Hand[0];
            var lastShape = PieceShapeCatalog.GetRotated(lastToken.Shape, run.Deck.HandRotations[0]);

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

            var outcome = run.PlacePiece(0, lastAnchor.Value.x, lastAnchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(RunState.AwaitingDraft, outcome.StateAfter);
            Assert.AreEqual(0, run.Deck.Hand.Count,
                "Hand should stay empty until the NEW round actually starts, not refill during this round's own ending placement");

            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            var modifierOptions = run.RollModifierDraftOptions();
            run.ApplyModifierPick(modifierOptions[0].Id);

            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.AreEqual(DeckManager.HandSize, run.Deck.Hand.Count,
                "The deferred hand should only be drawn once the new round actually starts");
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
        private static void PlayRoundToAwaitingDraft(RunManager run)
        {
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

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
            run.Deck.TagGoldenTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue);

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

        [Test]
        public void PlacePiece_BlastTrait_AlsoScoresFilledOrthogonalNeighborsAsGolden()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagBlastTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[0];
            const int anchorX = 3;
            const int anchorY = 3;
            Assert.IsTrue(run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), anchorX, anchorY));

            FillCell(run.Grid, anchorX - 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX + 1, anchorY, token.Color);
            FillCell(run.Grid, anchorX, anchorY - 1, token.Color);
            FillCell(run.Grid, anchorX, anchorY + 1, token.Color);

            var outcome = run.PlacePiece(0, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(5 * ScoringConstants.GoldenCellBonus, outcome.Placement.GoldenBonus,
                "The trait cell plus all 4 filled orthogonal neighbors should each score golden");
        }

        [Test]
        public void PlacePiece_BeaconTrait_StacksMultiplierWithFilledRowNeighbors()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagBeaconTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[0];
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

            var outcome = run.PlacePiece(0, anchorX, anchorY);

            Assert.IsTrue(outcome.Placement.Success);
            int expectedMultiplier = ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier;
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell * expectedMultiplier, outcome.Placement.GroupBonus);
        }

        [Test]
        public void PlacePiece_MirrorTrait_DuplicatesGroupBonusOntoSymmetricPartner()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagMirrorTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue && t.Shape == ShapeId.Single);

            var token = run.Deck.Hand[0];

            // Pre-fill an off-center 2-cell run so the Mirror-tagged Single
            // piece, placed at one end (2,3), has a genuine symmetric partner
            // at (4,3) once it merges the 3 cells into one group — not itself
            // (which would be the case if it landed in the middle).
            FillCell(run.Grid, 3, 3, token.Color);
            FillCell(run.Grid, 4, 3, token.Color);

            var outcome = run.PlacePiece(0, 2, 3);

            Assert.IsTrue(outcome.Placement.Success);
            int perCellAmount = ScoringConstants.GroupBonusPerCell; // group multiplier is 1 here — no tinted/multiplier cells involved
            Assert.AreEqual(perCellAmount, outcome.Placement.TraitBonus);

            bool foundMirrorEvent = false;
            foreach (var e in outcome.Placement.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Trait && e.Position == new Vector2Int(4, 3) && e.Amount == perCellAmount)
                {
                    foundMirrorEvent = true;
                }
            }
            Assert.IsTrue(foundMirrorEvent, "Mirror's duplicated bonus should appear as its own ScoreEvent at the symmetric partner cell");
        }

        [Test]
        public void PlacePiece_SeederTrait_LeavesTheGridCellGoldenForTheRestOfTheRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSeederTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[0];
            var rotation = run.Deck.HandRotations[0];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var traitPos = anchor.Value + shape.Cells[token.Trait.Value.LocalCellIndex];

            var outcome = run.PlacePiece(0, anchor.Value.x, anchor.Value.y);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.IsTrue(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden,
                "Seeder's stamp should NOT be cleared after scoring — unlike every other trait, it stays golden for the rest of THIS round");
        }

        [Test]
        public void PlacePiece_SeederTrait_ClearsOnceTheRoundItWasSetInEnds()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.Deck.TagSeederTokensRandom(run.Deck.DeckCount, new SystemRandomProvider(2));
            ChurnUntilHandMatches(run, t => t.Trait.HasValue);

            var token = run.Deck.Hand[0];
            var rotation = run.Deck.HandRotations[0];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var traitPos = anchor.Value + shape.Cells[token.Trait.Value.LocalCellIndex];

            run.PlacePiece(0, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden);

            // Skip straight to the next round via the debug helper (same one
            // wired to the F9 editor shortcut) rather than grinding out real
            // placements — a permanent-for-the-whole-run golden cell was
            // judged too powerful, so it should be gone once round 2 starts.
            run.DebugForceRoundComplete();
            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            var options = run.RollModifierDraftOptions();
            run.ApplyModifierPick(options[0].Id);

            Assert.AreEqual(2, run.CurrentRoundNumber);
            Assert.IsFalse(run.Grid.GetCell(traitPos.x, traitPos.y).IsGolden,
                "Seeder's stamp should be cleared once the round it was set in ends");
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
        /// would touch the grid) until the draw pile reshuffles from the
        /// now-tagged deck and a token matching <paramref name="predicate"/>
        /// actually reaches the hand.
        /// </summary>
        private static void ChurnUntilHandMatches(RunManager run, System.Func<PieceToken, bool> predicate)
        {
            int guard = 0;
            while (!predicate(run.Deck.Hand[0]))
            {
                run.Deck.PlayFromHand(0);
                guard++;
                Assert.Less(guard, 300, "A matching token should reach the hand well within a few reshuffle cycles");
            }
        }
    }
}
