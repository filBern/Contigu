using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public class GridManagerTests
    {
        /// <summary>Group scoring is progressive (the Nth cell scored, 1-indexed, is worth N*GroupBonusPerCell — see GridManager.PlacePiece's group loop), so a full group of <paramref name="cellCount"/> cells earns the triangular number cellCount*(cellCount+1)/2 * GroupBonusPerCell, not a flat cellCount*GroupBonusPerCell.</summary>
        private static int ExpectedGroupBonus(int cellCount)
        {
            return cellCount * (cellCount + 1) / 2 * ScoringConstants.GroupBonusPerCell;
        }

        [Test]
        public void PlacePiece_ClearingAMonochromeLine_IsOneColorWorthOfLueur()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }
            var final = grid.PlacePiece(single, PieceColor.Coral, 7, 0);

            Assert.Greater(final.LineClearScore, 0, "Sanity check: row 0 should have cleared");
            Assert.AreEqual(EconomyConstants.LueurPerColorGroup, final.LueurEarned,
                "A fully monochrome line is one color, however many cells it spans");
            Assert.AreEqual(1, final.LueurGroups.Count);
            Assert.AreEqual(GridManager.Size, final.LueurGroups[0].Cells.Count, "The single group should cover every cell of that color");
        }

        [Test]
        public void PlacePiece_ClearingAllFourBaseColors_EarnsOneGroupPerColor()
        {
            // Each color appears twice, and never in two ADJACENT cells —
            // proves grouping is keyed on distinct color across the whole
            // line, not contiguous runs: this is 4 groups (one per color),
            // not 8 (one per run), even though no run is longer than 1 cell.
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var colors = new[] { PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime };
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, colors[x % colors.Length], x, 0);
            }
            var final = grid.PlacePiece(single, colors[(GridManager.Size - 1) % colors.Length], 7, 0);

            Assert.Greater(final.LineClearScore, 0, "Sanity check: row 0 should have cleared");
            Assert.AreEqual(colors.Length, final.LueurGroups.Count, "One group per distinct color, not per run");
            Assert.AreEqual(colors.Length * EconomyConstants.LueurPerColorGroup, final.LueurEarned);
        }

        [Test]
        public void PlacePiece_ColorSplitAcrossMultipleRuns_StillCountsAsOneGroup()
        {
            // Coral appears in two separate runs (start and end) with Teal
            // and Lime in between — only 3 distinct colors, so 3 groups
            // (was briefly 4 under a contiguous-run formula, since that
            // would have counted the two Coral runs separately).
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var colors = new[]
            {
                PieceColor.Coral, PieceColor.Coral, PieceColor.Teal, PieceColor.Teal,
                PieceColor.Teal, PieceColor.Lime, PieceColor.Lime, PieceColor.Coral
            };
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, colors[x], x, 0);
            }
            var final = grid.PlacePiece(single, colors[GridManager.Size - 1], 7, 0);

            Assert.AreEqual(3, final.LueurGroups.Count);
            Assert.AreEqual(3 * EconomyConstants.LueurPerColorGroup, final.LueurEarned);

            LueurGroup coralGroup = default;
            bool foundCoralGroup = false;
            for (int i = 0; i < final.LueurGroups.Count; i++)
            {
                if (final.LueurGroups[i].Cells.Count == 3)
                {
                    coralGroup = final.LueurGroups[i];
                    foundCoralGroup = true;
                }
            }
            Assert.IsTrue(foundCoralGroup, "The 3 Coral cells (split across two runs) should all end up in one group");
        }

        [Test]
        public void PlacePiece_JokerCellsInLine_NeverCountTowardAnyColorsGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var colors = new[]
            {
                PieceColor.Coral, PieceColor.Coral, PieceColor.Coral, PieceColor.Joker,
                PieceColor.Coral, PieceColor.Coral, PieceColor.Coral, PieceColor.Coral
            };
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, colors[x], x, 0);
            }
            var final = grid.PlacePiece(single, colors[GridManager.Size - 1], 7, 0);

            Assert.AreEqual(1, final.LueurGroups.Count, "Only the Coral color earns — the Joker cell doesn't start a group of its own");
            Assert.AreEqual(EconomyConstants.LueurPerColorGroup, final.LueurEarned);
            Assert.AreEqual(GridManager.Size - 1, final.LueurGroups[0].Cells.Count, "All 7 non-Joker cells should be in the one Coral group");
        }

        [Test]
        public void PlacePiece_NoLineClear_EarnsNoLueur()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            Assert.AreEqual(0, result.LueurEarned);
            Assert.AreEqual(0, result.LueurGroups.Count);
        }

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
            // exactly the piece itself: progressive scoring over 4 cells.
            Assert.AreEqual(ExpectedGroupBonus(4), result.GroupBonus);
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
            Assert.AreEqual(ExpectedGroupBonus(8), second.GroupBonus);
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

            Assert.AreEqual(ExpectedGroupBonus(2), second.GroupBonus);
        }

        [Test]
        public void PlacePiece_JokerDoesNotBridgeTwoDifferentRealColorsIntoOneGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            var jokerResult = grid.PlacePiece(single, PieceColor.Joker, 1, 0);
            Assert.AreEqual(ExpectedGroupBonus(2), jokerResult.GroupBonus, "Joker should still merge with the one real color it's adjacent to");

            var thirdResult = grid.PlacePiece(single, PieceColor.Teal, 2, 0);

            // Teal is a different real color from Lime. Even though Teal is
            // adjacent to the joker (which is itself adjacent to Lime), the
            // joker must NOT bridge Lime and Teal into one 3-cell group —
            // Teal's own group is just {Teal, Joker} = 2 cells, never
            // {Lime, Joker, Teal} = 3.
            Assert.AreEqual(ExpectedGroupBonus(2), thirdResult.GroupBonus);
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
            Assert.AreEqual(ExpectedGroupBonus(3), third.GroupBonus);
        }

        [Test]
        public void PlacePiece_JokerJoinsWhicheverAdjacentGroupScoresTheMost()
        {
            // Explicit request: "lorsqu'un joker est posé, il devrait être
            // jumelé avec le groupe faisant le plus de points" — when a
            // joker piece touches two different real colors at once, it
            // must anchor on whichever one's resulting group would score
            // higher, not whichever a fixed traversal order happens to
            // reach first.
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Lime, 0, 0);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0);
            grid.PlacePiece(single, PieceColor.Teal, 3, 0);
            grid.PlacePiece(single, PieceColor.Teal, 4, 0);

            // The joker at (1,0) touches the lone Lime cell on its left
            // (2-cell result) and the 3-cell Teal group on its right
            // (4-cell result) — it should join Teal.
            var jokerResult = grid.PlacePiece(single, PieceColor.Joker, 1, 0);

            Assert.AreEqual(ExpectedGroupBonus(4), jokerResult.GroupBonus);
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

            Assert.AreEqual(ExpectedGroupBonus(3), result.GroupBonus);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier * ScoringConstants.TintedMatchMultiplier, result.GroupMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(3) * ScoringConstants.TintedMatchMultiplier * ScoringConstants.TintedMatchMultiplier, result.TotalScore);
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
            Assert.AreEqual(ExpectedGroupBonus(2), result.GroupBonus);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier, result.GroupMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(2) * ScoringConstants.TintedMatchMultiplier, result.TotalScore);
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

            Assert.AreEqual(ExpectedGroupBonus(2), result.GroupBonus);
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

            Assert.AreEqual(ExpectedGroupBonus(2), result.GroupBonus);
            Assert.AreEqual(ScoringConstants.MultiplierZoneMultiplier, result.GroupMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(2) * ScoringConstants.MultiplierZoneMultiplier, result.TotalScore);
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

            Assert.AreEqual(ExpectedGroupBonus(3), result.GroupBonus);
            Assert.AreEqual(ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier, result.GroupMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(3) * ScoringConstants.MultiplierZoneMultiplier * ScoringConstants.MultiplierZoneMultiplier, result.TotalScore);
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

            Assert.AreEqual(ExpectedGroupBonus(2), result.GroupBonus);
            Assert.AreEqual(4, result.GroupMultiplier);
            // Only the multiplier-zone half of this stacked x4 reaches the
            // line-clear bonus — Tinted's own contribution never does (see
            // PlacePiece_TintedMatch_DoublesGroupAndGoldenBonusesButNotLineClear_UnlikeMultiplierZone).
            Assert.AreEqual(ScoringConstants.MultiplierZoneMultiplier, result.LineClearMultiplier);
            Assert.AreEqual(ExpectedGroupBonus(2) * 4, result.TotalScore);
        }

        [Test]
        public void PlacePiece_TintedMatch_DoublesGroupAndGoldenBonusesButNotLineClear_UnlikeMultiplierZone()
        {
            // On explicit player feedback that Tinted and Multiplier Zone had
            // become functionally identical once Tinted always matches its
            // own piece's color: Tinted's multiplier stops at the group and
            // golden bonuses and never reaches the line-clear bonus, unlike
            // Multiplier Zone (see PlacePiece_GroupMultiplier_AlsoAppliesToGoldenAndLineClearBonuses,
            // same setup but with IsMultiplierZone instead of IsTinted).
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var tinted = grid.GetCell(3, 0);
            tinted.IsTinted = true;
            tinted.TintedColor = PieceColor.Coral;
            grid.GetCell(4, 0).IsGolden = true;
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }

            var finalResult = grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0);

            int expectedGroupBonus = ExpectedGroupBonus(GridManager.Size);
            int expectedGoldenBonus = ScoringConstants.GoldenCellBonus;
            int expectedLineClearScore = GridManager.Size * ScoringConstants.LineClearBonusPerCell;
            Assert.AreEqual(expectedGroupBonus, finalResult.GroupBonus);
            Assert.AreEqual(expectedGoldenBonus, finalResult.GoldenBonus);
            Assert.AreEqual(expectedLineClearScore, finalResult.LineClearScore);
            Assert.AreEqual(ScoringConstants.TintedMatchMultiplier, finalResult.GroupMultiplier);
            Assert.AreEqual(1, finalResult.LineClearMultiplier, "Tinted alone should leave LineClearMultiplier at 1 — no multiplier-zone cell is involved");
            Assert.AreEqual((expectedGroupBonus + expectedGoldenBonus) * ScoringConstants.TintedMatchMultiplier + expectedLineClearScore, finalResult.TotalScore);
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
            int expectedGroupBonus = ExpectedGroupBonus(GridManager.Size);
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
        public void LockRandomCells_NeverLocksAnAlreadyFilledCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var locked = grid.LockRandomCells(GridManager.Size * GridManager.Size, new SystemRandomProvider(1));

            CollectionAssert.DoesNotContain(locked, new Vector2Int(0, 0), "A cell the player has actually filled should never become a boss obstacle");
        }

        [Test]
        public void LockFreeCellsAndCheckClears_OnlyLocksStillEmptyCells()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var outcome = grid.LockFreeCellsAndCheckClears(GridManager.Size * GridManager.Size, new SystemRandomProvider(1));

            CollectionAssert.DoesNotContain(outcome.LockedCells, new Vector2Int(0, 0));
            foreach (var pos in outcome.LockedCells)
            {
                Assert.IsTrue(grid.GetCell(pos).IsLocked);
            }
        }

        [Test]
        public void LockFreeCellsAndCheckClears_CompletingALineByLockingItsLastEmptyCell_ScoresAndClears()
        {
            // Every cell except (7, 0) is already filled — the single
            // remaining empty spot is the only `!IsLocked && !IsFilled`
            // candidate anywhere on the board, so the lock is deterministic
            // regardless of the RNG seed, and locking it instantly completes
            // (and clears) both its row and its column.
            var grid = new GridManager();
            foreach (var pos in GridManager.AllPositions())
            {
                if (pos.x == 7 && pos.y == 0)
                {
                    continue;
                }
                var cell = grid.GetCell(pos);
                cell.IsFilled = true;
                cell.FilledColor = PieceColor.Coral;
            }

            var outcome = grid.LockFreeCellsAndCheckClears(1, new SystemRandomProvider(1));

            CollectionAssert.AreEqual(new[] { new Vector2Int(7, 0) }, outcome.LockedCells);
            Assert.Greater(outcome.LineClearScore, 0, "Locking the board's last empty cell should complete and clear at least its row and column");
            CollectionAssert.Contains(outcome.ClearedCells, new Vector2Int(0, 0));
        }

        [Test]
        public void CompletingALine_CreditsALockedBastionCellTheLineClearBonusWithoutClearingIt()
        {
            // "Bastion Tile" (on explicit request — "Locked cell upgraded.
            // N'est pas cleared mais fait quand même les points cleared"):
            // GridManager doesn't need to know about PieceTrait to honor
            // this — it's purely a Cell.IsBastion/IsLocked combination.
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var bastionCell = grid.GetCell(3, 0);
            bastionCell.IsFilled = true;
            bastionCell.FilledColor = PieceColor.Coral;
            bastionCell.IsBastion = true;
            bastionCell.IsLocked = true;

            grid.PlacePiece(single, PieceColor.Teal, 0, 0);
            grid.PlacePiece(single, PieceColor.Teal, 1, 0);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0);
            grid.PlacePiece(single, PieceColor.Teal, 4, 0);
            grid.PlacePiece(single, PieceColor.Teal, 5, 0);
            grid.PlacePiece(single, PieceColor.Teal, 6, 0);
            var final = grid.PlacePiece(single, PieceColor.Teal, 7, 0);

            Assert.AreEqual(GridManager.Size - 1, final.LineClearCellCount, "Only the 7 unlocked cells actually clear");
            Assert.AreEqual(GridManager.Size * ScoringConstants.LineClearBonusPerCell, final.LineClearScore,
                "The locked Bastion cell still earns the same per-cell bonus as an actually-cleared cell");
            CollectionAssert.DoesNotContain(final.ClearedCells, new Vector2Int(3, 0));

            var stillThere = grid.GetCell(3, 0);
            Assert.IsTrue(stillThere.IsFilled, "Bastion cell should never actually be cleared");
            Assert.IsTrue(stillThere.IsLocked);
            Assert.IsTrue(stillThere.IsBastion);
            Assert.AreEqual(PieceColor.Coral, stillThere.FilledColor);
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
        public void CompletingALine_AlsoClearsAnyModifierFlagsAndOriginTraitOnThoseCells()
        {
            // On explicit player report: a cell used to keep its Golden/
            // Tinted/MultiplierZone/OriginTrait stamp even once cleared by a
            // completed line, so an unrelated piece placed in that exact
            // spot afterward would wrongly inherit an enchantment it never
            // earned (most notably a "Seeder" cell, otherwise golden for
            // the rest of the round regardless of what's actually filling it).
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.GetCell(3, 0).IsGolden = true;
            var tinted = grid.GetCell(4, 0);
            tinted.IsTinted = true;
            tinted.TintedColor = PieceColor.Coral;
            grid.GetCell(5, 0).IsMultiplierZone = true;
            grid.GetCell(6, 0).OriginTrait = new PieceTrait(PieceTraitKind.Golden, 0);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }
            grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0);

            Assert.IsFalse(grid.GetCell(3, 0).IsFilled, "Line should have cleared");
            Assert.IsFalse(grid.GetCell(3, 0).IsGolden);
            Assert.IsFalse(grid.GetCell(4, 0).IsTinted);
            Assert.IsFalse(grid.GetCell(5, 0).IsMultiplierZone);
            Assert.IsFalse(grid.GetCell(6, 0).OriginTrait.HasValue);

            // An unrelated piece landing on the same spot afterward should
            // score as a completely plain cell — no leftover golden bonus.
            var result = grid.PlacePiece(single, PieceColor.Teal, 3, 0);
            Assert.AreEqual(0, result.GoldenBonus);
        }

        [Test]
        public void PlacePiece_ClearedCellTraits_CapturesEachClearedCellsOriginTraitBeforeWipingIt()
        {
            // Regression test (bug report: "lorsqu'on clear une ligne, le
            // badge doit se détruire en même temps que sa tuile, pas au
            // début du décomptage de point") — the presentation layer holds
            // a just-completed line visually filled while its score plays
            // out (see GridView.RefreshHoldingClearedCells) and needs each
            // cleared cell's pre-clear OriginTrait, parallel to
            // ClearedCells/ClearedCellColors, so the trait badge keeps
            // showing during that hold instead of disappearing the instant
            // Core clears the line (Cell.OriginTrait itself is still wiped
            // immediately, same as before — only this extra parallel list
            // is new).
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.GetCell(2, 0).OriginTrait = new PieceTrait(PieceTraitKind.Bastion, 0);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
            }
            var result = grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0);

            Assert.AreEqual(GridManager.Size, result.ClearedCells.Count);
            Assert.AreEqual(GridManager.Size, result.ClearedCellTraits.Count);

            int taggedIndex = -1;
            for (int i = 0; i < result.ClearedCells.Count; i++)
            {
                if (result.ClearedCells[i] == new Vector2Int(2, 0))
                {
                    taggedIndex = i;
                    break;
                }
            }
            Assert.AreNotEqual(-1, taggedIndex, "The tagged cell should be among the cleared cells");
            Assert.IsTrue(result.ClearedCellTraits[taggedIndex].HasValue);
            Assert.AreEqual(PieceTraitKind.Bastion, result.ClearedCellTraits[taggedIndex].Value.Kind);

            for (int i = 0; i < result.ClearedCells.Count; i++)
            {
                if (i == taggedIndex)
                {
                    continue;
                }
                Assert.IsFalse(result.ClearedCellTraits[i].HasValue, "Every other cleared cell never had a trait");
            }

            Assert.IsFalse(grid.GetCell(2, 0).OriginTrait.HasValue, "Core state itself is still wiped immediately");
        }

        [Test]
        public void ClearRandomFilledCell_AlsoClearsAnyModifierFlagsAndOriginTraitOnThatCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(3, 3).IsGolden = true;
            grid.GetCell(3, 3).OriginTrait = new PieceTrait(PieceTraitKind.Golden, 0);
            grid.PlacePiece(single, PieceColor.Coral, 3, 3);

            var cleared = grid.ClearRandomFilledCell(new SystemRandomProvider(1), System.Array.Empty<Vector2Int>());

            Assert.IsTrue(cleared.HasValue);
            Assert.AreEqual(new Vector2Int(3, 3), cleared.Value);
            Assert.IsFalse(grid.GetCell(3, 3).IsGolden);
            Assert.IsFalse(grid.GetCell(3, 3).OriginTrait.HasValue);
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
            var seenAmounts = new List<int>();
            foreach (var e in groupEvents)
            {
                seenPositions.Add(e.Position);
                seenAmounts.Add(e.Amount);
            }
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(1, 1)));
            Assert.IsTrue(seenPositions.Contains(new Vector2Int(1, 0)));

            // Group scoring is progressive (see ExpectedGroupBonus's doc
            // comment), so the 3 cells don't share one flat amount anymore —
            // each earns its scan-order index (1, 2 or 3) times
            // GroupBonusPerCell, in whatever order the flood fill visits
            // them. Check the multiset of amounts rather than a fixed order.
            seenAmounts.Sort();
            Assert.AreEqual(1 * ScoringConstants.GroupBonusPerCell, seenAmounts[0]);
            Assert.AreEqual(2 * ScoringConstants.GroupBonusPerCell, seenAmounts[1]);
            Assert.AreEqual(3 * ScoringConstants.GroupBonusPerCell, seenAmounts[2]);

            int sum = 0;
            foreach (var e in groupEvents) sum += e.Amount;
            Assert.AreEqual(result.GroupBonus, sum);
            Assert.AreEqual(ExpectedGroupBonus(3), sum);
        }

        [Test]
        public void PlacePiece_GroupScoreEvents_FollowReadingOrder_TopRowFirstThenLeftToRight()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            // Builds a staggered group across 2 rows/3 columns: (0,0)-(1,0)
            // on the bottom row, (1,1)-(2,1) on the top row, connected only
            // through (1,0)-(1,1). Finishing at (2,1) makes the flood fill
            // start from the TOP-RIGHT cell and walk left/down from there —
            // the opposite of reading order — so this only passes if
            // FloodFillGroup's own explicit sort (not incidental DFS luck)
            // is what's producing the final order.
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 1);
            var result = grid.PlacePiece(single, PieceColor.Coral, 2, 1);

            var groupPositions = new List<Vector2Int>();
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Group)
                {
                    groupPositions.Add(e.Position);
                }
            }

            // Reading order (on explicit report: "j'ai l'impression qu'on
            // passe au travers des pièces... j'aimerais qu'on le fasse pas
            // ordre de lecture (gauche à droite en partant d'en haut)") —
            // y increases UPWARD on screen (see GridView.Build), so the
            // "top" row is the higher y, scanned first; then left-to-right
            // (ascending x) within each row, top row before bottom row.
            var expected = new List<Vector2Int>
            {
                new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(0, 0), new Vector2Int(1, 0)
            };
            Assert.AreEqual(expected, groupPositions);
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

        [Test]
        public void PreviewGroup_OnAnEmptyBoard_IsJustThePiecesOwnCells()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);

            var preview = grid.PreviewGroup(square, PieceColor.Coral, 2, 2);

            Assert.AreEqual(square.Cells.Count, preview.Count);
            foreach (var offset in square.Cells)
            {
                CollectionAssert.Contains(preview, new Vector2Int(2 + offset.x, 2 + offset.y));
            }
        }

        [Test]
        public void PreviewGroup_IncludesAdjacentSameColorCells_WithoutMutatingTheGrid()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0);

            // Placing a 3rd Coral single at (2,0) would join the existing
            // 2-cell group into a 3-cell one — the preview should show all
            // 3 positions even though nothing has actually been placed yet.
            var preview = grid.PreviewGroup(single, PieceColor.Coral, 2, 0);

            Assert.AreEqual(3, preview.Count);
            CollectionAssert.Contains(preview, new Vector2Int(0, 0));
            CollectionAssert.Contains(preview, new Vector2Int(1, 0));
            CollectionAssert.Contains(preview, new Vector2Int(2, 0));

            // Nothing was actually mutated by the preview call.
            Assert.IsFalse(grid.GetCell(2, 0).IsFilled);
        }

        [Test]
        public void PreviewGroup_ExcludesAdjacentCellsOfADifferentColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Teal, 1, 0);

            var preview = grid.PreviewGroup(single, PieceColor.Coral, 0, 1);

            // (0,1) is adjacent to (0,0) [Coral, joins] but not to (1,0)
            // [Teal, diagonal — never adjacent anyway] — regardless, a Teal
            // cell must never appear in a Coral placement's preview.
            CollectionAssert.Contains(preview, new Vector2Int(0, 0));
            CollectionAssert.Contains(preview, new Vector2Int(0, 1));
            CollectionAssert.DoesNotContain(preview, new Vector2Int(1, 0));
        }
    }
}
