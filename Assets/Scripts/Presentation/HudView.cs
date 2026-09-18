using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Two progress bars pinned to the top and bottom edges of the screen: the
    /// top bar tracks round score against the round's quota, the bottom bar
    /// tracks remaining piece budget for the round. Replaces the old text-only
    /// readout (round number, round score, total score) — the run's overall
    /// progress isn't shown moment-to-moment, just what the player needs to
    /// finish the current round.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float BarWidth = 520f;
        private const float BarHeight = 34f;

        private Image _scoreFill;
        private Text _scoreLabel;
        private Image _piecesFill;
        private Text _piecesLabel;

        public void Build(Transform parent)
        {
            BuildBar(parent, "ScoreBar", UITheme.Success,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f),
                out _scoreFill, out _scoreLabel);

            BuildBar(parent, "PiecesBar", UITheme.ButtonSelected,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                out _piecesFill, out _piecesLabel);
        }

        private static void BuildBar(Transform parent, string name, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition,
            out Image fill, out Text label)
        {
            var bg = UIFactory.CreatePanel(parent, name, UITheme.Panel);
            bg.rectTransform.anchorMin = anchorMin;
            bg.rectTransform.anchorMax = anchorMax;
            bg.rectTransform.pivot = pivot;
            bg.rectTransform.anchoredPosition = anchoredPosition;
            bg.rectTransform.sizeDelta = new Vector2(BarWidth, BarHeight);
            var bgOutline = bg.gameObject.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            bgOutline.effectDistance = new Vector2(2f, -2f);

            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            var fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);

            var text = UIFactory.CreateText(bg.transform, "Label", "", 16, UITheme.TextPrimary);
            text.fontStyle = FontStyle.Bold;
            UIFactory.StretchFull(text.rectTransform);
            var textOutline = text.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textOutline.effectDistance = new Vector2(1.5f, -1.5f);

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
