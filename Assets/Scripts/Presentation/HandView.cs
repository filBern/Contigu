using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Renders the current hand of up to 3 pieces as selectable slots.</summary>
    public sealed class HandView : MonoBehaviour
    {
        public event Action<int> SlotSelected;

        private DeckManager _deck;
        private Image[] _slotBackgrounds;
        private Text[] _slotLabels;
        private RectTransform[] _previewContainers;
        private int _selectedIndex = -1;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public RectTransform Build(Transform parent, DeckManager deck)
        {
            _deck = deck;

            var container = UIFactory.CreateUIObject("HandContainer", parent);
            var layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _slotBackgrounds = new Image[DeckManager.HandSize];
            _slotLabels = new Text[DeckManager.HandSize];
            _previewContainers = new RectTransform[DeckManager.HandSize];

            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreatePanel(container, "Slot" + i, UITheme.ButtonIdle);
                slot.rectTransform.sizeDelta = new Vector2(120f, 140f);
                var btn = slot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSlotClicked(idx));

                var previewContainer = UIFactory.CreateUIObject("Preview", slot.transform);
                previewContainer.anchorMin = new Vector2(0.5f, 1f);
                previewContainer.anchorMax = new Vector2(0.5f, 1f);
                previewContainer.pivot = new Vector2(0.5f, 1f);
                previewContainer.anchoredPosition = new Vector2(0f, -10f);
                previewContainer.sizeDelta = new Vector2(90f, 68f);

                var label = UIFactory.CreateText(slot.transform, "Label", "", 13, UITheme.TextPrimary);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0f);
                label.rectTransform.pivot = new Vector2(0.5f, 0f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                label.rectTransform.sizeDelta = new Vector2(-8f, 40f);

                _slotBackgrounds[i] = slot;
                _slotLabels[i] = label;
                _previewContainers[i] = previewContainer;
            }

            Refresh();
            return container;
        }

        private void OnSlotClicked(int idx)
        {
            if (idx >= _deck.Hand.Count)
            {
                return;
            }
            _selectedIndex = idx;
            UpdateSelectionVisuals();
            if (SlotSelected != null)
            {
                SlotSelected(idx);
            }
        }

        public void ClearSelection()
        {
            _selectedIndex = -1;
            UpdateSelectionVisuals();
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
            _selectedIndex = -1;
            Refresh();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                bool hasPiece = i < _deck.Hand.Count;
                bool selected = i == _selectedIndex;
                _slotBackgrounds[i].color = !hasPiece ? UITheme.Panel : (selected ? UITheme.ButtonSelected : UITheme.ButtonIdle);
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                var preview = _previewContainers[i];
                for (int c = preview.childCount - 1; c >= 0; c--)
                {
                    Destroy(preview.GetChild(c).gameObject);
                }

                if (i < _deck.Hand.Count)
                {
                    var token = _deck.Hand[i];
                    BuildShapePreview(preview, token);
                    _slotLabels[i].text = VisualDefaults.GetShapeName(token.Shape) + "\n" + VisualDefaults.GetColorName(token.Color);
                }
                else
                {
                    _slotLabels[i].text = string.Empty;
                }
            }
            UpdateSelectionVisuals();
        }

        private void BuildShapePreview(RectTransform container, PieceToken token)
        {
            var shape = PieceShapeCatalog.Get(token.Shape);
            int maxX = 0;
            int maxY = 0;
            var occupied = new HashSet<Vector2Int>();
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var c = shape.Cells[i];
                occupied.Add(c);
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            int cols = maxX + 1;
            int rows = maxY + 1;
            float cell = Mathf.Min(container.sizeDelta.x / cols, container.sizeDelta.y / rows);
            var color = VisualDefaults.GetColor(token.Color);

            float startX = -(cols * cell) / 2f + cell / 2f;
            float startY = (rows * cell) / 2f - cell / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    bool filled = occupied.Contains(new Vector2Int(x, y));
                    var img = UIFactory.CreatePanel(container, "c" + x + "_" + y, filled ? color : new Color(1f, 1f, 1f, 0.05f));
                    img.rectTransform.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                    img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchoredPosition = new Vector2(startX + x * cell, startY - y * cell);
                }
            }
        }
    }
}
