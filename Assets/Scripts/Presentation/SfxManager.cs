using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>One-shot sound effect this project has a clip for (see Assets/Resources/SFX) — Score doubles as the rising-pitch "combo tick" (see PlayComboTick), and Whoosh doubles for both a line/column clear and an overlay opening/closing.</summary>
    public enum SfxId
    {
        PickUpPiece,
        ValidDrop,
        InvalidDrop,
        Score,
        LineClear,
        LueurGain,
        Shuffle,
        ButtonHover,
        ButtonClick,
        Overlay,
        CarouselTick,
        Harvest,
        Clang,
        Crumble,
        LevelUp,
        Victory,
        Defeat,
        RoundClear,
        SlotPop,
        Buy
    }

    /// <summary>
    /// Plain static class that lazily creates one hidden, DontDestroyOnLoad
    /// GameObject on first use, so both GameBootstrap and MainMenuBootstrap
    /// can call into the same instance without either one owning it. One
    /// clip can cover more than one SfxId (Whoosh for both LineClear and
    /// Overlay; Score is also PlayComboTick's clip).
    /// </summary>
    public static class SfxManager
    {
        private const string ClipFolder = "SFX/";
        // Small per-play pitch jitter on every one-shot, so a sound played
        // many times per run doesn't fatigue the ear.
        private const float PitchJitter = 0.05f;
        // The "combo" (see PlayComboTick) rises in pitch across a whole
        // placement's score cascade, capped so a very long combo doesn't
        // end up in ultrasonic-chipmunk territory.
        private const float ComboPitchStep = 0.035f;
        private const float ComboPitchMax = 1.6f;
        // Small extra jitter on top of the rising ladder itself, smaller
        // than PitchJitter since this one already varies step to step.
        private const float ComboPitchJitter = 0.02f;
        // Whoosh.wav is a tiny source clip (~125ms). LineClear can stack up
        // several plays in quick succession, so it's pitched up to stay
        // snappy (~100ms), while Overlay keeps the slower, longer take.
        private const float LineClearPitch = 1.25f;
        // Overlay open/close is a single, one-off moment; lowering pitch
        // slows playback, stretching duration since no longer source clip
        // exists for it.
        private const float OverlayPitch = 0.5f;
        // BackgroundMusic.wav plays much louder than the SFX relative to it,
        // so it's cut to a quarter of whatever the Music slider says.
        private const float MusicVolumeScale = 0.25f;

        private static AudioSource _sfxSource;
        private static AudioSource _musicSource;
        private static AudioSource _padSource;
        private static PadFader _padFader;
        private static Dictionary<SfxId, AudioClip> _clips;
        private static int _comboStep;

        private static void EnsureInitialized()
        {
            if (_sfxSource != null)
            {
                return;
            }

            var host = new GameObject("SfxManager");
            Object.DontDestroyOnLoad(host);
            _sfxSource = host.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _musicSource = host.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _padSource = host.AddComponent<AudioSource>();
            _padSource.playOnAwake = false;
            _padSource.loop = true;
            _padSource.volume = 0f;
            _padFader = host.AddComponent<PadFader>();

            _clips = new Dictionary<SfxId, AudioClip>
            {
                { SfxId.PickUpPiece, Resources.Load<AudioClip>(ClipFolder + "PickUpPiece") },
                { SfxId.ValidDrop, Resources.Load<AudioClip>(ClipFolder + "ValidDropPiece") },
                { SfxId.InvalidDrop, Resources.Load<AudioClip>(ClipFolder + "Denied") },
                { SfxId.Score, Resources.Load<AudioClip>(ClipFolder + "Score") },
                { SfxId.LineClear, Resources.Load<AudioClip>(ClipFolder + "Harvest") },
                { SfxId.LueurGain, Resources.Load<AudioClip>(ClipFolder + "LueurGain") },
                { SfxId.Shuffle, Resources.Load<AudioClip>(ClipFolder + "Shuffle") },
                { SfxId.ButtonHover, Resources.Load<AudioClip>(ClipFolder + "ButtonHover") },
                { SfxId.ButtonClick, Resources.Load<AudioClip>(ClipFolder + "ButtonClick") },
                { SfxId.Overlay, Resources.Load<AudioClip>(ClipFolder + "Whoosh") },
                { SfxId.CarouselTick, Resources.Load<AudioClip>(ClipFolder + "CarouselTick") },
                { SfxId.Harvest, Resources.Load<AudioClip>(ClipFolder + "Harvest") },
                { SfxId.Clang, Resources.Load<AudioClip>(ClipFolder + "Clang") },
                { SfxId.Crumble, Resources.Load<AudioClip>(ClipFolder + "Crumble") },
                { SfxId.LevelUp, Resources.Load<AudioClip>(ClipFolder + "LevelUp") },
                { SfxId.Victory, Resources.Load<AudioClip>(ClipFolder + "Victory") },
                { SfxId.Defeat, Resources.Load<AudioClip>(ClipFolder + "Defeat") },
                { SfxId.RoundClear, Resources.Load<AudioClip>(ClipFolder + "RoundClear") },
                { SfxId.SlotPop, Resources.Load<AudioClip>(ClipFolder + "SlotPop") },
                { SfxId.Buy, Resources.Load<AudioClip>(ClipFolder + "Buy") }
            };
            _musicSource.clip = Resources.Load<AudioClip>(ClipFolder + "BackgroundMusic");

            ApplyVolumes();
            VolumeSettings.Changed += ApplyVolumes;
        }

        private static void ApplyVolumes()
        {
            // AudioListener.volume (see GameBootstrap.ApplyVolumeSettings)
            // already scales every AudioSource in the scene by MasterVolume;
            // these two just need their own Music/Sfx slider applied on top.
            _sfxSource.volume = VolumeSettings.SfxVolume;
            _musicSource.volume = VolumeSettings.MusicVolume * MusicVolumeScale;
        }

        /// <summary>The ambient pad sits well under the music.</summary>
        private const float PadVolumeScale = 0.35f;

        /// <summary>Volume the pad settles at when audible: the Music slider, scaled down.</summary>
        public static float PadBaseVolume
        {
            get { return VolumeSettings.MusicVolume * PadVolumeScale; }
        }

        /// <summary>Fades in the ambient pad of a challenge (Resources/SFX/Pad_&lt;name&gt;), crossfading from the previous one.</summary>
        public static void PlayPad(string challengeName)
        {
            EnsureInitialized();
            var clip = Resources.Load<AudioClip>(ClipFolder + "Pad_" + challengeName);
            _padFader.Switch(_padSource, clip);
        }

        public static void StopPad()
        {
            if (_padFader != null)
            {
                _padFader.Switch(_padSource, null);
            }
        }

        /// <summary>The pad steps back (shop, menus over it) or returns to its level.</summary>
        public static void DuckPad(bool ducked)
        {
            if (_padFader != null)
            {
                _padFader.Ducked = ducked;
            }
        }

        /// <summary>Starts the background music loop — a no-op if it's already playing (safe to call again from either scene's bootstrap) or if the clip failed to load.</summary>
        public static void PlayMusic()
        {
            EnsureInitialized();
            if (_musicSource.clip != null && !_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        public static void Play(SfxId id)
        {
            EnsureInitialized();
            if (!_clips.TryGetValue(id, out var clip) || clip == null)
            {
                return;
            }
            float basePitch = id == SfxId.Overlay ? OverlayPitch
                : 1f;
            _sfxSource.pitch = basePitch + Random.Range(-PitchJitter, PitchJitter);
            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Call once at the start of a placement's whole feedback cascade (see GameBootstrap.PlayPlacementSequence), before any PlayComboTick calls for it.</summary>
        public static void ResetComboPitch()
        {
            _comboStep = 0;
        }

        /// <summary>
        /// Score.wav, one step further up a rising-pitch ladder that spans
        /// the whole placement cascade (points and Mult catch-up ticks
        /// alike, see ResetComboPitch) rather than restarting per section,
        /// so the sequence reads as one continuous building combo.
        /// </summary>
        public static void PlayComboTick()
        {
            EnsureInitialized();
            if (!_clips.TryGetValue(SfxId.Score, out var clip) || clip == null)
            {
                return;
            }
            _sfxSource.pitch = Mathf.Min(ComboPitchMax, 1f + _comboStep * ComboPitchStep + Random.Range(-ComboPitchJitter, ComboPitchJitter));
            _comboStep++;
            _sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>Fades the ambient pad in and out (and swaps its clip while it is silent): the pad never starts or stops abruptly.</summary>
    public sealed class PadFader : MonoBehaviour
    {
        private const float FadeSeconds = 1.6f;
        private const float DuckedFactor = 0.45f;
        private AudioSource _source;
        private AudioClip _wanted;
        private bool _hasWanted;
        public bool Ducked;

        public void Switch(AudioSource source, AudioClip clip)
        {
            _source = source;
            _wanted = clip;
            _hasWanted = true;
        }

        private void Update()
        {
            if (_source == null)
            {
                return;
            }
            // A different clip (or none): fade out first, swap once silent.
            bool clipPending = _hasWanted && _source.clip != _wanted;
            float target = clipPending || _wanted == null ? 0f : SfxManager.PadBaseVolume * (Ducked ? DuckedFactor : 1f);
            float rate = Mathf.Max(0.01f, SfxManager.PadBaseVolume) / FadeSeconds;
            _source.volume = Mathf.MoveTowards(_source.volume, target, rate * Time.unscaledDeltaTime);
            if (clipPending && _source.volume <= 0.001f)
            {
                _source.Stop();
                _source.clip = _wanted;
                if (_wanted != null)
                {
                    _source.Play();
                }
                _hasWanted = false;
            }
            if (!clipPending && _wanted != null && !_source.isPlaying)
            {
                _source.Play();
            }
        }
    }
}
