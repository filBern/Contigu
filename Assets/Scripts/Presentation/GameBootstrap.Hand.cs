using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Hand slot selection, the Shuffle button and the manual shuffle animation.</summary>
    public sealed partial class GameBootstrap
    {
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
            SetStatusText("Drag onto the grid, or click a sprout, to place: " + VisualDefaults.GetShapeName(token.Shape) + " (" + VisualDefaults.GetColorName(token.Color) + ")");
        }

        /// <summary>HandView refused to pick up this slot's piece because it has no valid placement anywhere (the slot itself already played its shake, see HandView.ShakeSlot). Shows an emphasized status message and pulses Shuffle.</summary>
        private void OnHandSlotUnplayable(int handIndex)
        {
            // Only hint at Shuffle when nothing in the hand can be played; another piece may still have a spot.
            if (_handView.IsHandStuck)
            {
                _handView.SetShufflePulsing(true);
                SetStatusText("This seed can't be placed anywhere — try shuffling your hand.", emphasize: true);
            }
            else
            {
                SetStatusText("This seed can't be placed anywhere — try another one.", emphasize: true);
            }
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

            string blightBefore = BlightSignature();
            bool shuffled = _run.ShuffleHand();
            if (shuffled)
            {
                PlayBlightmothActIfBlightChanged(blightBefore);
                // The resident enemy's grid change stays hidden until the hand is back, then plays (see below).
                var worldChange = _run.LastWorldGridChange;
                if (worldChange.HasValue)
                {
                    _gridView.HoldWorldChange(worldChange.Value);
                }
            }
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
            // screen even though the model already updated. Every currently
            // locked/poisoned cell just got (re-)placed by this very Shuffle
            // (the effect always re-picks, even onto the same cell), so
            // each one gets its own circle flying in from its owning
            // enemy's icon before the cell is allowed to actually switch to
            // its locked/poisoned look — this refresh suppresses just those
            // cells (they keep showing whatever they looked like before the
            // Shuffle) so the tile doesn't go instantly bare/grey the
            // moment the model updates, well before the circle arrives.
            SnapshotEnemyEffectCells(out var lockedCellsNow, out var poisonedCellsNow);
            var affectedCellsNow = new List<Vector2Int>(lockedCellsNow);
            affectedCellsNow.AddRange(poisonedCellsNow);
            _gridView.RefreshHoldingClearedCells(new List<Vector2Int>(), new List<PieceColor>(), new List<PieceTrait?>(), affectedCellsNow);
            // Zeroed in the same frame as the refresh above, before anything
            // yields — ApplyState always paints malus opaque, so without
            // this it would flash at full alpha before FadeMalus resets it
            // to 0. A no-op for the suppressed cells above, which aren't
            // flagged as showing malus yet.
            _gridView.SetMalusAlpha(0f);

            // Heatwave: the tile this Shuffle scorched stays on screen (held) until its flames have played.
            var burned = _run.LastBurnedTile;
            if (burned.HasValue)
            {
                _gridView.RefreshHoldingClearedCells(
                    new List<Vector2Int> { burned.Value.Pos }, new List<PieceColor> { burned.Value.Color }, new List<PieceTrait?> { burned.Value.Trait }, affectedCellsNow);
                _gridView.SetMalusAlpha(0f);
                ShakeScreen(1, 8);
                _hudView.PulseWeather();
                _gridView.PlayBurn(burned.Value.Pos.x, burned.Value.Pos.y);
                yield return new WaitForSeconds(0.95f);
                _gridView.RefreshHoldingClearedCells(new List<Vector2Int>(), new List<PieceColor>(), new List<PieceTrait?>(), affectedCellsNow);
                _gridView.SetMalusAlpha(0f);
            }

            yield return _handView.FadeSlotPieces(0f, 1f, ShuffleFadeDuration);
            PlayThiefStealEffect();
            // Reveals each affected enemy's own cell(s) one enemy at a time — icon pulse, circle, reveal —
            // instead of every enemy firing in parallel (see PlayEnemyEffectsSequentially).
            yield return PlayEnemyEffectsSequentially(affectedCellsNow);

            PlayWorldEnemyActIfAny();
            var pendingWorld = _run.LastWorldGridChange;
            if (pendingWorld.HasValue)
            {
                yield return _gridView.PlayWorldGridChange(pendingWorld.Value);
            }

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
    }
}
