using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Shuffle, round end, enemies: damage, their Shuffle effects, poison and thief.</summary>
    public sealed partial class RunManager
    {
        /// <summary>
        /// Spends one shuffle charge to re-roll all 3 hand slots at once.
        /// No-op (returns false) when the run isn't InProgress or no charges
        /// are left. Re-runs EvaluateRoundEnd right after re-rolling since a
        /// shuffle can turn a stuck hand into a playable one - or, if the
        /// fresh hand is also unplayable and this was the last charge,
        /// confirm the loss immediately.
        /// </summary>
        public bool ShuffleHand()
        {
            if (State != RunState.InProgress || ShufflesRemaining <= 0)
            {
                return false;
            }
            ShufflesRemaining--;
            ApplyHeatwaveShuffleBurn();
            DrawFreshHand();
            EvaluateRoundEnd();
            return true;
        }

        /// <summary>
        /// Whether the most recently resolved Shuffle (manual <see
        /// cref="ShuffleHand"/>, or the auto-refill <see cref="PlacePiece"/>
        /// triggers) actually stole a piece via an alive Thief. Grid-based
        /// enemy effects are discovered by Presentation through a
        /// before/after snapshot of the grid itself (see
        /// GameBootstrap.SnapshotEnemyEffectCells); Thief's steal touches
        /// only the hand, which has no such snapshot, so this flag is the
        /// equivalent for Presentation to read right after.
        /// </summary>
        public bool ThiefStoleOnLastShuffle { get; private set; }

        /// <summary>The hand slot Thief just emptied, alongside <see cref="ThiefStoleOnLastShuffle"/> — null unless it's true. The slot itself is already empty by the time Presentation reads this (DeckManager.StealRandomHandTile runs before Presentation ever sees the fresh hand), so this and <see cref="ThiefStolenToken"/>/<see cref="ThiefStolenRotation"/> are how Presentation still knows what to animate flying away from it.</summary>
        public int? ThiefStolenHandIndex { get; private set; }

        /// <summary>Every piece stolen by the most recent Shuffle, in order (an enraged Thief takes two) — Presentation animates each one flying off. Empty unless <see cref="ThiefStoleOnLastShuffle"/>.</summary>
        public IReadOnlyList<(int Index, PieceToken Token, PieceRotation Rotation)> ThiefStolenPieces { get; private set; } = new List<(int, PieceToken, PieceRotation)>();

        /// <summary>True when the Thief that stole this Shuffle was below half HP (so it took two seeds instead of one) — Presentation announces it so the double steal doesn't read as a bug.</summary>
        public bool ThiefEnragedOnLastShuffle { get; private set; }

        /// <summary>The actual piece Thief stole, for Presentation to render as a flying ghost — see <see cref="ThiefStolenHandIndex"/>.</summary>
        public PieceToken? ThiefStolenToken { get; private set; }

        /// <summary>The stolen piece's rotation as it was shown in the hand, so its flying ghost matches what the player actually saw.</summary>
        public PieceRotation? ThiefStolenRotation { get; private set; }

        /// <summary>Every lock/poison cell released by an enemy dying during the current PlacePiece call (see CleanUpDefeatedEnemy) — cleared at the start of each PlacePiece, read back into that call's own PlacementOutcome.DeathReleasedCells at the end.</summary>
        private readonly List<Vector2Int> _deathReleasedCellsThisPlacement = new List<Vector2Int>();

        /// <summary>
        /// A Shuffle triggered during this round - PlacePiece's own
        /// post-placement empty-hand refill, and a manual ShuffleHand -
        /// resolving each alive enemy's own On-Shuffle effect first (see
        /// ResolveEnemyShuffleEffects) before Deck.DrawNewHand deals the
        /// fresh hand. Deliberately not used for StartRound's own
        /// carried-over-hand-was-empty draw. No-op beyond the draw itself
        /// when there's no active encounter.
        /// </summary>
        private void DrawFreshHand()
        {
            if (HasActiveEncounter)
            {
                ResolveEnemyShuffleEffects();
            }
            Deck.DrawNewHand();
            ThiefStoleOnLastShuffle = HasActiveEncounter && ResolveThiefShuffleEffect();
        }

        private void EvaluateRoundEnd()
        {
            bool roundCleared = HasActiveEncounter ? AllEnemiesDefeated() : RoundScore >= CurrentQuota;
            if (roundCleared)
            {
                CompleteRoundSuccessfully();
                return;
            }

            if (PiecesRemainingThisRound <= 0)
            {
                State = RunState.RunDefeat;
                return;
            }

            // A fully empty hand (PlacePiece defers its refill when the round
            // might be ending - see PlayFromHand's refillIfEmpty) has nothing to
            // evaluate yet, so it can never count as "stuck": skip the check and
            // let the round stay InProgress. PlacePiece's own
            // post-EvaluateRoundEnd check then draws the next hand right away,
            // which the next placement will correctly check.
            //
            // A stuck hand (no legal placement for any of its 3 pieces) is only
            // a genuine loss once ShufflesRemaining is also exhausted - with
            // shuffles still in the bank, the player can re-roll the hand
            // instead of losing outright (see ShuffleHand, which re-runs this
            // exact check right after re-rolling).
            if (!Deck.IsHandFullyEmpty() && !HasAnyHandPlacement() && ShufflesRemaining <= 0)
            {
                State = RunState.RunDefeat;
            }
        }

        /// <summary>
        /// The "round cleared" branch shared by both of EvaluateRoundEnd's
        /// win conditions: banks the unused-piece-budget Lueur bonus, rolls
        /// Risky Mult's loss chance, and transitions to either RunVictory
        /// (the run's last round) or AwaitingShop.
        /// </summary>
        private void CompleteRoundSuccessfully()
        {
            // Converting every unused piece into +1 Lueur means finishing a
            // round fast still pays out close to what grinding it out fully
            // would have. LastRoundEndLueurBonus is read once by the
            // presentation layer
            // (GameBootstrap.PlayRoundEndLueurBonusSequence) to replay this as a
            // "+1 Lueur" popup per unused piece before the shop actually opens -
            // Lueur itself is already final here, same "core computes the end
            // state instantly, presentation fakes the gradual reveal"
            // convention as every other scoring event in PlacePiece.
            LastRoundEndLueurBonus = PiecesRemainingThisRound;
            Lueur += LastRoundEndLueurBonus;
            ApplyMultCinqRisqueLossChance();
            // Only ever true once (the challenge's own final CurrentRoundIndex,
            // RoundCount - 1, can't recur).
            if (CurrentRoundIndex == _challenge.RoundCount - 1)
            {
                State = RunState.RunVictory;
                return;
            }
            State = RunState.AwaitingShop;
            OpenShop();
        }

        /// <summary>True once every enemy in <see cref="CurrentEncounter"/> is dead - vacuously true for an empty encounter.</summary>
        private bool AllEnemiesDefeated()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                if (!_currentEncounter[i].IsDead)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Builds this round's enemies fresh from EncounterCatalog's authored EnemyIds - a brand-new EnemyInstance per id.</summary>
        private IReadOnlyList<EnemyInstance> BuildEncounter(int roundIndex)
        {
            var ids = EncounterCatalog.GetEncounter(_challenge.Id, roundIndex);
            if (ids.Count == 0)
            {
                return System.Array.Empty<EnemyInstance>();
            }

            var instances = new List<EnemyInstance>(ids.Count);
            float hpScale = EncounterCatalog.GetHpScale(roundIndex);
            for (int i = 0; i < ids.Count; i++)
            {
                var instance = new EnemyInstance(EnemyCatalog.Get(ids[i]), hpScale);
                if (ids[i] == EnemyId.ColorHater)
                {
                    var colors = PieceColorUtility.BaseColors;
                    instance.HatedColor = colors[_rng.Next(colors.Count)];
                }
                else if (ids[i] == EnemyId.ShapeHater)
                {
                    var shapes = (ShapeId[])System.Enum.GetValues(typeof(ShapeId));
                    instance.HatedShape = shapes[_rng.Next(shapes.Length)];
                }
                else if (ids[i] == EnemyId.Reclaimer)
                {
                    // Playtest request: a boss that can't be one-shot by a single huge combo —
                    // see EnemyInstance.HasShield's own doc comment.
                    instance.HasShield = true;
                }
                instances.Add(instance);
            }
            return instances;
        }

        /// <summary>Damages the first alive enemy in encounter order (targeting is always front-to-back) - can be negative (see EnemyInstance.ApplyDamage). Cleans up the kill immediately if lethal.</summary>
        private void ApplyDamageToEncounter(int damage)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.ApplyDamage(damage))
                {
                    CleanUpDefeatedEnemy(enemy);
                }
                break;
            }
        }

        /// <summary>
        /// Scans this placement's whole scored group (see
        /// PlacementResult.GroupCells) for every distinct Joker-exclusive
        /// combat trait kind stamped on any of its cells' <see
        /// cref="Cell.OriginTrait"/> - not just the cells this placement
        /// itself just filled - and dispatches this placement's damage
        /// through each one found, via <see cref="ApplyJokerCombatDamage"/>,
        /// instead of the ordinary front-alive-enemy hit. Falls back to <see
        /// cref="ApplyDamageToEncounter"/> when the group carries none. This
        /// also covers an older Joker piece's combat trait retriggering once
        /// this placement's merge pulls its already-stamped cells into a
        /// newly scored group. A kind is deduplicated across however many of
        /// the group's cells carry it, so two separate Joker pieces of the
        /// same combat kind merged into one group count as a single trigger,
        /// not two.
        /// </summary>
        private void ApplyJokerCombatOrDefaultDamage(IReadOnlyList<Vector2Int> groupCells, int damage)
        {
            List<PieceTraitKind> kindsFound = null;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                var origin = Grid.GetCell(pos.x, pos.y).OriginTrait;
                if (!origin.HasValue || !PieceTrait.IsJokerCombatKind(origin.Value.Kind))
                {
                    continue;
                }
                if (kindsFound == null)
                {
                    kindsFound = new List<PieceTraitKind>();
                }
                if (!kindsFound.Contains(origin.Value.Kind))
                {
                    kindsFound.Add(origin.Value.Kind);
                }
            }

            if (kindsFound == null)
            {
                ApplyDamageToEncounter(damage);
                return;
            }

            // Renfort Joker: only reachable once a combat kind was actually
            // found above, by design - an ordinary front-hit placement is never
            // boosted.
            int boostedDamage = HasEffectiveModifier(ModifierId.RenfortJoker)
                ? Mathf.RoundToInt(damage * (1f + ScoringConstants.RenfortJokerDamageBonusPercent / 100f))
                : damage;

            for (int i = 0; i < kindsFound.Count; i++)
            {
                ApplyJokerCombatDamage(kindsFound[i], boostedDamage);
            }

            // Siphon: Sangsue's own siphon generalized to any combat kind found
            // above, not just Sangsue itself - fires once per placement off the
            // same boosted damage total, regardless of how many enemies it
            // actually reached. Stacks independently with Sangsue if both are
            // held and Sangsue is among the kinds found.
            if (boostedDamage > 0 && HasEffectiveModifier(ModifierId.Siphon))
            {
                Lueur += Mathf.Min(ScoringConstants.SiphonLueurCapPerTrigger, Mathf.FloorToInt(boostedDamage * ScoringConstants.SiphonLueurFraction));
            }
        }

        /// <summary>
        /// Dispatches to a single Joker-exclusive combat trait kind, instead
        /// of the ordinary front-alive-enemy hit <see
        /// cref="ApplyDamageToEncounter"/> always applies - called once per
        /// distinct kind found by <see
        /// cref="ApplyJokerCombatOrDefaultDamage"/>.
        /// </summary>
        private void ApplyJokerCombatDamage(PieceTraitKind kind, int damage)
        {
            switch (kind)
            {
                case PieceTraitKind.Bombe:
                    ApplyBombeDamage(damage);
                    break;
                case PieceTraitKind.Range:
                    ApplyRangeDamage(damage);
                    break;
                case PieceTraitKind.Eclat:
                    ApplyEclatDamage(damage);
                    break;
                case PieceTraitKind.Precision:
                    ApplyPrecisionDamage(damage);
                    break;
                case PieceTraitKind.Sangsue:
                    ApplySangsueDamage(damage);
                    break;
                default:
                    ApplyDamageToEncounter(damage);
                    break;
            }
        }

        /// <summary>How many enemies in <see cref="_currentEncounter"/> are currently alive — "Bombe" splits damage across exactly this many.</summary>
        private int CountAliveEnemies()
        {
            int count = 0;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                if (!_currentEncounter[i].IsDead)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>"Bombe" - splits this placement's damage equally across every alive enemy instead of just the front one. Integer division: a non-exact split rounds each share down.</summary>
        private void ApplyBombeDamage(int damage)
        {
            int aliveCount = CountAliveEnemies();
            if (aliveCount == 0)
            {
                return;
            }
            int share = damage / aliveCount;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.ApplyDamage(share))
                {
                    CleanUpDefeatedEnemy(enemy);
                }
            }
        }

        /// <summary>"Range" - damages the last alive enemy in encounter order instead of the front one; otherwise the same single-target rule as <see cref="ApplyDamageToEncounter"/>.</summary>
        private void ApplyRangeDamage(int damage)
        {
            for (int i = _currentEncounter.Count - 1; i >= 0; i--)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.ApplyDamage(damage))
                {
                    CleanUpDefeatedEnemy(enemy);
                }
                break;
            }
        }

        /// <summary>
        /// "Eclat" - damages the front alive enemy same as the default rule,
        /// but any overkill (damage beyond its remaining HP) cascades onto
        /// the next alive enemy in line, and so on. A non-positive damage
        /// total never cascades - falls back to the ordinary single-target
        /// rule, since "overkill" has no meaning for a heal.
        /// </summary>
        private void ApplyEclatDamage(int damage)
        {
            if (damage <= 0)
            {
                ApplyDamageToEncounter(damage);
                return;
            }
            int remainingDamage = damage;
            for (int i = 0; i < _currentEncounter.Count && remainingDamage > 0; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                int hpBeforeThisHit = enemy.CurrentHp;
                if (enemy.ApplyDamage(remainingDamage))
                {
                    CleanUpDefeatedEnemy(enemy);
                }
                remainingDamage -= hpBeforeThisHit;
            }
        }

        /// <summary>"Précision" — always damages whichever ALIVE enemy currently has the LOWEST HP, ignoring the usual front-to-back order — a finishing blow instead of chipping at the front.</summary>
        private void ApplyPrecisionDamage(int damage)
        {
            EnemyInstance weakest = null;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (weakest == null || enemy.CurrentHp < weakest.CurrentHp)
                {
                    weakest = enemy;
                }
            }
            if (weakest == null)
            {
                return;
            }
            if (weakest.ApplyDamage(damage))
            {
                CleanUpDefeatedEnemy(weakest);
            }
        }

        /// <summary>"Sangsue" — damages the front alive enemy exactly like the default rule, but also converts <see cref="ScoringConstants.SangsueLueurFraction"/> of the damage dealt into bonus Lueur (rounded down), tying combat back into the economy. Only a positive damage total siphons anything — a poison-flipped negative placement still heals the enemy same as ever, but grants no Lueur for it.</summary>
        private void ApplySangsueDamage(int damage)
        {
            ApplyDamageToEncounter(damage);
            if (damage > 0)
            {
                Lueur += Mathf.Min(ScoringConstants.SiphonLueurCapPerTrigger, Mathf.FloorToInt(damage * ScoringConstants.SangsueLueurFraction));
            }
        }

        /// <summary>
        /// "Leech": heals every alive Leech instance <see
        /// cref="ScoringConstants.LeechHealPerLineClear"/> HP per row/column
        /// this placement cleared. Goes through <see
        /// cref="EnemyInstance.HealOrGrow"/>, not the plain <see
        /// cref="EnemyInstance.Heal"/> - a heal landing while already at
        /// full HP raises its ceiling instead of going to waste.
        /// HealOrGrow itself no-ops once dead.
        /// </summary>
        private void HealLeech(int clearedLineCount)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.Definition.Id == EnemyId.Leech)
                {
                    enemy.HealOrGrow(ScoringConstants.LeechHealPerLineClear * clearedLineCount);
                }
            }
        }

        /// <summary>When an enemy dies, its active effects are cancelled/cleaned up immediately: Locker's current lock is released, and every tile Poisoner poisoned is normalized. Every cell released this way is also recorded in <see cref="_deathReleasedCellsThisPlacement"/>, so Presentation can tell a death-caused release apart from an ordinary Shuffle-move release (RunManager.ResolveLockerShuffleEffect/ResolvePoisonerShuffleEffect) and play a different reveal for it (see PlacePiece's own DeathReleasedCells on the returned PlacementOutcome).</summary>
        private void CleanUpDefeatedEnemy(EnemyInstance enemy)
        {
            if (enemy.LockedCell.HasValue)
            {
                var lockedCell = Grid.GetCell(enemy.LockedCell.Value);
                lockedCell.IsLocked = false;
                lockedCell.IsLineClearObstacle = false;
                _deathReleasedCellsThisPlacement.Add(enemy.LockedCell.Value);
                enemy.LockedCell = null;
            }
            for (int i = 0; i < enemy.PoisonedCells.Count; i++)
            {
                Grid.GetCell(enemy.PoisonedCells[i]).IsPoisoned = false;
                _deathReleasedCellsThisPlacement.Add(enemy.PoisonedCells[i]);
            }
            enemy.ClearPoisonedCells();
            if (enemy.Definition.Id == EnemyId.Blightmoth)
            {
                _blightedModifiers.Clear();
            }
        }

        /// <summary>
        /// <summary>
        /// Resolves every alive enemy's own On-Shuffle effect, in encounter
        /// order. Called from <see cref="DrawFreshHand"/>, always before the
        /// fresh hand is actually dealt. HeavyLocker/Plague are Locker's/
        /// Poisoner's own mechanics at boss scale. Reclaimer has no
        /// On-Shuffle effect at all - it heals reactively off poison's own
        /// negative scoring instead (see HealReclaimer/ApplyPoisonScoreRule).
        /// </summary>
        /// </summary>
        private void ResolveEnemyShuffleEffects()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.Definition.Id == EnemyId.Locker || enemy.Definition.Id == EnemyId.HeavyLocker)
                {
                    ResolveLockerShuffleEffect(enemy);
                }
                else if (enemy.Definition.Id == EnemyId.Poisoner)
                {
                    ResolvePoisonerShuffleEffect(enemy, 1);
                }
                else if (enemy.Definition.Id == EnemyId.Plague)
                {
                    ResolvePoisonerShuffleEffect(enemy, 5);
                }
                else if (enemy.Definition.Id == EnemyId.Blightmoth)
                {
                    ResolveBlightmothShuffleEffect();
                }
            }
        }

        private const int EnemyTickInterval = ScoringConstants.EnemyTickInterval;

        /// <summary>Seeds still to place before the next timed enemy trick (counts down 6..1, then the trick fires and it restarts); shown by the HUD.</summary>
        public int PlacementsUntilEnemyTick
        {
            get
            {
                int played = CurrentBudget - PiecesRemainingThisRound;
                return EnemyTickInterval - (played % EnemyTickInterval);
            }
        }

        /// <summary>True when an alive enemy in this round has a timed trick (so the HUD shows its countdown).</summary>
        public bool HasTimedEnemyTricks
        {
            get
            {
                for (int i = 0; i < _currentEncounter.Count; i++)
                {
                    if (!_currentEncounter[i].IsDead && HasTimedTrick(_currentEncounter[i].Definition.Id))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Bramble re-locks a plot and Blightmoth re-blights a fertilizer every few seeds, on top of their Shuffle trigger.</summary>
        public static bool HasTimedTrick(EnemyId id)
        {
            return id == EnemyId.Locker || id == EnemyId.Blightmoth;
        }

        /// <summary>Fires the timed tricks of every alive enemy that has one (the Shuffle trigger is separate, see ResolveEnemyShuffleEffects).</summary>
        private void ResolveEnemyTickEffects()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.Definition.Id == EnemyId.Locker)
                {
                    ResolveLockerShuffleEffect(enemy);
                }
                else if (enemy.Definition.Id == EnemyId.Blightmoth)
                {
                    ResolveBlightmothShuffleEffect();
                }
            }
        }

        /// <summary>
        /// Locker locks 1 grid cell per Shuffle; the previous lock is
        /// released at the next Shuffle and a new cell is selected. Uses the
        /// plain <see cref="GridManager.LockRandomCells"/> (no line-clear
        /// re-check), so Locker's own lock should never itself be the reason
        /// a row/column completes and clears - that remains specific to the
        /// pre-existing ProgressiveCellLock boss effect (see
        /// ApplyBossLockTick), not this enemy. Also stamps
        /// Cell.IsLineClearObstacle so the row/column it sits in can't be
        /// cleared by a later placement either, for as long as the lock
        /// stays there (the old boss lock and a Bastion cell are
        /// deliberately exempt - see Cell.IsLineClearObstacle's own doc
        /// comment).
        /// </summary>
        private void ResolveLockerShuffleEffect(EnemyInstance locker)
        {
            if (locker.LockedCell.HasValue)
            {
                var previousCell = Grid.GetCell(locker.LockedCell.Value);
                previousCell.IsLocked = false;
                previousCell.IsLineClearObstacle = false;
                locker.LockedCell = null;
            }

            var locked = Grid.LockRandomCells(1, _rng);
            if (locked.Count > 0)
            {
                locker.LockedCell = locked[0];
                Grid.GetCell(locked[0]).IsLineClearObstacle = true;
            }
        }

        /// <summary>
        /// Poisons <paramref name="count"/> tiles per Shuffle (Poisoner: 1,
        /// Plague: 5). Roams exactly like Locker's single lock instead of
        /// accumulating: releases every cell this instance poisoned so far,
        /// including any cell contamination (see ContaminateAdjacentCell)
        /// added since its last Shuffle, before picking new ones. Any
        /// not-yet-poisoned cell is a valid target, filled or empty alike -
        /// an empty one simply sits as a trap for whatever piece lands there
        /// later.
        /// </summary>
        private void ResolveBlightmothShuffleEffect()
        {
            // The previous blight is lifted first, so only the freshly picked slot is blighted at a time.
            _blightedModifiers.Clear();
            if (_activeModifiers.Count == 0)
            {
                return;
            }
            _blightedModifiers.Add(_rng.Next(_activeModifiers.Count));
        }

        private void ResolvePoisonerShuffleEffect(EnemyInstance poisoner, int count)
        {
            for (int i = 0; i < poisoner.PoisonedCells.Count; i++)
            {
                Grid.GetCell(poisoner.PoisonedCells[i]).IsPoisoned = false;
            }
            poisoner.ClearPoisonedCells();

            var candidates = new List<Vector2Int>();
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var cell = Grid.GetCell(x, y);
                    if (!cell.IsPoisoned)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int idx = _rng.Next(candidates.Count);
                var pos = candidates[idx];
                candidates.RemoveAt(idx);
                Grid.GetCell(pos).IsPoisoned = true;
                poisoner.AddPoisonedCell(pos);
            }
        }

        /// <summary>
        /// "Contamination": called from ApplyPoisonScoreRule once per
        /// distinct poisoned position a placement actually scored negatively
        /// through. Poisons one random orthogonally-adjacent, not-yet-
        /// poisoned cell (in-bounds only; a no-op if every neighbor is
        /// already poisoned or <paramref name="pos"/>'s own poison isn't
        /// owned by any living enemy instance), adding it to the same
        /// instance's own PoisonedCells list so it rolls away at that
        /// instance's next Shuffle exactly like the original cell did.
        /// </summary>
        private void ContaminateAdjacentCell(Vector2Int pos)
        {
            EnemyInstance owner = FindPoisonOwner(pos);
            if (owner == null)
            {
                return;
            }

            var offsets = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            var candidates = new List<Vector2Int>();
            for (int i = 0; i < offsets.Length; i++)
            {
                var neighbor = pos + offsets[i];
                if (GridManager.InBounds(neighbor.x, neighbor.y) && !Grid.GetCell(neighbor).IsPoisoned)
                {
                    candidates.Add(neighbor);
                }
            }
            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[_rng.Next(candidates.Count)];
            Grid.GetCell(target).IsPoisoned = true;
            owner.AddPoisonedCell(target);
        }

        /// <summary>The enemy instance whose own PoisonedCells list currently tracks <paramref name="pos"/>, or null if nothing in the current encounter owns it (dead enemies are cleaned up immediately on death, so this only ever finds a living owner).</summary>
        private EnemyInstance FindPoisonOwner(Vector2Int pos)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                for (int j = 0; j < enemy.PoisonedCells.Count; j++)
                {
                    if (enemy.PoisonedCells[j] == pos)
                    {
                        return enemy;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Heals every alive Reclaimer instance by <paramref
        /// name="totalPoisonMagnitude"/>, the sum of every point this
        /// placement lost to poison (see ApplyPoisonScoreRule's return
        /// value). Replaces Reclaimer's old On-Shuffle consume-and-heal
        /// effect entirely - it no longer has one (see
        /// ResolveEnemyShuffleEffects). Goes through <see
        /// cref="EnemyInstance.HealOrGrow"/>, not the plain Heal every other
        /// healer uses - a heal landing while already at full HP grows it
        /// permanently stronger instead of being wasted.
        /// </summary>
        private void HealReclaimer(int totalPoisonMagnitude)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.Definition.Id == EnemyId.Reclaimer)
                {
                    enemy.HealOrGrow(totalPoisonMagnitude);
                }
            }
        }

        /// <summary>
        /// Thief steals 1 random tile from the hand per Shuffle. Unlike
        /// every other enemy's On-Shuffle effect (grid-based, resolved via
        /// ResolveEnemyShuffleEffects before Deck.DrawNewHand), Thief needs
        /// the fresh hand to already exist to steal from it - so
        /// DrawFreshHand calls this separately, after the deal. Returns
        /// whether any alive Thief actually stole something (see
        /// ThiefStoleOnLastShuffle).
        /// </summary>
        private const int ThiefEnragedStealCount = 2;

        private bool ResolveThiefShuffleEffect()
        {
            bool stole = false;
            var stolenList = new List<(int, PieceToken, PieceRotation)>();
            ThiefStolenPieces = stolenList;
            ThiefEnragedOnLastShuffle = false;
            ThiefStolenHandIndex = null;
            ThiefStolenToken = null;
            ThiefStolenRotation = null;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (!enemy.IsDead && enemy.Definition.Id == EnemyId.Thief)
                {
                    // Soulslike-style stage change, playtest request: "Boss avec plusieurs stages ...
                    // qui force le joueur a jouer différemment" — below half HP, Thief steals twice per
                    // Shuffle instead of once, forcing the player to stop relying on holding pieces
                    // in hand as it nears death.
                    int stealCount = enemy.CurrentHp * 2 <= enemy.CurrentMaxHp ? ThiefEnragedStealCount : 1;
                    if (stealCount > 1)
                    {
                        ThiefEnragedOnLastShuffle = true;
                    }
                    // Several alive Thieves (or ThiefEnragedStealCount>1 steals from the same one) would
                    // each steal a piece, but only the last one's is kept here to animate — same
                    // simplification ThiefStoleOnLastShuffle's own plain bool already makes for "how many".
                    for (int s = 0; s < stealCount; s++)
                    {
                        if (Deck.StealRandomHandTile(_rng, out var stolenIndex, out var stolenToken, out var stolenRotation))
                        {
                            stole = true;
                            stolenList.Add((stolenIndex, stolenToken, stolenRotation));
                            ThiefStolenHandIndex = stolenIndex;
                            ThiefStolenToken = stolenToken;
                            ThiefStolenRotation = stolenRotation;
                        }
                    }
                }
            }
            return stole;
        }

        /// <summary>Every currently-poisoned position across every alive enemy instance in <see cref="CurrentEncounter"/>, regardless of which one owns it. See PlacePiece's own snapshot-before-mutation comment for why this must be read before Grid.PlacePiece runs.</summary>
        private HashSet<Vector2Int> GetPoisonedPositionsSnapshot()
        {
            var snapshot = new HashSet<Vector2Int>();
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                for (int j = 0; j < enemy.PoisonedCells.Count; j++)
                {
                    snapshot.Add(enemy.PoisonedCells[j]);
                }
            }
            return snapshot;
        }

        /// <summary>
        /// Negates a poisoned position's point events instead of dropping
        /// them, with the same preserve-clears/Lueur/non-point-multiplier-
        /// effects contract as <see cref="ApplyCursedColorScoreRule"/> -
        /// poison is a cost, not an exemption, and <see
        /// cref="EnemyInstance.ApplyDamage"/> reads the resulting (possibly
        /// net-negative) TotalScore as this placement's damage, so playing
        /// into poison can genuinely heal the enemy back up. Returns the
        /// total magnitude flipped negative (0 if none), which the caller
        /// feeds into <see cref="HealReclaimer"/>, and spreads
        /// "contamination" (see ContaminateAdjacentCell) from every distinct
        /// poisoned position actually triggered this way.
        /// </summary>
        private int ApplyPoisonScoreRule(PlacementResult placement, HashSet<Vector2Int> poisonedPositions)
        {
            placement.GroupBonus = 0;
            placement.GoldenBonus = 0;
            placement.LineClearScore = 0;
            placement.ModifierBonus = 0;
            placement.TraitBonus = 0;
            placement.ShapeMasteryBonus = 0;
            placement.ColorMasteryBonus = 0;

            int totalPoisonMagnitude = 0;
            var triggeredPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                // A modifier that reads a second cell to decide its own
                // eligibility (e.g. Contraste reading a contrasting neighbor - see
                // ScoreEvent.ReferencedPosition) is just as much "using" a poisoned
                // tile as one scored directly on it, so it flips negative too.
                bool touchesPoison = poisonedPositions.Contains(scoreEvent.Position)
                    || (scoreEvent.ReferencedPosition.HasValue && poisonedPositions.Contains(scoreEvent.ReferencedPosition.Value));
                if (IsPointEvent(scoreEvent.Type) && scoreEvent.Amount > 0 && touchesPoison)
                {
                    totalPoisonMagnitude += scoreEvent.Amount;
                    if (poisonedPositions.Contains(scoreEvent.Position))
                    {
                        triggeredPositions.Add(scoreEvent.Position);
                    }
                    if (scoreEvent.ReferencedPosition.HasValue && poisonedPositions.Contains(scoreEvent.ReferencedPosition.Value))
                    {
                        triggeredPositions.Add(scoreEvent.ReferencedPosition.Value);
                    }
                    scoreEvent.Amount = -scoreEvent.Amount;
                }

                switch (scoreEvent.Type)
                {
                    case ScoreEventType.Group: placement.GroupBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Golden: placement.GoldenBonus += scoreEvent.Amount; break;
                    case ScoreEventType.LineClear:
                    case ScoreEventType.Bastion: placement.LineClearScore += scoreEvent.Amount; break;
                    case ScoreEventType.Modifier: placement.ModifierBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Trait: placement.TraitBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ShapeMastery: placement.ShapeMasteryBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ColorMastery: placement.ColorMasteryBonus += scoreEvent.Amount; break;
                }
            }

            foreach (var pos in triggeredPositions)
            {
                ContaminateAdjacentCell(pos);
            }

            return totalPoisonMagnitude;
        }

        /// <summary>
        /// Dwindling (Epuisement): once its decaying bonus (see
        /// GridManager.EpuisementCurrentBonus) has fully bottomed out at 0,
        /// it can never earn another point for the rest of the run - removed
        /// the instant that happens, freeing its modifier slot instead of
        /// leaving a permanently-dead entry sitting in it.
        /// _activeModifiers can never hold more than one copy of Epuisement
        /// (RollBlisterSlot excludes modifiers already owned), so IndexOf is
        /// unambiguous.
        /// </summary>
        private void RemoveDepletedEpuisement()
        {
            if (Grid.EpuisementCurrentBonus > 0)
            {
                return;
            }
            int index = _activeModifiers.IndexOf(ModifierId.Epuisement);
            if (index >= 0)
            {
                RemoveActiveModifierAt(index);
            }
        }

        /// <summary>Risky Mult (MultCinqRisque): rolled once per held copy, right at the end of a successfully completed round. Each copy independently has a 1-in-EconomyConstants.MultCinqRisqueLossChanceDenominator chance to be removed.</summary>
        private void ApplyMultCinqRisqueLossChance()
        {
            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                if (_activeModifiers[i] != ModifierId.MultCinqRisque)
                {
                    continue;
                }
                if (_rng.Next(EconomyConstants.MultCinqRisqueLossChanceDenominator) == 0)
                {
                    RemoveActiveModifierAt(i);
                }
            }
        }

        /// <summary>True if at least one piece currently in hand, in its actual dealt rotation, can be legally placed somewhere on the grid — empty slots are skipped.</summary>
        private bool HasAnyHandPlacement()
        {
            var hand = Deck.Hand;
            var rotations = Deck.HandRotations;
            var shapes = new List<PieceShape>(hand.Count);
            for (int i = 0; i < hand.Count; i++)
            {
                if (!hand[i].HasValue || IsHandSlotLocked(i))
                {
                    continue;
                }
                shapes.Add(PieceShapeCatalog.GetRotated(hand[i].Value.Shape, rotations[i]));
            }
            return Grid.HasAnyValidPlacement(shapes);
        }

        // ---- Lueur shop ----
    }
}
