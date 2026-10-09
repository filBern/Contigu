using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Builds the entire uGUI tree from code and wires it to a
    /// <see cref="RunManager"/>. Attach this to an otherwise-empty GameObject in
    /// the bootstrap scene — no prefabs or hand-authored scene UI required.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const float CellSize = VisualDefaults.GridCellSize;
        private const float ScoreEventStaggerSeconds = 0.22f;
        private const float LineClearStaggerSeconds = 0.14f;
        // Lueur groups play before the score cascade, at a flat pace (not
        // the combo speedup ramp) since each one waits for its flying popup
        // to land before the reveal reads clearly.
        private const float LueurGroupStaggerSeconds = 0.3f;
        // Each combo addition waits 3% less than the previous one, floored
        // so a long chain still keeps a perceptible beat instead of
        // collapsing to an instant dump.
        private const float ComboSpeedupFactor = 0.97f;
        private const float MinStaggerSeconds = 0.1f;
        private const float RoundEndLueurBonusStaggerSeconds = 0.1f;
        // Gentle ±5% scale wobble, distinct from ModifierPanelView.Pulse's
        // sharper one-shot flash used for score feedback.
        private const float StatusPulseAmplitude = 0.05f;
        private const float StatusPulseSpeed = 1.5f;
        // Shared by the manual Shuffle button and an auto-refill Shuffle's
        // event sequence (see PlayManualShuffleSequence and
        // PlayPlacementSequence's handWasAboutToAutoRefill branch). Applied
        // per element — HandView.FadeSlotPieces/GridView.FadeMalus fade one
        // slot/cell at a time, not the whole group at once.
        private const float ShuffleFadeDuration = 0.12f;
        private const string IdleStatusMessage = "Select or drag a piece onto the grid.";
        private const int DefaultStatusFontSize = 19;
        private const int EmphasizedStatusFontSize = 27;
        private const string TutorialSeenPrefsKey = "TutorialSeen";

        private RunManager _run;
        private IMetaStatsStore _metaStatsStore;
        private MetaStats _metaStats;

        private GridView _gridView;
        private HandView _handView;
        private HudView _hudView;
        private ComboView _comboView;
        private ShopView _shopView;
        private DraftView _draftView;
        private TileChoiceView _tileChoiceView;
        private PieceChoiceView _pieceChoiceView;
        private ModifierUpgradeChoiceView _modifierUpgradeChoiceView;
        private UpgradeRevealView _upgradeRevealView;
        private ModifierCarouselView _modifierCarouselView;
        private ShapeCarouselView _shapeCarouselView;
        private ColorCarouselView _colorCarouselView;
        private ModifierPanelView _modifierPanelView;
        private TooltipView _tooltipView;
        private DeckView _deckView;
        private EndScreenView _endScreenView;
        private TutorialView _tutorialView;
        private ChallengeSelectView _challengeSelectView;
        private SettingsView _settingsView;
        private AnimatedBackgroundView _animatedBackgroundView;
        private FeedbackLayer _feedbackLayer;
        private Text _statusText;
        private Coroutine _statusPulseCoroutine;
        private bool _isPlayingPlacementSequence;

        private void Awake()
        {
            EnsureEventSystem();
            var canvasRect = BuildCanvas();

            _run = new RunManager(new SystemRandomProvider());
            _metaStatsStore = new MetaStatsFileStore();
            _metaStats = _metaStatsStore.Load();

            BuildUI(canvasRect);
            WireEvents();
            RefreshAll();

            // RefreshAll covers grid/hand/HUD/modifier panel; the shop and
            // deck-view overlay are only rebuilt if actually open, since a
            // hidden one renders correctly next time it's shown anyway.
            ColorblindMode.Changed += OnColorblindModeChanged;

            // Applies the persisted Master volume immediately at launch,
            // then again any time SettingsView's slider changes it.
            VolumeSettings.Changed += ApplyVolumeSettings;
            ApplyVolumeSettings();
            SfxManager.PlayMusic();

            // MainMenuBootstrap's "Play" button loads this scene fresh, so
            // the Classic run built above is never actually played —
            // ChallengeSelectView replaces it with whichever challenge gets
            // picked. TutorialView's first-launch auto-show (see
            // OnChallengeChosen) fires right after.
            _challengeSelectView.Show(_metaStats);
        }

        /// <summary>Scales every AudioSource in the scene (see AudioListener.volume) by VolumeSettings.MasterVolume — real, immediate effect even with zero clips loaded yet, unlike Music/SFX which have no bus of their own to attenuate without an AudioMixer.</summary>
        private static void ApplyVolumeSettings()
        {
            AudioListener.volume = VolumeSettings.MasterVolume;
        }

        private void OnColorblindModeChanged()
        {
            RefreshAll();
            if (_run.State == RunState.AwaitingShop)
            {
                _shopView.Refresh(_run);
            }
            if (_deckView.IsVisible)
            {
                _deckView.Show();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F7))
            {
                DebugForceUpgradeSlotShortcut();
            }
            if (Input.GetKeyDown(KeyCode.F8))
            {
                DebugTriggerRandomModifierShortcut();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                DebugForceRoundWin();
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                DebugGrantLueurShortcut();
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                DebugGrantStarsShortcut();
            }
#endif
            // Unlike the F-key shortcuts above, these bindings are always
            // available, including in real builds.
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _deckView.Toggle();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                ColorblindMode.Toggle();
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                _tutorialView.Show();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _settingsView.Toggle();
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                TrySellHoveredModifier();
            }
        }

        /// <summary>Sells the currently hovered modifier badge; ModifierPanelView.HoveredRowIndex is -1 while the panel is blocked mid-animation. No-op if nothing's hovered or the run isn't InProgress/AwaitingShop.</summary>
        private void TrySellHoveredModifier()
        {
            if (_run.State != RunState.InProgress && _run.State != RunState.AwaitingShop)
            {
                return;
            }
            int hoveredIndex = _modifierPanelView.HoveredRowIndex;
            if (hoveredIndex < 0 || hoveredIndex >= _run.ActiveModifiers.Count)
            {
                return;
            }

            var id = _run.ActiveModifiers[hoveredIndex];
            var badgeAnchor = _modifierPanelView.GetBadgeTransform(id, hoveredIndex);
            if (!_run.SellModifier(hoveredIndex, out int refundedLueur))
            {
                return;
            }

            if (badgeAnchor != null)
            {
                _feedbackLayer.SpawnFlyingPopup(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + refundedLueur, VisualDefaults.GoldenColor);
            }
            _modifierPanelView.Refresh(_run.ActiveModifiers);
            _hudView.SetLueur(_run.Lueur);
            if (_run.State == RunState.AwaitingShop)
            {
                // A sale can drop ActiveModifiers.Count back under the cap,
                // which un-caps the shop's "Full" buttons — refresh to
                // reflect that.
                _shopView.Refresh(_run);
            }
            SetStatusText("Sold " + ModifierCatalog.Get(id).Name + " for " + refundedLueur + " Lueur.");
        }

