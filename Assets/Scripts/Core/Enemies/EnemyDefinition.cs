namespace Contigu.Core
{
    /// <summary>Static per-type enemy data (see EnemyCatalog) — the template EnemyInstance is built fresh from every round an EnemyId appears in (see EncounterCatalog/RunManager.BuildEncounter).</summary>
    public sealed class EnemyDefinition
    {
        public EnemyId Id;
        public string Name;
        public EnemyRole Role;

        /// <summary>Initial tuning value, not derived from any spec number (the GDD only says "Low HP"/"High HP") — expect this to drift as balance work continues, same as RunConfig.Quotas/PieceBudgets.</summary>
        public int MaxHp;
    }
}
