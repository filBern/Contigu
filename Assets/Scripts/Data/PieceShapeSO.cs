using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Optional editor-tunable display metadata for one <see cref="ShapeId"/>
    /// (display name only — the actual cell layout is fixed in
    /// <see cref="PieceShapeCatalog"/> per spec 4.3).
    /// </summary>
    [CreateAssetMenu(fileName = "PieceShape", menuName = "Contigu/Piece Shape")]
    public sealed class PieceShapeSO : ScriptableObject
    {
        public ShapeId shapeId;
        public string displayName = "Shape";
    }
}
