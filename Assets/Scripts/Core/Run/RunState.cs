namespace Contigu.Core
{
    public enum RunState
    {
        /// <summary>Round in progress, waiting for the player to place pieces.</summary>
        InProgress,

        /// <summary>Round quota reached with pieces still budgeted or exactly exhausted; a draft is pending.</summary>
        AwaitingDraft,

        /// <summary>Budget exhausted without reaching the quota: the run is over.</summary>
        RunDefeat,

        /// <summary>Round 8 (boss) cleared: the run is won.</summary>
        RunVictory
    }
}
