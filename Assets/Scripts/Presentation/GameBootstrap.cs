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
        // Lueur groups play before anything else in the sequence (explicit
        // request: "au début du décompte du score") and don't share the
        // score cascade's own combo speedup ramp — a flat, slightly slower
        // pace since each one also has to wait for its flying popup to
        // actually land (see FeedbackLayer.SpawnFlyingPopup) before the
        // reveal reads clearly.
        private const float LueurGroupStaggerSeconds = 0.3f;
        // Each combo addition waits 3% less than the previous one (on
        // explicit request — 10% was too fast), so a big combo doesn't make
        // the player sit through a long, linearly-paced popup sequence —
        // floored so a very long chain still keeps a perceptible beat
        // instead of collapsing to an instant dump.
        private const float ComboSpeedupFactor = 0.97f;
        private const float MinStaggerSeconds = 0.1f;
        // Slow "breathing" pulse on the idle status prompt (on explicit
        // request: "j'aimerais qu'il pulse lentement") — a gentle ±5% scale
        // wobble, not the sharper one-shot flash ModifierPanelView.Pulse
        // uses for score feedback.
        private const float StatusPulseAmplitude = 0.05f;
        private const float StatusPulseSpeed = 1.5f;
        private const string IdleStatusMessage = "Select or drag a piece onto the grid.";

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
        private UpgradeRevealView _upgradeRevealView;
        private ModifierPanelView _modifierPanelView;
        private TooltipView _tooltipView;
        private DeckView _deckView;
        private EndScreenView _endScreenView;
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

            // Re-renders every already-built piece-color view immediately on
            // toggle (see ColorblindMode) rather than waiting for the next
            // unrelated refresh — RefreshAll covers the grid/hand/HUD/
            // modifier panel; the shop and deck-view overlay are only
            // rebuilt too if actually open right now, since a hidden one
            // will render correctly the next time it's shown anyway.
            ColorblindMode.Changed += OnColorblindModeChanged;
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
            if (Input.GetKeyDown(KeyCode.F9))
            {
                DebugForceRoundWin();
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                DebugGrantLueurShortcut();
            }
