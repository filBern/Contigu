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

        public PlacementOutcome(PlacementResult placement, RunState stateAfter, int roundScoreAfter, int totalScoreAfter, int piecesRemainingAfter)
        {
            Placement = placement;
            StateAfter = stateAfter;
            RoundScoreAfter = roundScoreAfter;
            TotalScoreAfter = totalScoreAfter;
            PiecesRemainingAfter = piecesRemainingAfter;
        }
    }
}
