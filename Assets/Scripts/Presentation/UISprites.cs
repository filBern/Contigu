using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Lazily-loaded, cached references to the "Colorful UI" asset pack
    /// (Assets/Resources/Colorful_UI/...) — same lazy Resources.Load-and-cache
    /// pattern as UIFactory.DefaultFont(), just one property per sprite
    /// instead of repeating the resource path at every call site.
    /// </summary>
    public static class UISprites
    {
        private const string SliderPath = "Colorful_UI/colorful/sprites/slider/";
        private const string GameUIPath = "Colorful_UI/colorful/sprites/gameUI/";
        private const string IconsPath = "Icons/";

        private static Sprite _barTrack;
        private static Sprite _scoreBarFill;
        private static Sprite _piecesBarFill;
        private static Sprite _cardBackground;
        private static Sprite _chooseButtonBackground;
        private static Sprite _cancelButtonBackground;
        private static Sprite _countBadge;
        private static Sprite _sfxBarFill;
        private static Sprite _sliderHandle;

        /// <summary>Shared rounded-pill track behind SettingsView's volume sliders (HudView builds its own flat-panel progress bars instead — see HudView.BuildBar).</summary>
        public static Sprite BarTrack
        {
            get { return _barTrack != null ? _barTrack : (_barTrack = Resources.Load<Sprite>(SliderPath + "progress_bar (1)")); }
        }

        /// <summary>Fill for SettingsView's Master volume slider.</summary>
        public static Sprite ScoreBarFill
        {
            get { return _scoreBarFill != null ? _scoreBarFill : (_scoreBarFill = Resources.Load<Sprite>(SliderPath + "blueBarFill")); }
        }

        /// <summary>Fill for SettingsView's Music volume slider.</summary>
        public static Sprite PiecesBarFill
        {
            get { return _piecesBarFill != null ? _piecesBarFill : (_piecesBarFill = Resources.Load<Sprite>(SliderPath + "purpleBarFill")); }
        }

        /// <summary>The "card_bg_3" card art — background of each hand slot (HandView) and, tinted darker, of the shop's modifier/upgrade cards (ShopView, on explicit request: "pour le background des ''cartes'' dans le shop j'aimerais qu'on utilise card_bg_3.png teinté en plus foncé").</summary>
        public static Sprite CardBackground
        {
            get { return _cardBackground != null ? _cardBackground : (_cardBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_3")); }
        }

        /// <summary>
        /// Background for a primary "Choose"/confirm action button — every
        /// positive-action button in the game (Play, Buy, Confirm, Next
        /// round, the Shuffle button, etc.). Sourced from a hand-authored
        /// art asset the player supplied directly (a plain white rounded
        /// rect, ~132x40, corner radius ~18px), then recolored here by a
        /// straight per-pixel multiply against this button's own fill color
        /// — every pixel's gray value (R channel; the source is true
        /// grayscale) scales this color, so the source's baked-in bottom
        /// shading band survives as a proportionally darker shade of
        /// whatever color it's tinted to, rather than being redrawn by
        /// hand. 9-slice border is 18/0/18/0 (left/bottom/right/top) to
        /// match this asset's own corner radius — see this same folder's
        /// sibling <see cref="CancelButtonBackground"/>, tinted from the
        /// exact same source PNG. Import's filter mode is Bilinear, not
        /// this project's usual Point (every other Icons/ sprite) — on
        /// explicit report that the buttons looked "très pixelisé" once
        /// <see cref="UIFactory.AddThickOutline"/> stopped hiding the
        /// rounded corners' staircase under Point sampling's nearest-pixel
        /// jump between the source PNG's own anti-aliased alpha steps.
        /// </summary>
        public static Sprite ChooseButtonBackground
        {
            get { return _chooseButtonBackground != null ? _chooseButtonBackground : (_chooseButtonBackground = Resources.Load<Sprite>(IconsPath + "pill_button_blue")); }
        }

        /// <summary>Background for a "Cancel"/negative-action button (Reroll, the shop's Mult pill, etc.) — same new flat-pill sprite family as <see cref="ChooseButtonBackground"/>, just the Danger-red variant.</summary>
        public static Sprite CancelButtonBackground
        {
            get { return _cancelButtonBackground != null ? _cancelButtonBackground : (_cancelButtonBackground = Resources.Load<Sprite>(IconsPath + "pill_button_danger")); }
        }

        /// <summary>Small filled circle used as a corner badge showing a remaining-count number (HandView's Shuffle button — explicit request: "utiliser Ellipse 19.png en haut à droite du bouton et qu'on mette le nombre de shuffle restant au milieu", replacing the "Shuffle (10)" label text that wrapped to 2 lines).</summary>
        public static Sprite CountBadge
        {
            get { return _countBadge != null ? _countBadge : (_countBadge = Resources.Load<Sprite>(SliderPath + "Ellipse 19")); }
        }

        /// <summary>Fill for the SFX volume slider (SettingsView) — a third color distinct from ScoreBarFill (Master)/PiecesBarFill (Music) so the 3 sliders stay visually distinguishable at a glance.</summary>
        public static Sprite SfxBarFill
        {
            get { return _sfxBarFill != null ? _sfxBarFill : (_sfxBarFill = Resources.Load<Sprite>(SliderPath + "greenBarFill")); }
        }

        /// <summary>Round knob for every UnityEngine.UI.Slider in SettingsView (Master/Music/SFX volume) — same "Ellipse" family already used for HandView's Shuffle count badge.</summary>
        public static Sprite SliderHandle
        {
            get { return _sliderHandle != null ? _sliderHandle : (_sliderHandle = Resources.Load<Sprite>(SliderPath + "Ellipse 20")); }
        }
    }
}
