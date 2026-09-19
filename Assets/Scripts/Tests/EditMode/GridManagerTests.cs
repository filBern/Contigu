using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public class GridManagerTests
    {
        [Test]
        public void CanPlace_ReturnsFalse_WhenOutOfBounds()
        {
            var grid = new GridManager();
            var shape = PieceShapeCatalog.Get(ShapeId.TriIH); // spans 3 cells horizontally

            Assert.IsFalse(grid.CanPlace(shape, 6, 0)); // would need x=6,7,8 -> out of bounds
            Assert.IsTrue(grid.CanPlace(shape, 5, 0));
        }

        [Test]
        public void CanPlace_ReturnsFalse_WhenOverlappingFilledOrLockedCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 3, 3);
            Assert.IsFalse(grid.CanPlace(single, 3, 3));

            grid.GetCell(4, 4).IsLocked = true;
            Assert.IsFalse(grid.CanPlace(single, 4, 4));
        }

        [Test]
        public void HasAnyValidPlacement_ReturnsTrue_OnAnEmptyBoard()
        {
            var grid = new GridManager();
            var shapes = new[] { PieceShapeCatalog.Get(ShapeId.Single) };

            Assert.IsTrue(grid.HasAnyValidPlacement(shapes));
        }

        [Test]
        public void HasAnyValidPlacement_ReturnsFalse_WhenBoardIsCompletelyBlocked()
        {
            var grid = new GridManager();
            foreach (var pos in GridManager.AllPositions())
            {
                grid.GetCell(pos).IsLocked = true;
            }
            var shapes = new[] { PieceShapeCatalog.Get(ShapeId.Single), PieceShapeCatalog.Get(ShapeId.Sq2) };

            Assert.IsFalse(grid.HasAnyValidPlacement(shapes));
        }

        [Test]
        public void HasAnyValidPlacement_ReturnsFalse_WhenTheOnlyFreeCellIsTooSmallForEveryHandShape()
        {
            var grid = new GridManager();
            foreach (var pos in GridManager.AllPositions())
            {
                if (pos.x != 0 || pos.y != 0)
                {
                    grid.GetCell(pos).IsLocked = true;
                }
            }
            var shapes = new[] { PieceShapeCatalog.Get(ShapeId.DomH) }; // needs 2 adjacent free cells; only 1 remains

            Assert.IsFalse(grid.HasAnyValidPlacement(shapes));
        }

        [Test]
        public void HasAnyValidPlacement_ReturnsTrue_WhenAtLeastOneHandShapeStillFits()
        {
            var grid = new GridManager();
            foreach (var pos in GridManager.AllPositions())
            {
                if (pos.x != 0 || pos.y != 0)
                {
                    grid.GetCell(pos).IsLocked = true;
                }
            }
            var shapes = new[] { PieceShapeCatalog.Get(ShapeId.DomH), PieceShapeCatalog.Get(ShapeId.Single) };

            Assert.IsTrue(grid.HasAnyValidPlacement(shapes)); // Single still fits at (0,0)
        }

        [Test]
        public void PlacePiece_SingleIsolatedCell_ScoresGroupOfOne()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(0, result.GoldenBonus);
            Assert.AreEqual(0, result.LineClearScore);
        }

        [Test]
        public void PlacePiece_IsolatedMultiCellPiece_ScoresFullPieceSize()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2); // 4 cells, alone on an empty board

            var result = grid.PlacePiece(square, PieceColor.Lime, 0, 0);

            // A piece's own cells are always mutually connected, so the group is
            // exactly the piece itself: 4 cells x 1 point/cell.
            Assert.AreEqual(4 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
        }

        [Test]
        public void PlacePiece_ConnectingToExistingGroup_RescoresWholeMergedGroupInFull()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2); // (0,0),(1,0),(0,1),(1,1)

            grid.PlacePiece(square, PieceColor.Lime, 0, 0); // group of 4
            var second = grid.PlacePiece(square, PieceColor.Lime, 2, 0); // touches (1,0)/(1,1) -> merges into one group of 8

            // The whole merged group is rescored in full on this placement, not
            // just the 4 newly placed cells (Scrabble-style word extension).
            Assert.AreEqual(8 * ScoringConstants.GroupBonusPerCell, second.GroupBonus);
        }

        [Test]
        public void PlacePiece_AdjacentDifferentColor_NoJoker_DoesNotMergeGroups()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            var second = grid.PlacePiece(single, PieceColor.Teal, 1, 0);

            // Different, non-joker colors never connect, so the second cell only
            // scores its own group of one.
            Assert.AreEqual(ScoringConstants.GroupBonusPerCell, second.GroupBonus);
        }

        [Test]
        public void PlacePiece_JokerColor_MergesWithAnyColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            var second = grid.PlacePiece(single, PieceColor.Joker, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, second.GroupBonus);
        }

        [Test]
        public void PlacePiece_JokerDoesNotBridgeTwoDifferentRealColorsIntoOneGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            var jokerResult = grid.PlacePiece(single, PieceColor.Joker, 1, 0);
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, jokerResult.GroupBonus, "Joker should still merge with the one real color it's adjacent to");

            var thirdResult = grid.PlacePiece(single, PieceColor.Teal, 2, 0);

            // Teal is a different real color from Lime. Even though Teal is
            // adjacent to the joker (which is itself adjacent to Lime), the
            // joker must NOT bridge Lime and Teal into one 3-cell group —
            // Teal's own group is just {Teal, Joker} = 2 cells, never
            // {Lime, Joker, Teal} = 3.
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, thirdResult.GroupBonus);
        }

        [Test]
        public void PlacePiece_ChainOfOnlyJokers_AllMergeIntoOneGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Joker, 0, 0);
            grid.PlacePiece(single, PieceColor.Joker, 1, 0);
            var third = grid.PlacePiece(single, PieceColor.Joker, 2, 0);

            // With no real color anywhere in the chain, there's nothing for a
            // joker to conflict with, so an all-joker chain still merges fully.
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, third.GroupBonus);
        }

        [Test]
        public void PlacePiece_OnGoldenCell_AddsFixedBonusIndependentOfGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(2, 2).IsGolden = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 2, 2);

            // Golden is a flat bonus, computed separately and just added — never
            // multiplied by group size or the group's tinted/multiplier factor.
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, result.GoldenBonus);
            Assert.AreEqual(ScoringConstants.GroupBonusPerCell, result.GroupBonus);
        }

        [Test]
        public void PlacePiece_GoldenCell_FiresAgainEveryTimeItsGroupIsRescored()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(0, 0).IsGolden = true;

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, first.GoldenBonus);

            // Connecting a new same-color cell grows the group and rescores it
            // in full — the golden cell is part of that group, so it fires
            // again, not just on its own original placement.
            var second = grid.PlacePiece(single, PieceColor.Coral, 1, 0);
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, second.GoldenBonus);

            var third = grid.PlacePiece(single, PieceColor.Coral, 2, 0);
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, third.GoldenBonus);
        }

        [Test]
        public void PlacePiece_GoldenCell_DoesNotFire_WhenUnrelatedPlacementElsewhere()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(0, 0).IsGolden = true;
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            // Different color, not adjacent to the golden cell's group at all.
            var elsewhere = grid.PlacePiece(single, PieceColor.Teal, 5, 5);

            Assert.AreEqual(0, elsewhere.GoldenBonus);
        }

        [Test]
        public void PlacePiece_TwoTintedCellsInSameGroup_StackTheGroupMultiplier()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var tintedA = grid.GetCell(0, 0);
            tintedA.IsTinted = true;
            tintedA.TintedColor = PieceColor.Coral;
            var tintedB = grid.GetCell(2, 0);
            tintedB.IsTinted = true;
            tintedB.TintedColor = PieceColor.Coral;

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 2, 0);

            // Placing the connecting middle cell merges all 3 into one group
            // containing BOTH matching tinted cells — their x2 factors stack
            // (x4), not just one flat x2. GroupBonus itself stays the plain
            // unmultiplied per-cell sum now — the factor lives in
            // GroupMultiplier and is only applied once, in TotalScore (see
            // "apply the multiplier at the end", explicit request).
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier * ScoringConstants.TintedMatchMultiplier, result.GroupMultiplier);
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell * ScoringConstants.TintedMatchMultiplier * ScoringConstants.TintedMatchMultiplier, result.TotalScore);
        }

        [Test]
        public void PlacePiece_GroupContainingTintedMatch_SetsGroupMultiplierToTwo()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var tintedCell = grid.GetCell(1, 0);
            tintedCell.IsTinted = true;
            tintedCell.TintedColor = PieceColor.Coral;

            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            // Group of 2, x2 because the tinted cell (anywhere in the group)
            // matches — the multiplier applies to the placement's WHOLE total
            // at the end (see TotalScore), not baked into GroupBonus itself.
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier, result.GroupMultiplier);
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * ScoringConstants.TintedMatchMultiplier, result.TotalScore);
        }

        [Test]
        public void PlacePiece_OnTintedCellWithWrongColor_LeavesGroupMultiplierAtOne()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var tintedCell = grid.GetCell(1, 0);
            tintedCell.IsTinted = true;
            tintedCell.TintedColor = PieceColor.Teal; // placed color (Coral) won't match

            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(1, result.GroupMultiplier);
        }

        [Test]
        public void PlacePiece_GroupContainingMultiplierZone_SetsGroupMultiplierToTwo()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            grid.GetCell(1, 0).IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.MultiplierZoneMultiplier, result.GroupMultiplier);
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * ScoringConstants.MultiplierZoneMultiplier, result.TotalScore);
        }

        [Test]
        public void PlacePiece_TwoMultiplierZoneCellsInSameGroup_StackTheGroupMultiplier()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.GetCell(0, 0).IsMultiplierZone = true;
            grid.GetCell(2, 0).IsMultiplierZone = true;

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 2, 0);

            // Placing the connecting middle cell merges all 3 into one group
            // containing BOTH multiplier-zone cells — their x2 factors stack
            // (x4), not just one flat x2. This mirrors Tinted's own stacking
            // (see the test above) and is what gives "Multiplier Beacon" (which
            // can tag many row/column cells with IsMultiplierZone at once) real
            // extra teeth beyond the plain single-cell Multiplier trait.
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier, result.GroupMultiplier);
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell * ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier, result.TotalScore);
        }

        [Test]
        public void PlacePiece_GroupWithTintedAndMultiplierZone_QuadruplesTheGroupMultiplier()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);

            var cell = grid.GetCell(1, 0);
            cell.IsTinted = true;
            cell.TintedColor = PieceColor.Lime;
            cell.IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
            Assert.AreEqual(4, result.GroupMultiplier);
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * 4, result.TotalScore);
        }

        [Test]
        public void PlacePiece_GroupMultiplier_AlsoAppliesToGoldenAndLineClearBonuses()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            // Fill x=0..6 of row 0, with a multiplier-zone AND a golden cell
            // already in the mix, then complete the row with the final piece —
            // the x2 group multiplier should now also apply to the golden
            // bonus and the line-clear bonus, not just the group bonus like
            // before (explicit request: "surtout avec les golden tiles et row
            // clear").
            grid.GetCell(3, 0).IsMultiplierZone = true;
            grid.GetCell(4, 0).IsGolden = true;
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }

            var finalResult = grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0);

            Assert.AreEqual(2, finalResult.GroupMultiplier);
            int expectedGroupBonus = GridManager.Size * ScoringConstants.GroupBonusPerCell;
            int expectedGoldenBonus = ScoringConstants.GoldenCellBonus;
            int expectedLineClearScore = GridManager.Size * ScoringConstants.LineClearBonusPerCell;
            Assert.AreEqual(expectedGroupBonus, finalResult.GroupBonus);
            Assert.AreEqual(expectedGoldenBonus, finalResult.GoldenBonus);
            Assert.AreEqual(expectedLineClearScore, finalResult.LineClearScore);
            Assert.AreEqual((expectedGroupBonus + expectedGoldenBonus + expectedLineClearScore) * 2, finalResult.TotalScore);
        }

        [Test]
        public void PlacePiece_CompletingRow_ClearsRowAndScoresPerCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }

            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0);

            Assert.AreEqual(GridManager.Size, finalResult.LineClearCellCount);
            Assert.AreEqual(GridManager.Size * ScoringConstants.LineClearBonusPerCell, finalResult.LineClearScore);

            for (int x = 0; x < GridManager.Size; x++)
            {
                Assert.IsFalse(grid.GetCell(x, 0).IsFilled, "Cell (" + x + ",0) should have been cleared");
            }
        }

        [Test]
        public void PlacePiece_RowWithLockedCell_CompletesWithoutFillingLockedCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(7, 0).IsLocked = true;

            // Fill the 6 non-locked cells x=0..5, leaving x=6 as the last non-locked cell.
            for (int x = 0; x < GridManager.Size - 2; x++)
            {
                var result = grid.PlacePiece(single, PieceColor.Violet, x, 0);
                Assert.AreEqual(0, result.LineClearCellCount);
            }

            var finalResult = grid.PlacePiece(single, PieceColor.Violet, GridManager.Size - 2, 0);

            // Row is complete because every NON-locked cell (x=0..6) is filled; locked cell (x=7) is excluded from the condition.
            Assert.AreEqual(GridManager.Size - 1, finalResult.LineClearCellCount);
            Assert.IsFalse(grid.GetCell(7, 0).IsFilled);
            Assert.IsTrue(grid.GetCell(7, 0).IsLocked);
        }

        [Test]
        public void LockRandomCells_AvoidsCellsWithExistingModifiers()
        {
            var grid = new GridManager();
            var rng = new SystemRandomProvider(99);
            int i = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (i >= 10) break;
                grid.GetCell(pos).IsGolden = true;
                i++;
            }

            var locked = grid.LockRandomCells(14, rng);

            foreach (var pos in locked)
            {
                Assert.IsFalse(grid.GetCell(pos).IsGolden, "Locked cell should avoid pre-existing golden modifier when possible");
            }
        }

        [Test]
        public void ResetForNewRound_ClearsFillLockAndModifiers()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.GetCell(1, 1).IsLocked = true;
            grid.GetCell(2, 2).IsGolden = true;
            var tinted = grid.GetCell(3, 3);
            tinted.IsTinted = true;
            tinted.TintedColor = PieceColor.Coral;
            grid.GetCell(4, 4).IsMultiplierZone = true;

            grid.ResetForNewRound();

            Assert.IsFalse(grid.GetCell(0, 0).IsFilled);
            Assert.IsFalse(grid.GetCell(1, 1).IsLocked);
            // A "Seeder"-tagged piece is the only thing that can leave a cell
            // golden/tinted/multiplier-zone past its own placement, and even
            // that is only meant to last "for the rest of the round" — a
            // permanent-for-the-whole-run golden cell was judged too
            // powerful — so every modifier flag clears here too, unlike fill/
            // lock's already-established per-round reset.
            Assert.IsFalse(grid.GetCell(2, 2).IsGolden, "Golden modifier should not survive a round reset");
            Assert.IsFalse(grid.GetCell(3, 3).IsTinted, "Tinted modifier should not survive a round reset");
            Assert.IsFalse(grid.GetCell(4, 4).IsMultiplierZone, "Multiplier-zone modifier should not survive a round reset");
        }

        [Test]
        public void PlacementsSinceLastClear_TracksConsecutiveNoClearPlacements_AndResetsOnAClear()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            Assert.AreEqual(0, grid.PlacementsSinceLastClear, "No placements made yet this round");

            grid.PlacePiece(single, PieceColor.Coral, 0, 5);
            Assert.AreEqual(1, grid.PlacementsSinceLastClear);

            grid.PlacePiece(single, PieceColor.Coral, 1, 5);
            Assert.AreEqual(2, grid.PlacementsSinceLastClear);

            for (int x = 0; x < GridManager.Size; x++)
            {
                grid.PlacePiece(single, PieceColor.Teal, x, 6);
            }
            // The row-6 loop above fills every cell of a fresh row (a clear),
            // so the streak should have reset to 0 by the time it returns here.
            Assert.AreEqual(0, grid.PlacementsSinceLastClear, "A placement that clears a line/column resets the streak");
        }

        [Test]
        public void PlacementsSinceLastClear_ResetsAtResetForNewRound()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0);
            Assert.AreEqual(2, grid.PlacementsSinceLastClear);

            grid.ResetForNewRound();

            Assert.AreEqual(0, grid.PlacementsSinceLastClear);
        }

        [Test]
        public void ClearRandomFilledCell_ClearsTheOnlyEligibleCell_ExcludingGivenPositions()
        {
            var grid = new GridManager();
            grid.GetCell(2, 2).IsFilled = true;
            grid.GetCell(2, 2).FilledColor = PieceColor.Coral;
            grid.GetCell(5, 5).IsFilled = true;
            grid.GetCell(5, 5).FilledColor = PieceColor.Teal;

            var cleared = grid.ClearRandomFilledCell(new SystemRandomProvider(1), new[] { new Vector2Int(2, 2) });

            Assert.AreEqual(new Vector2Int(5, 5), cleared);
            Assert.IsFalse(grid.GetCell(5, 5).IsFilled);
            Assert.IsTrue(grid.GetCell(2, 2).IsFilled, "Excluded position should never be touched");
        }

        [Test]
        public void ClearRandomFilledCell_NeverPicksALockedCell_EvenIfSomehowFilled()
        {
            var grid = new GridManager();
            // A locked cell is never actually filled through normal play
            // (CanPlace forbids it), but set both flags directly here to
            // prove ClearRandomFilledCell's own guard excludes it regardless.
            var locked = grid.GetCell(3, 3);
            locked.IsLocked = true;
            locked.IsFilled = true;
            locked.FilledColor = PieceColor.Coral;

            var cleared = grid.ClearRandomFilledCell(new SystemRandomProvider(1), System.Array.Empty<Vector2Int>());

            Assert.IsNull(cleared);
            Assert.IsTrue(locked.IsFilled, "Locked cell must never be cleared");
        }

        [Test]
        public void PlacePiece_ScoreEvents_OneGroupEntryPerCellInTheMergedGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 1);

            // Placing at (1,0) connects to both earlier cells, merging all three
            // into one group — every cell in the group gets rescored.
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            var groupEvents = new List<ScoreEvent>();
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Group) groupEvents.Add(e);
            }

            Assert.AreEqual(3, groupEvents.Count, "One event per cell in the merged group");
            var seenPositions = new HashSet<Vector2Int>();
            foreach (var e in groupEvents)
            {
                Assert.AreEqual(ScoringConstants.GroupBonusPerCell, e.Amount);
                seenPositions.Add(e.Position);
            }
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(1, 1)));
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(1, 0)));

            int sum = 0;
            foreach (var e in groupEvents) sum += e.Amount;
            Assert.AreEqual(result.GroupBonus, sum);
        }

        [Test]
        public void PlacePiece_ScoreEvents_IncludesOneGoldenEntryAlongsideTheGroupEntry()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(3, 3).IsGolden = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 3, 3);

            // Golden event for the fixed bonus, plus one group event for this
            // isolated cell's group of one.
            Assert.AreEqual(2, result.ScoreEvents.Count);

            ScoreEvent goldenEvent = null;
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Golden) goldenEvent = e;
            }
            Assert.IsNotNull(goldenEvent);
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, goldenEvent.Amount);
            Assert.AreEqual(new Vector2Int(3, 3), goldenEvent.Position);
        }

        [Test]
        public void PlacePiece_ScoreEvents_OneLineClearEntryPerClearedCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0);

            int lineClearEvents = 0;
            foreach (var e in finalResult.ScoreEvents)
            {
                if (e.Type == ScoreEventType.LineClear)
                {
                    lineClearEvents++;
                    Assert.AreEqual(ScoringConstants.LineClearBonusPerCell, e.Amount);
                }
            }
            Assert.AreEqual(GridManager.Size, lineClearEvents);
        }
    }
}
