using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    public class PieceShapeRotationTests
    {
        [Test]
        public void GetRotated_Deg0_MatchesTheBaseShapeExactly()
        {
            foreach (ShapeId id in PieceShapeCatalog.AllIds)
            {
                var baseShape = PieceShapeCatalog.Get(id);
                var rotated = PieceShapeCatalog.GetRotated(id, PieceRotation.Deg0);

                CollectionAssert.AreEquivalent(baseShape.Cells, rotated.Cells, "Deg0 should be identical to the unrotated base shape for " + id);
            }
        }

        [Test]
        public void GetRotated_PreservesCellCount_ForEveryShapeAndRotation()
        {
            foreach (ShapeId id in PieceShapeCatalog.AllIds)
            {
                int baseCount = PieceShapeCatalog.Get(id).Cells.Count;
                foreach (PieceRotation rotation in new[] { PieceRotation.Deg0, PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
                {
                    var rotated = PieceShapeCatalog.GetRotated(id, rotation);
                    Assert.AreEqual(baseCount, rotated.Cells.Count, id + " at " + rotation + " should keep the same cell count");
                }
            }
        }

        [Test]
        public void GetRotated_PreservesTheOriginalShapeId_RegardlessOfRotation()
        {
            foreach (ShapeId id in PieceShapeCatalog.AllIds)
            {
                foreach (PieceRotation rotation in new[] { PieceRotation.Deg0, PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
                {
                    Assert.AreEqual(id, PieceShapeCatalog.GetRotated(id, rotation).Id);
                }
            }
        }

        [Test]
        public void GetRotated_AlwaysNormalizesToAZeroBasedBoundingBox()
        {
            foreach (ShapeId id in PieceShapeCatalog.AllIds)
            {
                foreach (PieceRotation rotation in new[] { PieceRotation.Deg0, PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
                {
                    var cells = PieceShapeCatalog.GetRotated(id, rotation).Cells;
                    int minX = int.MaxValue;
                    int minY = int.MaxValue;
                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (cells[i].x < minX) minX = cells[i].x;
                        if (cells[i].y < minY) minY = cells[i].y;
                    }
                    Assert.AreEqual(0, minX, id + " at " + rotation + " should have a zero-based bounding box (x)");
                    Assert.AreEqual(0, minY, id + " at " + rotation + " should have a zero-based bounding box (y)");
                }
            }
        }

        // GetRotated_RotatingAHorizontalTromino90Degrees_ProducesTheVerticalTrominosShape
        // and GetRotated_RotatingADomino90Degrees_ProducesTheOtherDominosShape used to
        // live here, proving TriIH/DomH's 90°-rotated cells exactly matched TriIV's/
        // DomV's base cells — which is exactly why TriIV and DomV were removed as
        // separate ShapeIds entirely (see ShapeId's own doc comment): they were
        // redundant with TriIH/DomH under the rotation every piece already gets.

        [Test]
        public void GetRotated_Sq2IsRotationInvariant()
        {
            var deg0 = PieceShapeCatalog.GetRotated(ShapeId.Sq2, PieceRotation.Deg0);
            foreach (PieceRotation rotation in new[] { PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
            {
                var rotated = PieceShapeCatalog.GetRotated(ShapeId.Sq2, rotation);
                CollectionAssert.AreEquivalent(deg0.Cells, rotated.Cells, "A 2x2 square should look identical at every rotation");
            }
        }

        [Test]
        public void GetRotated_SingleIsRotationInvariant()
        {
            var deg0 = PieceShapeCatalog.GetRotated(ShapeId.Single, PieceRotation.Deg0);
            foreach (PieceRotation rotation in new[] { PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
            {
                var rotated = PieceShapeCatalog.GetRotated(ShapeId.Single, rotation);
                CollectionAssert.AreEquivalent(deg0.Cells, rotated.Cells);
            }
        }

        [Test]
        public void GetRotated_STetro_Has180DegreeSymmetry_ButDiffersAt90Degrees()
        {
            var deg0 = PieceShapeCatalog.GetRotated(ShapeId.STetro, PieceRotation.Deg0);
            var deg180 = PieceShapeCatalog.GetRotated(ShapeId.STetro, PieceRotation.Deg180);
            var deg90 = PieceShapeCatalog.GetRotated(ShapeId.STetro, PieceRotation.Deg90);

            CollectionAssert.AreEquivalent(deg0.Cells, deg180.Cells, "S-tetromino has 180-degree rotational symmetry");
            CollectionAssert.AreNotEquivalent(deg0.Cells, deg90.Cells, "S-tetromino's 90-degree rotation should be visually distinct from its 0-degree layout");
        }

        [Test]
        public void GetRotated_NeverProducesAMirroredShape_ForAChiralPiece()
        {
            // A pure rotation (never a reflection) of the S-tetromino must never
            // produce the mirrored Z-tetromino layout. Z's canonical cells (same
            // convention as STetro's base layout) would be {(0,0),(1,0),(1,1),(2,1)}.
            var zShapeCells = new HashSet<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)
            };

            foreach (PieceRotation rotation in new[] { PieceRotation.Deg0, PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270 })
            {
                var cells = new HashSet<Vector2Int>(PieceShapeCatalog.GetRotated(ShapeId.STetro, rotation).Cells);
                Assert.IsFalse(cells.SetEquals(zShapeCells), "Rotating STetro at " + rotation + " should never produce the mirrored Z-shape");
            }
        }
    }
}
