using System;
using Contigu.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders the current hand of up to 3 pieces as selectable slots — both
    /// click-to-select (a quick tap, per the original toggle flow) and
    /// drag-and-drop (dragging a slot onto the grid, on explicit request
    /// after playtesting) select and place a piece the same way underneath:
    /// selecting fires <see cref="SlotSelected"/> exactly as before, and a
    /// drop on a grid cell (see GridCellView.OnDrop) reuses the same
    /// CellClicked path a plain click on the grid already used.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        private const float DragGhostWidth = 120f;
        private const float DragGhostHeight = 140f;
        private const float DragGhostPreviewWidth = 100f;
        private const float DragGhostPreviewHeight = 110f;
        private const float DragGhostAlpha = 0.85f;

        public event Action<int> SlotSelected;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private Image[] _slotBackgrounds;
        private Button[] _slotButtons;
        private RectTransform[] _previewContainers;
        private int _selectedIndex = -1;
        private bool _interactable = true;

        private Transform _dragLayerParent;
        private RectTransform _dragGhost;
        private RectTransform _dragGhostPreview;
        private int _draggingIndex = -1;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public RectTransform Build(Transform parent, DeckManager deck, TooltipView tooltip)
        {
            _deck = deck;
            _tooltip = tooltip;
            _dragLayerParent = parent;

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

                var dragHandler = slot.gameObject.AddComponent<HandSlotDragHandler>();
                dragHandler.Init(this, idx);

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

            BuildDragGhost();

            Refresh();
            return container;
        }

        /// <summary>
        /// Floating preview that follows the pointer while a hand slot is
        /// being dragged — parented at the same level as the whole HUD
        /// (<see cref="_dragLayerParent"/>, brought to front on show) so it
        /// renders above the grid/hand/HUD regardless of where the drag
        /// started. <see cref="CanvasGroup.blocksRaycasts"/> is off so it
        /// never steals the drop raycast meant for the grid cell underneath.
        /// </summary>
        private void BuildDragGhost()
        {
            var ghost = UIFactory.CreatePanel(_dragLayerParent, "HandDragGhost", UITheme.ButtonSelected);
            _dragGhost = ghost.rectTransform;
            _dragGhost.sizeDelta = new Vector2(DragGhostWidth, DragGhostHeight);
            _dragGhost.pivot = new Vector2(0.5f, 0.5f);
            var canvasGroup = ghost.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = DragGhostAlpha;

            _dragGhostPreview = UIFactory.CreateUIObject("Preview", _dragGhost);
            _dragGhostPreview.anchorMin = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.anchorMax = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.pivot = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.anchoredPosition = Vector2.zero;
            _dragGhostPreview.sizeDelta = new Vector2(DragGhostPreviewWidth, DragGhostPreviewHeight);

            _dragGhost.gameObject.SetActive(false);
        }

        public void BeginSlotDrag(int index, PointerEventData eventData)
        {
            if (!_interactable || index >= _deck.Hand.Count)
            {
                return;
            }
            OnSlotClicked(index);
            _draggingIndex = index;
            ShowDragGhost(index);
            UpdateGhostPosition(eventData);
        }

        public void DragSlot(PointerEventData eventData)
        {
            if (_draggingIndex < 0)
            {
                return;
            }
            UpdateGhostPosition(eventData);
        }

        /// <summary>
        /// Always safe to call even if the drop already placed the piece
        /// (GridCellView.OnDrop fires and resolves the placement BEFORE
        /// Unity calls OnEndDrag on the source, per the standard uGUI event
        /// order) — this just hides the now-stale ghost either way.
        /// </summary>
        public void EndSlotDrag(PointerEventData eventData)
        {
            _draggingIndex = -1;
            _dragGhost.gameObject.SetActive(false);
        }

        private void ShowDragGhost(int index)
        {
            var token = _deck.Hand[index];
            var rotation = _deck.HandRotations[index];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            for (int c = _dragGhostPreview.childCount - 1; c >= 0; c--)
            {
                Destroy(_dragGhostPreview.GetChild(c).gameObject);
            }
            ShapePreviewFactory.Build(_dragGhostPreview, shape, token.Color, token.Trait, _tooltip, null);

            _dragGhost.gameObject.SetActive(true);
            _dragGhost.SetAsLastSibling();
        }

        private void UpdateGhostPosition(PointerEventData eventData)
        {
            var parentRect = (RectTransform)_dragLayerParent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, null, out var localPoint);
            _dragGhost.anchoredPosition = localPoint;
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
                    BuildShapePreview(preview, token, rotation, _slotBackgrounds[i].gameObject);
                }
            }
            UpdateSelectionVisuals();
        }

        private void BuildShapePreview(RectTransform container, PieceToken token, PieceRotation rotation, GameObject clickForwardTarget)
        {
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            ShapePreviewFactory.Build(container, shape, token.Color, token.Trait, _tooltip, clickForwardTarget);
        }
    }
}
