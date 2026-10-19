using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Placing a piece: the click handler and the full score/damage feedback sequence that follows it.</summary>
    public sealed partial class GameBootstrap
    {
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
            if (handIndex < 0 || handIndex >= _run.Deck.Hand.Count || !_run.Deck.Hand[handIndex].HasValue)
            {
                SetStatusText("Select a seed from your hand first.");
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
            // DamageTargetPredictor.FindDamagedEnemyIndices, which mirrors RunManager.
            // ApplyJokerCombatOrDefaultDamage's targeting read-only. Only
            // index 0 gets the smooth drain below; any further index is a
            // Bombe/Éclat candidate snapped to its final value by
            // DrainSecondaryEnemyHits.
            var damagedEnemyIndices = DamageTargetPredictor.FindDamagedEnemyIndices(_run, handIndex, x, y, out var damageCombatKind);
            // Range/Précision pick a different enemy than the usual front one — a visual cue (see
            // FeedbackLayer.SpawnImpactLine) makes that redirection actually readable instead of the player
            // having to notice which HP bar moved. Bombe/Éclat's spread already reads as "everyone" without one.
            bool isRetargetHit = damageCombatKind == PieceTraitKind.Range || damageCombatKind == PieceTraitKind.Precision;
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
            bool targetWasShielded = damagedEnemyIndex >= 0 && _run.CurrentEncounter[damagedEnemyIndex].HasShield;

            string blightBefore = BlightSignature();
            int weatherBonusesBefore = _run.WeatherBonusTriggerCount;
            var outcome = _run.PlacePiece(handIndex, x, y);
            if (_run.WeatherBonusTriggerCount != weatherBonusesBefore)
            {
                _hudView.PulseWeather();
            }
            if (!outcome.Placement.Success)
            {
                SfxManager.Play(SfxId.InvalidDrop);
                SetStatusText("Invalid placement there.");
                return;
            }
            SfxManager.Play(SfxId.ValidDrop);
            PlayBlightmothActIfBlightChanged(blightBefore);

            // Only valid right after a call that ran DrawFreshHand this
            // placement — otherwise ThiefStoleOnLastShuffle still holds a
            // stale value from an earlier, unrelated Shuffle.
            bool thiefStoleThisPlacement = handWasAboutToAutoRefill && _run.ThiefStoleOnLastShuffle;

            // A spent shield stays drawn until the damage drain reaches it, then shatters (see BreakPendingShield).
            _pendingShieldBreakIndex = targetWasShielded && !_run.CurrentEncounter[damagedEnemyIndex].HasShield ? damagedEnemyIndex : -1;
            if (_pendingShieldBreakIndex >= 0)
            {
                _hudView.HoldShield(_pendingShieldBreakIndex);
            }

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
            var effectDiff = EnemyEffectCellDiff.Compute(lockedCellsBefore, poisonedCellsBefore, lockedCellsAfter, poisonedCellsAfter, outcome.BossLockedCells);
            var deferredNewMalusCells = effectDiff.NewMalusCells;
            var deferredReleasedLockedCells = effectDiff.ReleasedLockedCells;
            var deferredReleasedPoisonedCells = effectDiff.ReleasedPoisonedCells;
            bool hasDeferredGridChange = effectDiff.HasChange;

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
            // A frozen plot broken free by the line has no tile to hold: it keeps showing its ice until it shatters.
            var heldCells = new List<Vector2Int>();
            var heldColors = new List<PieceColor>();
            var heldTraits = new List<PieceTrait?>();
            var heldFrozenCells = new List<Vector2Int>();
            for (int i = 0; i < outcome.Placement.ClearedCells.Count; i++)
            {
                if (IsClearedCellFrozen(outcome.Placement, i))
                {
                    heldFrozenCells.Add(outcome.Placement.ClearedCells[i]);
                    continue;
                }
                heldCells.Add(outcome.Placement.ClearedCells[i]);
                heldColors.Add(outcome.Placement.ClearedCellColors[i]);
                heldTraits.Add(outcome.Placement.ClearedCellTraits[i]);
            }
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
            // Hurricane: the blown tiles keep showing at their old plots (held) and are drawn as empty plots at their new ones until
            // the end of the sequence, when they actually fly across (see PlayWeatherTileFlights).
            var weatherHiddenCells = new List<Vector2Int>();
            for (int i = 0; i < outcome.WeatherMovedTiles.Count; i++)
            {
                var move = outcome.WeatherMovedTiles[i];
                if (!heldCells.Contains(move.To))
                {
                    weatherHiddenCells.Add(move.To);
                }
                heldCells.Add(move.From);
                heldColors.Add(move.Color);
                heldTraits.Add(null);
            }
            _gridView.SetFrozenHold(heldFrozenCells, true);
            _gridView.RefreshHoldingClearedCells(heldCells, heldColors, heldTraits, deferredNewMalusCells, deferredReleasedLockedCells, deferredReleasedPoisonedCells, heldFrozenCells);
            _gridView.ShowCellsAsEmpty(weatherHiddenCells);
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
            StartCoroutine(PlayPlacementSequence(outcome, roundScoreBefore, lueurBefore, damagedEnemyIndex, damagedEnemyId, enemyHpBefore, enemyHpAfter, enemyMaxHp, isRetargetHit, hasDeferredGridChange, deferredNewMalusCells, deferredReleasedLockedCells, deferredReleasedPoisonedCells, thiefStoleThisPlacement, handWasAboutToAutoRefill, leechIndex, leechHealAmount, secondaryDrainTargets));
        }

        /// <summary>True when playing the hand slot at <paramref name="handIndex"/> is about to leave every slot empty, which RunManager.PlacePiece auto-refills (and resolves enemy Shuffle effects for) synchronously before returning — see OnCellClicked's own snapshot comment.</summary>
        /// <summary>A brief near-freeze on a killing blow, restored by the finally block (and OnDisable) so time can never stay slowed.</summary>
        private System.Collections.IEnumerator PlayHitStop(float realSeconds)
        {
            try
            {
                Time.timeScale = 0.06f;
                yield return new WaitForSecondsRealtime(realSeconds);
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        /// <summary>Camera shake: the whole interface jolts for a moment (stronger for a bigger share of the enemy's HP), restarting cleanly if another hit lands meanwhile.</summary>
        private void ShakeScreen(int damage, int enemyMaxHp)
        {
            float share = enemyMaxHp > 0 ? Mathf.Clamp01(damage * 3f / enemyMaxHp) : 0f;
            float magnitude = Mathf.Lerp(3.5f, 10f, share);
            float duration = Mathf.Lerp(0.16f, 0.3f, share);
            if (_screenShakeCoroutine != null)
            {
                StopCoroutine(_screenShakeCoroutine);
                _mainRoot.anchoredPosition = Vector2.zero;
            }
            _screenShakeCoroutine = StartCoroutine(ScreenShakeRoutine(duration, magnitude));
        }

        private System.Collections.IEnumerator ScreenShakeRoutine(float duration, float magnitude)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float damping = 1f - Mathf.Clamp01(t / duration);
                _mainRoot.anchoredPosition = Random.insideUnitCircle * magnitude * damping;
                yield return null;
            }
            _mainRoot.anchoredPosition = Vector2.zero;
            _screenShakeCoroutine = null;
        }

        private static List<Vector2Int> FrozenClearedCells(PlacementResult placement)
        {
            var cells = new List<Vector2Int>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                if (IsClearedCellFrozen(placement, i))
                {
                    cells.Add(placement.ClearedCells[i]);
                }
            }
            return cells;
        }

        private static bool IsClearedCellFrozen(PlacementResult placement, int index)
        {
            return index < placement.ClearedCellFrozen.Count && placement.ClearedCellFrozen[index];
        }

        private int _pendingShieldBreakIndex = -1;

        private void BreakPendingShield()
        {
            if (_pendingShieldBreakIndex >= 0)
            {
                _hudView.BreakShield(_pendingShieldBreakIndex);
                _pendingShieldBreakIndex = -1;
            }
        }

        private bool IsHandAboutToAutoRefill(int handIndex)
        {
            for (int i = 0; i < _run.Deck.Hand.Count; i++)
            {
                if (i != handIndex && _run.Deck.Hand[i].HasValue)
                {
                    return false;
                }
            }
            return true;
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
            int damagedEnemyIndex, EnemyId damagedEnemyId, int enemyHpBefore, int enemyHpAfter, int enemyMaxHp, bool isRetargetHit,
            bool hasDeferredGridChange, List<Vector2Int> deferredNewMalusCells, List<Vector2Int> deferredReleasedLockedCells, List<Vector2Int> deferredReleasedPoisonedCells,
            bool thiefStoleThisPlacement, bool handWasAboutToAutoRefill,
            int leechIndex, int leechHealAmount,
            List<(int Index, EnemyId Identity, int HpBefore, int HpAfter, int MaxHp)> secondaryDrainTargets)
        {
            // Wrapped in try/finally (deliberately not reindented below, to keep this fix to a reviewable
            // diff rather than touching ~450 lines of an already-long sequence): this sequence spans many
            // animation phases, indices and transforms, so one bad edge case throwing partway through used
            // to leave _isPlayingPlacementSequence stuck true and the hand's own fade-out alpha stuck at 0
            // forever — a permanent soft-lock (reported by playtesters as "placed the last piece of the
            // hand, nothing happened: no score countdown, no shuffle"). The finally below guarantees
            // input/hand visibility/state transition still happen even if a step above throws, instead of
            // only on a fully clean run.
            try
            {
            var placement = outcome.Placement;
            SfxManager.ResetComboPitch();
            _currentAttackSprite = ResolveAttackSprite(placement, out _currentPetalSprite);

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
                var cellPositions = new List<Vector3>();
                for (int c = 0; c < group.Cells.Count; c++)
                {
                    var cell = group.Cells[c];
                    _gridView.PulseCell(cell.x, cell.y);
                    var cellRect = _gridView.GetCellTransform(cell.x, cell.y);
                    if (cellRect != null)
                    {
                        centerSum += cellRect.position;
                        cellsWithTransform++;
                        cellPositions.Add(cellRect.position);
                    }
                }
                // Pollen motes sweep from every harvested cell to the counter, trailing golden specks.
                _feedbackLayer.SpawnPollenStream(cellPositions, _hudView.LueurLabelTransform);
                if (cellsWithTransform > 0)
                {
                    Vector3 center = centerSum / cellsWithTransform;
                    _feedbackLayer.SpawnPollenFlight(center, _hudView.LueurLabelTransform, "+" + group.Amount, VisualDefaults.GoldenColor);
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
                        _feedbackLayer.SpawnPollenFlight(badgeAnchor.position, _hudView.LueurLabelTransform, "+" + scoreEvent.Amount, VisualDefaults.GoldenColor);
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
                _feedbackLayer.SpawnPopup(anchor, (negative ? "" : "+") + displayAmount, color, fontSize: negative ? SubtractionPopupFontSize : FeedbackLayer.DefaultPopupFontSize);
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
                _feedbackLayer.SpawnPopup(anchor, (negative ? "" : "+") + amount, negative ? UITheme.Danger : UITheme.Success, fontSize: negative ? SubtractionPopupFontSize : FeedbackLayer.DefaultPopupFontSize);
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
                yield return PlayTileClearBursts(placement, leechIndex, leechHealAmount, deferredNewMalusCells, deferredReleasedLockedCells, deferredReleasedPoisonedCells);
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
                else if (scoreEvent.Type == ScoreEventType.ModifierMultiplier)
                {
                    // Not from a modifier: the whole grid was just cleared (see GridManager's GridClearMultiplier).
                    _feedbackLayer.SpawnPopup(_gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2), "CLEAN SWEEP x" + FormatMultAmount(amount), UITheme.Success, fontSize: 40);
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
                if (placement.ComboMultiplier >= BigComboShakeThreshold)
                {
                    _gridView.PlayShake(ComboShakeDuration, ComboShakeMagnitude);
                }
            }

            // Force-sync to the authoritative total — guards against float
            // rounding in the two catch-ups above drifting the displayed
            // total away from placement.TotalScore by a point or two.
            if (displayedRoundScore != roundScoreBefore + placement.TotalScore)
            {
                displayedRoundScore = roundScoreBefore + placement.TotalScore;
                _hudView.SetScores(displayedRoundScore, _run.CurrentQuota);
            }

            // Only once the combo total is finished, wait a beat so the
            // player can read the final number, then drain it down to 0
            // while transferring it into the targeted enemy's HP. Skipped
            // for a non-positive total (a placement scored entirely through
            // poison nets a heal, not damage — nothing positive to drain).
            if (damagedEnemyIndex >= 0 && placement.TotalScore > 0)
            {
                yield return new WaitForSeconds(PostComboReadDelaySeconds);
                yield return DrainComboIntoDamage(placement.TotalScore, placement.Chips, placement.Mult, damagedEnemyIndex, damagedEnemyId, enemyHpBefore, enemyHpAfter, enemyMaxHp, isRetargetHit);
                BreakPendingShield();
                // Bombe/Éclat's further targets snap straight to their final
                // value right after the primary target's smooth drain
                // finishes, rather than a second parallel lerp.
                yield return DrainSecondaryEnemyHits(secondaryDrainTargets);
            }

            // Sap Drain / Siphon convert the damage just dealt into Pollen inside RunManager without any score
            // event, so the counter only caught up on the next refresh. Fold the remainder in right here, as
            // part of this same count — a round-ending bonus is excluded, the shop sequence animates that one.
            int expectedLueur = _run.Lueur - (_run.State == RunState.AwaitingShop ? _run.LastRoundEndLueurBonus : 0);
            int siphoned = expectedLueur - displayedLueur;
            if (siphoned > 0)
            {
                var siphonAnchor = damagedEnemyIndex >= 0 ? _hudView.GetEnemyIconTransform(damagedEnemyIndex) : null;
                if (siphonAnchor != null)
                {
                    _feedbackLayer.SpawnPollenFlight(siphonAnchor.position, _hudView.LueurLabelTransform, "+" + siphoned, VisualDefaults.GoldenColor);
                }
                SfxManager.Play(SfxId.LueurGain);
                displayedLueur = expectedLueur;
                _hudView.SetLueur(displayedLueur);
                yield return new WaitForSeconds(MinStaggerSeconds);
            }

            BreakPendingShield();

            // Only now — after the enemy has finished draining (and fading
            // out, if lethal) — do the cleared/destroyed cells actually
            // visually empty. Their points already counted during the
            // score cascade above.
            if (deferTileClear)
            {
                yield return PlayTileClearBursts(placement, leechIndex, leechHealAmount, deferredNewMalusCells, deferredReleasedLockedCells, deferredReleasedPoisonedCells);
            }

            // Boss-locked cells, any Locker/Poisoner cell this placement's
            // auto-refill changed, and any lock/poison an enemy dying from
            // this placement's damage (or this placement's own poison
            // contamination — see RunManager.ContaminateAdjacentCell) just
            // released were held back from view this whole time — revealed
            // now that the rest of the sequence (including any enemy death)
            // has played out.
            //
            // Only the cells this placement actually changed fade — see
            // GridView.FadeMalusForCells, as opposed to FadeMalus's blanket
            // sweep of every malus cell on the board. Every other
            // already-shown lock/poison badge stays on screen untouched the
            // whole time instead of flickering out and back in along with
            // it. The hand's own fade-in only plays when it actually
            // auto-refilled this placement — otherwise the hand was never
            // hidden (see HandView.RefreshHoldingEmpty), so there's nothing
            // of its own to reveal, just the grid's malus.
            //
            // A released cell whose enemy died (outcome.DeathReleasedCells) plays a red explosion burst
            // instead of the ordinary fade — a Shuffle-moved lock/poison just relocates, but a death is a
            // destructive event and should read as one.
            var deathReleasedSet = new HashSet<Vector2Int>(outcome.DeathReleasedCells);
            var shuffleReleasedCells = new List<Vector2Int>();
            for (int i = 0; i < deferredReleasedLockedCells.Count; i++)
            {
                if (!deathReleasedSet.Contains(deferredReleasedLockedCells[i]))
                {
                    shuffleReleasedCells.Add(deferredReleasedLockedCells[i]);
                }
            }
            for (int i = 0; i < deferredReleasedPoisonedCells.Count; i++)
            {
                if (!deathReleasedSet.Contains(deferredReleasedPoisonedCells[i]))
                {
                    shuffleReleasedCells.Add(deferredReleasedPoisonedCells[i]);
                }
            }
            for (int i = 0; i < outcome.DeathReleasedCells.Count; i++)
            {
                var pos = outcome.DeathReleasedCells[i];
                _gridView.PlayClearBurst(pos.x, pos.y, UITheme.Danger);
            }
            if (handWasAboutToAutoRefill)
            {
                yield return _gridView.FadeMalusForCells(shuffleReleasedCells, 1f, 0f, ShuffleFadeDuration);

                _handView.SetSlotPiecesAlpha(0f);
                _handView.Refresh();
                // Same suppress-then-reveal as PlayManualShuffleSequence, scoped to just the cells this
                // placement's auto-refill actually changed (deferredNewMalusCells) — they keep showing their
                // previous look here instead of switching to their locked/poisoned one, so the tile doesn't
                // go bare/grey before its circle (below) actually lands.
                _gridView.RefreshHoldingClearedCells(new List<Vector2Int>(), new List<PieceColor>(), new List<PieceTrait?>(), deferredNewMalusCells);
                // Same same-frame zeroing as PlayManualShuffleSequence — ApplyState always paints a newly
                // revealed malus cell opaque, so without this it would briefly flash at full alpha.
                _gridView.SetMalusAlphaForCells(deferredNewMalusCells, 0f);

                yield return _handView.FadeSlotPieces(0f, 1f, ShuffleFadeDuration);
                if (thiefStoleThisPlacement)
                {
                    PlayThiefStealEffect();
                }
                yield return PlayEnemyEffectsSequentially(deferredNewMalusCells);
            }
            else if (hasDeferredGridChange)
            {
                yield return _gridView.FadeMalusForCells(shuffleReleasedCells, 1f, 0f, ShuffleFadeDuration);
                _gridView.RefreshHoldingClearedCells(new List<Vector2Int>(), new List<PieceColor>(), new List<PieceTrait?>(), deferredNewMalusCells);
                _gridView.SetMalusAlphaForCells(deferredNewMalusCells, 0f);
                yield return PlayEnemyEffectsSequentially(deferredNewMalusCells);
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

            yield return PlayFrozenShatters(placement);

            if (outcome.WeatherMovedTiles.Count > 0)
            {
                SfxManager.Play(SfxId.Shuffle);
                _hudView.PulseWeather();
                ShakeScreen(1, 5);
                yield return _gridView.PlayTileFlights(outcome.WeatherMovedTiles, 0.65f);
            }

            }
            finally
            {
                // Whatever happened above, no ice may stay held (a normal run already released it in PlayFrozenShatters).
                var frozenToRelease = FrozenClearedCells(outcome.Placement);
                if (frozenToRelease.Count > 0)
                {
                    _gridView.SetFrozenHold(frozenToRelease, false);
                    _gridView.RefreshCells(frozenToRelease);
                }
                // Hurricane: whatever happened above, no destination plot may stay drawn empty.
                var weatherDestCells = new List<Vector2Int>();
                for (int i = 0; i < outcome.WeatherMovedTiles.Count; i++)
                {
                    weatherDestCells.Add(outcome.WeatherMovedTiles[i].To);
                    weatherDestCells.Add(outcome.WeatherMovedTiles[i].From);
                }
                if (weatherDestCells.Count > 0)
                {
                    _gridView.RefreshCells(weatherDestCells);
                }
                // SetSlotPiecesAlpha(1f) covers the case where this threw mid-fade (see the comment at the
                // top of this method) — Refresh() alone doesn't touch a CanvasGroup's alpha, so without this
                // the hand could stay invisible even once its contents are correctly rebuilt below.
                _handView.SetSlotPiecesAlpha(1f);
                _handView.Refresh();
                // A Chameleon recolor must never leave the placed cells on a stale tint.
                if (outcome.ChameleonOriginalColor.HasValue)
                {
                    _gridView.RefreshCells(outcome.Placement.PlacedCells);
                }
                _isPlayingPlacementSequence = false;
                _handView.SetInteractable(true);
                RefreshShuffleButton();
                _modifierPanelView.SetInteractable(true);
                HandleStateTransition(outcome.StateAfter);
            }
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
        private System.Collections.IEnumerator PlayTileClearBursts(PlacementResult placement, int leechIndex, int leechHealAmount, IReadOnlyList<Vector2Int> deferredNewMalusCells, IReadOnlyList<Vector2Int> releasedLockedCells = null, IReadOnlyList<Vector2Int> releasedPoisonedCells = null)
        {
            // A cleared cell can itself be this same placement's own poison contamination target (see
            // RunManager.ContaminateAdjacentCell) — contamination always spreads to an orthogonal neighbor,
            // which can easily sit in the very line/group just cleared. ClearCellVisual's own ApplyState
            // always reflects the TRUE current model state, so without suppressing it here, that neighbor's
            // brand-new poison badge would appear the instant its burst plays — well before
            // PlayEnemyEffectsSequentially (further down this same sequence) has even started its circle.
            var suppressedCells = new HashSet<Vector2Int>(deferredNewMalusCells);
            // A lock/poison the model already released (shuffle on auto-refill) keeps its badge until the end-of-sequence reveal.
            var keepLocked = releasedLockedCells != null ? new HashSet<Vector2Int>(releasedLockedCells) : new HashSet<Vector2Int>();
            var keepPoisoned = releasedPoisonedCells != null ? new HashSet<Vector2Int>(releasedPoisonedCells) : new HashSet<Vector2Int>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                var pos = placement.ClearedCells[i];
                if (IsClearedCellFrozen(placement, i))
                {
                    // Frozen plots keep their ice until the very end of the sequence (see PlayFrozenShatters).
                    continue;
                }
                SfxManager.Play(SfxId.LineClear);
                // Tinted to the color the cell had right before clearing,
                // via RefreshHoldingClearedCells's held color.
                _gridView.PlayClearBurst(pos.x, pos.y, VisualDefaults.GetColor(placement.ClearedCellColors[i]), placement.ClearedCellColors[i]);
                _gridView.ClearCellVisual(pos.x, pos.y, suppressedCells.Contains(pos), keepLocked.Contains(pos), keepPoisoned.Contains(pos));
                yield return new WaitForSeconds(MinStaggerSeconds);
            }
            // Void Tile / Kamikaze Tile destructions — same burst as a line
            // clear above, but no per-cell score popup (their points already
            // showed as a single Trait ScoreEvent at the enchanted cell).
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                var pos = placement.DestroyedCells[i];
                var color = placement.DestroyedCellColors[i];
                _gridView.PlayClearBurst(pos.x, pos.y, color.HasValue ? VisualDefaults.GetColor(color.Value) : UITheme.TextPrimary, color);
                _gridView.ClearCellVisual(pos.x, pos.y, suppressedCells.Contains(pos));
                SfxManager.Play(SfxId.LineClear);
                yield return new WaitForSeconds(MinStaggerSeconds);
            }
            if (leechHealAmount > 0)
            {
                PlayLeechHealEffect(leechIndex, leechHealAmount);
            }
        }

        /// <summary>The ice of frozen plots this placement's lines broke: held on screen through the whole count and enemy reveals, then shattered here, last.</summary>
        private System.Collections.IEnumerator PlayFrozenShatters(PlacementResult placement)
        {
            var frozen = FrozenClearedCells(placement);
            if (frozen.Count == 0)
            {
                yield break;
            }
            _gridView.SetFrozenHold(frozen, false);
            for (int i = 0; i < frozen.Count; i++)
            {
                SfxManager.Play(SfxId.LineClear);
                _gridView.PlayIceShatter(frozen[i].x, frozen[i].y);
                _gridView.ClearCellVisual(frozen[i].x, frozen[i].y);
                yield return new WaitForSeconds(MinStaggerSeconds);
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


        /// <summary>Counts the combo total down from <paramref name="startTotal"/> to 0 while the targeted enemy's HP ticks down from <paramref name="hpBefore"/> to <paramref name="hpAfter"/> in lockstep — the visual "transfer" of combo score into damage. That countdown only starts once the flying damage orb has actually landed and its "-X" is shown (see FeedbackLayer.SpawnFlyingDamageOrb's onArrival), not while it's still mid-flight. <paramref name="isRetargetHit"/> is true for Range/Précision, which hit a different enemy than the usual front one.</summary>
        private System.Collections.IEnumerator DrainComboIntoDamage(int startTotal, int startChips, float startMult, int enemyIndex, EnemyId identity, int hpBefore, int hpAfter, int maxHp, bool isRetargetHit)
        {
            var enemyAnchor = _hudView.GetEnemyIconTransform(enemyIndex);
            int damage = hpBefore - hpAfter;
            if (enemyAnchor != null && damage > 0)
            {
                // A small plain circle (no text) flies from the combo readout toward the enemy — the actual
                // "-X" amount only appears once it lands (see FeedbackLayer.SpawnFlyingDamageOrb), rather than
                // flying across the screen as text itself. The icon's own hurt shrink plays in lockstep with
                // that reveal instead of at throw time, so it reads as "the hit landing".
                bool damageShown = false;
                _feedbackLayer.SpawnFlyingDamageOrb(_comboView.TotalTransform.position, enemyAnchor, "-" + damage, UITheme.Danger, fontSize: SubtractionFlyingPopupFontSize, attackSprite: _currentAttackSprite, petalSprite: _currentPetalSprite, onArrival: () =>
                {
                    _hudView.PlayEnemyHurtEffect(enemyIndex);
                    ShakeScreen(damage, maxHp);
                    damageShown = true;
                });
                if (isRetargetHit)
                {
                    var originAnchor = _gridView.GetCellTransform(GridManager.Size / 2, GridManager.Size / 2);
                    if (originAnchor != null)
                    {
                        _feedbackLayer.SpawnImpactLine(originAnchor.position, enemyAnchor.position, UITheme.Danger);
                    }
                }
                // The HP bar (and combo total) only start draining once the orb has actually landed and the
                // damage number is shown — draining them earlier, while the orb is still mid-flight, would
                // move the HP bar before the player has seen what hit it.
                while (!damageShown)
                {
                    yield return null;
                }
            }
            const float duration = 0.6f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _comboView.SetTotal(Mathf.RoundToInt(Mathf.Lerp(startTotal, 0f, p)));
                // The chips and mult pills fall in the same stride: chips to 0, mult to 1.
                _comboView.SetPills(Mathf.RoundToInt(Mathf.Lerp(startChips, 0f, p)), Mathf.Lerp(startMult, 1f, p));
                // isDead stays false for every in-between frame, even if the
                // lerp passes through 0 on its way — the icon only turns
                // "dead" on the last frame below, once HP truly lands on
                // hpAfter.
                _hudView.SetEnemyHpDisplay(enemyIndex, Mathf.RoundToInt(Mathf.Lerp(hpBefore, hpAfter, p)), maxHp, identity, false);
                yield return null;
            }
            _comboView.SetTotal(0);
            _comboView.SetPills(0, 1f);
            bool isDead = hpAfter <= 0;
            _hudView.SetEnemyHpDisplay(enemyIndex, hpAfter, maxHp, identity, isDead);

            if (isDead)
            {
                if (EnemyCatalog.Get(identity).Role == EnemyRole.Boss)
                {
                    _gridView.PlayShake(BossKillShakeDuration, BossKillShakeMagnitude);
                }
                // A beat of near-freeze (longer for a boss), then the pause that lets the player register the kill
                // (and the dead-gray tint just applied) before the icon fades.
                yield return PlayHitStop(EnemyCatalog.Get(identity).Role == EnemyRole.Boss ? 0.2f : 0.11f);
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
                int damage = target.HpBefore - target.HpAfter;
                if (damage > 0)
                {
                    var anchor = _hudView.GetEnemyIconTransform(target.Index);
                    if (anchor != null)
                    {
                        // Playtest bug report: Bombe's further targets used to get a plain instant "-X"
                        // popup with no flight. Now each gets its own circle flown from the combo readout,
                        // same visual as the primary target's own DrainComboIntoDamage — one at a time
                        // (sequential, like PlayEnemyEffectsSequentially elsewhere) so a Bombe hitting many
                        // enemies at once still reads clearly instead of several circles landing together.
                        bool damageShown = false;
                        _feedbackLayer.SpawnFlyingDamageOrb(_comboView.TotalTransform.position, anchor, "-" + damage, UITheme.Danger, fontSize: SubtractionFlyingPopupFontSize, attackSprite: _currentAttackSprite, petalSprite: _currentPetalSprite, onArrival: () =>
                        {
                            _hudView.PlayEnemyHurtEffect(target.Index);
                            ShakeScreen(damage, target.MaxHp);
                            damageShown = true;
                        });
                        // Death is only ever checked/applied below once THIS target's own circle has
                        // actually landed — explicit playtest request: verify each hit enemy for death
                        // right after it's actually hit, not before or all at once.
                        while (!damageShown)
                        {
                            yield return null;
                        }
                    }
                }
                _hudView.SetEnemyHpDisplay(target.Index, target.HpAfter, target.MaxHp, target.Identity, isDead);
                if (isDead)
                {
                    if (EnemyCatalog.Get(target.Identity).Role == EnemyRole.Boss)
                    {
                        _gridView.PlayShake(BossKillShakeDuration, BossKillShakeMagnitude);
                    }
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
    }
}
