using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Lazily-loaded, cached references to the "Colorful UI" asset pack (Assets/Resources/Colorful_UI/...).</summary>
    public static class UISprites
    {
        private const string SliderPath = "Colorful_UI/colorful/sprites/slider/";
        private const string GameUIPath = "Colorful_UI/colorful/sprites/gameUI/";
        private const string IconsPath = "Icons/";
        private const string ButtonsPath = "Buttons/";

        private static Sprite _barTrack;
        private static Sprite _scoreBarFill;
        private static Sprite _piecesBarFill;
        private static Sprite _cardBackground;
        private static Sprite _chooseButtonBackground;
        private static Sprite _cancelButtonBackground;
        private static Sprite _countBadge;
        private static Sprite _sfxBarFill;
        private static Sprite _sliderHandle;
        private static Sprite _handUtilityButtonBackground;
        private static Sprite _comboBackground;
        private static Sprite _slotGroupBackground;
        private static Sprite _shuffleIcon;

        /// <summary>Background image for the Show deck and Show rules buttons.</summary>
        public static Sprite HandUtilityButtonBackground
        {
            get
            {
                if (_handUtilityButtonBackground == null)
                {
                    _handUtilityButtonBackground = Resources.Load<Sprite>(ButtonsPath + "160x48");
                    if (_handUtilityButtonBackground != null)
                    {
                        _handUtilityButtonBackground.texture.filterMode = FilterMode.Bilinear;
                    }
                }
                return _handUtilityButtonBackground;
            }
        }

        /// <summary>Background art for the combo score panel.</summary>
        public static Sprite ComboBackground
        {
            get { return _comboBackground != null ? _comboBackground : (_comboBackground = Resources.Load<Sprite>(ButtonsPath + "BackgroundCombo")); }
        }

        /// <summary>Background image enclosing the three hand slots.</summary>
        public static Sprite SlotGroupBackground
        {
            get { return _slotGroupBackground != null ? _slotGroupBackground : (_slotGroupBackground = Resources.Load<Sprite>(ButtonsPath + "SlotGroup")); }
        }

        /// <summary>Round icon used by HandView's shuffle control.</summary>
        public static Sprite ShuffleIcon
        {
            get { return _shuffleIcon != null ? _shuffleIcon : (_shuffleIcon = Resources.Load<Sprite>(ButtonsPath + "Shuffle")); }
        }

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

        /// <summary>The "card_bg_3" card art — background of each hand slot (HandView) and, tinted darker, of the shop's modifier/upgrade cards (ShopView).</summary>
        public static Sprite CardBackground
        {
            get { return _cardBackground != null ? _cardBackground : (_cardBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_3")); }
        }

        /// <summary>
        /// Background for a primary "Choose"/confirm action button, used across every positive-action button
        /// in the game. A grayscale rounded-rect source tinted per-pixel against the button's own fill color,
        /// with a 9-slice border of 18/0/18/0 matching the asset's corner radius. Import filter mode is
        /// Bilinear rather than this project's usual Point, to keep the rounded corners smooth.
        /// </summary>
        public static Sprite ChooseButtonBackground
        {
            get { return _chooseButtonBackground != null ? _chooseButtonBackground : (_chooseButtonBackground = Resources.Load<Sprite>(IconsPath + "pill_button_blue")); }
        }

        /// <summary>Background for a "Cancel"/negative-action button — same flat-pill sprite family as <see cref="ChooseButtonBackground"/>, the Danger-red variant.</summary>
        public static Sprite CancelButtonBackground
        {
            get { return _cancelButtonBackground != null ? _cancelButtonBackground : (_cancelButtonBackground = Resources.Load<Sprite>(IconsPath + "pill_button_danger")); }
        }

        /// <summary>Small filled circle used as a corner badge showing a remaining-count number (HandView's Shuffle button).</summary>
        public static Sprite CountBadge
        {
            get { return _countBadge != null ? _countBadge : (_countBadge = Resources.Load<Sprite>(SliderPath + "Ellipse 19")); }
        }

        /// <summary>Fill for the SFX volume slider — a third color distinct from ScoreBarFill (Master)/PiecesBarFill (Music).</summary>
        public static Sprite SfxBarFill
        {
            get { return _sfxBarFill != null ? _sfxBarFill : (_sfxBarFill = Resources.Load<Sprite>(SliderPath + "greenBarFill")); }
        }

        /// <summary>Round knob for every UnityEngine.UI.Slider in SettingsView — same "Ellipse" family used for HandView's Shuffle count badge.</summary>
        public static Sprite SliderHandle
        {
            get { return _sliderHandle != null ? _sliderHandle : (_sliderHandle = Resources.Load<Sprite>(SliderPath + "Ellipse 20")); }
        }
    }
}
