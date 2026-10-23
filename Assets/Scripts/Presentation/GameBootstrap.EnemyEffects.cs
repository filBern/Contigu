using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>How enemy tricks (locks, blight, steals) are revealed on the board.</summary>
    public sealed partial class GameBootstrap
    {
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
                if (enemy.ExtraLockedCell.HasValue)
                {
                    lockedCells.Add(enemy.ExtraLockedCell.Value);
                }
                poisonedCells.AddRange(enemy.PoisonedCells);
            }
        }

        /// <summary>Which fertilizer slots are blighted right now, as a string: compare before/after a placement or Shuffle to know Blightmoth just struck.</summary>
        private string BlightSignature()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _run.ActiveModifiers.Count; i++)
            {
                sb.Append(_run.IsModifierBlighted(i) ? '1' : '0');
            }
            return sb.ToString();
        }

        /// <summary>If the blight moved since <paramref name="before"/>, the Blightmoth's portrait reacts (and the fertilizer panel is already redrawn by RefreshAll).</summary>
        /// <summary>
        /// Plays what a challenge's resident enemy (Hoarfrost, Locust Swarm, Rotgrub, Fruitworm, Whitefly Cloud) just did on a Shuffle: its
        /// portrait pulses, the status line says it, the Locust's bite shows on the Pollen counter and Rotgrub's on the fertilizer panel. The
        /// grid itself is refreshed by the Shuffle flow, so frozen/recolored/swapped plots simply show up. No-op when none acted.
        /// </summary>
        private void PlayWorldEnemyActIfAny()
        {
            var acted = _run.LastWorldEnemyAct;
            if (!acted.HasValue)
            {
                return;
            }
            int index = FindAliveEnemyIndex(acted.Value);
            if (index >= 0)
            {
                _hudView.PlayEnemyAct(index);
                _hudView.PlayEnemyEffectPulse(index);
            }
            SetStatusText(_run.LastWorldEnemyMessage);
            if (acted.Value == EnemyId.Locust && _run.LastPollenEaten > 0)
            {
                _hudView.SetLueur(_run.Lueur);
                _feedbackLayer.SpawnPopup(_hudView.LueurLabelTransform, "-" + _run.LastPollenEaten, UITheme.Danger);
            }
            else if (acted.Value == EnemyId.Rotgrub)
            {
                _modifierPanelView.Refresh(_run.ActiveModifiers);
            }
        }

        // What the placement's auto-refill Shuffle did, waiting for the score count to finish before it is shown.
        private bool _pendingShuffleActs;
        private string _pendingBlightBefore;
        private WorldGridChange? _pendingWorldChange;

        /// <summary>The end-of-sequence reveal of the Shuffle effects that must not show before the points are counted: Blightmoth's blight, then the resident enemy (portrait, Pollen bite, and its tile crossfade).</summary>
        private System.Collections.IEnumerator PlayDeferredShuffleActs()
        {
            // The blight can also change without any Shuffle: a Blightmoth killed by this very placement lifts it, and the fertilizer panel must follow.
            if (_pendingBlightBefore != null)
            {
                PlayBlightmothActIfBlightChanged(_pendingBlightBefore);
                _pendingBlightBefore = null;
            }
            if (!_pendingShuffleActs)
            {
                yield break;
            }
            _pendingShuffleActs = false;
            PlayWorldEnemyActIfAny();
            if (_pendingWorldChange.HasValue)
            {
                var change = _pendingWorldChange.Value;
                _pendingWorldChange = null;
                yield return _gridView.PlayWorldGridChange(change);
            }
        }

        private void PlayBlightmothActIfBlightChanged(string before)
        {
            string after = BlightSignature();
            if (before == after)
            {
                return;
            }
            // Redraw the fertilizer panel so the blight veil/mark appears (or clears) right when it happens.
            _modifierPanelView.Refresh(_run.ActiveModifiers);
            for (int i = 0; i < after.Length && i < _run.ActiveModifiers.Count; i++)
            {
                if (after[i] == '1' && (i >= before.Length || before[i] != '1'))
                {
                    _modifierPanelView.Pulse(_run.ActiveModifiers[i]);
                }
            }
            int index = FindAliveEnemyIndex(EnemyId.Blightmoth);
            if (index >= 0)
            {
                _hudView.PlayEnemyAct(index);
            }
        }

        // Lock's circle reads as a cool, inert obstacle; poison's reuses the same red as its own badge
        // (UITheme.Danger) — same two colors used for every other lock/poison visual in the game.
        private static readonly Color EnemyGridEffectLockColor = UITheme.LightBlue;

        // Pause between one enemy's effect finishing (circle landed, its cells faded in) and the next
        // enemy's icon pulsing — so a Shuffle with several effect-enemies active reads as a legible
        // one-after-another sequence instead of everything firing at once.
        private const float SequentialEnemyEffectGapSeconds = 0.06f;

        /// <summary>
        /// Reveals each alive Locker/HeavyLocker/Poisoner/Plague's new lock/poison one enemy at a time,
        /// instead of every enemy's icon pulsing and flying a circle all at once: for each enemy (in
        /// encounter order) that currently owns a cell in <paramref name="cells"/>, pulses its icon, flies a
        /// circle to each of its own cells in that list, waits for them to land, then reveals just those
        /// cells (switches them off suppressMalus and fades them in) before moving on to the next enemy.
        /// <paramref name="cells"/> must already be suppressed (see GridView.RefreshHoldingClearedCells) when
        /// this starts. A cell with no owning living enemy (e.g. a boss-locked one — see
        /// RunManager.ApplyBossLockTick) has no icon to pulse or circle to fly, so it's revealed in one
        /// group at the very end instead. No-op if <paramref name="cells"/> is empty.
        /// </summary>
        private System.Collections.IEnumerator PlayEnemyEffectsSequentially(IReadOnlyList<Vector2Int> cells)
        {
            if (cells.Count == 0)
            {
                yield break;
            }
            var remaining = new HashSet<Vector2Int>(cells);
            // First time a pest's trick actually lands on the board: Sprig points out that the garden is the battlefield.
            if (GamePrefs.ConsumeFirstTime(BoardEffectTipPrefsKey))
            {
                _sprigView.Say("See that? The pests fight right on your garden! Locked and blighted plots change how you plant. Work around them!", SprigMood.Sad, 7f);
            }
            if (_run.HasActiveEncounter)
            {
                var encounter = _run.CurrentEncounter;
                for (int i = 0; i < encounter.Count; i++)
                {
                    var enemy = encounter[i];
                    if (enemy.IsDead)
                    {
                        continue;
                    }
                    var ownCells = new List<Vector2Int>();
                    var ownColors = new List<Color>();
                    if (enemy.LockedCell.HasValue && remaining.Contains(enemy.LockedCell.Value))
                    {
                        ownCells.Add(enemy.LockedCell.Value);
                        ownColors.Add(EnemyGridEffectLockColor);
                    }
                    if (enemy.ExtraLockedCell.HasValue && remaining.Contains(enemy.ExtraLockedCell.Value))
                    {
                        ownCells.Add(enemy.ExtraLockedCell.Value);
                        ownColors.Add(EnemyGridEffectLockColor);
                    }
                    for (int j = 0; j < enemy.PoisonedCells.Count; j++)
                    {
                        var pos = enemy.PoisonedCells[j];
                        if (remaining.Contains(pos))
                        {
                            ownCells.Add(pos);
                            ownColors.Add(UITheme.Danger);
                        }
                    }
                    if (ownCells.Count == 0)
                    {
                        continue;
                    }

                    var iconAnchor = _hudView.GetEnemyIconTransform(i);
                    _hudView.PlayEnemyEffectPulse(i);
                    _hudView.PlayEnemyAct(i);

                    int pending = 0;
                    for (int c = 0; c < ownCells.Count; c++)
                    {
                        if (iconAnchor == null)
                        {
                            continue;
                        }
                        var cellAnchor = _gridView.GetCellTransform(ownCells[c].x, ownCells[c].y);
                        if (cellAnchor == null)
                        {
                            continue;
                        }
                        pending++;
                        _feedbackLayer.SpawnFlyingEffectCircle(iconAnchor.position, cellAnchor, ownColors[c], () => pending--);
                    }
                    while (pending > 0)
                    {
                        yield return null;
                    }

                    remaining.ExceptWith(ownCells);
                    _gridView.RefreshSuppressingCells(new List<Vector2Int>(remaining));
                    _gridView.SetMalusAlphaForCells(ownCells, 0f);
                    yield return _gridView.FadeMalusForCells(ownCells, 0f, 1f, ShuffleFadeDuration);
                    yield return new WaitForSeconds(SequentialEnemyEffectGapSeconds);
                }
            }

            // Whatever's left has no owning enemy to pulse/fly a circle from (e.g. a boss-locked cell) —
            // reveal it together, right after the last enemy's own turn.
            if (remaining.Count > 0)
            {
                var leftoverCells = new List<Vector2Int>(remaining);
                _gridView.RefreshSuppressingCells(new List<Vector2Int>());
                _gridView.SetMalusAlphaForCells(leftoverCells, 0f);
                yield return _gridView.FadeMalusForCells(leftoverCells, 0f, 1f, ShuffleFadeDuration);
            }
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
                    _hudView.PlayEnemyAct(i);
                    StartCoroutine(PlayThiefStealRoutine(i));
                    return;
                }
            }
        }

        /// <summary>
        /// Each stolen seed flies from its (already empty) hand slot to the Crow's icon, one after the other, instead of
        /// only the last one animating. An enraged Crow (below half HP, two seeds per Shuffle) also announces itself with
        /// a big "ENRAGED!" callout and a stronger pulse, so the double steal reads as intended rather than a bug.
        /// </summary>
        private System.Collections.IEnumerator PlayThiefStealRoutine(int thiefIndex)
        {
            var anchor = _hudView.GetEnemyIconTransform(thiefIndex);
            var stolen = new List<(int Index, PieceToken Token, PieceRotation Rotation)>(_run.ThiefStolenPieces);
            bool enraged = _run.ThiefEnragedOnLastShuffle;

            _hudView.PlayEnemyEffectPulse(thiefIndex);
            if (anchor != null && enraged)
            {
                // floatDown: this anchor sits near the top of the screen; SpawnPopup's usual float-up would run the text off the edge.
                _feedbackLayer.SpawnPopup(anchor, "ENRAGED!", UITheme.Danger, fontSize: 40, floatDown: true);
                SfxManager.Play(SfxId.InvalidDrop);
                yield return new WaitForSeconds(0.35f);
            }

            for (int k = 0; k < stolen.Count; k++)
            {
                _hudView.PlayEnemyEffectPulse(thiefIndex);
                if (anchor != null)
                {
                    var slotTransform = _handView.GetSlotPreviewTransform(stolen[k].Index);
                    if (slotTransform != null)
                    {
                        var shape = PieceShapeCatalog.GetRotated(stolen[k].Token.Shape, stolen[k].Rotation);
                        _feedbackLayer.SpawnFlyingPiece(slotTransform.position, anchor, shape, stolen[k].Token.Color);
                    }
                }
                SfxManager.Play(SfxId.PickUpPiece);
                if (k < stolen.Count - 1)
                {
                    yield return new WaitForSeconds(0.3f);
                }
            }

            if (anchor != null)
            {
                string text = stolen.Count > 1 ? "Stole " + stolen.Count + " seeds!" : "Stole a seed!";
                _feedbackLayer.SpawnPopup(anchor, text, UITheme.Danger, floatDown: true);
            }
        }
    }
}
