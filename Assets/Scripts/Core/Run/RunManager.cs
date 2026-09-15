namespace Contigu.Core
{
    /// <summary>
    /// Orchestrates the 8-round run: quotas, piece budgets, boss round locking,
    /// and the win/lose transition into the upgrade draft (spec sections 1 and 6).
    ///
    /// Per spec 3.4, a round's success/failure is only evaluated once its piece
    /// budget is exhausted (not the instant the quota is first reached), so the
    /// player can keep placing budgeted pieces to pad their score.
    /// </summary>
    public sealed class RunManager
    {
        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

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
            var shape = PieceShapeCatalog.Get(token.Shape);

            if (!Grid.CanPlace(shape, x, y))
            {
                return new PlacementOutcome(PlacementResult.Failure("Invalid placement"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            var placement = Grid.PlacePiece(shape, token.Color, x, y);
            RoundScore += placement.TotalScore;
            TotalScore += placement.TotalScore;
            Deck.PlayFromHand(handIndex);
            PiecesRemainingThisRound--;

            EvaluateRoundEnd();

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound);
        }

        private void EvaluateRoundEnd()
        {
            if (PiecesRemainingThisRound > 0)
            {
                return;
            }

            if (RoundScore >= CurrentQuota)
            {
                State = CurrentRoundIndex == RunConfig.RoundCount - 1 ? RunState.RunVictory : RunState.AwaitingDraft;
            }
            else
            {
                State = RunState.RunDefeat;
            }
        }

        public UpgradeDraft RollDraftOptions()
        {
            return Upgrades.RollDraft();
        }

        /// <summary>
        /// Applies the drafted upgrade and advances to the next round. Only valid
        /// while <see cref="State"/> is <see cref="RunState.AwaitingDraft"/>.
        /// </summary>
        public bool ApplyUpgradeAndAdvance(UpgradeDefinition upgrade, UpgradeSubChoice subChoice)
        {
            if (State != RunState.AwaitingDraft)
            {
                return false;
            }

            bool applied = Upgrades.Apply(upgrade, subChoice, Grid, Deck);
            CurrentRoundIndex++;
            StartRound();
            return applied;
        }
    }
}
