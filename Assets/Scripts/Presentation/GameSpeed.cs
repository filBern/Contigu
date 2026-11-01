using System;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// The animation speed (x1 / x2 / x4), chosen in Settings and kept between launches: it speeds up the score count, the placement animations
    /// and the shop's longer animations (flights, crumbling, celebrations).
    /// </summary>
    public static class GameSpeed
    {
        private const string Key = "GameSpeedIndex";
        private static readonly int[] Steps = { 1, 2, 4 };

        public static event Action Changed;

        private static int Index
        {
            get { return Mathf.Clamp(GamePrefs.GetInt(Key, 0), 0, Steps.Length - 1); }
        }

        public static int Multiplier
        {
            get { return Steps[Index]; }
        }

        public static string Label
        {
            get { return "x" + Multiplier; }
        }

        /// <summary>The frame time the shop's own animations advance by: real time, times the chosen speed.</summary>
        public static float AnimationDelta
        {
            get { return Time.unscaledDeltaTime * Multiplier; }
        }

        /// <summary>x1 to x2 to x4 and back to x1.</summary>
        public static void Cycle()
        {
            GamePrefs.SetInt(Key, (Index + 1) % Steps.Length);
            GamePrefs.Save();
            if (Changed != null)
            {
                Changed();
            }
        }
    }
}
