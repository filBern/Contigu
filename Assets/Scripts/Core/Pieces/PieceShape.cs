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
    /// Static registry of the 10 fixed shapes described in spec section 4.3.
    /// </summary>
    public static class PieceShapeCatalog
    {
        private static readonly Dictionary<ShapeId, PieceShape> Shapes = BuildShapes();

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

        public static IReadOnlyCollection<ShapeId> AllIds
        {
            get { return Shapes.Keys; }
        }
    }
}
