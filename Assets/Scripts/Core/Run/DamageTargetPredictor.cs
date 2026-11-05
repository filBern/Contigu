using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Read-only prediction of which enemy a placement's damage will touch, made BEFORE RunManager.PlacePiece runs so the presentation
    /// layer can hold the target's HP display at its pre-placement value. Every placement hits the front alive enemy
    /// (RunManager.ApplyDamageToEncounter); it never mutates the run.
    /// </summary>
    public static class DamageTargetPredictor
    {
        /// <summary>The enemy indices this placement's damage touches: the front alive enemy only (empty when nothing is alive or no encounter is active).</summary>
        public static List<int> FindDamagedEnemyIndices(RunManager run)
        {
            var result = new List<int>();
            if (!run.HasActiveEncounter)
            {
                return result;
            }
            var encounter = run.CurrentEncounter;
            for (int i = 0; i < encounter.Count; i++)
            {
                if (!encounter[i].IsDead)
                {
                    result.Add(i);
                    break;
                }
            }
            return result;
        }
    }
}
