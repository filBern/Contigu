using System;

namespace Contigu.Presentation
{
    /// <summary>The gameplay speed button (x1 / x2 / x4): it speeds up the score count and the other placement animations. Not saved between launches.</summary>
    public static class GameSpeed
    {
        private static readonly int[] Steps = { 1, 2, 4 };
        private static int _index;

        public static event Action Changed;

        public static int Multiplier
        {
            get { return Steps[_index]; }
        }

        public static string Label
        {
            get { return "x" + Multiplier; }
        }

        /// <summary>x1 to x2 to x4 and back to x1.</summary>
        public static void Cycle()
        {
            _index = (_index + 1) % Steps.Length;
            if (Changed != null)
            {
                Changed();
            }
        }
    }
}
