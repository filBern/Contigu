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
        public void ApplyModifierPick_NeverRequiresRemoval_ModifierCountIsUnlimited()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            const int PicksBeyondOldCap = 7;

            for (int i = 0; i < PicksBeyondOldCap; i++)
            {
                PlayRoundToAwaitingDraft(run);
                run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
                Assert.AreEqual(RunState.AwaitingModifierPick, run.State);
                var options = run.RollModifierDraftOptions();
                bool applied = run.ApplyModifierPick(options[0].Id);
                Assert.IsTrue(applied);
                Assert.AreEqual(RunState.InProgress, run.State, "Picking a modifier should never require a removal step");
            }

            Assert.AreEqual(PicksBeyondOldCap, run.ActiveModifiers.Count);
        }

        [Test]
        public void RollModifierDraftOptions_NeverOffersAModifierAlreadyActive()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            for (int i = 0; i < 5; i++)
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
            Assert.AreEqual(RunState.AwaitingDraft, outcome.StateAfter);
            Assert.IsTrue(run.Deck.IsHandFullyEmpty(),
                "Hand should stay empty until the NEW round actually starts, not refill during this round's own ending placement");

            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            var modifierOptions = run.RollModifierDraftOptions();
            run.ApplyModifierPick(modifierOptions[0].Id);

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
        private static void PlayRoundToAwaitingDraft(RunManager run)
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
            Assert.AreEqual(RunState.AwaitingDraft, run.State);
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
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, outcome.Placement.GroupBonus);
            Assert.AreEqual(expectedMultiplier, outcome.Placement.GroupMultiplier);
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell * expectedMultiplier, outcome.Placement.TotalScore);
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
            int perCellAmount = ScoringConstants.GroupBonusPerCell; // group multiplier is 1 here — no tinted/multiplier cells involved
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
            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            var options = run.RollModifierDraftOptions();
            run.ApplyModifierPick(options[0].Id);

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
            // Group is 4 cells (the trait cell + the 3 pre-filled ones), no
            // tinted/multiplier factor here, so each cell's share is exactly
            // GroupBonusPerCell — Twin duplicates it onto the OTHER 3 cells.
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

        /// <summary>Plays a full round (quota met via all-golden cells, same trick as PlayRoundToAwaitingDraft), applies a throwaway upgrade, then picks exactly <paramref name="id"/> as the modifier — bypassing the random 3-option draft since ApplyModifierPick doesn't actually validate its argument against RollModifierDraftOptions' output. Also forces a fresh full 3-card hand afterward, since the round-ending placement can leave a partial hand carried into the next round.</summary>
        private static void GiveActiveModifier(RunManager run, ModifierId id)
        {
            PlayRoundToAwaitingDraft(run);
            run.ApplyUpgradeAndAdvance(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice));
            run.ApplyModifierPick(id);
            Assert.AreEqual(RunState.InProgress, run.State);
            CollectionAssert.Contains(run.ActiveModifiers, id);
            run.Deck.DrawNewHand();
        }

        [Test]
        public void SlotUn_DoublesGroupBonus_WhenPlacingFromHandSlotZero()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            GiveActiveModifier(run, ModifierId.SlotUn);

            var outcome = run.PlacePiece(0, 0, 0);

            Assert.IsTrue(outcome.Placement.Success);
            Assert.Greater(outcome.Placement.GroupBonus, 0);
            Assert.AreEqual(outcome.Placement.GroupBonus, outcome.Placement.ModifierBonus);
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
                int expected = handIndex == matchingHandIndex ? outcome.Placement.GroupBonus : 0;
                Assert.AreEqual(expected, outcome.Placement.ModifierBonus, id + " vs hand index " + handIndex);
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
    }
}
