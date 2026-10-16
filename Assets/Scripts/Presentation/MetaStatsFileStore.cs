using System;
using System.IO;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Real (file-backed) IMetaStatsStore — a small JSON file under
    /// Application.persistentDataPath, the standard Unity location for save
    /// data that survives a rebuild/reinstall of the game (unlike a path
    /// next to the executable). Kept out of Core so Core stays free of any
    /// actual disk I/O; Core only defines the interface and the pure
    /// MetaStatsRecorder update logic.
    /// </summary>
    public sealed class MetaStatsFileStore : IMetaStatsStore
    {
        private readonly string _filePath;

        public MetaStatsFileStore()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "meta_stats.json");
        }

        /// <summary>Missing or unreadable/corrupted file both fall back to a fresh all-zero MetaStats rather than throwing — a save file is an external boundary (player's disk, previous game version, a manual edit), not something the rest of the game should ever crash over.</summary>
        public MetaStats Load()
        {
            if (!File.Exists(_filePath))
            {
                return new MetaStats();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var stats = JsonUtility.FromJson<MetaStats>(json);
                return stats ?? new MetaStats();
            }
            catch (Exception)
            {
                return new MetaStats();
            }
        }

        public void Save(MetaStats stats)
        {
            string json = JsonUtility.ToJson(stats);
            File.WriteAllText(_filePath, json);
        }
    }
}
