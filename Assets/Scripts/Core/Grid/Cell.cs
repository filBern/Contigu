namespace Contigu.Core
{
    /// <summary>
    /// Mutable state for a single grid cell. Fill/lock state resets every round;
    /// the golden/tinted/multiplier modifiers are set once by upgrades and persist
    /// for the rest of the run (see spec section 2).
    /// </summary>
    public sealed class Cell
    {
        public bool IsFilled;
        public PieceColor? FilledColor;

        /// <summary>Only ever true during the boss round (spec 6.1).</summary>
        public bool IsLocked;

        public bool IsGolden;

        public bool IsTinted;
        public PieceColor TintedColor;

        public bool IsMultiplierZone;

        public bool HasAnyModifier
        {
            get { return IsGolden || IsTinted || IsMultiplierZone; }
        }

        public void ResetForNewRound()
        {
            IsFilled = false;
            FilledColor = null;
            IsLocked = false;
        }
    }
}
