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

        private Image _scoreFill;
        private Text _scoreLabel;
        private Image _piecesFill;
        private Text _piecesLabel;

        public void Build(Transform parent)
        {
            BuildBar(parent, "ScoreBar", UITheme.Success, top: true, out _scoreFill, out _scoreLabel);
            BuildBar(parent, "PiecesBar", UITheme.ButtonSelected, top: false, out _piecesFill, out _piecesLabel);
        }

        private static void BuildBar(Transform parent, string name, Color fillColor, bool top,
            out Image fill, out Text label)
        {
            float edgeY = top ? 1f : 0f;
            var bg = UIFactory.CreatePanel(parent, name, UITheme.Panel);
            // Stretched full-width (anchor min/max x = 0/1) and flush against
            // the top or bottom edge (anchor, pivot and anchoredPosition all
            // pinned to that same edge — zero anchoredPosition means no gap).
            bg.rectTransform.anchorMin = new Vector2(0f, edgeY);
            bg.rectTransform.anchorMax = new Vector2(1f, edgeY);
            bg.rectTransform.pivot = new Vector2(0.5f, edgeY);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, BarHeight);

            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            var fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var text = UIFactory.CreateText(bg.transform, "Label", "", 16, UITheme.TextPrimary);
            text.fontStyle = FontStyle.Bold;
            UIFactory.StretchFull(text.rectTransform);

            fill = fillImg;
            label = text;
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
            _scoreFill.fillAmount = quota > 0 ? Mathf.Clamp01((float)roundScore / quota) : 0f;
        }

        private void UpdatePieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = "Remaining pieces: " + piecesRemaining;
            _piecesFill.fillAmount = budget > 0 ? Mathf.Clamp01((float)piecesRemaining / budget) : 0f;
        }
    }
}
