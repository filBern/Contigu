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
        public void Prisme_Fires_WhenPlacementTouchesFourDistinctColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisme };

            // Prisme looks at colors touching the placement (itself + its
            // direct neighbors), not colors inside its scored group — a group
            // can never mix real colors (see FindConnectedGroup), so "distinct
            // colors in the group" could never be satisfied otherwise.
            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Joker, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.PrismeMultiplier, center.ModifierMultiplier);
        }

        [Test]
        public void Prisme_Fires_WhenPlacementTouchesThreeDistinctColorsPlusAJoker()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisme };

            grid.PlacePiece(single, PieceColor.Teal, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Joker, 2, 3, modifiers);

            // Center's own color (Teal) matches one of the ring colors instead
            // of introducing a 4th one, so touching colors stay at exactly
            // {Teal, Violet, Lime} + a joker — the "3 distinct + joker" clause,
            // not the "4 distinct" one.
            var center = grid.PlacePiece(single, PieceColor.Teal, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.PrismeMultiplier, center.ModifierMultiplier);
        }

        [Test]
        public void Prisme_DoesNotFire_WhenFewerThanThreeDistinctColorsOrNoJoker()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Prisme };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            // 2 distinct colors touching, no joker: doesn't qualify either clause.
            var result = grid.PlacePiece(single, PieceColor.Teal, 1, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
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
            Assert.AreEqual(ScoringConstants.ArchitecteMultiplier, squareResult.ModifierMultiplier);

            var singleResult = grid.PlacePiece(single, PieceColor.Lime, 3, 3, modifiers);
            Assert.AreEqual(1, singleResult.ModifierMultiplier);
        }

        [Test]
        public void Puriste_AppliesItsMultiplier_WhenGroupIsMonochromeExcludingJokers()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2); // 4 cells, alone
            var modifiers = new List<ModifierId> { ModifierId.Puriste };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            int expectedGroupBonus = 4 * ScoringConstants.GroupBonusPerCell;
            Assert.AreEqual(expectedGroupBonus, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.PuristeMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void Puriste_Fires_ForAnAllJokerGroup()
        {
            // A connected group can never mix two different real colors (see
            // GridManager.FindConnectedGroup: a joker never bridges two
            // different colors together), so Puriste's "not monochrome"
            // rejection can only ever be exercised by a group made entirely of
            // jokers — which still counts as vacuously monochrome.
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Puriste };

            grid.PlacePiece(single, PieceColor.Joker, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Joker, 1, 0, modifiers);

            int expectedGroupBonus = 2 * ScoringConstants.GroupBonusPerCell;
            Assert.AreEqual(expectedGroupBonus, result.GroupBonus);
            Assert.AreEqual(ScoringConstants.PuristeMultiplier, result.ModifierMultiplier);
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
        public void MultipleActiveModifiers_StackTheirMultipliersMultiplicatively()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Architecte, ModifierId.Puriste };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.ArchitecteMultiplier * ScoringConstants.PuristeMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void ScoreEvents_IncludeModifierMultiplierEntry_WhenAModifierFires()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Architecte };

            var result = grid.PlacePiece(square, PieceColor.Lime, 0, 0, modifiers);

            bool foundModifierEvent = false;
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type == ScoreEventType.ModifierMultiplier && e.Amount == ScoringConstants.ArchitecteMultiplier)
                {
                    foundModifierEvent = true;
                }
            }
            Assert.IsTrue(foundModifierEvent);
        }

        [Test]
        public void Tricolore_FiresOnlyWithExactlyThreeDistinctColorsTouchingThePlacement()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Tricolore };

            // 3 ring cells filled (4th side left empty), center reuses one of
            // the ring's colors so it doesn't introduce a 4th.
            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            var withThreeColors = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.TricoloreMultiplier, withThreeColors.ModifierMultiplier);
        }

        [Test]
        public void Tricolore_DoesNotFire_WithTwoOrFourDistinctColorsTouchingThePlacement()
        {
            var twoColorsGrid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Tricolore };

            twoColorsGrid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            var withTwoColors = twoColorsGrid.PlacePiece(single, PieceColor.Teal, 1, 0, modifiers);
            Assert.AreEqual(1, withTwoColors.ModifierMultiplier, "Only 2 distinct colors touching");

            var fourColorsGrid = new GridManager();
            fourColorsGrid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            fourColorsGrid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            fourColorsGrid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            fourColorsGrid.PlacePiece(single, PieceColor.Lime, 2, 3, modifiers);
            var withFourColors = fourColorsGrid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);
            Assert.AreEqual(1, withFourColors.ModifierMultiplier, "4 distinct colors touching no longer qualifies — exactly 3 required");
        }

        [Test]
        public void Complementaire_FiresWhenPlacementTouchesAKnownPair()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Complementaire };

            grid.PlacePiece(single, PieceColor.Violet, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(ScoringConstants.ComplementaireMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void Complementaire_DoesNotFire_WithoutAFullPair()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Complementaire };

            grid.PlacePiece(single, PieceColor.Teal, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
        }

        [Test]
        public void Ilot_FiresForLoneSingleCellGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Ilot };

            var result = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);

            Assert.AreEqual(ScoringConstants.IlotMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void Ilot_DoesNotFire_WhenConnectedToAnotherCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Ilot };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
        }

        [Test]
        public void Couronne_FiresForBorderCellsOnly()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Couronne };

            var edgeResult = grid.PlacePiece(single, PieceColor.Coral, 0, 3, modifiers);
            Assert.AreEqual(ScoringConstants.CouronneBonusPerCell, edgeResult.ModifierBonus);

            var centerResult = grid.PlacePiece(single, PieceColor.Teal, 3, 3, modifiers);
            Assert.AreEqual(0, centerResult.ModifierBonus);
        }

        [Test]
        public void Carrefour_FiresForCellSurroundedByAtLeastTwoDifferentColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Carrefour };

            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Joker, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.CarrefourBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Carrefour_DoesNotFire_WhenAllFourNeighborsShareTheSameColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Carrefour };

            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(0, center.ModifierBonus);
        }

        [Test]
        public void Carrefour_Fires_WhenTwoNeighborColorsDifferFromOwnColor_DespiteOtherNeighborsMatchingOwnColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Carrefour };

            // 2 of the 4 neighbors share the center's own color (Coral) — they
            // must not count toward the "2 different colors" requirement, but
            // the other 2 (Teal, Violet) still qualify it on their own.
            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.CarrefourBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Carrefour_DoesNotFire_WhenOnlyOneNeighborColorDiffersFromOwnColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Carrefour };

            // Raw neighbor colors are {Coral, Teal} — 2 distinct — but 3 of the
            // 4 neighbors share the center's own color (Coral) and must be
            // excluded, leaving only Teal: not enough to qualify.
            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(0, center.ModifierBonus);
        }

        [Test]
        public void Macon_FiresWhenPlacementClearsNoLine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Macon };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.MaconMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void Macon_DoesNotFire_WhenPlacementClearsALine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Macon };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Demolisseur_FiresWhenTwoLinesClearSimultaneously()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Demolisseur };

            // Leave (0,0) as the only empty cell shared by both row 0 and
            // column 0, so the final placement completes both simultaneously.
            for (int x = 1; x < GridManager.Size; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            for (int y = 1; y < GridManager.Size; y++)
            {
                grid.PlacePiece(single, PieceColor.Coral, 0, y, modifiers);
            }

            var finalResult = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            // Row 0 (8 cells) + column 0 (8 cells) share exactly 1 cell (0,0),
            // so 15 distinct cells clear — confirms both lines completed.
            Assert.AreEqual(2 * GridManager.Size - 1, finalResult.LineClearCellCount);
            Assert.AreEqual(ScoringConstants.DemolisseurMultiplierPerLine * ScoringConstants.DemolisseurMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Demolisseur_DoesNotFire_WhenOnlyOneLineClears()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Demolisseur };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void ScoreEvents_TagEachModifierEventWithItsTriggeringModifierId()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Architecte, ModifierId.Puriste };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            ModifierId? architecteTag = null;
            ModifierId? puristeTag = null;
            foreach (var e in result.ScoreEvents)
            {
                if (e.Type != ScoreEventType.ModifierMultiplier)
                {
                    continue;
                }
                if (e.Amount == ScoringConstants.ArchitecteMultiplier)
                {
                    architecteTag = e.TriggeringModifier;
                }
                else
                {
                    puristeTag = e.TriggeringModifier;
                }
            }

            Assert.AreEqual(ModifierId.Architecte, architecteTag);
            Assert.AreEqual(ModifierId.Puriste, puristeTag);
        }

        [Test]
        public void ScoreEvents_NonModifierEvents_LeaveTriggeringModifierNull()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            grid.GetCell(0, 0).IsGolden = true;

            var result = grid.PlacePiece(single, PieceColor.Lime, 0, 0);

            foreach (var e in result.ScoreEvents)
            {
                Assert.IsNull(e.TriggeringModifier);
            }
        }

        // ---- Second batch (16 more modifiers) ----

        [Test]
        public void CercleChromatique_FiresWhenFourCardinalNeighborsCoverAllFourBaseColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.CercleChromatique };

            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.CercleChromatiqueBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void CercleChromatique_DoesNotFire_WhenAJokerNeighborTakesOneOfTheFourSlots()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.CercleChromatique };

            grid.PlacePiece(single, PieceColor.Teal, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Joker, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Teal, 2, 2, modifiers);

            Assert.AreEqual(0, center.ModifierBonus, "A joker consumes a slot without contributing a base color, so only 3 real colors show");
        }

        [Test]
        public void Monochrome_FiresOnlyForAMonochromeGroupWithZeroJokers()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.Monochrome };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(4 * ScoringConstants.MonochromeBonusPerCell, result.ModifierBonus);
        }

        [Test]
        public void Monochrome_DoesNotFire_WhenGroupContainsAJoker()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Monochrome };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Joker, 1, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus, "Puriste would tolerate this joker, but Monochrome requires zero jokers anywhere in the group");
        }

        [Test]
        public void Contraste_FiresPerPlacedCellWithADifferentColoredNeighbor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Contraste };

            grid.PlacePiece(single, PieceColor.Teal, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(ScoringConstants.ContrasteBonusPerCell, result.ModifierBonus);
        }

        [Test]
        public void Contraste_DoesNotFire_WhenNoNeighborDiffersInColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Contraste };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Degrade_FiresOnlyWhenThisPlacementsGroupIsStrictlyBiggerThanThePreviousOneThisRound()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Degrade };

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 5, modifiers);
            Assert.AreEqual(1, first.ModifierMultiplier, "No previous placement this round to compare against yet");

            // Merges with the first cell: group grows from 1 to 2 -> strictly bigger.
            var second = grid.PlacePiece(single, PieceColor.Coral, 1, 5, modifiers);
            Assert.AreEqual(ScoringConstants.DegradeMultiplier, second.ModifierMultiplier);

            // A fresh, unconnected single-cell group (size 1) isn't bigger than the previous placement's 2.
            var third = grid.PlacePiece(single, PieceColor.Teal, 7, 7, modifiers);
            Assert.AreEqual(1, third.ModifierMultiplier);
        }

        [Test]
        public void Degrade_TrackedPreviousGroupSizeResetsAtResetForNewRound()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Degrade };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            grid.ResetForNewRound();

            var afterReset = grid.PlacePiece(single, PieceColor.Teal, 4, 4, modifiers);
            Assert.AreEqual(1, afterReset.ModifierMultiplier, "ResetForNewRound should clear the tracked previous group size");
        }

        [Test]
        public void Emmitouflee_FiresOnlyWhenAllFourDiagonalNeighborsAreFilled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Emmitouflee };

            grid.PlacePiece(single, PieceColor.Coral, 1, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 3, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 3, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 3, 3, modifiers);

            // No cardinal neighbor is filled, so this would NOT qualify for
            // Forteresse/Prisonnier, but Emmitouflée only cares about the 4 diagonals.
            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(ScoringConstants.EmmitoufleeBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Emmitouflee_DoesNotFire_WhenOnlyTheCardinalRingIsFilled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Emmitouflee };

            grid.PlacePiece(single, PieceColor.Coral, 2, 1, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 3, 2, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 2, 3, modifiers);

            var center = grid.PlacePiece(single, PieceColor.Coral, 2, 2, modifiers);

            Assert.AreEqual(0, center.ModifierBonus);
        }

        [Test]
        public void Jardinier_FiresForGroupCellsAdjacentToAGoldenTintedOrMultiplierZoneCell()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Jardinier };
            grid.GetCell(4, 5).IsGolden = true;

            var adjacentResult = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);
            Assert.AreEqual(ScoringConstants.JardinierBonusPerCell, adjacentResult.ModifierBonus);

            var farResult = grid.PlacePiece(single, PieceColor.Teal, 0, 0, modifiers);
            Assert.AreEqual(0, farResult.ModifierBonus);
        }

        [Test]
        public void ArcEnCiel_FiresWhenAClearedLineContainsAllFourBaseColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.ArcEnCiel };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 3, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 4, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 5, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 6, 0, modifiers);
            var finalResult = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            Assert.AreEqual(GridManager.Size, finalResult.LineClearCellCount);
            Assert.AreEqual(ScoringConstants.ArcEnCielMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void ArcEnCiel_DoesNotFire_WhenClearedLineHasOnlyThreeDistinctColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.ArcEnCiel };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Alternance_FiresWhenClearedLineStrictlyAlternatesBetweenTwoColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Alternance };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Coral, PieceColor.Teal,
                PieceColor.Coral, PieceColor.Teal, PieceColor.Coral
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, 7, 0, modifiers);

            Assert.AreEqual(ScoringConstants.AlternanceMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Alternance_DoesNotFire_WhenTwoAdjacentCellsShareAColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Alternance };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Coral, PieceColor.Coral,
                PieceColor.Teal, PieceColor.Coral, PieceColor.Teal
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Coral, 7, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Palindrome_FiresWhenClearedLineReadsTheSameForwardsAndBackwards()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Palindrome };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime,
                PieceColor.Lime, PieceColor.Violet, PieceColor.Teal
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Coral, 7, 0, modifiers);

            Assert.AreEqual(ScoringConstants.PalindromeMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Palindrome_DoesNotFire_WhenSequenceIsNotSymmetric()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Palindrome };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Gradient_FiresWhenNoTwoAdjacentCellsInClearedLineShareAColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Gradient };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime,
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            // First-ever Gradient line this run: permanent counter goes 0 -> 1,
            // so the multiplier is (1 + 1) = x2 (see Gradient_Permanently...
            // below for the "never resets" behavior this now exists for).
            Assert.AreEqual(2, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Gradient_DoesNotFire_WhenTwoAdjacentCellsShareAColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Gradient };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Gradient_PermanentlyGrowsAndNeverResets_EvenAcrossRounds()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Gradient };

            // Before Gradient has ever fired, even a placement that clears
            // nothing is still a no-op multiplier.
            var beforeAnyClear = grid.PlacePiece(single, PieceColor.Coral, 0, 5, modifiers);
            Assert.AreEqual(1, beforeAnyClear.ModifierMultiplier);

            // Clear one Gradient-qualifying line: fires for the first time
            // this run, so the permanent counter goes 0 -> 1 and this SAME
            // placement is already multiplied by (1 + 1) = x2.
            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime,
                PieceColor.Coral, PieceColor.Teal, PieceColor.Violet
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var firstClear = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);
            Assert.AreEqual(2, firstClear.ModifierMultiplier, "First-ever Gradient line: counter 0->1, so x(1+1)");

            // A later placement that clears NOTHING still benefits from the
            // permanently-grown multiplier — no longer a per-placement effect.
            var afterFirstClear = grid.PlacePiece(single, PieceColor.Coral, 1, 5, modifiers);
            Assert.AreEqual(2, afterFirstClear.ModifierMultiplier, "Permanent bonus keeps applying to a placement that clears nothing");

            // Starting a new round must NOT reset the counter (explicit
            // request: "Ne se reset jamais").
            grid.ResetForNewRound();
            var afterNewRound = grid.PlacePiece(single, PieceColor.Violet, 3, 3, modifiers);
            Assert.AreEqual(2, afterNewRound.ModifierMultiplier, "Gradient's permanent bonus survives ResetForNewRound");
        }

        [Test]
        public void Bloc_FiresWhenTheClearedLineIsMadeOnlyOfContiguousRunsOfAtLeastTwo()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Bloc };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Coral, PieceColor.Teal, PieceColor.Teal,
                PieceColor.Violet, PieceColor.Violet, PieceColor.Lime
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            Assert.AreEqual(ScoringConstants.BlocMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void Bloc_DoesNotFire_WhenAnIsolatedSingleCellBreaksTheRuns()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Bloc };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Coral, PieceColor.Teal, PieceColor.Violet,
                PieceColor.Violet, PieceColor.Lime, PieceColor.Lime
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Coral, 7, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier, "The lone Teal cell at index 2 has no matching neighbor on either side");
        }

        [Test]
        public void MonochromeLigne_FiresWhenTheEntireClearedLineIsOneColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.MonochromeLigne };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(ScoringConstants.MonochromeLigneMultiplierPerLine, finalResult.ModifierMultiplier);
        }

        [Test]
        public void MonochromeLigne_DoesNotFire_WhenTheLineHasTwoDifferentColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.MonochromeLigne };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(1, finalResult.ModifierMultiplier);
        }

        // ---- Third batch (basic per-color / per-shape modifiers) ----

        [Test]
        public void DevotionCoral_AppliesX2Multiplier_WhenPlacementColorMatches()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.DevotionCoral };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.DevotionMultiplier, result.ModifierMultiplier, "Devotion is now a genuine xN multiplier (ninth batch), not an additive bonus — Éclat is the +pts version");
        }

        [Test]
        public void DevotionCoral_DoesNotFire_WhenPlacementColorDiffers()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.DevotionCoral };

            var result = grid.PlacePiece(single, PieceColor.Teal, 0, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
        }

        [Test]
        public void DevotionModifiers_EachOnlyFiresForItsOwnColor()
        {
            AssertDevotionFiresOnlyForColor(ModifierId.DevotionCoral, PieceColor.Coral);
            AssertDevotionFiresOnlyForColor(ModifierId.DevotionTeal, PieceColor.Teal);
            AssertDevotionFiresOnlyForColor(ModifierId.DevotionViolet, PieceColor.Violet);
            AssertDevotionFiresOnlyForColor(ModifierId.DevotionLime, PieceColor.Lime);
        }

        private static void AssertDevotionFiresOnlyForColor(ModifierId id, PieceColor matchingColor)
        {
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { id };
            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var grid = new GridManager();
                var result = grid.PlacePiece(single, baseColors[i], 0, 0, modifiers);
                int expected = baseColors[i] == matchingColor ? ScoringConstants.DevotionMultiplier : 1;
                Assert.AreEqual(expected, result.ModifierMultiplier, id + " vs " + baseColors[i]);
            }
        }

        [Test]
        public void FormeSq2_AppliesX2Multiplier_WhenPlacedShapeMatches()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.FormeSq2 };

            var result = grid.PlacePiece(square, PieceColor.Lime, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.FormeSpecialistMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void FormeSq2_DoesNotFire_WhenPlacedShapeDiffers()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.FormeSq2 };

            var result = grid.PlacePiece(single, PieceColor.Lime, 0, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
        }

        [Test]
        public void FormeModifiers_EachOnlyFiresForItsOwnShape()
        {
            AssertFormeFiresOnlyForShape(ModifierId.FormeSingle, ShapeId.Single);
            AssertFormeFiresOnlyForShape(ModifierId.FormeDomH, ShapeId.DomH);
            AssertFormeFiresOnlyForShape(ModifierId.FormeDomV, ShapeId.DomV);
            AssertFormeFiresOnlyForShape(ModifierId.FormeTriL, ShapeId.TriL);
            AssertFormeFiresOnlyForShape(ModifierId.FormeTriIH, ShapeId.TriIH);
            AssertFormeFiresOnlyForShape(ModifierId.FormeTriIV, ShapeId.TriIV);
            AssertFormeFiresOnlyForShape(ModifierId.FormeSq2, ShapeId.Sq2);
            AssertFormeFiresOnlyForShape(ModifierId.FormeLTetro, ShapeId.LTetro);
            AssertFormeFiresOnlyForShape(ModifierId.FormeTTetro, ShapeId.TTetro);
            AssertFormeFiresOnlyForShape(ModifierId.FormeSTetro, ShapeId.STetro);
        }

        private static void AssertFormeFiresOnlyForShape(ModifierId id, ShapeId matchingShape)
        {
            var modifiers = new List<ModifierId> { id };
            var otherShape = matchingShape == ShapeId.Single ? ShapeId.DomH : ShapeId.Single;

            var matchGrid = new GridManager();
            var matchResult = matchGrid.PlacePiece(PieceShapeCatalog.Get(matchingShape), PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.FormeSpecialistMultiplier, matchResult.ModifierMultiplier, id + " should apply its xN multiplier to its own shape");

            var otherGrid = new GridManager();
            var otherResult = otherGrid.PlacePiece(PieceShapeCatalog.Get(otherShape), PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(1, otherResult.ModifierMultiplier, id + " should not fire for shape " + otherShape);
        }

        // ---- Fourth batch (hand-slot, piece-size, per-color-tile bonuses) ----
        // SlotUn/Deux/Trois aren't covered here — GridManager.PlacePiece has no
        // handIndex parameter to evaluate them with, so they're covered in
        // RunManagerTests.cs instead (see RunManager.ApplyHandSlotModifierBonus).

        [Test]
        public void GrandFormat_Fires_WhenPlacedPieceHasAtLeastThreeCells()
        {
            var grid = new GridManager();
            var triL = PieceShapeCatalog.Get(ShapeId.TriL);
            var modifiers = new List<ModifierId> { ModifierId.GrandFormat };

            var result = grid.PlacePiece(triL, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(triL.Cells.Count * ScoringConstants.GrandFormatBonusPerCell, result.ModifierBonus);
        }

        [Test]
        public void GrandFormat_DoesNotFire_WhenPlacedPieceHasFewerThanThreeCells()
        {
            var grid = new GridManager();
            var domino = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.GrandFormat };

            var result = grid.PlacePiece(domino, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void HorsNorme_Fires_WhenPlacedPieceDoesNotHaveExactlyThreeCells()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.HorsNorme };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.HorsNormeBonus, result.ModifierBonus);
        }

        [Test]
        public void HorsNorme_DoesNotFire_WhenPlacedPieceHasExactlyThreeCells()
        {
            var grid = new GridManager();
            var triL = PieceShapeCatalog.Get(ShapeId.TriL);
            var modifiers = new List<ModifierId> { ModifierId.HorsNorme };

            var result = grid.PlacePiece(triL, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void EclatCoral_GivesFlatPerCellBonus_WhenPlacementColorMatches()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.EclatCoral };

            var result = grid.PlacePiece(square, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(square.Cells.Count * ScoringConstants.EclatBonusPerCell, result.ModifierBonus,
                "Éclat should be a flat per-tile bonus, not Devotion's full double");
        }

        [Test]
        public void EclatModifiers_EachOnlyFiresForItsOwnColor()
        {
            AssertEclatFiresOnlyForColor(ModifierId.EclatCoral, PieceColor.Coral);
            AssertEclatFiresOnlyForColor(ModifierId.EclatTeal, PieceColor.Teal);
            AssertEclatFiresOnlyForColor(ModifierId.EclatViolet, PieceColor.Violet);
            AssertEclatFiresOnlyForColor(ModifierId.EclatLime, PieceColor.Lime);
        }

        private static void AssertEclatFiresOnlyForColor(ModifierId id, PieceColor matchingColor)
        {
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { id };
            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var grid = new GridManager();
                var result = grid.PlacePiece(single, baseColors[i], 0, 0, modifiers);
                int expected = baseColors[i] == matchingColor ? ScoringConstants.EclatBonusPerCell : 0;
                Assert.AreEqual(expected, result.ModifierBonus, id + " vs " + baseColors[i]);
            }
        }

        // ---- Fifth batch: 8 new modifiers (on explicit request) ----

        [Test]
        public void Diagonale_FiresForGroupCellsOnEitherMainDiagonal()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Diagonale };

            var onMainDiagonal = grid.PlacePiece(single, PieceColor.Coral, 3, 3, modifiers);
            Assert.AreEqual(ScoringConstants.DiagonaleBonusPerCell, onMainDiagonal.ModifierBonus);

            var onAntiDiagonal = grid.PlacePiece(single, PieceColor.Teal, 5, 2, modifiers); // 5 + 2 == Size - 1
            Assert.AreEqual(ScoringConstants.DiagonaleBonusPerCell, onAntiDiagonal.ModifierBonus);

            var offDiagonal = grid.PlacePiece(single, PieceColor.Violet, 1, 4, modifiers);
            Assert.AreEqual(0, offDiagonal.ModifierBonus);
        }

        [Test]
        public void Nid_FiresForAGroupCellWithExactlyThreeFilledCardinalNeighbors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Teal, 3, 4);
            grid.PlacePiece(single, PieceColor.Violet, 5, 4);
            grid.PlacePiece(single, PieceColor.Lime, 4, 3);
            // (4, 5) left empty — only 3 of the 4 cardinal neighbors are filled.

            var modifiers = new List<ModifierId> { ModifierId.Nid };
            var center = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);

            Assert.AreEqual(ScoringConstants.NidBonusPerCell, center.ModifierBonus);
        }

        [Test]
        public void Nid_DoesNotFire_WhenAllFourOrFewerThanThreeCardinalNeighborsAreFilled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Nid };

            // Zero neighbors filled.
            var isolated = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(0, isolated.ModifierBonus);
        }

        [Test]
        public void Solitaire_FiresOnlyForABrandNewMultiCellGroupThatMergesWithNothing()
        {
            var grid = new GridManager();
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Solitaire };

            var first = grid.PlacePiece(domH, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.SolitaireMultiplier, first.ModifierMultiplier, "Brand new 2-cell group, nothing pre-existing merged in");

            // Adjacent, same color — merges into the existing group, so it's
            // no longer "solitary".
            var second = grid.PlacePiece(domH, PieceColor.Coral, 2, 0, modifiers);
            Assert.AreEqual(1, second.ModifierMultiplier);
        }

        [Test]
        public void Solitaire_DoesNotFire_ForASingleCellPlacement()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Solitaire };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier, "Îlot already covers the size-1 case");
        }

        [Test]
        public void PetitFormat_FiresOnlyForPiecesOfAtMostTwoCells()
        {
            var grid = new GridManager();
            var modifiers = new List<ModifierId> { ModifierId.PetitFormat };

            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var singleResult = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(single.Cells.Count * ScoringConstants.PetitFormatBonusPerCell, singleResult.ModifierBonus);

            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var domResult = grid.PlacePiece(domH, PieceColor.Teal, 2, 0, modifiers);
            Assert.AreEqual(domH.Cells.Count * ScoringConstants.PetitFormatBonusPerCell, domResult.ModifierBonus);

            var triL = PieceShapeCatalog.Get(ShapeId.TriL);
            var triResult = grid.PlacePiece(triL, PieceColor.Violet, 4, 4, modifiers);
            Assert.AreEqual(0, triResult.ModifierBonus, "3-cell piece should not qualify as small format");
        }

        [Test]
        public void Fraicheur_FiresOnlyWhenThisPlacementsColorIsNewToTheBoard()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Fraicheur };

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.FraicheurMultiplier, first.ModifierMultiplier, "First Coral tile on an empty board should be fresh");

            var second = grid.PlacePiece(single, PieceColor.Coral, 5, 5, modifiers);
            Assert.AreEqual(1, second.ModifierMultiplier, "Coral is already on the board, no longer fresh");

            var third = grid.PlacePiece(single, PieceColor.Teal, 7, 7, modifiers);
            Assert.AreEqual(ScoringConstants.FraicheurMultiplier, third.ModifierMultiplier, "Teal is still new to the board");
        }

        [Test]
        public void EspaceLibre_FiresOnlyWhileBoardIsAtMost25PercentFilled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.EspaceLibre };

            var early = grid.PlacePiece(single, PieceColor.Coral, 7, 7, modifiers);
            Assert.AreEqual(ScoringConstants.EspaceLibreMultiplier, early.ModifierMultiplier, "Board is nearly empty, should fire");

            // Fill past the 16-cell (25%) threshold without ever completing a
            // row/column (each of these 3 rows leaves its last column empty).
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < GridManager.Size - 1; x++)
                {
                    grid.PlacePiece(single, PieceColor.Teal, x, y);
                }
            }

            var late = grid.PlacePiece(single, PieceColor.Violet, 0, 3, modifiers);
            Assert.AreEqual(1, late.ModifierMultiplier, "Board should no longer count as open once past the threshold");
        }

        [Test]
        public void Rafale_FiresWhenThisPlacementAndTheImmediatelyPreviousOneBothClearALine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
                grid.PlacePiece(single, PieceColor.Teal, x, 1);
            }

            var first = grid.PlacePiece(single, PieceColor.Coral, 7, 0); // completes row 0
            Assert.Greater(first.LineClearScore, 0, "Sanity check: row 0 should have cleared");

            var modifiers = new List<ModifierId> { ModifierId.Rafale };
            var second = grid.PlacePiece(single, PieceColor.Teal, 7, 1, modifiers); // completes row 1, right after another clear

            Assert.AreEqual(ScoringConstants.RafaleMultiplier, second.ModifierMultiplier);
        }

        [Test]
        public void Rafale_DoesNotFire_WhenThePreviousPlacementDidNotClearALine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 0, 5); // no clear

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Teal, x, 2);
            }
            var modifiers = new List<ModifierId> { ModifierId.Rafale };
            var clearing = grid.PlacePiece(single, PieceColor.Teal, 7, 2, modifiers); // completes row 2

            Assert.Greater(clearing.LineClearScore, 0, "Sanity check: row 2 should have cleared");
            Assert.AreEqual(1, clearing.ModifierMultiplier, "Previous placement didn't clear, so Rafale shouldn't fire");
        }

        // ---- Sixth batch: 11 more modifiers (player-authored brainstorm, on explicit request) ----

        [Test]
        public void Pont_ScoresPerPreExistingGroupBridgedBeyondTheFirst()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            grid.PlacePiece(single, PieceColor.Coral, 2, 0);
            // (0,0) and (2,0) are two separate Coral groups with a gap at (1,0).

            var modifiers = new List<ModifierId> { ModifierId.Pont };
            var bridge = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(ScoringConstants.PontMultiplierPerBridge, bridge.ModifierMultiplier);
        }

        [Test]
        public void Pont_DoesNotFire_WhenTouchingAtMostOnePreExistingGroup()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Pont };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);
            var result = grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);

            Assert.AreEqual(1, result.ModifierMultiplier);
        }

        [Test]
        public void Encerclement_FiresForACornerCellWhoseRemainingNeighborsAreAllFilled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Teal, 1, 0);
            grid.PlacePiece(single, PieceColor.Teal, 0, 1);
            grid.PlacePiece(single, PieceColor.Teal, 1, 1);

            var modifiers = new List<ModifierId> { ModifierId.Encerclement };
            var corner = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.EncerclementBonusPerCell, corner.ModifierBonus);
        }

        [Test]
        public void Encerclement_DoesNotFire_WhenACellHasAnEmptyNeighbor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Encerclement };

            var result = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Boucher_ScoresForAPreExistingTileThisPlacementCausesToBecomeEncircled()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            grid.PlacePiece(single, PieceColor.Teal, 0, 0);
            grid.PlacePiece(single, PieceColor.Teal, 1, 0);
            grid.PlacePiece(single, PieceColor.Teal, 0, 1);
            // (0,0)'s only remaining missing neighbor is (1,1).

            var modifiers = new List<ModifierId> { ModifierId.Boucher };
            var sealing = grid.PlacePiece(single, PieceColor.Coral, 1, 1, modifiers);

            Assert.AreEqual(ScoringConstants.BoucherBonusPerCell, sealing.ModifierBonus);
        }

        [Test]
        public void GrosseFamille_FiresOnlyWhenThisColorFormsExactlyOneGroupOnTheBoard()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.GrosseFamille };

            var solo = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.GrosseFamilleMultiplier, solo.ModifierMultiplier, "Only Coral group on the board");

            // A second, disconnected Coral group elsewhere breaks the "single group" condition.
            var second = grid.PlacePiece(single, PieceColor.Coral, 7, 7, modifiers);
            Assert.AreEqual(1, second.ModifierMultiplier);
        }

        [Test]
        public void Repetition_MultiplierGrowsWithConsecutiveSameShapePlacements()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Repetition };

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(1, first.ModifierMultiplier, "Round's first placement starts no streak yet");

            var second = grid.PlacePiece(single, PieceColor.Teal, 3, 3, modifiers);
            Assert.AreEqual(2, second.ModifierMultiplier, "2nd consecutive Single in a row");

            var third = grid.PlacePiece(single, PieceColor.Violet, 5, 5, modifiers);
            Assert.AreEqual(3, third.ModifierMultiplier, "3rd consecutive Single in a row");

            var broken = grid.PlacePiece(domH, PieceColor.Lime, 0, 6, modifiers);
            Assert.AreEqual(1, broken.ModifierMultiplier, "A different shape breaks and restarts the streak");

            var restarted = grid.PlacePiece(single, PieceColor.Coral, 6, 6, modifiers);
            Assert.AreEqual(1, restarted.ModifierMultiplier, "Streak restarts at 1 (no bonus yet) right after it broke");
        }

        [Test]
        public void Synergie_MultipliesByTheTotalNumberOfModifiersHeld()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var alone = new List<ModifierId> { ModifierId.Synergie };
            var solo = grid.PlacePiece(single, PieceColor.Coral, 0, 0, alone);
            Assert.AreEqual(1, solo.ModifierMultiplier, "Synergie alone counts only itself");

            // Couronne/Encerclement are purely additive (ModifierBonus), never
            // touch ModifierMultiplier, so they can't contaminate this count.
            var withOthers = new List<ModifierId> { ModifierId.Synergie, ModifierId.Couronne, ModifierId.Encerclement };
            var withThree = grid.PlacePiece(single, PieceColor.Teal, 3, 3, withOthers);
            Assert.AreEqual(3, withThree.ModifierMultiplier, "3 modifiers held in total");

            var duplicated = new List<ModifierId> { ModifierId.Synergie, ModifierId.Synergie };
            var stacked = grid.PlacePiece(single, PieceColor.Violet, 5, 5, duplicated);
            Assert.AreEqual(4, stacked.ModifierMultiplier, "Each of the 2 held copies independently multiplies by the count (2), stacking to 4");
        }

        [Test]
        public void Densite_MultiplierGrowsWithHowManyCellsAreFilledOnTheBoard()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Densite };

            var early = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(1, early.ModifierMultiplier, "Only 1 cell filled, well under the 10-cell step");

            // Fill 18 more cells without completing any row/column (3 rows of
            // 6, none reaching the 8-cell width), for 19 filled cells total.
            for (int y = 1; y <= 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    grid.PlacePiece(single, PieceColor.Teal, x, y);
                }
            }

            var late = grid.PlacePiece(single, PieceColor.Violet, 6, 1, modifiers);
            Assert.AreEqual(2, late.ModifierMultiplier, "20 cells filled / 10 per step = x2");
        }

        [Test]
        public void AlternancePieces_FiresOnlyWhenThisPiecesColorDiffersFromThePreviousPlacement()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.AlternancePieces };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0);

            var differentColor = grid.PlacePiece(single, PieceColor.Teal, 3, 3, modifiers);
            Assert.AreEqual(ScoringConstants.AlternancePiecesMultiplier, differentColor.ModifierMultiplier);

            var sameColor = grid.PlacePiece(single, PieceColor.Teal, 5, 5, modifiers);
            Assert.AreEqual(1, sameColor.ModifierMultiplier);
        }

        [Test]
        public void Combo_DoublesTheWholePlacementScore_WhenThePreviousPlacementClearedALine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0);
                grid.PlacePiece(single, PieceColor.Teal, x, 1);
            }
            grid.PlacePiece(single, PieceColor.Coral, 7, 0); // completes row 0

            var modifiers = new List<ModifierId> { ModifierId.Combo };
            var second = grid.PlacePiece(single, PieceColor.Teal, 7, 1, modifiers); // completes row 1, right after another clear

            Assert.AreEqual(ScoringConstants.ComboMultiplierFactor, second.ComboMultiplier);
            int baseSubtotal = (second.GroupBonus + second.GoldenBonus) * second.GroupMultiplier
                + second.LineClearScore * second.LineClearMultiplier + second.ModifierBonus + second.TraitBonus;
            Assert.AreEqual(baseSubtotal * ScoringConstants.ComboMultiplierFactor, second.TotalScore);
        }

        [Test]
        public void Combo_DoesNotMultiply_WhenThePreviousPlacementDidNotClearALine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Combo };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(1, result.ComboMultiplier);
        }

        [Test]
        public void Precision_DoesNotFire_WhenAnyPlacedCellHasNoPreExistingNeighbor()
        {
            var grid = new GridManager();
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Precision };

            var isolated = grid.PlacePiece(domH, PieceColor.Coral, 3, 3, modifiers);

            Assert.AreEqual(0, isolated.ModifierBonus);
        }

        [Test]
        public void Precision_Fires_WhenEveryCellOfTheDominoTouchesAPreExistingTile()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Precision };

            grid.PlacePiece(single, PieceColor.Teal, 3, 2);
            grid.PlacePiece(single, PieceColor.Violet, 4, 2);

            var result = grid.PlacePiece(domH, PieceColor.Coral, 3, 3, modifiers);

            Assert.AreEqual(domH.Cells.Count * ScoringConstants.PrecisionBonusPerCell, result.ModifierBonus);
        }

        [Test]
        public void Surpopulation_FiresOnlyWhenEveryPlacedCellTouchesAtLeastTwoPreExistingTiles()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Surpopulation };

            grid.PlacePiece(single, PieceColor.Teal, 3, 4);
            grid.PlacePiece(single, PieceColor.Violet, 4, 3);
            grid.PlacePiece(single, PieceColor.Lime, 4, 5);

            var result = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);

            Assert.AreEqual(ScoringConstants.SurpopulationBonusPerCell, result.ModifierBonus);
        }

        [Test]
        public void Surpopulation_DoesNotFire_WhenOnlyOnePreExistingNeighbor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Surpopulation };

            grid.PlacePiece(single, PieceColor.Teal, 3, 4);
            var result = grid.PlacePiece(single, PieceColor.Coral, 4, 4, modifiers);

            Assert.AreEqual(0, result.ModifierBonus);
        }

        [Test]
        public void Minimaliste_FiresWhenTheWholePieceTouchesExactlyOnePreExistingTile()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Minimaliste };

            grid.PlacePiece(single, PieceColor.Teal, 3, 2);
            var result = grid.PlacePiece(domH, PieceColor.Coral, 3, 3, modifiers);

            Assert.AreEqual(ScoringConstants.MinimalisteMultiplier, result.ModifierMultiplier);
        }

        [Test]
        public void Minimaliste_DoesNotFire_WhenTouchingZeroPreExistingTiles()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Minimaliste };

            var zero = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(1, zero.ModifierMultiplier, "Zero neighbors is Îlot's territory, not Minimaliste's");
        }

        [Test]
        public void Joker_ResolvesAJokerPieceToWhicheverColorMaximizesDevotionOrEclat()
        {
            var grid = new GridManager();
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.Joker, ModifierId.DevotionCoral, ModifierId.EclatLime };

            var result = grid.PlacePiece(domH, PieceColor.Joker, 0, 0, modifiers);

            // Devotion(Coral) would only add the group bonus (2 cells); Éclat(Lime)
            // adds group-size * EclatBonusPerCell (2*4=8) — Lime wins, so only
            // Éclat actually fires, not Devotion.
            int devotionWouldGive = domH.Cells.Count * ScoringConstants.GroupBonusPerCell;
            int eclatWouldGive = domH.Cells.Count * ScoringConstants.EclatBonusPerCell;
            Assert.Greater(eclatWouldGive, devotionWouldGive, "Test setup sanity: Éclat should be the bigger prize here");
            Assert.AreEqual(eclatWouldGive, result.ModifierBonus);
        }

        [Test]
        public void Joker_DoesNotResolve_WhenTheModifierIsNotHeld()
        {
            var grid = new GridManager();
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.DevotionCoral, ModifierId.EclatLime };

            var result = grid.PlacePiece(domH, PieceColor.Joker, 0, 0, modifiers);

            Assert.AreEqual(0, result.ModifierBonus, "Without the Joker modifier, a Joker piece can't match any specific color");
        }

        // ---- Eighth batch: Lueur-earning modifiers, each adapted from an
        // existing score modifier above (same trigger condition, same board
        // setups reused here) but paying PlacementResult.ModifierLueurBonus
        // instead of ModifierBonus/ModifierMultiplier — see ModifierId's
        // eighth batch.

        [Test]
        public void ArcEnCielLueur_EarnsLueurWhenAClearedLineContainsAllFourBaseColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.ArcEnCielLueur };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Coral, 1, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 2, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 3, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 4, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Violet, 5, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Lime, 6, 0, modifiers);
            var finalResult = grid.PlacePiece(single, PieceColor.Lime, 7, 0, modifiers);

            Assert.AreEqual(GridManager.Size, finalResult.LineClearCellCount);
            Assert.AreEqual(EconomyConstants.ArcEnCielLueurPerLine, finalResult.ModifierLueurBonus);
            Assert.AreEqual(1, finalResult.ModifierMultiplier, "Pays Lueur, not a score multiplier");
        }

        [Test]
        public void ArcEnCielLueur_DoesNotFire_WhenClearedLineHasOnlyThreeDistinctColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.ArcEnCielLueur };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(0, finalResult.ModifierLueurBonus);
        }

        [Test]
        public void AlternanceLueur_EarnsLueurWhenClearedLineStrictlyAlternatesBetweenTwoColors()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.AlternanceLueur };

            var pattern = new[]
            {
                PieceColor.Coral, PieceColor.Teal, PieceColor.Coral, PieceColor.Teal,
                PieceColor.Coral, PieceColor.Teal, PieceColor.Coral
            };
            for (int x = 0; x < pattern.Length; x++)
            {
                grid.PlacePiece(single, pattern[x], x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Teal, 7, 0, modifiers);

            Assert.AreEqual(EconomyConstants.AlternanceLueurPerLine, finalResult.ModifierLueurBonus);
        }

        [Test]
        public void MonochromeLigneLueur_EarnsLueurWhenTheEntireClearedLineIsOneColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.MonochromeLigneLueur };

            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                grid.PlacePiece(single, PieceColor.Coral, x, 0, modifiers);
            }
            var finalResult = grid.PlacePiece(single, PieceColor.Coral, GridManager.Size - 1, 0, modifiers);

            Assert.AreEqual(EconomyConstants.MonochromeLigneLueurPerLine, finalResult.ModifierLueurBonus);
        }

        [Test]
        public void CollectionneurLueur_EarnsLueurPerDistinctClearedColor()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.CollectionneurLueur };

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
            Assert.AreEqual(4 * EconomyConstants.CollectionneurLueurPerColor, finalResult.ModifierLueurBonus);
        }

        [Test]
        public void CollectionneurLueur_DoesNotFire_WhenPlacementClearsNoLine()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.CollectionneurLueur };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(0, result.ModifierLueurBonus);
        }

        [Test]
        public void RepetitionLueur_EarnsAFlatBonusOnEachConsecutiveSameShapePlacement()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var domH = PieceShapeCatalog.Get(ShapeId.DomH);
            var modifiers = new List<ModifierId> { ModifierId.RepetitionLueur };

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(0, first.ModifierLueurBonus, "Round's first placement starts no streak yet");

            var second = grid.PlacePiece(single, PieceColor.Teal, 3, 3, modifiers);
            Assert.AreEqual(EconomyConstants.RepetitionLueurBonus, second.ModifierLueurBonus, "2nd consecutive Single in a row");

            var third = grid.PlacePiece(single, PieceColor.Violet, 5, 5, modifiers);
            Assert.AreEqual(EconomyConstants.RepetitionLueurBonus, third.ModifierLueurBonus, "3rd consecutive Single in a row too — flat, not progressive like Repetition's own score version");

            var broken = grid.PlacePiece(domH, PieceColor.Lime, 0, 6, modifiers);
            Assert.AreEqual(0, broken.ModifierLueurBonus, "A different shape breaks the streak");
        }

        // ---- Ninth batch (on explicit request: "+1 mult, +2 mult et +4
        // mult", per-shape "+pts" siblings for the Forme* multipliers,
        // "+1 mult chaque modifier possédé", and "+100pts, réduit de 5 a
        // chaque coup") — see ModifierId's ninth batch and
        // PlacementResult.AdditiveMultBonus.

        [Test]
        public void MultUnDeuxQuatre_EachAddsItsOwnFlatAmountToAdditiveMultBonus()
        {
            var single = PieceShapeCatalog.Get(ShapeId.Single);

            var gridUn = new GridManager();
            var resultUn = gridUn.PlacePiece(single, PieceColor.Coral, 0, 0, new List<ModifierId> { ModifierId.MultUn });
            Assert.AreEqual(ScoringConstants.MultUnBonus, resultUn.AdditiveMultBonus);

            var gridDeux = new GridManager();
            var resultDeux = gridDeux.PlacePiece(single, PieceColor.Coral, 0, 0, new List<ModifierId> { ModifierId.MultDeux });
            Assert.AreEqual(ScoringConstants.MultDeuxBonus, resultDeux.AdditiveMultBonus);

            var gridQuatre = new GridManager();
            var resultQuatre = gridQuatre.PlacePiece(single, PieceColor.Coral, 0, 0, new List<ModifierId> { ModifierId.MultQuatre });
            Assert.AreEqual(ScoringConstants.MultQuatreBonus, resultQuatre.AdditiveMultBonus);
        }

        [Test]
        public void MultModifiers_StackAdditively_WhenHeldTogether()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.MultUn, ModifierId.MultDeux, ModifierId.MultQuatre };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(ScoringConstants.MultUnBonus + ScoringConstants.MultDeuxBonus + ScoringConstants.MultQuatreBonus, result.AdditiveMultBonus);
            Assert.AreEqual(1 + result.AdditiveMultBonus, result.Mult);
        }

        [Test]
        public void FormeSq2Points_GivesFlatPerCellBonus_WhenPlacedShapeMatches()
        {
            var grid = new GridManager();
            var square = PieceShapeCatalog.Get(ShapeId.Sq2);
            var modifiers = new List<ModifierId> { ModifierId.FormeSq2Points };

            var result = grid.PlacePiece(square, PieceColor.Lime, 0, 0, modifiers);

            Assert.AreEqual(square.Cells.Count * ScoringConstants.FormeGlowBonusPerCell, result.ModifierBonus,
                "Forme*Points should be a flat per-tile bonus (the +pts sibling of the Forme* xN multiplier), not a multiplier itself");
        }

        [Test]
        public void FormePointsModifiers_EachOnlyFiresForItsOwnShape()
        {
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeSinglePoints, ShapeId.Single);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeDomHPoints, ShapeId.DomH);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeDomVPoints, ShapeId.DomV);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeTriLPoints, ShapeId.TriL);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeTriIHPoints, ShapeId.TriIH);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeTriIVPoints, ShapeId.TriIV);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeSq2Points, ShapeId.Sq2);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeLTetroPoints, ShapeId.LTetro);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeTTetroPoints, ShapeId.TTetro);
            AssertFormePointsFiresOnlyForShape(ModifierId.FormeSTetroPoints, ShapeId.STetro);
        }

        private static void AssertFormePointsFiresOnlyForShape(ModifierId id, ShapeId matchingShape)
        {
            var modifiers = new List<ModifierId> { id };
            var otherShape = matchingShape == ShapeId.Single ? ShapeId.DomH : ShapeId.Single;

            var matchGrid = new GridManager();
            var matchShape = PieceShapeCatalog.Get(matchingShape);
            var matchResult = matchGrid.PlacePiece(matchShape, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(matchShape.Cells.Count * ScoringConstants.FormeGlowBonusPerCell, matchResult.ModifierBonus, id + " should give its flat per-cell bonus for its own shape");

            var otherGrid = new GridManager();
            var otherResult = otherGrid.PlacePiece(PieceShapeCatalog.Get(otherShape), PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(0, otherResult.ModifierBonus, id + " should not fire for shape " + otherShape);
        }

        [Test]
        public void Solidarite_AddsMultEqualToTotalModifierCountHeld()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Solidarite, ModifierId.MultUn, ModifierId.MultDeux };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            // Solidarite adds Mult equal to activeModifiers.Count (3 held here,
            // itself included) on top of MultUn/MultDeux's own contributions.
            Assert.AreEqual(3 + ScoringConstants.MultUnBonus + ScoringConstants.MultDeuxBonus, result.AdditiveMultBonus);
        }

        [Test]
        public void Solidarite_CountsEachDuplicateCopySeparately()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Solidarite, ModifierId.Solidarite };

            var result = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);

            Assert.AreEqual(4, result.AdditiveMultBonus, "Each of the 2 Solidarite copies adds Mult equal to the full 2-modifier count");
        }

        [Test]
        public void Epuisement_StartsAtFullBonusAndDecaysByAFixedAmountEachPlacement()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Epuisement };

            var first = grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            Assert.AreEqual(ScoringConstants.EpuisementStartingBonus, first.ModifierBonus);

            var second = grid.PlacePiece(single, PieceColor.Teal, 1, 0, modifiers);
            Assert.AreEqual(ScoringConstants.EpuisementStartingBonus - ScoringConstants.EpuisementDecayPerPlacement, second.ModifierBonus);

            var third = grid.PlacePiece(single, PieceColor.Violet, 2, 0, modifiers);
            Assert.AreEqual(ScoringConstants.EpuisementStartingBonus - 2 * ScoringConstants.EpuisementDecayPerPlacement, third.ModifierBonus);
        }

        [Test]
        public void Epuisement_FloorsAtZero_AndNeverEmitsAZeroScoreEvent()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Epuisement };

            int placements = ScoringConstants.EpuisementStartingBonus / ScoringConstants.EpuisementDecayPerPlacement + 2;
            PlacementResult last = null;
            for (int i = 0; i < placements; i++)
            {
                int x = i % GridManager.Size;
                int y = (i / GridManager.Size) * 2;
                last = grid.PlacePiece(single, PieceColor.Coral, x, y, modifiers);
            }

            Assert.AreEqual(0, last.ModifierBonus, "Epuisement floors at 0 and stays there");
        }

        [Test]
        public void Epuisement_ResetsToStartingBonus_OnResetForNewRound()
        {
            var grid = new GridManager();
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var modifiers = new List<ModifierId> { ModifierId.Epuisement };

            grid.PlacePiece(single, PieceColor.Coral, 0, 0, modifiers);
            grid.PlacePiece(single, PieceColor.Teal, 1, 0, modifiers);

            grid.ResetForNewRound();
            var afterReset = grid.PlacePiece(single, PieceColor.Violet, 2, 0, modifiers);

            Assert.AreEqual(ScoringConstants.EpuisementStartingBonus, afterReset.ModifierBonus, "A fresh round-long burst, unlike Gradient's permanent counter");
        }
    }
}
