using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Full-screen victory/defeat overlay with a single centered restart button.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        public event Action RestartRequested;
        public event Action EndlessRequested;

        private const float CardWidth = 800f;
        private const float CardHeight = 600f;
        private const float RecordCellHeight = 58f;
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 1f);
        private static readonly Color Dark = new Color(0.13f, 0.09f, 0.06f, 1f);
        private static readonly Color VictoryBanner = new Color(0.27f, 0.58f, 0.27f, 1f);
        private static readonly Color DefeatBanner = new Color(0.72f, 0.24f, 0.2f, 1f);

        private RectTransform _root;
        private UnityEngine.UI.Image _banner;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _subtitleText;
        private UnityEngine.UI.Text _metaStatsText;
        private RectTransform _recordsPaper;
        private UnityEngine.UI.Text _extraText;
        private RectTransform _restartButtonRect;
        private RectTransform _endlessButtonRect;
        private UnityEngine.UI.Image _sprigPortrait;
        private UnityEngine.UI.Text _sprigText;

        public RectTransform Build(Transform parent)
        {
            // The shop's back wall (dark planks), one big planter on it, and the outcome as a plaque: the same look as the seed bag and the shop.
            var overlay = UIFactory.CreatePanel(parent, "EndScreen", new Color(0.17f, 0.115f, 0.085f, 1f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);
            StallBackdrop.AddPlanks(_root);

            var board = UIFactory.CreateSlicedImage(_root, "Card", UISprites.Garden("Wood") ?? UISprites.CardBackground);
            board.color = Color.white;
            UIFactory.AddDropShadow(board, 8f, 0.45f);
            var card = board.rectTransform;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(0f, 160f);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);

            // The outcome on a colored banner that overlaps the card's top edge.
            _banner = UIFactory.CreateSlicedImage(card, "Banner", UISprites.Garden("Green") ?? UISprites.CardBackground);
            _banner.color = Color.white;
            UIFactory.AddDropShadow(_banner, 6f, 0.45f);
            _banner.rectTransform.anchorMin = _banner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _banner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _banner.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            _banner.rectTransform.sizeDelta = new Vector2(560f, 76f);
            _titleText = UIFactory.CreateText(_banner.rectTransform, "Title", "", 38, UITheme.TextOnBackground);
            UIFactory.StretchFull(_titleText.rectTransform);

            _subtitleText = UIFactory.CreateText(card, "Subtitle", "", 24, UITheme.TextOnBackground);
            _subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _subtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -52f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(CardWidth - 40f, 36f);

            // The run's records sit on a cream parchment, two columns of label over value.
            var paper = UIFactory.CreateSlicedImage(card, "Records", UISprites.Garden("WoodLight") ?? UISprites.CardBackground);
            paper.color = Color.white;
            _recordsPaper = paper.rectTransform;
            _recordsPaper.anchorMin = _recordsPaper.anchorMax = new Vector2(0.5f, 1f);
            _recordsPaper.pivot = new Vector2(0.5f, 1f);
            _recordsPaper.anchoredPosition = new Vector2(0f, -98f);
            _recordsPaper.sizeDelta = new Vector2(CardWidth - 60f, RecordCellHeight * 4f + 24f);

            // Most triggered fertilizer and pledges, in two short lines under the parchment.
            _extraText = UIFactory.CreateText(card, "Extra", "", 18, UITheme.TextOnBackground);
            _extraText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _extraText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _extraText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _extraText.rectTransform.anchoredPosition = new Vector2(0f, -98f - (RecordCellHeight * 4f + 24f) - 8f);
            _extraText.rectTransform.sizeDelta = new Vector2(CardWidth - 60f, 60f);

            // Best score, best round, runs, and what the run unlocked.
            _metaStatsText = UIFactory.CreateText(card, "MetaStats", "", 19, UITheme.TextOnBackground);
            _metaStatsText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _metaStatsText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _metaStatsText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _metaStatsText.rectTransform.anchoredPosition = new Vector2(0f, 92f);
            _metaStatsText.rectTransform.sizeDelta = new Vector2(CardWidth - 60f, 76f);

            var restartBtn = UIFactory.CreateButton(card, "Restart", "New Run", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 22);
            var rect = restartBtn.GetComponent<RectTransform>();
            _restartButtonRect = rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 20f);
            rect.sizeDelta = new Vector2(220f, 58f);
            restartBtn.onClick.AddListener(() =>
            {
                if (RestartRequested != null) RestartRequested();
            });

            // After a victory: keep playing past the final round, enemies sturdier by the round, until the garden falls.
            var endlessBtn = UIFactory.CreateButton(card, "Endless", "Keep Growing (Endless)", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 20);
            _endlessButtonRect = endlessBtn.GetComponent<RectTransform>();
            _endlessButtonRect.anchorMin = _endlessButtonRect.anchorMax = new Vector2(0.5f, 0f);
            _endlessButtonRect.pivot = new Vector2(0.5f, 0f);
            _endlessButtonRect.anchoredPosition = new Vector2(-120f, 20f);
            _endlessButtonRect.sizeDelta = new Vector2(320f, 58f);
            endlessBtn.onClick.AddListener(() =>
            {
                if (EndlessRequested != null) EndlessRequested();
            });

            // Sprig's closing comment, bottom-left of the screen, below the card.
            _sprigPortrait = SprigView.BuildSpeaker(_root, "Sprig", 130f, 300f, 18, out _sprigText, out var sprigRoot);
            sprigRoot.anchoredPosition = new Vector2(24f, 16f);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>The run's records on the parchment, as a grid of label over value. <paramref name="topFertilizer"/> is a ready-made line about the most triggered fertilizer (plus any pledge lines), or empty.</summary>
        public void SetRunStats(RunStats stats, string topFertilizer)
        {
            for (int i = _recordsPaper.childCount - 1; i >= 0; i--)
            {
                Destroy(_recordsPaper.GetChild(i).gameObject);
            }
            string placementLabel = "Biggest placement";
            if (stats.BestPlacementScore > 0)
            {
                placementLabel += " - round " + stats.BestPlacementRound + ", " + VisualDefaults.GetColorName(stats.BestPlacementColor) + " " + VisualDefaults.GetShapeName(stats.BestPlacementShape);
            }
            var cells = new[]
            {
                new[] { placementLabel, stats.BestPlacementScore.ToString("N0") },
                new[] { "Highest Mult", "x" + stats.BestMult.ToString("0.#") },
                new[] { "Biggest group", stats.BestGroupSize + " sprouts" },
                new[] { "Lines harvested", stats.LinesHarvested + "   (most at once: " + stats.BestHarvestLines + ")" },
                new[] { "Seeds planted", stats.Placements.ToString() },
                new[] { "Pollen from placements", stats.PollenFromPlacements.ToString() },
                new[] { "Shuffles used", stats.ShufflesUsed.ToString() },
            };
            float columnWidth = (_recordsPaper.sizeDelta.x - 24f) / 2f;
            for (int i = 0; i < cells.Length; i++)
            {
                int column = i / 4;
                int row = i % 4;
                float x = 18f + column * columnWidth;
                float y = -(12f + row * RecordCellHeight);
                AddRecordCell(cells[i][0], cells[i][1], x, y, columnWidth - 12f);
            }
            _extraText.text = string.IsNullOrEmpty(topFertilizer) ? "" : topFertilizer;
        }

        private void AddRecordCell(string label, string value, float x, float y, float width)
        {
            var l = UIFactory.CreateText(_recordsPaper, "Label", label, 15, UITheme.TextMuted, TextAnchor.LowerLeft);
            l.raycastTarget = false;
            l.rectTransform.anchorMin = l.rectTransform.anchorMax = new Vector2(0f, 1f);
            l.rectTransform.pivot = new Vector2(0f, 1f);
            l.rectTransform.anchoredPosition = new Vector2(x, y);
            l.rectTransform.sizeDelta = new Vector2(width, 22f);
            var v = UIFactory.CreateText(_recordsPaper, "Value", value, 26, UITheme.TextPrimary, TextAnchor.UpperLeft);
            v.raycastTarget = false;
            v.rectTransform.anchorMin = v.rectTransform.anchorMax = new Vector2(0f, 1f);
            v.rectTransform.pivot = new Vector2(0f, 1f);
            v.rectTransform.anchoredPosition = new Vector2(x, y - 22f);
            v.rectTransform.sizeDelta = new Vector2(width, 34f);
        }

        /// <summary>The outcome plaque is a planter whose bed says how it went (green: victory, red: defeat), its title in the bed's light color.</summary>
        private void SetBanner(string plot)
        {
            _banner.sprite = UISprites.Garden(plot) ?? _banner.sprite;
            _banner.color = Color.white;
            _titleText.color = UIFactory.PlotTextColor("Plot" + plot);
        }

        private void SprigComment(bool victory, bool isNewBestScore)
        {
            string line = SprigView.EndLine(victory, isNewBestScore, out var mood);
            _sprigPortrait.sprite = SprigView.Portrait(mood);
            _sprigText.text = line;
        }

        public void ShowVictory(int totalScore, MetaStats metaStats, bool isNewBestScore, string unlockedText = "")
        {
            _titleText.text = "Victory!";
            SetBanner("Green");
            _subtitleText.text = "Run complete - total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore, unlockedText);
            SprigComment(true, isNewBestScore);
            _restartButtonRect.anchoredPosition = new Vector2(190f, 20f);
            _endlessButtonRect.anchoredPosition = new Vector2(-120f, 20f);
            _endlessButtonRect.gameObject.SetActive(true);
            _root.gameObject.SetActive(true);
        }

        /// <param name="outOfSeeds">True when the seeds ran out with pests still alive; false when no seed of the hand could be placed any more.</param>
        public void ShowDefeat(int roundNumber, int totalScore, MetaStats metaStats, bool isNewBestScore, bool endless = false, string unlockedText = "", bool outOfSeeds = true)
        {
            _endlessButtonRect.gameObject.SetActive(false);
            _titleText.text = endless ? "Endless - fell in Round " + roundNumber : "Defeat - Round " + roundNumber;
            SetBanner("Red");
            _subtitleText.text = (endless ? "Rounds cleared past the finale: " + System.Math.Max(0, roundNumber - 1 - RunConfig.RoundCount) + " - " : (outOfSeeds ? "Out of seeds - " : "No seed can be planted - ")) + "total score: " + totalScore;
            SetMetaStatsText(metaStats, isNewBestScore, unlockedText);
            SprigComment(false, isNewBestScore);
            _restartButtonRect.anchoredPosition = new Vector2(0f, 20f);
            _root.gameObject.SetActive(true);
        }

        /// <summary>
        /// Lightweight meta-progression with no gameplay effect, just
        /// cross-run stats persisted via MetaStatsFileStore. "New record"
        /// is highlighted in Success color; the rest stays muted.
        /// </summary>
        private void SetMetaStatsText(MetaStats metaStats, bool isNewBestScore, string unlockedText = "")
        {
            string bestScoreLine = isNewBestScore
                ? "Best score: " + metaStats.BestScore + " - new record!"
                : "Best score: " + metaStats.BestScore;
            _metaStatsText.text = bestScoreLine
                + "\nBest round reached: " + metaStats.BestRoundReached
                + "   ·   Runs played: " + metaStats.TotalRunsPlayed
                + "   ·   Victories: " + metaStats.TotalVictories
                + (string.IsNullOrEmpty(unlockedText) ? "" : "\n" + unlockedText);
            _metaStatsText.color = isNewBestScore ? new Color(0.62f, 0.9f, 0.45f) : UITheme.TextOnBackground;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
