namespace Contigu.Core
{
    /// <summary>
    /// Mutable state for a single grid cell. Fill/lock state resets every
    /// round, and so do the golden/tinted/multiplier modifiers — under the
    /// piece-trait upgrade system (see PieceTrait) these are stamped
    /// transiently by RunManager for a single placement's scoring and cleared
    /// right after, except "Seeder" (PieceTraitKind.Seeder), which leaves its
    /// stamp in place for the REST OF THE CURRENT ROUND only: a permanent
    /// golden cell for a whole run was judged too powerful, so it's cleared
    /// here like everything else once the round ends.
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
            IsGolden = false;
            IsTinted = false;
            IsMultiplierZone = false;
        }
    }
}
