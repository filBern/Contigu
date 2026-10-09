namespace Contigu.Core
{
    /// <summary>
    /// Thin abstraction over where <see cref="MetaStats"/> actually lives —
    /// same rationale as IRandomProvider: keeps Core testable without
    /// touching disk, while the real (Presentation-side) implementation
    /// reads/writes a JSON file under Application.persistentDataPath.
    /// </summary>
    public interface IMetaStatsStore
    {
        /// <summary>Returns the previously saved stats, or a fresh all-zero MetaStats if none exist yet (first launch).</summary>
        MetaStats Load();

        void Save(MetaStats stats);
    }
}
