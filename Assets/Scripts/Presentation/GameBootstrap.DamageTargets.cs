using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>The projectile art a placement uses (which enemies it hits is predicted by Core's DamageTargetPredictor).</summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Projectile art for the color of the piece just placed, for the flying damage orbs of this placement's hits (null falls back to the plain circle).</summary>
        private Sprite _currentAttackSprite;
        private Sprite _currentPetalSprite;

        private Sprite ResolveAttackSprite(PlacementResult placement, out Sprite petalSprite)
        {
            petalSprite = null;
            if (placement.PlacedCells.Count == 0)
            {
                return null;
            }
            var pos = placement.PlacedCells[0];
            PieceColor? color = null;
            for (int i = 0; i < placement.ClearedCells.Count && i < placement.ClearedCellColors.Count; i++)
            {
                if (placement.ClearedCells[i] == pos)
                {
                    color = placement.ClearedCellColors[i];
                    break;
                }
            }
            if (!color.HasValue)
            {
                color = _run.Grid.GetCell(pos).FilledColor;
            }
            if (!color.HasValue)
            {
                return null;
            }
            // The floating particles reuse the harvest petal art (Icons/Petals), the projectile keeps the attack art.
            petalSprite = VisualDefaults.GetPetalSprite(color.Value);
            return VisualDefaults.GetAttackSprite(color.Value);
        }
    }
}
