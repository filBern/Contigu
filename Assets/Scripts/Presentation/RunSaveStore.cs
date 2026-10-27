using System;
using System.IO;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>The saved run on disk (one slot, a small text file next to the persistent stats): written at the start of every round and on Save and quit, removed when the run ends.</summary>
    public static class RunSaveStore
    {
        private static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "run_save.txt"); }
        }

        public static bool Exists
        {
            get { return Load() != null; }
        }

        public static void Save(RunSnapshot snapshot)
        {
            try
            {
                File.WriteAllText(FilePath, snapshot.ToText());
            }
            catch (Exception)
            {
                // A save that cannot be written must never break the run being played.
            }
        }

        /// <summary>The saved run, or null when there is none or the file is unreadable (an old version, a damaged file).</summary>
        public static RunSnapshot Load()
        {
            try
            {
                return File.Exists(FilePath) ? RunSnapshot.FromText(File.ReadAllText(FilePath)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
