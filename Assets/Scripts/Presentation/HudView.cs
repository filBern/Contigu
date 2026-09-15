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

            return container;
        }

        public void Refresh(RunManager run)
        {
            _roundText.text = "Manche " + run.CurrentRoundNumber + "/" + RunConfig.RoundCount + (run.IsBossRound ? " (BOSS)" : "");
            _quotaText.text = "Quota " + run.RoundScore + " / " + run.CurrentQuota;
            _roundScoreText.text = "Score manche: " + run.RoundScore;
            _budgetText.text = "Pièces restantes: " + run.PiecesRemainingThisRound;
            _totalScoreText.text = "Score total (run): " + run.TotalScore;
        }
    }
}
