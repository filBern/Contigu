using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class UpgradeSystemTests
    {
        [Test]
        public void RollFromPool_Bank_OnlyReturnsBankUpgrades()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = system.RollFromPool(UpgradePool.Bank);
                Assert.AreEqual(UpgradePool.Bank, picked.Pool);
            }
        }

        [Test]
        public void RollFromPool_Grid_OnlyReturnsGridUpgrades()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = system.RollFromPool(UpgradePool.Grid);
                Assert.AreEqual(UpgradePool.Grid, picked.Pool);
            }
        }

        [Test]
        public void RollFromPool_OverManySeeds_PicksCommonRarityUpgradesMoreOftenThanRare()
        {
            // Statistical check of the weighting itself (see
            // UpgradeRarityUtility.GetDraftWeight: Common=8, Rare=2, a 4x
            // gap) rather than any single draw — GoldenCells (Common) should
            // come up clearly more often than VoidTile (Rare).
            int goldenCount = 0;
            int voidCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var picked = system.RollFromPool(UpgradePool.Grid);
                if (picked.Id == UpgradeId.GoldenCells) goldenCount++;
                if (picked.Id == UpgradeId.VoidTile) voidCount++;
            }

            Assert.Greater(goldenCount, voidCount, "Common-rarity GoldenCells should come up more often than Rare-rarity VoidTile");
        }

        [Test]
        public void RollFromPool_OverManySeeds_PicksRemovePieceFarLessOftenThanDuplicatePiece()
        {
            // RemovePiece was dropped from Common to Rare (on explicit
            // report: "L'upgrade 'remove a piece' est beaucoup trop
            // fréquente et surtout chiante en début de partie") — a 4x cut
            // in its draft weight relative to its still-Common Bank-pool
            // sibling DuplicatePiece.
            int removeCount = 0;
            int duplicateCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var picked = system.RollFromPool(UpgradePool.Bank);
                if (picked.Id == UpgradeId.RemovePiece) removeCount++;
                if (picked.Id == UpgradeId.DuplicatePiece) duplicateCount++;
            }

            Assert.Greater(duplicateCount, removeCount, "Common-rarity DuplicatePiece should come up more often than Rare-rarity RemovePiece");
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
        public void Apply_JokerPiece_ReturnsFalse_JokerGoesThroughApplyJokerInstead()
        {
            // Joker is the one Bank upgrade with no sub-choice, so unlike
            // RemovePiece/DuplicatePiece/RecolorPiece it never actually goes
            // through Apply in production (see RunManager.BuyUpgradeSlot) —
            // ApplyJoker below is its real path.
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            bool applied = system.Apply(UpgradeCatalog.JokerPiece, default(UpgradeSubChoice), deck);

            Assert.IsFalse(applied);
        }

        [Test]
        public void ApplyJoker_AddsAJokerTokenAndReturnsItsShape()
        {
            var tokens = new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Coral) };
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            int before = deck.DeckCount;

            ShapeId addedShape = system.ApplyJoker(deck);

            Assert.AreEqual(before + 1, deck.DeckCount);
            var addedToken = deck.Deck[deck.Deck.Count - 1];
            Assert.AreEqual(addedShape, addedToken.Shape);
            Assert.AreEqual(PieceColor.Joker, addedToken.Color);
        }

        [Test]
        public void Apply_GridPoolUpgrade_ReturnsFalse_NeverTagsAnything()
        {
            // Grid-pool upgrades no longer go through Apply at all — the shop
            // always resolves them via GetCandidateTilesFor/ApplyToChosenTiles
            // instead, since the player picks which tokens get the trait.
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(2));

            bool applied = system.Apply(UpgradeCatalog.GoldenCells, default(UpgradeSubChoice), deck);

            Assert.IsFalse(applied);
            Assert.AreEqual(0, CountTagged(deck, PieceTraitKind.Golden));
        }

        [Test]
        public void GetCandidateTilesFor_BankUpgrade_ReturnsNoCandidates()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTilesFor(UpgradeCatalog.JokerPiece, deck);

            Assert.AreEqual(0, candidates.Count);
        }

        [Test]
        public void GetCandidateTilesFor_GridUpgrade_ReturnsUpToShopTileCandidateCount()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTilesFor(UpgradeCatalog.GoldenCells, deck);

            Assert.AreEqual(EconomyConstants.ShopTileCandidateCount, candidates.Count);
            Assert.AreEqual(candidates.Count, new HashSet<int>(candidates).Count, "Candidates should be distinct deck indices");
        }

        [Test]
        public void GetCandidateTilesFor_Tinted_ExcludesJokerTokens()
        {
            var tokens = new List<PieceToken> { new PieceToken(ShapeId.Single, PieceColor.Joker) };
            for (int i = 0; i < 10; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Sq2, PieceColor.Coral));
            }
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTilesFor(UpgradeCatalog.TintedCells, deck);

            foreach (var index in candidates)
            {
                Assert.AreNotEqual(PieceColor.Joker, deck.Deck[index].Color,
                    "A Joker token's stored color never resolves to a real one, so Tinted could never fire on it either way");
            }
        }

        [Test]
        public void GetCandidateTypesFor_GridUpgrade_ReturnsNoCandidates()
        {
            var deck = MakeManyDistinctTypesDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTypesFor(UpgradeCatalog.GoldenCells, deck);

            Assert.AreEqual(0, candidates.Count);
        }

        [Test]
        public void GetCandidateTypesFor_JokerUpgrade_ReturnsNoCandidates()
        {
            // Joker is the one Bank upgrade with no sub-choice at all.
            var deck = MakeManyDistinctTypesDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTypesFor(UpgradeCatalog.JokerPiece, deck);

            Assert.AreEqual(0, candidates.Count);
        }

        [Test]
        public void GetCandidateTypesFor_DuplicatePiece_CapsAtShopTileCandidateCountAndStaysDistinct()
        {
            var deck = MakeManyDistinctTypesDeck(); // 8 distinct types, well over the cap
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTypesFor(UpgradeCatalog.DuplicatePiece, deck);

            Assert.AreEqual(EconomyConstants.ShopTileCandidateCount, candidates.Count);
            Assert.AreEqual(candidates.Count, new HashSet<(ShapeId, PieceColor)>(candidates).Count, "Candidates should be distinct types");
        }

        [Test]
        public void GetCandidateTypesFor_RemovePiece_ExcludesTypesTheDeckCantActuallyRemove()
        {
            // Exactly MinDeckSize, one copy of each of 10 distinct types —
            // CanRemove is false for every one of them (removing any copy
            // would drop the deck below its floor), so none should qualify.
            var tokens = new List<PieceToken>();
            var allShapes = new[]
            {
                ShapeId.Single, ShapeId.DomH, ShapeId.DomV, ShapeId.TriL, ShapeId.TriIH,
                ShapeId.TriIV, ShapeId.Sq2, ShapeId.LTetro, ShapeId.TTetro, ShapeId.STetro
            };
            foreach (var shape in allShapes)
            {
                tokens.Add(new PieceToken(shape, PieceColor.Coral));
            }
            Assert.AreEqual(DeckManager.MinDeckSize, tokens.Count);
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTypesFor(UpgradeCatalog.RemovePiece, deck);

            Assert.AreEqual(0, candidates.Count);
        }

        private static DeckManager MakeManyDistinctTypesDeck()
        {
            var tokens = new List<PieceToken>();
            var shapes = new[]
            {
                ShapeId.Single, ShapeId.DomH, ShapeId.DomV, ShapeId.TriL,
                ShapeId.TriIH, ShapeId.TriIV, ShapeId.Sq2, ShapeId.LTetro
            };
            foreach (var shape in shapes)
            {
                for (int i = 0; i < 3; i++)
                {
                    tokens.Add(new PieceToken(shape, PieceColor.Coral));
                }
            }
            return new DeckManager(tokens, new SystemRandomProvider(1));
        }

        [Test]
        public void ApplyToChosenTiles_TagsExactlyTheGivenIndices()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            var candidates = system.GetCandidateTilesFor(UpgradeCatalog.GoldenCells, deck);
            var chosen = new List<int> { candidates[0], candidates[1] };

            bool applied = system.ApplyToChosenTiles(UpgradeCatalog.GoldenCells, chosen, deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(2, CountTagged(deck, PieceTraitKind.Golden));
            foreach (var index in chosen)
            {
                Assert.AreEqual(PieceTraitKind.Golden, deck.Deck[index].Trait.Value.Kind);
            }
        }

        [Test]
        public void ApplyToChosenTiles_Tinted_UsesTheTokensOwnColor()
        {
            var deck = MakeTwentyTokenDeck(); // all Coral
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            var candidates = system.GetCandidateTilesFor(UpgradeCatalog.TintedCells, deck);
            var chosen = new List<int> { candidates[0] };

            system.ApplyToChosenTiles(UpgradeCatalog.TintedCells, chosen, deck);

            var trait = deck.Deck[chosen[0]].Trait.Value;
            Assert.AreEqual(PieceTraitKind.Tinted, trait.Kind);
            Assert.AreEqual(PieceColor.Coral, trait.TintedColor.Value);
        }

        [Test]
        public void ApplyToChosenTiles_BankUpgrade_ReturnsFalse_NeverTagsAnything()
        {
            var deck = MakeTwentyTokenDeck();
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            bool applied = system.ApplyToChosenTiles(UpgradeCatalog.JokerPiece, new List<int> { 0 }, deck);

            Assert.IsFalse(applied);
            Assert.AreEqual(0, CountTagged(deck, PieceTraitKind.Golden));
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
