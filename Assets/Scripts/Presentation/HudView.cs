using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Two progress bars, flush against the top and bottom edges of the
    /// screen and spanning its full width (no margin, no border) — the top
    /// bar tracks round score against the round's quota, the bottom bar
    /// tracks remaining piece budget for the round. Replaces the old
    /// text-only readout (round number, round score, total score) — the
    /// run's overall progress isn't shown moment-to-moment, just what the
    /// player needs to finish the current round.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float BarHeight = 68f;

        private RectTransform _scoreFillRect;
        private Text _scoreLabel;
        private RectTransform _piecesFillRect;
        private Text _piecesLabel;

        public void Build(Transform parent)
        {
            BuildBar(parent, "ScoreBar", UITheme.Success, top: true, out _scoreFillRect, out _scoreLabel);
            BuildBar(parent, "PiecesBar", UITheme.ButtonSelected, top: false, out _piecesFillRect, out _piecesLabel);
        }

        private static void BuildBar(Transform parent, string name, Color fillColor, bool top,
            out RectTransform fillRect, out Text label)
        {
            float edgeY = top ? 1f : 0f;
            // PanelLight rather than Panel for the track: Panel sits too
            // close in luminance to UITheme.Background, so with no border to
            // define its edge (removed on request) the unfilled portion just
            // blended into the screen and the bar read as a shapeless blob
            // rather than a container with a fill.
            var bg = UIFactory.CreatePanel(parent, name, UITheme.PanelLight);
            // Stretched full-width (anchor min/max x = 0/1) and flush against
            // the top or bottom edge (anchor, pivot and anchoredPosition all
            // pinned to that same edge — zero anchoredPosition means no gap).
            bg.rectTransform.anchorMin = new Vector2(0f, edgeY);
            bg.rectTransform.anchorMax = new Vector2(1f, edgeY);
            bg.rectTransform.pivot = new Vector2(0.5f, edgeY);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, BarHeight);

            // The fill is a plain colored rect whose RIGHT edge is driven
            // directly by anchorMax.x (see SetRatio) — a pure layout resize,
            // not Image.Type.Filled — so the bar's width is guaranteed to
            // track the ratio with no dependency on fill-shader/mesh behavior.
            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = UIFactory.CreateText(bg.transform, "Label", "", 32, UITheme.TextPrimary);
            text.fontStyle = FontStyle.Bold;
            UIFactory.StretchFull(text.rectTransform);

            fillRect = rt;
            label = text;
        }

        /// <summary>Resizes a bar's fill rect so its right edge sits at <paramref name="ratio"/> (0-1) of the bar's width.</summary>
        private static void SetRatio(RectTransform fillRect, float ratio)
        {
            var max = fillRect.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            fillRect.anchorMax = max;
        }

        public void Refresh(RunManager run)
        {
            UpdatePieces(run.PiecesRemainingThisRound, run.CurrentBudget);
            SetScores(run.RoundScore, run.CurrentQuota);
        }

        /// <summary>
        /// Updates just the score bar, without touching the pieces bar — lets
        /// the presentation layer animate the score up progressively in sync
        /// with score popups instead of always jumping straight to the final
        /// value.
        /// </summary>
        public void SetScores(int roundScore, int quota)
        {
            _scoreLabel.text = roundScore + " / " + quota;
            SetRatio(_scoreFillRect, quota > 0 ? (float)roundScore / quota : 0f);
        }

        private void UpdatePieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = piecesRemaining + " / " + budget;
            SetRatio(_piecesFillRect, budget > 0 ? (float)piecesRemaining / budget : 0f);
        }
    }
}
