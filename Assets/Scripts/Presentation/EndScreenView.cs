using System;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Full-screen victory/defeat overlay with a single centered restart button.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        public event Action RestartRequested;
        public event Action EndlessRequested;

        private RectTransform _root;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _subtitleText;
        private UnityEngine.UI.Text _metaStatsText;
        private RectTransform _restartButtonRect;
        private RectTransform _endlessButtonRect;
        private UnityEngine.UI.Image _sprigPortrait;
        private UnityEngine.UI.Text _sprigText;

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
            _subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 0.52f);
            _subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
            _subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(700f, 50f);

            _metaStatsText = UIFactory.CreateText(_root, "MetaStats", "", 20, UITheme.TextMutedOnBackground);
            _metaStatsText.rectTransform.anchorMin = new Vector2(0.5f, 0.45f);
            _metaStatsText.rectTransform.anchorMax = new Vector2(0.5f, 0.45f);
            _metaStatsText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _metaStatsText.rectTransform.sizeDelta = new Vector2(700f, 60f);

            var restartBtn = UIFactory.CreateButton(_root, "Restart", "New Run", UISprites.ChooseButtonBackground, 18);
            var rect = restartBtn.GetComponent<RectTransform>();
            _restartButtonRect = rect;
            rect.anchorMin = new Vector2(0.5f, 0.34f);
            rect.anchorMax = new Vector2(0.5f, 0.34f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120f, 0f);
            rect.sizeDelta = new Vector2(220f, 52f);
            restartBtn.onClick.AddListener(() =>
            {
                if (RestartRequested != null) RestartRequested();
            });

            // After a victory: keep playing past the final round, enemies sturdier by the round, until the garden falls.
            var endlessBtn = UIFactory.CreateButton(_root, "Endless", "Keep Growing (Endless)", UISprites.ChooseButtonBackground, 18);
            _endlessButtonRect = endlessBtn.GetComponent<RectTransform>();
            _endlessButtonRect.anchorMin = new Vector2(0.5f, 0.34f);
            _endlessButtonRect.anchorMax = new Vector2(0.5f, 0.34f);
            _endlessButtonRect.pivot = new Vector2(0.5f, 0.5f);
            _endlessButtonRect.sizeDelta = new Vector2(320f, 52f);
            endlessBtn.onClick.AddListener(() =>
            {
                if (EndlessRequested != null) EndlessRequested();
            });

            // Sprig's closing comment, bottom-left.
            _sprigPortrait = SprigView.BuildSpeaker(_root, "Sprig", 130f, 300f, 18, out _sprigText, out var sprigRoot);
            // Small and low: it must stay clear of the buttons in the middle (a wider block covered Keep Growing and New Run).
            sprigRoot.anchoredPosition = new Vector2(24f, 20f);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private void SprigComment(bool victory, bool isNewBestScore)
        {
            string line = SprigView.EndLine(victory, isNewBestScore, out var mood);
            _sprigPortrait.sprite = SprigView.Portrait(mood);
            _sprigText.text = line;
        }

        public void ShowVictory(int totalScore, MetaStats metaStats, bool isNewBestScore, int starsEarned = 0)
        {
            _titleText.text = "Victory!";
            _titleText.color = UITheme.Success;
            _subtitleText.text = "Run complete - total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore, starsEarned);
            SprigComment(true, isNewBestScore);
            _restartButtonRect.anchoredPosition = new Vector2(190f, 0f);
            _endlessButtonRect.anchoredPosition = new Vector2(-120f, 0f);
            _endlessButtonRect.gameObject.SetActive(true);
            _root.gameObject.SetActive(true);
        }

        /// <param name="outOfSeeds">True when the seeds ran out with pests still alive; false when no seed of the hand could be placed any more.</param>
        public void ShowDefeat(int roundNumber, int totalScore, MetaStats metaStats, bool isNewBestScore, bool endless = false, int starsEarned = 0, bool outOfSeeds = true)
        {
            _endlessButtonRect.gameObject.SetActive(false);
            _titleText.text = endless ? "Endless - fell in Round " + roundNumber : "Defeat - Round " + roundNumber;
            _titleText.color = UITheme.Danger;
            _subtitleText.text = (endless ? "Rounds cleared past the finale: " + System.Math.Max(0, roundNumber - 1 - RunConfig.RoundCount) + " - " : (outOfSeeds ? "Out of seeds - " : "No seed can be planted - ")) + "total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore, starsEarned);
            SprigComment(false, isNewBestScore);
            _restartButtonRect.anchoredPosition = new Vector2(0f, 0f);
            _root.gameObject.SetActive(true);
        }

        /// <summary>
        /// Lightweight meta-progression with no gameplay effect, just
        /// cross-run stats persisted via MetaStatsFileStore. "New record"
        /// is highlighted in Success color; the rest stays muted.
        /// </summary>
        private void SetMetaStatsText(MetaStats metaStats, bool isNewBestScore, int starsEarned = 0)
        {
            string bestScoreLine = isNewBestScore
                ? "Best score: " + metaStats.BestScore + " - new record!"
                : "Best score: " + metaStats.BestScore;
            _metaStatsText.text = bestScoreLine
                + "\nBest round reached: " + metaStats.BestRoundReached
                + "   ·   Runs played: " + metaStats.TotalRunsPlayed
                + "   ·   Victories: " + metaStats.TotalVictories
                + (starsEarned > 0 ? "\nStars earned: +" + starsEarned + " (total " + metaStats.Stars + ")" : "");
            _metaStatsText.color = isNewBestScore ? UITheme.Success : UITheme.TextMutedOnBackground;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
