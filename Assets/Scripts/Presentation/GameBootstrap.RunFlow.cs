using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Run state transitions, status line, restart and challenge start.</summary>
    public sealed partial class GameBootstrap
    {
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
                _feedbackLayer.SpawnPollenFlight(fromAnchor.position, _hudView.LueurLabelTransform, "+1", VisualDefaults.GoldenColor);
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
                    status = "Boss round: every " + _run.Challenge.BossLockPiecesInterval + " seeds, " + _run.Challenge.BossLockCellsPerInterval + " cell(s) will lock.";
                    break;
                case BossEffect.LockedHandSlot:
                    status = "Boss round: hand slot " + (_run.BossLockedHandSlotIndex.Value + 1) + " is locked for this round.";
                    break;
                case BossEffect.CursedColor:
                    status = VisualDefaults.GetColorName(_run.BossCursedColor.Value) + " seeds score 0 points this round.";
                    break;
                default:
                    status = _run.Challenge.BossActiveEveryRound
                        ? "Wildgrowth: every " + _run.Challenge.BossLockPiecesInterval + " seeds, " + _run.Challenge.BossLockCellsPerInterval + " cell(s) will lock."
                        : "New round: select a seed, then click the grid.";
                    break;
            }
            if (_run.Challenge.BossActiveEveryRound && _run.CurrentBossEffect != BossEffect.None && _run.CurrentBossEffect != BossEffect.ProgressiveCellLock)
            {
                status += " Wildgrowth also locks a cell every " + _run.Challenge.BossLockPiecesInterval + " seeds.";
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
                StartCoroutine(RunSprigGuideAfterDelay());
            }
            else
            {
                StartCoroutine(SprigRunStartComment());
            }
        }

        private System.Collections.IEnumerator RunSprigGuideAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            _sprigView.RunGuide(BuildSprigGuide());
        }

        private System.Collections.IEnumerator SprigRunStartComment()
        {
            yield return new WaitForSeconds(1.2f);
            _sprigView.Say(SprigView.RunStartLine(), SprigMood.Happy, 5f);
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
