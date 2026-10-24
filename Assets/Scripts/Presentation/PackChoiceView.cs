using System;
using System.Collections.Generic;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>One card of a pack opening: a fertilizer or a tile upgrade, fully visible.</summary>
    public struct PackOption
    {
        public string Name;
        public string Description;
        public string Subtitle;
        public Sprite Icon;
        public Color Accent;
    }

    /// <summary>
    /// The pack opening: a Sprout Pack or Fertilizer Pack reveals its options side by side; clicking one keeps it, "Skip" walks away from
    /// the pack. The whole point of a pack is that nothing is hidden: the player chooses with full information.
    /// </summary>
    public sealed class PackChoiceView : MonoBehaviour
    {
        private const float CardWidth = 240f;
        private const float CardHeight = 330f;
        private const float CardSpacing = 30f;
        private const float IconSize = 110f;

        /// <summary>Fires with the picked card's index.</summary>
        public event Action<int> OptionChosen;

        /// <summary>Fires when the player leaves without taking anything.</summary>
        public event Action Skipped;

        private RectTransform _root;
        private Text _title;
        private Text _hint;
        private RectTransform _cardsRow;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "PackChoiceOverlay", new Color(0f, 0f, 0f, 0.9f));
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);

            _title = UIFactory.CreateText(_root, "Title", "", 32, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _title.rectTransform.anchoredPosition = new Vector2(0f, CardHeight * 0.5f + 78f);
            _title.rectTransform.sizeDelta = new Vector2(900f, 46f);

            _hint = UIFactory.CreateText(_root, "Hint", "", 17, UITheme.TextMutedOnBackground);
            _hint.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, CardHeight * 0.5f + 36f);
            _hint.rectTransform.sizeDelta = new Vector2(900f, 26f);

            _cardsRow = UIFactory.CreateUIObject("Cards", _root);
            _cardsRow.anchorMin = new Vector2(0.5f, 0.5f);
            _cardsRow.anchorMax = new Vector2(0.5f, 0.5f);
            _cardsRow.pivot = new Vector2(0.5f, 0.5f);
            _cardsRow.anchoredPosition = Vector2.zero;
            _cardsRow.sizeDelta = new Vector2(3 * CardWidth + 2 * CardSpacing, CardHeight);

            var skip = UIFactory.CreateButton(_root, "Skip", "Skip this pack", UISprites.CancelButtonBackground, 18);
            UIFactory.DisablePlants(skip);
            var skipRect = skip.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0.5f);
            skipRect.anchorMax = new Vector2(0.5f, 0.5f);
            skipRect.pivot = new Vector2(0.5f, 0.5f);
            skipRect.anchoredPosition = new Vector2(0f, -CardHeight * 0.5f - 46f);
            skipRect.sizeDelta = new Vector2(240f, 48f);
            skip.onClick.AddListener(() =>
            {
                _root.gameObject.SetActive(false);
                if (Skipped != null)
                {
                    Skipped();
                }
            });

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(string title, string hint, IReadOnlyList<PackOption> options)
        {
            for (int i = _cardsRow.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsRow.GetChild(i).gameObject);
            }
            _title.text = title;
            _hint.text = hint;
            float totalWidth = options.Count * CardWidth + (options.Count - 1) * CardSpacing;
            _cardsRow.sizeDelta = new Vector2(totalWidth, CardHeight);
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(i, options[i], -totalWidth * 0.5f + CardWidth * 0.5f + i * (CardWidth + CardSpacing));
            }
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        private void BuildCard(int index, PackOption option, float x)
        {
            var card = UIFactory.CreateSlicedImage(_cardsRow, "Option_" + index, UISprites.CardBackground);
            card.color = UITheme.Panel;
            UIFactory.AddThickOutline(card, UITheme.Border);
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchoredPosition = new Vector2(x, 0f);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            int captured = index;
            button.onClick.AddListener(() => Choose(captured));

            AddLabel(card.transform, option.Name, 22, -14f, 56f, TextAnchor.UpperCenter, UITheme.TextPrimary);

            if (!string.IsNullOrEmpty(option.Subtitle))
            {
                var chip = UIFactory.CreatePanel(card.transform, "Chip", new Color(option.Accent.r, option.Accent.g, option.Accent.b, 0.35f));
                chip.raycastTarget = false;
                chip.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                chip.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                chip.rectTransform.pivot = new Vector2(0.5f, 1f);
                chip.rectTransform.anchoredPosition = new Vector2(0f, -72f);
                chip.rectTransform.sizeDelta = new Vector2(CardWidth - 50f, 24f);
                var chipText = UIFactory.CreateText(chip.transform, "Text", option.Subtitle, 14, new Color(option.Accent.r * 0.45f, option.Accent.g * 0.45f, option.Accent.b * 0.45f, 1f));
                chipText.raycastTarget = false;
                UIFactory.StretchFull(chipText.rectTransform);
            }

            if (option.Icon != null)
            {
                var icon = UIFactory.CreatePanel(card.transform, "Icon", Color.white);
                icon.sprite = option.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                icon.rectTransform.pivot = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, -104f);
                icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            }

            AddLabel(card.transform, option.Description, 15, -222f, 100f, TextAnchor.UpperCenter, UITheme.TextPrimary);
        }

        private static void AddLabel(Transform parent, string text, int fontSize, float y, float height, TextAnchor anchor, Color color)
        {
            var label = UIFactory.CreateText(parent, "Label", text, fontSize, color, anchor);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, y);
            label.rectTransform.sizeDelta = new Vector2(CardWidth - 24f, height);
        }

        private void Choose(int index)
        {
            _root.gameObject.SetActive(false);
            if (OptionChosen != null)
            {
                OptionChosen(index);
            }
        }
    }
}
