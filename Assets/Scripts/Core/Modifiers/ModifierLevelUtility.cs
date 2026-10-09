using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Converts a modifier slot's level (see RunManager.GetModifierLevel, 1
    /// by default — un-leveled) into the scoring multiplier the "Modifier
    /// Upgrade" shop upgrade grants. A single tunable constant here scales
    /// every modifier in the catalog uniformly regardless of how its own
    /// effect is expressed (flat bonus, xN multiplier, +Mult additive, or
    /// Lueur) — see GridManager.ApplyPreClearModifiers/ApplyPostClearModifiers
    /// and RunManager.ApplyDeckStateModifierBonuses/ApplyHandSlotModifierBonus
    /// for where this actually gets applied, generically, without touching
    /// any individual modifier's own Apply* scoring logic.
    /// </summary>
    public static class ModifierLevelUtility
    {
        /// <summary>+50% per level above 1 (level 2 = 1.5x, level 3 = 2x, ...) — linear rather than compounding, to avoid runaway power creep from stacking several purchases on the same modifier. A single tunable knob if playtesting calls for a gentler or steeper curve.</summary>
        public const float BonusPerLevel = 0.5f;

        public static float LevelToFactor(int level)
        {
            int clamped = level < 1 ? 1 : level;
            return 1f + (clamped - 1) * BonusPerLevel;
        }
    }
}
