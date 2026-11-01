using System;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>A short card between a cleared round and the shop: how the round went (score, biggest placement, harvests, Pollen), then on to the shop.</summary>
    public sealed class RoundSummaryView : MonoBehaviour
    {
        private const float CardWidth = 440f;
        private const float RowHeight = 34f;
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 1f);
        private static readonly Color Dark = new Color(0.13f, 0.09f, 0.06f, 1f);

        private RectTransform _root;
        private RectTransform _card;
        private Text _title;
        private RectTransform _rows;
        private Button _continue;
        private Action _onContinue;
        private Coroutine _pop;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "RoundSummaryOverlay", new Color(0f, 0f, 0f, 0.55f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var board = UIFactory.CreateSlicedImage(_root, "Card", UISprites.CardBackground);
            board.color = Wood;
            UIFactory.AddThickOutline(board, Dark);
            _card = board.rectTransform;
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(CardWidth, 420f);

            _title = UIFactory.CreateText(_card, "Title", "", 34, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            _title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            _title.rectTransform.sizeDelta = new Vector2(CardWidth - 30f, 46f);

            // A cream parchment holds the figures.
            var paper = UIFactory.CreateSlicedImage(_card, "Paper", UISprites.CardBackground);
            paper.color = UITheme.Panel;
            UIFactory.AddThickOutline(paper, Dark);
            paper.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            paper.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            paper.rectTransform.pivot = new Vector2(0.5f, 1f);
            paper.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            paper.rectTransform.sizeDelta = new Vector2(CardWidth - 40f, 250f);
            _rows = paper.rectTransform;

            _continue = UIFactory.CreateButton(_card, "Continue", "To the shop", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 24);
            var rect = _continue.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(260f, 58f);
            _continue.onClick.AddListener(OnContinue);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(RoundSummary summary, Action onContinue)
        {
            _onContinue = onContinue;
            _title.text = "Round " + summary.RoundNumber + " cleared";
            for (int i = _rows.childCount - 1; i >= 0; i--)
            {
                Destroy(_rows.GetChild(i).gameObject);
            }
            int row = 0;
            AddRow(ref row, "Round score", summary.Score.ToString("N0"), false);
            AddRow(ref row, "Biggest placement", summary.BestPlacementScore.ToString("N0"), true);
            AddRow(ref row, "Lines harvested", summary.LinesHarvested.ToString(), false);
            AddRow(ref row, "Seeds planted", summary.Placements.ToString(), false);
            AddRow(ref row, "Pollen from placements", "+" + summary.PollenFromPlacements, false);
            AddRow(ref row, "Unused seeds + bonuses", "+" + summary.EndOfRoundPollen + " Pollen", false);
            AddRow(ref row, "Stars", "+" + summary.StarsEarned, false);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            SfxManager.Play(SfxId.RoundClear);
            if (!MotionSettings.Reduced)
            {
                if (_pop != null) { StopCoroutine(_pop); }
                _pop = StartCoroutine(PopIn());
            }
            else
            {
                _card.localScale = Vector3.one;
            }
        }

        private void AddRow(ref int row, string label, string value, bool highlight)
        {
            var left = UIFactory.CreateText(_rows, "Label" + row, label, 20, UITheme.TextMuted, TextAnchor.MiddleLeft);
            left.raycastTarget = false;
            left.rectTransform.anchorMin = new Vector2(0f, 1f);
            left.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 1f);
            left.rectTransform.offsetMin = new Vector2(18f, -(14f + (row + 1) * RowHeight));
            left.rectTransform.offsetMax = new Vector2(0f, -(14f + row * RowHeight));
            var right = UIFactory.CreateText(_rows, "Value" + row, value, highlight ? 26 : 22, highlight ? UITheme.Success : UITheme.TextPrimary, TextAnchor.MiddleRight);
            right.raycastTarget = false;
            right.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 1f);
            right.rectTransform.offsetMin = new Vector2(0f, -(14f + (row + 1) * RowHeight));
            right.rectTransform.offsetMax = new Vector2(-18f, -(14f + row * RowHeight));
            row++;
        }

        private System.Collections.IEnumerator PopIn()
        {
            float t = 0f;
            const float Duration = 0.3f;
            while (t < Duration)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / Duration);
                float s = p < 0.7f ? Mathf.Lerp(0.8f, 1.06f, p / 0.7f) : Mathf.Lerp(1.06f, 1f, (p - 0.7f) / 0.3f);
                _card.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _card.localScale = Vector3.one;
        }

        private void OnContinue()
        {
            _root.gameObject.SetActive(false);
            var next = _onContinue;
            _onContinue = null;
            if (next != null) { next(); }
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
