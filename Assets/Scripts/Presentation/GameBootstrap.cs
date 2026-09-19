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
        private const float CellSize = 54f;
        private const float ScoreEventStaggerSeconds = 0.22f;
        private const float LineClearStaggerSeconds = 0.14f;

        private RunManager _run;

        private GridView _gridView;
        private HandView _handView;
        private HudView _hudView;
        private ComboView _comboView;
        private DraftView _draftView;
        private ModifierDraftView _modifierDraftView;
        private ModifierPanelView _modifierPanelView;
        private TooltipView _tooltipView;
        private EndScreenView _endScreenView;
        private FeedbackLayer _feedbackLayer;
        private Text _statusText;
        private bool _isPlayingPlacementSequence;

        private void Awake()
        {
            EnsureEventSystem();
            var canvasRect = BuildCanvas();

            _run = new RunManager(new SystemRandomProvider());

            BuildUI(canvasRect);
            WireEvents();
            RefreshAll();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                DebugForceRoundWin();
            }
        }

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
#endif

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

            _statusText = UIFactory.CreateText(mainRoot, "Status", "Select or drag a piece onto the grid.", 16, UITheme.TextMuted);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Below the top bar (68 tall, see HudView.BarHeight) with a 12px gap.
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            _statusText.rectTransform.sizeDelta = new Vector2(700f, 26f);

            // Dead center of the screen — the top/bottom progress bars and the
            // status text float above it rather than pushing it down, so the
            // grid itself isn't biased toward the top.
            _gridView = gameObject.AddComponent<GridView>();
            var gridRect = _gridView.Build(mainRoot, _run.Grid, CellSize);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;

            // Built before HandView since its trait badges need a live
            // TooltipView to hover — see below.
            _tooltipView = gameObject.AddComponent<TooltipView>();
            _tooltipView.Build(mainRoot);

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
            comboRect.sizeDelta = new Vector2(400f, 50f);

            _feedbackLayer = gameObject.AddComponent<FeedbackLayer>();
            _feedbackLayer.Build(mainRoot);

            _draftView = gameObject.AddComponent<DraftView>();
            _draftView.Build(mainRoot, _run.Deck, _tooltipView);

            _modifierDraftView = gameObject.AddComponent<ModifierDraftView>();
            _modifierDraftView.Build(mainRoot, _tooltipView);

            _modifierPanelView = gameObject.AddComponent<ModifierPanelView>();
            _modifierPanelView.Build(mainRoot, _tooltipView);

            _endScreenView = gameObject.AddComponent<EndScreenView>();
            _endScreenView.Build(mainRoot);
        }

        private void WireEvents()
        {
            _gridView.CellClicked += OnCellClicked;
            _handView.SlotSelected += OnHandSlotSelected;
            _draftView.UpgradeConfirmed += OnUpgradeConfirmed;
            _modifierDraftView.ModifierPicked += OnModifierPicked;
            _modifierDraftView.ModifierRemoved += OnModifierRemoved;
            _endScreenView.RestartRequested += OnRestartRequested;
        }

        private void OnHandSlotSelected(int handIndex)
        {
            var token = _run.Deck.Hand[handIndex];
            var rotation = _run.Deck.HandRotations[handIndex];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
            _gridView.SetSelectedShape(shape, token.Color, token.Trait);
            _statusText.text = "Drag onto the grid, or click a tile, to place: " + VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")";
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
            if (handIndex < 0 || handIndex >= _run.Deck.Hand.Count)
            {
                _statusText.text = "Select a piece from your hand first.";
                return;
            }

            int roundScoreBefore = _run.RoundScore;

            var outcome = _run.PlacePiece(handIndex, x, y);
            if (!outcome.Placement.Success)
            {
                _statusText.text = "Invalid placement there.";
                return;
            }

            _gridView.SetSelectedShape(null);
            _handView.ClearSelection();

            // Hold any completed line/column visually filled (instead of
            // instantly vanishing) while its score is still playing out.
            _gridView.RefreshHoldingClearedCells(outcome.Placement.ClearedCells, outcome.Placement.ClearedCellColors);
            _handView.Refresh();
            // Round/budget update immediately; the score numbers themselves stay
            // at their pre-placement values until PlayPlacementSequence catches
            // them up in step with each popup.
            _hudView.Refresh(_run);
            _hudView.SetScores(roundScoreBefore, _run.CurrentQuota);
            _statusText.text = "Select or drag a piece onto the grid.";

            _isPlayingPlacementSequence = true;
            _handView.SetInteractable(false);
            StartCoroutine(PlayPlacementSequence(outcome, roundScoreBefore));
        }

        /// <summary>
        /// Plays a placement's full feedback sequence in order: each golden/
        /// group score popup one at a time — pulsing its cell and advancing the
        /// HUD's score bar at that exact moment, so the displayed score climbs
        /// progressively instead of jumping straight to the final value — then,
        /// only once that's done, clears any completed line/column one cell at
        /// a time (each with its own popup and score bump), and only then
        /// advances the run state (draft/victory/defeat), so nothing interrupts
        /// the player while they're still reading their score.
        /// </summary>
        private System.Collections.IEnumerator PlayPlacementSequence(PlacementOutcome outcome, int roundScoreBefore)
        {
            var placement = outcome.Placement;
            int displayedRoundScore = roundScoreBefore;
            int comboTotal = 0;
            _comboView.Show(0);

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type == ScoreEventType.LineClear)
                {
                    continue; // played below, synced with each cell's visual clear
                }

                var anchor = _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);
                Color color = scoreEvent.Type == ScoreEventType.Golden ? VisualDefaults.GoldenColor
                    : scoreEvent.Type == ScoreEventType.Modifier ? UITheme.Modifier
                    : scoreEvent.Type == ScoreEventType.Trait ? UITheme.PanelLight
                    : UITheme.TextPrimary;
                _feedbackLayer.SpawnPopup(anchor, "+" + scoreEvent.Amount, color);
                _gridView.PulseCell(scoreEvent.Position.x, scoreEvent.Position.y);
                if (scoreEvent.Type == ScoreEventType.Modifier && scoreEvent.TriggeringModifier.HasValue)
                {
                    _modifierPanelView.Pulse(scoreEvent.TriggeringModifier.Value);
                }

                displayedRoundScore += scoreEvent.Amount;
                comboTotal += scoreEvent.Amount;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(comboTotal);

                yield return new WaitForSeconds(ScoreEventStaggerSeconds);
            }

            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                var anchor = _gridView.GetCellTransform(pos.x, pos.y);
                _feedbackLayer.SpawnPopup(anchor, "+" + ScoringConstants.LineClearBonusPerCell, UITheme.Success);
                _gridView.PulseCell(pos.x, pos.y);
                _gridView.ClearCellVisual(pos.x, pos.y);

                displayedRoundScore += ScoringConstants.LineClearBonusPerCell;
                comboTotal += ScoringConstants.LineClearBonusPerCell;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
                _comboView.Show(comboTotal);

                yield return new WaitForSeconds(LineClearStaggerSeconds);
            }

            _isPlayingPlacementSequence = false;
            _handView.SetInteractable(true);
            HandleStateTransition(outcome.StateAfter);
        }

        private void HandleStateTransition(RunState state)
        {
            switch (state)
            {
                case RunState.AwaitingDraft:
                    var draft = _run.RollDraftOptions();
                    _draftView.Show(draft);
                    break;

                case RunState.RunVictory:
                    _endScreenView.ShowVictory(_run.TotalScore);
                    break;

                case RunState.RunDefeat:
                    _endScreenView.ShowDefeat(_run.CurrentRoundNumber, _run.TotalScore);
                    break;
            }
        }

        private void OnUpgradeConfirmed(UpgradeDefinition upgrade, UpgradeSubChoice subChoice)
        {
            _run.ApplyUpgradeAndAdvance(upgrade, subChoice);
            // State is now AwaitingModifierPick — refresh so any golden/tinted/
            // multiplier cells the upgrade just added are visible right away,
            // then offer the modifier draft next.
            RefreshAll();
            var modifierOptions = _run.RollModifierDraftOptions();
            _modifierDraftView.ShowPick(modifierOptions);
        }

        private void OnModifierPicked(ModifierId modifierId)
        {
            _run.ApplyModifierPick(modifierId);
            if (_run.State == RunState.AwaitingModifierRemoval)
            {
                _modifierPanelView.Refresh(_run.ActiveModifiers);
                _modifierDraftView.ShowRemoval(_run.ActiveModifiers);
                return;
            }

            FinishModifierFlowAndAdvance();
        }

        private void OnModifierRemoved(ModifierId modifierId)
        {
            _run.RemoveModifierAndAdvance(modifierId);
            FinishModifierFlowAndAdvance();
        }

        private void FinishModifierFlowAndAdvance()
        {
            RefreshAll();
            _statusText.text = _run.IsBossRound
                ? "Boss round: the frozen grid locks 14 cells."
                : "New round: select a piece, then click the grid.";
        }

        private void OnRestartRequested()
        {
            _isPlayingPlacementSequence = false;
            _endScreenView.Hide();
            _run = new RunManager(new SystemRandomProvider());
            _gridView.Rebind(_run.Grid);
            _handView.Rebind(_run.Deck);
            _draftView.Rebind(_run.Deck);
            RefreshAll();
            _statusText.text = "Select or drag a piece onto the grid.";
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
