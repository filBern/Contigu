using System;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Full-screen victory/defeat overlay with a restart button.</summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        public event Action RestartRequested;

        private RectTransform _root;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _subtitleText;

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

            _subtitleText = UIFactory.CreateText(_root, "Subtitle", "", 22, UITheme.TextMuted);
            _subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 0.48f);
            _subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.48f);
            _subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _subtitleText.rectTransform.sizeDelta = new Vector2(700f, 50f);

            var restartBtn = UIFactory.CreateButton(_root, "Restart", "Nouveau run", UITheme.ButtonSelected, 18);
            var rect = restartBtn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.35f);
            rect.anchorMax = new Vector2(0.5f, 0.35f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 52f);
            restartBtn.onClick.AddListener(() =>
            {
                if (RestartRequested != null) RestartRequested();
            });

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void ShowVictory(int totalScore)
        {
            _titleText.text = "Victoire !";
            _titleText.color = UITheme.Success;
            _subtitleText.text = "Run terminé — score total : " + totalScore;
            _root.gameObject.SetActive(true);
        }

        public void ShowDefeat(int roundNumber, int totalScore)
        {
            _titleText.text = "Défaite — Manche " + roundNumber;
            _titleText.color = UITheme.Danger;
            _subtitleText.text = "Quota non atteint — score total : " + totalScore;
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
