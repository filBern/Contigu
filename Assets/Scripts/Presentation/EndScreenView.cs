using System;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Full-screen victory/defeat overlay with a restart button — on Victory, also an "Endless" button (explicit request: "j'aimerais que le joueur ait l'option d'aller en endless mode... pour continuer sa run ou de retourner au menu") to keep the same run going past its scheduled last round instead of ending it.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        public event Action RestartRequested;

        /// <summary>Fires only from the Victory screen's "Continue" button — never shown/wired on Defeat, since there's no scheduled run left to extend once the player has actually lost.</summary>
        public event Action ContinueEndlessRequested;

        private RectTransform _root;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _subtitleText;
        private UnityEngine.UI.Text _metaStatsText;
        private UnityEngine.UI.Button _continueButton;
        private RectTransform _restartButtonRect;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "EndScreen", new Color(0.04f, 0.04f, 0.06f, 0.95f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            _titleText = UIFactory.CreateText(_root, "Title", "", 42, UITheme.TextPrimary);
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 0.6f);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            _titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _titleText.rectTransform.sizeDelta = new Vector2(700f, 80f);

            _subtitleText = UIFactory.CreateText(_root, "Subtitle", "", 28, UITheme.TextOnBackground);
            _subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 0.48f);
            _subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.48f);
            _subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(700f, 50f);

            _metaStatsText = UIFactory.CreateText(_root, "MetaStats", "", 20, UITheme.TextMutedOnBackground);
            _metaStatsText.rectTransform.anchorMin = new Vector2(0.5f, 0.40f);
            _metaStatsText.rectTransform.anchorMax = new Vector2(0.5f, 0.40f);
            _metaStatsText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _metaStatsText.rectTransform.sizeDelta = new Vector2(700f, 60f);

            // Side by side (same -120/+120 spacing as ShopView's Reroll/
            // Leave pair) once Victory has something to offer besides
            // restarting — Continue on the left, New Run on the right.
            _continueButton = UIFactory.CreateButton(_root, "Continue", "Continue (Endless)", UISprites.ChooseButtonBackground, 18);
            var continueRect = _continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0.28f);
            continueRect.anchorMax = new Vector2(0.5f, 0.28f);
            continueRect.pivot = new Vector2(0.5f, 0.5f);
            continueRect.anchoredPosition = new Vector2(-120f, 0f);
            continueRect.sizeDelta = new Vector2(220f, 52f);
            _continueButton.onClick.AddListener(() =>
            {
                if (ContinueEndlessRequested != null) ContinueEndlessRequested();
            });

            var restartBtn = UIFactory.CreateButton(_root, "Restart", "New Run", UISprites.ChooseButtonBackground, 18);
            var rect = restartBtn.GetComponent<RectTransform>();
            _restartButtonRect = rect;
            rect.anchorMin = new Vector2(0.5f, 0.28f);
            rect.anchorMax = new Vector2(0.5f, 0.28f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120f, 0f);
            rect.sizeDelta = new Vector2(220f, 52f);
            restartBtn.onClick.AddListener(() =>
            {
                if (RestartRequested != null) RestartRequested();
            });

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void ShowVictory(int totalScore, MetaStats metaStats, bool isNewBestScore)
        {
            _titleText.text = "Victory!";
            _titleText.color = UITheme.Success;
            _subtitleText.text = "Run complete — total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore);
            _continueButton.gameObject.SetActive(true);
            // Side by side with Continue — see Build's own -120/+120 comment.
            _restartButtonRect.anchoredPosition = new Vector2(120f, 0f);
            _root.gameObject.SetActive(true);
        }

        public void ShowDefeat(int roundNumber, int totalScore, MetaStats metaStats, bool isNewBestScore)
        {
            _titleText.text = "Defeat — Round " + roundNumber;
            _titleText.color = UITheme.Danger;
            _subtitleText.text = "Quota not reached — total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore);
            _continueButton.gameObject.SetActive(false);
            // No Continue button on Defeat (there's no scheduled run left to
            // extend), so New Run is the only button here — center it
            // instead of leaving it offset where it sat next to Continue
            // (explicit request: "Si on a perdu la partie, le bouton new
            // run doit être centré horizontalement").
            _restartButtonRect.anchoredPosition = new Vector2(0f, 0f);
            _root.gameObject.SetActive(true);
        }

        /// <summary>
        /// Lightweight meta-progression (explicit request: "enchaînons sur
        /// la meta progression" -> "suivi de stats et meilleurs scores" —
        /// no gameplay effect, just cross-run stats persisted via
        /// MetaStatsFileStore). "New record" is highlighted in Success
        /// color; the rest stays muted like the existing subtitle style.
        /// </summary>
        private void SetMetaStatsText(MetaStats metaStats, bool isNewBestScore)
        {
            string bestScoreLine = isNewBestScore
                ? "Best score: " + metaStats.BestScore + " — new record!"
                : "Best score: " + metaStats.BestScore;
            // No longer "/RunConfig.RoundCount" (explicit request added
            // Endless mode, so BestRoundReached can now run past the
            // scheduled 8 — "X/8" would misread as somehow falling short
            // of a cap that no longer exists).
            _metaStatsText.text = bestScoreLine
                + "\nBest round reached: " + metaStats.BestRoundReached
                + "   ·   Runs played: " + metaStats.TotalRunsPlayed
                + "   ·   Victories: " + metaStats.TotalVictories;
            _metaStatsText.color = isNewBestScore ? UITheme.Success : UITheme.TextMutedOnBackground;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
