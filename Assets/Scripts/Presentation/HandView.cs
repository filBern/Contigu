using System;
using System.Collections;
using Contigu.Core;
using Contigu.Data;
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
        private const float SlotLevelLabelHeight = 22f;
        private const float HandBoxPadding = 12f;
        private const float SlotWidth = 120f;
        private const float SlotHeight = 140f;
        private const float SlotSpacing = 16f;
        private const float ShuffleButtonWidth = 46f;
        private const float ShuffleButtonHeight = 48f;
        // On explicit clarification: the "100%" the player wants over a
        // valid spot is the GRID's own footprint preview (GridCellView.
        // SetHoverTint, already exactly cell-snapped) — not the cursor
        // ghost itself, which only ever loosely follows the raw pointer.
        // So the ghost fully hides once valid (0f) rather than trying to
        // compete with that already-correct preview, and stays a faded 50%
        // everywhere else as the "not placed yet" cue.
        private const float CursorGhostValidAlpha = 0f;
        private const float CursorGhostInvalidAlpha = 0.5f;
        // Same slow "breathing" feel as GameBootstrap's idle status-text
        // pulse (StatusPulseAmplitude/Speed) — explicit request: "On
        // devrait mettre en valeur le shuffle button en même temps (slow
        // pulse)".
        private const float ShufflePulseAmplitude = 0.08f;
        private const float ShufflePulseSpeed = 1.5f;
        // Short, decaying side-to-side shake — explicit request: "qu'on lui
        // fasse une animation de vibration courte" — on a slot the player
        // just tried to pick up but can't (see SelectSlot).
        private const float UnplayableShakeDuration = 0.3f;
        private const float UnplayableShakeMagnitude = 8f;
        private const float UnplayableShakeCycles = 4f;

        public event Action<int> SlotSelected;

        /// <summary>Fires when re-clicking the already-selected slot toggles it off (on explicit request) — GameBootstrap uses this to clear the grid's selected-shape preview the same way a successful placement does.</summary>
        public event Action SelectionCleared;

        /// <summary>Fires instead of <see cref="SlotSelected"/> when the player tries to pick up a piece that has no valid placement anywhere on the grid (explicit request: "Lorsqu'une pièce ne peut pas être joué, j'aimerais qu'elle ne puisse pas être récupéré") — the slot shakes (see UnplayableShakeDuration) and stays unselected; GameBootstrap uses this to show an emphasized status message and pulse the Shuffle button.</summary>
        public event Action<int> SlotUnplayable;

        /// <summary>Fires when the player clicks the Shuffle icon on the hand box — GameBootstrap forwards this to RunManager.ShuffleHand and refreshes the hand/button state with the result (see RunManager.ShuffleHand/ShufflesRemaining).</summary>
        public event Action ShuffleRequested;

        private DeckManager _deck;
        private RunManager _run;
        private TooltipView _tooltip;
        private Image[] _slotBackgrounds;
        private Button[] _slotButtons;
        private RectTransform[] _previewContainers;
        private Text[] _slotLevelLabels;
        private Text[] _slotLockLabels;
        private Button _shuffleButton;
        private Text _shuffleCountLabel;
        private Coroutine _shufflePulseCoroutine;
        private Coroutine[] _shakeCoroutines;
        private bool _shuffleAllowed = true;
        private int _selectedIndex = -1;
        private bool _interactable = true;
        private int? _bossLockedSlotIndex;

        private Transform _dragLayerParent;
        private RectTransform _dragGhost;
        private CanvasGroup _dragGhostCanvasGroup;
        private RectTransform _dragGhostPreview;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public RectTransform Build(Transform parent, RunManager run, TooltipView tooltip)
        {
            _run = run;
            _deck = run.Deck;
            _tooltip = tooltip;
            _dragLayerParent = parent;

            float slotsWidth = DeckManager.HandSize * SlotWidth + (DeckManager.HandSize - 1) * SlotSpacing;
            var containerImage = UIFactory.CreatePanel(parent, "HandContainer", Color.white);
            containerImage.sprite = UISprites.SlotGroupBackground;
            containerImage.type = Image.Type.Simple;
            containerImage.raycastTarget = false;
            var container = containerImage.rectTransform;
            // Asset bounds: 3 × 120px slots + 2 × 16px gaps, with 12px
            // padding on each side = 416 × 164 UI units. SlotGroup.png is
            // displayed across these bounds.
            container.sizeDelta = new Vector2(slotsWidth + HandBoxPadding * 2f, SlotHeight + HandBoxPadding * 2f);

            var slotsRow = UIFactory.CreateUIObject("SlotsRow", container);
            slotsRow.anchorMin = new Vector2(0.5f, 0.5f);
            slotsRow.anchorMax = new Vector2(0.5f, 0.5f);
            slotsRow.pivot = new Vector2(0.5f, 0.5f);
            slotsRow.sizeDelta = new Vector2(slotsWidth, SlotHeight);
            var layout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _slotBackgrounds = new Image[DeckManager.HandSize];
            _slotButtons = new Button[DeckManager.HandSize];
            _previewContainers = new RectTransform[DeckManager.HandSize];
            _slotLevelLabels = new Text[DeckManager.HandSize];
            _slotLockLabels = new Text[DeckManager.HandSize];
            _shakeCoroutines = new Coroutine[DeckManager.HandSize];

            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreateSlicedImage(slotsRow, "Slot" + i, UISprites.CardBackground);
                UIFactory.AddThickOutline(slot, UITheme.Border);
                slot.rectTransform.sizeDelta = new Vector2(SlotWidth, SlotHeight);
                // Plain Image/Button has no ILayoutElement, so the row's
                // HorizontalLayoutGroup needs explicit preferred dimensions.
                var slotLayout = slot.gameObject.AddComponent<LayoutElement>();
                slotLayout.preferredWidth = SlotWidth;
                slotLayout.preferredHeight = SlotHeight;
                var btn = slot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSlotClicked(idx));
                _slotButtons[i] = btn;

                var dragHandler = slot.gameObject.AddComponent<HandSlotDragHandler>();
                dragHandler.Init(this, idx);

                // Leave room at the bottom for the piece's combined mastery level.
                var previewContainer = UIFactory.CreateUIObject("Preview", slot.transform);
                previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
                previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
                previewContainer.pivot = new Vector2(0.5f, 0.5f);
                previewContainer.anchoredPosition = new Vector2(0f, 10f);
                previewContainer.sizeDelta = new Vector2(100f, 110f);

                var levelLabel = UIFactory.CreateText(slot.transform, "PieceLevel", "", 14, Color.black);
                levelLabel.raycastTarget = false;
                levelLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
                levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
                levelLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
                levelLabel.rectTransform.anchoredPosition = new Vector2(0f, 3f);
                levelLabel.rectTransform.sizeDelta = new Vector2(0f, SlotLevelLabelHeight);

                var lockLabel = UIFactory.CreateText(slot.transform, "BossLock", "", 11, UITheme.Danger);
                lockLabel.raycastTarget = false;
                lockLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
                lockLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
                lockLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
                lockLabel.rectTransform.anchoredPosition = new Vector2(0f, -5f);
                lockLabel.rectTransform.sizeDelta = new Vector2(0f, 18f);

                _slotBackgrounds[i] = slot;
                _previewContainers[i] = previewContainer;
                _slotLevelLabels[i] = levelLabel;
                _slotLockLabels[i] = lockLabel;
            }

            BuildShuffleButton(container);
            BuildDragGhost();

            Refresh();
            return container;
        }

        /// <summary>
        /// Anchored inside the hand box's upper-right corner — re-rolls all 3 slots at once
        /// for a limited number of uses per run (spec extension, explicit
        /// request: "un bouton shuffle qui permet de shuffle les 3 slots de
        /// pièce au hasard. Le joueur a droit à 10 shuffle"). Reuses the
        /// supplied round Shuffle icon, with the remaining-use count centered
        /// on top of it instead of a text label.
        /// </summary>
        private void BuildShuffleButton(Transform container)
        {
            _shuffleButton = UIFactory.CreateButton(container, "ShuffleButton", "", UISprites.ShuffleIcon, 15);
            var shuffleImage = _shuffleButton.GetComponent<Image>();
            shuffleImage.type = Image.Type.Simple;
            shuffleImage.color = UITheme.LightBlue;
            var buttonRect = _shuffleButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(20f, 20f);
            buttonRect.sizeDelta = new Vector2(ShuffleButtonWidth, ShuffleButtonHeight);

            // The icon itself is the button background; show the remaining
            // shuffle count centered directly over it, with no word label or
            // separate count badge.
            _shuffleCountLabel = _shuffleButton.GetComponentInChildren<Text>();
            _shuffleCountLabel.text = _run.ShufflesRemaining.ToString();
            _shuffleCountLabel.color = UITheme.TextPrimary;
            _shuffleCountLabel.raycastTarget = false;
            UIFactory.StretchFull(_shuffleCountLabel.rectTransform);

            _shuffleButton.onClick.AddListener(() =>
            {
                if (ShuffleRequested != null)
                {
                    ShuffleRequested();
                }
            });
        }

        /// <summary>Updates the Shuffle icon's centered remaining-use count and enabled state — called by GameBootstrap whenever RunManager.ShufflesRemaining changes (a successful shuffle, or a fresh/restarted run). Combined with <see cref="_interactable"/> (see SetInteractable) so a shuffle can't be triggered mid-animation any more than a slot click can.</summary>
        public void SetShuffleState(int remaining, bool canShuffle)
        {
            _shuffleCountLabel.text = remaining.ToString();
            _shuffleAllowed = canShuffle;
            _shuffleButton.interactable = _interactable && _shuffleAllowed;
        }

        /// <summary>
        /// Starts/stops a slow "breathing" scale pulse on the Shuffle
        /// button — GameBootstrap turns this on the moment the player
        /// selects a piece that has nowhere valid to go anywhere on the
        /// board (explicit request: "on empêche le joueur de perdre son
        /// temps a essayer de trouver un endroit a placer la piece ... on
        /// devrait mettre en valeur le shuffle button en même temps (slow
        /// pulse)"), pointing them at the way out instead of leaving them to
        /// discover Shuffle on their own. Same continuous sine-wave idea as
        /// GameBootstrap's own idle status-text pulse, just on this button.
        /// </summary>
        public void SetShufflePulsing(bool pulsing)
        {
            if (pulsing)
            {
                if (_shufflePulseCoroutine == null)
                {
                    _shufflePulseCoroutine = StartCoroutine(PulseShuffleButton());
                }
                return;
            }
            if (_shufflePulseCoroutine != null)
            {
                StopCoroutine(_shufflePulseCoroutine);
                _shufflePulseCoroutine = null;
                _shuffleButton.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator PulseShuffleButton()
        {
            while (true)
            {
                float scale = 1f + ShufflePulseAmplitude * Mathf.Sin(Time.time * ShufflePulseSpeed);
                _shuffleButton.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
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
            if (!_interactable || IsBossLockedSlot(index) || !_deck.Hand[index].HasValue)
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
        public void DragSlot(int index, PointerEventData eventData)
        {
            if (_selectedIndex != index)
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
            if (!_interactable || IsBossLockedSlot(idx) || !_deck.Hand[idx].HasValue)
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
            // Checked up front, before the piece is actually picked up —
            // explicit request: "Lorsqu'une pièce ne peut pas être joué,
            // j'aimerais qu'elle ne puisse pas être récupéré" — a piece
            // with nowhere legal to go anywhere on the board never becomes
            // selected/draggable; it just shakes in place instead.
            var token = _deck.Hand[idx].Value;
            var rotation = _deck.HandRotations[idx];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            if (!_run.Grid.HasAnyValidPlacement(new[] { shape }))
            {
                if (_shakeCoroutines[idx] != null)
                {
                    StopCoroutine(_shakeCoroutines[idx]);
                }
                _shakeCoroutines[idx] = StartCoroutine(ShakeSlot(idx));
                if (SlotUnplayable != null)
                {
                    SlotUnplayable(idx);
                }
                return;
            }

            _selectedIndex = idx;
            UpdateSelectionVisuals();
            ShowCursorGhost(idx);
            if (SlotSelected != null)
            {
                SlotSelected(idx);
            }
        }

        /// <summary>Short, decaying side-to-side shake on a slot's own background — see UnplayableShakeDuration's own doc comment.</summary>
        private IEnumerator ShakeSlot(int index)
        {
            var rect = _slotBackgrounds[index].rectTransform;
            Vector2 originalPos = rect.anchoredPosition;
            float t = 0f;
            while (t < UnplayableShakeDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / UnplayableShakeDuration);
                float damping = 1f - progress;
                float offsetX = Mathf.Sin(progress * UnplayableShakeCycles * Mathf.PI * 2f) * UnplayableShakeMagnitude * damping;
                rect.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
                yield return null;
            }
            rect.anchoredPosition = originalPos;
            _shakeCoroutines[index] = null;
        }

        private bool IsBossLockedSlot(int index)
        {
            return _bossLockedSlotIndex.HasValue && _bossLockedSlotIndex.Value == index;
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
            UpdateSlotInteractability();
            _shuffleButton.interactable = interactable && _shuffleAllowed;
            UpdateSelectionVisuals();
        }

        /// <summary>Applies or clears the boss's round-long hand-slot lock without preventing Shuffle.</summary>
        public void SetBossLockedSlot(int? slotIndex)
        {
            _bossLockedSlotIndex = slotIndex;
            if (_selectedIndex >= 0 && _bossLockedSlotIndex == _selectedIndex)
            {
                ClearSelection();
                if (SelectionCleared != null)
                {
                    SelectionCleared();
                }
            }
            UpdateSlotInteractability();
            UpdateSelectionVisuals();
        }

        private void UpdateSlotInteractability()
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                bool bossLocked = _bossLockedSlotIndex.HasValue && _bossLockedSlotIndex.Value == i;
                _slotButtons[i].interactable = _interactable && !bossLocked;
            }
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) DeckManager instance.</summary>
        public void Rebind(RunManager run)
        {
            _run = run;
            _deck = run.Deck;
            _bossLockedSlotIndex = run.BossLockedHandSlotIndex;
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
                // Every slot background is the same darker Panel tint (on
                // explicit request: "Les slots non sélectionné sont
                // difficile a voir leur pièce, met les plus foncé. Idem pour
                // lorsqu'ils sont sélectionné") — the old idle/selected
                // tints (ButtonIdle #5f699c, ButtonSelected #65aed6) were
                // both LIGHTER than the empty-slot Panel tint and close in
                // hue to the piece colors themselves (esp. Blue, #3498db),
                // so a piece could all but disappear into its own slot.
                // Selected slots get a slight lift toward white instead —
                // a follow-up request dropped the border-frame overlay this
                // used to show selection with ("j'aime pas le cadre de
                // sélection, peux-tu le retirer et juste mettre légèrement
                // plus clair"), so selection is now just this small tint
                // step on the same background rather than a separate
                // element drawn over the slot.
                var baseColor = selected ? Color.Lerp(UITheme.Panel, Color.white, 0.25f) : UITheme.Panel;
                // Same "recede into the void" treatment as locked grid cells
                // (see VisualDefaults.LockedColor) — dims toward the
                // background instead of a one-off grey, so it reads as part
                // of the same visual language rather than a new state.
                bool lockedByBoss = _bossLockedSlotIndex.HasValue && _bossLockedSlotIndex.Value == i;
                _slotBackgrounds[i].color = !_interactable
                    ? Color.Lerp(baseColor, UITheme.Background, 0.7f)
                    : lockedByBoss ? Color.Lerp(baseColor, UITheme.Background, 0.55f) : baseColor;
                if (_slotLockLabels != null)
                {
                    _slotLockLabels[i].text = lockedByBoss ? "LOCKED" : string.Empty;
                }
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
                    int level = _run.GetColorMasteryLevel(token.Color) + _run.GetShapeMasteryLevel(token.Shape) - 1;
                    _slotLevelLabels[i].text = "Lv. " + level;
                }
                else
                {
                    _slotLevelLabels[i].text = string.Empty;
                }
            }
            UpdateSlotInteractability();
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// Same as <see cref="Refresh"/>, except every slot is shown empty
        /// regardless of the live model — used when this placement emptied
        /// the hand and RunManager.PlacePiece already auto-refilled it with
        /// a brand new hand (and resolved each alive enemy's own On-Shuffle
        /// effect) internally, synchronously, before any animation even
        /// started: without this hold, the new pieces (and the enemy's
        /// freshly moved lock/poison — see GridView.RefreshHoldingClearedCells's
        /// deferredLockCells) would already be visible while the player is
        /// still watching THIS placement's own score/damage play out
        /// (explicit request: "il faut attendre la fin de décompte de point
        /// avant de faire l'action de shuffle et les effets des ennemies
        /// qui vont avec"). GameBootstrap calls a plain <see cref="Refresh"/>
        /// again once that sequence finishes to reveal the real hand.
        /// </summary>
        public void RefreshHoldingEmpty()
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                var preview = _previewContainers[i];
                for (int c = preview.childCount - 1; c >= 0; c--)
                {
                    Destroy(preview.GetChild(c).gameObject);
                }
                _slotLevelLabels[i].text = string.Empty;
            }
            UpdateSlotInteractability();
            UpdateSelectionVisuals();
        }

        private void BuildShapePreview(RectTransform container, PieceToken token, PieceRotation rotation, GameObject clickForwardTarget)
        {
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            ShapePreviewFactory.Build(container, shape, token.Color, token.Trait, _tooltip, clickForwardTarget);
        }
    }
}
