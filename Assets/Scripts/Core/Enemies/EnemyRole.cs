namespace Contigu.Core
{
    /// <summary>An enemy's tag in the GDD's own encounter table — cosmetic/authoring metadata, not read by any gameplay logic (HeavyLocker/Plague/Thief/Reclaimer are Boss; Leech is Reactor only, not Boss, per the GDD's own Role column).</summary>
    public enum EnemyRole
    {
        Baseline,
        Creator,
        /// <summary>Reacts to board/round state rather than creating a new hazard itself — Reclaimer (also Boss) and Leech (GDD §07's only non-Boss "High HP" enemy).</summary>
        Reactor,
        Boss
    }
}
