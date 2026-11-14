using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// The one place the presentation layer reads and writes simple persisted settings (volume, "seen" flags).
    /// A thin wrapper over PlayerPrefs with a swappable backend, so every settings site goes through a single seam instead of
    /// touching PlayerPrefs directly (and so a test or a different storage can replace it).
    /// </summary>
    public static class GamePrefs
    {
        /// <summary>The raw key/value storage behind <see cref="GamePrefs"/>.</summary>
        public interface IBackend
        {
            int GetInt(string key, int defaultValue);
            void SetInt(string key, int value);
            float GetFloat(string key, float defaultValue);
            void SetFloat(string key, float value);
            void DeleteAll();
            void Save();
        }

        private sealed class PlayerPrefsBackend : IBackend
        {
            public int GetInt(string key, int defaultValue) { return PlayerPrefs.GetInt(key, defaultValue); }
            public void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); }
            public float GetFloat(string key, float defaultValue) { return PlayerPrefs.GetFloat(key, defaultValue); }
            public void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); }
            public void DeleteAll() { PlayerPrefs.DeleteAll(); }
            public void Save() { PlayerPrefs.Save(); }
        }

        /// <summary>Replace to redirect all settings (defaults to Unity's PlayerPrefs).</summary>
        public static IBackend Backend = new PlayerPrefsBackend();

        public static int GetInt(string key, int defaultValue) { return Backend.GetInt(key, defaultValue); }
        public static void SetInt(string key, int value) { Backend.SetInt(key, value); }
        public static float GetFloat(string key, float defaultValue) { return Backend.GetFloat(key, defaultValue); }
        public static void SetFloat(string key, float value) { Backend.SetFloat(key, value); }
        public static void DeleteAll() { Backend.DeleteAll(); }
        public static void Save() { Backend.Save(); }

        /// <summary>True the first time it is called for <paramref name="key"/> (and remembers that, saved immediately) — for one-time tips and tutorials.</summary>
        public static bool ConsumeFirstTime(string key)
        {
            if (Backend.GetInt(key, 0) != 0)
            {
                return false;
            }
            Backend.SetInt(key, 1);
            Backend.Save();
            return true;
        }
    }
}
