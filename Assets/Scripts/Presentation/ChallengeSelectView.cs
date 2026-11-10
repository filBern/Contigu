using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Pre-run challenge picker shown at every launch and every "New Run" (see ChallengeCatalog/UnlockCatalog/
    /// UnlockCatalog). A single-card carousel, stepped left/right, rather than one card
    /// per entry laid out side by side, since its width stays constant regardless of how many challenges exist.
    /// One card's UI is built once and rebound to whichever entry is shown (see ShowChallengeAt). A locked
    /// challenge unlocks and plays in the same click once affordable — no separate confirm step.
    /// </summary>
    public sealed class ChallengeSelectView : MonoBehaviour
    {
        private static float CardWidth { get { return UITweaks.Current.challengeCardWidth; } }
        private static float CardHeight { get { return UITweaks.Current.challengeCardHeight; } }
        private const float CardY = -10f;
        private static float BannerHeight { get { return UITweaks.Current.challengeBannerHeight; } }
        private const float ArrowButtonSize = 76f;
        private const float ArrowGap = 28f;
        private const float PopDuration = 0.18f;
        private static readonly Color BackdropWood = new Color(0.15f, 0.10f, 0.08f, 1f);
        private static readonly Color BackdropStripe = new Color(0.20f, 0.13f, 0.10f, 1f);
        private static readonly Color AwningRed = new Color(0.78f, 0.25f, 0.22f, 1f);
        private static readonly Color AwningCream = new Color(0.96f, 0.90f, 0.76f, 1f);
        // Entrance fade — Show() can now fire slightly before the main menu's uncover sweep has fully
        // cleared the screen (see GameBootstrap.Awake), so this view needs its own visible transition
        // in rather than just snapping to fully opaque underneath still-vanishing tiles.
        private const float FadeInDuration = 0.3f;

        /// <summary>Dev/testing: the picker's "Unlock all" button was pressed (only built in the editor and development builds).</summary>
        public event Action UnlockAllRequested;

        /// <summary>Fired once a challenge is ready to play, already built at the chosen difficulty level.</summary>
        public event Action<ChallengeDefinition> ChallengeChosen;

        /// <summary>The "Continue" button: resume the saved run.</summary>
        public event Action ContinueRequested;

        /// <summary>The "Collection" button: open the collection of discoveries.</summary>
        public event Action CollectionRequested;
        private Button _continueButton;

        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
        private MetaStats _metaStats;
        private int _currentIndex;

        private Image _card;
        private Text _nameLabel;
        private Text _descriptionLabel;
        private Text _statusLabel;
        private Button _playButton;
        private Text _playButtonLabel;
        private bool _playArmed;
        private const string PledgeHeading = "Optional pledges: keep them all the way to the victory";
        private Text _pledgeTitle;
        private Pledge _selectedPledges;
        private int _selectedLevel = 1;
        private int _lastLevelChallenge = -1;
        private Text _levelLabel;
        private Button _levelDown;
        private Button _levelUp;
        private readonly Dictionary<Pledge, Image> _pledgeChips = new Dictionary<Pledge, Image>();

        /// <summary>The optional pledges ticked for the run about to start.</summary>
        public Pledge SelectedPledges { get { return _selectedPledges; } }
        private Image _banner;
        private Image _bannerIcon;
        private Text _statsLabel;
        private Image[] _dots;
        private Coroutine _popCoroutine;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "ChallengeSelectOverlay", BackdropWood);
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);
            _canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            // Same stall as the shop: dark wooden planks and a striped awning along the top.
            const int Planks = 9;
            for (int i = 0; i < Planks; i += 2)
            {
                var stripe = UIFactory.CreatePanel(_root, "Plank" + i, BackdropStripe);
                stripe.raycastTarget = false;
                stripe.rectTransform.anchorMin = new Vector2(i / (float)Planks, 0f);
                stripe.rectTransform.anchorMax = new Vector2((i + 1) / (float)Planks, 1f);
                stripe.rectTransform.offsetMin = Vector2.zero;
                stripe.rectTransform.offsetMax = Vector2.zero;
            }
            const int AwningStripes = 16;
            for (int i = 0; i < AwningStripes; i++)
            {
                var piece = UIFactory.CreatePanel(_root, "Awning" + i, i % 2 == 0 ? AwningRed : AwningCream);
                piece.raycastTarget = false;
                piece.rectTransform.anchorMin = new Vector2(i / (float)AwningStripes, 1f);
                piece.rectTransform.anchorMax = new Vector2((i + 1) / (float)AwningStripes, 1f);
                piece.rectTransform.pivot = new Vector2(0.5f, 1f);
                piece.rectTransform.offsetMin = new Vector2(0f, -22f);
                piece.rectTransform.offsetMax = Vector2.zero;
            }

            var header = UIFactory.CreateText(_root, "Header", "CHOOSE YOUR CHALLENGE", 44, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 56f);

            BuildCard(_root);
            BuildArrowButton(_root, "LeftArrow", "<", -(CardWidth / 2f + ArrowGap + ArrowButtonSize / 2f), Step(-1));
            BuildArrowButton(_root, "RightArrow", ">", CardWidth / 2f + ArrowGap + ArrowButtonSize / 2f, Step(1));

            // One dot per challenge under the card.
            int count = ChallengeCatalog.All.Length;
            _dots = new Image[count];
            const float DotSpacing = 26f;
            for (int i = 0; i < count; i++)
            {
                var dot = UIFactory.CreatePanel(_root, "Dot" + i, Color.white);
                dot.raycastTarget = false;
                dot.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                dot.rectTransform.anchoredPosition = new Vector2((i - (count - 1) / 2f) * DotSpacing, CardY - CardHeight / 2f - 24f);
                dot.rectTransform.sizeDelta = new Vector2(12f, 12f);
                _dots[i] = dot;
            }

            BuildPledgeRow(_card.transform);

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

        private const float PledgeChipWidth = 170f;
        private static readonly Color PledgeOff = new Color(0.62f, 0.62f, 0.62f, 1f);
        private static readonly Color PledgeOn = new Color(1f, 0.9f, 0.5f, 1f);
        private RectTransform _pledgeGroup;

        /// <summary>
        /// Optional pledges inside the card, above Play (2 x 2 chips): tick the rules you want to play by. Only shown once the default challenge has been won.
        /// </summary>
        private void BuildPledgeRow(Transform card)
        {
            _pledgeGroup = UIFactory.CreateUIObject("Pledges", card);
            UIFactory.StretchFull(_pledgeGroup);
            var title = UIFactory.CreateText(_pledgeGroup, "PledgeTitle", PledgeHeading, 14, UITheme.TextMuted);
            _pledgeTitle = title;
            title.raycastTarget = false;
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            title.rectTransform.pivot = new Vector2(0.5f, 0f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 204f);
            title.rectTransform.sizeDelta = new Vector2(CardWidth - 30f, 24f);
            int n = PledgeCatalog.All.Length;
            for (int i = 0; i < n; i++)
            {
                var pledge = PledgeCatalog.All[i];
                var chip = UIFactory.CreateButton(_pledgeGroup, "Pledge" + pledge, PledgeCatalog.Name(pledge), UISprites.ChooseButtonBackground, 16);
                UIFactory.DisablePlants(chip);
                var rect = chip.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                float height = MobileSetup.TouchMin(38f);
                rect.sizeDelta = new Vector2(PledgeChipWidth, height);
                int column = i % 2;
                int row = i / 2;
                rect.anchoredPosition = new Vector2((column == 0 ? -1f : 1f) * (PledgeChipWidth * 0.5f + 6f), 118f + (1 - row) * (height + 6f));
                var image = chip.GetComponent<Image>();
                _pledgeChips[pledge] = image;
                chip.onClick.AddListener(() =>
                {
                    _selectedPledges ^= pledge;
                    RefreshPledgeChips();
                    // The rule of the pledge just touched replaces the heading until the next visit.
                    _pledgeTitle.text = PledgeCatalog.Name(pledge) + ": " + PledgeCatalog.Description(pledge) + ((_selectedPledges & pledge) != Pledge.None ? "  (on)" : "  (off)");
                });
            }
            RefreshPledgeChips();
        }

        private void RefreshPledgeChips()
        {
            foreach (var chip in _pledgeChips)
            {
                bool on = (_selectedPledges & chip.Key) != Pledge.None;
                chip.Value.color = on ? PledgeOn : Color.white;
            }
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

            // A banner in the challenge's own color carries its name.
            _banner = UIFactory.CreatePanel(_card.transform, "Banner", Color.white);
            _banner.raycastTarget = false;
            _banner.rectTransform.anchorMin = new Vector2(0f, 1f);
            _banner.rectTransform.anchorMax = new Vector2(1f, 1f);
            _banner.rectTransform.pivot = new Vector2(0.5f, 1f);
            _banner.rectTransform.offsetMin = new Vector2(6f, -BannerHeight - 6f);
            _banner.rectTransform.offsetMax = new Vector2(-6f, -6f);

            _bannerIcon = UIFactory.CreatePanel(_banner.transform, "SeasonIcon", Color.white);
            _bannerIcon.raycastTarget = false;
            _bannerIcon.preserveAspect = true;
            _bannerIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _bannerIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _bannerIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            _bannerIcon.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            _bannerIcon.rectTransform.sizeDelta = new Vector2(BannerHeight - 12f, BannerHeight - 12f);

            _nameLabel = UIFactory.CreateText(_banner.transform, "Name", "", 38, UITheme.TextOnBackground);
            _nameLabel.raycastTarget = false;
            UIFactory.StretchFull(_nameLabel.rectTransform);

            _statsLabel = UIFactory.CreateText(_card.transform, "Stats", "", 18, UITheme.TextPrimary);
            _statsLabel.raycastTarget = false;
            _statsLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _statsLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _statsLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _statsLabel.rectTransform.anchoredPosition = new Vector2(0f, -BannerHeight - 20f);
            _statsLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 28f);

            // The difficulty level: winning a challenge at level N opens level N+1.
            _levelLabel = UIFactory.CreateText(_card.transform, "Level", "", 17, UITheme.TextPrimary);
            _levelLabel.raycastTarget = false;
            _levelLabel.rectTransform.anchorMin = _levelLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _levelLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _levelLabel.rectTransform.anchoredPosition = new Vector2(0f, -BannerHeight - 58f);
            _levelLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 170f, 34f);
            _levelDown = BuildLevelButton(_card.transform, "LevelDown", "-", -(CardWidth / 2f - 46f), -1);
            _levelUp = BuildLevelButton(_card.transform, "LevelUp", "+", CardWidth / 2f - 46f, 1);

            _descriptionLabel = UIFactory.CreateText(_card.transform, "Description", "", 19, UITheme.TextMuted);
            _descriptionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _descriptionLabel.rectTransform.anchoredPosition = new Vector2(0f, -BannerHeight - 100f);
            _descriptionLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 40f, 96f);

            // Coral rather than gold: gold has poor contrast against this card's light fill, and coral already reads as "blocked" elsewhere (see UITheme.Danger/HoverInvalid).
            _statusLabel = UIFactory.CreateText(_card.transform, "Status", "", 16, UITheme.Danger);
            _statusLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            _statusLabel.rectTransform.anchoredPosition = new Vector2(0f, 84f);
            _statusLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 26f);

            _playButton = UIFactory.CreateButton(_card.transform, "Play", "", UISprites.Garden("Gold") ?? UISprites.ChooseButtonBackground, 26);
            var playRect = _playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(0f, 22f);
            playRect.sizeDelta = new Vector2(CardWidth - 60f, 56f);
            _playButtonLabel = _playButton.GetComponentInChildren<Text>();
            _playButton.onClick.AddListener(OnPlayClicked);

            // Under the card: resume the run saved by Save and quit (or the start of the last round), shown only when there is one.
            _continueButton = UIFactory.CreateButton(_root, "Continue", "Continue saved run", UISprites.ChooseButtonBackground, 18);
            var continueRect = _continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0f);
            continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0f, 36f);
            continueRect.sizeDelta = new Vector2(300f, MobileSetup.TouchMin(50f));
            _continueButton.onClick.AddListener(() => { if (ContinueRequested != null) { ContinueRequested(); } });
            _continueButton.gameObject.SetActive(false);

            var collectionButton = UIFactory.CreateButton(_root, "Collection", "Collection", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 22);
            var collectionRect = collectionButton.GetComponent<RectTransform>();
            collectionRect.anchorMin = new Vector2(0f, 1f);
            collectionRect.anchorMax = new Vector2(0f, 1f);
            collectionRect.pivot = new Vector2(0f, 1f);
            collectionRect.anchoredPosition = new Vector2(30f, -44f);
            collectionRect.sizeDelta = new Vector2(210f, 54f);
            collectionButton.onClick.AddListener(() => { if (CollectionRequested != null) { CollectionRequested(); } });
        }

        private Button BuildLevelButton(Transform card, string name, string label, float x, int direction)
        {
            var button = UIFactory.CreateButton(card, name, label, UISprites.ChooseButtonBackground, 26);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            float size = MobileSetup.TouchMin(44f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, -BannerHeight - 52f);
            button.onClick.AddListener(() =>
            {
                SfxManager.Play(SfxId.CarouselTick);
                _selectedLevel += direction;
                RefreshLevelRow(ChallengeCatalog.All[_currentIndex]);
            });
            return button;
        }

        /// <summary>The level row: "LEVEL 3 / 3 - what it adds", with - and + to pick any level up to the highest one the player may play.</summary>
        private void RefreshLevelRow(ChallengeDefinition challenge)
        {
            int highest = _metaStats != null ? _metaStats.HighestPlayableLevel(challenge.Id) : 1;
            _selectedLevel = Mathf.Clamp(_selectedLevel, 1, highest);
            bool unlocked = _metaStats != null && _metaStats.IsUnlocked(challenge.Id);
            _levelLabel.gameObject.SetActive(unlocked);
            _levelDown.gameObject.SetActive(unlocked && highest > 1);
            _levelUp.gameObject.SetActive(unlocked && highest > 1);
            _levelDown.interactable = _selectedLevel > 1;
            _levelUp.interactable = _selectedLevel < highest;
            _levelLabel.text = "LEVEL " + _selectedLevel + (highest > 1 ? " / " + highest : "") + "   -   " + DifficultyLevels.Describe(_selectedLevel);
        }

        private void BuildArrowButton(Transform parent, string name, string label, float x, Action onClick)
        {
            var button = UIFactory.CreateButton(parent, name, label, UISprites.Garden("Blue") ?? UISprites.ChooseButtonBackground, 36);
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
            // A new run replaces the saved one: ask once, right on the button, before throwing it away.
            if (RunSaveStore.Exists && !_playArmed)
            {
                _playArmed = true;
                _playButtonLabel.text = "Replace save?";
                return;
            }
            _playArmed = false;
            ChallengeChosen?.Invoke(ChallengeCatalog.All[_currentIndex].AtLevel(_selectedLevel));
        }

        public void Show(MetaStats metaStats)
        {
            SfxManager.Play(SfxId.Overlay);
            _metaStats = metaStats;
            _lastLevelChallenge = -1;
            ShowChallengeAt(0);
            _selectedPledges = Pledge.None;
            RefreshPledgeChips();
            if (_pledgeTitle != null) { _pledgeTitle.text = PledgeHeading; }
            // Pledges are a reward for having won the default challenge once.
            if (_pledgeGroup != null) { _pledgeGroup.gameObject.SetActive(metaStats != null && metaStats.ClassicVictory); }
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

        /// <summary>Re-renders the currently shown card's lock state/cost/button label against what is unlocked now, so a re-opened screen (via a later "New Run") always reflects the latest progress.</summary>
        public void Refresh(MetaStats metaStats)
        {
            _metaStats = metaStats;
            RefreshCurrentCard();
        }

        /// <summary>Moves the carousel to <paramref name="index"/>, wrapping at either end. C#'s % can return a negative result for a negative dividend, so the length is added back before the final mod.</summary>
        private void ShowChallengeAt(int index)
        {
            int count = ChallengeCatalog.All.Length;
            bool moved = _currentIndex != ((index % count) + count) % count;
            _currentIndex = ((index % count) + count) % count;
            RefreshCurrentCard();
            if (moved && !MotionSettings.Reduced && _root.gameObject.activeInHierarchy)
            {
                if (_popCoroutine != null)
                {
                    StopCoroutine(_popCoroutine);
                }
                _popCoroutine = StartCoroutine(PopRoutine());
            }
        }

        internal static Color AccentFor(ChallengeDefinition challenge)
        {
            string name = challenge.Name.ToLowerInvariant();
            if (name.Contains("spring")) return new Color(0.36f, 0.66f, 0.28f);
            if (name.Contains("winter")) return new Color(0.36f, 0.62f, 0.86f);
            if (name.Contains("summer")) return new Color(0.92f, 0.58f, 0.16f);
            if (name.Contains("autumn")) return new Color(0.76f, 0.36f, 0.16f);
            if (name.Contains("orchard")) return new Color(0.78f, 0.24f, 0.30f);
            if (name.Contains("greenhouse")) return new Color(0.24f, 0.66f, 0.60f);
            return new Color(0.55f, 0.38f, 0.72f);
        }

        private static string StatsFor(ChallengeDefinition challenge)
        {
            string rounds = challenge.RoundCount + " ROUNDS";
            return rounds + "   -   " + challenge.GridSize + "x" + challenge.GridSize + "   -   " + challenge.ActiveColorCount + " COLORS";
        }

        /// <summary>The card pops in whenever the carousel moves, so stepping feels alive.</summary>
        private System.Collections.IEnumerator PopRoutine()
        {
            float t = 0f;
            var rect = _card.rectTransform;
            while (t < PopDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PopDuration);
                float scale = Mathf.Lerp(0.93f, 1f, 1f - (1f - k) * (1f - k));
                rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
            _popCoroutine = null;
        }

        private void RefreshCurrentCard()
        {
            if (_metaStats == null)
            {
                return;
            }

            _playArmed = false;
            var challenge = ChallengeCatalog.All[_currentIndex];
            _nameLabel.text = challenge.Name.ToUpperInvariant();
            _descriptionLabel.text = challenge.Description;
            var accent = AccentFor(challenge);
            _banner.color = accent;
            var icon = Resources.Load<Sprite>("Icons/Challenges/" + challenge.Name);
            _bannerIcon.sprite = icon;
            _bannerIcon.enabled = icon != null;
            // With an icon the name sits in the rest of the banner, centered there.
            _nameLabel.rectTransform.offsetMin = new Vector2(icon != null ? BannerHeight : 0f, 0f);
            _nameLabel.rectTransform.offsetMax = Vector2.zero;
            _statsLabel.text = StatsFor(challenge);
            for (int i = 0; _dots != null && i < _dots.Length; i++)
            {
                bool active = i == _currentIndex;
                _dots[i].color = active ? accent : new Color(1f, 1f, 1f, 0.35f);
                _dots[i].rectTransform.sizeDelta = active ? new Vector2(18f, 18f) : new Vector2(12f, 12f);
            }

            bool unlocked = _metaStats.IsUnlocked(challenge.Id);
            // Starting level: the highest one the player may play on this challenge.
            if (_lastLevelChallenge != _currentIndex)
            {
                _selectedLevel = _metaStats.HighestPlayableLevel(challenge.Id);
                _lastLevelChallenge = _currentIndex;
            }
            RefreshLevelRow(challenge);
            if (unlocked)
            {
                _statusLabel.text = "";
                _playButtonLabel.text = "Play";
                _playButton.interactable = true;
            }
            else
            {
                var entry = UnlockCatalog.Find(UnlockKind.Challenge, (int)challenge.Id);
                _statusLabel.text = "Locked - " + (entry != null ? entry.Condition.Text : "play more to unlock it");
                _playButtonLabel.text = "Locked";
                _playButton.interactable = false;
            }
        }
    }
}
