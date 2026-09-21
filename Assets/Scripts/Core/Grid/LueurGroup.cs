using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// One contiguous same-color run within a single cleared line — worth
    /// EconomyConstants.LueurPerColorGroup (explicit request: "chaque groupe
    /// d'une couleur sur la ligne = 2 points"), replacing the old
    /// distinct-color-count formula. Computed by
    /// GridManager.ComputeLueurGroups so the presentation layer can animate
    /// each group's Lueur separately (pulse its cells, then fly a popup from
    /// the group's own center to the Lueur HUD label) instead of only ever
    /// seeing the placement's total. A run of Joker cells never becomes a
    /// group (Jokers earn no Lueur, same convention as before) but still
    /// breaks contiguity between the non-Joker runs on either side of it.
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
