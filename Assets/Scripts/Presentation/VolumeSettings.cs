using System;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persisted volume prefs for SettingsView (spec extension, explicit
    /// request: "un menu settings pour gérer le volume de musique, de sfx,
    /// général"). Same PlayerPrefs-backed, load-once-at-static-init pattern
    /// as ColorblindMode. Named "VolumeSettings" rather than "AudioSettings"
    /// to avoid colliding with UnityEngine.AudioSettings (a real built-in
    /// static class) wherever this file's "using UnityEngine;" and a caller
    /// elsewhere might both be in scope.
    ///
    /// <see cref="MasterVolume"/> is applied to AudioListener.volume (see
    /// GameBootstrap.ApplyVolumeSettings), scaling every AudioSource in the
    /// scene at once; <see cref="MusicVolume"/>/<see cref="SfxVolume"/> are
    /// applied directly to SfxManager's own two AudioSources instead, since
    /// there's still no AudioMixer to route a real Music/SFX bus through.
    /// </summary>
    public static class VolumeSettings
    {
        private const string MasterVolumeKey = "MasterVolume";
        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private const float DefaultVolume = 1f;

        public static event Action Changed;

        public static float MasterVolume { get; private set; } = PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
        public static float MusicVolume { get; private set; } = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        public static float SfxVolume { get; private set; } = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
