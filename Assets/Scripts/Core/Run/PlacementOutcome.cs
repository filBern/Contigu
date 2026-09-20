using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Wraps a <see cref="PlacementResult"/> with the run-level consequences of
    /// that placement (updated state, round/total score) for the presentation
    /// layer to react to in one call.
    /// </summary>
    public sealed class PlacementOutcome
    {
        public readonly PlacementResult Placement;
        public readonly RunState StateAfter;
        public readonly int RoundScoreAfter;
        public readonly int TotalScoreAfter;
        public readonly int PiecesRemainingAfter;

        /// <summary>Any cell(s) the boss round just locked as part of this placement (see RunConfig.BossLockPiecesInterval) — empty outside a boss round, or on a tick that didn't land on this exact piece count.</summary>
        public readonly IReadOnlyList<Vector2Int> BossLockedCells;

        public PlacementOutcome(PlacementResult placement, RunState stateAfter, int roundScoreAfter, int totalScoreAfter, int piecesRemainingAfter, IReadOnlyList<Vector2Int> bossLockedCells = null)
        {
            Placement = placement;
            StateAfter = stateAfter;
            RoundScoreAfter = roundScoreAfter;
            TotalScoreAfter = totalScoreAfter;
            PiecesRemainingAfter = piecesRemainingAfter;
            BossLockedCells = bossLockedCells ?? System.Array.Empty<Vector2Int>();
        }
    }
}
