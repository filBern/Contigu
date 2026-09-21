using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Every cell of one DISTINCT non-Joker color within a single cleared
    /// line (not necessarily adjacent to each other) — worth
    /// EconomyConstants.LueurPerColorGroup, "2 points par couleur" on
    /// explicit request. Computed by GridManager.ComputeLueurGroups so the
    /// presentation layer can animate each color's Lueur separately (pulse
    /// its cells, then fly a popup from the group's own center to the Lueur
    /// HUD label) instead of only ever seeing the placement's total.
    /// </summary>
    public readonly struct LueurGroup
    {
        public readonly IReadOnlyList<Vector2Int> Cells;
        public readonly int Amount;

        public LueurGroup(IReadOnlyList<Vector2Int> cells, int amount)
        {
            Cells = cells;
            Amount = amount;
        }
    }
}
