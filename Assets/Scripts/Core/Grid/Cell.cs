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
    /// here like everything else once the round ends. That "rest of the
    /// round" only ever means "for as long as this cell stays filled",
    /// though — a line clear or the "Void Tile" trait emptying it mid-round
    /// destroys the enchantment right along with the tile (see <see
    /// cref="ClearFill"/>), rather than leaving it to silently attach
    /// itself to whatever unrelated piece lands there next.
    /// </summary>
    public sealed class Cell
    {
        public bool IsFilled;
        public PieceColor? FilledColor;

        /// <summary>Only ever true during the boss round (spec 6.1).</summary>
        public bool IsLocked;

        /// <summary>
        /// "Poisoner" enemy mechanic (spec extension, explicit request:
        /// "ajouter un petit peu d'autobattling" — see GDD §07/RunManager.
        /// ResolvePoisonerShuffleEffect): any point event scored on this
        /// cell counts negative instead of positive while this is true (see
        /// RunManager.ApplyPoisonScoreRule). Only ever true on an already-
        /// FILLED cell — an empty one has nothing to invert yet.
        /// </summary>
        public bool IsPoisoned;

        public bool IsGolden;

        public bool IsTinted;
        public PieceColor TintedColor;

        public bool IsMultiplierZone;

        /// <summary>
        /// Flat bonus from a "Piece Mastery" purchase, stamped once at
        /// placement time from whatever level the piece's exact shape had
        /// THEN (RunManager.PlacePiece, before GridManager.PlacePiece runs)
        /// — 0 if that shape was never leveled up, or wasn't yet when this
        /// tile was placed. A later Piece Mastery purchase only affects
        /// tiles placed AFTER it; this one's value is frozen. Same
        /// "persistent per-cell stamp, rescored every time this cell's
        /// group scores again" pattern as <see cref="IsGolden"/>'s
        /// "Seeder" case (on explicit report: "Chaque tuile devient niveau
        /// 2. Donc chaque fois que cette tuile est comptabilisé on fait
        /// +1" — a flat bonus applied once per PLACEMENT, not once per
        /// tile, undercounted a multi-cell piece).
        /// </summary>
        public int ShapeMasteryBonus;

        /// <summary>Piece Mastery's exact sibling, from a "Color Mastery" purchase — same per-cell stamp-and-rescore pattern as <see cref="ShapeMasteryBonus"/>, keyed by the tile's own color instead of its piece's shape.</summary>
        public int ColorMasteryBonus;

        /// <summary>
        /// "Bastion Tile" stamp (spec extension, explicit request — "Locked
        /// cell upgraded. N'est pas cleared mais fait quand même les points
        /// cleared"): set together with <see cref="IsLocked"/> once a
        /// Bastion-enchanted piece has actually been placed here (see
        /// RunManager.ApplyBastionEffect). From then on this cell behaves
        /// like any other locked cell for placement/clearing purposes — see
        /// <see cref="IsLocked"/> — but GridManager.CheckAndClearLines still
        /// credits it the line-clear bonus every time its row/column
        /// completes, without ever actually emptying it.
        /// </summary>
        public bool IsBastion;

        /// <summary>
        /// Which tile-upgrade trait (if any) was originally enchanted onto
        /// this cell, stamped by RunManager.ApplyTokenTrait for the rest of
        /// the round regardless of trait kind — purely cosmetic (on explicit
        /// request: showing it on the grid helps the player keep track of
        /// their deck's upgrades), independent of the Golden/Tinted/
        /// MultiplierZone flags above, which most trait kinds clear again
        /// right after the placement that set them scores.
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
        /// and <see cref="OriginTrait"/>: on explicit player report, these
        /// used to only be reset by ResetForNewRound, so a cell cleared
        /// mid-round (including a "Seeder" cell, permanently golden for the
        /// rest of the round otherwise) kept its stamp even once genuinely
        /// empty — an unrelated piece placed in that same spot later would
        /// then inherit an enchantment it never actually earned.
        /// </summary>
        public void ClearFill()
        {
            IsFilled = false;
            FilledColor = null;
            IsPoisoned = false;
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
            IsLocked = false;
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
