using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class UpgradeSystemTests
    {
        [Test]
        public void RollDraft_ReturnsThreeOptions_WithAtLeastOneBankAndOneGridPick()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();

                Assert.AreEqual(3, draft.Options.Length);
                bool hasBank = false;
                bool hasGrid = false;
                foreach (var option in draft.Options)
                {
                    hasBank |= option.Pool == UpgradePool.Bank;
                    hasGrid |= option.Pool == UpgradePool.Grid;
                }
                Assert.IsTrue(hasBank, "Draft should always guarantee at least one Bank option");
                Assert.IsTrue(hasGrid, "Draft should always guarantee at least one Grid option");
            }
        }

        [Test]
        public void PickDistinct_ReturnsAllEntries_WhenCountExceedsPoolSize()
        {
            var pool = new List<int> { 1, 2, 3 };
            var picked = UpgradeSystem.PickDistinct(pool, 10, new SystemRandomProvider(1));

            Assert.AreEqual(3, picked.Length);
            var seen = new HashSet<int>(picked);
            Assert.AreEqual(3, seen.Count);
        }

        [Test]
        public void PickDistinct_ReturnsRequestedCount_WithoutDuplicates()
        {
            var pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            for (int seed = 0; seed < 10; seed++)
            {
                var picked = UpgradeSystem.PickDistinct(pool, 3, new SystemRandomProvider(seed));
                Assert.AreEqual(3, picked.Length);
                Assert.AreEqual(3, new HashSet<int>(picked).Count);
            }
        }

        [Test]
        public void Apply_RemovePiece_DelegatesToDeck()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < DeckManager.MinDeckSize + 1; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Single, PieceColor.Coral));
            }
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var grid = new GridManager();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            bool applied = system.Apply(UpgradeCatalog.RemovePiece, new UpgradeSubChoice(ShapeId.Single, PieceColor.Coral), grid, deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(DeckManager.MinDeckSize, deck.DeckCount);
        }

        [Test]
        public void Apply_JokerPiece_AddsJokerIgnoringSubChoice()
        {
            var tokens = new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) };
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var grid = new GridManager();
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            int before = deck.DeckCount;

            bool applied = system.Apply(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice), grid, deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(before + 1, deck.DeckCount);
        }

        [Test]
        public void Apply_GoldenCells_Marks3Cells()
        {
            var deck = new DeckManager(new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) }, new SystemRandomProvider(1));
            var grid = new GridManager();
            var system = new UpgradeSystem(new SystemRandomProvider(2));

            system.Apply(UpgradeCatalog.GoldenCells, default(UpgradeSubChoice), grid, deck);

            Assert.AreEqual(UpgradeSystem.GoldenCellsCount, CountWithModifier(grid, c => c.IsGolden));
        }

        [Test]
        public void Apply_TintedCells_Marks2CellsWithBaseColor()
        {
            var deck = new DeckManager(new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) }, new SystemRandomProvider(1));
            var grid = new GridManager();
            var system = new UpgradeSystem(new SystemRandomProvider(3));

            system.Apply(UpgradeCatalog.TintedCells, default(UpgradeSubChoice), grid, deck);

            int tintedCount = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                var cell = grid.GetCell(pos);
                if (cell.IsTinted)
                {
                    tintedCount++;
                    Assert.AreNotEqual(PieceColor.Joker, cell.TintedColor);
                }
            }
            Assert.AreEqual(UpgradeSystem.TintedCellsCount, tintedCount);
        }

        [Test]
        public void Apply_MultiplierZone_Marks3Cells()
        {
            var deck = new DeckManager(new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) }, new SystemRandomProvider(1));
            var grid = new GridManager();
            var system = new UpgradeSystem(new SystemRandomProvider(4));

            system.Apply(UpgradeCatalog.MultiplierZone, default(UpgradeSubChoice), grid, deck);

            Assert.AreEqual(UpgradeSystem.MultiplierZoneCount, CountWithModifier(grid, c => c.IsMultiplierZone));
        }

        private static int CountWithModifier(GridManager grid, System.Func<Cell, bool> predicate)
        {
            int count = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (predicate(grid.GetCell(pos))) count++;
            }
            return count;
        }
    }
}
