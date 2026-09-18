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
        private Button[] _slotButtons;
        private RectTransform[] _previewContainers;
        private int _selectedIndex = -1;
        private bool _interactable = true;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public RectTransform Build(Transform parent, DeckManager deck)
        {
            _deck = deck;

            var container = UIFactory.CreateUIObject("HandContainer", parent);
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _slotBackgrounds = new Image[DeckManager.HandSize];
            _slotButtons = new Button[DeckManager.HandSize];
            _previewContainers = new RectTransform[DeckManager.HandSize];

            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreatePanel(container, "Slot" + i, UITheme.ButtonIdle);
                slot.rectTransform.sizeDelta = new Vector2(120f, 140f);
                // Plain Image/Button has no ILayoutElement, so without this the
                // parent VerticalLayoutGroup has no size to read and collapses
                // the slot toward zero instead of respecting sizeDelta.
                var slotLayout = slot.gameObject.AddComponent<LayoutElement>();
                slotLayout.preferredWidth = 120f;
                slotLayout.preferredHeight = 140f;
                var btn = slot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSlotClicked(idx));
                _slotButtons[i] = btn;

                // Fills most of the slot now that there's no name/color label
                // below it — the shape + color-icon preview alone (plus the
                // color-icon badge on each filled square) is clear enough on
                // its own.
                var previewContainer = UIFactory.CreateUIObject("Preview", slot.transform);
                previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
                previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
                previewContainer.pivot = new Vector2(0.5f, 0.5f);
                previewContainer.anchoredPosition = Vector2.zero;
                previewContainer.sizeDelta = new Vector2(100f, 110f);

                _slotBackgrounds[i] = slot;
                _previewContainers[i] = previewContainer;
            }

            Refresh();
            return container;
        }

        private void OnSlotClicked(int idx)
        {
            if (!_interactable || idx >= _deck.Hand.Count)
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

        /// <summary>
        /// Blocks (and visually greys out) hand selection — used while a
        /// placement's feedback sequence is still animating, so a new
        /// selection can't be made until the player has seen the current one
        /// resolve. The Button.interactable flag alone already stops clicks;
        /// this also dims each slot toward UITheme.Background so the state is
        /// visible, not just enforced.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                _slotButtons[i].interactable = interactable;
            }
            UpdateSelectionVisuals();
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(DeckManager deck)
        {
            _deck = deck;
            _selectedIndex = -1;
            _interactable = true;
            Refresh();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                bool hasPiece = i < _deck.Hand.Count;
                bool selected = i == _selectedIndex;
                var baseColor = !hasPiece ? UITheme.Panel : (selected ? UITheme.ButtonSelected : UITheme.ButtonIdle);
                // Same "recede into the void" treatment as locked grid cells
                // (see VisualDefaults.LockedColor) — dims toward the
                // background instead of a one-off grey, so it reads as part
                // of the same visual language rather than a new state.
                _slotBackgrounds[i].color = _interactable ? baseColor : Color.Lerp(baseColor, UITheme.Background, 0.7f);
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
                    var rotation = _deck.HandRotations[i];
                    BuildShapePreview(preview, token, rotation);
                }
            }
            UpdateSelectionVisuals();
        }

        private void BuildShapePreview(RectTransform container, PieceToken token, PieceRotation rotation)
        {
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
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
            // Y increases UPWARD here too, to match GridView's own convention
            // (see its "y increases upward" comment) — otherwise this preview
            // renders every shape vertically flipped from how it actually looks
            // once placed on the grid, which defeats the point of showing the
            // piece's real (now randomized) rotation.
            float startY = -(rows * cell) / 2f + cell / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    bool filled = occupied.Contains(new Vector2Int(x, y));
                    var img = UIFactory.CreatePanel(container, "c" + x + "_" + y, filled ? color : new Color(1f, 1f, 1f, 0.05f));
                    img.rectTransform.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                    img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchoredPosition = new Vector2(startX + x * cell, startY + y * cell);

                    // Same colorblind-accessibility icon as a filled grid
                    // cell (see GridCellView), so the hand preview already
                    // shows a piece's color both ways before it's even
                    // placed. Skipped for a color with no icon yet (Coral).
                    if (filled)
                    {
                        var icon = VisualDefaults.GetColorIcon(token.Color);
                        if (icon != null)
                        {
                            var iconImg = UIFactory.CreatePanel(img.transform, "Icon", Color.white);
                            iconImg.sprite = icon;
                            UIFactory.StretchFull(iconImg.rectTransform);
                        }
                    }
                }
            }
        }
    }
}
