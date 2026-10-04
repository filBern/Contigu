using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    [CreateAssetMenu(fileName = "PieceColorDatabase", menuName = "Contigu/Piece Color Database")]
    public sealed class PieceColorDatabase : ScriptableObject
    {
        public List<PieceColorSO> colors = new List<PieceColorSO>();

        public PieceColorSO Get(PieceColor id)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] != null && colors[i].colorId == id)
                {
                    return colors[i];
                }
            }
            return null;
        }
    }
}
