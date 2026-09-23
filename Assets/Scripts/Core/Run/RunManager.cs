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
        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

        private readonly List<ModifierId> _activeModifiers = new List<ModifierId>();

        /// <summary>Modifiers currently held by the player, persisting for the whole run (never reset between rounds). Capped at EconomyConstants.MaxActiveModifiers now that the shop lets Lueur buy them far more freely than the old one-per-round draft ever could.</summary>
        public IReadOnlyList<ModifierId> ActiveModifiers
        {
            get { return _activeModifiers; }
        }

        /// <summary>
        /// Swaps the modifiers at two positions in <see
        /// cref="ActiveModifiers"/> — the "tap 2 modifiers to swap them"
        /// reordering gesture (see Presentation.ModifierPanelView), on
        /// explicit request: modifier order now determines scoring order
        /// (see PlacementResult.Mult's ordered fold), and the player needs
        /// a way to arrange it — "mettre les x après les +". A no-op for an
        /// out-of-range or identical pair.
        /// </summary>
        public void SwapModifiers(int indexA, int indexB)
        {
            if (!IsValidModifierIndex(indexA) || !IsValidModifierIndex(indexB) || indexA == indexB)
            {
                return;
            }

            var temp = _activeModifiers[indexA];
            _activeModifiers[indexA] = _activeModifiers[indexB];
            _activeModifiers[indexB] = temp;
        }

        /// <summary>
        /// Moves the modifier at <paramref name="fromIndex"/> to <paramref
        /// name="toIndex"/>, shifting every modifier in between by one
        /// position — the drag-and-drop reordering gesture (see
        /// Presentation.ModifierPanelView), sibling of <see
        /// cref="SwapModifiers"/> above but a true re-insertion rather than
        /// a 2-way swap, matching how dragging a card into a new slot
        /// behaves everywhere else in the game (e.g. HandView). A no-op for
        /// an out-of-range or identical pair.
        /// </summary>
        public void MoveModifier(int fromIndex, int toIndex)
        {
            if (!IsValidModifierIndex(fromIndex) || !IsValidModifierIndex(toIndex) || fromIndex == toIndex)
            {
                return;
            }

            var moved = _activeModifiers[fromIndex];
            _activeModifiers.RemoveAt(fromIndex);
            _activeModifiers.Insert(toIndex, moved);
        }

        private bool IsValidModifierIndex(int index)
        {
            return index >= 0 && index < _activeModifiers.Count;
        }

        /// <summary>The last REAL modifier actually added by a shop purchase this run (never Copieur/Mimic itself, see BuyModifierSlot) — null until the player's first purchase. "Mimic" (Copieur) reads this to decide which modifier it copies.</summary>
        private ModifierId? _lastPurchasedModifierId;

        /// <summary>
        /// "Lueur" currency (spec extension, explicit request — a Balatro-
        /// style economy): earned by clearing lines with DIVERSE colors (see
        /// GridManager.PlacementResult.LueurEarned), persists for the whole
        /// run like <see cref="TotalScore"/>, and spent in the between-round
        /// shop (see <see cref="ShopModifierSlots"/>/<see cref="ShopUpgradeSlots"/>).
        /// </summary>
        public int Lueur { get; private set; }

        private readonly ShopSlot[] _modifierSlots = new ShopSlot[EconomyConstants.ShopModifierSlotCount];
        private readonly ShopSlot[] _upgradeSlots = new ShopSlot[EconomyConstants.ShopUpgradeSlotCount];

        /// <summary>How many purchases (slot buys AND rerolls) have happened in the CURRENT shop visit — every one raises the price of everything else still on offer (see GetSlotPrice/GetRerollPrice), reset to 0 each time the shop opens.</summary>
        private int _purchasesThisVisit;

        public IReadOnlyList<ShopSlot> ShopModifierSlots
        {
            get { return _modifierSlots; }
        }

        public IReadOnlyList<ShopSlot> ShopUpgradeSlots
        {
            get { return _upgradeSlots; }
        }

        /// <summary>
        /// Non-null while a purchased Upgrade slot still needs a follow-up
        /// from the player before the shop can be used for anything else —
        /// a sub-choice (Bank pool: which piece type, and for Recolorer
        /// which target color) or a tile choice (Grid pool: which of
        /// <see cref="PendingUpgradeTileCandidates"/> receive the trait).
        /// Cleared once <see cref="ResolveUpgradeSubChoice"/> or
        /// <see cref="ResolveUpgradeTileChoice"/> succeeds.
        /// </summary>
        public UpgradeDefinition PendingUpgrade { get; private set; }

        /// <summary>Candidate deck token indices for <see cref="PendingUpgrade"/> — only populated (non-empty) when it's a Grid-pool upgrade; empty for a Bank-pool one, which needs a sub-choice instead.</summary>
        public IReadOnlyList<int> PendingUpgradeTileCandidates { get; private set; }

        /// <summary>Candidate piece TYPES for <see cref="PendingUpgrade"/>'s sub-choice — only populated (non-empty) for a Bank-pool upgrade that needs one (Retirer/Dupliquer/Recolorer); empty otherwise. Capped the same way PendingUpgradeTileCandidates is, so the type picker never lists the whole deck composition at once.</summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> PendingUpgradeTypeCandidates { get; private set; }

        /// <summary>The shape most recently rolled by a Joker purchase (see BuyUpgradeSlot/UpgradeSystem.ApplyJoker) — read once by the presentation layer (UpgradeRevealView) right after the purchase to show the real piece that got added instead of just describing the upgrade in text. Meaningless before any Joker purchase this run.</summary>
        public ShapeId LastJokerShapeAdded { get; private set; }

        /// <summary>How many times each modifier has actually fired (scored at least one point) so far this run — see <see cref="CountModifierUsage"/>. Read via <see cref="GetModifierUsageCount"/>.</summary>
        private readonly Dictionary<ModifierId, int> _modifierUsageCounts = new Dictionary<ModifierId, int>();

        /// <summary>How many times <paramref name="id"/> has fired this run (0 if never, or not currently held).</summary>
        public int GetModifierUsageCount(ModifierId id)
        {
            return _modifierUsageCounts.TryGetValue(id, out var count) ? count : 0;
        }

        /// <summary>How many pieces carrying a trait ("special" pieces) have been PLACED so far this run — Experience's driver, the played-count counterpart to CountUpgradedDeckCards' "currently in deck" count. Permanent for the whole run, same as Gradient's counter (nothing in the spec calls for resetting it, and it isn't tied to round-scoped grid state the way Epuisement is).</summary>
        private int _specialPiecesPlayedCount;

        /// <summary>
        /// The CURRENT effective state of a progressive/incremental modifier,
        /// formatted for its tooltip (on explicit request: "Tous les
        /// modifiers avec des bonus incrémentaux, il faut afficher dans le
        /// tooltip l'état progressif du modifier (ex: Currently x2.3)") —
        /// null for every modifier whose bonus is fixed and doesn't grow or
        /// shrink over the round/run (that's most of them). Modifiers whose
        /// counter is a genuine whole number (Gradient, Repetition,
        /// Solidarite, Epuisement, Multitude) show a plain integer — Repetition's
        /// is a PREVIEW of what its own NEXT placement would score if it kept
        /// the streak alive (see GridManager.RepetitionCurrentMultiplier),
        /// unlike the others here which report what their OWN LAST
        /// placement already applied; Densite
        /// shows its TRUE float multiplicative factor now actually applied
        /// to the score (see PlacementResult.ProgressiveMultiplier — on
        /// explicit request, no longer rounded down mid-calculation: "on
        /// doit multiplier comme si c'était un float"). CartesEnchantees/
        /// Experience are ADDITIVE contributors to the SAME "+Mult" pool as
        /// Solidarite/MultUn (see PlacementResult.ProgressiveAdditiveMult),
        /// not a multiplier of their own, so they show "+N Mult" the same
        /// way Solidarite does, NOT "xN" — showing "x" here was a bug (on
        /// explicit report: "tu as oublié la baseline de 1 et non de 0"):
        /// their own raw contribution starts at +0.1 (never +0, "counting
        /// from a baseline of 1" card), which read as a NERF ("x0.1") under
        /// the old "x" phrasing instead of the bonus it actually is — the
        /// separate "+1" that makes the OVERALL Mult never drop below x1 is
        /// PlacementResult.Mult's own baseline, added once, game-wide, not
        /// specific to either of these two modifiers.
        /// </summary>
        public string GetProgressiveModifierStateText(ModifierId id)
        {
            switch (id)
            {
                case ModifierId.Gradient:
                    return "Currently x" + Grid.GradientCurrentMultiplier;
                case ModifierId.Repetition:
                    return "Currently x" + Grid.RepetitionCurrentMultiplier;
                case ModifierId.Densite:
                    return "Currently x" + FormatMultDisplay(Mathf.Max(1f, Grid.FilledCellCount / (float)ScoringConstants.DensiteFilledCellsPerMultiplierStep));
                case ModifierId.Epuisement:
                    return "Currently +" + Grid.EpuisementCurrentBonus + " pts";
                case ModifierId.Solidarite:
                    return "Currently +" + _activeModifiers.Count + " Mult";
                case ModifierId.Multitude:
                    return "Currently +" + (Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard) + " pts";
                case ModifierId.CartesEnchantees:
                    return "Currently +" + FormatMultDisplay((1 + CountUpgradedDeckCards()) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep) + " Mult";
                case ModifierId.Experience:
                    return "Currently +" + FormatMultDisplay((1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep) + " Mult";
                default:
                    return null;
            }
        }

        /// <summary>Whole number when <paramref name="value"/> is (near enough) an integer, one decimal otherwise — same conditional formatting as ComboView's mult pill, so the tooltip and the in-placement popups never disagree on how a given value reads.</summary>
        private static string FormatMultDisplay(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < 0.05f)
            {
                return Mathf.RoundToInt(value).ToString();
            }
            // Invariant culture — "F1" would otherwise render with a comma
            // decimal separator ("2,3") under a French system locale, and
            // this string is also parsed back out nowhere, so there's no
            // reason to let it vary.
            return value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly IRandomProvider _rng;
        private readonly ChallengeDefinition _challenge;

        /// <summary>The challenge this run was started with (see ChallengeCatalog) — read by Presentation for the boss-round status text and to show which challenge is currently in play.</summary>
        public ChallengeDefinition Challenge
        {
            get { return _challenge; }
        }

        /// <summary>0-based index into <see cref="ChallengeDefinition"/>'s Quotas/PieceBudgets arrays.</summary>
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
            get { return _challenge.Quotas[CurrentRoundIndex]; }
        }

        public int CurrentBudget
        {
            get { return _challenge.PieceBudgets[CurrentRoundIndex]; }
        }

        /// <summary>Classic/Marathon: only the last round. Chaos: every round (see ChallengeDefinition.BossActiveEveryRound).</summary>
        public bool IsBossRound
        {
            get { return _challenge.BossActiveEveryRound || CurrentRoundIndex == _challenge.BossRoundIndex; }
        }

        /// <summary><paramref name="challenge"/> defaults to ChallengeCatalog.Classic (the original run) when omitted — keeps every existing call site (tests included) on the standard rules without having to pass one explicitly.</summary>
        public RunManager(IRandomProvider rng, ChallengeDefinition challenge = null)
        {
            _rng = rng;
            _challenge = challenge ?? ChallengeCatalog.Classic;
            Grid = new GridManager();
            var startingDeck = _challenge.Id == ChallengeId.Marathon ? InitialDeckFactory.BuildMarathon() : InitialDeckFactory.Build();
            Deck = new DeckManager(startingDeck, rng);
            Upgrades = new UpgradeSystem(rng);
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            CurrentRoundIndex = 0;
            StartRound();
        }

        private void StartRound()
        {
            Grid.ResetForNewRound();
            // No more upfront lock here — the boss round now ratchets up
            // gradually instead, see ApplyBossLockTick (called from
            // PlacePiece every _challenge.BossLockPiecesInterval pieces).
            // Normally a no-op (the hand carries over from the previous
            // round untouched) — only fires for the deferred draw PlacePiece
            // skips when the placement that empties the hand also ends the
            // round, so the fresh hand is drawn here, for the round it
            // actually belongs to, rather than during the previous round's
            // tail end before the player has even picked their upgrade.
            if (Deck.IsHandFullyEmpty())
            {
                Deck.DrawNewHand();
            }
            RoundScore = 0;
            PiecesRemainingThisRound = CurrentBudget;
            State = RunState.InProgress;

            // Same stuck-check PlacePiece runs after a mid-round redraw (see
            // there) — covers the rare case of a boss round's locked cells
            // leaving zero legal placements for the very first hand of the
            // round, which would otherwise go undetected until the player
            // gave up trying.
            EvaluateRoundEnd();
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

            if (handIndex < 0 || handIndex >= DeckManager.HandSize || !Deck.Hand[handIndex].HasValue)
            {
                return new PlacementOutcome(PlacementResult.Failure("Invalid hand index"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            var token = Deck.Hand[handIndex].Value;
            var rotation = Deck.HandRotations[handIndex];
            var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);

            if (!Grid.CanPlace(shape, x, y))
            {
                return new PlacementOutcome(PlacementResult.Failure("Invalid placement"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            Vector2Int? traitCellPos = token.Trait.HasValue
                ? new Vector2Int(x, y) + shape.Cells[token.Trait.Value.LocalCellIndex]
                : (Vector2Int?)null;

            // Two traits need grid state read BEFORE Grid.PlacePiece mutates
            // it: Chameleon overrides the color the piece actually places
            // as, and Spark needs the no-clear streak as it stood before
            // this placement (Grid.PlacePiece itself updates that streak for
            // the NEXT placement to read, so reading it any later would see
            // this placement's own outcome instead of the one it's scoring
            // against).
            PieceColor placementColor = token.Color;
            int sparkStreakBeforePlacement = 0;
            if (token.Trait.HasValue)
            {
                // Experience's driver — every trait kind counts as "special",
                // incremented here regardless of which branch below fires
                // (on explicit request: "un modifier +0.1 mult pour chaque
                // carte spéciale joué").
                _specialPiecesPlayedCount++;
                var kind = token.Trait.Value.Kind;
                if (kind == PieceTraitKind.Chameleon)
                {
                    placementColor = ResolveChameleonColor(shape, x, y, traitCellPos.Value, token.Color);
                    if (placementColor != token.Color)
                    {
                        // "Si une pièce est recolorée, elle est recolorée
                        // dans le deck aussi" — see DeckManager.RecolorHandToken.
                        // Must run before Deck.PlayFromHand below clears this
                        // slot (harmless either way — PlayFromHand never
                        // touches the deck itself — but this is the one spot
                        // that still has both handIndex and the resolved
                        // color in scope together).
                        Deck.RecolorHandToken(handIndex, placementColor);
                    }
                }
                else if (kind == PieceTraitKind.Spark)
                {
                    sparkStreakBeforePlacement = Grid.PlacementsSinceLastClear;
                }
            }

            var transientTraitCells = ApplyTokenTrait(token.Trait, traitCellPos);
            var placement = Grid.PlacePiece(shape, placementColor, x, y, _activeModifiers);
            ClearTokenTraitCells(transientTraitCells);
            if (token.Trait.HasValue)
            {
                ApplyPostPlacementTraitBonus(token.Trait.Value, traitCellPos.Value, sparkStreakBeforePlacement, placement);
            }
            ApplyHandSlotModifierBonus(handIndex, placement);
            ApplyDeckStateModifierBonuses(placement);
            CountModifierUsage(placement);
            RoundScore += placement.TotalScore;
            TotalScore += placement.TotalScore;
            // ModifierLueurBonus is a second, independent source of Lueur
            // (see PlacementResult.ModifierLueurBonus) — the 5 Lueur-earning
            // modifiers, on top of the line-clearing LueurEarned above.
            Lueur += placement.LueurEarned + placement.ModifierLueurBonus;
            // Don't auto-refill yet — if this placement also ends the round,
            // drawing the next 3 pieces here would hand them out before the
            // player has even picked this round's upgrade (see StartRound,
            // which draws instead in that case).
            Deck.PlayFromHand(handIndex, refillIfEmpty: false);
            PiecesRemainingThisRound--;

            IReadOnlyList<Vector2Int> bossLockedCells = System.Array.Empty<Vector2Int>();
            if (IsBossRound)
            {
                int piecesPlayedThisRound = CurrentBudget - PiecesRemainingThisRound;
                if (piecesPlayedThisRound > 0 && piecesPlayedThisRound % _challenge.BossLockPiecesInterval == 0)
                {
                    bossLockedCells = ApplyBossLockTick();
                }
            }

            EvaluateRoundEnd();

            if (State == RunState.InProgress && Deck.IsHandFullyEmpty())
            {
                Deck.DrawNewHand();
                // EvaluateRoundEnd's stuck-check above deliberately skips an
                // EMPTY hand (nothing to evaluate yet) — but the fresh hand
                // just drawn is no longer empty, and might itself have no
                // legal placement anywhere on the board. Without this
                // second check, that stuck state went undetected entirely
                // (bug report: "je ne peux pas jouer de tuile et pourtant
                // je n'ai pas perdu") until the player tried a placement,
                // which never comes since none is legal.
                EvaluateRoundEnd();
            }

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound, bossLockedCells);
        }

        /// <summary>
        /// Boss round mechanic (see ChallengeDefinition.BossLockPiecesInterval):
        /// locks _challenge.BossLockCellsPerInterval more random empty cells and
        /// folds in any score that locking happens to produce (see
        /// GridManager.LockFreeCellsAndCheckClears) — "après avoir compté
        /// les bonus" this placement's own score is already in RoundScore/
        /// TotalScore by the time this runs, so the lock's score is simply
        /// added on top of it, same as any other placement. Returns the
        /// newly locked cells so the presentation layer can refresh their
        /// visuals.
        /// </summary>
        private IReadOnlyList<Vector2Int> ApplyBossLockTick()
        {
            var lockOutcome = Grid.LockFreeCellsAndCheckClears(_challenge.BossLockCellsPerInterval, _rng);
            if (lockOutcome.LineClearScore > 0)
            {
                RoundScore += lockOutcome.LineClearScore;
                TotalScore += lockOutcome.LineClearScore;
            }
            Lueur += lockOutcome.LueurEarned;
            return lockOutcome.LockedCells;
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
        /// until the round itself resets it, see Cell.ResetForNewRound) and
        /// the second/third-batch kinds (Mirror, Catalyst, Twin,
        /// Detonator, Chameleon, Spark, Void, Bastion, Kamikaze), none of
        /// which stamp a cell at all — their effects are resolved either
        /// before Grid.PlacePiece runs (Chameleon) or after it returns, from
        /// the resulting PlacementResult/grid state (see
        /// <see cref="ApplyPostPlacementTraitBonus"/>).
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

            // Cosmetic, persists for the rest of the round regardless of
            // trait kind — not added to transientCells, so ClearTokenTraitCells
            // never touches it (see Cell.OriginTrait).
            cell.OriginTrait = trait.Value;

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
                case PieceTraitKind.Catalyst:
                case PieceTraitKind.Twin:
                case PieceTraitKind.Detonator:
                case PieceTraitKind.Chameleon:
                case PieceTraitKind.Spark:
                case PieceTraitKind.Void:
                case PieceTraitKind.Bastion:
                case PieceTraitKind.Kamikaze:
                    // None of these stamp a Cell flag before placement —
                    // every one of them is either resolved before
                    // Grid.PlacePiece runs (Chameleon, via placementColor
                    // above) or computed after it returns, from the
                    // resulting PlacementResult/grid state (see
                    // ApplyPostPlacementTraitBonus). Bastion in particular
                    // can't lock its cell yet: Grid.PlacePiece's own
                    // internal CanPlace re-check would then see this
                    // placement's own cell as already locked and reject it.
                    break;
            }
            return transientCells;
        }

        /// <summary>
        /// "Chameleon Tile": resolves the color the WHOLE piece should place
        /// as — of every already-filled orthogonal neighbor color around the
        /// enchanted cell, picks whichever would make this placement's own
        /// resulting group score the most (explicit request: "il devrait
        /// être jumelé avec le groupe faisant le plus de points", same
        /// treatment as Joker — see GridManager.ResolveBestJokerGroup),
        /// instead of just the first one found in a fixed scan order (left,
        /// right, down, up). Falls back to the piece's own color when no
        /// neighbor is filled yet (checked before Grid.PlacePiece runs, so
        /// only PRE-EXISTING board state can match — never another cell of
        /// this same about-to-be-placed piece).
        /// </summary>
        private PieceColor ResolveChameleonColor(PieceShape shape, int anchorX, int anchorY, Vector2Int traitCellPos, PieceColor fallbackColor)
        {
            var candidates = new List<PieceColor>();
            AddDistinctNeighborColor(traitCellPos.x - 1, traitCellPos.y, candidates);
            AddDistinctNeighborColor(traitCellPos.x + 1, traitCellPos.y, candidates);
            AddDistinctNeighborColor(traitCellPos.x, traitCellPos.y - 1, candidates);
            AddDistinctNeighborColor(traitCellPos.x, traitCellPos.y + 1, candidates);

            if (candidates.Count == 0)
            {
                return fallbackColor;
            }
            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            PieceColor bestColor = candidates[0];
            int bestScore = Grid.PreviewGroupScore(shape, candidates[0], anchorX, anchorY);
            for (int i = 1; i < candidates.Count; i++)
            {
                int score = Grid.PreviewGroupScore(shape, candidates[i], anchorX, anchorY);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestColor = candidates[i];
                }
            }
            return bestColor;
        }

        private void AddDistinctNeighborColor(int x, int y, List<PieceColor> candidates)
        {
            var color = TryGetFilledNeighborColor(x, y);
            if (color.HasValue && !candidates.Contains(color.Value))
            {
                candidates.Add(color.Value);
            }
        }

        private PieceColor? TryGetFilledNeighborColor(int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return null;
            }
            var cell = Grid.GetCell(x, y);
            return cell.IsFilled ? cell.FilledColor : null;
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
        /// Handles every trait kind whose bonus can't be computed by
        /// GridManager's own per-cell scoring loop (it doesn't know about
        /// PieceTrait): Mirror/Catalyst/Twin need the group as it stood right
        /// after scoring, Detonator needs the line-clear outcome, Spark needs
        /// the pre-placement no-clear streak captured earlier in PlacePiece,
        /// and Void mutates the grid outside this placement's own cells
        /// entirely (no score of its own). Golden/Tinted/Multiplier/Blast/
        /// Beacon/Seeder/Chameleon all resolve elsewhere (Cell-flag stamping
        /// or, for Chameleon, ResolveChameleonColor) and need nothing here.
        /// </summary>
        private void ApplyPostPlacementTraitBonus(PieceTrait trait, Vector2Int traitCellPos, int sparkStreakBeforePlacement, PlacementResult placement)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Mirror:
                    ApplyMirrorBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Catalyst:
                    ApplyCatalystBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Twin:
                    ApplyTwinBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Detonator:
                    ApplyDetonatorBonus(placement);
                    break;
                case PieceTraitKind.Spark:
                    ApplySparkBonus(sparkStreakBeforePlacement, placement);
                    break;
                case PieceTraitKind.Void:
                    ApplyVoidEffect(placement);
                    break;
                case PieceTraitKind.Bastion:
                    ApplyBastionEffect(traitCellPos, placement);
                    break;
                case PieceTraitKind.Kamikaze:
                    ApplyKamikazeEffect(traitCellPos, placement);
                    break;
            }
        }

        /// <summary>Adds a flat trait bonus to <paramref name="placement"/> and appends a matching <see cref="ScoreEventType.Trait"/> event, mutating it directly (its fields are plain mutable ints/lists) so both the score total and the presentation layer's popup feed see it.</summary>
        private static void AddTraitBonus(PlacementResult placement, Vector2Int pos, int bonus)
        {
            placement.TraitBonus += bonus;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.Trait, pos, bonus));
            placement.ScoreEvents = events;
        }

        // Which SlotUn/Deux/Trois modifier corresponds to each 0-based hand index.
        private static readonly ModifierId?[] HandSlotModifiers = { ModifierId.SlotUn, ModifierId.SlotDeux, ModifierId.SlotTrois };

        /// <summary>
        /// "Slot N Loyalty": xN multiplier (see ScoringConstants.SlotLoyaltyMultiplier)
        /// on this placement's ENTIRE score when the piece was played from hand
        /// slot <paramref name="handIndex"/> (0-based) and the matching modifier
        /// is active — was "doubles just the group bonus", changed to double
        /// everything on explicit request ("au lieu de double group placement,
        /// on va tout doubler"), so it now multiplies the same
        /// PlacementResult.ModifierMultiplier field GridManager's own xN
        /// modifiers use instead of adding to ModifierBonus. Unlike every other
        /// modifier, GridManager.PlacePiece can't evaluate this itself — it has
        /// no idea which of the 3 hand slots a piece came from, only this
        /// method's caller (PlacePiece(handIndex, x, y)) does — so it's
        /// resolved here, the same post-hoc pattern already used for the
        /// second-batch PieceTrait kinds (see ApplyPostPlacementTraitBonus).
        /// </summary>
        private void ApplyHandSlotModifierBonus(int handIndex, PlacementResult placement)
        {
            if (handIndex < 0 || handIndex >= HandSlotModifiers.Length)
            {
                return;
            }

            var slotModifier = HandSlotModifiers[handIndex];
            if (!slotModifier.HasValue || !_activeModifiers.Contains(slotModifier.Value))
            {
                return;
            }

            placement.ModifierMultiplier *= ScoringConstants.SlotLoyaltyMultiplier;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            var scoreEvent = new ScoreEvent(ScoreEventType.ModifierMultiplier, placement.PlacedCells[0], ScoringConstants.SlotLoyaltyMultiplier);
            scoreEvent.TriggeringModifier = slotModifier.Value;
            // Its own position in _activeModifiers — needed so PlacementResult.Mult's
            // ordered left-to-right fold (see its own doc comment) places this
            // correctly relative to every other Mult modifier instead of
            // always applying it last regardless of where the player put it.
            scoreEvent.TriggeringModifierIndex = _activeModifiers.IndexOf(slotModifier.Value);
            events.Add(scoreEvent);
            placement.ScoreEvents = events;
        }

        /// <summary>
        /// Enchanted Cards (CartesEnchantees), Multitude (ninth batch) and
        /// Experience (tenth batch, its "played" counterpart to Enchanted
        /// Cards' "currently in deck" count) all depend on RunManager-only
        /// state (upgraded-card count, total deck size, special-pieces-
        /// played count), not grid/placement state — like
        /// ApplyHandSlotModifierBonus above, GridManager can't evaluate
        /// these itself since it knows nothing about the deck (or this
        /// counter), so they're resolved here instead, the same post-hoc
        /// pattern. Loops over every held copy of each (rather than just
        /// checking Contains) so holding any one of them more than once
        /// stacks, same convention as every other modifier.
        /// </summary>
        private void ApplyDeckStateModifierBonuses(PlacementResult placement)
        {
            if (!_activeModifiers.Contains(ModifierId.CartesEnchantees) && !_activeModifiers.Contains(ModifierId.Multitude) && !_activeModifiers.Contains(ModifierId.Experience))
            {
                return;
            }

            var events = new List<ScoreEvent>(placement.ScoreEvents);
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i] == ModifierId.CartesEnchantees)
                {
                    int upgradedCount = CountUpgradedDeckCards();
                    // TRUE float — no longer floored to a whole "+1 Mult"
                    // step (on explicit request: "on doit multiplier comme
                    // si c'était un float au lieu d'arrondir a la baisse").
                    // Always at least 0.1 ("counting from a baseline of
                    // 1"), so this always fires, and PreciseAmount lets the
                    // badge popup show the exact value (e.g. "+1.3") instead
                    // of a misleadingly rounded "+1" (on explicit report:
                    // "le popup de score qui apparait est un int et non un
                    // float").
                    float trueMult = (1 + upgradedCount) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep;
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.CartesEnchantees;
                    // Its own position in _activeModifiers — see the same
                    // stamp in ApplyHandSlotModifierBonus for why (feeds
                    // PlacementResult.Mult's ordered fold).
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Multitude)
                {
                    int bonus = Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard;
                    placement.ModifierBonus += bonus;
                    var ptsEvent = new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus);
                    ptsEvent.TriggeringModifier = ModifierId.Multitude;
                    ptsEvent.TriggeringModifierIndex = i;
                    events.Add(ptsEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Experience)
                {
                    // Same TRUE-float treatment as CartesEnchantees above.
                    float trueMult = (1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep;
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.Experience;
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
            }
            placement.ScoreEvents = events;
        }

        /// <summary>How many tokens in the whole deck (any pile) currently carry a PieceTrait — the "upgraded card" count Enchanted Cards scales with.</summary>
        private int CountUpgradedDeckCards()
        {
            int count = 0;
            var deck = Deck.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Trait.HasValue)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Tallies every <see cref="ScoreEventType.Modifier"/> AND <see
        /// cref="ScoreEventType.ModifierMultiplier"/> event in this
        /// placement's final <see cref="PlacementResult.ScoreEvents"/> — called
        /// last, after every modifier bonus (GridManager's own plus the
        /// hand-slot ones added above) has already been appended, so it sees
        /// the complete list regardless of which method actually produced
        /// each event. Powers the "used N times" tooltip stat (see
        /// GetModifierUsageCount) — a modifier only counts as "used" the
        /// instant it actually scores, not just while merely held. Counts
        /// ModifierMultiplier too since converting a modifier from a flat
        /// bonus to a multiplier (see PlacementResult.ModifierMultiplier)
        /// shouldn't silently stop it from ever incrementing this stat again.
        /// </summary>
        private void CountModifierUsage(PlacementResult placement)
        {
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                bool isModifierEvent = scoreEvent.Type == ScoreEventType.Modifier || scoreEvent.Type == ScoreEventType.ModifierMultiplier || scoreEvent.Type == ScoreEventType.LueurBonus || scoreEvent.Type == ScoreEventType.MultBonus;
                if (!isModifierEvent || !scoreEvent.TriggeringModifier.HasValue)
                {
                    continue;
                }

                var id = scoreEvent.TriggeringModifier.Value;
                _modifierUsageCounts.TryGetValue(id, out var count);
                _modifierUsageCounts[id] = count + 1;
            }
        }

        /// <summary>
        /// The scored group's total cell count, and the SPECIFIC amount the
        /// cell at <paramref name="cellPos"/> itself earned (0 if that
        /// position isn't part of the scored group) — group scoring is now
        /// progressive (the Nth cell scored is worth N*GroupBonusPerCell,
        /// not a flat shared amount, see GridManager.PlacePiece's group
        /// loop), so unlike before, a specific cell's own share can no
        /// longer be read off ANY Group event; it has to be the one at
        /// that exact position.
        /// </summary>
        private static void GetGroupShare(PlacementResult placement, Vector2Int cellPos, out int groupSize, out int cellAmount)
        {
            groupSize = 0;
            cellAmount = 0;
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type != ScoreEventType.Group)
                {
                    continue;
                }
                groupSize++;
                if (scoreEvent.Position == cellPos)
                {
                    cellAmount = scoreEvent.Amount;
                }
            }
        }

        /// <summary>
        /// "Mirror Tile": duplicates the enchanted cell's own group-bonus
        /// share onto ONE random OTHER cell in the scored group. Simplified
        /// (on explicit request — the original geometric-symmetry rule,
        /// "the cell reflected through the group's bounding-box center, if
        /// one exists there", was too hard to reason about at a glance) from
        /// a rule that only fired for specific symmetric group shapes into
        /// one that always fires whenever the group has another cell to
        /// target, same firing condition as Twin Tile. No-op if the group is
        /// just this placement's own cell.
        /// </summary>
        private void ApplyMirrorBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out _, out int enchantedCellAmount);
            var otherPositions = new List<Vector2Int>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type == ScoreEventType.Group && scoreEvent.Position != traitCellPos)
                {
                    otherPositions.Add(scoreEvent.Position);
                }
            }

            if (otherPositions.Count == 0)
            {
                return;
            }

            var targetPos = otherPositions[_rng.Next(otherPositions.Count)];
            AddTraitBonus(placement, targetPos, enchantedCellAmount);
        }

        /// <summary>"Catalyst Tile": scores extra points for every cell in this placement's scored group that was already on the grid before this placement — the group's size (from GetGroupShare) minus this piece's own cell count.</summary>
        private static void ApplyCatalystBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out int groupSize, out _);
            int existingCells = groupSize - placement.PlacedCells.Count;
            if (existingCells <= 0)
            {
                return;
            }

            int bonus = existingCells * ScoringConstants.CatalystBonusPerExistingCell;
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Twin Tile": like Mirror, but duplicates the enchanted cell's own group-bonus share onto EVERY other cell in the group, not just one at random — total extra is that specific cell's own amount times (group size - 1).</summary>
        private static void ApplyTwinBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out int groupSize, out int enchantedCellAmount);
            if (groupSize < 2)
            {
                return;
            }

            int bonus = enchantedCellAmount * (groupSize - 1);
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Detonator Tile": doubles this placement's ENTIRE line-clear bonus, if it clears at least one row/column.</summary>
        private static void ApplyDetonatorBonus(PlacementResult placement)
        {
            if (placement.LineClearScore <= 0)
            {
                return;
            }
            AddTraitBonus(placement, placement.PlacedCells[0], placement.LineClearScore);
        }

        /// <summary>"Spark Tile": scores more the longer it's been since the last line/column clear this round — <paramref name="streakBeforePlacement"/> is the streak as captured in PlacePiece BEFORE Grid.PlacePiece ran, so this placement's own clear (if any) doesn't erase the streak it's scoring against.</summary>
        private static void ApplySparkBonus(int streakBeforePlacement, PlacementResult placement)
        {
            if (streakBeforePlacement <= 0)
            {
                return;
            }
            int bonus = streakBeforePlacement * ScoringConstants.SparkBonusPerPlacement;
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Void Tile": clears one random already-filled, unlocked cell elsewhere on the grid — excludes this placement's own cells (only pre-existing board state is eligible). Pure risk/utility, no score of its own; a no-op if nothing else on the grid is eligible.</summary>
        private void ApplyVoidEffect(PlacementResult placement)
        {
            Grid.ClearRandomFilledCell(_rng, placement.PlacedCells);
        }

        /// <summary>
        /// "Bastion Tile" (spec extension, explicit request — "Locked cell
        /// upgraded. N'est pas cleared mais fait quand même les points
        /// cleared"): once this placement itself has fully resolved (its own
        /// line clears included), the enchanted cell locks in place for the
        /// rest of the round — GridManager.CheckAndClearLines then skips it
        /// forever after, but still credits it the line-clear bonus every
        /// time its row/column completes. No-op if this same placement's own
        /// line clear already wiped the cell before we got here (nothing
        /// left to lock).
        /// </summary>
        private void ApplyBastionEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            var cell = Grid.GetCell(traitCellPos);
            if (!cell.IsFilled)
            {
                return;
            }
            cell.IsBastion = true;
            cell.IsLocked = true;
        }

        /// <summary>
        /// "Kamikaze Tile": destroys every already-filled, unlocked cell in
        /// the enchanted cell's 8 surrounding tiles (Moore neighborhood) —
        /// this placement's own cells are excluded, same "protect what was
        /// just placed" convention as Void Tile — scoring
        /// ScoringConstants.KamikazeBonusPerDestroyedCell per tile actually
        /// destroyed.
        /// </summary>
        private void ApplyKamikazeEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            int destroyed = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int x = traitCellPos.x + dx;
                    int y = traitCellPos.y + dy;
                    if (!GridManager.InBounds(x, y))
                    {
                        continue;
                    }

                    var pos = new Vector2Int(x, y);
                    var cell = Grid.GetCell(pos);
                    if (!cell.IsFilled || cell.IsLocked || ContainsCell(placement.PlacedCells, pos))
                    {
                        continue;
                    }

                    cell.ClearFill();
                    destroyed++;
                }
            }

            if (destroyed > 0)
            {
                AddTraitBonus(placement, traitCellPos, destroyed * ScoringConstants.KamikazeBonusPerDestroyedCell);
            }
        }

        private static bool ContainsCell(IReadOnlyList<Vector2Int> cells, Vector2Int pos)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == pos)
                {
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// Debug-only helper: adds <paramref name="modifierId"/> straight to
        /// the player's active set, bypassing the shop's random slot roll
        /// and its Lueur cost entirely — same "skip the grind" spirit as
        /// <see cref="DebugForceRoundComplete"/>, but with no in-game
        /// shortcut wired to it (no gameplay reason to skip paying for a
        /// specific modifier) — used by EditMode tests that need a specific
        /// modifier active without fighting shop RNG. Still respects
        /// EconomyConstants.MaxActiveModifiers and never duplicates a
        /// modifier already held.
        /// </summary>
        public bool DebugGrantModifier(ModifierId modifierId)
        {
            if (_activeModifiers.Contains(modifierId) || _activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            _activeModifiers.Add(modifierId);
            return true;
        }

        /// <summary>Debug-only helper: adds Lueur directly, bypassing gameplay entirely — same "skip the grind" spirit as <see cref="DebugGrantModifier"/>, used by EditMode tests that need to exercise shop purchases without earning real Lueur from line clears first.</summary>
        public void DebugGrantLueur(int amount)
        {
            Lueur += amount;
        }

        /// <summary>Debug-only helper: pins Lueur to an exact value, bypassing gameplay — lets EditMode tests establish a known baseline before asserting purchase/reroll outcomes, instead of assuming a round-completion helper (e.g. one that fills the board with golden cells to reach quota fast) happens to earn exactly zero incidental Lueur from line clears along the way. That assumption broke silently once InitialDeckFactory's starting deck composition changed and a seeded round started clearing lines it previously didn't.</summary>
        public void DebugSetLueur(int amount)
        {
            Lueur = amount;
        }

        private void EvaluateRoundEnd()
        {
            if (RoundScore >= CurrentQuota)
            {
                ApplyMultCinqRisqueLossChance();
                if (CurrentRoundIndex == _challenge.RoundCount - 1)
                {
                    State = RunState.RunVictory;
                    return;
                }
                State = RunState.AwaitingShop;
                OpenShop();
                return;
            }

            if (PiecesRemainingThisRound <= 0)
            {
                State = RunState.RunDefeat;
                return;
            }

            // A fully empty hand (PlacePiece defers its refill when the round
            // might be ending — see PlayFromHand's refillIfEmpty) has
            // nothing to evaluate yet, so it can never count as "stuck":
            // skip the check and let the round stay InProgress. PlacePiece's
            // own post-EvaluateRoundEnd check then draws the next hand right
            // away, which the NEXT placement will correctly check.
            if (!Deck.IsHandFullyEmpty() && !HasAnyHandPlacement())
            {
                State = RunState.RunDefeat;
            }
        }

        /// <summary>Risky Mult (MultCinqRisque): rolled once PER held copy, right at the end of a successfully completed round (see EvaluateRoundEnd) — on explicit request ("un modifier +5 mult avec une chance sur 5 de perdre le modifier a la fin de la round"). Each copy independently has a 1-in-EconomyConstants.MultCinqRisqueLossChanceDenominator chance to be removed.</summary>
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
                    _activeModifiers.RemoveAt(i);
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
                if (!hand[i].HasValue)
                {
                    continue;
                }
                shapes.Add(PieceShapeCatalog.GetRotated(hand[i].Value.Shape, rotations[i]));
            }
            return Grid.HasAnyValidPlacement(shapes);
        }

        // ---- Lueur shop (replaces the old round-end draft entirely — spec
        // extension, explicit request: "je ne veux plus du tout du système
        // actuel") ----

        /// <summary>Rolls every slot fresh — called once, the instant the shop opens (see EvaluateRoundEnd).</summary>
        private void OpenShop()
        {
            _purchasesThisVisit = 0;
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            for (int i = 0; i < _modifierSlots.Length; i++)
            {
                _modifierSlots[i] = RollModifierSlot();
            }
            for (int i = 0; i < _upgradeSlots.Length; i++)
            {
                _upgradeSlots[i] = RollUpgradeSlot();
            }
        }

        private ShopSlot RollModifierSlot()
        {
            var available = new List<ModifierDefinition>(ModifierCatalog.All.Length);
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var candidate = ModifierCatalog.All[i];
                if (_activeModifiers.Contains(candidate.Id) || IsAlreadyOfferedThisVisit(candidate.Id))
                {
                    continue;
                }
                available.Add(candidate);
            }
            if (available.Count == 0)
            {
                // Only possible once held+offered modifiers together cover
                // the whole catalog — extremely unlikely at the 10-modifier
                // cap with 68+ entries, but stay defensive rather than throw.
                for (int i = 0; i < ModifierCatalog.All.Length; i++)
                {
                    if (!_activeModifiers.Contains(ModifierCatalog.All[i].Id))
                    {
                        available.Add(ModifierCatalog.All[i]);
                    }
                }
            }

            var picked = available[_rng.Next(available.Count)];
            return ShopSlot.ForModifier(picked.Id);
        }

        private bool IsAlreadyOfferedThisVisit(ModifierId id)
        {
            for (int i = 0; i < _modifierSlots.Length; i++)
            {
                if (_modifierSlots[i] != null && _modifierSlots[i].ModifierId == id)
                {
                    return true;
                }
            }
            return false;
        }

        private ShopSlot RollUpgradeSlot()
        {
            var pool = _rng.Next(2) == 0 ? UpgradePool.Bank : UpgradePool.Grid;
            var upgrade = Upgrades.RollFromPool(pool);
            return ShopSlot.ForUpgrade(upgrade);
        }

        /// <summary>Current Lueur price of modifier slot <paramref name="index"/> — varies per modifier (see ModifierPricing, on explicit request), including this visit's escalation (see EconomyConstants.ShopPriceEscalationPerPurchase).</summary>
        public int GetModifierSlotPrice(int index)
        {
            if (index < 0 || index >= _modifierSlots.Length || _modifierSlots[index] == null)
            {
                return 0;
            }
            return ComputePrice(ModifierPricing.GetPrice(_modifierSlots[index].ModifierId));
        }

        /// <summary>Current Lueur price of upgrade slot <paramref name="index"/> — Grid-pool slots cost more than Bank-pool ones (a permanent piece enchantment is generally the stronger pick), including this visit's escalation.</summary>
        public int GetUpgradeSlotPrice(int index)
        {
            if (index < 0 || index >= _upgradeSlots.Length || _upgradeSlots[index] == null)
            {
                return 0;
            }
            int basePrice = _upgradeSlots[index].Pool == UpgradePool.Grid
                ? EconomyConstants.GridUpgradeShopBasePrice
                : EconomyConstants.BankUpgradeShopBasePrice;
            return ComputePrice(basePrice);
        }

        public int GetRerollPrice()
        {
            return ComputePrice(EconomyConstants.ShopRerollBasePrice);
        }

        private int ComputePrice(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _purchasesThisVisit));
        }

        /// <summary>
        /// Buys modifier slot <paramref name="index"/> outright — modifiers
        /// are never a mystery, so this is the whole purchase, no follow-up
        /// needed. Fails (no charge, no state change) if the shop isn't
        /// open, the slot is invalid/already bought, the player can't
        /// afford it, or they're already at EconomyConstants.MaxActiveModifiers.
        /// </summary>
        public bool BuyModifierSlot(int index)
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            if (index < 0 || index >= _modifierSlots.Length || _modifierSlots[index] == null || _modifierSlots[index].Purchased)
            {
                return false;
            }
            if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            int price = GetModifierSlotPrice(index);
            if (Lueur < price)
            {
                return false;
            }

            Lueur -= price;
            _purchasesThisVisit++;
            var slot = _modifierSlots[index];
            slot.Purchased = true;

            // "Mimic" (Copieur) isn't a real modifier of its own — buying it
            // adds another copy of whichever modifier was purchased
            // immediately before it instead (on explicit request: "un
            // modifier qui copy le modifier précédemment acheté"). A no-op
            // (still costs Lueur, still marks the slot sold) if nothing has
            // been purchased yet this run. _lastPurchasedModifierId only
            // ever tracks a REAL purchase, never Copieur itself, so buying
            // several Mimics in a row all copy the same underlying modifier
            // rather than chaining off each other.
            if (slot.ModifierId == ModifierId.Copieur)
            {
                if (_lastPurchasedModifierId.HasValue)
                {
                    _activeModifiers.Add(_lastPurchasedModifierId.Value);
                }
            }
            else
            {
                _activeModifiers.Add(slot.ModifierId);
                _lastPurchasedModifierId = slot.ModifierId;
            }
            return true;
        }

        /// <summary>
        /// Buys upgrade slot <paramref name="index"/> — the specific upgrade
        /// underneath (only its <see cref="UpgradePool"/> was ever shown)
        /// gets revealed as <see cref="PendingUpgrade"/>. A Bank-pool
        /// upgrade with no sub-choice (Joker) applies immediately and leaves
        /// PendingUpgrade null; one that needs a sub-choice (Retirer/
        /// Dupliquer/Recolorer) or a Grid-pool upgrade (needs a tile choice,
        /// see PendingUpgradeTileCandidates) leaves PendingUpgrade set until
        /// <see cref="ResolveUpgradeSubChoice"/>/<see cref="ResolveUpgradeTileChoice"/>
        /// finishes it — nothing else in the shop can be done meanwhile.
        /// Fails (no charge) under the same conditions as
        /// <see cref="BuyModifierSlot"/> (minus the modifier cap, which
        /// doesn't apply to upgrades).
        /// </summary>
        public bool BuyUpgradeSlot(int index)
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            if (index < 0 || index >= _upgradeSlots.Length || _upgradeSlots[index] == null || _upgradeSlots[index].Purchased)
            {
                return false;
            }
            int price = GetUpgradeSlotPrice(index);
            if (Lueur < price)
            {
                return false;
            }

            Lueur -= price;
            _purchasesThisVisit++;
            var slot = _upgradeSlots[index];
            slot.Purchased = true;
            var upgrade = slot.HiddenUpgrade;

            if (upgrade.Pool == UpgradePool.Grid)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTileCandidates = Upgrades.GetCandidateTilesFor(upgrade, Deck);
                return true;
            }

            if (upgrade.RequiresSubChoice)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTypeCandidates = Upgrades.GetCandidateTypesFor(upgrade, Deck);
                return true;
            }

            // The only Bank-pool upgrade left with RequiresSubChoice false —
            // see UpgradeCatalog.BankPool — is Joker, so this is always it.
            // ApplyJoker (not the generic Apply) so the actual shape rolled
            // can be surfaced via LastJokerShapeAdded for the reveal to show
            // (see UpgradeRevealView) instead of just naming the upgrade.
            LastJokerShapeAdded = Upgrades.ApplyJoker(Deck);
            return true;
        }

        /// <summary>Resolves a Bank-pool <see cref="PendingUpgrade"/> that needed a sub-choice (which piece type, and for Recolorer which target color). No-op (false) if nothing is pending or it's actually a Grid-pool upgrade.</summary>
        public bool ResolveUpgradeSubChoice(UpgradeSubChoice subChoice)
        {
            if (PendingUpgrade == null || PendingUpgrade.Pool != UpgradePool.Bank)
            {
                return false;
            }

            bool applied = Upgrades.Apply(PendingUpgrade, subChoice, Deck);
            PendingUpgrade = null;
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            return applied;
        }

        /// <summary>Resolves a Grid-pool <see cref="PendingUpgrade"/> — <paramref name="chosenDeckIndices"/> must come from <see cref="PendingUpgradeTileCandidates"/> (not validated beyond that here; the presentation layer only ever offers those). No-op (false) if nothing is pending or it's actually a Bank-pool upgrade.</summary>
        public bool ResolveUpgradeTileChoice(IReadOnlyList<int> chosenDeckIndices)
        {
            if (PendingUpgrade == null || PendingUpgrade.Pool != UpgradePool.Grid)
            {
                return false;
            }

            bool applied = Upgrades.ApplyToChosenTiles(PendingUpgrade, chosenDeckIndices, Deck);
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            return applied;
        }

        /// <summary>
        /// Refreshes every slot — modifier AND upgrade, purchased or not —
        /// with a new random offer. Used to only touch still-unsold slots
        /// (leaving a "SOLD" one exactly as it was), but on explicit
        /// feedback that read as reroll silently doing nothing whenever
        /// most of the shop had already been bought: "mes upgrades et
        /// modifiers que j'ai acheté sont encore marqué sold, il faut que
        /// j'aie tout de disponible". Buying a slot still permanently
        /// grants whatever it held (the modifier/upgrade is already applied
        /// by then) — this only replaces the SLOT OFFER itself, giving the
        /// player a fresh purchasable pick where a spent one used to sit.
        /// Costs Lueur (see GetRerollPrice), and itself counts toward this
        /// visit's price escalation like any other purchase.
        /// </summary>
        public bool RerollShop()
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            int price = GetRerollPrice();
            if (Lueur < price)
            {
                return false;
            }

            Lueur -= price;
            _purchasesThisVisit++;
            for (int i = 0; i < _modifierSlots.Length; i++)
            {
                _modifierSlots[i] = RollModifierSlot();
            }
            for (int i = 0; i < _upgradeSlots.Length; i++)
            {
                _upgradeSlots[i] = RollUpgradeSlot();
            }
            return true;
        }

        /// <summary>Closes the shop and starts the next round. Only valid while the shop is open and nothing is pending a follow-up.</summary>
        public bool LeaveShop()
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
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
