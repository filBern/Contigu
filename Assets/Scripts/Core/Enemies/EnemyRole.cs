namespace Contigu.Core
{
    /// <summary>An enemy's tag in the GDD's own encounter table — cosmetic/authoring metadata today (nothing in RunManager branches on it yet), kept alongside EnemyDefinition so EncounterCatalog's future boss-round picks can filter by it once the Boss-tagged enemies (Heavy Locker, Plague, Thief, Reclaimer) exist.</summary>
    public enum EnemyRole
    {
        Baseline,
        Creator,
        Boss
    }
}
