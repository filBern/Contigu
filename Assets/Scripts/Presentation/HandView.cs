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
    /// CellClicked path a plain click on the grid already used. As soon as
    /// EITHER path selects a slot, a cursor-following ghost of that piece
    /// appears and tracks the pointer every frame (see Update()) — on
    /// explicit request, this used to only happen while actively dragging;
    /// now click-select gets the exact same live preview.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        private const float DragGhostWidth = 120f;
        private const float DragGhostHeight = 140f;
        private const float DragGhostPreviewWidth = 100f;
        private const float DragGhostPreviewHeight = 110f;
        // On explicit clarification: the "100%" the player wants over a
        // valid spot is the GRID's own footprint preview (GridCellView.
        // SetHoverTint, already exactly cell-snapped) — not the cursor
        // ghost itself, which only ever loosely follows the raw pointer.
        // So the ghost fully hides once valid (0f) rather than trying to
        // compete with that already-correct preview, and stays a faded 50%
        // everywhere else as the "not placed yet" cue.
        private const float CursorGhostValidAlpha = 0f;
        private const float CursorGhostInvalidAlpha = 0.5f;

        public event Action<int> SlotSelected;

        /// <summary>Fires when re-clicking the already-selected slot toggles it off (on explicit request) — GameBootstrap uses this to clear the grid's selected-shape preview the same way a successful placement does.</summary>
        public event Action SelectionCleared;

        private DeckManager _deck;
        private TooltipView _tooltip;
        private Image[] _slotBackgrounds;
        private Button[] _slotButtons;
        private RectTransform[] _previewContainers;
        private Image[] _selectionOverlays;
        private int _selectedIndex = -1;
        private bool _interactable = true;

        private Transform _dragLayerParent;
        private RectTransform _dragGhost;
        private CanvasGroup _dragGhostCanvasGroup;
        private RectTransform _dragGhostPreview;

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
            _selectionOverlays = new Image[DeckManager.HandSize];

            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreateSlicedImage(container, "Slot" + i, UISprites.CardBackground);
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

                // Selection indicator, drawn as a FRAME only (fillCenter =
                // false skips the 9-slice's center region, leaving just its
                // 14px sliced border opaque) instead of a full translucent
                // wash over the whole slot. A prior version covered the
                // WHOLE slot including the piece preview, on the reasoning
                // that a background-only tint was too easy to miss once a
                // piece's own colors cover most of the slot — but that in
                // turn greyed out the piece itself while selected, which is
                // exactly what got reported next: "j'aimerais qu'on grey out
                // pas le preview dans la slot lorsqu'elle est sélectionné".
                // The border-only frame keeps the selected state just as
                // unmissable (drawn last/topmost, fully opaque) without ever
                // drawing over the preview area in the middle; raycastTarget
                // off so it never swallows the click meant for the slot's
                // own Button.
                var selectionOverlay = UIFactory.CreateSlicedImage(slot.transform, "SelectionOverlay", UISprites.CardBackground);
                UIFactory.StretchFull(selectionOverlay.rectTransform);
                selectionOverlay.fillCenter = false;
                selectionOverlay.color = UITheme.ButtonSelected;
                selectionOverlay.raycastTarget = false;
                selectionOverlay.gameObject.SetActive(false);

                _slotBackgrounds[i] = slot;
                _previewContainers[i] = previewContainer;
                _selectionOverlays[i] = selectionOverlay;
            }

            BuildDragGhost();

            Refresh();
            return container;
        }

        /// <summary>
        /// Floating preview that follows the pointer for as long as a hand
        /// slot is selected — whether that selection came from a plain
        /// click or an active drag, they're the same thing to this ghost
        /// (see Update()) — parented at the same level as the whole HUD
        /// (<see cref="_dragLayerParent"/>, brought to front on show) so it
        /// renders above the grid/hand/HUD regardless of where the pointer
        /// is. <see cref="CanvasGroup.blocksRaycasts"/> is off so it never
        /// steals a click/drop raycast meant for whatever's underneath. No
        /// background panel — just the shape preview itself — and its alpha
        /// reflects placement validity (see <see cref="SetHoveringValidDrop"/>):
        /// fully hidden over a valid spot, since the grid's own footprint
        /// preview (GridCellView.SetHoverTint) is already exactly
        /// cell-snapped and at full opacity there — this loosely-cursor-
        /// following ghost would only compete with it; faded (50%) as the
        /// "not placed yet" cue everywhere else.
        /// </summary>
        private void BuildDragGhost()
        {
            _dragGhost = UIFactory.CreateUIObject("HandDragGhost", _dragLayerParent);
            _dragGhost.sizeDelta = new Vector2(DragGhostWidth, DragGhostHeight);
            _dragGhost.pivot = new Vector2(0.5f, 0.5f);
            _dragGhostCanvasGroup = _dragGhost.gameObject.AddComponent<CanvasGroup>();
            _dragGhostCanvasGroup.blocksRaycasts = false;
            _dragGhostCanvasGroup.alpha = CursorGhostInvalidAlpha;

            _dragGhostPreview = UIFactory.CreateUIObject("Preview", _dragGhost);
            _dragGhostPreview.anchorMin = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.anchorMax = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.pivot = new Vector2(0.5f, 0.5f);
            _dragGhostPreview.anchoredPosition = Vector2.zero;
            _dragGhostPreview.sizeDelta = new Vector2(DragGhostPreviewWidth, DragGhostPreviewHeight);

            _dragGhost.gameObject.SetActive(false);
        }

        public void BeginSlotDrag(int index)
        {
            if (!_interactable || !_deck.Hand[index].HasValue)
            {
                return;
            }
            // Unconditionally selects (never the OnSlotClicked toggle-off
            // path below) — starting a drag on the already-selected slot
            // must keep it selected for the drop, not deselect it. The
            // ghost is already shown/tracking the cursor as soon as ANY
            // selection happens (see SelectSlot) — a drag doesn't need to
            // do anything extra for it anymore.
            SelectSlot(index);
        }

        /// <summary>Fired by GameBootstrap from GridView.HoverValidityChanged whenever hover validity changes over the grid — updates the cursor ghost's opacity (see CursorGhostValidAlpha/CursorGhostInvalidAlpha) regardless of whether the current selection came from a click or a drag.</summary>
        public void SetHoveringValidDrop(bool valid)
        {
            if (_selectedIndex >= 0)
            {
                _dragGhostCanvasGroup.alpha = valid ? CursorGhostValidAlpha : CursorGhostInvalidAlpha;
            }
        }

        /// <summary>Kept for immediacy during an actual drag gesture — Update() already tracks the cursor every frame regardless of drag state, but forwarding the drag's own event here avoids a single-frame lag while the pointer is moving fast.</summary>
        public void DragSlot(PointerEventData eventData)
        {
            if (_selectedIndex < 0)
            {
                return;
            }
            UpdateGhostPosition(eventData.position);
        }

        /// <summary>
        /// No longer hides the ghost unconditionally — its visibility is now
        /// purely a function of whether a piece is SELECTED (see
        /// SelectSlot/ClearSelection), not whether a drag happens to still
        /// be in progress, so a drag that ends without a valid drop
        /// correctly leaves the ghost (and the piece) selected and ready to
        /// place again instead of stranding the player with no visual cue.
        /// </summary>
        public void EndSlotDrag(PointerEventData eventData)
        {
        }

        private void Update()
        {
            if (_selectedIndex < 0)
            {
                return;
            }
            UpdateGhostPosition(Input.mousePosition);
        }

        private void ShowCursorGhost(int index)
        {
            // SelectSlot already guarded that this slot is occupied.
            var token = _deck.Hand[index].Value;
            var rotation = _deck.HandRotations[index];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            for (int c = _dragGhostPreview.childCount - 1; c >= 0; c--)
            {
                Destroy(_dragGhostPreview.GetChild(c).gameObject);
            }
            ShapePreviewFactory.Build(_dragGhostPreview, shape, token.Color, token.Trait, _tooltip, null);

            // Not yet known to be over a valid spot — starts faded until the
            // next grid hover event says otherwise (see SetHoveringValidDrop).
            _dragGhostCanvasGroup.alpha = CursorGhostInvalidAlpha;
            _dragGhost.gameObject.SetActive(true);
            _dragGhost.SetAsLastSibling();
            // Snaps to the current cursor position immediately rather than
            // waiting for the next Update() tick, so it doesn't visibly lag
            // one frame behind a fresh selection.
            UpdateGhostPosition(Input.mousePosition);
        }

        private void UpdateGhostPosition(Vector2 screenPosition)
        {
            var parentRect = (RectTransform)_dragLayerParent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, null, out var localPoint);
            _dragGhost.anchoredPosition = localPoint;
        }

        private void OnSlotClicked(int idx)
        {
            if (!_interactable || !_deck.Hand[idx].HasValue)
            {
                return;
            }
            if (_selectedIndex == idx)
            {
                // Re-clicking the already-selected slot deselects it instead
                // of just re-firing the same selection (on explicit
                // request) — mirrors the same clearing GameBootstrap does
                // after a successful placement.
                ClearSelection();
                if (SelectionCleared != null)
                {
                    SelectionCleared();
                }
                return;
            }
            SelectSlot(idx);
        }

        private void SelectSlot(int idx)
        {
            _selectedIndex = idx;
            UpdateSelectionVisuals();
            ShowCursorGhost(idx);
            if (SlotSelected != null)
            {
                SlotSelected(idx);
            }
        }

        public void ClearSelection()
        {
            _selectedIndex = -1;
            UpdateSelectionVisuals();
            _dragGhost.gameObject.SetActive(false);
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
            // In case a piece was still selected (and its cursor ghost
            // still showing) at the moment of the restart — Refresh() below
            // doesn't touch the ghost on its own.
            _dragGhost.gameObject.SetActive(false);
            Refresh();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                bool selected = i == _selectedIndex;
                // Every slot background is now the same darker Panel tint
                // regardless of occupied/idle/selected state (on explicit
                // request: "Les slots non sélectionné sont difficile a voir
                // leur pièce, met les plus foncé. Idem pour lorsqu'ils sont
                // sélectionné") — the old idle/selected tints (ButtonIdle
                // #5f699c, ButtonSelected #65aed6) were both LIGHTER than
                // the empty-slot Panel tint and close in hue to the piece
                // colors themselves (esp. Blue, #3498db), so a piece could
                // all but disappear into its own slot. Selection is now
                // shown by the border-only frame overlay below instead of a
                // brighter fill, so darkening this doesn't cost legibility.
                var baseColor = UITheme.Panel;
                // Same "recede into the void" treatment as locked grid cells
                // (see VisualDefaults.LockedColor) — dims toward the
                // background instead of a one-off grey, so it reads as part
                // of the same visual language rather than a new state.
                _slotBackgrounds[i].color = _interactable ? baseColor : Color.Lerp(baseColor, UITheme.Background, 0.7f);
                _selectionOverlays[i].gameObject.SetActive(selected && _interactable);
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

                if (_deck.Hand[i].HasValue)
                {
                    var token = _deck.Hand[i].Value;
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
