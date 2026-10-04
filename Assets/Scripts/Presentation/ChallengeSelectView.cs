using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Pre-run challenge picker (spec extension, explicit request: "Meta
    /// progression avec différents challenge qui offrent différents boss
    /// et état de depart", "Débloqués progressivement" — see
    /// ChallengeCatalog/MetaStats.Stars/MetaStatsRecorder.
    /// TryUnlockChallenge). Shown at every launch and every "New Run" (not
    /// just the first ever session) — the game's first real pre-run menu,
    /// since it previously just booted straight into a Classic run.
    ///
    /// A single-card carousel (explicit request: "j'aimerais qu'on fasse un
    /// caroussel avec les challenges au cas ou je finis par en avoir
    /// plusieurs", with 2 buttons to step left/right) rather than one card
    /// per ChallengeCatalog entry laid out side by side — that fixed-width
    /// row would only get more cramped as more challenges are added, while
    /// a carousel's own width never depends on how many there are. One
    /// card's worth of UI is built once and rebound to whichever entry is
    /// currently shown (see ShowChallengeAt). Classic always plays
    /// immediately; Marathon/Chaos show their Stars cost while locked and
    /// unlock-then-play in the same click once affordable — no separate
    /// confirm step, since spending Stars here is never a mistake a player
    /// needs protecting from (it only ever buys permanent access, never
    /// removes anything).
    /// </summary>
    public sealed class ChallengeSelectView : MonoBehaviour
    {
        private const float CardWidth = 280f;
        private const float CardHeight = 320f;
        private const float CardY = -20f;
        private const float ArrowButtonSize = 56f;
        private const float ArrowGap = 20f;

        /// <summary>Fired once a challenge is actually ready to play — after any auto-unlock spend already happened and was persisted by the caller.</summary>
        public event Action<ChallengeDefinition> ChallengeChosen;

        private RectTransform _root;
        private Text _starsLabel;
        private MetaStats _metaStats;
        private int _currentIndex;

        private Image _card;
        private Text _nameLabel;
        private Text _descriptionLabel;
        private Text _statusLabel;
        private Button _playButton;
        private Text _playButtonLabel;
        private Text _pageLabel;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "ChallengeSelectOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "CHOOSE YOUR CHALLENGE", 30, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 44f);

            _starsLabel = UIFactory.CreateText(_root, "Stars", "", 18, VisualDefaults.GoldenColor);
            _starsLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _starsLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _starsLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _starsLabel.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            _starsLabel.rectTransform.sizeDelta = new Vector2(1000f, 28f);

            BuildCard(_root);
            BuildArrowButton(_root, "LeftArrow", "<", -(CardWidth / 2f + ArrowGap + ArrowButtonSize / 2f), Step(-1));
            BuildArrowButton(_root, "RightArrow", ">", CardWidth / 2f + ArrowGap + ArrowButtonSize / 2f, Step(1));

            _pageLabel = UIFactory.CreateText(_root, "Page", "", 16, UITheme.TextMutedOnBackground);
            _pageLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _pageLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _pageLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _pageLabel.rectTransform.anchoredPosition = new Vector2(0f, CardY - CardHeight / 2f - 12f);
            _pageLabel.rectTransform.sizeDelta = new Vector2(200f, 24f);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private void BuildCard(Transform parent)
        {
            _card = UIFactory.CreateSlicedImage(parent, "Card", UISprites.CardBackground);
            _card.color = UITheme.Panel; // same cream card fill ShopView's own cards use — TextPrimary/TextMuted below stay dark, since UITheme.Panel is light in the current DA.
            UIFactory.AddThickOutline(_card, UITheme.Border);
            _card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            _card.rectTransform.anchoredPosition = new Vector2(0f, CardY);

            _nameLabel = UIFactory.CreateText(_card.transform, "Name", "", 22, UITheme.TextPrimary);
            _nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            _nameLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 32f);

            _descriptionLabel = UIFactory.CreateText(_card.transform, "Description", "", 14, UITheme.TextMuted);
            _descriptionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            _descriptionLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 28f, 150f);

            // Coral, not the gold used for the Stars readout above (explicit
            // report: "le texte Locked n'est pas lisible en jaune") — gold
            // reads fine on the dark overlay behind the Stars label, but has
            // poor contrast against this card's own light cream fill; coral
            // already reads as "blocked/invalid" everywhere else in the DA
            // (see UITheme.Danger/HoverInvalid).
            _statusLabel = UIFactory.CreateText(_card.transform, "Status", "", 15, UITheme.Danger);
            _statusLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            _statusLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 24f);

            _playButton = UIFactory.CreateButton(_card.transform, "Play", "", UISprites.ChooseButtonBackground, 16);
            var playRect = _playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(0f, 20f);
            playRect.sizeDelta = new Vector2(CardWidth - 40f, 40f);
            _playButtonLabel = _playButton.GetComponentInChildren<Text>();
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void BuildArrowButton(Transform parent, string name, string label, float x, Action onClick)
        {
            var button = UIFactory.CreateButton(parent, name, label, UITheme.PanelLight, 22);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, CardY);
            rect.sizeDelta = new Vector2(ArrowButtonSize, ArrowButtonSize);
            button.onClick.AddListener(() => onClick());
        }

        /// <summary>Curries the step direction (-1 left, +1 right) into a plain Action, since UIFactory.CreateButton's onClick has no parameters to pass one through directly.</summary>
        private Action Step(int direction)
        {
            return () =>
            {
                SfxManager.Play(SfxId.CarouselTick);
                ShowChallengeAt(_currentIndex + direction);
            };
        }

        private void OnPlayClicked()
        {
            ChallengeChosen?.Invoke(ChallengeCatalog.All[_currentIndex]);
        }

        public void Show(MetaStats metaStats)
        {
            SfxManager.Play(SfxId.Overlay);
            _metaStats = metaStats;
            ShowChallengeAt(0);
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Re-renders the currently shown card's lock state/cost/button label against the current Stars balance — called right after a successful unlock-and-play, so a re-opened screen (via a later "New Run") always reflects the latest balance.</summary>
        public void Refresh(MetaStats metaStats)
        {
            _metaStats = metaStats;
            RefreshCurrentCard();
        }

        /// <summary>
        /// Moves the carousel to <paramref name="index"/>, wrapping around
        /// at either end (explicit request's own "carousel" framing implies
        /// endless stepping, not a dead stop at the first/last entry) —
        /// C#'s % can return a negative result for a negative dividend, so
        /// this adds the length back in before the final mod rather than
        /// using % directly on a possibly-negative index.
        /// </summary>
        private void ShowChallengeAt(int index)
        {
            int count = ChallengeCatalog.All.Length;
            _currentIndex = ((index % count) + count) % count;
            RefreshCurrentCard();
        }

        private void RefreshCurrentCard()
        {
            if (_metaStats == null)
            {
                return;
            }

            _starsLabel.text = "Stars: " + _metaStats.Stars;

            var challenge = ChallengeCatalog.All[_currentIndex];
            _nameLabel.text = challenge.Name.ToUpperInvariant();
            _descriptionLabel.text = challenge.Description;
            _pageLabel.text = (_currentIndex + 1) + " / " + ChallengeCatalog.All.Length;

            bool unlocked = _metaStats.IsUnlocked(challenge.Id);
            if (unlocked)
            {
                _statusLabel.text = "";
                _playButtonLabel.text = "Play";
                _playButton.interactable = true;
            }
            else
            {
                bool affordable = _metaStats.Stars >= challenge.UnlockCost;
                _statusLabel.text = affordable
                    ? "Locked — " + challenge.UnlockCost + " Stars to unlock"
                    : "Locked — needs " + challenge.UnlockCost + " Stars (you have " + _metaStats.Stars + ")";
                _playButtonLabel.text = affordable ? "Unlock & Play" : "Locked";
                _playButton.interactable = affordable;
            }
        }
    }
}