#endif
            // Tab toggles the deck-view overlay (on explicit request: an
            // in-game way to check the deck's composition without waiting
            // for the next draft) — always available, not an editor-only
            // debug shortcut like F9 above.
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _deckView.Toggle();
            }
            // C toggles colorblind mode (see ColorblindMode) — same
            // "always-available, not editor-only" reasoning as Tab above;
            // an accessibility setting has to be reachable in a real build,
            // not just in the editor.
            if (Input.GetKeyDown(KeyCode.C))
            {
                ColorblindMode.Toggle();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only debug shortcut (F9): instantly completes the current
        /// round so the upgrade draft appears right away — lets upgrades be
        /// tested without grinding out a full round for real. Stripped from
        /// real builds by the UNITY_EDITOR guard around this whole block.
        /// </summary>
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

        /// <summary>
        /// Editor-only debug shortcut (F10): grants 100 Lueur instantly, on
        /// explicit request — lets the shop be tested (or just played with)
        /// without grinding out real line clears for it first. Same
        /// "skip the grind" spirit as F9 above, and likewise stripped from
        /// real builds by the UNITY_EDITOR guard around this whole block.
        /// </summary>
        private void DebugGrantLueurShortcut()
        {
            _run.DebugGrantLueur(100);
            RefreshAll();
            _shopView.Refresh(_run);
        }
#endif

        /// <summary>
        /// Sets the status line's text, starting or stopping its slow
        /// "breathing" pulse depending on whether it's showing the default
        /// idle prompt (on explicit request: "j'aimerais qu'il pulse
        /// lentement et qu'il soit légèrement plus gros") — every other
        /// status message (a piece selected, an error, a round transition)
        /// stays still, since a constant pulse there would compete with the
        /// message actually being new/important.
        /// </summary>
        private void SetStatusText(string text)
        {
            _statusText.text = text;
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
            // Rounds every UI element's rendered position to a whole pixel —
            // without it, ScaleWithScreenSize's non-integer scale factor on
            // most window sizes leaves text sitting at sub-pixel offsets,
            // which reads as soft/blurry under anti-aliasing (most visible
            // on Digitalt's thick strokes at small sizes).
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            // Our whole layout is a fixed-height vertical stack (HUD + status +
            // grid + hand), so match on HEIGHT (1) rather than blend width/height
            // (0.5): with match=1 the canvas is always exactly 800 units tall
            // regardless of the window's aspect ratio, so the hand row at the
            // bottom never gets squeezed off-screen on wide/short windows.
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.CreatePanel(canvasGo.transform, "Background", UITheme.Background);
            UIFactory.StretchFull(bg.rectTransform);

            return canvasGo.GetComponent<RectTransform>();
        }

        private void BuildUI(RectTransform canvas)
        {
            var mainRoot = UIFactory.CreateUIObject("MainRoot", canvas);
            UIFactory.StretchFull(mainRoot);

            // Both progress bars pin themselves to the top/bottom edges inside
            // HudView.Build — nothing to position here.
            _hudView = gameObject.AddComponent<HudView>();
            _hudView.Build(mainRoot);

            _statusText = UIFactory.CreateText(mainRoot, "Status", IdleStatusMessage, 19, UITheme.TextMuted);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Below the top bar (68 tall, see HudView.BarHeight) with a 12px gap.
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _statusText.rectTransform.sizeDelta = new Vector2(700f, 26f);
            StartStatusPulse();

            // Dead center of the screen — the top/bottom progress bars and the
            // status text float above it rather than pushing it down, so the
            // grid itself isn't biased toward the top.
            // Built before GridView/HandView since both need a live
            // TooltipView to hover (grid cells' trait-origin badges and
            // hand pieces' trait badges, respectively).
            _tooltipView = gameObject.AddComponent<TooltipView>();
            _tooltipView.Build(mainRoot);

            _gridView = gameObject.AddComponent<GridView>();
            var gridRect = _gridView.Build(mainRoot, _run.Grid, CellSize, _tooltipView);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;

            // To the right of the grid, vertically centered on it (which is
            // now screen center too). Grid right edge sits 226.5 (half of its
            // 453-wide 8x8+spacing footprint, see GridView.Build) from screen
            // center; the hand's own width is 120 (its slot width, via
            // ContentSizeFitter) so its center needs to clear the grid by
            // 226.5 + a 24 gap + its own half-width (60).
            _handView = gameObject.AddComponent<HandView>();
            var handRect = _handView.Build(mainRoot, _run.Deck, _tooltipView);
            handRect.anchorMin = new Vector2(0.5f, 0.5f);
            handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = new Vector2(0.5f, 0.5f);
            handRect.anchoredPosition = new Vector2(310f, 0f);

            _comboView = gameObject.AddComponent<ComboView>();
            var comboRect = _comboView.Build(mainRoot);
            comboRect.anchorMin = new Vector2(0.5f, 0f);
            comboRect.anchorMax = new Vector2(0.5f, 0f);
            comboRect.pivot = new Vector2(0.5f, 0.5f);
            // Centered under the grid, in the gap between the grid's bottom
            // edge and the pieces bar flush against the screen's bottom edge.
            // Grid is centered on an always-800-tall canvas (CanvasScaler
            // matches height) and 453 tall, so its bottom edge sits 400 -
            // 453/2 = 173.5 above the bottom. The pieces bar is 68 tall (see
            // HudView.BarHeight). Midpoint between the grid's bottom and the
            // bar's top: (173.5 + 68) / 2 = 120.75.
            comboRect.anchoredPosition = new Vector2(0f, 120.75f);
            // No explicit sizeDelta — ComboView's own ContentSizeFitter
            // sizes it to fit its two pills (chips + mult).

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

            _upgradeRevealView = gameObject.AddComponent<UpgradeRevealView>();
            _upgradeRevealView.Build(mainRoot, _tooltipView);

            _modifierPanelView = gameObject.AddComponent<ModifierPanelView>();
            // Lambda (not the method group _run.GetModifierUsageCount) so a
            // restart's new RunManager instance is picked up automatically —
            // _run is reassigned on restart but this view is never rebuilt,
            // only Refreshed, so a bound delegate would keep querying the
            // old, discarded run forever.
            _modifierPanelView.Build(mainRoot, _tooltipView, id => _run.GetModifierUsageCount(id), id => _run.GetProgressiveModifierStateText(id));
            // Reordering (drag-and-drop or tap-tap swap, on explicit
            // request: modifier order now determines scoring order, see
            // PlacementResult.Mult) — same "read the current _run field at
            // invocation time" reasoning as the lambdas just above, so a
            // restart's fresh RunManager is picked up automatically.
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
            _deckView.Build(mainRoot, _run.Deck, _tooltipView);

            _endScreenView = gameObject.AddComponent<EndScreenView>();
            _endScreenView.Build(mainRoot);
        }

        private void WireEvents()
        {
            _gridView.CellClicked += OnCellClicked;
            _gridView.HoverValidityChanged += _handView.SetHoveringValidDrop;
            _handView.SlotSelected += OnHandSlotSelected;
            _handView.SelectionCleared += OnHandSelectionCleared;
            _shopView.ModifierBuyRequested += OnModifierBuyRequested;
            _shopView.UpgradeBuyRequested += OnUpgradeBuyRequested;
            _shopView.RerollRequested += OnRerollRequested;
            _shopView.LeaveRequested += OnLeaveShopRequested;
            _draftView.SubChoiceConfirmed += OnSubChoiceConfirmed;
            _tileChoiceView.TileChoiceConfirmed += OnTileChoiceConfirmed;
            _endScreenView.RestartRequested += OnRestartRequested;
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
            var token = slot.Value;
            var rotation = _run.Deck.HandRotations[handIndex];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            _gridView.SetSelectedShape(shape, token.Color, token.Trait);
            SetStatusText("Drag onto the grid, or click a tile, to place: " + VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")");
        }

        /// <summary>Re-clicking the already-selected hand slot deselects it (see HandView.OnSlotClicked) — clears the grid's hover preview the same way a successful placement already does.</summary>
        private void OnHandSelectionCleared()
        {
            _gridView.SetSelectedShape(null);
            SetStatusText(IdleStatusMessage);
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

            var outcome = _run.PlacePiece(handIndex, x, y);
            if (!outcome.Placement.Success)
            {
                SetStatusText("Invalid placement there.");
                return;
            }

            _gridView.SetSelectedShape(null);
            _handView.ClearSelection();

            // Hold any completed line/column visually filled (instead of
            // instantly vanishing) while its score is still playing out.
            _gridView.RefreshHoldingClearedCells(outcome.Placement.ClearedCells, outcome.Placement.ClearedCellColors, outcome.Placement.ClearedCellTraits);
            _handView.Refresh();
            // Round/budget update immediately; the score AND Lueur numbers
            // themselves stay at their pre-placement values until
            // PlayPlacementSequence catches them up in step with each popup.
            _hudView.Refresh(_run);
            _hudView.SetLueur(lueurBefore);
            _hudView.SetScores(roundScoreBefore, _run.CurrentQuota);
            SetStatusText(IdleStatusMessage);

            _isPlayingPlacementSequence = true;
            _handView.SetInteractable(false);
            // Blocks reordering while the score sequence below reads
            // GetBadgeTransform/Pulse per event — a mid-animation Refresh()
            // from a reorder would otherwise rebuild every badge out from
            // under it.
            _modifierPanelView.SetInteractable(false);
            StartCoroutine(PlayPlacementSequence(outcome, roundScoreBefore, lueurBefore));
        }

        /// <summary>
        /// Plays a placement's full feedback sequence in order: first each
        /// Lueur group, one at a time — pulsing its cells and flying a popup
        /// to the Lueur label, advancing that display progressively as it
        /// goes (explicit request: "au début du décompte du score" — before
        /// anything else, and "progressif et non d'un coup") — then each
        /// golden/group score popup one at a time, pulsing its cell and
        /// advancing the HUD's score bar at that exact moment, so the
        /// displayed score climbs progressively instead of jumping straight
        /// to the final value — then, only once that's done, clears any
        /// completed line/column one cell at a time (each with its own
        /// popup and score bump), and only then advances the run state
        /// (draft/victory/defeat), so nothing interrupts the player while
        /// they're still reading their score.
        /// </summary>
        private System.Collections.IEnumerator PlayPlacementSequence(PlacementOutcome outcome, int roundScoreBefore, int lueurBefore)
        {
            var placement = outcome.Placement;

            // Lueur groups play first, ahead of the score cascade below (on
            // explicit request) — each group pulses its own cells, flies a
            // "+N" popup from the group's own center to the Lueur label
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

                displayedLueur += group.Amount;
                _hudView.SetLueur(displayedLueur);

                yield return new WaitForSeconds(LueurGroupStaggerSeconds);
            }

            int displayedRoundScore = roundScoreBefore;
            // Mirrors PlacementResult.Chips/.Mult progressively as the
            // sequence plays, rather than only computing them at the very
            // end — chipsTotal * multTotal always equals the placement's
            // own subtotal so far (displayedRoundScore - roundScoreBefore),
            // same invariant as Chips * Mult == TotalScore in Core. multTotal
            // is a float, not an int, since Mult itself is now (progressive
            // modifiers keep full precision — see PlacementResult.Mult); a
            // final sync below guards against any float-rounding drift
            // across the individual catch-up steps.
            int chipsTotal = 0;
            float multTotal = 1f;
            _comboView.Show(0, 1f);
            // Multiplies every stagger wait below — starts at 1 (full pace)
            // and shrinks by ComboSpeedupFactor after each combo addition,
            // shared across score events, line clears AND the multiplier
            // catch-up, so the whole sequence accelerates together as one
            // continuous combo rather than each section restarting at full pace.
            float staggerSpeed = 1f;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type == ScoreEventType.LineClear)
                {
                    continue; // played below, synced with each cell's visual clear
                }

                if (scoreEvent.Type == ScoreEventType.ModifierMultiplier || scoreEvent.Type == ScoreEventType.MultBonus)
                {
                    // Every Mult-contributing event (both the "xN" and the
                    // "+N Mult" families) is handled together, AFTER this
                    // loop, strictly in modifier-index order — not here in
                    // whatever order GridManager happened to compute them
                    // (pre-clear pass, then post-clear pass, then
                    // RunManager's hand-slot/deck-state ones) — see the
                    // ordered catch-up below and PlacementResult.Mult's own
                    // doc comment for why (on explicit request: "leur
                    // pointage se fasse par ordre d'index").
                    continue;
                }

                if (scoreEvent.Type == ScoreEventType.LueurBonus)
                {
                    // Lueur (not score) from one of the player's active
                    // modifiers (see PlacementResult.ModifierLueurBonus) — a
                    // second, independent source of the same currency as the
                    // LueurGroups loop above, so it reuses the exact same
                    // flying-popup-to-the-Lueur-label visual, plus the usual
                    // badge pulse every other modifier event gets.
                    if (scoreEvent.TriggeringModifier.HasValue)
                    {
                        var badgeAnchor = _modifierPanelView.GetBadgeTransform(scoreEvent.TriggeringModifier.Value, scoreEvent.TriggeringModifierIndex)
                            ?? _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                        _feedbackLayer.SpawnFlyingPopup(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + scoreEvent.Amount, VisualDefaults.GoldenColor);
                        _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                    }
                    displayedLueur += scoreEvent.Amount;
                    _hudView.SetLueur(displayedLueur);
                    yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                    staggerSpeed *= ComboSpeedupFactor;
                    continue;
                }

                // The tile(s) that actually earned this event's points always
                // pulse — for a Modifier event this is on top of the badge
                // pulse below, not instead of it (on explicit request). A
                // per-cell modifier (Forteresse, Carrefour, etc.) gets one
                // ScoreEvent per qualifying cell, each with its own Position,
                // so this naturally pulses every one of them in turn as the
                // sequence plays; a flat-bonus modifier (Prisme, Devotion,
                // etc.) only ever has the one representative cell to pulse.
                _gridView.PulseCell(scoreEvent.Position.x, scoreEvent.Position.y);

                RectTransform anchor;
                if (scoreEvent.Type == ScoreEventType.Modifier && scoreEvent.TriggeringModifier.HasValue)
                {
                    // Popup shows on the modifier's own badge instead of the
                    // tile (on explicit request) — falls back to the tile if
                    // the badge can't be found for some reason.
                    anchor = _modifierPanelView.GetBadgeTransform(scoreEvent.TriggeringModifier.Value, scoreEvent.TriggeringModifierIndex)
                        ?? _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                    _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                }
                else
                {
                    anchor = _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                }

                // A Modifier event is always a POINTS bonus (see
                // PlacementResult.ModifierBonus) — blue like every other
                // points popup, never red (on explicit report: "+100 pts du
                // dweling et le texte est apparu rouge... c'est par rapport
                // au points et non au mult"). ModifierMultiplier/MultBonus
                // events (genuinely Mult) get their own red popups on their
                // own badge, handled earlier in this loop, not here.
                Color color = scoreEvent.Type == ScoreEventType.Golden ? VisualDefaults.GoldenColor
                    : scoreEvent.Type == ScoreEventType.Modifier ? UITheme.ButtonSelected
                    : scoreEvent.Type == ScoreEventType.Trait ? UITheme.PanelLight
                    : scoreEvent.Type == ScoreEventType.Bastion ? UITheme.Success
                    : UITheme.TextPrimary;
                _feedbackLayer.SpawnPopup(anchor, "+" + scoreEvent.Amount, color);

                displayedRoundScore += scoreEvent.Amount;
                chipsTotal += scoreEvent.Amount;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                var anchor = _gridView.GetCellTransform(pos.x, pos.y);
                _feedbackLayer.SpawnPopup(anchor, "+" + ScoringConstants.LineClearBonusPerCell, UITheme.Success);
                _gridView.PulseCell(pos.x, pos.y);
                _gridView.ClearCellVisual(pos.x, pos.y);

                displayedRoundScore += ScoringConstants.LineClearBonusPerCell;
                chipsTotal += ScoringConstants.LineClearBonusPerCell;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, LineClearStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            // GroupMultiplier (Tinted+Multiplier-Zone cells) and LineClearMultiplier
            // (Multiplier-Zone cells only — see PlacementResult.LineClearMultiplier
            // for why Tinted stops short of the line-clear bonus) are each applied
            // once over their own share of the placement's total, Balatro-style,
            // rather than inflating each individual popup above — so the "extra"
            // they add still needs its own catch-up moment here or the displayed
            // score would end up short of placement.TotalScore.
            int multipliedExtra = (placement.GroupBonus + placement.GoldenBonus) * (placement.GroupMultiplier - 1)
                + placement.LineClearScore * (placement.LineClearMultiplier - 1);
            if (multipliedExtra > 0)
            {
                var centerAnchor = _gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2);
                // GroupMultiplier is always >= LineClearMultiplier (multiplier-zone
                // cells count toward both, Tinted only toward GroupMultiplier), so
                // it's the more informative single label even when the two differ.
                _feedbackLayer.SpawnPopup(centerAnchor, "x" + Mathf.Max(placement.GroupMultiplier, placement.LineClearMultiplier), UITheme.ButtonSelected);

                // GroupMultiplier/LineClearMultiplier land on the CHIPS side
                // of the Balatro-style split (see PlacementResult.Chips),
                // not the red mult pill — they're baked per-cell into a
                // group/line-clear term rather than a placement-wide factor
                // like ModifierMultiplier/ComboMultiplier below.
                displayedRoundScore += multipliedExtra;
                chipsTotal += multipliedExtra;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseChips();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
            }

            // Every Mult-contributing modifier (the "+N Mult" family —
            // MultUn/Deux/Quatre, Risky Mult, Solidarité, Enchanted Cards,
            // Experience — AND the "xN" family — Prisme, Devotion*, the
            // line-pattern family, Combo, Slot Loyalty, Densité, ...) now
            // catches up in ONE pass, strictly in modifier-index order
            // (mirrors PlacementResult.Mult's own ordered fold exactly — on
            // explicit request: "leur pointage se fasse par ordre d'index.
            // Le premier acheté est le premier index" + the follow-up
            // clarifying it's a strict left-to-right fold, not PEMDAS).
            // Each modifier already flashed its own "xN"/"+N" popup on its
            // badge back in the main event loop above — this only handles
            // the actual score catch-up, one step per modifier, in the
            // exact order that determines the final total, so a player who
            // put their xN AFTER their +N modifiers visibly sees the bigger
            // jump happen in that same order.
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

                float subtotalSoFar = displayedRoundScore - roundScoreBefore;
                int extra = Mathf.RoundToInt(subtotalSoFar * (multTotal / multBefore - 1f));
                displayedRoundScore += extra;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(chipsTotal, multTotal);
                _comboView.PulseMult();

                yield return new WaitForSeconds(Mathf.Max(MinStaggerSeconds, ScoreEventStaggerSeconds * staggerSpeed));
                staggerSpeed *= ComboSpeedupFactor;
            }

            // "Combo" (see PlacementResult.ComboMultiplier) is now just
            // another ModifierMultiplier event in the loop above (see
            // GridManager.ComputeComboMultiplier), so it no longer needs
            // its own separate catch-up here — but it keeps its distinct
            // "COMBO xN" center-screen callout, on top of the ordinary
            // per-badge popup every other Mult modifier gets, since it's
            // reacting to the ROUND's streak state rather than a modifier
            // condition on this one placement.
            if (placement.ComboMultiplier > 1)
            {
                var centerAnchor = _gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2);
                _feedbackLayer.SpawnPopup(centerAnchor, "COMBO x" + placement.ComboMultiplier, UITheme.Success);
            }

            // Force-sync to the authoritative total — a no-op whenever
            // nothing progressive fired (the catch-ups above stayed exact
            // integer math, as before), but guards against the float
            // rounding in the two catch-ups above ever drifting the
            // displayed running total away from placement.TotalScore by a
            // point or two.
            if (displayedRoundScore != roundScoreBefore + placement.TotalScore)
            {
                displayedRoundScore = roundScoreBefore + placement.TotalScore;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
            }

            if (outcome.BossLockedCells.Count > 0)
            {
                // The boss just locked more cells (see RunConfig.BossLockPiecesInterval)
                // outside of anything this sequence already animated above —
                // a full refresh is the simplest way to surface them (and any
                // Bastion cell they might have grazed) without a bespoke
                // per-cell lock animation.
                _gridView.Refresh();
            }

            _isPlayingPlacementSequence = false;
            _handView.SetInteractable(true);
            _modifierPanelView.SetInteractable(true);
            HandleStateTransition(outcome.StateAfter);
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
            switch (state)
            {
                case RunState.AwaitingShop:
                    _shopView.Show(_run);
                    break;

                case RunState.RunVictory:
                {
                    bool isNewBestScore = RecordRunOutcome(victory: true, roundReached: RunConfig.RoundCount);
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
        /// Meta-progression (spec extension, explicit request: "enchaînons
        /// sur la meta progression" -> lightweight option chosen: stats/
        /// best-score tracking only, no gameplay effect). Folds this run's
        /// outcome into the persisted MetaStats and saves immediately —
        /// same "commit right away, don't wait for a graceful shutdown"
        /// reasoning as every other piece of run state, since there's no
        /// guaranteed exit hook in a WebGL/browser build. Returns whether
        /// this run's score is a new all-time best, for the end screen's
        /// "new record" callout.
        /// </summary>
        private bool RecordRunOutcome(bool victory, int roundReached)
        {
            bool isNewBestScore = _run.TotalScore > _metaStats.BestScore;
            _metaStats = MetaStatsRecorder.RecordRunOutcome(_metaStats, _run.TotalScore, roundReached, victory);
            _metaStatsStore.Save(_metaStats);
            return isNewBestScore;
        }

        // ---- Lueur shop (spec extension, explicit request — replaces the
        // old draft/modifier-pick screens entirely) ----

        private void OnModifierBuyRequested(int index)
        {
            _run.BuyModifierSlot(index);
            RefreshAll();
            _shopView.Refresh(_run);
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
            RefreshAll();
            _shopView.Refresh(_run);

            var pending = _run.PendingUpgrade;
            if (pending == null)
            {
                // Bank upgrade with no sub-choice (Joker) — already applied;
                // still show the reveal card (plus a preview of the actual
                // piece added, see RunManager.LastJokerShapeAdded) so the
                // player can see what it was.
                _upgradeRevealView.Show(revealedUpgrade, _run.LastJokerShapeAdded, PieceColor.Joker);
                return;
            }
            if (pending.Pool == UpgradePool.Grid)
            {
                _tileChoiceView.Show(_run.Deck, _run.PendingUpgradeTileCandidates, EconomyConstants.ShopTileChoiceCount, pending);
            }
            else
            {
                _draftView.ShowForPendingUpgrade(pending, _run.PendingUpgradeTypeCandidates);
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
            SetStatusText(_run.IsBossRound
                ? "Boss round: every " + RunConfig.BossLockPiecesInterval + " pieces played, the boss locks " + RunConfig.BossLockCellsPerInterval + " more cells."
                : "New round: select a piece, then click the grid.");
        }

        private void OnRestartRequested()
        {
            _isPlayingPlacementSequence = false;
            _endScreenView.Hide();
            _run = new RunManager(new SystemRandomProvider());
            _gridView.Rebind(_run.Grid);
            _handView.Rebind(_run.Deck);
            _draftView.Rebind(_run.Deck);
            _tileChoiceView.Rebind(_run.Deck);
            _deckView.Rebind(_run.Deck);
            _deckView.Hide();
            RefreshAll();
            SetStatusText(IdleStatusMessage);
        }

        private void RefreshAll()
        {
            _gridView.Refresh();
            _handView.Refresh();
            _hudView.Refresh(_run);
            _comboView.Hide();
            _modifierPanelView.Refresh(_run.ActiveModifiers);
        }
    }
}
