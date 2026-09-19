using System.Collections.Generic;
using System.Linq;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public class DeckManagerTests
    {
        private static DeckManager MakeMinimalDeck()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < DeckManager.MinDeckSize; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Single, PieceColor.Coral));
            }
            return new DeckManager(tokens, new SystemRandomProvider(1));
        }

        private static List<PieceToken> BuildTwelveUniqueTokens()
        {
            var list = new List<PieceToken>();
            foreach (ShapeId shape in System.Enum.GetValues(typeof(ShapeId)))
            {
                list.Add(new PieceToken(shape, PieceColor.Coral));
            }
            list.Add(new PieceToken(ShapeId.Single, PieceColor.Teal));
            list.Add(new PieceToken(ShapeId.DomH, PieceColor.Teal));
            return list; // 10 shapes x Coral + 2 extra Teal = 12 unique tokens
        }

        private static string Key(PieceToken t)
        {
            return t.Shape + ":" + t.Color;
        }

        /// <summary>True when every slot in <paramref name="hand"/> is non-null.</summary>
        private static bool AllSlotsFilled(IReadOnlyList<PieceToken?> hand)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (!hand[i].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        [Test]
        public void InitialDeckFactory_Build_Produces24TokensNoJokers()
        {
            var tokens = InitialDeckFactory.Build();

            Assert.AreEqual(24, tokens.Count);
            Assert.IsFalse(tokens.Exists(t => t.Color == PieceColor.Joker));
        }

        [Test]
        public void Constructor_DrawsInitialHandOfThree()
        {
            var dm = MakeMinimalDeck();

            Assert.AreEqual(DeckManager.HandSize, dm.Hand.Count);
        }

        [Test]
        public void HandRotations_StaysInLockstepWithHand_ThroughConstructionAndPlay()
        {
            var dm = MakeMinimalDeck();

            Assert.AreEqual(dm.Hand.Count, dm.HandRotations.Count);

            dm.PlayFromHand(1); // empties the middle slot in place, not index 0
            Assert.AreEqual(dm.Hand.Count, dm.HandRotations.Count);
            Assert.IsFalse(dm.Hand[1].HasValue, "Slot 1 should be empty, not shifted away");
            Assert.IsTrue(dm.Hand[0].HasValue, "Slots 0 and 2 should be untouched by playing slot 1");
            Assert.IsTrue(dm.Hand[2].HasValue);

            dm.PlayFromHand(0);
            dm.PlayFromHand(2); // every slot now empty -> triggers a fresh DrawNewHand
            Assert.AreEqual(DeckManager.HandSize, dm.Hand.Count);
            Assert.AreEqual(DeckManager.HandSize, dm.HandRotations.Count);
            Assert.IsTrue(AllSlotsFilled(dm.Hand));
        }

        [Test]
        public void DrawNewHand_AssignsAValidRotationToEverySlot()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var dm = new DeckManager(BuildTwelveUniqueTokens(), new SystemRandomProvider(seed));

                foreach (var rotation in dm.HandRotations)
                {
                    Assert.IsTrue(rotation == PieceRotation.Deg0 || rotation == PieceRotation.Deg90
                        || rotation == PieceRotation.Deg180 || rotation == PieceRotation.Deg270);
                }
            }
        }

        [Test]
        public void PlayFromHand_LeavesThatSlotEmpty_WithoutShiftingTheOthers()
        {
            var dm = MakeMinimalDeck();

            dm.PlayFromHand(0);
            Assert.IsFalse(dm.Hand[0].HasValue, "The played slot should stay empty");
            Assert.IsTrue(dm.Hand[1].HasValue, "Slot 1 should NOT shift down into slot 0 (on explicit request)");
            Assert.IsTrue(dm.Hand[2].HasValue);
            Assert.AreEqual(DeckManager.HandSize, dm.Hand.Count, "Hand list itself never shrinks — only individual slots empty out");

            dm.PlayFromHand(1);
            Assert.IsFalse(dm.Hand[1].HasValue);
            Assert.IsTrue(dm.Hand[2].HasValue, "Slot 2 should NOT shift down into slot 1 either");
            Assert.AreEqual(DeckManager.HandSize, dm.Hand.Count);
        }

        [Test]
        public void PlayFromHand_OnlyRefillsWhenHandFullyEmpty()
        {
            var dm = MakeMinimalDeck();

            dm.PlayFromHand(0);
            Assert.IsFalse(dm.IsHandFullyEmpty());

            dm.PlayFromHand(1);
            Assert.IsFalse(dm.IsHandFullyEmpty());

            dm.PlayFromHand(2);
            Assert.IsTrue(AllSlotsFilled(dm.Hand), "Hand should refill only once every slot is empty");
        }

        [Test]
        public void PlayFromHand_WithRefillIfEmptyFalse_LeavesEverySlotEmptyInstead()
        {
            var dm = MakeMinimalDeck();

            dm.PlayFromHand(0);
            dm.PlayFromHand(1);
            dm.PlayFromHand(2, refillIfEmpty: false);

            Assert.IsTrue(dm.IsHandFullyEmpty(), "Caller opted out of the auto-refill, so every slot should stay empty until DrawNewHand is called explicitly");
            Assert.AreEqual(DeckManager.HandSize, dm.Hand.Count, "The list itself still has HandSize entries, just all null");

            dm.DrawNewHand();
            Assert.IsTrue(AllSlotsFilled(dm.Hand));
        }

        [Test]
        public void Draw_IsWithoutReplacement_AcrossOneFullCycle()
        {
            var tokens = BuildTwelveUniqueTokens();
            var dm = new DeckManager(tokens, new SystemRandomProvider(7));

            var order = new List<string>();
            foreach (var t in dm.Hand)
            {
                order.Add(Key(t.Value));
            }

            for (int batch = 0; batch < 3; batch++)
            {
                // Plays each slot by its own index (0, 1, 2) — with slots no
                // longer shifting, replaying index 0 three times would only
                // ever touch slot 0.
                for (int i = 0; i < DeckManager.HandSize; i++)
                {
                    dm.PlayFromHand(i);
                }
                foreach (var t in dm.Hand)
                {
                    order.Add(Key(t.Value));
                }
            }

            Assert.AreEqual(12, order.Count);
            Assert.AreEqual(12, new HashSet<string>(order).Count, "All 12 draws in one cycle should be distinct (no replacement).");
        }

        [Test]
        public void RemoveOneOfType_RespectsMinDeckFloor()
        {
            var dm = MakeMinimalDeck(); // exactly MinDeckSize copies of one type

            Assert.IsFalse(dm.CanRemove(ShapeId.Single, PieceColor.Coral));
            Assert.IsFalse(dm.RemoveOneOfType(ShapeId.Single, PieceColor.Coral));
            Assert.AreEqual(DeckManager.MinDeckSize, dm.DeckCount);
        }

        [Test]
        public void RemoveOneOfType_AboveFloor_Succeeds()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < DeckManager.MinDeckSize + 1; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Single, PieceColor.Coral));
            }
            var dm = new DeckManager(tokens, new SystemRandomProvider(1));

            Assert.IsTrue(dm.RemoveOneOfType(ShapeId.Single, PieceColor.Coral));
            Assert.AreEqual(DeckManager.MinDeckSize, dm.DeckCount);
        }

        [Test]
        public void DuplicateOfType_AddsCopy()
        {
            var dm = MakeMinimalDeck();
            int before = dm.DeckCount;

            Assert.IsTrue(dm.DuplicateOfType(ShapeId.Single, PieceColor.Coral));
            Assert.AreEqual(before + 1, dm.DeckCount);
        }

        [Test]
        public void DuplicateOfType_UnknownType_Fails()
        {
            var dm = MakeMinimalDeck();

            Assert.IsFalse(dm.DuplicateOfType(ShapeId.STetro, PieceColor.Joker));
        }

        [Test]
        public void AddJoker_AddsAJokerColoredTokenOfSomeShape()
        {
            var dm = MakeMinimalDeck();
            int before = dm.DeckCount;

            dm.AddJoker(new SystemRandomProvider(1));

            Assert.AreEqual(before + 1, dm.DeckCount);
            Assert.IsTrue(dm.Deck.Any(t => t.Color == PieceColor.Joker));
        }

        [Test]
        public void AddJoker_OverManySeeds_PicksMoreThanJustSingleShape()
        {
            var shapesSeen = new HashSet<ShapeId>();
            for (int seed = 0; seed < 100; seed++)
            {
                var dm = MakeMinimalDeck();
                dm.AddJoker(new SystemRandomProvider(seed));
                var added = dm.Deck.Last(t => t.Color == PieceColor.Joker);
                shapesSeen.Add(added.Shape);
            }

            Assert.Greater(shapesSeen.Count, 1, "AddJoker should pick a random shape, not always Single");
        }

        [Test]
        public void RecolorOneOfType_ChangesOneCopyColor()
        {
            var dm = MakeMinimalDeck(); // 10x Single/Coral

            Assert.IsTrue(dm.RecolorOneOfType(ShapeId.Single, PieceColor.Coral, PieceColor.Teal));

            var composition = dm.GetDeckComposition();
            Assert.AreEqual(DeckManager.MinDeckSize - 1, composition[(ShapeId.Single, PieceColor.Coral)]);
            Assert.AreEqual(1, composition[(ShapeId.Single, PieceColor.Teal)]);
            Assert.AreEqual(DeckManager.MinDeckSize, dm.DeckCount);
        }

        [Test]
        public void RecolorOneOfType_UnknownSourceType_Fails()
        {
            var dm = MakeMinimalDeck();

            Assert.IsFalse(dm.RecolorOneOfType(ShapeId.STetro, PieceColor.Coral, PieceColor.Teal));
        }

        private static DeckManager MakeTwentyTokenSq2Deck()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < 20; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Sq2, PieceColor.Coral));
            }
            return new DeckManager(tokens, new SystemRandomProvider(1));
        }

        [Test]
        public void TagGoldenTokensRandom_TagsExactlyRequestedCount_WithValidLocalCellIndex()
        {
            var dm = MakeTwentyTokenSq2Deck();
            int cellCount = PieceShapeCatalog.Get(ShapeId.Sq2).Cells.Count;

            var tagged = dm.TagGoldenTokensRandom(3, new SystemRandomProvider(5));

            Assert.AreEqual(3, tagged.Count);
            Assert.AreEqual(3, new HashSet<int>(tagged).Count, "Tagged deck indices should be distinct");
            foreach (int idx in tagged)
            {
                var trait = dm.Deck[idx].Trait;
                Assert.IsTrue(trait.HasValue);
                Assert.AreEqual(PieceTraitKind.Golden, trait.Value.Kind);
                Assert.GreaterOrEqual(trait.Value.LocalCellIndex, 0);
                Assert.Less(trait.Value.LocalCellIndex, cellCount);
            }
        }

        [Test]
        public void TagTintedTokensRandom_AssignsANonJokerBaseColor()
        {
            var dm = MakeTwentyTokenSq2Deck();

            var tagged = dm.TagTintedTokensRandom(2, new SystemRandomProvider(6));

            foreach (int idx in tagged)
            {
                var trait = dm.Deck[idx].Trait.Value;
                Assert.AreEqual(PieceTraitKind.Tinted, trait.Kind);
                Assert.IsTrue(trait.TintedColor.HasValue);
                Assert.AreNotEqual(PieceColor.Joker, trait.TintedColor.Value);
            }
        }

        [Test]
        public void TagRandomTokens_PrefersUntaggedTokens_WhenEnoughAreAvailable()
        {
            var dm = MakeTwentyTokenSq2Deck();
            var rng = new SystemRandomProvider(7);

            var firstBatch = dm.TagGoldenTokensRandom(5, rng);
            var secondBatch = dm.TagMultiplierTokensRandom(5, rng);

            foreach (int idx in secondBatch)
            {
                CollectionAssert.DoesNotContain(firstBatch, idx,
                    "With 20 tokens and only 10 tagged so far, the second tag pass should still find untagged tokens");
            }
        }

        [Test]
        public void TagRandomTokens_FallsBackToAlreadyTaggedTokens_WhenNotEnoughUntaggedRemain()
        {
            var tokens = new List<PieceToken>();
            for (int i = 0; i < DeckManager.MinDeckSize; i++)
            {
                tokens.Add(new PieceToken(ShapeId.Single, PieceColor.Coral));
            }
            var dm = new DeckManager(tokens, new SystemRandomProvider(8));

            dm.TagGoldenTokensRandom(DeckManager.MinDeckSize, new SystemRandomProvider(8));
            var secondPass = dm.TagMultiplierTokensRandom(3, new SystemRandomProvider(9));

            Assert.AreEqual(3, secondPass.Count, "Should still tag the requested count by re-tagging already-golden tokens");
        }

        [Test]
        public void TagBlastBeaconMirrorSeederTokensRandom_EachProducesItsOwnTraitKind()
        {
            var dm = MakeTwentyTokenSq2Deck();
            var rng = new SystemRandomProvider(10);

            var blast = dm.TagBlastTokensRandom(1, rng);
            var beacon = dm.TagBeaconTokensRandom(1, rng);
            var mirror = dm.TagMirrorTokensRandom(1, rng);
            var seeder = dm.TagSeederTokensRandom(1, rng);

            Assert.AreEqual(PieceTraitKind.Blast, dm.Deck[blast[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Beacon, dm.Deck[beacon[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Mirror, dm.Deck[mirror[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Seeder, dm.Deck[seeder[0]].Trait.Value.Kind);
        }

        [Test]
        public void TagCatalystTwinDetonatorChameleonSparkVoidTokensRandom_EachProducesItsOwnTraitKind()
        {
            var dm = MakeTwentyTokenSq2Deck();
            var rng = new SystemRandomProvider(11);

            var catalyst = dm.TagCatalystTokensRandom(1, rng);
            var twin = dm.TagTwinTokensRandom(1, rng);
            var detonator = dm.TagDetonatorTokensRandom(1, rng);
            var chameleon = dm.TagChameleonTokensRandom(1, rng);
            var spark = dm.TagSparkTokensRandom(1, rng);
            var voidTag = dm.TagVoidTokensRandom(1, rng);

            Assert.AreEqual(PieceTraitKind.Catalyst, dm.Deck[catalyst[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Twin, dm.Deck[twin[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Detonator, dm.Deck[detonator[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Chameleon, dm.Deck[chameleon[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Spark, dm.Deck[spark[0]].Trait.Value.Kind);
            Assert.AreEqual(PieceTraitKind.Void, dm.Deck[voidTag[0]].Trait.Value.Kind);
        }
    }
}
