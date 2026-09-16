using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    /// <summary>Covers the first batch of persistent modifiers (see ModifierCatalog) via GridManager.PlacePiece's optional activeModifiers parameter.</summary>
    public class GridManagerModifierTests
    {
        [Test]
        public void PlacePiece_NoActiveModifiers_LeavesModifierBonusAtZero()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Prisme_Fires_WhenGroupHasFourDistinctColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisme };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Joker, 1, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Joker, 3, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Violet, 4, 0, modifiers);

            // Group so far: Coral, Joker, Teal, Joker, Violet — 3 distinct
            // non-joker colors + a joker present, so Prisme's "3 + joker"
            // clause qualifies.
            Assert.AreEqual(ScoringConstants.PrismeBonus, result.ModifierBonus);

            grid.PlacePiece(single, PieceColor.Joker, 5, 0, modifiers);
            var finalWithLime = grid.PlacePiece(single, PieceColor.Lime, 6, 0, modifiers);

            // Now 4 distinct non-joker colors present — still qualifies.
            Assert.AreEqual(ScoringConstants.PrismeBonus, finalWithLime.ModifierBonus);
        }

        [Test]
        public void Prisme_DoesNotFire_WhenFewerThanThreeDistinctColorsOrNoJoker()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisme };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            // 2 distinct colors, no joker: doesn't qualify either clause.
            var result = grid.PlacePiece(single, PieceColor.Teal, 1, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Chaine_FiresOnlyOnceGroupReachesFiveCells()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Chaine };

            for (int x = 0; x < 4; x++)
            {
                var r = grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
                Assert.AreEqual(0, r.ModifierBonus, "Chaine shouldn't fire before the group reaches 5 cells");
            }

            var fifth = grid.PlacePiece(single, PieceColor.Coral, 4, 0, modifiers);
            Assert.AreEqual(ScoringConstants.ChaineBonus, fifth.ModifierBonus);
        }

        [Test]
        public void MegaChaine_FiresBaseBonus_AtExactlyTenCells()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.MegaChaine };

            // A 5x2 block (x=0..4, y=0..1): never fills a whole row (needs all
            // 8 columns) or column (needs all 8 rows), so nothing auto-clears
            // mid-sequence and the group grows cleanly to exactly 10 cells.
            for (int x = 0; x < 5; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            for (int x = 0; x < 4; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 1, modifiers);
            }
            var tenth = grid.PlacePiece(single, PieceColor.Coral, 4, 1, modifiers);

            Assert.AreEqual(ScoringConstants.MegaChaineBaseBonus, tenth.ModifierBonus);
        }

        [Test]
        public void Forteresse_FiresOnlyForFullyEncircledCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Forteresse };

            // Fill all 8 neighbors of (2,2) first, then place the center last so
            // it's part of this placement's own scored group.
            var ring = new[]
            {
                new UnityEngine.Vector2Int(1, 1), new UnityEngine.Vector2Int(2, 1), new UnityEngine.Vector2Int(3, 1),
                new UnityEngine.Vector2Int(1, 2), new UnityEngine.Vector2Int(3, 2),
                new UnityEngine.Vector2Int(1, 3), new UnityEngine.Vector2Int(2, 3), new UnityEngine.Vector2Int(3, 3),
            };
            foreach (var pos in ring)
            {
                grid.PlacePiece(single, PieceColor.Coral, pos.x, pos.y, modifiers);
            }

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.ForteresseBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Prisonnier_FiresForCardinalOnlyEncirclement_WithoutRequiringDiagonals()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisonnier };

            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers); // below
            grid.PlacePiece(single, PieceColor.Coral, 1, 2, modifiers); // left
            grid.PlacePiece(single, PieceColor.Coral, 3, 2, modifiers); // right
            grid.PlacePiece(single, PieceColor.Coral, 2, 3, modifiers); // above

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            // No diagonal neighbor is filled, so this would NOT qualify for
            // Forteresse, but Prisonnier only cares about the 4 cardinals.
            Assert.AreEqual(ScoringConstants.PrisonnierBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Architecte_FiresOnlyForTheSq2Shape()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Architecte };

            var squareResult = grid.PlacePiece(square, PieceColor.Lime, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.ArchitecteBonus, squareResult.ModifierBonus);

            var singleResult = grid.PlacePiece(single, PieceColor.Lime, 3, 3, modifiers);
            Assert.AreEqual(0, singleResult.ModifierBonus);
        }

        [Test]
        public void Puriste_AddsHalfGroupBonus_WhenGroupIsMonochromeExcludingJokers()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2); // 4 cells, alone
            var modifiers = new List<ModifierId> { ModifierId.Puriste };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            int expectedGroupBonus = 4 * ScoringConstants.GroupBonusPerCell;
            Assert.AreEqual(expectedGroupBonus, result.GroupBonus);
            Assert.AreEqual(expectedGroupBonus / 2, result.ModifierBonus);
        }

        [Test]
        public void Puriste_DoesNotFire_WhenGroupHasTwoDifferentNonJokerColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Puriste };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Joker, 1, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Teal, 2, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Collectionneur_ScoresPerDistinctClearedColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Collectionneur };

            // Fill x=0..6 with 3 distinct colors, leaving x=7 to complete the row.
            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 3, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 4, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 5, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 6, 0, modifiers);

            var finalResult = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            Assert.AreEqual(GridManager.Size, finalResult.LineClearCellCount);
            Assert.AreEqual(4 * ScoringConstants.CollectionneurBonusPerColor, finalResult.ModifierBonus);
        }

        [Test]
        public void Collectionneur_DoesNotFire_WhenPlacementClearsNoLine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Collectionneur };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void MultipleActiveModifiers_StackTheirBonusesInTheSamePlacement()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Architecte, ModifierId.Puriste };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            int expectedGroupBonus = 4 * ScoringConstants.GroupBonusPerCell;
            int expectedPuriste = expectedGroupBonus / 2;
            Assert.AreEqual(ScoringConstants.ArchitecteBonus + expectedPuriste, result.ModifierBonus);
        }

        [Test]
        public void ScoreEvents_IncludeModifierEntry_WhenAModifierFires()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Architecte };

            var result = grid.PlacePiece(square, PieceColor.Lime, 0, 0, modifiers);

            bool foundModifierEvent = false;
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.Modifier && e.Amount == ScoringConstants.ArchitecteBonus)
                {
                    foundModifierEvent = true;
                }
            }
            Assert.IsTrue(foundModifierEvent);
        }
    }
}
