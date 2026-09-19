using System;
using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// A polyomino shape: an id plus the set of relative (dx, dy) cells it occupies
    /// when anchored at (0, 0). Immutable, shared instances handed out by
    /// <see cref="PieceShapeCatalog"/>.
    /// </summary>
    public sealed class PieceShape
    {
        public ShapeId Id { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }

        internal PieceShape(ShapeId id, Vector2Int[] cells)
        {
            Id = id;
            Cells = Array.AsReadOnly(cells);
        }
    }

    /// <summary>
    /// Static registry of the 10 fixed shapes described in spec section 4.3,
    /// plus every shape pre-rotated at each of the 4 quarter-turns
    /// (<see cref="GetRotated"/>) for the random per-hand-slot rotation feature.
    /// </summary>
    public static class PieceShapeCatalog
    {
        private static readonly PieceRotation[] AllRotations =
        {
            PieceRotation.Deg0, PieceRotation.Deg90, PieceRotation.Deg180, PieceRotation.Deg270
        };

        private static readonly Dictionary<ShapeId, PieceShape> Shapes = BuildShapes();
        private static readonly Dictionary<(ShapeId, PieceRotation), PieceShape> RotatedShapes = BuildRotatedShapes();

        private static Dictionary<ShapeId, PieceShape> BuildShapes()
        {
            var shapes = new Dictionary<ShapeId, PieceShape>();

            shapes[ShapeId.Single] = new PieceShape(ShapeId.Single, new[]
            {
                new Vector2Int(0, 0)
            });

            shapes[ShapeId.DomH] = new PieceShape(ShapeId.DomH, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0)
            });

            shapes[ShapeId.DomV] = new PieceShape(ShapeId.DomV, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1)
            });

            shapes[ShapeId.TriL] = new PieceShape(ShapeId.TriL, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)
            });

            shapes[ShapeId.TriIH] = new PieceShape(ShapeId.TriIH, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)
            });

            shapes[ShapeId.TriIV] = new PieceShape(ShapeId.TriIV, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)
            });

            shapes[ShapeId.Sq2] = new PieceShape(ShapeId.Sq2, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
            });

            shapes[ShapeId.LTetro] = new PieceShape(ShapeId.LTetro, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2)
            });

            shapes[ShapeId.TTetro] = new PieceShape(ShapeId.TTetro, new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)
            });

            shapes[ShapeId.STetro] = new PieceShape(ShapeId.STetro, new[]
            {
                new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
            });

            return shapes;
        }

        public static PieceShape Get(ShapeId id)
        {
            return Shapes[id];
        }

        /// <summary>The shape's cells rotated by the given number of quarter-turns and re-normalized so the bounding box starts at (0, 0) again — the <see cref="PieceShape.Id"/> stays the base <see cref="ShapeId"/> regardless of rotation.</summary>
        public static PieceShape GetRotated(ShapeId id, PieceRotation rotation)
        {
            return RotatedShapes[(id, rotation)];
        }

        public static IReadOnlyCollection<ShapeId> AllIds
        {
            get { return Shapes.Keys; }
        }

        private static Dictionary<(ShapeId, PieceRotation), PieceShape> BuildRotatedShapes()
        {
            var result = new Dictionary<(ShapeId, PieceRotation), PieceShape>();
            foreach (var kvp in Shapes)
            {
                for (int i = 0; i < AllRotations.Length; i++)
                {
                    var rotation = AllRotations[i];
                    result[(kvp.Key, rotation)] = new PieceShape(kvp.Key, RotateAndNormalize(kvp.Value.Cells, rotation));
                }
            }
            return result;
        }

        private static Vector2Int[] RotateAndNormalize(IReadOnlyList<Vector2Int> baseCells, PieceRotation rotation)
        {
            var rotated = new Vector2Int[baseCells.Count];
            for (int i = 0; i < baseCells.Count; i++)
            {
                rotated[i] = RotateCell(baseCells[i], rotation);
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            for (int i = 0; i < rotated.Length; i++)
            {
                if (rotated[i].x < minX) minX = rotated[i].x;
                if (rotated[i].y < minY) minY = rotated[i].y;
            }
            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = new Vector2Int(rotated[i].x - minX, rotated[i].y - minY);
            }
            return rotated;
        }

        /// <summary>Rotates a single relative cell offset counter-clockwise by the given number of quarter-turns (a proper rotation, never a mirror — S-tetromino never becomes a Z-shape).</summary>
        private static Vector2Int RotateCell(Vector2Int cell, PieceRotation rotation)
        {
            switch (rotation)
            {
                case PieceRotation.Deg90:
                    return new Vector2Int(-cell.y, cell.x);
                case PieceRotation.Deg180:
                    return new Vector2Int(-cell.x, -cell.y);
                case PieceRotation.Deg270:
                    return new Vector2Int(cell.y, -cell.x);
                default:
                    return cell;
            }
        }
    }
}
