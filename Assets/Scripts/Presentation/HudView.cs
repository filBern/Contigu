using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Top bar showing round number, quota, round score, remaining piece budget and total score.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private Text _roundText;
        private Text _quotaText;
        private Text _roundScoreText;
        private Text _budgetText;
        private Text _totalScoreText;
        private Text _comboText;

        public RectTransform Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "Hud", UITheme.Panel);
            var container = panel.rectTransform;

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.spacing = 32f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            _roundText = UIFactory.CreateText(container, "Round", "", 18, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            _quotaText = UIFactory.CreateText(container, "Quota", "", 18, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            _roundScoreText = UIFactory.CreateText(container, "RoundScore", "", 18, UITheme.Success, TextAnchor.MiddleLeft);
            _budgetText = UIFactory.CreateText(container, "Budget", "", 18, UITheme.TextMuted, TextAnchor.MiddleLeft);
            _totalScoreText = UIFactory.CreateText(container, "TotalScore", "", 18, UITheme.TextMuted, TextAnchor.MiddleLeft);
            _comboText = UIFactory.CreateText(container, "Combo", "", 18, UITheme.Modifier, TextAnchor.MiddleLeft);
            _comboText.gameObject.SetActive(false);

            return container;
        }

        public void Refresh(RunManager run)
        {
            _roundText.text = "Manche " + run.CurrentRoundNumber + "/" + RunConfig.RoundCount + (run.IsBossRound ? " (BOSS)" : "");
            _budgetText.text = "Pièces restantes: " + run.PiecesRemainingThisRound;
            SetScores(run.RoundScore, run.CurrentQuota, run.TotalScore);
        }

        /// <summary>
        /// Updates just the score-derived texts (quota progress, round score,
        /// total score) without touching round/budget — lets the presentation
        /// layer animate these up progressively in sync with score popups
        /// instead of always jumping straight to the final value.
        /// </summary>
        public void SetScores(int roundScore, int quota, int totalScore)
        {
            _quotaText.text = "Quota " + roundScore + " / " + quota;
            _roundScoreText.text = "Score manche: " + roundScore;
            _totalScoreText.text = "Score total (run): " + totalScore;
        }

        /// <summary>Shows/updates the running total of the placement's combo currently being played out (see GameBootstrap.PlayPlacementSequence).</summary>
        public void ShowCombo(int amount)
        {
            _comboText.gameObject.SetActive(true);
            _comboText.text = "Combo: +" + amount;
        }

        public void HideCombo()
        {
            _comboText.gameObject.SetActive(false);
        }
    }
}
