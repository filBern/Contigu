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
        private static Sprite _modifierHeaderBanner;
        private static Sprite _handSlotBackground;
        private static Sprite _upgradeCardBackground;
        private static Sprite _upgradeNameBanner;
        private static Sprite _chooseButtonBackground;
        private static Sprite _cancelButtonBackground;

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
        /// Was "panel_bg" — swapped to "card_bg_2" (on explicit request) because
        /// panel_bg's art bakes a distinct-looking header band into its 9-slice
        /// borders, which visibly stretched/squished as the panel's height
        /// changed with the active modifier count. card_bg_2 is a plain card
        /// with no such baked-in band, so it stretches cleanly at any height —
        /// the "Modifiers" title gets its own separate banner instead (see
        /// ModifierHeaderBanner), the same way an upgrade card's name sits on
        /// its own Union banner rather than being part of the card art.
        /// </summary>
        public static Sprite ModifierPanelBackground
        {
            get { return _modifierPanelBackground != null ? _modifierPanelBackground : (_modifierPanelBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_2")); }
        }

        /// <summary>Ribbon-shaped banner sat behind the modifier panel's "Modifiers" title — same sprite as UpgradeNameBanner, kept as its own property since the two are conceptually different call sites.</summary>
        public static Sprite ModifierHeaderBanner
        {
            get { return _modifierHeaderBanner != null ? _modifierHeaderBanner : (_modifierHeaderBanner = Resources.Load<Sprite>(GameUIPath + "Union")); }
        }

        /// <summary>Background of each hand slot (HandView).</summary>
        public static Sprite HandSlotBackground
        {
            get { return _handSlotBackground != null ? _handSlotBackground : (_handSlotBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_3")); }
        }

        /// <summary>Background of each upgrade-draft card (DraftView) — same sprite as HandSlotBackground, kept as its own property since the two are conceptually different call sites.</summary>
        public static Sprite UpgradeCardBackground
        {
            get { return _upgradeCardBackground != null ? _upgradeCardBackground : (_upgradeCardBackground = Resources.Load<Sprite>(GameUIPath + "card_bg_3")); }
        }

        /// <summary>Ribbon-shaped banner sat behind an upgrade card's name label.</summary>
        public static Sprite UpgradeNameBanner
        {
            get { return _upgradeNameBanner != null ? _upgradeNameBanner : (_upgradeNameBanner = Resources.Load<Sprite>(GameUIPath + "Union")); }
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
    }
}
