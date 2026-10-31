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

        /// <summary>The piece's own color before Chameleon Tile (see RunManager.ResolveChameleonColor) recolored it to match an adjacent filled neighbor, for Presentation to animate the switch rather than let it snap straight to the resolved color. Null unless this placement actually recolored (no Chameleon cell, or one that found no neighbor to match).</summary>
        public readonly PieceColor? ChameleonOriginalColor;

        /// <summary>Every lock/poison cell released this placement because the enemy that owned it died (see RunManager.CleanUpDefeatedEnemy) — a subset of a Shuffle-move's own released cells in RunManager.ResolveLockerShuffleEffect/ResolvePoisonerShuffleEffect, which this never includes. Presentation plays a red explosion VFX for these instead of the ordinary reveal fade (see GameBootstrap). Empty when nothing died this placement.</summary>
        public readonly IReadOnlyList<Vector2Int> DeathReleasedCells;

        /// <summary>Tiles the Hurricane weather blew to other plots right after this placement (empty otherwise) — the grid already holds their new positions; Presentation animates the flight.</summary>
        public readonly IReadOnlyList<(Vector2Int From, Vector2Int To, PieceColor Color)> WeatherMovedTiles;

        public PlacementOutcome(PlacementResult placement, RunState stateAfter, int roundScoreAfter, int totalScoreAfter, int piecesRemainingAfter, IReadOnlyList<Vector2Int> bossLockedCells = null, PieceColor? chameleonOriginalColor = null, IReadOnlyList<Vector2Int> deathReleasedCells = null, IReadOnlyList<(Vector2Int From, Vector2Int To, PieceColor Color)> weatherMovedTiles = null)
        {
            Placement = placement;
            StateAfter = stateAfter;
            RoundScoreAfter = roundScoreAfter;
            TotalScoreAfter = totalScoreAfter;
            PiecesRemainingAfter = piecesRemainingAfter;
            BossLockedCells = bossLockedCells ?? System.Array.Empty<Vector2Int>();
            ChameleonOriginalColor = chameleonOriginalColor;
            DeathReleasedCells = deathReleasedCells ?? System.Array.Empty<Vector2Int>();
            WeatherMovedTiles = weatherMovedTiles ?? System.Array.Empty<(Vector2Int, Vector2Int, PieceColor)>();
        }
    }
}
