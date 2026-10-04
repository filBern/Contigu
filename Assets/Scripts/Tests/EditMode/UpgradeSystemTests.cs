using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Mastery split out of Bank into its own pool (explicit request: "séparer les mastery upgrades des pieces upgrades pour qu'elles soient leur propre type") — same pool-purity check as the Bank/Grid tests above.</summary>
        [Test]
        public void RollFromPool_Mastery_OnlyReturnsMasteryUpgrades()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = system.RollFromPool(UpgradePool.Mastery);
                Assert.AreEqual(UpgradePool.Mastery, picked.Pool);
            }
        }

        /// <summary>Modifier (Random Modifier + Modifier Upgrade) split out of Bank the same way, right after Mastery (explicit request: "L'upgrade 'upgrade modifier' devrait être dans le type random modifier") — same pool-purity check.</summary>
        [Test]
        public void RollFromPool_Modifier_OnlyReturnsModifierUpgrades()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = system.RollFromPool(UpgradePool.Modifier);
                Assert.AreEqual(UpgradePool.Modifier, picked.Pool);
            }
        }

        /// <summary>Random Modifier lost its old +12 draft-weight override once it got its own small 2-item pool (explicit report: "le type random modifier arrive un peu trop souvent comme upgrade") — both entries are Uncommon, so it should land roughly 50/50 with Modifier Upgrade rather than dominating it.</summary>
        [Test]
        public void RollFromPool_OverManySeeds_PicksRandomModifierAndModifierUpgradeAtComparableRates()
        {
            int randomModifierCount = 0;
            int modifierUpgradeCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var picked = system.RollFromPool(UpgradePool.Modifier);
                if (picked.Id == UpgradeId.RandomModifier) randomModifierCount++;
                if (picked.Id == UpgradeId.ModifierUpgrade) modifierUpgradeCount++;
            }

            Assert.Greater(randomModifierCount, 0);
            Assert.Greater(modifierUpgradeCount, 0);
            int diff = System.Math.Abs(randomModifierCount - modifierUpgradeCount);
            Assert.Less(diff, (randomModifierCount + modifierUpgradeCount) / 2, "Same Uncommon rarity now that the +12 override is gone, so neither should come up roughly twice as often as the other");
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
        public void RollFromPool_OverManySeeds_PicksReplacePieceAndDuplicatePieceAtComparableRates()
        {
            // ReplacePiece (née RemovePiece) used to be dropped to Rare (on
            // explicit report: "L'upgrade 'remove a piece' est beaucoup
            // trop fréquente et surtout chiante en début de partie"), a 4x
            // cut below its Common Bank-pool sibling DuplicatePiece — but
            // it was bumped back up to Common (same push that split
            // Mastery into its own pool), so that gap is gone: both are
            // Common now and should come up at roughly the same rate,
            // neither dominating the other the way the old Rare-vs-Common
            // test checked for.
            int replaceCount = 0;
            int duplicateCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var picked = system.RollFromPool(UpgradePool.Bank);
                if (picked.Id == UpgradeId.ReplacePiece) replaceCount++;
                if (picked.Id == UpgradeId.DuplicatePiece) duplicateCount++;
            }

            Assert.Greater(replaceCount, 0);
            Assert.Greater(duplicateCount, 0);
            int diff = System.Math.Abs(replaceCount - duplicateCount);
            Assert.Less(diff, (replaceCount + duplicateCount) / 2, "Same Common rarity now, so neither should come up roughly twice as often as the other");
        }

        [Test]
        public void Apply_ReplacePiece_DelegatesToDeck_SwappingOneTypeForAnother()
        {
            // Redesign, explicit request: "Les upgrades 'remove' sont
            // vraiment chiante, peux-tu la changer pour un replace?" —
            // Replace swaps a duplicate of one EXISTING type for one copy
            // of another, so the deck's net count never changes (unlike
            // the old Remove, which shrank it).
            var tokens = new List<PieceToken>
            {
                new PieceToken(ShapeId.Single, PieceColor.Coral),
                new PieceToken(ShapeId.Sq2, PieceColor.Teal)
            };
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            bool applied = system.Apply(UpgradeCatalog.ReplacePiece,
                new UpgradeSubChoice(ShapeId.Single, PieceColor.Coral, addShape: ShapeId.Sq2, addColor: PieceColor.Teal),
                deck);

            Assert.IsTrue(applied);
            Assert.AreEqual(2, deck.DeckCount, "Net count should stay the same — one removed, one added");
            Assert.AreEqual(0, deck.Deck.Count(t => t.Matches(ShapeId.Single, PieceColor.Coral)));
            Assert.AreEqual(2, deck.Deck.Count(t => t.Matches(ShapeId.Sq2, PieceColor.Teal)));
        }

        [Test]
        public void Apply_JokerPiece_ReturnsFalse_JokerGoesThroughApplyJokerInstead()
        {
            // Joker is the one Bank upgrade with no sub-choice, so unlike
            // ReplacePiece/DuplicatePiece/RecolorPiece it never actually goes
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
        public void GetCandidateTypesFor_ReplacePiece_NeverExcludesAnyType_EvenAtTheMinDeckFloor()
        {
            // Redesign, explicit request: "Les upgrades 'remove' sont
            // vraiment chiante, peux-tu la changer pour un replace?" —
            // unlike the old Remove (which excluded every type once the
            // deck was down to exactly MinDeckSize, since removing any of
            // them would have dropped below the floor), Replace never
            // shrinks the deck at all, so every type stays a valid first-
            // step candidate regardless. Exactly MinDeckSize tokens: one
            // copy each of all 8 distinct shapes, plus 2 extra copies
            // (Single, DomH).
            var tokens = new List<PieceToken>();
            foreach (var shape in InitialDeckFactory.ShapeOrder)
            {
                tokens.Add(new PieceToken(shape, PieceColor.Coral));
            }
            tokens.Add(new PieceToken(ShapeId.Single, PieceColor.Coral));
            tokens.Add(new PieceToken(ShapeId.DomH, PieceColor.Coral));
            Assert.AreEqual(DeckManager.MinDeckSize, tokens.Count);
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidateTypesFor(UpgradeCatalog.ReplacePiece, deck);

            Assert.AreEqual(EconomyConstants.ShopTileCandidateCount, candidates.Count);
        }

        [Test]
        public void GetReplacementCandidateTypesFor_ExcludesTheChosenRemovalType()
        {
            var tokens = new List<PieceToken>();
            foreach (var shape in InitialDeckFactory.ShapeOrder)
            {
                tokens.Add(new PieceToken(shape, PieceColor.Coral));
            }
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetReplacementCandidateTypesFor(deck, ShapeId.Single, PieceColor.Coral);

            Assert.IsFalse(candidates.Any(c => c.Shape == ShapeId.Single && c.Color == PieceColor.Coral),
                "The type just picked to go away should never be offered as its own replacement");
        }

        [Test]
        public void GetReplacementCandidateTypesFor_FallsBackToTheExcludedType_WhenItsTheOnlyOneLeft()
        {
            // A degenerate (single-type) deck has nothing else to offer —
            // the picker should still return something rather than coming
            // up empty, even if that means falling back to the "replace it
            // with itself" no-op (see DeckManager.ReplaceOneOfType's own
            // doc comment on that case).
            var tokens = new List<PieceToken>
            {
                new PieceToken(ShapeId.Single, PieceColor.Coral),
                new PieceToken(ShapeId.Single, PieceColor.Coral)
            };
            var deck = new DeckManager(tokens, new SystemRandomProvider(1));
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetReplacementCandidateTypesFor(deck, ShapeId.Single, PieceColor.Coral);

            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual(ShapeId.Single, candidates[0].Shape);
            Assert.AreEqual(PieceColor.Coral, candidates[0].Color);
        }

        [Test]
        public void GetCandidatePiecesFor_OtherUpgrade_ReturnsNoCandidates()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidatePiecesFor(UpgradeCatalog.JokerPiece);

            Assert.AreEqual(0, candidates.Count);
        }

        [Test]
        public void GetCandidatePiecesFor_RandomPiece_ReturnsShopTileCandidateCountCandidates()
        {
            var system = new UpgradeSystem(new SystemRandomProvider(1));

            var candidates = system.GetCandidatePiecesFor(UpgradeCatalog.RandomPiece);

            Assert.AreEqual(EconomyConstants.ShopTileCandidateCount, candidates.Count);
            foreach (var candidate in candidates)
            {
                // Never Joker — Random Piece rolls from the same 4 base
                // colors a fresh run's starting deck does, not the Joker
                // wildcard (that's what Joker Piece itself is for).
                Assert.AreNotEqual(PieceColor.Joker, candidate.Color);
            }
        }

        [Test]
        public void GetCandidatePiecesFor_OverManySeeds_GrantsTraitsAtRoughlyTheConfiguredRate()
        {
            // Statistical check (same style as RollFromPool's rarity-
            // weighting test above) of EconomyConstants.
            // RandomPieceTraitChancePercent (25) — over many independent
            // rolls, roughly a quarter of candidates should carry a trait.
            // Loose bounds since this is inherently random, just enough to
            // catch the rate being wired up backwards or not at all (e.g.
            // always/never enchanting).
            int total = 0;
            int withTrait = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var system = new UpgradeSystem(new SystemRandomProvider(seed));
                var candidates = system.GetCandidatePiecesFor(UpgradeCatalog.RandomPiece);
                foreach (var candidate in candidates)
                {
                    total++;
                    if (candidate.Trait.HasValue)
                    {
                        withTrait++;
                    }
                }
            }

            float rate = withTrait / (float)total;
            Assert.Greater(rate, 0.15f, "Roughly a quarter of candidates should carry a trait — this looks too low");
            Assert.Less(rate, 0.35f, "Roughly a quarter of candidates should carry a trait — this looks too high");
        }

        [Test]
        public void GetCandidatePiecesFor_TintedTrait_PinsTheTargetColorToTheCandidatesOwnColor()
        {
            // AlwaysZeroRandomProvider makes EVERY candidate roll a trait
            // (Next(100) == 0 < 25) and always picks Grid-pool index 0 —
            // deterministic enough to assert on Tinted specifically without
            // a seed search, as long as index 0 happens to be Tinted for
            // this particular rarity-weighted ordering. Falls back to
            // asserting nothing (still passing) if it isn't, rather than
            // asserting on the wrong trait kind by accident.
            var system = new UpgradeSystem(new AlwaysZeroRandomProvider());

            var candidates = system.GetCandidatePiecesFor(UpgradeCatalog.RandomPiece);

            foreach (var candidate in candidates)
            {
                Assert.IsTrue(candidate.Trait.HasValue, "AlwaysZeroRandomProvider should make the trait roll hit every time");
                if (candidate.Trait.Value.Kind == PieceTraitKind.Tinted)
                {
                    Assert.AreEqual(candidate.Color, candidate.Trait.Value.TintedColor);
                }
            }
        }

        /// <summary>Same helper RunManagerTests keeps privately for MultCinqRisque — duplicated here rather than shared across test assemblies, matching this codebase's existing per-file convention for small deterministic IRandomProvider stubs.</summary>
        private sealed class AlwaysZeroRandomProvider : IRandomProvider
        {
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }

        private static DeckManager MakeManyDistinctTypesDeck()
        {
            var tokens = new List<PieceToken>();
            foreach (var shape in InitialDeckFactory.ShapeOrder)
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
