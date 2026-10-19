using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown after buying Seed Mastery / Color Mastery: two cards (RunManager.PendingMasteryOptions), each a
    /// shape or color with the level it would reach; clicking one picks it. Replaces the old random slot-machine
    /// reel, so the player decides instead of rolling.
    /// </summary>
    public sealed class MasteryChoiceView : MonoBehaviour
    {
        private const float CardWidth = 220f;
        private const float CardHeight = 310f;
        private const float GlyphBoxSize = 110f;
        private const float CardSpacing = 36f;

        /// <summary>Fires with the picked option's index.</summary>
        public event Action<int> OptionChosen;

        private RectTransform _root;
        private Text _title;
        private RectTransform _cardsRow;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "MasteryChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);

            _title = UIFactory.CreateText(_root, "Title", "", 30, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _title.rectTransform.anchoredPosition = new Vector2(0f, CardHeight * 0.5f + 50f);
            _title.rectTransform.sizeDelta = new Vector2(800f, 44f);

            _cardsRow = UIFactory.CreateUIObject("Cards", _root);
            _cardsRow.anchorMin = new Vector2(0.5f, 0.5f);
            _cardsRow.anchorMax = new Vector2(0.5f, 0.5f);
            _cardsRow.pivot = new Vector2(0.5f, 0.5f);
            _cardsRow.anchoredPosition = Vector2.zero;
            _cardsRow.sizeDelta = new Vector2(2 * CardWidth + CardSpacing, CardHeight);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(IReadOnlyList<MasteryOption> options, RunManager run)
        {
            for (int i = _cardsRow.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsRow.GetChild(i).gameObject);
            }

            bool shapes = options.Count > 0 && options[0].Shape.HasValue;
            _title.text = shapes ? "Choose a seed shape to master" : "Choose a bloom color to master";

            float totalWidth = options.Count * CardWidth + (options.Count - 1) * CardSpacing;
            _cardsRow.sizeDelta = new Vector2(totalWidth, CardHeight);
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(i, options[i], run, -totalWidth * 0.5f + CardWidth * 0.5f + i * (CardWidth + CardSpacing));
            }

            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        private void BuildCard(int index, MasteryOption option, RunManager run, float x)
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

            string name;
            int levelNow;
            var glyph = UIFactory.CreateUIObject("Glyph", card.transform);
            glyph.anchorMin = new Vector2(0.5f, 1f);
            glyph.anchorMax = new Vector2(0.5f, 1f);
            glyph.pivot = new Vector2(0.5f, 1f);
            glyph.anchoredPosition = new Vector2(0f, -22f);
            glyph.sizeDelta = new Vector2(GlyphBoxSize, GlyphBoxSize);
            if (option.Shape.HasValue)
            {
                name = VisualDefaults.GetShapeName(option.Shape.Value);
                levelNow = run.GetShapeMasteryLevel(option.Shape.Value);
                ShapePreviewFactory.BuildMono(glyph, PieceShapeCatalog.Get(option.Shape.Value), new Color(0.55f, 0.55f, 0.55f));
            }
            else
            {
                var color = option.Color.Value;
                name = VisualDefaults.GetColorName(color);
                levelNow = run.GetColorMasteryLevel(color);
                ShapePreviewFactory.Build(glyph, PieceShapeCatalog.Get(ShapeId.Single), color, null, null, null);
            }

            AddLabel(card.transform, name, 26, -(22f + GlyphBoxSize + 14f), 34f);
            AddLabel(card.transform, "Level " + levelNow + " → " + (levelNow + option.Gain), 22, -(22f + GlyphBoxSize + 52f), 30f);
            AddLabel(card.transform, "+" + option.Gain + (option.Gain == 1 ? " level" : " levels"), 18, -(22f + GlyphBoxSize + 86f), 26f);
            AddLabel(card.transform, "+" + (levelNow + option.Gain - 1) + " pts per sprout", 16, -(22f + GlyphBoxSize + 114f), 24f);
        }

        private static void AddLabel(Transform parent, string text, int fontSize, float y, float height)
        {
            var label = UIFactory.CreateText(parent, "Label", text, fontSize, UITheme.TextPrimary);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, y);
            label.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, height);
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
