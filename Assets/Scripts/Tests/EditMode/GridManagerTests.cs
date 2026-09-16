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
        public void PlacePiece_GroupContainingTintedMatch_DoublesWholeGroupBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var tintedCell = grid.GetCell(1, 0);
            tintedCell.IsTinted = true;
            tintedCell.TintedColor = PieceColor.Coral;

            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            // Group of 2, x2 because the tinted cell (anywhere in the group)
            // matches — the multiplier applies to the whole group, not just the
            // tinted cell itself.
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * ScoringConstants.TintedMatchMultiplier, result.GroupBonus);
        }

        [Test]
        public void PlacePiece_OnTintedCellWithWrongColor_DoesNotDoubleBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var tintedCell = grid.GetCell(1, 0);
            tintedCell.IsTinted = true;
            tintedCell.TintedColor = PieceColor.Teal; // placed color (Coral) won't match

            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, result.GroupBonus);
        }

        [Test]
        public void PlacePiece_GroupContainingMultiplierZone_DoublesWholeGroupBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            grid.GetCell(1, 0).IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * ScoringConstants.MultiplierZoneMultiplier, result.GroupBonus);
        }

        [Test]
        public void PlacePiece_GroupWithTintedAndMultiplierZone_QuadruplesWholeGroupBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);

            var cell = grid.GetCell(1, 0);
            cell.IsTinted = true;
            cell.TintedColor = PieceColor.Lime;
            cell.IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell * 4, result.GroupBonus);
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
        public void ApplyGoldenCellsRandom_MarksExactlyRequestedCount()
        {
            var grid = new GridManager();
            var rng = new SystemRandomProvider(1234);

            var chosen = grid.ApplyGoldenCellsRandom(3, rng);

            Assert.AreEqual(3, chosen.Count);
            int goldenCount = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (grid.GetCell(pos).IsGolden) goldenCount++;
            }
            Assert.AreEqual(3, goldenCount);
        }

        [Test]
        public void LockRandomCells_AvoidsCellsWithExistingModifiers()
        {
            var grid = new GridManager();
            var rng = new SystemRandomProvider(99);
            grid.ApplyGoldenCellsRandom(10, rng);

            var locked = grid.LockRandomCells(14, rng);

            foreach (var pos in locked)
            {
                Assert.IsFalse(grid.GetCell(pos).IsGolden, "Locked cell should avoid pre-existing golden modifier when possible");
            }
        }

        [Test]
        public void ResetForNewRound_ClearsFillAndLock_ButKeepsModifiers()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.GetCell(1, 1).IsLocked = true;
            grid.GetCell(2, 2).IsGolden = true;

            grid.ResetForNewRound();

            Assert.IsFalse(grid.GetCell(0, 0).IsFilled);
            Assert.IsFalse(grid.GetCell(1, 1).IsLocked);
            Assert.IsTrue(grid.GetCell(2, 2).IsGolden, "Golden modifier must persist across rounds");
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
