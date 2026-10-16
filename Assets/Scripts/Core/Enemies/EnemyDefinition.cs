namespace Contigu.Core
{
    /// <summary>Static per-type enemy data (see EnemyCatalog) — the template EnemyInstance is built fresh from every round an EnemyId appears in (see EncounterCatalog/RunManager.BuildEncounter).</summary>
    public sealed class EnemyDefinition
    {
        public EnemyId Id;
        public string Name;
        public EnemyRole Role;

        public int MaxHp;

        /// <summary>Plain-English summary of this enemy's On-Shuffle effect — shown by Presentation.EnemyIconView's hover tooltip.</summary>
        public string Description;
    }
}
