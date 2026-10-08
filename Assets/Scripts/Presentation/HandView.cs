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
        // The ghost hides fully over a valid spot since the grid's own
        // footprint preview (GridCellView.SetHoverTint) already covers that
        // case; it stays faded elsewhere as a "not placed yet" cue.
        private const float CursorGhostValidAlpha = 0f;
        private const float CursorGhostInvalidAlpha = 0.5f;
        private const float ShufflePulseAmplitude = 0.08f;
        private const float ShufflePulseSpeed = 1.5f;
        // Short, decaying side-to-side shake on a slot the player can't pick up (see SelectSlot).
        private const float UnplayableShakeDuration = 0.3f;
        private const float UnplayableShakeMagnitude = 8f;
        private const float UnplayableShakeCycles = 4f;

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
            _previewCanvasGroups = new CanvasGroup[DeckManager.HandSize];
            _slotLevelLabels = new Text[DeckManager.HandSize];
            _slotLockLabels = new Text[DeckManager.HandSize];
            _shakeCoroutines = new Coroutine[DeckManager.HandSize];

            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                int idx = i;
                var slot = UIFactory.CreateSlicedImage(slotsRow, "Slot" + i, UISprites.CardBackground);
                UIFactory.AddThickOutline(slot, UITheme.Border);
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

                // Leave room at the bottom for the piece's combined mastery level.
                var previewContainer = UIFactory.CreateUIObject("Preview", slot.transform);
                previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
                previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
                previewContainer.pivot = new Vector2(0.5f, 0.5f);
                previewContainer.anchoredPosition = new Vector2(0f, 10f);
                previewContainer.sizeDelta = new Vector2(100f, 110f);
                // Wraps just the piece's preview, not the slot's background/level/lock chrome, so a shuffle can fade the piece alone (see FadeSlotPieces).
                var previewCanvasGroup = previewContainer.gameObject.AddComponent<CanvasGroup>();

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
                _previewCanvasGroups[i] = previewCanvasGroup;
                _slotLevelLabels[i] = levelLabel;
                _slotLockLabels[i] = lockLabel;
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
            var token = _deck.Hand[index].Value;
            var rotation = _deck.HandRotations[index];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            for (int c = _dragGhostPreview.childCount - 1; c >= 0; c--)
            {
                Destroy(_dragGhostPreview.GetChild(c).gameObject);
            }
            ShapePreviewFactory.Build(_dragGhostPreview, shape, token.Color, token.Trait, _tooltip, null);

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
                var baseColor = selected ? Color.Lerp(UITheme.Panel, Color.white, 0.25f) : UITheme.Panel;
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

        /// <summary>Same as <see cref="Refresh"/>, except every slot is shown empty regardless of the live model, to hold the new hand hidden until the current placement's feedback finishes. Call <see cref="Refresh"/> afterward to reveal it.</summary>
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
