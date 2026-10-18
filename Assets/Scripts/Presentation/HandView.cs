using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders the current hand of up to 3 pieces as selectable slots. Both
    /// click-to-select and drag-and-drop select and place a piece through the
    /// same underlying path: selecting fires <see cref="SlotSelected"/>, and a
    /// drop on a grid cell (see GridCellView.OnDrop) reuses the same
    /// CellClicked path a plain click on the grid uses. Either path shows a
    /// cursor-following ghost of the selected piece that tracks the pointer
    /// every frame (see Update()).
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        private const float DragGhostWidth = 150f;
        private const float DragGhostHeight = 150f;
        private const float DragGhostPreviewWidth = 142f;
        private const float DragGhostPreviewHeight = 142f;
        private const float SlotLevelLabelHeight = 22f;
        private const float HandBoxPadding = 12f;
        // Every hand preview uses this same tile size, so a 2x2 piece and a 3x2 piece show equally big tiles; sized so the widest piece (3 cells) fits the 142px preview box.
        private const float PreviewTileSize = 56f;
        private const float GhostTileSize = 46f;
        private static readonly Color SlotDividerColor = new Color(0.075f, 0.13f, 0.18f, 0.35f);
        private static readonly Color SlotClear = new Color(1f, 1f, 1f, 0f);
        /// <summary>Largest side a slot preview may span: the slot minus its border and a little breathing room.</summary>
        private const float SlotPreviewBudget = 140f;
        private const float SlotWidth = 150f;
        private const float SlotHeight = 150f;
        private const float SlotSpacing = 22f;
        private const float ShuffleButtonWidth = 46f;
        private const float ShuffleButtonHeight = 48f;
        // The ghost hides fully over a valid spot since the grid's own
        // footprint preview (GridCellView.SetHoverTint) already covers that
        // case; it stays faded elsewhere as a "not placed yet" cue.
        private const float CursorGhostValidAlpha = 0f;
        private const float CursorGhostInvalidAlpha = 0.5f;
        private const float ShufflePulseAmplitude = 0.08f;
        private const float ShufflePulseSpeed = 1.5f;
        // Short, decaying side-to-side shake on a slot the player can't pick up (see SelectSlot).
        private const float UnplayableShakeDuration = 0.45f;
        private const float UnplayableShakeMagnitude = 15f;
        private const float UnplayableShakeCycles = 5f;
        // When the whole hand is stuck, the Shuffle button pulses much harder than its usual gentle breathing.
        private const float StuckPulseAmplitude = 0.22f;
        private const float StuckPulseSpeed = 5.5f;
        // Light scale-up pulse on the piece preview itself (not the whole slot) when it's picked up, whether
        // by a plain click-select or the start of a drag (BeginSlotDrag always routes through SelectSlot) —
        // smaller peak than GridCellView.Pulse's 1.18 since this should read as a light acknowledgement, not
        // a scoring-style pop.
        private const float SelectPulseDuration = 0.22f;
        private const float SelectPulsePeakScale = 1.1f;
        private const float SelectPulsePeakFraction = 0.4f;

        public event Action<int> SlotSelected;

        /// <summary>Fires when re-clicking the already-selected slot deselects it; GameBootstrap uses this to clear the grid's selected-shape preview.</summary>
        public event Action SelectionCleared;

        /// <summary>Fires instead of <see cref="SlotSelected"/> when the player tries to pick up a piece with no valid placement on the grid; the slot shakes (see UnplayableShakeDuration) and stays unselected.</summary>
        public event Action<int> SlotUnplayable;

        /// <summary>Fires when the player clicks the Shuffle icon on the hand box.</summary>
        public event Action ShuffleRequested;

        private DeckManager _deck;
        private RunManager _run;
        private TooltipView _tooltip;
        private Image[] _slotBackgrounds;
        private Button[] _slotButtons;
        private RectTransform[] _previewContainers;
        private CanvasGroup[] _previewCanvasGroups;
        private Text[] _slotLevelLabels;
        private Text[] _slotLockLabels;
        private Button _shuffleButton;
        private Text _shuffleCountLabel;
        private Coroutine _shufflePulseCoroutine;
        private Coroutine _stuckPulseCoroutine;
        private bool _handStuck;
        private Coroutine[] _shakeCoroutines;
        private Coroutine[] _selectPulseCoroutines;
        private bool _shuffleAllowed = true;
        private int _selectedIndex = -1;
        private bool _interactable = true;
        private int? _bossLockedSlotIndex;

        private Transform _dragLayerParent;
        private RectTransform _dragGhost;
        private CanvasGroup _dragGhostCanvasGroup;
        private RectTransform _dragGhostPreview;

        // Built once for DeckManager.MaxHandSize slots (same "build the max, show/hide the rest" pattern as
        // HudView's enemy band) so ModifierId.QuatriemeEmplacement can reveal the 4th slot instantly without
        // rebuilding the hand box — see UpdateHandSizeLayout, called from Refresh.
        private Image _handContainer;
        private RectTransform _slotsRow;
        private RectTransform[] _slotDividers;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        /// <summary>A hand slot's own fixed-position preview container (see Build) — unlike the piece inside it, this stays put regardless of what's currently shown there, so a caller can still anchor an effect on a slot that's since been emptied (e.g. Thief's steal — see GameBootstrap.PlayThiefStealEffect). Null if index is out of range.</summary>
        public RectTransform GetSlotPreviewTransform(int index)
        {
            if (index < 0 || index >= DeckManager.MaxHandSize)
            {
                return null;
            }
            return _previewContainers[index];
        }

        public RectTransform Build(Transform parent, RunManager run, TooltipView tooltip)
        {
            _run = run;
            _deck = run.Deck;
            _tooltip = tooltip;
            _dragLayerParent = parent;

            // Box starts sized for the base HandSize, same as before this slot was introduced — only the
            // hand's effective size (which starts at HandSize, see DeckManager) ever grows it (see
            // UpdateHandSizeLayout, called at the end of this method and from every Refresh after).
            var containerImage = UIFactory.CreatePanel(parent, "HandContainer", Color.white);
            containerImage.sprite = UISprites.SlotGroupBackground;
            containerImage.type = Image.Type.Simple;
            containerImage.raycastTarget = false;
            var container = containerImage.rectTransform;
            _handContainer = containerImage;

            var slotsRow = UIFactory.CreateUIObject("SlotsRow", container);
            slotsRow.anchorMin = new Vector2(0.5f, 0.5f);
            slotsRow.anchorMax = new Vector2(0.5f, 0.5f);
            slotsRow.pivot = new Vector2(0.5f, 0.5f);
            var layout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            _slotsRow = slotsRow;

            _slotBackgrounds = new Image[DeckManager.MaxHandSize];
            _slotButtons = new Button[DeckManager.MaxHandSize];
            _previewContainers = new RectTransform[DeckManager.MaxHandSize];
            _previewCanvasGroups = new CanvasGroup[DeckManager.MaxHandSize];
            _slotLevelLabels = new Text[DeckManager.MaxHandSize];
            _slotLockLabels = new Text[DeckManager.MaxHandSize];
            _shakeCoroutines = new Coroutine[DeckManager.MaxHandSize];
            _selectPulseCoroutines = new Coroutine[DeckManager.MaxHandSize];

            for (int i = 0; i < DeckManager.MaxHandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreateSlicedImage(slotsRow, "Slot" + i, UISprites.CardBackground);
                // No card art: the slot is an invisible hit area that only tints for selection/feedback (see UpdateSelectionVisuals).
                slot.color = SlotClear;
                slot.rectTransform.sizeDelta = new Vector2(SlotWidth, SlotHeight);
                // Plain Image/Button has no ILayoutElement; the row's HorizontalLayoutGroup needs explicit preferred dimensions.
                var slotLayout = slot.gameObject.AddComponent<LayoutElement>();
                slotLayout.preferredWidth = SlotWidth;
                slotLayout.preferredHeight = SlotHeight;
                var btn = slot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSlotClicked(idx));
                _slotButtons[i] = btn;

                var dragHandler = slot.gameObject.AddComponent<HandSlotDragHandler>();
                dragHandler.Init(this, idx);

                var previewContainer = UIFactory.CreateUIObject("Preview", slot.transform);
                previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
                previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
                previewContainer.pivot = new Vector2(0.5f, 0.5f);
                previewContainer.anchoredPosition = Vector2.zero;
                previewContainer.sizeDelta = new Vector2(146f, 146f);
                // Wraps just the piece's preview, not the slot's background/level/lock chrome, so a shuffle can fade the piece alone (see FadeSlotPieces).
                var previewCanvasGroup = previewContainer.gameObject.AddComponent<CanvasGroup>();

                var levelLabel = UIFactory.CreateText(slot.transform, "PieceLevel", "", 14, Color.black);
                levelLabel.raycastTarget = false;
                levelLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
                levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
                levelLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
                levelLabel.rectTransform.anchoredPosition = new Vector2(0f, 3f);
                levelLabel.rectTransform.sizeDelta = new Vector2(0f, SlotLevelLabelHeight);
                // No level readout in the slot any more — the preview takes the room instead.
                levelLabel.gameObject.SetActive(false);

                var lockLabel = UIFactory.CreateText(slot.transform, "BossLock", "", 11, UITheme.Danger);
                lockLabel.raycastTarget = false;
                lockLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
                lockLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
                lockLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
                lockLabel.rectTransform.anchoredPosition = new Vector2(0f, -5f);
                lockLabel.rectTransform.sizeDelta = new Vector2(0f, 18f);

                _slotBackgrounds[i] = slot;
                _previewContainers[i] = previewContainer;
                _previewCanvasGroups[i] = previewCanvasGroup;
                _slotLevelLabels[i] = levelLabel;
                _slotLockLabels[i] = lockLabel;
            }

            // Thin vertical separators between slots, positioned in UpdateHandSizeLayout.
            _slotDividers = new RectTransform[DeckManager.MaxHandSize - 1];
            for (int i = 0; i < _slotDividers.Length; i++)
            {
                var divider = UIFactory.CreatePanel(slotsRow, "Divider" + i, SlotDividerColor);
                divider.raycastTarget = false;
                divider.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var dr = divider.rectTransform;
                dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(0.5f, 0.5f);
                dr.sizeDelta = new Vector2(3f, SlotHeight * 0.62f);
                _slotDividers[i] = dr;
            }

            BuildShuffleButton(container);
            BuildDragGhost();

            Refresh();
            return container;
        }

        /// <summary>Builds the Shuffle button in the hand box's upper-right corner, with the remaining-use count centered over the icon.</summary>
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

        /// <summary>Updates the Shuffle icon's remaining-use count and enabled state.</summary>
        public void SetShuffleState(int remaining, bool canShuffle)
        {
            _shuffleCountLabel.text = remaining.ToString();
            _shuffleAllowed = canShuffle;
            _shuffleButton.interactable = _interactable && _shuffleAllowed;
        }

        /// <summary>Starts/stops a slow breathing scale pulse on the Shuffle button.</summary>
        public void SetShufflePulsing(bool pulsing)
        {
            // While the whole hand is stuck the strong pulse owns the button.
            if (_handStuck)
            {
                return;
            }
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

        /// <summary>
        /// True when no piece in the hand fits anywhere on the grid (and a Shuffle is still available): the Shuffle button pulses
        /// hard and every slot breathes red — before the player even tries to pick a piece. Recomputed on every
        /// <see cref="Refresh"/>.
        /// </summary>
        private void UpdateStuckState()
        {
            bool stuck = false;
            if (_run.State == RunState.InProgress && _run.ShufflesRemaining > 0)
            {
                var shapes = new List<PieceShape>();
                for (int i = 0; i < _deck.Hand.Count; i++)
                {
                    if (_deck.Hand[i].HasValue && !IsBossLockedSlot(i))
                    {
                        shapes.Add(PieceShapeCatalog.GetRotated(_deck.Hand[i].Value.Shape, _deck.HandRotations[i]));
                    }
                }
                stuck = shapes.Count > 0 && !_run.Grid.HasAnyValidPlacement(shapes);
            }
            SetHandStuck(stuck);
        }

        private void SetHandStuck(bool stuck)
        {
            if (stuck == _handStuck)
            {
                return;
            }
            _handStuck = stuck;
            if (stuck)
            {
                // Hand over from the gentle breathing pulse to the strong one.
                if (_shufflePulseCoroutine != null)
                {
                    StopCoroutine(_shufflePulseCoroutine);
                    _shufflePulseCoroutine = null;
                }
                _stuckPulseCoroutine = StartCoroutine(StuckPulseRoutine());
                return;
            }
            if (_stuckPulseCoroutine != null)
            {
                StopCoroutine(_stuckPulseCoroutine);
                _stuckPulseCoroutine = null;
            }
            _shuffleButton.transform.localScale = Vector3.one;
            UpdateSelectionVisuals();
        }

        private IEnumerator StuckPulseRoutine()
        {
            while (true)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * StuckPulseSpeed);
                float scale = 1f + StuckPulseAmplitude * wave;
                _shuffleButton.transform.localScale = new Vector3(scale, scale, 1f);
                // The hand can be smaller than the slot array (effective hand size), so bound by the hand.
                for (int i = 0; i < _deck.Hand.Count && i < _slotBackgrounds.Length; i++)
                {
                    if (_deck.Hand[i].HasValue && _slotBackgrounds[i].gameObject.activeSelf && _interactable)
                    {
                        _slotBackgrounds[i].color = new Color(1f, 0.62f, 0.58f, 0.3f * wave);
                    }
                }
                yield return null;
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
        /// Floating preview that follows the pointer while a hand slot is
        /// selected, parented at the HUD level (<see cref="_dragLayerParent"/>)
        /// so it renders above the grid/hand/HUD. <see cref="CanvasGroup.blocksRaycasts"/>
        /// is off so it never steals a click/drop raycast. Alpha reflects
        /// placement validity (see <see cref="SetHoveringValidDrop"/>).
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
            // Always selects (never the OnSlotClicked toggle-off path) so starting a drag on the selected slot keeps it selected.
            SelectSlot(index);
        }

        /// <summary>Updates the cursor ghost's opacity based on grid hover validity.</summary>
        public void SetHoveringValidDrop(bool valid)
        {
            if (_selectedIndex >= 0)
            {
                _dragGhostCanvasGroup.alpha = valid ? CursorGhostValidAlpha : CursorGhostInvalidAlpha;
            }
        }

        /// <summary>Forwards drag events so fast pointer movement doesn't lag a frame behind the per-frame Update() tracking.</summary>
        public void DragSlot(int index, PointerEventData eventData)
        {
            if (_selectedIndex != index)
            {
                return;
            }
            UpdateGhostPosition(eventData.position);
        }

        /// <summary>Ghost visibility depends only on whether a piece is selected (see SelectSlot/ClearSelection), so an ended drag without a valid drop leaves the piece selected.</summary>
        public void EndSlotDrag(PointerEventData eventData)
        {
        }

        private int _hoverIndex = -1;
        private const float HoverScale = 1.1f;
        private const float HoverScaleSpeed = 14f;

        /// <summary>Pointer entered/left a slot; the hovered piece grows slightly and the slot gets a faint highlight (see UpdateSelectionVisuals).</summary>
        public void SetSlotHover(int index, bool hovering)
        {
            if (hovering)
            {
                _hoverIndex = index;
            }
            else if (_hoverIndex == index)
            {
                _hoverIndex = -1;
            }
            if (_slotBackgrounds != null && _deck != null)
            {
                UpdateSelectionVisuals();
            }
        }

        private bool IsHoverActive(int index)
        {
            return index == _hoverIndex && _interactable && _deck != null && index < _deck.Hand.Count
                && _deck.Hand[index].HasValue && !IsBossLockedSlot(index);
        }

        private void UpdateHoverScale()
        {
            if (_previewContainers == null)
            {
                return;
            }
            for (int i = 0; i < _previewContainers.Length; i++)
            {
                float target = IsHoverActive(i) && i != _selectedIndex ? HoverScale : 1f;
                var rt = _previewContainers[i];
                float current = rt.localScale.x;
                if (Mathf.Abs(current - target) < 0.001f)
                {
                    continue;
                }
                float next = Mathf.MoveTowards(current, target, HoverScaleSpeed * (HoverScale - 1f) * Time.unscaledDeltaTime);
                rt.localScale = new Vector3(next, next, 1f);
            }
        }

        private void Update()
        {
            UpdateHoverScale();
            if (_selectedIndex < 0)
            {
                return;
            }
            UpdateGhostPosition(Input.mousePosition);
        }

        private void ShowCursorGhost(int index)
        {
            var token = _deck.Hand[index].Value;
            var rotation = _deck.HandRotations[index];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            for (int c = _dragGhostPreview.childCount - 1; c >= 0; c--)
            {
                Destroy(_dragGhostPreview.GetChild(c).gameObject);
            }
            ShapePreviewFactory.Build(_dragGhostPreview, shape, token.Color, token.Trait, _tooltip, null, GhostTileSize);

            _dragGhostCanvasGroup.alpha = CursorGhostInvalidAlpha;
            _dragGhost.gameObject.SetActive(true);
            _dragGhost.SetAsLastSibling();
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
            // A piece with nowhere legal to go never becomes selected/draggable; it shakes in place instead.
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
                SfxManager.Play(SfxId.InvalidDrop);
                if (SlotUnplayable != null)
                {
                    SlotUnplayable(idx);
                }
                return;
            }

            _selectedIndex = idx;
            UpdateSelectionVisuals();
            ShowCursorGhost(idx);
            PulseSelectedPiece(idx);
            if (SlotSelected != null)
            {
                SlotSelected(idx);
            }
        }

        /// <summary>Light scale-up pulse on just the piece preview, played whenever its slot is picked up (see SelectSlot's own doc comment).</summary>
        private void PulseSelectedPiece(int index)
        {
            if (_selectPulseCoroutines[index] != null)
            {
                StopCoroutine(_selectPulseCoroutines[index]);
            }
            _selectPulseCoroutines[index] = StartCoroutine(SelectPulseRoutine(index));
        }

        private IEnumerator SelectPulseRoutine(int index)
        {
            var rt = _previewContainers[index];
            float t = 0f;
            while (t < SelectPulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SelectPulseDuration);
                float scale = p < SelectPulsePeakFraction
                    ? Mathf.Lerp(1f, SelectPulsePeakScale, p / SelectPulsePeakFraction)
                    : Mathf.Lerp(SelectPulsePeakScale, 1f, (p - SelectPulsePeakFraction) / (1f - SelectPulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _selectPulseCoroutines[index] = null;
        }

        /// <summary>Short, decaying side-to-side shake on a slot's own background.</summary>
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
                // Red flash that fades as the shake dies down.
                _slotBackgrounds[index].color = new Color(1f, 0.45f, 0.4f, 0.45f * damping);
                yield return null;
            }
            rect.anchoredPosition = originalPos;
            _shakeCoroutines[index] = null;
            if (!_handStuck)
            {
                UpdateSelectionVisuals();
            }
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

        /// <summary>Blocks and visually greys out hand selection while a placement's feedback sequence animates.</summary>
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
            _dragGhost.gameObject.SetActive(false);
            Refresh();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                bool selected = i == _selectedIndex;
                bool lockedByBoss = _bossLockedSlotIndex.HasValue && _bossLockedSlotIndex.Value == i;
                _slotBackgrounds[i].color = lockedByBoss ? new Color(0.8f, 0.2f, 0.2f, 0.16f)
                    : selected && _interactable ? new Color(1f, 1f, 1f, 0.35f)
                    : IsHoverActive(i) ? new Color(1f, 1f, 1f, 0.14f)
                    : SlotClear;
                if (_slotLockLabels != null)
                {
                    _slotLockLabels[i].text = lockedByBoss ? "LOCKED" : string.Empty;
                }
            }
        }

        /// <summary>Instantly sets every hand slot's piece preview alpha (slot chrome untouched), so <see cref="FadeSlotPieces"/> can fade freshly-built pieces in instead of popping at full opacity.</summary>
        public void SetSlotPiecesAlpha(float alpha)
        {
            for (int i = 0; i < _previewCanvasGroups.Length; i++)
            {
                _previewCanvasGroups[i].alpha = alpha;
            }
        }

        /// <summary>Fades each hand slot's piece preview one slot at a time, left to right, each taking <paramref name="duration"/> seconds.</summary>
        public IEnumerator FadeSlotPieces(float from, float to, float duration)
        {
            for (int i = 0; i < _previewCanvasGroups.Length; i++)
            {
                yield return FadeOneSlot(_previewCanvasGroups[i], from, to, duration);
            }
        }

        private static IEnumerator FadeOneSlot(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = to;
        }

        public void Refresh()
        {
            UpdateHandSizeLayout();
            for (int i = 0; i < _deck.Hand.Count; i++)
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
            UpdateStuckState();
        }

        /// <summary>
        /// Shows/hides each slot's GameObject to match the hand's current effective size (see
        /// DeckManager.SetEffectiveHandSize) and resizes the hand box/slots row to fit exactly that many —
        /// HorizontalLayoutGroup/ContentSizeFitter would recenter around however many children are active on
        /// their own, but the box's own outer background (_handContainer, a plain Image, not a layout
        /// element) needs its size driven explicitly. A no-op resync at the base HandSize for every player
        /// who never buys QuatriemeEmplacement.
        /// </summary>
        private void UpdateHandSizeLayout()
        {
            int activeCount = _deck.Hand.Count;
            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                _slotBackgrounds[i].gameObject.SetActive(i < activeCount);
            }
            float slotsWidth = activeCount * SlotWidth + (activeCount - 1) * SlotSpacing;
            _slotsRow.sizeDelta = new Vector2(slotsWidth, SlotHeight);
            if (_slotDividers != null)
            {
                for (int i = 0; i < _slotDividers.Length; i++)
                {
                    _slotDividers[i].gameObject.SetActive(i < activeCount - 1);
                    _slotDividers[i].anchoredPosition = new Vector2(-slotsWidth / 2f + (i + 1) * SlotWidth + (i + 0.5f) * SlotSpacing, 0f);
                }
            }
            _handContainer.rectTransform.sizeDelta = new Vector2(slotsWidth + HandBoxPadding * 2f, SlotHeight + HandBoxPadding * 2f);
        }

        /// <summary>Same as <see cref="Refresh"/>, except every slot is shown empty regardless of the live model, to hold the new hand hidden until the current placement's feedback finishes. Call <see cref="Refresh"/> afterward to reveal it.</summary>
        public void RefreshHoldingEmpty()
        {
            UpdateHandSizeLayout();
            for (int i = 0; i < _deck.Hand.Count; i++)
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
            // Shrinks tiles for tall/wide shapes so they stay inside the slot's border instead of overlapping it.
            int maxExtent = 1;
            for (int i = 0; i < shape.Cells.Count; i++)
            {
                maxExtent = Mathf.Max(maxExtent, shape.Cells[i].x + 1, shape.Cells[i].y + 1);
            }
            float cellSize = Mathf.Min(PreviewTileSize, SlotPreviewBudget / maxExtent);
            ShapePreviewFactory.Build(container, shape, token.Color, token.Trait, _tooltip, clickForwardTarget, cellSize);
        }
    }
}
