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
        public void PlacePiece_SingleIsolatedCell_HasZeroNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.NeighborBonus);
            Assert.AreEqual(0, result.GoldenBonus);
            Assert.AreEqual(0, result.LineClearScore);
        }

        [Test]
        public void PlacePiece_AdjacentMatchingColor_AddsNeighborBonusPerPair()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            var second = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair, second.NeighborBonus);
        }

        [Test]
        public void PlacePiece_AdjacentDifferentColor_NoJoker_NoNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            var second = grid.PlacePiece(single, PieceColor.Teal, 1, 0);

            Assert.AreEqual(0, second.NeighborBonus);
        }

        [Test]
        public void PlacePiece_JokerColor_AlwaysMatchesNeighbor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            var second = grid.PlacePiece(single, PieceColor.Joker, 1, 0);

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair, second.NeighborBonus);
        }

        [Test]
        public void PlacePiece_MultiCellPiece_DoesNotScoreAgainstItsOwnSiblingCells()
        {
            var grid = new GridManager();
            var domino = PieceShapeCatalog.Get(ShapeId.DomH); // (0,0),(1,0) - two same-colored adjacent cells placed together, alone on an empty board

            var result = grid.PlacePiece(domino, PieceColor.Violet, 0, 0);

            // The two cells are adjacent to each other but neither was on the
            // board before this placement, so a piece never scores a neighbor
            // bonus purely from its own shape — only connections to already-
            // filled cells count.
            Assert.AreEqual(0, result.NeighborBonus);
        }

        [Test]
        public void PlacePiece_MultiCellPiece_ScoresAgainstPreExistingNeighborsOnly()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var domino = PieceShapeCatalog.Get(ShapeId.DomV); // (0,0),(0,1)

            grid.PlacePiece(single, PieceColor.Violet, 2, 0); // pre-existing neighbor of the domino's (1,0) cell

            var result = grid.PlacePiece(domino, PieceColor.Violet, 1, 0);

            // Only the (1,0)->(2,0) connection to the pre-existing cell counts;
            // the domino's own two cells still don't score against each other.
            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair, result.NeighborBonus);
        }

        [Test]
        public void PlacePiece_OnGoldenCell_AddsFixedBonusRegardlessOfColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(2, 2).IsGolden = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 2, 2);

            Assert.AreEqual(ScoringConstants.GoldenCellBonus, result.GoldenBonus);
        }

        [Test]
        public void PlacePiece_OnTintedCellWithMatchingColor_DoublesNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var tintedCell = grid.GetCell(1, 0);
            tintedCell.IsTinted = true;
            tintedCell.TintedColor = PieceColor.Coral;

            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair * ScoringConstants.TintedMatchMultiplier, result.NeighborBonus);
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

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair, result.NeighborBonus);
        }

        [Test]
        public void PlacePiece_InMultiplierZone_DoublesNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            grid.GetCell(1, 0).IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair * ScoringConstants.MultiplierZoneMultiplier, result.NeighborBonus);
        }

        [Test]
        public void PlacePiece_TintedAndMultiplierStack_QuadruplesNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Lime, 0, 0);

            var cell = grid.GetCell(1, 0);
            cell.IsTinted = true;
            cell.TintedColor = PieceColor.Lime;
            cell.IsMultiplierZone = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 1, 0);

            Assert.AreEqual(ScoringConstants.NeighborBonusPerPair * 4, result.NeighborBonus);
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
        public void PlacePiece_ScoreEvents_OneEntryPerMatchingNeighbor_SummingToNeighborBonus()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 1);

            // Placing at (1,0) is orthogonally adjacent to both earlier cells.
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            var neighborEvents = new List<ScoreEvent>();
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Neighbor) neighborEvents.Add(e);
            }

            Assert.AreEqual(2, neighborEvents.Count, "One event per matching neighbor direction");
            foreach (var e in neighborEvents)
            {
                Assert.AreEqual(ScoringConstants.NeighborBonusPerPair, e.Amount);
                Assert.AreEqual(new Vector2Int(1, 0), e.Position);
            }

            int sum = 0;
            foreach (var e in neighborEvents) sum += e.Amount;
            Assert.AreEqual(result.NeighborBonus, sum);
        }

        [Test]
        public void PlacePiece_ScoreEvents_OneGoldenEntryPerGoldenCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(3, 3).IsGolden = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 3, 3);

            Assert.AreEqual(1, result.ScoreEvents.Count);
            var e = result.ScoreEvents[0];
            Assert.AreEqual(ScoreEventType.Golden, e.Type);
            Assert.AreEqual(ScoringConstants.GoldenCellBonus, e.Amount);
            Assert.AreEqual(new Vector2Int(3, 3), e.Position);
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
