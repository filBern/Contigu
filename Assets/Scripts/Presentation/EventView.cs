using System;
using UnityEngine;
using UnityEngine.UI;
using Contigu.Core;

namespace Contigu.Presentation
{
    /// <summary>
    /// "Something happens": the card shown over the shop every third round. A title, a short story, two or three options that each state their cost and
    /// their real chances, and a free "walk on"; after the choice, what happened, then Continue back to the shop. Nothing hints which option is best.
    /// </summary>
    public sealed class EventView : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _card;
        private RunManager _run;
        private Action _onResolved;

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "EventOverlay", new Color(UITheme.Background.r, UITheme.Background.g, UITheme.Background.b, 0.97f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);
            _card = UIFactory.CreateUIObject("Card", _root);
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(940f, 640f);
            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Rebind(RunManager run)
        {
            _run = run;
        }

        public void Hide()
        {
            if (_root != null) { _root.gameObject.SetActive(false); }
        }

        /// <summary>Shows the run's pending event; <paramref name="onResolved"/> runs once the player has chosen and pressed Continue.</summary>
        public void Show(RunManager run, Action onResolved)
        {
            _run = run;
            _onResolved = onResolved;
            if (_run == null || _run.PendingEvent == null)
            {
                if (_onResolved != null) { _onResolved(); }
                return;
            }
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            BuildChoices();
        }

        private void Clear()
        {
            for (int i = _card.childCount - 1; i >= 0; i--) { Destroy(_card.GetChild(i).gameObject); }
        }

        private Text AddText(string name, string content, int size, Color color, float top, float height, TextAnchor anchor = TextAnchor.UpperCenter)
        {
            var text = UIFactory.CreateText(_card, name, content, size, color, anchor);
            text.raycastTarget = false;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = new Vector2(0f, -top);
            text.rectTransform.sizeDelta = new Vector2(-60f, height);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = text.fontSize;
            return text;
        }

        private void BuildChoices()
        {
            Clear();
            var e = _run.PendingEvent;
            AddText("Kicker", "Something happens...", 20, UITheme.TextMuted, 6f, 34f);
            AddText("Title", e.Title, 40, UITheme.Ink, 40f, 60f);
            AddText("Story", e.Story, 24, UITheme.TextPrimary, 108f, 86f);

            float y = 214f;
            const float OptionHeight = 112f;
            for (int i = 0; i < e.Options.Length; i++)
            {
                var option = e.Options[i];
                bool free = i == e.Options.Length - 1;
                bool available = _run.IsEventOptionAvailable(i);
                var button = UIFactory.CreateButton(_card, "Option" + i, "", UISprites.Garden(free ? "Green" : "WoodLight") ?? UISprites.CardBackground, 14);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -y);
                rect.sizeDelta = new Vector2(-60f, OptionHeight);
                button.interactable = available;
                var label = button.GetComponentInChildren<Text>();
                PlotButtonDecor.Suppress(button);
                label.text = option.Label;
                label.alignment = TextAnchor.UpperCenter;
                label.fontSize = UITheme.Scaled(26);
                label.resizeTextMaxSize = UITheme.Scaled(26);
                label.rectTransform.offsetMin = new Vector2(24f, 10f);
                label.rectTransform.offsetMax = new Vector2(-24f, -10f);
                string detail = option.Describe();
                if (!available && option.RequiredPollen > _run.Lueur) { detail = "Needs " + option.RequiredPollen + " Pollen.   " + detail; }
                var description = UIFactory.CreateText(button.transform, "Detail", detail, 17, UITheme.Ink, TextAnchor.LowerCenter);
                description.raycastTarget = false;
                description.rectTransform.anchorMin = Vector2.zero;
                description.rectTransform.anchorMax = Vector2.one;
                description.rectTransform.offsetMin = new Vector2(24f, 8f);
                description.rectTransform.offsetMax = new Vector2(-24f, -48f);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                description.resizeTextForBestFit = true;
                description.resizeTextMinSize = 11;
                description.resizeTextMaxSize = description.fontSize;
                int captured = i;
                button.onClick.AddListener(() => Choose(captured));
                y += OptionHeight + 12f;
            }
        }

        private void Choose(int index)
        {
            if (_run == null || !_run.ResolveEvent(index))
            {
                return;
            }
            SfxManager.Play(SfxId.LueurGain);
            Clear();
            AddText("Kicker", "And so...", 20, UITheme.TextMuted, 6f, 34f);
            float y = 60f;
            foreach (var line in _run.LastEventResult)
            {
                AddText("Result", line, 30, UITheme.Ink, y, 56f);
                y += 62f;
            }
            var next = UIFactory.CreateButton(_card, "Continue", "Continue", UISprites.Garden("Gold") ?? UISprites.CardBackground, 22);
            var rect = next.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(320f, MobileSetup.TouchMin(64f));
            next.onClick.AddListener(() =>
            {
                Hide();
                if (_onResolved != null) { _onResolved(); }
            });
        }
    }
}
