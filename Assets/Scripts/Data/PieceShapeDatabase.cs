using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    [CreateAssetMenu(fileName = "PieceShapeDatabase", menuName = "Contigu/Piece Shape Database")]
    public sealed class PieceShapeDatabase : ScriptableObject
    {
        public List<PieceShapeSO> shapes = new List<PieceShapeSO>();

        public PieceShapeSO Get(ShapeId id)
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i] != null && shapes[i].shapeId == id)
                {
                    return shapes[i];
                }
            }
            return null;
        }
    }
}