#if UNITY_EDITOR
        /// <summary>Editor-only debug shortcut (F7): forces upgrade slot 0 to "Random Modifier" so the real Buy button path (BuyUpgradeSlot) can be tested directly. No-op with a status message if the shop isn't open.</summary>
        private void DebugForceUpgradeSlotShortcut()
        {
            if (!_run.DebugForceUpgradeSlotToRandomModifier(0))
            {
                SetStatusText("F7: open the shop first.");
                return;
            }
            _shopView.Refresh(_run);
            SetStatusText("F7: upgrade slot 1 is now Random Modifier — buy it for real.");
        }

        /// <summary>Editor-only debug shortcut (F8): triggers the Random Modifier grant+reveal directly, bypassing the shop's roll odds. Works any time, unlike a real purchase.</summary>
        private void DebugTriggerRandomModifierShortcut()
        {
            var granted = _run.DebugTriggerRandomModifierGrant();
            RefreshAll();
            if (_run.State == RunState.AwaitingShop)
            {
                _shopView.Refresh(_run);
            }
            if (granted.HasValue)
            {
                _modifierCarouselView.Show(granted.Value, "Random modifier");
            }
        }

        /// <summary>Editor-only debug shortcut (F9): instantly completes the current round.</summary>
        private void DebugForceRoundWin()
        {
            if (_isPlayingPlacementSequence || _run.State != RunState.InProgress)
            {
                return;
            }
            var state = _run.DebugForceRoundComplete();
            RefreshAll();
            HandleStateTransition(state);
        }

        /// <summary>Editor-only debug shortcut (F10): grants 100 Lueur instantly.</summary>
        private void DebugGrantLueurShortcut()
        {
            _run.DebugGrantLueur(100);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        /// <summary>Editor-only debug shortcut (F11): grants 100 Stars instantly and saves. Refreshing the picker is harmless even while hidden — it re-reads current state next time it's shown.</summary>
        private void DebugGrantStarsShortcut()
        {
            _metaStats.Stars += 100;
            _metaStatsStore.Save(_metaStats);
            _challengeSelectView.Refresh(_metaStats);
        }
#endif

        /// <summary>Starts/stops the idle-prompt pulse depending on whether <paramref name="text"/> is the default idle message; other callers leave it off. <paramref name="emphasize"/> bumps the font size to <see cref="EmphasizedStatusFontSize"/>.</summary>
        private void SetStatusText(string text, bool emphasize = false)
        {
            _statusText.text = text;
            _statusText.fontSize = emphasize ? EmphasizedStatusFontSize : DefaultStatusFontSize;
            if (text == IdleStatusMessage)
            {
                StartStatusPulse();
            }
            else
            {
                StopStatusPulse();
            }
        }

        private void StartStatusPulse()
        {
            if (_statusPulseCoroutine == null)
            {
                _statusPulseCoroutine = StartCoroutine(PulseStatusText());
            }
        }

        private void StopStatusPulse()
        {
            if (_statusPulseCoroutine != null)
            {
                StopCoroutine(_statusPulseCoroutine);
                _statusPulseCoroutine = null;
            }
            _statusText.rectTransform.localScale = Vector3.one;
        }

        /// <summary>Continuous sine-wave scale wobble (never a one-shot flash like ModifierPanelView.Pulse) — runs for as long as the idle prompt stays on screen, stopped/reset by SetStatusText the moment it's replaced by anything else.</summary>
        private System.Collections.IEnumerator PulseStatusText()
        {
            while (true)
            {
                float scale = 1f + StatusPulseAmplitude * Mathf.Sin(Time.time * StatusPulseSpeed);
                _statusText.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private RectTransform BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Rounds UI element positions to whole pixels — without it,
            // ScaleWithScreenSize's non-integer scale factor leaves text at
            // sub-pixel offsets that read as blurry under anti-aliasing.
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            // The layout is a fixed-height vertical stack (HUD + status +
            // grid + hand), so match on HEIGHT (1): the canvas is always
            // exactly 800 units tall regardless of aspect ratio, so the
            // hand row never gets squeezed off-screen on wide/short windows.
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.CreatePanel(canvasGo.transform, "Background", UITheme.Background);
            UIFactory.StretchFull(bg.rectTransform);

            // Sits directly above the flat fill and below everything built
            // in BuildUI below (MainRoot, added as canvasGo's next child
            // after this) — see AnimatedBackgroundView's own doc comment.
            _animatedBackgroundView = gameObject.AddComponent<AnimatedBackgroundView>();
            _animatedBackgroundView.Build(canvasGo.transform);

            return canvasGo.GetComponent<RectTransform>();
        }

        private void BuildUI(RectTransform canvas)
        {
            var mainRoot = UIFactory.CreateUIObject("MainRoot", canvas);
            UIFactory.StretchFull(mainRoot);

            _comboView = gameObject.AddComponent<ComboView>();
            var comboRect = _comboView.Build(mainRoot);
            comboRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboRect.pivot = new Vector2(0.5f, 0.5f);
            comboRect.anchoredPosition = new Vector2(0f, 65f);

            // Built before HudView/GridView/HandView since all three need a
            // live TooltipView to hover (enemy icons, grid trait badges,
            // hand piece trait badges).
            _tooltipView = gameObject.AddComponent<TooltipView>();
            _tooltipView.Build(mainRoot);

            // Both progress bars pin themselves to the top/bottom edges inside
            // HudView.Build — nothing to position here.
            _hudView = gameObject.AddComponent<HudView>();
            _hudView.Build(mainRoot, mainRoot, _tooltipView);
            _hudView.SetLueur(_run.Lueur);

            _statusText = UIFactory.CreateText(mainRoot, "Status", IdleStatusMessage, DefaultStatusFontSize, UITheme.TextMutedOnBackground);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Below the top bar/enemy band (HudView.EnemyBandHeight is
            // taller than the plain score bar) with a 12px gap.
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, -(HudView.EnemyBandHeight + 12f));
            _statusText.rectTransform.sizeDelta = new Vector2(700f, 26f);
            StartStatusPulse();

            // Slightly above screen center to make room for the horizontal
            // hand row below while keeping a clear gap below the status text.
            _gridView = gameObject.AddComponent<GridView>();
            var gridRect = _gridView.Build(mainRoot, _run.Grid, CellSize, _tooltipView);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, 55f);

            // Center each side panel in the gap between the grid edge and
            // the screen edge. Computed from Screen.width/height (matching
            // CanvasScaler's match-height formula — see BuildCanvas) rather
            // than mainRoot.rect.width, which isn't guaranteed to reflect
            // CanvasScaler's applied scale factor yet at this point in a
            // synchronous Build call, and could under-compute canvasWidth
            // on a wide-aspect screen.
            float halfGridWidth = gridRect.rect.width * 0.5f;
            float canvasWidth = Screen.height > 0 ? 800f * Screen.width / Screen.height : mainRoot.rect.width;
            float halfCanvasWidth = canvasWidth * 0.5f;
            float sidePanelCenterX = (halfGridWidth + halfCanvasWidth) * 0.5f;

            // Clamp so the combo card (the widest thing at sidePanelCenterX)
            // never hangs off the screen edge on a narrow aspect ratio.
            // Needs the card's real, content-fitted width, hence the forced
            // rebuild here. Prioritizes staying on-screen over exact
            // centering in the gap.
            LayoutRebuilder.ForceRebuildLayoutImmediate(comboRect);
            const float SidePanelEdgeMargin = 12f;
            float maxSidePanelCenterX = halfCanvasWidth - comboRect.rect.width * 0.5f - SidePanelEdgeMargin;
            if (sidePanelCenterX > maxSidePanelCenterX)
            {
                sidePanelCenterX = maxSidePanelCenterX;
            }
            comboRect.anchoredPosition = new Vector2(sidePanelCenterX, 65f);

            var lueurRect = _hudView.LueurLabelTransform;
            lueurRect.anchorMin = new Vector2(0.5f, 0.5f);
            lueurRect.anchorMax = new Vector2(0.5f, 0.5f);
            lueurRect.pivot = new Vector2(0.5f, 0.5f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(lueurRect);
            lueurRect.anchoredPosition = new Vector2(
                sidePanelCenterX,
                comboRect.anchoredPosition.y + comboRect.rect.height * 0.5f + 12f + lueurRect.rect.height * 0.5f);

            // Center the three horizontal piece slots beneath the grid;
            // HandView anchors Shuffle separately just to the row's right.
            _handView = gameObject.AddComponent<HandView>();
            var handRect = _handView.Build(mainRoot, _run, _tooltipView);
            handRect.anchorMin = new Vector2(0.5f, 0.5f);
            handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = new Vector2(0.5f, 0.5f);
            handRect.anchoredPosition = new Vector2(0f, -230f);
            BuildComboUtilityButtons(comboRect);

            _feedbackLayer = gameObject.AddComponent<FeedbackLayer>();
            _feedbackLayer.Build(mainRoot);

            // Layered in this order so later ones render on top: the shop
            // is the base screen, the sub-choice/tile-choice overlays cover
            // it once a purchase needs a follow-up.
            _shopView = gameObject.AddComponent<ShopView>();
            _shopView.Build(mainRoot, _tooltipView);

            _draftView = gameObject.AddComponent<DraftView>();
            _draftView.Build(mainRoot, _run.Deck, _tooltipView);

            _tileChoiceView = gameObject.AddComponent<TileChoiceView>();
            _tileChoiceView.Build(mainRoot, _tooltipView);

            _pieceChoiceView = gameObject.AddComponent<PieceChoiceView>();
            _pieceChoiceView.Build(mainRoot, _tooltipView);

            _modifierUpgradeChoiceView = gameObject.AddComponent<ModifierUpgradeChoiceView>();
            _modifierUpgradeChoiceView.Build(mainRoot, _tooltipView);

            _upgradeRevealView = gameObject.AddComponent<UpgradeRevealView>();
            _upgradeRevealView.Build(mainRoot, _tooltipView);

            _modifierCarouselView = gameObject.AddComponent<ModifierCarouselView>();
            _modifierCarouselView.Build(mainRoot, _tooltipView);

            _shapeCarouselView = gameObject.AddComponent<ShapeCarouselView>();
            _shapeCarouselView.Build(mainRoot);

            _colorCarouselView = gameObject.AddComponent<ColorCarouselView>();
            _colorCarouselView.Build(mainRoot);

            _modifierPanelView = gameObject.AddComponent<ModifierPanelView>();
            // Lambda, not a method group, so a restart's new RunManager is
            // picked up automatically — _run is reassigned on restart but
            // this view is only Refreshed, never rebuilt.
            _modifierPanelView.Build(mainRoot, _tooltipView, id => _run.GetModifierUsageCount(id), (id, index) => _run.GetProgressiveModifierStateText(id, index), index => _run.GetModifierLevel(index), -sidePanelCenterX);
            // Modifier order determines scoring order (see
            // PlacementResult.Mult); same lambda-captures-_run reasoning as
            // above.
            _modifierPanelView.SwapRequested += (a, b) =>
            {
                _run.SwapModifiers(a, b);
                _modifierPanelView.Refresh(_run.ActiveModifiers);
            };
            _modifierPanelView.MoveRequested += (from, to) =>
            {
                _run.MoveModifier(from, to);
                _modifierPanelView.Refresh(_run.ActiveModifiers);
            };

            _deckView = gameObject.AddComponent<DeckView>();
            _deckView.Build(mainRoot, _run, _tooltipView);

            _endScreenView = gameObject.AddComponent<EndScreenView>();
            _endScreenView.Build(mainRoot);

            _tutorialView = gameObject.AddComponent<TutorialView>();
            _tutorialView.Build(mainRoot);

            _challengeSelectView = gameObject.AddComponent<ChallengeSelectView>();
            _challengeSelectView.Build(mainRoot);

            _settingsView = gameObject.AddComponent<SettingsView>();
            _settingsView.Build(mainRoot);
        }

        private void WireEvents()
        {
            _gridView.CellClicked += OnCellClicked;
            _gridView.HoverValidityChanged += _handView.SetHoveringValidDrop;
            _handView.SlotSelected += OnHandSlotSelected;
            _handView.SlotUnplayable += OnHandSlotUnplayable;
            _handView.SelectionCleared += OnHandSelectionCleared;
            _handView.ShuffleRequested += OnShuffleRequested;
            _shopView.BlisterBuyRequested += OnBlisterBuyRequested;
            _shopView.UpgradeBuyRequested += OnUpgradeBuyRequested;
            _shopView.RerollRequested += OnRerollRequested;
            _shopView.LeaveRequested += OnLeaveShopRequested;
            _draftView.SubChoiceConfirmed += OnSubChoiceConfirmed;
            _tileChoiceView.TileChoiceConfirmed += OnTileChoiceConfirmed;
            _pieceChoiceView.PieceChoiceConfirmed += OnPieceChoiceConfirmed;
            _modifierUpgradeChoiceView.ModifierUpgradeChoiceConfirmed += OnModifierUpgradeChoiceConfirmed;
            _endScreenView.RestartRequested += OnRestartRequested;
            _challengeSelectView.ChallengeChosen += OnChallengeChosen;
            _modifierCarouselView.Dismissed += OnModifierCarouselDismissed;
        }

        /// <summary>Quick access to the deck and rules, anchored below the combo card so their position follows its content-driven height.</summary>
        private void BuildComboUtilityButtons(Transform comboParent)
        {
            var deckButton = UIFactory.CreateButton(comboParent, "ShowDeckButton", "Show deck", UISprites.HandUtilityButtonBackground, 12);
            var deckLabel = deckButton.GetComponentInChildren<Text>();
            deckLabel.color = Color.black;
            deckLabel.fontSize = 25;
            var deckRect = deckButton.GetComponent<RectTransform>();
            deckRect.anchorMin = new Vector2(0.5f, 0f);
            deckRect.anchorMax = new Vector2(0.5f, 0f);
            deckRect.pivot = new Vector2(0.5f, 1f);
            deckRect.anchoredPosition = new Vector2(-81f, -24f);
            deckRect.sizeDelta = new Vector2(150f, 46.5f);
            deckButton.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            deckButton.onClick.AddListener(() => _deckView.Show());

            var rulesButton = UIFactory.CreateButton(comboParent, "ShowRulesButton", "Show rules", UISprites.HandUtilityButtonBackground, 12);
            var rulesLabel = rulesButton.GetComponentInChildren<Text>();
            rulesLabel.color = Color.black;
            rulesLabel.fontSize = 25;
            var rulesRect = rulesButton.GetComponent<RectTransform>();
            rulesRect.anchorMin = new Vector2(0.5f, 0f);
            rulesRect.anchorMax = new Vector2(0.5f, 0f);
            rulesRect.pivot = new Vector2(0.5f, 1f);
            rulesRect.anchoredPosition = new Vector2(81f, -24f);
            rulesRect.sizeDelta = new Vector2(150f, 46.5f);
            rulesButton.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rulesButton.onClick.AddListener(() => _tutorialView.Show());
        }

        private void OnHandSlotSelected(int handIndex)
        {
            var slot = _run.Deck.Hand[handIndex];
            if (!slot.HasValue)
            {
                // HandView already guards against selecting an empty slot —
                // this is just defense in depth.
                return;
            }
            // Fires for both a plain click and a drag start (see
            // HandView.SelectSlot, called from BeginSlotDrag and
            // OnSlotClicked). HandView already refused the selection (see
            // SlotUnplayable below) if the piece has nowhere to go, so it's
            // always placeable by the time this runs.
            SfxManager.Play(SfxId.PickUpPiece);
            var token = slot.Value;
            var rotation = _run.Deck.HandRotations[handIndex];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            _gridView.SetSelectedShape(shape, token.Color, token.Trait);
            _handView.SetShufflePulsing(false);
            SetStatusText("Drag onto the grid, or click a tile, to place: " + VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")");
        }

        /// <summary>HandView refused to pick up this slot's piece because it has no valid placement anywhere (the slot itself already played its shake, see HandView.ShakeSlot). Shows an emphasized status message and pulses Shuffle.</summary>
        private void OnHandSlotUnplayable(int handIndex)
        {
            _handView.SetShufflePulsing(true);
            SetStatusText("This piece can't be placed anywhere — try shuffling your hand.", emphasize: true);
        }

        /// <summary>Re-clicking the already-selected hand slot deselects it (see HandView.OnSlotClicked) — clears the grid's hover preview the same way a successful placement already does.</summary>
        private void OnHandSelectionCleared()
        {
            _handView.SetShufflePulsing(false);
            _gridView.SetSelectedShape(null);
            SetStatusText(IdleStatusMessage);
        }

        /// <summary>Re-rolls the hand; on a stuck, out-of-shuffles hand, ends the run immediately rather than waiting for a placement that can never come (same reasoning as PlacePiece's post-refill stuck check).</summary>
        private void OnShuffleRequested()
        {
            if (_isPlayingPlacementSequence)
            {
                return;
            }
            // Same eligibility check as RunManager.ShuffleHand, read-only
            // here, so this bails before any animation starts.
            if (_run.State != RunState.InProgress || _run.ShufflesRemaining <= 0)
            {
                return;
            }
            StartCoroutine(PlayManualShuffleSequence());
        }

        /// <summary>Event order: fade out hand slot pieces, fade out all enemy malus (Locker locks, Poisoner badges — see GridView.FadeMalus, wiped and redrawn as one group rather than diffed cell by cell), fade in slot pieces, fade in malus. The auto-refill path in PlayPlacementSequence follows the same order minus this sequence's first step (see its handWasAboutToAutoRefill branch).</summary>
        private System.Collections.IEnumerator PlayManualShuffleSequence()
        {
            _isPlayingPlacementSequence = true;
            _handView.SetInteractable(false);
            _modifierPanelView.SetInteractable(false);

            yield return _handView.FadeSlotPieces(1f, 0f, ShuffleFadeDuration);
            yield return _gridView.FadeMalus(1f, 0f, ShuffleFadeDuration);

            bool shuffled = _run.ShuffleHand();
            if (!shuffled)
            {
                // Shouldn't happen (checked before this coroutine even
                // started), but restores visibility rather than leaving the
                // hand/grid faded out forever if something changed the
                // run's state mid-animation.
                _handView.SetSlotPiecesAlpha(1f);
                _gridView.SetMalusAlpha(1f);
                _isPlayingPlacementSequence = false;
                _handView.SetInteractable(true);
                _modifierPanelView.SetInteractable(true);
                yield break;
            }
            SfxManager.Play(SfxId.Shuffle);
            _handView.SetShufflePulsing(false);
            _gridView.SetSelectedShape(null);
            _handView.ClearSelection();

            _handView.SetSlotPiecesAlpha(0f);
            _handView.Refresh();
            // A manual Shuffle resolves each alive enemy's On-Shuffle effect
            // (RunManager.DrawFreshHand/ResolveEnemyShuffleEffects), which
            // can move Locker's lock or add a Poisoner tile — a grid effect,
            // so it needs its own refresh or the badge never appears on
            // screen even though the model already updated.
            _gridView.Refresh();
            // Zeroed in the same frame as Refresh() above, before anything
            // yields — ApplyState always paints malus opaque, so without
            // this it would flash at full alpha before FadeMalus resets it
            // to 0.
            _gridView.SetMalusAlpha(0f);

            yield return _handView.FadeSlotPieces(0f, 1f, ShuffleFadeDuration);
            PulseShuffleEffectEnemies();
            PlayThiefStealEffect();
            yield return _gridView.FadeMalus(0f, 1f, ShuffleFadeDuration);

            RefreshShuffleButton();
            SetStatusText(IdleStatusMessage);
            HandleStateTransition(_run.State);

            _isPlayingPlacementSequence = false;
            _handView.SetInteractable(true);
            _modifierPanelView.SetInteractable(true);
        }

        /// <summary>Syncs HandView's Shuffle button to RunManager.ShufflesRemaining/State — called everywhere the hand itself gets refreshed (RefreshAll, right after a placement, and here) so the button's count and enabled state never lag behind the actual run.</summary>
        private void RefreshShuffleButton()
        {
            _handView.SetShuffleState(_run.ShufflesRemaining, _run.State == RunState.InProgress && _run.ShufflesRemaining > 0);
        }

        private void OnCellClicked(int x, int y)
        {
            if (_isPlayingPlacementSequence)
            {
                // Ignore clicks while a previous placement's hold/clear
                // animation is still playing — placing again mid-sequence would
                // refresh the grid from live state and un-hold the still-
                // animating line early.
                return;
            }

            int handIndex = _handView.SelectedIndex;
            if (handIndex < 0 || handIndex >= DeckManager.HandSize || !_run.Deck.Hand[handIndex].HasValue)
            {
                SetStatusText("Select a piece from your hand first.");
                return;
            }

            int roundScoreBefore = _run.RoundScore;
            int lueurBefore = _run.Lueur;

            // Snapshot the targeted enemy/enemies' HP before the placement
            // mutates it, so damage can be held back and drained in
            // alongside the combo total instead of jumping instantly.
            // Index 0 is the primary target — the default front enemy, or
            // whichever a Joker combat trait retargets (back for Range,
            // weakest for Précision, spread for Bombe/Éclat) — see
            // FindDamagedEnemyIndices, which mirrors RunManager.
            // ApplyJokerCombatOrDefaultDamage's targeting read-only. Only
            // index 0 gets the smooth drain below; any further index is a
            // Bombe/Éclat candidate snapped to its final value by
            // DrainSecondaryEnemyHits.
            var damagedEnemyIndices = FindDamagedEnemyIndices(handIndex, x, y);
            var enemyHpBeforeList = new List<int>(damagedEnemyIndices.Count);
            var enemyIdList = new List<EnemyId>(damagedEnemyIndices.Count);
            for (int i = 0; i < damagedEnemyIndices.Count; i++)
            {
                enemyHpBeforeList.Add(_run.CurrentEncounter[damagedEnemyIndices[i]].CurrentHp);
                enemyIdList.Add(_run.CurrentEncounter[damagedEnemyIndices[i]].Definition.Id);
            }
            int damagedEnemyIndex = damagedEnemyIndices.Count > 0 ? damagedEnemyIndices[0] : -1;
            int enemyHpBefore = enemyHpBeforeList.Count > 0 ? enemyHpBeforeList[0] : 0;
            EnemyId damagedEnemyId = enemyIdList.Count > 0 ? enemyIdList[0] : default;

            // Same idea for Leech's heal — snapshotting HP before lets
            // PlayLeechHealEffect show the actual amount gained
            // (EnemyInstance.Heal clamps at CurrentMaxHp) rather than
            // assuming the full ScoringConstants.LeechHealPerLineClear,
            // which would overstate it near full HP.
            int leechIndex = FindAliveEnemyIndex(EnemyId.Leech);
            int leechHpBeforeHeal = leechIndex >= 0 ? _run.CurrentEncounter[leechIndex].CurrentHp : 0;

            // Snapshot of every cell an alive Locker/Poisoner currently has
            // locked/poisoned, before this placement — PlacePiece can
            // trigger a synchronous auto-refill that resolves each enemy's
            // On-Shuffle effect, moving the lock or re-rolling the poison
            // tile, before any animation starts. Diffed against the same
            // snapshot taken again right after so whichever cells changed
            // can be held back from view until this placement's feedback
            // sequence finishes.
            bool handWasAboutToAutoRefill = IsHandAboutToAutoRefill(handIndex);
            SnapshotEnemyEffectCells(out var lockedCellsBefore, out var poisonedCellsBefore);

            var outcome = _run.PlacePiece(handIndex, x, y);
            if (!outcome.Placement.Success)
            {
                SfxManager.Play(SfxId.InvalidDrop);
                SetStatusText("Invalid placement there.");
                return;
            }
            SfxManager.Play(SfxId.ValidDrop);

            // Only valid right after a call that ran DrawFreshHand this
            // placement — otherwise ThiefStoleOnLastShuffle still holds a
            // stale value from an earlier, unrelated Shuffle.
            bool thiefStoleThisPlacement = handWasAboutToAutoRefill && _run.ThiefStoleOnLastShuffle;

            int leechHealAmount = leechIndex >= 0 ? _run.CurrentEncounter[leechIndex].CurrentHp - leechHpBeforeHeal : 0;

            int enemyHpAfter = damagedEnemyIndex >= 0 ? _run.CurrentEncounter[damagedEnemyIndex].CurrentHp : 0;
            // CurrentMaxHp, not Definition.MaxHp — Reclaimer's ceiling can
            // grow past the shared Definition value (see
            // EnemyInstance.HealOrGrow).
            int enemyMaxHp = damagedEnemyIndex >= 0 ? _run.CurrentEncounter[damagedEnemyIndex].CurrentMaxHp : 0;

            // Bombe/Éclat's further candidates (index 1+ of
            // damagedEnemyIndices) — only the ones that actually changed HP
            // become real drain targets, since Bombe always lists every
            // alive enemy as a candidate even though a dead one scores no
            // share. See DrainSecondaryEnemyHits.
            var secondaryDrainTargets = new List<(int Index, EnemyId Identity, int HpBefore, int HpAfter, int MaxHp)>();
            for (int i = 1; i < damagedEnemyIndices.Count; i++)
            {
                int idx = damagedEnemyIndices[i];
                int hpAfterSecondary = _run.CurrentEncounter[idx].CurrentHp;
                if (hpAfterSecondary != enemyHpBeforeList[i])
                {
                    secondaryDrainTargets.Add((idx, enemyIdList[i], enemyHpBeforeList[i], hpAfterSecondary, _run.CurrentEncounter[idx].CurrentMaxHp));
                }
            }
            // Only cells whose status actually changed stay hidden — the
            // symmetric difference of the two snapshots, not the union.
            // GAINED stays hidden; RELEASED looks locked/poisoned until the
            // reveal (see RefreshHoldingClearedCells).
            SnapshotEnemyEffectCells(out var lockedCellsAfter, out var poisonedCellsAfter);
            var lockedBeforeSet = new HashSet<Vector2Int>(lockedCellsBefore);
            var lockedAfterSet = new HashSet<Vector2Int>(lockedCellsAfter);
            var poisonedBeforeSet = new HashSet<Vector2Int>(poisonedCellsBefore);
            var poisonedAfterSet = new HashSet<Vector2Int>(poisonedCellsAfter);

            var deferredNewMalusCells = new List<Vector2Int>(outcome.BossLockedCells);
            foreach (var pos in lockedAfterSet)
            {
                if (!lockedBeforeSet.Contains(pos))
                {
                    deferredNewMalusCells.Add(pos);
                }
            }
            foreach (var pos in poisonedAfterSet)
            {
                if (!poisonedBeforeSet.Contains(pos))
                {
                    deferredNewMalusCells.Add(pos);
                }
            }

            var deferredReleasedLockedCells = new List<Vector2Int>();
            foreach (var pos in lockedBeforeSet)
            {
                if (!lockedAfterSet.Contains(pos))
                {
                    deferredReleasedLockedCells.Add(pos);
                }
            }
            var deferredReleasedPoisonedCells = new List<Vector2Int>();
            foreach (var pos in poisonedBeforeSet)
            {
                if (!poisonedAfterSet.Contains(pos))
                {
                    deferredReleasedPoisonedCells.Add(pos);
                }
            }
            bool hasDeferredGridChange = deferredNewMalusCells.Count > 0 || deferredReleasedLockedCells.Count > 0 || deferredReleasedPoisonedCells.Count > 0;

            _handView.SetShufflePulsing(false);
            // ClearSelectionStateOnly, not SetSelectedShape(null) — the
            // latter's ClearHover redraws the hover footprint from live
            // grid state, which can leak an auto-refill-triggered lock/
            // poison move early. RefreshHoldingClearedCells below redraws
            // every cell with the correct held/deferred treatment anyway.
            _gridView.ClearSelectionStateOnly();
            _handView.ClearSelection();

            // Hold any completed line/column, or a Void/Kamikaze
            // destruction, visually filled instead of instantly vanishing
            // while its score plays out — the destroy burst needs a
            // still-filled tile to play against.
            var heldCells = new List<Vector2Int>(outcome.Placement.ClearedCells);
            var heldColors = new List<PieceColor>(outcome.Placement.ClearedCellColors);
            var heldTraits = new List<PieceTrait?>(outcome.Placement.ClearedCellTraits);
            for (int i = 0; i < outcome.Placement.DestroyedCells.Count; i++)
            {
                var destroyedColor = outcome.Placement.DestroyedCellColors[i];
                if (destroyedColor.HasValue)
                {
                    heldCells.Add(outcome.Placement.DestroyedCells[i]);
                    heldColors.Add(destroyedColor.Value);
                    heldTraits.Add(null);
                }
            }
            // Boss-locked cells and any Locker/Poisoner cell this
            // placement's auto-refill (or an enemy death) just changed are
            // held back from this redraw, revealed only once
            // PlayPlacementSequence's end-of-sequence reveal runs.
            _gridView.RefreshHoldingClearedCells(heldCells, heldColors, heldTraits, deferredNewMalusCells, deferredReleasedLockedCells, deferredReleasedPoisonedCells);
            // Same hold for the hand when auto-refill already dealt the
            // next hand — PlayPlacementSequence reveals it once the
            // sequence finishes (see HandView.RefreshHoldingEmpty).
            if (handWasAboutToAutoRefill)
            {
                _handView.RefreshHoldingEmpty();
            }
            else
            {
                _handView.Refresh();
            }
            RefreshShuffleButton();
            // Round/budget update immediately; score and Lueur stay at
            // pre-placement values until PlayPlacementSequence catches them
            // up in step with each popup — same hold-back for the targeted
            // enemy's HP, drained only once DrainComboIntoDamage runs.
            _hudView.Refresh(_run);
            _hudView.SetLueur(lueurBefore);
            _hudView.SetScores(roundScoreBefore, _run.CurrentQuota);
            if (damagedEnemyIndex >= 0)
            {
                // isDead: false — even if this hit is lethal, the player
                // hasn't seen HP reach 0 yet, so the icon must still read
                // as alive here.
                _hudView.SetEnemyHpDisplay(damagedEnemyIndex, enemyHpBefore, enemyMaxHp, damagedEnemyId, false);
            }
            // Same hold-back for every Bombe/Éclat secondary target — the
            // Refresh above already drew their true post-placement
            // (possibly dead) state.
            for (int i = 0; i < secondaryDrainTargets.Count; i++)
            {
                var heldTarget = secondaryDrainTargets[i];
                _hudView.SetEnemyHpDisplay(heldTarget.Index, heldTarget.HpBefore, heldTarget.MaxHp, heldTarget.Identity, false);
            }
            SetStatusText(IdleStatusMessage);

            _isPlayingPlacementSequence = true;
            _handView.SetInteractable(false);
            // Blocks reordering while the score sequence below reads
            // GetBadgeTransform/Pulse per event — a mid-animation Refresh()
            // from a reorder would otherwise rebuild every badge out from
            // under it.
            _modifierPanelView.SetInteractable(false);
            StartCoroutine(PlayPlacementSequence(outcome, roundScoreBefore, lueurBefore, damagedEnemyIndex, damagedEnemyId, enemyHpBefore, enemyHpAfter, enemyMaxHp, hasDeferredGridChange, thiefStoleThisPlacement, handWasAboutToAutoRefill, leechIndex, leechHealAmount, secondaryDrainTargets));
        }

        /// <summary>True when playing the hand slot at <paramref name="handIndex"/> is about to leave every slot empty, which RunManager.PlacePiece auto-refills (and resolves enemy Shuffle effects for) synchronously before returning — see OnCellClicked's own snapshot comment.</summary>
        private bool IsHandAboutToAutoRefill(int handIndex)
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != handIndex && _run.Deck.Hand[i].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Every cell locked by an alive Locker and every cell poisoned by an alive Poisoner, kept as two separate lists so OnCellClicked's before/after diff can distinguish a released lock from a released poison (see GridCellView.ApplyState's forceShowLocked/forceShowPoisoned).</summary>
        private void SnapshotEnemyEffectCells(out List<Vector2Int> lockedCells, out List<Vector2Int> poisonedCells)
        {
            lockedCells = new List<Vector2Int>();
            poisonedCells = new List<Vector2Int>();
            if (!_run.HasActiveEncounter)
            {
                return;
            }
            var encounter = _run.CurrentEncounter;
            for (int i = 0; i < encounter.Count; i++)
            {
                var enemy = encounter[i];
                if (enemy.LockedCell.HasValue)
                {
                    lockedCells.Add(enemy.LockedCell.Value);
                }
                poisonedCells.AddRange(enemy.PoisonedCells);
            }
        }

        /// <summary>Which combat kind drives FindDamagedEnemyIndices's targeting prediction, read from the placement's whole merged group rather than just the placed token's own trait (mirrors RunManager.ApplyJokerCombatOrDefaultDamage's group scan). If both a spreading kind (Bombe/Éclat) and a single-target one are present at once, the spreading kind wins — its "every alive enemy" animation safely covers the narrower target too.</summary>
        private PieceTraitKind? FindPriorityCombatKind(List<Vector2Int> groupCells)
        {
            bool hasSpreadingKind = false;
            PieceTraitKind? singleTargetKind = null;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                var origin = _run.Grid.GetCell(pos.x, pos.y).OriginTrait;
                if (!origin.HasValue || !PieceTrait.IsJokerCombatKind(origin.Value.Kind))
                {
                    continue;
                }
                var kind = origin.Value.Kind;
                if (kind == PieceTraitKind.Bombe || kind == PieceTraitKind.Eclat)
                {
                    hasSpreadingKind = true;
                }
                else if (singleTargetKind == null)
                {
                    singleTargetKind = kind;
                }
            }
            if (hasSpreadingKind)
            {
                return PieceTraitKind.Bombe;
            }
            return singleTargetKind;
        }

        /// <summary>Every enemy index this placement's damage could plausibly touch, read before PlacePiece so OnCellClicked can hold each one's HP display at its pre-placement value. Index 0 is the primary target, the one DrainComboIntoDamage smoothly lerps; further indices are Bombe/Éclat candidates for DrainSecondaryEnemyHits.</summary>
        private List<int> FindDamagedEnemyIndices(int handIndex, int anchorX, int anchorY)
        {
            var result = new List<int>();
            if (!_run.HasActiveEncounter)
            {
                return result;
            }
            var encounter = _run.CurrentEncounter;
            var tokenSlot = _run.Deck.Hand[handIndex];
            PieceTraitKind? kind = null;
            if (tokenSlot.HasValue)
            {
                var token = tokenSlot.Value;
                var shape = PieceShapeCatalog.GetRotated(token.Shape, _run.Deck.HandRotations[handIndex]);
                var previewGroup = _run.Grid.PreviewGroup(shape, token.Color, anchorX, anchorY);
                kind = FindPriorityCombatKind(previewGroup);
            }

            if (kind == PieceTraitKind.Range)
            {
                // Back-to-front instead of front-to-back — the only
                // difference from the default rule below.
                for (int i = encounter.Count - 1; i >= 0; i--)
                {
                    if (!encounter[i].IsDead)
                    {
                        result.Add(i);
                        return result;
                    }
                }
                return result;
            }

            if (kind == PieceTraitKind.Precision)
            {
                int weakest = -1;
                for (int i = 0; i < encounter.Count; i++)
                {
                    if (encounter[i].IsDead)
                    {
                        continue;
                    }
                    if (weakest < 0 || encounter[i].CurrentHp < encounter[weakest].CurrentHp)
                    {
                        weakest = i;
                    }
                }
                if (weakest >= 0)
                {
                    result.Add(weakest);
                }
                return result;
            }

            // Default (including no trait) and Sangsue both hit only the
            // front alive enemy. Bombe/Éclat can reach further ones — every
            // other alive enemy is listed as a candidate, filtered down
            // later by DrainSecondaryEnemyHits' caller to whichever ones
            // actually took damage.
            for (int i = 0; i < encounter.Count; i++)
            {
                if (!encounter[i].IsDead)
                {
                    result.Add(i);
                }
            }
            if (kind != PieceTraitKind.Bombe && kind != PieceTraitKind.Eclat && result.Count > 1)
            {
                result.RemoveRange(1, result.Count - 1);
            }
            return result;
        }

        /// <summary>RunManager.ThiefStoleOnLastShuffle is the only way Presentation can tell a Thief steal happened — unlike Locker/Poisoner, a hand-only effect leaves no trace for SnapshotEnemyEffectCells to diff. No-op if nothing stole this Shuffle, or if Thief's icon isn't on screen.</summary>
        private void PlayThiefStealEffect()
        {
            if (!_run.ThiefStoleOnLastShuffle)
            {
                return;
            }
            var encounter = _run.CurrentEncounter;
            for (int i = 0; i < encounter.Count; i++)
            {
                if (encounter[i].Definition.Id == EnemyId.Thief)
                {
                    _hudView.PlayEnemyEffectPulse(i);
                    var anchor = _hudView.GetEnemyIconTransform(i);
                    if (anchor != null)
                    {
                        // floatDown: true — this anchor sits near the top of
                        // the screen; SpawnPopup's usual float-up would run
                        // the text off the top edge.
                        _feedbackLayer.SpawnPopup(anchor, "Stole a piece!", UITheme.Danger, floatDown: true);
                    }
                    SfxManager.Play(SfxId.PickUpPiece);
                    return;
                }
            }
        }

        /// <summary>
        /// Plays a placement's full feedback sequence in order: each Lueur
        /// group (pulsing its cells, flying a popup to the Lueur label,
        /// advancing that display), then each golden/group score popup
        /// (advancing the score bar as it goes), then any completed line/
        /// column clearing one cell at a time, and only then advances the
        /// run state — so nothing interrupts the player mid-readout.
        /// </summary>
        private System.Collections.IEnumerator PlayPlacementSequence(PlacementOutcome outcome, int roundScoreBefore, int lueurBefore,
            int damagedEnemyIndex, EnemyId damagedEnemyId, int enemyHpBefore, int enemyHpAfter, int enemyMaxHp,
            bool hasDeferredGridChange, bool thiefStoleThisPlacement, bool handWasAboutToAutoRefill,
            int leechIndex, int leechHealAmount,
            List<(int Index, EnemyId Identity, int HpBefore, int HpAfter, int MaxHp)> secondaryDrainTargets)
        {
            var placement = outcome.Placement;
            SfxManager.ResetComboPitch();

            // Chameleon Tile recolored this whole piece to match a neighbor before Grid.PlacePiece ran, so by
            // now every placed cell already shows the resolved color — play the actual color switch here,
            // before anything below (Lueur pulses, score cascade) reads these cells' color.
            if (outcome.ChameleonOriginalColor.HasValue && placement.PlacedCells.Count > 0)
            {
                var resolvedColor = _run.Grid.GetCell(placement.PlacedCells[0]).FilledColor.Value;
                yield return _gridView.PlayChameleonColorTransition(placement.PlacedCells, outcome.ChameleonOriginalColor.Value, resolvedColor);
            }

            // Whether cleared/destroyed tiles should keep showing as
            // still-filled past the score cascade, only actually emptying
            // once the enemy damage drain (and death fade-out) finishes.
            // Scoped to placements that actually drive a drain (same
            // condition as the drain itself, further below) so an ordinary
            // non-combat placement's tiles still clear inline as usual.
            bool deferTileClear = damagedEnemyIndex >= 0 && placement.TotalScore > 0;

            // Lueur groups play first, ahead of the score cascade — each
            // group pulses its own cells, flies a "+N" popup from the
            // group's own center to the Lueur label
            // (see FeedbackLayer.SpawnFlyingPopup), and only then bumps the
            // displayed Lueur total, so it visibly climbs one group at a
            // time instead of jumping straight to the final value.
            int displayedLueur = lueurBefore;
            for (int i = 0; i < placement.LueurGroups.Count; i++)
            {
                var group = placement.LueurGroups[i];
                Vector3 centerSum = Vector3.zero;
                int cellsWithTransform = 0;
                for (int c = 0; c < group.Cells.Count; c++)
                {
                    var cell = group.Cells[c];
                    _gridView.PulseCell(cell.x, cell.y);
                    var cellRect = _gridView.GetCellTransform(cell.x, cell.y);
                    if (cellRect != null)
                    {
                        centerSum += cellRect.position;
                        cellsWithTransform++;
                    }
                }
                if (cellsWithTransform > 0)
                {
                    Vector3 center = centerSum / cellsWithTransform;
                    _feedbackLayer.SpawnFlyingPopup(center, _hudView.LueurLabelTransform, "+" + group.Amount, VisualDefaults.GoldenColor);
                }
                SfxManager.Play(SfxId.LueurGain);

                displayedLueur += group.Amount;
                _hudView.SetLueur(displayedLueur);

                yield return new WaitForSeconds(LueurGroupStaggerSeconds);
            }

            int displayedRoundScore = roundScoreBefore;
            // Mirrors PlacementResult.Chips/.Mult progressively: chipsTotal
            // * multTotal always equals the placement's subtotal so far
            // (displayedRoundScore - roundScoreBefore), same invariant as
            // Chips * Mult == TotalScore in Core. multTotal is a float since
            // progressive modifiers keep full precision; a final sync below
            // guards against float-rounding drift across catch-up steps.
            int chipsTotal = 0;
            float multTotal = 1f;
            // ShapeMastery/ColorMastery is folded into each cell's own
            // Group popup (via this dictionary and the Group branch below)
            // rather than shown as one aggregate, so a poisoned cell's
            // popup is always correctly negative on its own, never
            // borrowing another cell's sign.
            var masteryByPosition = new Dictionary<Vector2Int, int>();
            for (int e = 0; e < placement.ScoreEvents.Count; e++)
            {
                var masteryEvent = placement.ScoreEvents[e];
                if (masteryEvent.Type == ScoreEventType.ShapeMastery || masteryEvent.Type == ScoreEventType.ColorMastery)
                {
                    masteryByPosition.TryGetValue(masteryEvent.Position, out int soFar);
                    masteryByPosition[masteryEvent.Position] = soFar + masteryEvent.Amount;
                }
            }
            _comboView.Show(0, 1f);
            // Multiplies every stagger wait below, shrinking by
            // ComboSpeedupFactor after each addition; shared across score
            // events, line clears and the multiplier catch-up so the whole
            // sequence accelerates as one continuous combo.
            float staggerSpeed = 1f;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type == ScoreEventType.LineClear)
                {
                    continue; // played below, synced with each cell's visual clear
                }

                if (scoreEvent.Type == ScoreEventType.ShapeMastery || scoreEvent.Type == ScoreEventType.ColorMastery)
                {
                    // Folded into its own cell's Group popup instead — see
                    // masteryByPosition's own doc comment above and the
                    // Group branch below.
                    continue;
                }

                if (scoreEvent.Type == ScoreEventType.ModifierMultiplier || scoreEvent.Type == ScoreEventType.MultBonus)
                {
                    // Every Mult-contributing event ("xN" and "+N Mult"
                    // families) is handled together after this loop, in
                    // strict modifier-index order — not in whatever order
                    // GridManager computed them. See the ordered catch-up
                    // below and PlacementResult.Mult's doc comment.
                    continue;
                }

                if (scoreEvent.Type == ScoreEventType.LueurBonus)
                {
                    // Lueur (not score) from an active modifier (see
                    // PlacementResult.ModifierLueurBonus) — reuses the same
                    // flying-popup visual as the LueurGroups loop above,
                    // plus the usual badge pulse.
                    if (scoreEvent.TriggeringModifier.HasValue)
                    {
                        var badgeAnchor = _modifierPanelView.GetBadgeTransform(scoreEvent.TriggeringModifier.Value, scoreEvent.TriggeringModifierIndex)
                            ?? _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                        _feedbackLayer.SpawnFlyingPopup(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + scoreEvent.Amount, VisualDefaults.GoldenColor);
                        _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                    }
                    SfxManager.Play(SfxId.LueurGain);
                    displayedLueur += scoreEvent.Amount;
                    _hudView.SetLueur(displayedLueur);
                    yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                    staggerSpeed *= ComboSpeedupFactor;
                    continue;
                }

                // A per-cell modifier (Forteresse, Carrefour, etc.) gets one
                // ScoreEvent per qualifying cell, so this naturally pulses
                // each in turn; a flat-bonus modifier only has the one
                // representative cell.
                _gridView.PulseCell(scoreEvent.Position.x, scoreEvent.Position.y);

                RectTransform anchor;
                if (scoreEvent.Type == ScoreEventType.Modifier && scoreEvent.TriggeringModifier.HasValue)
                {
                    // Popup shows on the modifier's badge, falling back to
                    // the tile if the badge can't be found.
                    anchor = _modifierPanelView.GetBadgeTransform(scoreEvent.TriggeringModifier.Value, scoreEvent.TriggeringModifierIndex)
                        ?? _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                    _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                }
                else
                {
                    anchor = _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                }

                // A Modifier event is always a points bonus (see
                // PlacementResult.ModifierBonus), colored like any other
                // points popup — ModifierMultiplier/MultBonus (genuinely
                // Mult) get their own red popups, handled earlier in this
                // loop. A poisoned position (RunManager.ApplyPoisonScoreRule)
                // can flip any event type negative, Modifier included —
                // shown in red with its real sign and an always-"+" prefix.
                // Group popups fold in the same cell's ShapeMastery/
                // ColorMastery amount, if any (masteryByPosition above) —
                // both are already independently flipped by poison, so the
                // combined sign is never in conflict.
                int displayAmount = scoreEvent.Amount;
                if (scoreEvent.Type == ScoreEventType.Group && masteryByPosition.TryGetValue(scoreEvent.Position, out int masteryAtThisCell))
                {
                    displayAmount += masteryAtThisCell;
                }
                bool negative = displayAmount < 0;
                Color color = negative ? UITheme.Danger
                    : scoreEvent.Type == ScoreEventType.Golden ? VisualDefaults.GoldenColor
                    : scoreEvent.Type == ScoreEventType.Modifier ? UITheme.ButtonSelected
                    : scoreEvent.Type == ScoreEventType.Bastion ? UITheme.Success
                    : UITheme.TextPrimary;
                _feedbackLayer.SpawnPopup(anchor, (negative ? "" : "+") + displayAmount, color);
                SfxManager.PlayComboTick();

                displayedRoundScore += displayAmount;
                chipsTotal += displayAmount;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            // Per-cell signed amount for each cleared cell (poison can flip
            // a cell's +LineClearBonusPerCell negative — see
            // RunManager.ApplyPoisonScoreRule) — looked up from the actual
            // ScoreEvents instead of assuming every cell is worth the flat
            // +ScoringConstants.LineClearBonusPerCell.
            var clearedCellAmounts = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var e = placement.ScoreEvents[i];
                if (e.Type == ScoreEventType.LineClear)
                {
                    clearedCellAmounts[e.Position] = e.Amount;
                }
            }

            // Scoring only here — the actual visual clearing (burst + empty
            // the tile, see PlayTileClearBursts) runs right after this loop
            // unless deferTileClear is set, in which case it's held until
            // after the enemy damage drain further down.
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                int amount = clearedCellAmounts.TryGetValue(pos, out int signed) ? signed : ScoringConstants.LineClearBonusPerCell;
                bool negative = amount < 0;
                var anchor = _gridView.GetCellTransform(pos.x, pos.y);
                _feedbackLayer.SpawnPopup(anchor, (negative ? "" : "+") + amount, negative ? UITheme.Danger : UITheme.Success);
                _gridView.PulseCell(pos.x, pos.y);
                SfxManager.Play(SfxId.LineClear);

                displayedRoundScore += amount;
                chipsTotal += amount;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, LineClearStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            if (!deferTileClear)
            {
                yield return PlayTileClearBursts(placement, leechIndex, leechHealAmount);
            }

            // GroupMultiplier (Tinted+Multiplier-Zone cells) and
            // LineClearMultiplier (Multiplier-Zone cells only — see
            // PlacementResult.LineClearMultiplier) are each applied once
            // over their own share of the total, Balatro-style, rather than
            // inflating each popup above, so the "extra" needs its own
            // catch-up here or the displayed score ends up short.
            int multipliedExtra = (placement.GroupBonus + placement.GoldenBonus) * (placement.GroupMultiplier - 1)
                + placement.LineClearScore * (placement.LineClearMultiplier - 1);
            if (multipliedExtra > 0)
            {
                var centerAnchor = _gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2);
                // GroupMultiplier is always >= LineClearMultiplier (multiplier-zone
                // cells count toward both, Tinted only toward GroupMultiplier), so
                // it's the more informative single label even when the two differ.
                _feedbackLayer.SpawnPopup(centerAnchor, "x" + Mathf.Max(placement.GroupMultiplier, placement.LineClearMultiplier), UITheme.ButtonSelected);
                SfxManager.PlayComboTick();

                // Lands on the CHIPS side of the Balatro-style split (see
                // PlacementResult.Chips), not the red mult pill — baked
                // per-cell rather than a placement-wide factor like
                // ModifierMultiplier/ComboMultiplier below.
                displayedRoundScore += multipliedExtra;
                chipsTotal += multipliedExtra;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
            }

            // Every Mult-contributing modifier (the "+N Mult" family and the
            // "xN" family) catches up in one pass, strictly in
            // modifier-index order — mirrors PlacementResult.Mult's own
            // ordered fold (a strict left-to-right fold, not PEMDAS). Each
            // modifier already flashed its own popup in the main loop
            // above; this handles the score catch-up in the exact order
            // that determines the final total.
            var multEvents = new List<ScoreEvent>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var e = placement.ScoreEvents[i];
                if (e.Type == ScoreEventType.ModifierMultiplier || e.Type == ScoreEventType.MultBonus)
                {
                    multEvents.Add(e);
                }
            }
            multEvents.Sort((a, b) => a.TriggeringModifierIndex.CompareTo(b.TriggeringModifierIndex));

            for (int i = 0; i < multEvents.Count; i++)
            {
                var scoreEvent = multEvents[i];
                float amount = scoreEvent.PreciseAmount ?? scoreEvent.Amount;
                float multBefore = multTotal;
                if (scoreEvent.Type == ScoreEventType.ModifierMultiplier)
                {
                    multTotal *= amount;
                }
                else
                {
                    multTotal += amount;
                }

                if (scoreEvent.TriggeringModifier.HasValue)
                {
                    var badgeAnchor = _modifierPanelView.GetBadgeTransform(scoreEvent.TriggeringModifier.Value, scoreEvent.TriggeringModifierIndex)
                        ?? _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                    string label = scoreEvent.Type == ScoreEventType.ModifierMultiplier
                        ? "x" + FormatMultAmount(amount)
                        : "+" + FormatMultAmount(amount);
                    _feedbackLayer.SpawnPopup(badgeAnchor, label, UITheme.Danger);
                    _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                }
                SfxManager.PlayComboTick();

                float subtotalSoFar = displayedRoundScore - roundScoreBefore;
                int extra = Mathf.RoundToInt(subtotalSoFar * (multTotal / multBefore - 1f));
                displayedRoundScore += extra;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseMult();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            // "Combo" (see PlacementResult.ComboMultiplier) is just another
            // ModifierMultiplier event in the loop above, already caught up
            // — this only adds its distinct "COMBO xN" center-screen
            // callout, since it reacts to the round's streak state rather
            // than a per-modifier condition.
            if (placement.ComboMultiplier > 1)
            {
                var centerAnchor = _gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2);
                _feedbackLayer.SpawnPopup(centerAnchor, "COMBO x" + placement.ComboMultiplier, UITheme.Success);
            }

            // Force-sync to the authoritative total — guards against float
            // rounding in the two catch-ups above drifting the displayed
            // total away from placement.TotalScore by a point or two.
            if (displayedRoundScore != roundScoreBefore + placement.TotalScore)
            {
                displayedRoundScore = roundScoreBefore + placement.TotalScore;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
            }

            // Only once the combo total is finished, wait 1s so the player
            // can read the final number, then drain it down to 0 while
            // transferring it into the targeted enemy's HP. Skipped for a
            // non-positive total (a placement scored entirely through
            // poison nets a heal, not damage — nothing positive to drain).
            if (damagedEnemyIndex >= 0 && placement.TotalScore > 0)
            {
                yield return new WaitForSeconds(1f);
                yield return DrainComboIntoDamage(placement.TotalScore, damagedEnemyIndex, damagedEnemyId, enemyHpBefore, enemyHpAfter, enemyMaxHp);
                // Bombe/Éclat's further targets snap straight to their final
                // value right after the primary target's smooth drain
                // finishes, rather than a second parallel lerp.
                yield return DrainSecondaryEnemyHits(secondaryDrainTargets);
            }

            // Only now — after the enemy has finished draining (and fading
            // out, if lethal) — do the cleared/destroyed cells actually
            // visually empty. Their points already counted during the
            // score cascade above.
            if (deferTileClear)
            {
                yield return PlayTileClearBursts(placement, leechIndex, leechHealAmount);
            }

            // Boss-locked cells, any Locker/Poisoner cell this placement's
            // auto-refill changed, and any lock/poison an enemy dying from
            // this placement's damage just released were held back from
            // view this whole time — a full refresh surfaces them now that
            // the rest of the sequence (including any enemy death) has
            // played out.
            //
            // Whenever anything is deferred, the reveal follows the same
            // fade-out-malus/fade-in-malus order the manual Shuffle button
            // uses (see PlayManualShuffleSequence). The hand's own fade-in
            // only plays when it actually auto-refilled this placement —
            // otherwise the hand was never hidden (see
            // HandView.RefreshHoldingEmpty), so there's nothing of its own
            // to reveal, just the grid's malus.
            if (handWasAboutToAutoRefill)
            {
                yield return _gridView.FadeMalus(1f, 0f, ShuffleFadeDuration);

                _handView.SetSlotPiecesAlpha(0f);
                _handView.Refresh();
                _gridView.Refresh();
                // Same same-frame zeroing as PlayManualShuffleSequence —
                // ApplyState always paints malus opaque, so without this it
                // would briefly flash at full alpha.
                _gridView.SetMalusAlpha(0f);

                yield return _handView.FadeSlotPieces(0f, 1f, ShuffleFadeDuration);
                PulseShuffleEffectEnemies();
                if (thiefStoleThisPlacement)
                {
                    PlayThiefStealEffect();
                }
                yield return _gridView.FadeMalus(0f, 1f, ShuffleFadeDuration);
            }
            else if (hasDeferredGridChange)
            {
                yield return _gridView.FadeMalus(1f, 0f, ShuffleFadeDuration);
                _gridView.Refresh();
                _gridView.SetMalusAlpha(0f);
                yield return _gridView.FadeMalus(0f, 1f, ShuffleFadeDuration);
                // Reveals the real hand for real if it was held empty (see
                // HandView.RefreshHoldingEmpty) — a harmless no-op resync
                // here, since handWasAboutToAutoRefill is false.
                _handView.Refresh();
            }
            else
            {
                // Reveals the real hand for real if it was held empty (see
                // HandView.RefreshHoldingEmpty) — a harmless no-op resync
                // otherwise.
                _handView.Refresh();
            }

            _isPlayingPlacementSequence = false;
            _handView.SetInteractable(true);
            RefreshShuffleButton();
            _modifierPanelView.SetInteractable(true);
            HandleStateTransition(outcome.StateAfter);
        }

        /// <summary>
        /// The actual visual clearing of cleared/destroyed cells — burst VFX
        /// plus emptying the tile, one cell at a time, with no score popup
        /// of its own (that already happened in the score cascade). Runs
        /// exactly once per placement regardless of whether it's called
        /// right after the score cascade or deferred until after the enemy
        /// damage drain (see deferTileClear), so it's also the single spot
        /// where Leech's heal popup is shown.
        /// </summary>
        private System.Collections.IEnumerator PlayTileClearBursts(PlacementResult placement, int leechIndex, int leechHealAmount)
        {
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                SfxManager.Play(SfxId.LineClear);
                // Tinted to the color the cell had right before clearing,
                // via RefreshHoldingClearedCells's held color.
                _gridView.PlayClearBurst(pos.x, pos.y, VisualDefaults.GetColor(placement.ClearedCellColors[i]));
                _gridView.ClearCellVisual(pos.x, pos.y);
                yield return new WaitForSeconds(MinStaggerSeconds);
            }
            // Void Tile / Kamikaze Tile destructions — same burst as a line
            // clear above, but no per-cell score popup (their points already
            // showed as a single Trait ScoreEvent at the enchanted cell).
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                var pos = placement.DestroyedCells[i];
                var color = placement.DestroyedCellColors[i];
                _gridView.PlayClearBurst(pos.x, pos.y, color.HasValue ? VisualDefaults.GetColor(color.Value) : UITheme.TextPrimary);
                _gridView.ClearCellVisual(pos.x, pos.y);
                SfxManager.Play(SfxId.LineClear);
                yield return new WaitForSeconds(MinStaggerSeconds);
            }
            if (leechHealAmount > 0)
            {
                PlayLeechHealEffect(leechIndex, leechHealAmount);
            }
        }

        /// <summary>floatDown: true for the same reason as PlayThiefStealEffect's popup — this anchor sits near the top of the screen, and SpawnPopup's usual float-up would run it off the edge.</summary>
        private void PlayLeechHealEffect(int leechIndex, int healAmount)
        {
            _hudView.PlayEnemyEffectPulse(leechIndex);
            var anchor = _hudView.GetEnemyIconTransform(leechIndex);
            if (anchor != null)
            {
                _feedbackLayer.SpawnPopup(anchor, "+" + healAmount, UITheme.Success, floatDown: true);
            }
        }

        /// <summary>Index of the first alive enemy in CurrentEncounter matching <paramref name="id"/>, or -1 if there's no active encounter or none alive — see OnCellClicked's own Leech HP snapshot.</summary>
        private int FindAliveEnemyIndex(EnemyId id)
        {
            if (!_run.HasActiveEncounter)
            {
                return -1;
            }
            var encounter = _run.CurrentEncounter;
            for (int i = 0; i < encounter.Count; i++)
            {
                if (encounter[i].Definition.Id == id && !encounter[i].IsDead)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Pulses every alive Locker/HeavyLocker/Poisoner/Plague's icon — the four enemies whose On-Shuffle effect (RunManager.ResolveEnemyShuffleEffects) fires unconditionally on every alive instance each Shuffle, manual or auto-refill. Thief's own steal effect isn't unconditional (only fires if the hand has a piece left to steal), so it's pulsed separately by PlayThiefStealEffect once RunManager confirms it actually happened.</summary>
        private void PulseShuffleEffectEnemies()
        {
            if (!_run.HasActiveEncounter)
            {
                return;
            }
            var encounter = _run.CurrentEncounter;
            for (int i = 0; i < encounter.Count; i++)
            {
                var enemy = encounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                var id = enemy.Definition.Id;
                if (id == EnemyId.Locker || id == EnemyId.HeavyLocker || id == EnemyId.Poisoner || id == EnemyId.Plague)
                {
                    _hudView.PlayEnemyEffectPulse(i);
                }
            }
        }

        /// <summary>Counts the combo total down from <paramref name="startTotal"/> to 0 while the targeted enemy's HP ticks down from <paramref name="hpBefore"/> to <paramref name="hpAfter"/> in lockstep — the visual "transfer" of combo score into damage.</summary>
        private System.Collections.IEnumerator DrainComboIntoDamage(int startTotal, int enemyIndex, EnemyId identity, int hpBefore, int hpAfter, int maxHp)
        {
            _hudView.PlayEnemyHurtEffect(enemyIndex);
            const float duration = 0.6f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _comboView.SetTotal(Mathf.RoundToInt(Mathf.Lerp(startTotal, 0f, p)));
                // isDead stays false for every in-between frame, even if the
                // lerp passes through 0 on its way — the icon only turns
                // "dead" on the last frame below, once HP truly lands on
                // hpAfter.
                _hudView.SetEnemyHpDisplay(enemyIndex, Mathf.RoundToInt(Mathf.Lerp(hpBefore, hpAfter, p)), maxHp, identity, false);
                yield return null;
            }
            _comboView.SetTotal(0);
            bool isDead = hpAfter <= 0;
            _hudView.SetEnemyHpDisplay(enemyIndex, hpAfter, maxHp, identity, isDead);

            if (isDead)
            {
                // Pause gives the player a beat to register the kill (and
                // the dead-gray tint just applied) before the icon fades.
                yield return new WaitForSeconds(0.25f);
                yield return _hudView.FadeOutEnemySlot(enemyIndex, 0.3f);
            }
        }

        /// <summary>
        /// Bombe/Éclat's further targets — each jumps straight from its held
        /// pre-placement HP to its final value (no lerp, unlike <see
        /// cref="DrainComboIntoDamage"/>'s primary target) and, if lethal,
        /// fades out the same way. Entries whose HP never changed were
        /// already filtered out by the caller.
        /// </summary>
        private System.Collections.IEnumerator DrainSecondaryEnemyHits(List<(int Index, EnemyId Identity, int HpBefore, int HpAfter, int MaxHp)> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                bool isDead = target.HpAfter <= 0;
                _hudView.PlayEnemyHurtEffect(target.Index);
                _hudView.SetEnemyHpDisplay(target.Index, target.HpAfter, target.MaxHp, target.Identity, isDead);
                if (isDead)
                {
                    yield return new WaitForSeconds(0.25f);
                    yield return _hudView.FadeOutEnemySlot(target.Index, 0.3f);
                }
            }
        }

        /// <summary>Whole number when <paramref name="value"/> is (near enough) an integer, one decimal otherwise — used by the two Mult catch-up popups above so a progressive modifier's true fractional contribution (Densité, Cartes Enchantées, Expérience) reads clearly without cluttering the common case (every other modifier, always a whole number) with a needless ".0".</summary>
        private static string FormatMultAmount(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < 0.05f)
            {
                return Mathf.RoundToInt(value).ToString();
            }
            return value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void HandleStateTransition(RunState state)
        {
            // Stops a Shuffle-button pulse left running from a selected
            // piece that had nowhere to go, in case the run ends (or moves
            // to the shop) before the player ever clears that selection
            // themselves.
            _handView.SetShufflePulsing(false);
            switch (state)
            {
                case RunState.AwaitingShop:
                    if (_run.LastRoundEndLueurBonus > 0)
                    {
                        StartCoroutine(PlayRoundEndLueurBonusSequence());
                    }
                    else
                    {
                        _shopView.Show(_run);
                    }
                    break;

                case RunState.RunVictory:
                {
                    bool isNewBestScore = RecordRunOutcome(victory: true, roundReached: _run.Challenge.RoundCount);
                    _endScreenView.ShowVictory(_run.TotalScore, _metaStats, isNewBestScore);
                    break;
                }

                case RunState.RunDefeat:
                {
                    bool isNewBestScore = RecordRunOutcome(victory: false, roundReached: _run.CurrentRoundNumber);
                    _endScreenView.ShowDefeat(_run.CurrentRoundNumber, _run.TotalScore, _metaStats, isNewBestScore);
                    break;
                }
            }
        }

        /// <summary>
        /// Replays the round-end "unused piece budget -> Lueur" bonus
        /// RunManager.EvaluateRoundEnd already applied to Lueur in full (see
        /// RunManager.LastRoundEndLueurBonus). One tick per unused piece:
        /// the pieces bar counts down and a "+1" popup flies to the Lueur
        /// label. Purely a presentation replay — Core's Lueur/
        /// PiecesRemainingThisRound are already final; only the local
        /// displayed values animate. The shop opens once this finishes.
        ///
        /// Blocks hand/modifier-panel input for its whole duration — by the
        /// time HandleStateTransition calls this, PlayPlacementSequence has
        /// already re-enabled the hand, so without this a piece could still
        /// be selected (and its drag ghost left stuck) between the two.
        /// </summary>
        private System.Collections.IEnumerator PlayRoundEndLueurBonusSequence()
        {
            _isPlayingPlacementSequence = true;
            _handView.SetInteractable(false);
            _modifierPanelView.SetInteractable(false);

            int bonus = _run.LastRoundEndLueurBonus;
            int displayedPieces = bonus;
            int displayedLueur = _run.Lueur - bonus;
            var fromAnchor = _hudView.PiecesBarTransform;

            for (int i = 0; i < bonus; i++)
            {
                displayedPieces--;
                displayedLueur++;
                _hudView.SetPieces(displayedPieces, _run.CurrentBudget);
                _feedbackLayer.SpawnFlyingPopup(fromAnchor.position, _hudView.LueurLabelTransform, "+1", VisualDefaults.GoldenColor);
                _hudView.SetLueur(displayedLueur);
                SfxManager.Play(SfxId.LueurGain);
                yield return new WaitForSeconds(RoundEndLueurBonusStaggerSeconds);
            }

            _isPlayingPlacementSequence = false;
            _handView.SetInteractable(true);
            RefreshShuffleButton();
            _modifierPanelView.SetInteractable(true);
            _shopView.Show(_run);
        }

        /// <summary>Folds this run's outcome into the persisted MetaStats and saves immediately — there's no guaranteed exit hook in a WebGL/browser build. Returns whether this run's score is a new all-time best, for the end screen's "new record" callout.</summary>
        private bool RecordRunOutcome(bool victory, int roundReached)
        {
            bool isNewBestScore = _run.TotalScore > _metaStats.BestScore;
            _metaStats = MetaStatsRecorder.RecordRunOutcome(_metaStats, _run.TotalScore, roundReached, victory);
            _metaStatsStore.Save(_metaStats);
            return isNewBestScore;
        }

        private void OnBlisterBuyRequested(int index)
        {
            if (index < 0 || index >= _run.ShopBlisterSlots.Count || _run.ShopBlisterSlots[index] == null)
            {
                return;
            }
            var slot = _run.ShopBlisterSlots[index];
            // A Modifier-kind slot applies outright, no follow-up screen
            // needed. An Upgrade-kind slot needs the same reveal/sub-choice
            // dispatch a Casino purchase does (see
            // HandleUpgradePurchaseResult) — its identity was already
            // visible before buying, but what happens next is unchanged.
            bool isUpgrade = slot.Kind == ShopSlotKind.Upgrade;
            var revealedUpgrade = isUpgrade ? slot.HiddenUpgrade : null;

            if (!_run.BuyBlisterSlot(index))
            {
                return;
            }

            if (isUpgrade)
            {
                HandleUpgradePurchaseResult(revealedUpgrade);
            }
            else
            {
                RefreshAll();
                _shopView.Refresh(_run);
            }
        }

        private void OnUpgradeBuyRequested(int index)
        {
            if (index < 0 || index >= _run.ShopUpgradeSlots.Count || _run.ShopUpgradeSlots[index] == null)
            {
                return;
            }
            // HiddenUpgrade stays readable on the ShopSlot object itself
            // after purchase (RunManager only flips Purchased, never clears
            // it) — grabbed here so the Joker case (applies immediately,
            // leaves PendingUpgrade null) still has something to reveal.
            var revealedUpgrade = _run.ShopUpgradeSlots[index].HiddenUpgrade;

            if (!_run.BuyUpgradeSlot(index))
            {
                return;
            }
            HandleUpgradePurchaseResult(revealedUpgrade);
        }

        /// <summary>
        /// Shared post-purchase reveal/follow-up dispatch for an upgrade
        /// just bought, whichever shop section it came from — the
        /// resolution logic only depends on RunManager.PendingUpgrade/
        /// LastRandomModifierGranted, never on which section triggered it.
        /// </summary>
        private void HandleUpgradePurchaseResult(UpgradeDefinition revealedUpgrade)
        {
            var pending = _run.PendingUpgrade;
            // Deferred until the carousel is dismissed (see
            // OnModifierCarouselDismissed) so the panel doesn't spoil the
            // reveal. Core has already granted it — only the panel's own
            // refresh needs to wait; everything else updates immediately.
            bool willShowModifierCarousel = pending == null && revealedUpgrade.Id == UpgradeId.RandomModifier && _run.LastRandomModifierGranted.HasValue;
            RefreshAll(refreshModifierPanel: !willShowModifierCarousel);
            _shopView.Refresh(_run);

            if (pending == null)
            {
                // A Bank upgrade with no sub-choice (Joker or Random
                // Modifier) — already applied; still shows the reveal card.
                // No reveal at all if the gamble didn't pay off
                // (LastRandomModifierGranted null — already at the cap).
                if (revealedUpgrade.Id == UpgradeId.JokerPiece)
                {
                    _upgradeRevealView.Show(revealedUpgrade, _run.LastJokerShapeAdded, PieceColor.Joker, new PieceTrait(_run.LastJokerCombatKindAdded, 0));
                }
                else if (revealedUpgrade.Id == UpgradeId.PieceMastery && _run.LastShapeMasteryGranted.HasValue)
                {
                    var grantedShape = _run.LastShapeMasteryGranted.Value;
                    _shapeCarouselView.Show(grantedShape, _run.GetShapeMasteryLevel(grantedShape));
                }
                else if (revealedUpgrade.Id == UpgradeId.ColorMastery && _run.LastColorMasteryGranted.HasValue)
                {
                    var grantedColor = _run.LastColorMasteryGranted.Value;
                    _colorCarouselView.Show(grantedColor, _run.GetColorMasteryLevel(grantedColor));
                }
                else if (_run.LastRandomModifierGranted.HasValue)
                {
                    _modifierCarouselView.Show(_run.LastRandomModifierGranted.Value, "Random modifier");
                }
                return;
            }
            if (pending.Pool == UpgradePool.Grid)
            {
                _tileChoiceView.Show(_run, _run.Deck, _run.PendingUpgradeTileCandidates, EconomyConstants.ShopTileChoiceCount, pending);
            }
            else if (pending.Id == UpgradeId.RandomPiece)
            {
                _pieceChoiceView.Show(_run.PendingUpgradePieceCandidates, pending, _run);
            }
            else if (pending.Id == UpgradeId.ModifierUpgrade)
            {
                _modifierUpgradeChoiceView.Show(_run.ActiveModifiers, index => _run.GetModifierLevel(index), pending);
            }
            else
            {
                _draftView.ShowForPendingUpgrade(pending, _run.PendingUpgradeTypeCandidates, _run);
            }
        }

        private void OnSubChoiceConfirmed(UpgradeSubChoice subChoice)
        {
            _run.ResolveUpgradeSubChoice(subChoice);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnTileChoiceConfirmed(IReadOnlyList<int> chosenDeckIndices)
        {
            _run.ResolveUpgradeTileChoice(chosenDeckIndices);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnPieceChoiceConfirmed(int candidateIndex)
        {
            _run.ResolveUpgradePieceChoice(candidateIndex);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnModifierUpgradeChoiceConfirmed(int slotIndex)
        {
            _run.ResolveModifierUpgradeChoice(slotIndex);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnRerollRequested()
        {
            _run.RerollShop();
            _shopView.Refresh(_run);
            _hudView.Refresh(_run);
        }

        private void OnLeaveShopRequested()
        {
            if (!_run.LeaveShop())
            {
                return;
            }
            _shopView.Hide();
            RefreshAll();
            SetStatusText(GetCurrentRoundStatus());
        }

        private string GetCurrentRoundStatus()
        {
            // An active encounter always suppresses the BossEffect roll
            // (see RunManager.HasActiveEncounter), so this short-circuits
            // before the switch below falls through to its generic default.
            if (_run.HasActiveEncounter)
            {
                return BuildEncounterStatus(_run.CurrentEncounter);
            }

            string status;
            switch (_run.CurrentBossEffect)
            {
                case BossEffect.ProgressiveCellLock:
                    status = "Boss round: every " + _run.Challenge.BossLockPiecesInterval + " pieces, " + _run.Challenge.BossLockCellsPerInterval + " cell(s) will lock.";
                    break;
                case BossEffect.LockedHandSlot:
                    status = "Boss round: hand slot " + (_run.BossLockedHandSlotIndex.Value + 1) + " is locked for this round.";
                    break;
                case BossEffect.CursedColor:
                    status = VisualDefaults.GetColorName(_run.BossCursedColor.Value) + " pieces score 0 points this round.";
                    break;
                default:
                    status = _run.Challenge.BossActiveEveryRound
                        ? "Chaos: every " + _run.Challenge.BossLockPiecesInterval + " pieces, " + _run.Challenge.BossLockCellsPerInterval + " cell(s) will lock."
                        : "New round: select a piece, then click the grid.";
                    break;
            }
            if (_run.Challenge.BossActiveEveryRound && _run.CurrentBossEffect != BossEffect.None && _run.CurrentBossEffect != BossEffect.ProgressiveCellLock)
            {
                status += " Chaos also locks a cell every " + _run.Challenge.BossLockPiecesInterval + " pieces.";
            }
            return status;
        }

        /// <summary>Lists every enemy in this round's encounter by name — a defeated one stays listed, marked "(defeated)", rather than dropped, so the count always matches EncounterCatalog's roster for the round.</summary>
        private static string BuildEncounterStatus(IReadOnlyList<EnemyInstance> encounter)
        {
            var text = new System.Text.StringBuilder("Enemy encounter: ");
            for (int i = 0; i < encounter.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(encounter[i].Definition.Name);
                if (encounter[i].IsDead)
                {
                    text.Append(" (defeated)");
                }
            }
            text.Append(". Defeat them all to clear the round.");
            return text.ToString();
        }

        /// <summary>"New Run" goes back through the challenge picker rather than immediately restarting the same challenge — see OnChallengeChosen for what starts the next run once one is picked.</summary>
        private void OnRestartRequested()
        {
            _endScreenView.Hide();
            _challengeSelectView.Show(_metaStats);
        }

        /// <summary>Spends Stars to unlock <paramref name="challenge"/> if it isn't already (a no-op charge for Classic/an already-unlocked one — see MetaStatsRecorder.TryUnlockChallenge), persists immediately, then starts the run. Re-checked here rather than trusted blindly, since this is the one thing in this flow that spends currency.</summary>
        private void OnChallengeChosen(ChallengeDefinition challenge)
        {
            var (updated, success) = MetaStatsRecorder.TryUnlockChallenge(_metaStats, challenge);
            if (!success)
            {
                _challengeSelectView.Refresh(_metaStats);
                return;
            }
            _metaStats = updated;
            _metaStatsStore.Save(_metaStats);

            _challengeSelectView.Hide();
            StartNewRun(challenge);

            if (PlayerPrefs.GetInt(TutorialSeenPrefsKey, 0) == 0)
            {
                PlayerPrefs.SetInt(TutorialSeenPrefsKey, 1);
                PlayerPrefs.Save();
                _tutorialView.Show();
            }
        }

        private void OnModifierCarouselDismissed()
        {
            // Only now does the modifier panel learn about the shop-bought
            // Random Modifier the carousel just revealed — refreshing
            // earlier would show the badge in the side panel while the
            // carousel's spin was still playing.
            _modifierPanelView.Refresh(_run.ActiveModifiers);
        }

        private void StartNewRun(ChallengeDefinition challenge)
        {
            _isPlayingPlacementSequence = false;
            _run = new RunManager(new SystemRandomProvider(), challenge);
            _gridView.Rebind(_run.Grid);
            _handView.Rebind(_run);
            _draftView.Rebind(_run.Deck);
            _tileChoiceView.Rebind(_run.Deck);
            _deckView.Rebind(_run);
            _deckView.Hide();

            RefreshAll();
            SetStatusText(IdleStatusMessage);
        }

        /// <summary><paramref name="refreshModifierPanel"/> defaults to true; OnUpgradeBuyRequested passes false when a Random Modifier grant is about to reveal through the carousel, deferring the panel's own refresh until that reveal is dismissed (see its own comment for why).</summary>
        private void RefreshAll(bool refreshModifierPanel = true)
        {
            _gridView.Refresh();
            _handView.SetBossLockedSlot(_run.BossLockedHandSlotIndex);
            _handView.Refresh();
            RefreshShuffleButton();
            _hudView.Refresh(_run);
            if (refreshModifierPanel)
            {
                _modifierPanelView.Refresh(_run.ActiveModifiers);
            }
        }
    }
}
