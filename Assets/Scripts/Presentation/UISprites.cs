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
        private const string ButtonPath = "Colorful_UI/colorful/sprites/button/";

        private static Sprite _barTrack;
        private static Sprite _scoreBarFill;
        private static Sprite _piecesBarFill;
        private static Sprite _modifierPanelBackground;
        private static Sprite _cardBackground;
        private static Sprite _chooseButtonBackground;
        private static Sprite _cancelButtonBackground;
        private static Sprite _countBadge;
        private static Sprite _sfxBarFill;
        private static Sprite _sliderHandle;

        /// <summary>Shared rounded-pill track behind both HUD progress bars.</summary>
        public static Sprite BarTrack
        {
            get { return _barTrack != null ? _barTrack : (_barTrack = Resources.Load<Sprite>(SliderPath + "progress_bar (1)")); }
        }

        /// <summary>Fill for the round-score bar (top).</summary>
        public static Sprite ScoreBarFill
        {
            get { return _scoreBarFill != null ? _scoreBarFill : (_scoreBarFill = Resources.Load<Sprite>(SliderPath + "blueBarFill")); }
        }

        /// <summary>Fill for the remaining-pieces bar (bottom).</summary>
        public static Sprite PiecesBarFill
        {
            get { return _piecesBarFill != null ? _piecesBarFill : (_piecesBarFill = Resources.Load<Sprite>(SliderPath + "purpleBarFill")); }
        }

        /// <summary>
        /// Background of the left-edge active-modifiers panel (ModifierPanelView).
        /// Was briefly swapped to "card_bg_2" to sidestep panel_bg's header
        /// band distorting under a dynamically-resizing panel — reverted back
        /// to "panel_bg" (on explicit request) now that the panel uses a
        /// static height instead (fits exactly 10 modifiers, 2x5), which
        /// removes the resizing that caused the distortion in the first place.
        /// </summary>
        public static Sprite ModifierPanelBackground
        {
            get { return _modifierPanelBackground != null ? _modifierPanelBackground : (_modifierPanelBackground = Resources.Load<Sprite>(GameUIPath + "panel_bg")); }
        }

        /// <summary>The "card_bg_3" card art — background of each hand slot (HandView) and, tinted darker, of the shop's modifier/upgrade cards (ShopView, on explicit request: "pour le background des ''cartes'' dans le shop j'aimerais qu'on utilise card_bg_3.png teinté en plus foncé").</summary>
        public static Sprite CardBackground
        {
            get { return _cardBackground != null ? _cardBackground : (_cardBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_3")); }
        }

        /// <summary>Background for a primary "Choose"/confirm action button on a draft card.</summary>
        public static Sprite ChooseButtonBackground
        {
            get { return _chooseButtonBackground != null ? _chooseButtonBackground : (_chooseButtonBackground = Resources.Load<Sprite>(ButtonPath + "emptyButtons/blueButton")); }
        }

        /// <summary>Background for a "Cancel" button.</summary>
        public static Sprite CancelButtonBackground
        {
            get { return _cancelButtonBackground != null ? _cancelButtonBackground : (_cancelButtonBackground = Resources.Load<Sprite>(GameUIPath + "red_btn")); }
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
