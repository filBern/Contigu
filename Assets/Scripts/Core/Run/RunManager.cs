using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Orchestrates the 8-round run: quotas, piece budgets, boss round locking,
    /// and the win/lose transition into the upgrade draft (spec sections 1 and 6).
    ///
    /// A round ends the instant its quota is reached (success), or as soon as
    /// either its piece budget runs out or its hand becomes unplayable — no
    /// legal placement left anywhere on the grid for any of the 3 hand pieces —
    /// without having reached the quota (defeat).
    /// </summary>
    public sealed class RunManager
    {
        /// <summary>Max number of modifiers a player can hold active at once (spec extension — see ModifierCatalog).</summary>
        public const int MaxActiveModifiers = 5;

        /// <summary>How many modifier options are offered per draft.</summary>
        public const int ModifierDraftSize = 3;

        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

        private readonly List<ModifierId> _activeModifiers = new List<ModifierId>();

        /// <summary>Modifiers currently held by the player, persisting for the whole run (never reset between rounds).</summary>
        public IReadOnlyList<ModifierId> ActiveModifiers
        {
            get { return _activeModifiers; }
        }

        private readonly IRandomProvider _rng;

        /// <summary>0-based index into <see cref="RunConfig"/> arrays.</summary>
        public int CurrentRoundIndex { get; private set; }

        public int RoundScore { get; private set; }
        public int TotalScore { get; private set; }
        public int PiecesRemainingThisRound { get; private set; }

        public RunState State { get; private set; }

        public int CurrentRoundNumber
        {
            get { return CurrentRoundIndex + 1; }
        }

        public int CurrentQuota
        {
            get { return RunConfig.Quotas[CurrentRoundIndex]; }
        }

        public int CurrentBudget
        {
            get { return RunConfig.PieceBudgets[CurrentRoundIndex]; }
        }

        public bool IsBossRound
        {
            get { return CurrentRoundIndex == RunConfig.BossRoundIndex; }
        }

        public RunManager(IRandomProvider rng)
        {
            _rng = rng;
            Grid = new GridManager();
            Deck = new DeckManager(InitialDeckFactory.Build(), rng);
            Upgrades = new UpgradeSystem(rng);
            CurrentRoundIndex = 0;
            StartRound();
        }

        private void StartRound()
        {
            Grid.ResetForNewRound();
            if (IsBossRound)
            {
                Grid.LockRandomCells(RunConfig.BossLockedCellCount, _rng);
            }
            // Normally a no-op (the hand carries over from the previous
            // round untouched) — only fires for the deferred draw PlacePiece
            // skips when the placement that empties the hand also ends the
            // round, so the fresh hand is drawn here, for the round it
            // actually belongs to, rather than during the previous round's
            // tail end before the player has even picked their upgrade.
            if (Deck.Hand.Count == 0)
            {
                Deck.DrawNewHand();
            }
            RoundScore = 0;
            PiecesRemainingThisRound = CurrentBudget;
            State = RunState.InProgress;
        }

        /// <summary>
        /// Attempts to place the hand piece at <paramref name="handIndex"/> anchored
        /// at (x, y). Returns the full outcome including whether the round/run
        /// ended as a result. No-ops (placement fails, state untouched) if the run
        /// isn't currently InProgress or the placement is invalid.
        /// </summary>
        public PlacementOutcome PlacePiece(int handIndex, int x, int y)
        {
            if (State != RunState.InProgress)
            {
                return new PlacementOutcome(PlacementResult.Failure("Run is not in progress"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            if (handIndex < 0 || handIndex >= Deck.Hand.Count)
            {
                return new PlacementOutcome(PlacementResult.Failure("Invalid hand index"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            var token = Deck.Hand[handIndex];
            var rotation = Deck.HandRotations[handIndex];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            if (!Grid.CanPlace(shape, x, y))
            {
                return new PlacementOutcome(PlacementResult.Failure("Invalid placement"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            Vector2Int? traitCellPos = token.Trait.HasValue
                ? new Vector2Int(x, y) + shape.Cells[token.Trait.Value.LocalCellIndex]
                : (Vector2Int?)null;

            var transientTraitCells = ApplyTokenTrait(token.Trait, traitCellPos);
            var placement = Grid.PlacePiece(shape, token.Color, x, y, _activeModifiers);
            ClearTokenTraitCells(transientTraitCells);
            if (token.Trait.HasValue && token.Trait.Value.Kind == PieceTraitKind.Mirror)
            {
                ApplyMirrorBonus(traitCellPos.Value, placement);
            }
            RoundScore += placement.TotalScore;
            TotalScore += placement.TotalScore;
            // Don't auto-refill yet — if this placement also ends the round,
            // drawing the next 3 pieces here would hand them out before the
            // player has even picked this round's upgrade (see StartRound,
            // which draws instead in that case).
            Deck.PlayFromHand(handIndex, refillIfEmpty: false);
            PiecesRemainingThisRound--;

            EvaluateRoundEnd();

            if (State == RunState.InProgress && Deck.Hand.Count == 0)
            {
                Deck.DrawNewHand();
            }

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound);
        }

        /// <summary>
        /// If <paramref name="trait"/> is present, stamps the grid cell(s) its
        /// effect touches with the matching golden/tinted/multiplier flag(s)
        /// just before <see cref="GridManager.PlacePiece"/> scores this
        /// placement — reusing the grid's existing modifier-scoring machinery
        /// for what is now a one-time, piece-carried enchantment (spec 5.4
        /// redesign) rather than a permanent cell property. Returns every cell
        /// that should be un-stamped again once scoring is done (see
        /// <see cref="ClearTokenTraitCells"/>) — every kind except
        /// <see cref="PieceTraitKind.Seeder"/> (whose stamp is meant to stay
        /// until the round itself resets it, see Cell.ResetForNewRound)
        /// and <see cref="PieceTraitKind.Mirror"/> (which stamps no cell at
        /// all; its bonus is computed after scoring, see
        /// <see cref="ApplyMirrorBonus"/>).
        /// </summary>
        private List<Cell> ApplyTokenTrait(PieceTrait? trait, Vector2Int? traitCellPos)
        {
            var transientCells = new List<Cell>();
            if (!trait.HasValue)
            {
                return transientCells;
            }

            var pos = traitCellPos.Value;
            var cell = Grid.GetCell(pos.x, pos.y);

            switch (trait.Value.Kind)
            {
                case PieceTraitKind.Golden:
                    cell.IsGolden = true;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Tinted:
                    cell.IsTinted = true;
                    cell.TintedColor = trait.Value.TintedColor.Value;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Multiplier:
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Blast:
                    cell.IsGolden = true;
                    transientCells.Add(cell);
                    StampGoldenIfInBounds(pos.x - 1, pos.y, transientCells);
                    StampGoldenIfInBounds(pos.x + 1, pos.y, transientCells);
                    StampGoldenIfInBounds(pos.x, pos.y - 1, transientCells);
                    StampGoldenIfInBounds(pos.x, pos.y + 1, transientCells);
                    break;

                case PieceTraitKind.Beacon:
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                    StampMultiplierAlongRowAndColumn(pos, transientCells);
                    break;

                case PieceTraitKind.Seeder:
                    // Intentionally NOT added to transientCells, so
                    // ClearTokenTraitCells never un-stamps it right after this
                    // placement's own scoring — unlike every other trait, this
                    // one keeps scoring as a normal golden grid cell for the
                    // rest of the CURRENT ROUND. It's Grid.ResetForNewRound
                    // (via Cell.ResetForNewRound), not this method, that
                    // eventually clears it — a permanent-for-the-whole-run
                    // golden cell was judged too powerful.
                    cell.IsGolden = true;
                    break;

                case PieceTraitKind.Mirror:
                    // No cell stamp — handled after Grid.PlacePiece returns,
                    // see ApplyMirrorBonus.
                    break;
            }
            return transientCells;
        }

        private void StampGoldenIfInBounds(int x, int y, List<Cell> transientCells)
        {
            if (!GridManager.InBounds(x, y))
            {
                return;
            }
            var cell = Grid.GetCell(x, y);
            cell.IsGolden = true;
            transientCells.Add(cell);
        }

        /// <summary>Marks every already-filled cell (pre-existing board state, not this placement's own cells) in <paramref name="origin"/>'s row and column as a multiplier zone, for "Multiplier Beacon".</summary>
        private void StampMultiplierAlongRowAndColumn(Vector2Int origin, List<Cell> transientCells)
        {
            for (int gx = 0; gx < GridManager.Size; gx++)
            {
                if (gx == origin.x)
                {
                    continue;
                }
                var cell = Grid.GetCell(gx, origin.y);
                if (cell.IsFilled)
                {
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                }
            }
            for (int gy = 0; gy < GridManager.Size; gy++)
            {
                if (gy == origin.y)
                {
                    continue;
                }
                var cell = Grid.GetCell(origin.x, gy);
                if (cell.IsFilled)
                {
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                }
            }
        }

        /// <summary>Reverts every stamp <see cref="ApplyTokenTrait"/> made — most enchantments fire once, on this placement's own scoring, not as a lasting grid modifier.</summary>
        private static void ClearTokenTraitCells(List<Cell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].IsGolden = false;
                cells[i].IsTinted = false;
                cells[i].IsMultiplierZone = false;
            }
        }

        /// <summary>
        /// "Mirror Tile": duplicates the enchanted cell's own group-bonus
        /// contribution onto the cell symmetrically opposite it in the scored
        /// group (reflected through the group's bounding-box center), if one
        /// exists there. Every cell in a placement's scored group earns the
        /// exact same per-cell amount (see GridManager.PlacePiece's
        /// perCellGroupScore), so any one Group-type event's Amount already IS
        /// the bonus to duplicate — no need to look up the trait cell's own
        /// event specifically. Mutates <paramref name="placement"/> directly
        /// (its fields are plain mutable ints/lists) so both the score total
        /// and the presentation layer's popup feed see the extra bonus.
        /// </summary>
        private static void ApplyMirrorBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            int perCellAmount = 0;
            var groupPositions = new HashSet<Vector2Int>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type != ScoreEventType.Group)
                {
                    continue;
                }
                groupPositions.Add(scoreEvent.Position);
                perCellAmount = scoreEvent.Amount;
                if (scoreEvent.Position.x < minX) minX = scoreEvent.Position.x;
                if (scoreEvent.Position.x > maxX) maxX = scoreEvent.Position.x;
                if (scoreEvent.Position.y < minY) minY = scoreEvent.Position.y;
                if (scoreEvent.Position.y > maxY) maxY = scoreEvent.Position.y;
            }

            if (groupPositions.Count == 0)
            {
                return;
            }

            var mirrorPos = new Vector2Int(minX + maxX - traitCellPos.x, minY + maxY - traitCellPos.y);
            if (mirrorPos == traitCellPos || !groupPositions.Contains(mirrorPos))
            {
                return;
            }

            placement.TraitBonus += perCellAmount;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.Trait, mirrorPos, perCellAmount));
            placement.ScoreEvents = events;
        }

        /// <summary>
        /// Debug-only helper (wired to an editor-only input shortcut in
        /// GameBootstrap): instantly completes the current round as if its
        /// quota had just been reached, so the upgrade draft appears right
        /// away instead of having to grind out a full round for real — handy
        /// for manually testing upgrades. No-op if the run isn't currently
        /// InProgress. Pure core logic (no UnityEditor dependency), so the
        /// method itself ships in real builds too; only its call site is
        /// gated behind #if UNITY_EDITOR.
        /// </summary>
        public RunState DebugForceRoundComplete()
        {
            if (State != RunState.InProgress)
            {
                return State;
            }
            RoundScore = CurrentQuota;
            EvaluateRoundEnd();
            return State;
        }

        private void EvaluateRoundEnd()
        {
            if (RoundScore >= CurrentQuota)
            {
                State = CurrentRoundIndex == RunConfig.RoundCount - 1 ? RunState.RunVictory : RunState.AwaitingDraft;
                return;
            }

            if (PiecesRemainingThisRound <= 0)
            {
                State = RunState.RunDefeat;
                return;
            }

            // An empty hand (PlacePiece defers its refill when the round
            // might be ending — see PlayFromHand's refillIfEmpty) has
            // nothing to evaluate yet, so it can never count as "stuck":
            // skip the check and let the round stay InProgress. PlacePiece's
            // own post-EvaluateRoundEnd check then draws the next hand right
            // away, which the NEXT placement will correctly check.
            if (Deck.Hand.Count > 0 && !HasAnyHandPlacement())
            {
                State = RunState.RunDefeat;
            }
        }

        /// <summary>True if at least one piece currently in hand, in its actual dealt rotation, can be legally placed somewhere on the grid.</summary>
        private bool HasAnyHandPlacement()
        {
            var hand = Deck.Hand;
            var rotations = Deck.HandRotations;
            var shapes = new List<PieceShape>(hand.Count);
            for (int i = 0; i < hand.Count; i++)
            {
                shapes.Add(PieceShapeCatalog.GetRotated(hand[i].Shape, rotations[i]));
            }
            return Grid.HasAnyValidPlacement(shapes);
        }

        public UpgradeDraft RollDraftOptions()
        {
            return Upgrades.RollDraft();
        }

        /// <summary>
        /// Applies the single drafted upgrade (tile and grid pools mixed
        /// together, spec 5.2) and moves on to the modifier pick rather than
        /// advancing the round directly — <see cref="ApplyModifierPick"/> (or
        /// <see cref="RemoveModifierAndAdvance"/> if the 5-slot cap is exceeded)
        /// does that. Only valid while <see cref="State"/> is
        /// <see cref="RunState.AwaitingDraft"/>.
        /// </summary>
        public bool ApplyUpgradeAndAdvance(UpgradeDefinition upgrade, UpgradeSubChoice subChoice)
        {
            if (State != RunState.AwaitingDraft)
            {
                return false;
            }

            bool applied = Upgrades.Apply(upgrade, subChoice, Deck);
            State = RunState.AwaitingModifierPick;
            return applied;
        }

        /// <summary>
        /// Rolls a modifier draft of up to <see cref="ModifierDraftSize"/>
        /// distinct options, drawn only from modifiers the player doesn't
        /// already hold — each modifier can only be active once per run.
        /// </summary>
        public ModifierDefinition[] RollModifierDraftOptions()
        {
            var available = new List<ModifierDefinition>(ModifierCatalog.All.Length);
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var candidate = ModifierCatalog.All[i];
                if (!_activeModifiers.Contains(candidate.Id))
                {
                    available.Add(candidate);
                }
            }
            return UpgradeSystem.PickDistinct(available, ModifierDraftSize, _rng);
        }

        /// <summary>
        /// Adds the picked modifier to the player's active set. If that pushes
        /// the count past <see cref="MaxActiveModifiers"/>, the round does not
        /// advance yet — <see cref="State"/> becomes
        /// <see cref="RunState.AwaitingModifierRemoval"/> and the player must
        /// call <see cref="RemoveModifierAndAdvance"/> next. Only valid while
        /// <see cref="State"/> is <see cref="RunState.AwaitingModifierPick"/>.
        /// </summary>
        public bool ApplyModifierPick(ModifierId modifierId)
        {
            if (State != RunState.AwaitingModifierPick)
            {
                return false;
            }

            _activeModifiers.Add(modifierId);
            if (_activeModifiers.Count > MaxActiveModifiers)
            {
                State = RunState.AwaitingModifierRemoval;
            }
            else
            {
                AdvanceRound();
            }
            return true;
        }

        /// <summary>
        /// Removes one active modifier (to get back down to the 5-slot cap) and
        /// advances to the next round. Only valid while <see cref="State"/> is
        /// <see cref="RunState.AwaitingModifierRemoval"/>.
        /// </summary>
        public bool RemoveModifierAndAdvance(ModifierId modifierId)
        {
            if (State != RunState.AwaitingModifierRemoval)
            {
                return false;
            }

            if (!_activeModifiers.Remove(modifierId))
            {
                return false;
            }

            AdvanceRound();
            return true;
        }

        private void AdvanceRound()
        {
            CurrentRoundIndex++;
            StartRound();
        }
    }
}
