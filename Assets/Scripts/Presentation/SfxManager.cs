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
        CarouselTick
    }

    /// <summary>
    /// Plain static class (no MonoBehaviour host of its own to build/wire —
    /// lazily creates one hidden, DontDestroyOnLoad GameObject on first use)
    /// so both GameBootstrap (gameplay scene) and MainMenuBootstrap (its own
    /// separate scene) can call into the exact same instance without either
    /// one owning it — same "first real caller wins" laziness as
    /// VolumeSettings' own PlayerPrefs-backed statics.
    ///
    /// Built from the P1 (main gameplay loop) and P3 (general UI) sound
    /// effects (see README's own SFX list) — P2 (shop/modifiers/victory/
    /// defeat) has no clips yet, added once those exist. One clip can cover
    /// more than one SfxId (Whoosh for both LineClear and Overlay; Score is
    /// also PlayComboTick's clip) rather than 1:1, since several distinct
    /// game moments share the same "character" the SFX list asked for.
    /// </summary>
    public static class SfxManager
    {
        private const string ClipFolder = "SFX/";
        // Small per-play pitch jitter on every one-shot (explicit note from
        // the original SFX request: "prévoir 2-3 variantes par son fréquent
        // ... avec une légère randomisation de pitch — sinon le son de
        // placement qui joue 200 fois par run va vite fatiguer l'oreille").
        private const float PitchJitter = 0.05f;
        // The "combo" (see PlayComboTick) rises in pitch across a WHOLE
        // placement's score cascade, capped so a very long combo doesn't
        // end up in ultrasonic-chipmunk territory.
        private const float ComboPitchStep = 0.035f;
        private const float ComboPitchMax = 1.6f;
        // Small extra jitter on top of the rising ladder itself — on
        // explicit request ("Le son de score doit avoir plus de variation
        // over time, légèrement"), smaller than PitchJitter above since
        // this one already varies step to step on its own.
        private const float ComboPitchJitter = 0.02f;
        // Whoosh.wav (LineClear/Overlay) is a genuinely tiny source clip —
        // ~125ms — so lowering its pitch (which also slows its playback
        // rate, stretching duration) is the only length lever available
        // without a longer replacement file, on explicit request ("Le
        // whoosh doit être plus long"). 0.5 roughly doubles it to ~250ms.
        // A proper fix still needs an actually-longer source recording;
        // this only stretches what's already there.
        private const float WhooshPitch = 0.5f;
        // BackgroundMusic.wav plays much louder than the SFX relative to
        // it, on explicit report ("La musique doit être 75% plus faible")
        // — cut to a quarter of whatever the Music slider says, rather
        // than changing the slider's own 0-1 range/default.
        private const float MusicVolumeScale = 0.25f;

        private static AudioSource _sfxSource;
        private static AudioSource _musicSource;
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

            _clips = new Dictionary<SfxId, AudioClip>
            {
                { SfxId.PickUpPiece, Resources.Load<AudioClip>(ClipFolder + "PickUpPiece") },
                { SfxId.ValidDrop, Resources.Load<AudioClip>(ClipFolder + "ValidDropPiece") },
                { SfxId.InvalidDrop, Resources.Load<AudioClip>(ClipFolder + "Denied") },
                { SfxId.Score, Resources.Load<AudioClip>(ClipFolder + "Score") },
                { SfxId.LineClear, Resources.Load<AudioClip>(ClipFolder + "Whoosh") },
                { SfxId.LueurGain, Resources.Load<AudioClip>(ClipFolder + "LueurGain") },
                { SfxId.Shuffle, Resources.Load<AudioClip>(ClipFolder + "Shuffle") },
                { SfxId.ButtonHover, Resources.Load<AudioClip>(ClipFolder + "ButtonHover") },
                { SfxId.ButtonClick, Resources.Load<AudioClip>(ClipFolder + "ButtonClick") },
                { SfxId.Overlay, Resources.Load<AudioClip>(ClipFolder + "Whoosh") },
                { SfxId.CarouselTick, Resources.Load<AudioClip>(ClipFolder + "CarouselTick") }
            };
            _musicSource.clip = Resources.Load<AudioClip>(ClipFolder + "BackgroundMusic");

            ApplyVolumes();
            VolumeSettings.Changed += ApplyVolumes;
        }

        private static void ApplyVolumes()
        {
            // AudioListener.volume (see GameBootstrap.ApplyVolumeSettings)
            // already scales EVERY AudioSource in the scene by MasterVolume
            // — these two just need their own Music/Sfx slider applied on
            // top of that.
            _sfxSource.volume = VolumeSettings.SfxVolume;
            _musicSource.volume = VolumeSettings.MusicVolume * MusicVolumeScale;
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
            float basePitch = id == SfxId.LineClear || id == SfxId.Overlay ? WhooshPitch : 1f;
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
        /// the WHOLE placement cascade (points AND Mult catch-up ticks
        /// alike — see ResetComboPitch) rather than restarting per section,
        /// so the whole sequence reads as one continuous building combo
        /// (on explicit request: "Incrément de combo — un tic/ding qui
        /// monte en pitch à chaque cran").
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
}
