namespace Contigu.Core
{
    public enum RunState
    {
        /// <summary>Round in progress, waiting for the player to place pieces.</summary>
        InProgress,

        /// <summary>Round quota reached with pieces still budgeted or exactly exhausted; the Lueur shop is open (see RunManager.ShopModifierSlots/ShopUpgradeSlots) until the player leaves it.</summary>
        AwaitingShop,

        /// <summary>Budget exhausted without reaching the quota: the run is over.</summary>
        RunDefeat,

        /// <summary>Round 8 (boss) cleared: the run is won.</summary>
        RunVictory
    }
}
