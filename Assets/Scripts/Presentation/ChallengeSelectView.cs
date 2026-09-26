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
    /// One card per ChallengeCatalog entry. Classic always plays
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
        private const float CardGap = 28f;

        /// <summary>Fired once a challenge is actually ready to play — after any auto-unlock spend already happened and was persisted by the caller.</summary>
        public event Action<ChallengeDefinition> ChallengeChosen;

        private RectTransform _root;
        private Text _starsLabel;
        private ChallengeCard[] _cards;

        private sealed class ChallengeCard
        {
            public ChallengeDefinition Challenge;
            public Text StatusLabel;
            public Button PlayButton;
            public Text PlayButtonLabel;
        }

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "ChallengeSelectOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "CHOOSE YOUR CHALLENGE", 30, UITheme.TextPrimary);
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

            _cards = new ChallengeCard[ChallengeCatalog.All.Length];
            float totalWidth = _cards.Length * CardWidth + (_cards.Length - 1) * CardGap;
            float startX = -totalWidth / 2f + CardWidth / 2f;
            for (int i = 0; i < _cards.Length; i++)
            {
                float x = startX + i * (CardWidth + CardGap);
                _cards[i] = BuildCard(_root, ChallengeCatalog.All[i], x);
            }

            _root.gameObject.SetActive(false);
            return _root;
        }

        private ChallengeCard BuildCard(Transform parent, ChallengeDefinition challenge, float x)
        {
            var card = UIFactory.CreateSlicedImage(parent, "Card_" + challenge.Id, UISprites.CardBackground);
            card.color = UITheme.Panel; // card_bg_3 tinted darker (on explicit report — the default white tint read as a pale lavender card, too light against the dark overlay), same tint ShopView's own cards already use.
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            card.rectTransform.anchoredPosition = new Vector2(x, -20f);

            var name = UIFactory.CreateText(card.transform, "Name", challenge.Name.ToUpperInvariant(), 22, UITheme.TextPrimary);
            name.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            name.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            name.rectTransform.pivot = new Vector2(0.5f, 1f);
            name.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            name.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 32f);

            var description = UIFactory.CreateText(card.transform, "Description", challenge.Description, 14, UITheme.TextMuted);
            description.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            description.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            description.rectTransform.pivot = new Vector2(0.5f, 1f);
            description.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            description.rectTransform.sizeDelta = new Vector2(CardWidth - 28f, 150f);

            var statusLabel = UIFactory.CreateText(card.transform, "Status", "", 15, VisualDefaults.GoldenColor);
            statusLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            statusLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            statusLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            statusLabel.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            statusLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, 24f);

            var playButton = UIFactory.CreateButton(card.transform, "Play", "", UISprites.ChooseButtonBackground, 16);
            var playRect = playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(0f, 20f);
            playRect.sizeDelta = new Vector2(CardWidth - 40f, 40f);
            var playButtonLabel = playButton.GetComponentInChildren<Text>();
            playButton.onClick.AddListener(() => OnCardClicked(challenge));

            return new ChallengeCard
            {
                Challenge = challenge,
                StatusLabel = statusLabel,
                PlayButton = playButton,
                PlayButtonLabel = playButtonLabel
            };
        }

        private void OnCardClicked(ChallengeDefinition challenge)
        {
            ChallengeChosen?.Invoke(challenge);
        }

        public void Show(MetaStats metaStats)
        {
            Refresh(metaStats);
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Re-renders every card's lock state/cost/button label against the current Stars balance — called on Show and again right after a successful unlock-and-play, so a re-opened screen (via a later "New Run") always reflects the latest balance.</summary>
        public void Refresh(MetaStats metaStats)
        {
            _starsLabel.text = "Stars: " + metaStats.Stars;

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                bool unlocked = metaStats.IsUnlocked(card.Challenge.Id);
                if (unlocked)
                {
                    card.StatusLabel.text = "";
                    card.PlayButtonLabel.text = "Play";
                }
                else
                {
                    bool affordable = metaStats.Stars >= card.Challenge.UnlockCost;
                    card.StatusLabel.text = affordable
                        ? "Locked — " + card.Challenge.UnlockCost + " Stars to unlock"
                        : "Locked — needs " + card.Challenge.UnlockCost + " Stars (you have " + metaStats.Stars + ")";
                    card.PlayButtonLabel.text = affordable ? "Unlock & Play" : "Locked";
                    card.PlayButton.interactable = affordable;
                }
            }
        }
    }
}
