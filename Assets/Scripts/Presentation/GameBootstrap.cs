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
    public sealed partial class GameBootstrap : MonoBehaviour
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
        // Empty on purpose: no idle prompt text any more (the status line only shows real events/errors).
        private const string IdleStatusMessage = "";
        private const int DefaultStatusFontSize = 19;
        private const int EmphasizedStatusFontSize = 27;
        private const string TutorialSeenPrefsKey = "TutorialSeen";
        private const string BoardEffectTipPrefsKey = "SprigBoardEffectTip";
        private const string NegativeScoreTipPrefsKey = "SprigNegativeScoreTip";
        // Juice: a big Combo (at least two stacked Combo modifiers) or a Boss-tier kill shakes the grid —
        // see the "COMBO xN" callout below and DrainComboIntoDamage/DrainSecondaryEnemyHits.
        private const int BigComboShakeThreshold = 4;
        private const float ComboShakeDuration = 0.25f;
        private const float ComboShakeMagnitude = 10f;
        private const float BossKillShakeDuration = 0.35f;
        private const float BossKillShakeMagnitude = 16f;
        // Red subtraction text (poison-flipped score, enemy damage) was getting lost at the same size as
        // every other popup — these are exactly double FeedbackLayer's own DefaultPopupFontSize/
        // DefaultFlyingPopupFontSize.
        private const int SubtractionPopupFontSize = 66;
        private const int SubtractionFlyingPopupFontSize = 40;
        // Pause between the combo total finishing its catch-up and DrainComboIntoDamage starting, so the
        // player gets a beat to read the final number before it starts draining into enemy damage.
        private const float PostComboReadDelaySeconds = 0.5f;

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
        private ShelfChoiceView _shelfChoiceView;
        private Button _abandonButton;
        private Text _abandonLabel;
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
        private CollectionView _collectionView;
        private SprigView _sprigView;
        private ChallengeSelectView _challengeSelectView;
        private SettingsView _settingsView;
        private ChallengeSceneryView _sceneryView;
        private AnimatedBackgroundView _animatedBackgroundView;
        private AmbientLifeView _ambientLifeView;
        private RectTransform _mainRoot;
        private Coroutine _screenShakeCoroutine;
        private RoundBannerView _roundBannerView;
        private FeedbackLayer _feedbackLayer;
        private Text _statusText;
        private Coroutine _statusPulseCoroutine;
        private bool _playingSequence;

        /// <summary>True while a placement or shuffle sequence plays; the speed button (x2/x4) only applies then.</summary>
        private bool _isPlayingPlacementSequence
        {
            get { return _playingSequence; }
            set
            {
                _playingSequence = value;
                ApplyTimeScale();
            }
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = _playingSequence ? GameSpeed.Multiplier : 1f;
        }

        private void Awake()
        {
            EnsureEventSystem();
            var canvasRect = BuildCanvas();

            _run = new RunManager(new SystemRandomProvider());
            GameSpeed.Changed += ApplyTimeScale;
            RegisterQaBridge();
            TileUpgradePreview.Provider = kind => _run != null ? _run.GetTileUpgradePreviewText(kind) : null;
            _metaStatsStore = new MetaStatsFileStore();
            _metaStats = _metaStatsStore.Load();

            BuildUI(canvasRect);
            WireEvents();
            RefreshAll();

            // RefreshAll covers grid/hand/HUD/modifier panel; the shop and
            // deck-view overlay are only rebuilt if actually open, since a
            // hidden one renders correctly next time it's shown anyway.

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
            //
            // The main menu's own tile sweep survived the scene load (DontDestroyOnLoad) covering the whole
            // screen — now that this scene's UI is actually built and ready underneath it, continue the same
            // animation by sweeping the tiles away instead of leaving the screen covered. PendingReveal is
            // null when this scene was entered any other way (e.g. opened directly in the editor), so the
            // carousel just shows immediately then, same as before this sweep existed. When it isn't null,
            // ChallengeSelectView.Show is deferred to PlayUncoverSweep's onNearlyRevealed callback — a beat
            // before the very last tile vanishes (see EarlyRevealOffset) rather than its full completion —
            // so the carousel's own fade-in (see ChallengeSelectView.Show) overlaps the tail of the sweep
            // instead of only starting once the screen is already fully clear.
            if (MainMenuTransitionView.PendingReveal != null)
            {
                var reveal = MainMenuTransitionView.PendingReveal;
                MainMenuTransitionView.PendingReveal = null;
                reveal.PlayUncoverSweep(onNearlyRevealed: () => _challengeSelectView.Show(_metaStats), onRevealed: null);
            }
            else
            {
                _challengeSelectView.Show(_metaStats);
            }
        }

        /// <summary>Scales every AudioSource in the scene (see AudioListener.volume) by VolumeSettings.MasterVolume — real, immediate effect even with zero clips loaded yet, unlike Music/SFX which have no bus of their own to attenuate without an AudioMixer.</summary>
        private static void ApplyVolumeSettings()
        {
            AudioListener.volume = VolumeSettings.MasterVolume;
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
            if (Input.GetKeyDown(KeyCode.F10))
            {
                OnUnlockAllChallengesRequested();
            }
#endif
            // Unlike the F-key shortcuts above, these bindings are always
            // available, including in real builds.
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _deckView.Toggle();
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                _tutorialView.Show();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Back: close the topmost thing first (settings, then the seed bag, then a purchase in progress); only then open the menu.
                if (_settingsView.IsVisible)
                {
                    _settingsView.Toggle();
                }
                else if (_deckView.IsVisible)
                {
                    _deckView.Hide();
                }
                else if (_run != null && _run.State == RunState.AwaitingShop && _run.PendingUpgrade != null)
                {
                    RequestAbandonPurchase();
                }
                else
                {
                    _settingsView.Toggle();
                }
            }
            UpdateAbandonButton();
            if (Input.GetKeyDown(KeyCode.X))
            {
                TrySellHoveredModifier();
            }
        }

        /// <summary>Sells the currently hovered modifier badge; ModifierPanelView.HoveredRowIndex is -1 while the panel is blocked mid-animation. No-op if nothing's hovered or the run isn't InProgress/AwaitingShop.</summary>
        private void TrySellHoveredModifier()
        {
            TrySellModifierAt(_modifierPanelView.HoveredRowIndex);
        }

        /// <summary>Sells the fertilizer in row <paramref name="hoveredIndex"/> (the X key under the pointer, or the Sell button under a tapped badge).</summary>
        private void TrySellModifierAt(int hoveredIndex)
        {
            if (_run.State != RunState.InProgress && _run.State != RunState.AwaitingShop)
            {
                return;
            }
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
                _feedbackLayer.SpawnPollenFlight(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + refundedLueur, UITheme.ScorePollen);
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
            SetStatusText("Sold " + ModifierCatalog.Get(id).Name + " for " + refundedLueur + " Pollen.");
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
            SetStatusText("F7: upgrade slot 1 is now Random Fertilizer - buy it for real.");
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
                _modifierCarouselView.Show(granted.Value, "Random fertilizer");
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
            // The grey status line above the grid (round/enemy hints, drag prompt, warnings) is gone to give the grid the room.
            // Callers still go through here, so the text is kept in the (hidden) label for debugging.
            _statusText.text = text;
            _statusText.fontSize = emphasize ? EmphasizedStatusFontSize : DefaultStatusFontSize;
            StopStatusPulse();
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
            canvasGo.AddComponent<CanvasAspectFit>();

            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.CreatePanel(canvasGo.transform, "Background", UITheme.Background);
            UIFactory.StretchFull(bg.rectTransform);

            _sceneryView = gameObject.AddComponent<ChallengeSceneryView>();
            _sceneryView.Build(canvasGo.transform);

            // Sits directly above the flat fill and below everything built
            // in BuildUI below (MainRoot, added as canvasGo's next child
            // after this) — see AnimatedBackgroundView's own doc comment.
            _animatedBackgroundView = gameObject.AddComponent<AnimatedBackgroundView>();
            _animatedBackgroundView.Build(canvasGo.transform);
            // Butterflies, leaves and the weather's particles, behind everything built in BuildUI.
            _ambientLifeView = gameObject.AddComponent<AmbientLifeView>();
            _ambientLifeView.Build(canvasGo.transform);
            ApplyChallengeAmbiance(_run != null ? _run.Challenge : ChallengeCatalog.Classic);

            return canvasGo.GetComponent<RectTransform>();
        }

        private void BuildUI(RectTransform canvas)
        {
            var mainRoot = UIFactory.CreateUIObject("MainRoot", canvas);
            UIFactory.StretchFull(mainRoot);
            _mainRoot = mainRoot;
            mainRoot.gameObject.AddComponent<SafeAreaFitter>();

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
            _comboView.AttachTooltip(_tooltipView);

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
            _statusText.gameObject.SetActive(false);

            // One row, centered: [score, fertilizers, Seed bag / Rules] [grid] [hand, a tall column]. The grid and the hand column take the whole
            // height between the enemy band and the bottom bar; the left column stacks the score card on top, the buttons at the bottom and the
            // fertilizers between them.
            _gridView = gameObject.AddComponent<GridView>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(comboRect);
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1.6f;
            float canvasHeight = aspect >= 1.6f ? 800f : 1280f / aspect;
            // Never narrower than the 1280 the layout is drawn for: below 16:10 the canvas scales to the width (see CanvasAspectFit).
            float canvasWidth = Mathf.Max(1280f, 800f * aspect);
            float halfCanvasWidth = canvasWidth * 0.5f;
            const float ZoneMargin = 12f;
            const float ColumnGap = 24f;
            const float ScreenMargin = 16f;
            float zoneTopY = canvasHeight * 0.5f - (HudView.EnemyBandHeight + ZoneMargin);
            float zoneBottomY = -canvasHeight * 0.5f + HudView.BarHeight + ZoneMargin;
            float leftColumnWidth = ModifierPanelView.PanelWidth; // the score card, the fertilizers and the two buttons are all this wide
            const float PlotSpacing = 2f; // GridView.CellSpacing
            int plots = GridManager.Size;
            float cellByHeight = (zoneTopY - zoneBottomY - (plots - 1) * PlotSpacing) / plots;
            float cellByWidth = (canvasWidth - 2f * ScreenMargin - leftColumnWidth - HandView.ColumnWidth - 2f * ColumnGap - (plots - 1) * PlotSpacing) / plots;
            float gridCellSize = Mathf.Clamp(Mathf.Min(cellByHeight, cellByWidth), CellSize, 130f);
            var gridRect = _gridView.Build(mainRoot, _run.Grid, gridCellSize, _tooltipView);
            float gridSize = plots * gridCellSize + (plots - 1) * PlotSpacing;
            float rowWidth = leftColumnWidth + ColumnGap + gridSize + ColumnGap + HandView.ColumnWidth;
            float rowStartX = -rowWidth * 0.5f;
            float leftColumnX = rowStartX + leftColumnWidth * 0.5f;
            float gridCenterX = rowStartX + leftColumnWidth + ColumnGap + gridSize * 0.5f;
            float handColumnX = rowStartX + leftColumnWidth + ColumnGap + gridSize + ColumnGap + HandView.ColumnWidth * 0.5f;
            float gridCenterY = (zoneTopY + zoneBottomY) * 0.5f;
            float gridTopY = gridCenterY + gridSize * 0.5f;
            float gridBottomY = gridCenterY - gridSize * 0.5f;
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(gridCenterX, gridCenterY);

            // The shop keeps its own side-panel spots (mirrored, see ShopView); these two feed it.
            const float SidePanelEdgeMargin = 64f;
            float sidePanelCenterX = halfCanvasWidth - SidePanelEdgeMargin - ModifierPanelView.PanelWidth * 0.5f;

            // Left column, top: the score card.
            comboRect.anchoredPosition = new Vector2(leftColumnX, gridTopY - comboRect.rect.height * 0.5f);
            // Left column, bottom: Seed bag over Rules, full width.
            const float UtilityButtonHeight = 56f;
            const float UtilityButtonGap = 8f;
            float rulesY = gridBottomY + UtilityButtonHeight * 0.5f;
            float deckY = rulesY + UtilityButtonHeight + UtilityButtonGap;
            // Left column, between: the fertilizer panel, centered in what is left.
            float scoreBottomY = gridTopY - comboRect.rect.height;
            float buttonsTopY = deckY + UtilityButtonHeight * 0.5f;
            float fertilizerY = (scoreBottomY + buttonsTopY) * 0.5f;

            // Pollen is a small chip in the top-left HUD, on the speed button's row (right of it), growing rightwards as the count does;
            // the shop moves it back above its own panel and then restores it here.
            var lueurRect = _hudView.LueurLabelTransform;
            lueurRect.anchorMin = new Vector2(0.5f, 0.5f);
            lueurRect.anchorMax = new Vector2(0.5f, 0.5f);
            lueurRect.pivot = new Vector2(0f, 0.5f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(lueurRect);
            // Sized to be about as tall as the x1 button beside it (44): no pill behind it any more, the band is dark.
            const float BoardPollenScale = 0.45f;
            lueurRect.localScale = Vector3.one * BoardPollenScale;
            float pollenX = -halfCanvasWidth + 16f + MobileSetup.TouchMin(64f) + 14f;
            float pollenY = canvasHeight * 0.5f - 62f - MobileSetup.TouchMin(44f) * 0.5f;
            lueurRect.anchoredPosition = new Vector2(pollenX, pollenY);

            // The hand: a tall column on the right, as tall as the grid.
            _handView = gameObject.AddComponent<HandView>();
            var handRect = _handView.Build(mainRoot, _run, _tooltipView);
            handRect.anchorMin = new Vector2(0.5f, 0.5f);
            handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = new Vector2(0.5f, 0.5f);
            _handView.SetColumn(handColumnX, gridCenterY, gridSize);
            handRect.anchoredPosition = new Vector2(handColumnX, gridCenterY);
            // Now that the column is placed, let it size its slots.
            _handView.Refresh();
            BuildComboUtilityButtons(mainRoot, leftColumnX, deckY, rulesY, leftColumnWidth, UtilityButtonHeight);

            _feedbackLayer = gameObject.AddComponent<FeedbackLayer>();
            _feedbackLayer.Build(mainRoot);

            // Layered in this order so later ones render on top: the shop
            // is the base screen, the sub-choice/tile-choice overlays cover
            // it once a purchase needs a follow-up.
            _shopView = gameObject.AddComponent<ShopView>();
            _shopView.Build(mainRoot, _tooltipView);
            // Built after the shop so it stays above the shop board: Menu (settings, Save & quit) must be reachable from the shop too.
            BuildMenuButton(mainRoot);
            _shopView.UseSharedLueur(_hudView.LueurLabelTransform, _hudView.LueurLabel);

            _draftView = gameObject.AddComponent<DraftView>();
            _draftView.Build(mainRoot, _run.Deck, _tooltipView);

            _tileChoiceView = gameObject.AddComponent<TileChoiceView>();
            _tileChoiceView.Build(mainRoot, _tooltipView);

            _shelfChoiceView = gameObject.AddComponent<ShelfChoiceView>();
            _shelfChoiceView.Build(mainRoot, _tooltipView);

            _abandonButton = UIFactory.CreateButton(mainRoot, "AbandonPurchase", Loc.T("choice.abandon"), UISprites.CancelButtonBackground, 15);
            UIFactory.DisablePlants(_abandonButton);
            _abandonLabel = _abandonButton.GetComponentInChildren<Text>();
            var abandonRect = _abandonButton.GetComponent<RectTransform>();
            abandonRect.anchorMin = new Vector2(1f, 0f);
            abandonRect.anchorMax = new Vector2(1f, 0f);
            abandonRect.pivot = new Vector2(1f, 0f);
            abandonRect.anchoredPosition = new Vector2(-16f, 40f);
            abandonRect.sizeDelta = new Vector2(330f, 46f);
            _abandonButton.onClick.AddListener(RequestAbandonPurchase);
            _abandonButton.gameObject.SetActive(false);

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
            var modifierPanelRect = _modifierPanelView.Build(mainRoot, _tooltipView, id => _run.GetModifierUsageCount(id), (id, index) => _run.GetProgressiveModifierStateText(id, index), index => _run.GetModifierLevel(index), -sidePanelCenterX);
            _modifierPanelView.BlightProvider = index => _run.IsModifierBlighted(index);
            _modifierPanelView.RoundsLeftProvider = index => _run.GetModifierRoundsLeft(index);
            _shopView.SetSidePanelCenterX(sidePanelCenterX);
            modifierPanelRect.anchoredPosition = new Vector2(leftColumnX, fertilizerY);
            _shopView.SetBoardPanels(modifierPanelRect, leftColumnX, fertilizerY, pollenX, pollenY, BoardPollenScale);
            PieceBox.SetSidePanelCenterX(sidePanelCenterX, ModifierPanelView.PanelWidth);
            OverlayLift.Panels.Clear();
            OverlayLift.Panels.Add(modifierPanelRect);
            OverlayLift.Panels.Add(_shopView.UpcomingBox);
            OverlayLift.Panels.Add(_hudView.LueurLabelTransform);
            // Modifier order determines scoring order (see
            // PlacementResult.Mult); same lambda-captures-_run reasoning as
            // above.
            _modifierPanelView.SellRequested += TrySellModifierAt;
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

            _collectionView = gameObject.AddComponent<CollectionView>();
            _collectionView.Build(mainRoot, _tooltipView);
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

            _sprigView = gameObject.AddComponent<SprigView>();
            _sprigView.Build(mainRoot);

            _roundBannerView = gameObject.AddComponent<RoundBannerView>();
            _roundBannerView.Build(mainRoot);
        }

        /// <summary>The short first-run guide Sprig gives in speech bubbles (frames the hand, grid and first enemy). Press H any time for the full rules.</summary>
        private List<SprigGuideStep> BuildSprigGuide()
        {
            return new List<SprigGuideStep>
            {
                new SprigGuideStep { Mood = SprigMood.Cheer, Text = "Hi, I'm Sprig, your head gardener! Let me show you around. It only takes a moment." },
                new SprigGuideStep
                {
                    Text = "These are your seeds. Drag one onto the grid, or click it and then click a plot.",
                    Targets = () => new[] { _handView.GetSlotPreviewTransform(0), _handView.GetSlotPreviewTransform(Mathf.Max(0, _run.Deck.Hand.Count - 1)) }
                },
                new SprigGuideStep
                {
                    Mood = SprigMood.Neutral,
                    Text = "Seeds of the same color that touch score together. Fill a whole row or column to harvest it for bonus points and Pollen!",
                    Targets = () => new[] { _gridView.GetCellTransform(0, 0), _gridView.GetCellTransform(GridManager.Size - 1, GridManager.Size - 1) }
                },
                new SprigGuideStep
                {
                    Mood = SprigMood.Neutral,
                    Text = "Pests guard each round. Your score becomes damage on the leftmost one. Hover a pest to read its tricks!",
                    Targets = () => new[] { _hudView.GetEnemyIconTransform(0) }
                },
                new SprigGuideStep
                {
                    Mood = SprigMood.Sad,
                    Text = "Careful: pests fight on your garden itself! They lock plots, blight tiles so they score negative, and steal seeds. Plan your planting around them.",
                    Targets = () => new[] { _gridView.GetCellTransform(0, 0), _gridView.GetCellTransform(GridManager.Size - 1, GridManager.Size - 1) }
                },
                new SprigGuideStep { Text = "Stuck? Shuffle swaps your hand. Between rounds, spend Pollen on fertilizers and upgrades. Press H for the full rules." },
                new SprigGuideStep { Mood = SprigMood.Cheer, Text = "Now go make it bloom. Happy planting!" },
            };
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
            _shopView.SeedBagRequested += () => _deckView.Show();
            _modifierUpgradeChoiceView.ModifierUpgradeChoiceConfirmed += OnModifierUpgradeChoiceConfirmed;
            _endScreenView.RestartRequested += OnRestartRequested;
            _endScreenView.EndlessRequested += OnEndlessRequested;
            _challengeSelectView.ChallengeChosen += OnChallengeChosen;
            _challengeSelectView.ContinueRequested += OnContinueRequested;
            _challengeSelectView.CollectionRequested += _collectionView.Show;
            _challengeSelectView.UnlockAllRequested += OnUnlockAllChallengesRequested;
            _settingsView.AbandonRunRequested += OnAbandonRunRequested;
            _settingsView.MainMenuRequested += OnMainMenuRequested;
            _settingsView.RulesRequested += _tutorialView.Show;
            _modifierCarouselView.Dismissed += OnModifierCarouselDismissed;
        }

        /// <summary>The shop's own "Menu" button (it opens Settings, where a run can be abandoned or left for the main menu, for players who don't know the Escape key). On the board the Menu button is in the left column.</summary>
        private void BuildMenuButton(Transform parent)
        {
            // The green button style with its own light label: black text on the wooden utility button was unreadable here.
            var menuButton = UIFactory.CreateButton(parent, "ShopMenuButton", "Menu", UISprites.ChooseButtonBackground, 12);
            UIFactory.DisablePlants(menuButton);
            var label = menuButton.GetComponentInChildren<Text>();
            label.fontSize = 22;
            // Position and size are set by the shop each time it opens (under its Seed bag button, see ShopView.PlaceSidePanels).
            menuButton.onClick.AddListener(() => _settingsView.Toggle());
            // Its own sorting layer above every overlay (shop choices, end screens, challenge picker): Menu stays reachable, and never looks active while something covers it.
            var menuCanvas = menuButton.gameObject.AddComponent<Canvas>();
            menuCanvas.overrideSorting = true;
            menuCanvas.sortingOrder = 100;
            menuButton.gameObject.AddComponent<GraphicRaycaster>();
            // Only the shop shows this one (its screens cover the board's own Menu button): the shop switches it on and off.
            menuButton.gameObject.SetActive(false);
            _shopView.SetShopMenuButton(menuButton.gameObject);
            _shopView.MenuHiddenWhen = () => _deckView != null && _deckView.IsVisible;
        }

        /// <summary>The seed bag and the menu (settings and the rules): stacked, full width, at the bottom of the left column.</summary>
        private void BuildComboUtilityButtons(Transform parent, float centerX, float deckY, float rulesY, float width, float height)
        {
            var deckButton = UIFactory.CreateButton(parent, "ShowDeckButton", "Seed bag", UISprites.HandUtilityButtonBackground, 12);
            var deckLabel = deckButton.GetComponentInChildren<Text>();
            deckLabel.color = Color.black;
            deckLabel.fontSize = 25;
            var deckRect = deckButton.GetComponent<RectTransform>();
            deckRect.anchorMin = new Vector2(0.5f, 0.5f);
            deckRect.anchorMax = new Vector2(0.5f, 0.5f);
            deckRect.pivot = new Vector2(0.5f, 0.5f);
            deckRect.anchoredPosition = new Vector2(centerX, deckY);
            deckRect.sizeDelta = new Vector2(width, height);
            deckButton.onClick.AddListener(() => _deckView.Show());

            var menuColumnButton = UIFactory.CreateButton(parent, "MenuButton", "Menu", UISprites.HandUtilityButtonBackground, 12);
            var menuColumnLabel = menuColumnButton.GetComponentInChildren<Text>();
            menuColumnLabel.color = Color.black;
            menuColumnLabel.fontSize = 25;
            var menuColumnRect = menuColumnButton.GetComponent<RectTransform>();
            menuColumnRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuColumnRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuColumnRect.pivot = new Vector2(0.5f, 0.5f);
            menuColumnRect.anchoredPosition = new Vector2(centerX, rulesY);
            menuColumnRect.sizeDelta = new Vector2(width, height);
            menuColumnButton.onClick.AddListener(() => _settingsView.Toggle());
        }
    }
}
