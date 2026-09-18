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
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            bool applied = system.Apply(UpgradeCatalog.RemovePiece, new UpgradeSubChoice(ShapeId.Single, PieceColor.Coral), deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(DeckManager.MinDeckSize, deck.DeckCount);
        }

        [Test]
        public void Apply_JokerPiece_AddsJokerIgnoringSubChoice()
        {
            var tokens = new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) };
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            int before = deck.DeckCount;

            bool applied = system.Apply(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice), deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(before + 1, deck.DeckCount);
        }

        [Test]
        public void Apply_GoldenCells_TagsTokensInDeck_NotGridCells()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(2));

            system.Apply(UpgradeCatalog.GoldenCells, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.GoldenCellsCount, CountTagged(deck, PieceTraitKind.Golden));
        }

        [Test]
        public void Apply_TintedCells_TagsTokensWithABaseColor()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(3));

            system.Apply(UpgradeCatalog.TintedCells, default(UpgradeSubChoice), deck);

            int tintedCount = 0;
            foreach (var token in deck.Deck)
            {
                if (token.Trait.HasValue && token.Trait.Value.Kind == PieceTraitKind.Tinted)
                {
                    tintedCount++;
                    Assert.IsTrue(token.Trait.Value.TintedColor.HasValue);
                    Assert.AreNotEqual(PieceColor.Joker, token.Trait.Value.TintedColor.Value);
                }
            }
            Assert.AreEqual(UpgradeSystem.TintedCellsCount, tintedCount);
        }

        [Test]
        public void Apply_MultiplierZone_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(4));

            system.Apply(UpgradeCatalog.MultiplierZone, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.MultiplierZoneCount, CountTagged(deck, PieceTraitKind.Multiplier));
        }

        private static DeckManager MakeTwentyTokenDeck()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < 20; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Sq2, PieceColor.Coral));
            }
            return new DeckManager(tokens, new SystemRandomProvider(1));
        }

        private static int CountTagged(DeckManager deck, PieceTraitKind kind)
        {
            int count = 0;
            foreach (var token in deck.Deck)
            {
                if (token.Trait.HasValue && token.Trait.Value.Kind == kind) count++;
            }
            return count;
        }
    }
}
