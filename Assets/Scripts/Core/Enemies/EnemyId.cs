namespace Contigu.Core
{
    /// <summary>
    /// The enemy roster (spec extension, explicit request: "ajouter un
    /// petit peu d'autobattling" — see GDD §07 Enemy System & Combat). Only
    /// the 3 needed for the first vertical slice exist so far (Classic
    /// rounds 1-4, see EncounterCatalog) — Heavy Locker, Plague, Thief,
    /// Reclaimer and Leech are designed in the GDD but not yet built.
    /// </summary>
    public enum EnemyId
    {
        Basic,
        Locker,
        Poisoner
    }
}
