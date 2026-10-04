namespace Contigu.Core
{
    /// <summary>
    /// The enemy roster (spec extension, explicit request: "ajouter un
    /// petit peu d'autobattling" — see GDD §07 Enemy System & Combat).
    /// HeavyLocker/Plague/Thief/Reclaimer are the GDD's Boss-tagged
    /// escalations of Locker/Poisoner plus two new mechanics (see
    /// EnemyRole), added on a later explicit request: "Ajoutons de
    /// nouveaux ennemies et boss". Leech is a non-Boss Reactor.
    /// </summary>
    public enum EnemyId
    {
        Basic,
        Locker,
        Poisoner,
        HeavyLocker,
        Plague,
        Thief,
        Reclaimer,
        Leech
    }
}
