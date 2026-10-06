using System;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Full-screen victory/defeat overlay with a single centered restart button.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        public event Action RestartRequested;

        private RectTransform _root;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _subtitleText;
        private UnityEngine.UI.Text _metaStatsText;
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
            // Centered — same single-button layout as ShowDefeat below,
            // since removing Endless mode left New Run as Victory's only
            // button too (explicit request: "On enleve le endless mode").
            _restartButtonRect.anchoredPosition = new Vector2(0f, 0f);
            _root.gameObject.SetActive(true);
        }

        public void ShowDefeat(int roundNumber, int totalScore, MetaStats metaStats, bool isNewBestScore)
        {
            _titleText.text = "Defeat — Round " + roundNumber;
            _titleText.color = UITheme.Danger;
            _subtitleText.text = "Quota not reached — total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore);
            // Centered (explicit request: "Si on a perdu la partie, le
            // bouton new run doit être centré horizontalement").
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
