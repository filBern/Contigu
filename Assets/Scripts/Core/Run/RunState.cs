namespace Contigu.Core
{
    public enum RunState
    {
        /// <summary>Round in progress, waiting for the player to place pieces.</summary>
        InProgress,

        /// <summary>Round quota reached with pieces still budgeted or exactly exhausted; a draft is pending.</summary>
        AwaitingDraft,

        /// <summary>Tile/grid upgrade just applied; the player must now pick 1 of 3 offered modifiers.</summary>
        AwaitingModifierPick,

        /// <summary>Budget exhausted without reaching the quota: the run is over.</summary>
        RunDefeat,

        /// <summary>Round 8 (boss) cleared: the run is won.</summary>
        RunVictory
    }
}
