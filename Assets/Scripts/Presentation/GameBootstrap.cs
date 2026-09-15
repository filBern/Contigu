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
        private DraftView _draftView;
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

            _hudView = gameObject.AddComponent<HudView>();
            var hudRect = _hudView.Build(mainRoot);
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.sizeDelta = new Vector2(0f, 56f);
            hudRect.anchoredPosition = Vector2.zero;

            _statusText = UIFactory.CreateText(mainRoot, "Status", "Sélectionnez une pièce puis cliquez sur la grille.", 16, UITheme.TextMuted);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            _statusText.rectTransform.sizeDelta = new Vector2(700f, 26f);

            _gridView = gameObject.AddComponent<GridView>();
            var gridRect = _gridView.Build(mainRoot, _run.Grid, CellSize);
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0f, -110f);

            _handView = gameObject.AddComponent<HandView>();
            var handRect = _handView.Build(mainRoot, _run.Deck);
            handRect.anchorMin = new Vector2(0.5f, 0f);
            handRect.anchorMax = new Vector2(0.5f, 0f);
            handRect.pivot = new Vector2(0.5f, 0f);
            handRect.anchoredPosition = new Vector2(0f, 24f);

            _feedbackLayer = gameObject.AddComponent<FeedbackLayer>();
            _feedbackLayer.Build(mainRoot);

            _draftView = gameObject.AddComponent<DraftView>();
            _draftView.Build(mainRoot, _run.Deck);

            _endScreenView = gameObject.AddComponent<EndScreenView>();
            _endScreenView.Build(mainRoot);
        }

        private void WireEvents()
        {
            _gridView.CellClicked += OnCellClicked;
            _handView.SlotSelected += OnHandSlotSelected;
            _draftView.UpgradeConfirmed += OnUpgradeConfirmed;
            _endScreenView.RestartRequested += OnRestartRequested;
        }

        private void OnHandSlotSelected(int handIndex)
        {
            var token = _run.Deck.Hand[handIndex];
            var shape = PieceShapeCatalog.Get(token.Shape);
            _gridView.SetSelectedShape(shape);
            _statusText.text = "Cliquez sur la grille pour poser : " + VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")";
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
                _statusText.text = "Sélectionnez d'abord une pièce dans la main.";
                return;
            }

            var outcome = _run.PlacePiece(handIndex, x, y);
            if (!outcome.Placement.Success)
            {
                _statusText.text = "Placement invalide à cet endroit.";
                return;
            }

            _gridView.SetSelectedShape(null);
            _handView.ClearSelection();

            // Hold any completed line/column visually filled (instead of
            // instantly vanishing) while its score is still playing out.
            _gridView.RefreshHoldingClearedCells(outcome.Placement.ClearedCells, outcome.Placement.ClearedCellColors);
            _handView.Refresh();
            _hudView.Refresh(_run);
            _statusText.text = "Sélectionnez une pièce puis cliquez sur la grille.";

            _isPlayingPlacementSequence = true;
            StartCoroutine(PlayPlacementSequence(outcome));
        }

        /// <summary>
        /// Plays a placement's full feedback sequence in order: the golden/group
        /// score popups first, then — only once that's done — clears any
        /// completed line/column one cell at a time (each with its own popup),
        /// and only then advances the run state (draft/victory/defeat), so
        /// nothing interrupts the player while they're still reading their score.
        /// </summary>
        private System.Collections.IEnumerator PlayPlacementSequence(PlacementOutcome outcome)
        {
            var placement = outcome.Placement;

            var immediateEvents = new System.Collections.Generic.List<ScoreEvent>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                if (placement.ScoreEvents[i].Type != ScoreEventType.LineClear)
                {
                    immediateEvents.Add(placement.ScoreEvents[i]);
                }
            }

            PlayScoreEventSequence(immediateEvents);

            if (immediateEvents.Count > 0)
            {
                float immediatePhaseDuration = (immediateEvents.Count - 1) * ScoreEventStaggerSeconds + FeedbackLayer.PopupDurationSeconds;
                yield return new WaitForSeconds(immediatePhaseDuration);
            }

            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                var anchor = _gridView.GetCellTransform(pos.x, pos.y);
                _feedbackLayer.SpawnPopup(anchor, "+" + ScoringConstants.LineClearBonusPerCell + " ligne", UITheme.Success);
                _gridView.ClearCellVisual(pos.x, pos.y);
                yield return new WaitForSeconds(LineClearStaggerSeconds);
            }

            _isPlayingPlacementSequence = false;
            HandleStateTransition(outcome.StateAfter);
        }

        /// <summary>
        /// Plays each score contribution as its own staggered "+X" popup at the
        /// cell it came from, instead of one lump total per placement, so the
        /// player can see where the points actually came from.
        /// </summary>
        private void PlayScoreEventSequence(System.Collections.Generic.IReadOnlyList<ScoreEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var scoreEvent = events[i];
                var anchor = _gridView.GetCellTransform(scoreEvent.Position.x, scoreEvent.Position.y);

                Color color;
                string label;
                switch (scoreEvent.Type)
                {
                    case ScoreEventType.Golden:
                        color = VisualDefaults.GoldenColor;
                        label = "+" + scoreEvent.Amount;
                        break;
                    case ScoreEventType.Group:
                    default:
                        color = UITheme.TextPrimary;
                        label = "+" + scoreEvent.Amount;
                        break;
                }

                _feedbackLayer.SpawnPopupDelayed(anchor, label, color, i * ScoreEventStaggerSeconds);
            }
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

        private void OnUpgradeConfirmed(UpgradeDefinition def, UpgradeSubChoice sub)
        {
            _run.ApplyUpgradeAndAdvance(def, sub);
            RefreshAll();
            _statusText.text = _run.IsBossRound
                ? "Manche boss : la grille gelée verrouille 14 cases."
                : "Nouvelle manche : sélectionnez une pièce puis cliquez sur la grille.";
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
            _statusText.text = "Sélectionnez une pièce puis cliquez sur la grille.";
        }

        private void RefreshAll()
        {
            _gridView.Refresh();
            _handView.Refresh();
            _hudView.Refresh(_run);
        }
    }
}
