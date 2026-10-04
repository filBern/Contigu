using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Optional editor-tunable display metadata for one <see cref="PieceColor"/>.
    /// The game works fine without any asset of this type assigned — see
    /// <see cref="VisualDefaults"/> for the built-in fallback — but designers can
    /// create instances via the Assets menu to re-balance the palette without
    /// touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "PieceColor", menuName = "Contigu/Piece Color")]
    public sealed class PieceColorSO : ScriptableObject
    {
        public PieceColor colorId;
        public string displayName = "Color";
        public Color displayColor = Color.white;
    }
}
