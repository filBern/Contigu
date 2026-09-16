using System.Collections.Generic;

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

            var placement = Grid.PlacePiece(shape, token.Color, x, y, _activeModifiers);
            RoundScore += placement.TotalScore;
            TotalScore += placement.TotalScore;
            Deck.PlayFromHand(handIndex);
            PiecesRemainingThisRound--;

            EvaluateRoundEnd();

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound);
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

            if (!HasAnyHandPlacement())
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

            bool applied = Upgrades.Apply(upgrade, subChoice, Grid, Deck);
            State = RunState.AwaitingModifierPick;
            return applied;
        }

        /// <summary>Rolls a modifier draft of up to <see cref="ModifierDraftSize"/> distinct options.</summary>
        public ModifierDefinition[] RollModifierDraftOptions()
        {
            return UpgradeSystem.PickDistinct(ModifierCatalog.All, ModifierDraftSize, _rng);
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
