namespace Contigu.Core
{
    /// <summary>
    /// Mutable state for a single grid cell. Fill/lock state resets every
    /// round, and so do the golden/tinted/multiplier modifiers — under the
    /// piece-trait upgrade system (see PieceTrait) these are stamped
    /// transiently by RunManager for a single placement's scoring and cleared
    /// right after, except "Seeder" (PieceTraitKind.Seeder), which leaves its
    /// stamp in place for the rest of the current round only, for as long as
    /// the cell stays filled — a line clear or the "Void Tile" trait emptying
    /// it mid-round destroys the enchantment right along with the tile (see
    /// <see cref="ClearFill"/>), rather than leaving it to silently attach
    /// itself to whatever unrelated piece lands there next.
    /// </summary>
    public sealed class Cell
    {
        public bool IsFilled;
        public PieceColor? FilledColor;

        /// <summary>
        /// Which shape originally filled this cell — same persistent-stamp
        /// convention as <see cref="FilledColor"/>, so a group re-scored by
        /// a later, unrelated placement still correctly remembers which
        /// shape originally placed each of its older cells (see
        /// RunManager.ApplyShapeHaterScoreRule). Cleared alongside
        /// FilledColor by <see cref="ClearFill"/>/<see cref="ResetForNewRound"/>.
        /// </summary>
        public ShapeId? FilledShapeId;

        /// <summary>Only ever true during the boss round, or for Locker's own roaming lock (see IsLineClearObstacle).</summary>
        public bool IsLocked;

        /// <summary>
        /// True only for Locker's own lock: unlike the old boss round's
        /// ProgressiveCellLock (LockFreeCellsAndCheckClears) and a Bastion
        /// cell (IsBastion), which are both deliberately designed to let a
        /// row/column complete and clear around (or even because of) them,
        /// a cell Locker has locked is meant to be a genuine obstacle —
        /// GridManager.IsRowComplete/IsColumnComplete treat it as an
        /// ordinary unfilled cell instead of skipping it, so the whole line
        /// it sits in can never complete while Locker's lock is there.
        /// Set/cleared together with IsLocked by RunManager.
        /// ResolveLockerShuffleEffect/CleanUpDefeatedEnemy only.
        /// </summary>
        public bool IsLineClearObstacle;

        /// <summary>
        /// "Poisoner" enemy mechanic: any point event scored on this cell
        /// counts negative instead of positive while this is true (see
        /// RunManager.ApplyPoisonScoreRule). Can be set on any cell, filled
        /// or empty alike — an empty one sits there as a trap for whatever
        /// piece lands on it later. Persists on a now-empty cell after a
        /// line clear — <see cref="ClearFill"/> deliberately leaves it
        /// alone: the hazard stays attached to this grid position, not to
        /// whichever tile happened to be filling it, until the next
        /// Shuffle rolls it away (see RunManager.ResolvePoisonerShuffleEffect)
        /// or until "contamination" spreads it onto a neighbor in the
        /// meantime (see RunManager.ContaminateAdjacentCell).
        /// </summary>
        public bool IsPoisoned;

        public bool IsGolden;

        public bool IsTinted;
        public PieceColor TintedColor;

        public bool IsMultiplierZone;

        /// <summary>
        /// Flat bonus from a "Piece Mastery" purchase, stamped once at
        /// placement time from whatever level the piece's exact shape had
        /// then (RunManager.PlacePiece, before GridManager.PlacePiece runs)
        /// — 0 if that shape was never leveled up, or wasn't yet when this
        /// tile was placed. A later Piece Mastery purchase only affects
        /// tiles placed after it; this one's value is frozen. Same
        /// persistent per-cell stamp, rescored every time this cell's
        /// group scores again, pattern as <see cref="IsGolden"/>'s
        /// "Seeder" case.
        /// </summary>
        public int ShapeMasteryBonus;

        /// <summary>Piece Mastery's exact sibling, from a "Color Mastery" purchase — same per-cell stamp-and-rescore pattern as <see cref="ShapeMasteryBonus"/>, keyed by the tile's own color instead of its piece's shape.</summary>
        public int ColorMasteryBonus;

        /// <summary>
        /// "Bastion Tile" stamp: set together with <see cref="IsLocked"/>
        /// once a Bastion-enchanted piece has actually been placed here
        /// (see RunManager.ApplyBastionEffect). From then on this cell
        /// behaves like any other locked cell for placement/clearing
        /// purposes — see <see cref="IsLocked"/> — but GridManager.
        /// CheckAndClearLines still credits it the line-clear bonus every
        /// time its row/column completes, without ever actually emptying it.
        /// </summary>
        public bool IsBastion;

        /// <summary>
        /// Which tile-upgrade trait (if any) was originally enchanted onto
        /// this cell, stamped by RunManager.ApplyTokenTrait for the rest of
        /// the round regardless of trait kind — purely cosmetic, independent
        /// of the Golden/Tinted/MultiplierZone flags above, which most
        /// trait kinds clear again right after the placement that set
        /// them scores.
        /// </summary>
        public PieceTrait? OriginTrait;

        public bool HasAnyModifier
        {
            get { return IsGolden || IsTinted || IsMultiplierZone || IsBastion; }
        }

        /// <summary>
        /// Empties this cell the way a completed line clear or the "Void
        /// Tile" trait's random clear does — unlike <see
        /// cref="ResetForNewRound"/>, <see cref="IsLocked"/> is left alone
        /// (locking is a boss-round grid property, not tied to whatever
        /// happened to be filling the cell). Also drops every modifier flag
        /// and <see cref="OriginTrait"/>, so an unrelated piece placed in
        /// this same spot later never inherits an enchantment it didn't
        /// earn. <see cref="IsPoisoned"/> is deliberately left alone, same
        /// reasoning as IsLocked — see its own doc comment.
        /// </summary>
        public void ClearFill()
        {
            IsFilled = false;
            FilledColor = null;
            FilledShapeId = null;
            IsGolden = false;
            IsTinted = false;
            IsMultiplierZone = false;
            IsBastion = false;
            OriginTrait = null;
            ShapeMasteryBonus = 0;
            ColorMasteryBonus = 0;
        }

        public void ResetForNewRound()
        {
            IsFilled = false;
            FilledColor = null;
            FilledShapeId = null;
            IsLocked = false;
            IsLineClearObstacle = false;
            IsPoisoned = false;
            IsGolden = false;
            IsTinted = false;
            IsMultiplierZone = false;
            IsBastion = false;
            OriginTrait = null;
            ShapeMasteryBonus = 0;
            ColorMasteryBonus = 0;
        }
    }
}
