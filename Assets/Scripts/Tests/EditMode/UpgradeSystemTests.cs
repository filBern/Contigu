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

        [Test]
        public void Apply_BlastTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(5));

            system.Apply(UpgradeCatalog.BlastTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.BlastTileCount, CountTagged(deck, PieceTraitKind.Blast));
        }

        [Test]
        public void Apply_MultiplierBeacon_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(6));

            system.Apply(UpgradeCatalog.MultiplierBeacon, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.MultiplierBeaconCount, CountTagged(deck, PieceTraitKind.Beacon));
        }

        [Test]
        public void Apply_MirrorTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(7));

            system.Apply(UpgradeCatalog.MirrorTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.MirrorTileCount, CountTagged(deck, PieceTraitKind.Mirror));
        }

        [Test]
        public void Apply_Seeder_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(8));

            system.Apply(UpgradeCatalog.Seeder, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.SeederCount, CountTagged(deck, PieceTraitKind.Seeder));
        }

        [Test]
        public void Apply_CatalystTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(9));

            system.Apply(UpgradeCatalog.CatalystTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.CatalystTileCount, CountTagged(deck, PieceTraitKind.Catalyst));
        }

        [Test]
        public void Apply_DrillerTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(10));

            system.Apply(UpgradeCatalog.DrillerTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.DrillerTileCount, CountTagged(deck, PieceTraitKind.Driller));
        }

        [Test]
        public void Apply_TwinTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(11));

            system.Apply(UpgradeCatalog.TwinTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.TwinTileCount, CountTagged(deck, PieceTraitKind.Twin));
        }

        [Test]
        public void Apply_DetonatorTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(12));

            system.Apply(UpgradeCatalog.DetonatorTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.DetonatorTileCount, CountTagged(deck, PieceTraitKind.Detonator));
        }

        [Test]
        public void Apply_ChameleonTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(13));

            system.Apply(UpgradeCatalog.ChameleonTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.ChameleonTileCount, CountTagged(deck, PieceTraitKind.Chameleon));
        }

        [Test]
        public void Apply_SparkTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(14));

            system.Apply(UpgradeCatalog.SparkTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.SparkTileCount, CountTagged(deck, PieceTraitKind.Spark));
        }

        [Test]
        public void Apply_VoidTile_TagsTokensInDeck()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(15));

            system.Apply(UpgradeCatalog.VoidTile, default(UpgradeSubChoice), deck);

            Assert.AreEqual(UpgradeSystem.VoidTileCount, CountTagged(deck, PieceTraitKind.Void));
        }

        [Test]
        public void RollDraft_OverManySeeds_PicksCommonRarityUpgradesMoreOftenThanRare()
        {
            // Statistical check of the weighting itself (see
            // UpgradeRarityUtility.GetDraftWeight: Common=8, Rare=2, a 4x
            // gap) rather than any single draw — GoldenCells (Common) should
            // come up clearly more often than MirrorTile (Rare) across many
            // rolls, even though both are always eligible for the Grid pick.
            int goldenCount = 0;
            int mirrorCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var draft = system.RollDraft();
                foreach (var option in draft.Options)
                {
                    if (option.Id == UpgradeId.GoldenCells) goldenCount++;
                    if (option.Id == UpgradeId.MirrorTile) mirrorCount++;
                }
            }

            Assert.Greater(goldenCount, mirrorCount, "Common-rarity GoldenCells should be drafted more often than Rare-rarity MirrorTile");
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
