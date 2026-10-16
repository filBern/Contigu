namespace Contigu.Core
{
    /// <summary>
    /// The enemy roster. HeavyLocker/Plague/Thief/Reclaimer are Boss-tagged
    /// escalations of Locker/Poisoner plus two new mechanics (see
    /// EnemyRole). Leech is a non-Boss Reactor. ColorHater/ShapeHater are a
    /// further pair of bosses — see EnemyInstance.HatedColor/HatedShape and
    /// RunManager.ApplyCursedColorScoreRule/ApplyShapeHaterScoreRule.
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
        Leech,
        ColorHater,
        ShapeHater
    }
}
