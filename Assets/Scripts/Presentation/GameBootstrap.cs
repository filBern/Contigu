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
        private const int SubtractionPopupFontSize = 44;
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
        private PieceChoiceView _pieceChoiceView;
        private MasteryChoiceView _masteryChoiceView;
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
                _feedbackLayer.SpawnPollenFlight(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + refundedLueur, VisualDefaults.GoldenColor);
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
            SetStatusText("F7: upgrade slot 1 is now Random Fertilizer — buy it for real.");
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
            _statusText.gameObject.SetActive(false);

            // Slightly above screen center to make room for the horizontal
            // hand row below while keeping a clear gap below the status text.
            _gridView = gameObject.AddComponent<GridView>();
            var gridRect = _gridView.Build(mainRoot, _run.Grid, CellSize, _tooltipView);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, 79f);

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
            _shopView.UseSharedLueur(_hudView.LueurLabelTransform, _hudView.LueurLabel);

            _draftView = gameObject.AddComponent<DraftView>();
            _draftView.Build(mainRoot, _run.Deck, _tooltipView);

            _tileChoiceView = gameObject.AddComponent<TileChoiceView>();
            _tileChoiceView.Build(mainRoot, _tooltipView);

            _pieceChoiceView = gameObject.AddComponent<PieceChoiceView>();
            _pieceChoiceView.Build(mainRoot, _tooltipView);

            _masteryChoiceView = gameObject.AddComponent<MasteryChoiceView>();
            _masteryChoiceView.Build(mainRoot);

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
            PieceBox.SetSidePanelCenterX(sidePanelCenterX, ModifierPanelView.PanelWidth);
            OverlayLift.Panels.Clear();
            OverlayLift.Panels.Add(modifierPanelRect);
            OverlayLift.Panels.Add(_shopView.UpcomingBox);
            OverlayLift.Panels.Add(_hudView.LueurLabelTransform);
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
            _pieceChoiceView.PieceChoiceConfirmed += OnPieceChoiceConfirmed;
            _masteryChoiceView.OptionChosen += OnMasteryOptionChosen;
            _modifierUpgradeChoiceView.ModifierUpgradeChoiceConfirmed += OnModifierUpgradeChoiceConfirmed;
            _endScreenView.RestartRequested += OnRestartRequested;
            _endScreenView.EndlessRequested += OnEndlessRequested;
            _challengeSelectView.ChallengeChosen += OnChallengeChosen;
            _challengeSelectView.UnlockAllRequested += OnUnlockAllChallengesRequested;
            _modifierCarouselView.Dismissed += OnModifierCarouselDismissed;
        }

        /// <summary>Quick access to the deck and rules, anchored below the combo card so their position follows its content-driven height.</summary>
        private void BuildComboUtilityButtons(Transform comboParent)
        {
            var deckButton = UIFactory.CreateButton(comboParent, "ShowDeckButton", "Seed bag", UISprites.HandUtilityButtonBackground, 12);
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

            var rulesButton = UIFactory.CreateButton(comboParent, "ShowRulesButton", "Rules", UISprites.HandUtilityButtonBackground, 12);
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
    }
}
