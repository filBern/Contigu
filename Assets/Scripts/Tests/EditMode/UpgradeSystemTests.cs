using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class UpgradeSystemTests
    {
        [Test]
        public void RollDraft_ReturnsThreeDistinctTileOptions_AllFromBankPool()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();

                Assert.AreEqual(3, draft.TileOptions.Length);
                var seen = new HashSet<UpgradeId>();
                foreach (var option in draft.TileOptions)
                {
                    Assert.AreEqual(UpgradePool.Bank, option.Pool);
                    Assert.IsTrue(seen.Add(option.Id), "Tile options should be distinct");
                }
            }
        }

        [Test]
        public void RollDraft_ReturnsAllThreeGridOptions_SincePoolHasExactlyThree()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();

                Assert.AreEqual(3, draft.GridOptions.Length);
                var seen = new HashSet<UpgradeId>();
                foreach (var option in draft.GridOptions)
                {
                    Assert.AreEqual(UpgradePool.Grid, option.Pool);
                    seen.Add(option.Id);
                }
                Assert.AreEqual(3, seen.Count, "All 3 distinct grid upgrades should be offered every time");
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
