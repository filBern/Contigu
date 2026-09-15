using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class UpgradeSystemTests
    {
        [Test]
        public void RollDraft_AlwaysReturnsThreeOptions_WithGuaranteedBankAndGridSlots()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();

                Assert.AreEqual(3, draft.Options.Length);
                Assert.AreEqual(UpgradePool.Bank, draft.Options[0].Pool);
                Assert.AreEqual(UpgradePool.Grid, draft.Options[1].Pool);
            }
        }

        [Test]
        public void RollDraft_ThirdOptionIsNeverADuplicateOfTheFirstTwo()
        {
            // The catalog always has more than 2 entries, so the exclusion pool is
            // never empty and the guarantee (spec 5.2) should hold every time.
            for (int seed = 0; seed < 50; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();

                Assert.AreNotSame(draft.Options[0], draft.Options[2]);
                Assert.AreNotSame(draft.Options[1], draft.Options[2]);
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
