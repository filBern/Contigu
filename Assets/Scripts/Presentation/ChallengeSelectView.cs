using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Pre-run challenge picker shown at every launch and every "New Run" (see ChallengeCatalog/MetaStats.Stars/
    /// MetaStatsRecorder.TryUnlockChallenge). A single-card carousel, stepped left/right, rather than one card
    /// per entry laid out side by side, since its width stays constant regardless of how many challenges exist.
    /// One card's UI is built once and rebound to whichever entry is shown (see ShowChallengeAt). A locked
    /// challenge unlocks and plays in the same click once affordable — no separate confirm step.
    /// </summary>
    public sealed class ChallengeSelectView : MonoBehaviour
    {
        private const float CardWidth = 280f;
        private const float CardHeight = 320f;
        private const float CardY = -20f;
        private const float ArrowButtonSize = 56f;
        private const float ArrowGap = 20f;
        // Entrance fade — Show() can now fire slightly before the main menu's uncover sweep has fully
        // cleared the screen (see GameBootstrap.Awake), so this view needs its own visible transition
        // in rather than just snapping to fully opaque underneath still-vanishing tiles.
        private const float FadeInDuration = 0.3f;

        /// <summary>Dev/testing: the picker's "Unlock all" button was pressed (only built in the editor and development builds).</summary>
        public event Action UnlockAllRequested;

        /// <summary>Fired once a challenge is actually ready to play — after any auto-unlock spend already happened and was persisted by the caller.</summary>
        public event Action<ChallengeDefinition> ChallengeChosen;

        /// <summary>The "Continue" button: resume the saved run.</summary>
        public event Action ContinueRequested;

        /// <summary>The "Collection" button: open the collection of discoveries.</summary>
        public event Action CollectionRequested;
        private Button _continueButton;

        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
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
            _canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD || CONTIGU_DEBUG
            var unlockAll = UIFactory.CreateButton(_root, "UnlockAll", "Unlock all (dev)", UITheme.PanelLight, 14);
            var unlockRect = unlockAll.GetComponent<RectTransform>();
            unlockRect.anchorMin = new Vector2(0f, 0f);
            unlockRect.anchorMax = new Vector2(0f, 0f);
            unlockRect.pivot = new Vector2(0f, 0f);
            unlockRect.anchoredPosition = new Vector2(24f, 24f);
            unlockRect.sizeDelta = new Vector2(190f, 38f);
            unlockAll.onClick.AddListener(() => { if (UnlockAllRequested != null) UnlockAllRequested(); });
#endif

            _root.gameObject.SetActive(false);
            return _root;
        }

        private void BuildCard(Transform parent)
        {
            _card = UIFactory.CreateSlicedImage(parent, "Card", UISprites.CardBackground);
            _card.color = UITheme.Panel;
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

            _descriptionLabel = UIFactory.CreateText(_card.transform, "Description", "", 17, UITheme.TextMuted);
            _descriptionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            _descriptionLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 28f, 150f);

            // Coral rather than gold: gold has poor contrast against this card's light fill, and coral already reads as "blocked" elsewhere (see UITheme.Danger/HoverInvalid).
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

            // Under the card: resume the run saved by Save and quit (or the start of the last round), shown only when there is one.
            _continueButton = UIFactory.CreateButton(_root, "Continue", "Continue saved run", UISprites.ChooseButtonBackground, 18);
            var continueRect = _continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0f);
            continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0f, 36f);
            continueRect.sizeDelta = new Vector2(300f, 50f);
            _continueButton.onClick.AddListener(() => { if (ContinueRequested != null) { ContinueRequested(); } });
            _continueButton.gameObject.SetActive(false);

            var collectionButton = UIFactory.CreateButton(_root, "Collection", "Collection", UITheme.PanelLight, 18);
            var collectionRect = collectionButton.GetComponent<RectTransform>();
            collectionRect.anchorMin = new Vector2(0f, 1f);
            collectionRect.anchorMax = new Vector2(0f, 1f);
            collectionRect.pivot = new Vector2(0f, 1f);
            collectionRect.anchoredPosition = new Vector2(30f, -30f);
            collectionRect.sizeDelta = new Vector2(180f, 44f);
            collectionButton.GetComponentInChildren<Text>().color = UITheme.TextPrimary;
            collectionButton.onClick.AddListener(() => { if (CollectionRequested != null) { CollectionRequested(); } });
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
            _continueButton.gameObject.SetActive(RunSaveStore.Exists);
            _root.gameObject.SetActive(true);
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _canvasGroup.alpha = 0f;
            _fadeCoroutine = StartCoroutine(FadeInRoutine());
        }

        private System.Collections.IEnumerator FadeInRoutine()
        {
            float t = 0f;
            while (t < FadeInDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(t / FadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
            _fadeCoroutine = null;
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

        /// <summary>Moves the carousel to <paramref name="index"/>, wrapping at either end. C#'s % can return a negative result for a negative dividend, so the length is added back before the final mod.</summary>
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
