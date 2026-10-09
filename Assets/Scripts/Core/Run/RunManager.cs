using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Orchestrates the run: quotas, piece budgets, boss round locking, and
    /// the win/lose transition into the shop.
    ///
    /// A round ends when its quota is reached (success), or when its piece
    /// budget runs out, or its hand is unplayable with no shuffles left to
    /// re-roll it (defeat). See <see cref="ShuffleHand"/>.
    /// </summary>
    public sealed class RunManager
    {
        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

        private readonly List<ModifierId> _activeModifiers = new List<ModifierId>();

        // Parallel to _activeModifiers (same index == same slot). Every
        // mutation of _activeModifiers (add/remove/swap/move) must mirror the
        // same operation here - see AddActiveModifier/RemoveActiveModifierAt,
        // and the inline swaps in SwapModifiers/MoveModifier.
        private readonly List<int> _modifierLevels = new List<int>();

        /// <summary>Modifiers currently held by the player, persisting for the whole run. Capped at <see cref="EconomyConstants.MaxActiveModifiers"/>.</summary>
        public IReadOnlyList<ModifierId> ActiveModifiers
        {
            get { return _activeModifiers; }
        }

        /// <summary>The level of the modifier at <paramref name="index"/> in <see cref="ActiveModifiers"/> (1 if never leveled, or an out-of-range index).</summary>
        public int GetModifierLevel(int index)
        {
            return index >= 0 && index < _modifierLevels.Count ? _modifierLevels[index] : 1;
        }

        /// <summary>The scoring multiplier for modifier slot <paramref name="index"/> (see <see cref="ModifierLevelUtility"/>).</summary>
        private float GetModifierLevelFactor(int index)
        {
            return ModifierLevelUtility.LevelToFactor(GetModifierLevel(index));
        }

        /// <summary>The only place that adds a slot to <see cref="_activeModifiers"/> - keeps a fresh slot's level in sync.</summary>
        private void AddActiveModifier(ModifierId id)
        {
            _activeModifiers.Add(id);
            _modifierLevels.Add(1);
        }

        /// <summary>The only place that removes a slot from <see cref="_activeModifiers"/> - keeps its level entry in sync.</summary>
        private void RemoveActiveModifierAt(int index)
        {
            _activeModifiers.RemoveAt(index);
            _modifierLevels.RemoveAt(index);
        }

        /// <summary>
        /// Swaps the modifiers at two positions in <see cref="ActiveModifiers"/>.
        /// Modifier order determines scoring order (see PlacementResult.Mult's
        /// ordered fold). A no-op for an out-of-range or identical pair.
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

            // Level travels with the modifier, not the slot position.
            var tempLevel = _modifierLevels[indexA];
            _modifierLevels[indexA] = _modifierLevels[indexB];
            _modifierLevels[indexB] = tempLevel;
        }

        /// <summary>
        /// Moves the modifier at <paramref name="fromIndex"/> to <paramref
        /// name="toIndex"/>, shifting the modifiers in between by one position.
        /// A no-op for an out-of-range or identical pair.
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

            // Level travels with the modifier, not the slot position.
            var movedLevel = _modifierLevels[fromIndex];
            _modifierLevels.RemoveAt(fromIndex);
            _modifierLevels.Insert(toIndex, movedLevel);
        }

        /// <summary>
        /// Sells the modifier at <paramref name="index"/> in <see
        /// cref="ActiveModifiers"/> for Lueur. Refunds <see
        /// cref="ModifierPricing"/>'s base catalog price minus 1, never the
        /// escalated price actually paid. No-op (returns false) for an
        /// out-of-range index.
        /// </summary>
        public bool SellModifier(int index, out int refundedLueur)
        {
            if (!IsValidModifierIndex(index))
            {
                refundedLueur = 0;
                return false;
            }

            var id = _activeModifiers[index];
            refundedLueur = ModifierPricing.GetPrice(id) - 1;
            RemoveActiveModifierAt(index);
            Lueur += refundedLueur;
            return true;
        }

        private bool IsValidModifierIndex(int index)
        {
            return index >= 0 && index < _activeModifiers.Count;
        }

        /// <summary>The last modifier actually added by a shop purchase this run (never Copieur itself) - null until the first purchase.</summary>
        private ModifierId? _lastPurchasedModifierId;

        /// <summary>
        /// Currency earned by clearing lines with diverse colors (see
        /// GridManager.PlacementResult.LueurEarned), persists for the whole run,
        /// and is spent in the between-round shop.
        /// </summary>
        public int Lueur { get; private set; }

        /// <summary>How many hand shuffles the player has left this run. Persists across rounds, never reset by StartRound.</summary>
        public int ShufflesRemaining { get; private set; }

        private readonly ShopSlot[] _blisterSlots = new ShopSlot[EconomyConstants.ShopBlisterSlotCount];
        private readonly ShopSlot[] _upgradeSlots = new ShopSlot[EconomyConstants.ShopUpgradeSlotCount];

        /// <summary>How many slot purchases have happened in the current shop visit - raises the price of every other slot still on offer (see GetBlisterSlotPrice/GetUpgradeSlotPrice). Reset to 0 each time the shop opens. Separate from <see cref="_rerollsThisVisit"/>: a slot purchase no longer escalates the reroll price.</summary>
        private int _purchasesThisVisit;

        /// <summary>How many times the shop has been rerolled this visit - drives only GetRerollPrice's escalation. Reset to 0 each time the shop opens.</summary>
        private int _rerollsThisVisit;

        /// <summary>The "Blister" section - a modifier or upgrade, drawn from one shared bag, always fully revealed. Never touched by RerollShop.</summary>
        public IReadOnlyList<ShopSlot> ShopBlisterSlots
        {
            get { return _blisterSlots; }
        }

        /// <summary>The "Casino" section - always an Upgrade-kind slot, only its UpgradePool shown until purchased. The only section RerollShop touches.</summary>
        public IReadOnlyList<ShopSlot> ShopUpgradeSlots
        {
            get { return _upgradeSlots; }
        }

        /// <summary>
        /// Non-null while a purchased Upgrade slot still needs a follow-up from
        /// the player before the shop can be used again: a sub-choice (Bank
        /// pool) or a tile choice (Grid pool, see
        /// <see cref="PendingUpgradeTileCandidates"/>). Cleared once <see
        /// cref="ResolveUpgradeSubChoice"/> or <see
        /// cref="ResolveUpgradeTileChoice"/> succeeds.
        /// </summary>
        public UpgradeDefinition PendingUpgrade { get; private set; }

        /// <summary>Candidate deck token indices for <see cref="PendingUpgrade"/> - only populated for a Grid-pool upgrade; empty for a Bank-pool one, which needs a sub-choice instead.</summary>
        public IReadOnlyList<int> PendingUpgradeTileCandidates { get; private set; }

        /// <summary>Candidate piece types for <see cref="PendingUpgrade"/>'s sub-choice - only populated for a Bank-pool upgrade that needs one (Retirer/Dupliquer/Recolorer); empty otherwise.</summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> PendingUpgradeTypeCandidates { get; private set; }

        /// <summary>Candidate freshly-rolled pieces (trait included) for <see cref="PendingUpgrade"/>'s sub-choice - only populated for "Random Piece"; empty otherwise, since these tokens don't exist in the deck yet.</summary>
        public IReadOnlyList<PieceToken> PendingUpgradePieceCandidates { get; private set; }

        /// <summary>The shape most recently rolled by a Joker purchase - read once by the presentation layer right after the purchase. Meaningless before any Joker purchase this run.</summary>
        public ShapeId LastJokerShapeAdded { get; private set; }

        /// <summary>The Joker-exclusive combat trait that same purchase also rolled (see <see cref="PieceTrait.JokerCombatKinds"/>). Meaningless before any Joker purchase this run.</summary>
        public PieceTraitKind LastJokerCombatKindAdded { get; private set; }

        /// <summary>The modifier most recently granted by a "Random Modifier" purchase - read once by the presentation layer. Null if the gamble granted nothing (already at the cap, or every modifier already held). Meaningless before any such purchase this run.</summary>
        public ModifierId? LastRandomModifierGranted { get; private set; }

        /// <summary>The shape most recently leveled up by a "Piece Mastery" purchase - read once by the presentation layer. Null before any such purchase this run.</summary>
        public ShapeId? LastShapeMasteryGranted { get; private set; }

        /// <summary>
        /// Current level per exact shape, from "Piece Mastery" purchases - 1
        /// (no bonus) for any shape never leveled up. Independent of any held
        /// modifier slot, keyed directly by <see cref="ShapeId"/>.
        /// </summary>
        private readonly Dictionary<ShapeId, int> _shapeMasteryLevels = new Dictionary<ShapeId, int>();

        /// <summary>Current level of <paramref name="shape"/> (1 if never leveled up).</summary>
        public int GetShapeMasteryLevel(ShapeId shape)
        {
            return _shapeMasteryLevels.TryGetValue(shape, out var level) ? level : 1;
        }

        /// <summary>Piece Mastery's exact sibling, keyed by PieceColor instead of ShapeId.</summary>
        public PieceColor? LastColorMasteryGranted { get; private set; }

        /// <summary>Current level per color, from "Color Mastery" purchases - 1 (no bonus) for any color never leveled up.</summary>
        private readonly Dictionary<PieceColor, int> _colorMasteryLevels = new Dictionary<PieceColor, int>();

        /// <summary>Current level of <paramref name="color"/> (1 if never leveled up).</summary>
        public int GetColorMasteryLevel(PieceColor color)
        {
            return _colorMasteryLevels.TryGetValue(color, out var level) ? level : 1;
        }

        /// <summary>How many times each modifier has actually fired (scored at least one point) so far this run. Read via <see cref="GetModifierUsageCount"/>.</summary>
        private readonly Dictionary<ModifierId, int> _modifierUsageCounts = new Dictionary<ModifierId, int>();

        /// <summary>How many times <paramref name="id"/> has fired this run (0 if never, or not currently held).</summary>
        public int GetModifierUsageCount(ModifierId id)
        {
            return _modifierUsageCounts.TryGetValue(id, out var count) ? count : 0;
        }

        /// <summary>How many trait-carrying pieces have been placed so far this run - Experience's driver. Permanent for the whole run.</summary>
        private int _specialPiecesPlayedCount;

        /// <summary>
        /// The current effective state of a progressive/incremental modifier,
        /// formatted for its tooltip - null for a modifier whose bonus doesn't
        /// grow or shrink over the round/run. Gradient/Repetition/Densite/
        /// Epuisement/Solidarite/Multitude report a plain number; Repetition's
        /// is a preview of its own next placement's value (see
        /// GridManager.RepetitionCurrentMultiplier), the others report their
        /// own last placement's value. CartesEnchantees/Experience/Densite are
        /// additive contributors to the same "+Mult" pool as
        /// Solidarite/MultUn (see PlacementResult.ProgressiveAdditiveMult), so
        /// they show "+N Mult" rather than "xN".
        ///
        /// <paramref name="index"/> is this slot's own position in <see
        /// cref="ActiveModifiers"/> (optional, -1 by default) - when given,
        /// the value is scaled by that slot's level factor (see
        /// ModifierLevelUtility), matching the scaling applied at score time.
        /// Left at -1 (factor 1) by a caller that doesn't know its own slot
        /// index.
        /// </summary>
        public string GetProgressiveModifierStateText(ModifierId id, int index = -1)
        {
            float levelFactor = index >= 0 ? GetModifierLevelFactor(index) : 1f;
            switch (id)
            {
                case ModifierId.Gradient:
                    return "Currently x" + FormatMultDisplay(Grid.GradientCurrentMultiplier * levelFactor);
                case ModifierId.Repetition:
                    return "Currently x" + FormatMultDisplay(Grid.RepetitionCurrentMultiplier * levelFactor);
                case ModifierId.Densite:
                    return "Currently +" + FormatMultDisplay(Grid.FilledCellCount / (float)ScoringConstants.DensiteFilledCellsPerMultStep * levelFactor) + " Mult";
                case ModifierId.Epuisement:
                    return "Currently +" + Mathf.RoundToInt(Grid.EpuisementCurrentBonus * levelFactor) + " pts";
                case ModifierId.Solidarite:
                    return "Currently +" + FormatMultDisplay((_activeModifiers.Count / ScoringConstants.SolidariteModifierCountDivisor) * levelFactor) + " Mult";
                case ModifierId.Multitude:
                    return "Currently +" + Mathf.RoundToInt(Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard * levelFactor) + " pts";
                case ModifierId.CartesEnchantees:
                    return "Currently +" + FormatMultDisplay((1 + CountUpgradedDeckCards()) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep * levelFactor) + " Mult";
                case ModifierId.Experience:
                    return "Currently +" + FormatMultDisplay((1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep * levelFactor) + " Mult";
                case ModifierId.Arsenal:
                    return "Currently +" + Mathf.RoundToInt(CountDistinctCombatKindsInDeck() * ScoringConstants.ArsenalMultPerDistinctCombatKind * levelFactor) + " Mult";
                case ModifierId.Polyvalence:
                    return "Currently +" + Mathf.RoundToInt(CountDistinctModifierCategoriesHeld() * ScoringConstants.PolyvalenceMultPerCategory * levelFactor) + " Mult";
                default:
                    return null;
            }
        }

        /// <summary>Whole number when <paramref name="value"/> is (near enough) an integer, one decimal otherwise - matches ComboView's mult pill formatting.</summary>
        private static string FormatMultDisplay(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < 0.05f)
            {
                return Mathf.RoundToInt(value).ToString();
            }
            // Invariant culture - "F1" would otherwise use a comma decimal
            // separator under a French system locale, and this string is never
            // parsed back.
            return value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly IRandomProvider _rng;
        private readonly ChallengeDefinition _challenge;

        /// <summary>The challenge this run was started with (see ChallengeCatalog).</summary>
        public ChallengeDefinition Challenge
        {
            get { return _challenge; }
        }

        /// <summary>0-based index into <see cref="ChallengeDefinition"/>'s Quotas/PieceBudgets arrays.</summary>
        public int CurrentRoundIndex { get; private set; }

        public int RoundScore { get; private set; }
        public int TotalScore { get; private set; }
        public int PiecesRemainingThisRound { get; private set; }

        /// <summary>How much Lueur the round that just ended converted its unused piece budget into (see EvaluateRoundEnd) - 0 until the first round ends, read once by GameBootstrap right after transitioning to AwaitingShop.</summary>
        public int LastRoundEndLueurBonus { get; private set; }

        public RunState State { get; private set; }

        /// <summary>The randomly rolled boss rule for this round; None on non-boss rounds, and always None while <see cref="HasActiveEncounter"/> is true - the two systems never run at once.</summary>
        public BossEffect CurrentBossEffect { get; private set; }

        private IReadOnlyList<EnemyInstance> _currentEncounter = System.Array.Empty<EnemyInstance>();

        /// <summary>
        /// This round's enemies, in their authored order. Empty for any round
        /// EncounterCatalog has nothing authored for - see <see
        /// cref="HasActiveEncounter"/>, which RunManager actually branches on.
        /// </summary>
        public IReadOnlyList<EnemyInstance> CurrentEncounter
        {
            get { return _currentEncounter; }
        }

        /// <summary>
        /// True while this round is a combat encounter rather than a quota
        /// round - <see cref="EvaluateRoundEnd"/>'s victory condition becomes
        /// "every enemy dead" instead of reaching <see cref="CurrentQuota"/>,
        /// and every placement's score damages the front alive enemy (see
        /// <see cref="ApplyDamageToEncounter"/>) instead of only banking
        /// toward quota.
        /// </summary>
        public bool HasActiveEncounter
        {
            get { return _currentEncounter.Count > 0; }
        }

        /// <summary>The hand slot disabled for this boss round, or null for other boss effects.</summary>
        public int? BossLockedHandSlotIndex { get; private set; }

        /// <summary>The base color that scores no points this boss round, or null for other boss effects.</summary>
        public PieceColor? BossCursedColor { get; private set; }

        public int CurrentRoundNumber
        {
            get { return CurrentRoundIndex + 1; }
        }

        /// <summary>This round's score target. Still read for display even once <see cref="HasActiveEncounter"/> makes "every enemy dead" the real win condition.</summary>
        public int CurrentQuota
        {
            get { return _challenge.Quotas[CurrentRoundIndex]; }
        }

        public int CurrentBudget
        {
            get { return _challenge.PieceBudgets[CurrentRoundIndex]; }
        }

        /// <summary>Classic/Marathon: rounds 4 and 8. Chaos keeps its cell-lock active every round (see ChallengeDefinition.BossActiveEveryRound).</summary>
        public bool IsBossRound
        {
            get { return _challenge.BossActiveEveryRound || (CurrentRoundIndex + 1) % RunConfig.BossRoundInterval == 0; }
        }

        public bool IsHandSlotLocked(int handIndex)
        {
            return BossLockedHandSlotIndex.HasValue && BossLockedHandSlotIndex.Value == handIndex;
        }

        /// <summary><paramref name="challenge"/> defaults to ChallengeCatalog.Classic when omitted.</summary>
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
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            ShufflesRemaining = RunConfig.StartingShuffleCount;
            CurrentRoundIndex = 0;
            StartRound();
        }

        private void StartRound()
        {
            Grid.ResetForNewRound();
            _currentEncounter = BuildEncounter(CurrentRoundIndex);
            if (HasActiveEncounter)
            {
                // The two systems never run at once - see HasActiveEncounter's own
                // doc comment.
                CurrentBossEffect = BossEffect.None;
                BossLockedHandSlotIndex = null;
                BossCursedColor = null;
            }
            else
            {
                RollBossEffectForRound();
            }
            // The boss round lock ratchets up gradually instead of all at once -
            // see ApplyBossLockTick, called from PlacePiece every
            // _challenge.BossLockPiecesInterval pieces.
            // Normally a no-op (the hand carries over from the previous round
            // untouched) - only fires when the placement that empties the hand
            // also ends the round, so PlacePiece defers the draw here instead of
            // dealing it before the player has picked this round's upgrade.
            // Deliberately plain Deck.DrawNewHand, not DrawFreshHand: this is the
            // round's starting hand, not a Shuffle triggered mid-round, so no
            // enemy here should get an on-Shuffle tick before the player has
            // placed a single piece against it.
            if (Deck.IsHandFullyEmpty())
            {
                Deck.DrawNewHand();
            }
            RoundScore = 0;
            PiecesRemainingThisRound = CurrentBudget;
            LastRoundEndLueurBonus = 0;
            State = RunState.InProgress;

            // Same stuck-check PlacePiece runs after a mid-round redraw (see
            // there) - covers a boss round's locked cells leaving zero legal
            // placements for the round's very first hand.
            EvaluateRoundEnd();
        }

        private void RollBossEffectForRound()
        {
            CurrentBossEffect = BossEffect.None;
            BossLockedHandSlotIndex = null;
            BossCursedColor = null;
            if ((CurrentRoundIndex + 1) % RunConfig.BossRoundInterval != 0)
            {
                return;
            }

            CurrentBossEffect = (BossEffect)(1 + _rng.Next(3));
            if (CurrentBossEffect == BossEffect.LockedHandSlot)
            {
                BossLockedHandSlotIndex = _rng.Next(DeckManager.HandSize);
            }
            else if (CurrentBossEffect == BossEffect.CursedColor)
            {
                var colors = PieceColorUtility.BaseColors;
                BossCursedColor = colors[_rng.Next(colors.Count)];
            }
        }

        /// <summary>
        /// <summary>
        /// Attempts to place the hand piece at <paramref name="handIndex"/>
        /// anchored at (x, y). No-ops (placement fails, state untouched) if
        /// the run isn't InProgress or the placement is invalid.
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

            if (IsHandSlotLocked(handIndex))
            {
                return new PlacementOutcome(PlacementResult.Failure("This hand slot is locked by the boss"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
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

            // Chameleon and Spark both need grid state read BEFORE Grid.PlacePiece
            // mutates it: Chameleon overrides the color the piece places as, and
            // Spark needs the no-clear streak as it stood before this placement
            // (Grid.PlacePiece updates that streak for the NEXT placement to
            // read).
            PieceColor placementColor = token.Color;
            PieceColor? chameleonOriginalColor = null;
            int sparkStreakBeforePlacement = 0;
            if (token.Trait.HasValue)
            {
                // Experience's driver - every trait kind counts as "special".
                _specialPiecesPlayedCount++;
                var kind = token.Trait.Value.Kind;
                if (kind == PieceTraitKind.Chameleon)
                {
                    placementColor = ResolveChameleonColor(shape, x, y, traitCellPos.Value, token.Color);
                    if (placementColor != token.Color)
                    {
                        // Recoloring also updates the deck copy, not just this placement.
                        // Must run before Deck.PlayFromHand below clears this slot (the one
                        // spot that still has both handIndex and the resolved color in
                        // scope together).
                        Deck.RecolorHandToken(handIndex, placementColor);
                        // Fed into the returned PlacementOutcome below so Presentation can animate
                        // the switch instead of letting it snap straight to the resolved color.
                        chameleonOriginalColor = token.Color;
                    }
                }
                else if (kind == PieceTraitKind.Spark)
                {
                    sparkStreakBeforePlacement = Grid.PlacementsSinceLastClear;
                }
            }

            StampMasteryBonuses(shape, placementColor, x, y);
            var transientTraitCells = ApplyTokenTrait(token.Trait, traitCellPos, shape, x, y);
            // Snapshot BEFORE Grid.PlacePiece, same reasoning as the
            // Chameleon/Spark reads above: a poisoned cell this placement clears
            // would read back "not poisoned" once Cell.ClearFill resets the flag.
            var poisonedPositions = HasActiveEncounter ? GetPoisonedPositionsSnapshot() : null;
            var placement = Grid.PlacePiece(shape, placementColor, x, y, _activeModifiers, _modifierLevels);
            ClearTokenTraitCells(transientTraitCells);
            if (token.Trait.HasValue)
            {
                ApplyPostPlacementTraitBonus(token.Trait.Value, traitCellPos.Value, sparkStreakBeforePlacement, placement);
            }
            ApplyHandSlotModifierBonus(handIndex, placement);
            ApplyDeckStateModifierBonuses(placement);
            if (BossCursedColor.HasValue)
            {
                ApplyCursedColorScoreRule(placement, BossCursedColor.Value);
            }
            if (HasActiveEncounter)
            {
                ApplyHaterScoreRules(placement);
            }
            int poisonMagnitudeForReclaimer = 0;
            if (poisonedPositions != null && poisonedPositions.Count > 0)
            {
                poisonMagnitudeForReclaimer = ApplyPoisonScoreRule(placement, poisonedPositions);
            }
            CountModifierUsage(placement);
            RemoveDepletedEpuisement();
            RoundScore += placement.TotalScore;
            TotalScore += placement.TotalScore;
            // ModifierLueurBonus is a second, independent source of Lueur, on top
            // of the line-clearing LueurEarned above.
            Lueur += placement.LueurEarned + placement.ModifierLueurBonus;
            if (HasActiveEncounter)
            {
                // A Joker piece carrying a combat trait retargets (or splits) this
                // placement's damage instead of the ordinary front-alive-enemy hit -
                // and the same applies whenever an older Joker piece's stamped cells
                // get pulled into this placement's scored group, so this scans the
                // whole group rather than just this token's own trait.
                ApplyJokerCombatOrDefaultDamage(placement.GroupCells, placement.TotalScore);
                if (placement.ClearedLineCount > 0)
                {
                    HealLeech(placement.ClearedLineCount);
                }
                if (poisonMagnitudeForReclaimer > 0)
                {
                    HealReclaimer(poisonMagnitudeForReclaimer);
                }
            }
            // Don't auto-refill yet - if this placement also ends the round,
            // drawing the next 3 pieces here would hand them out before the
            // player has picked this round's upgrade (see StartRound).
            Deck.PlayFromHand(handIndex, refillIfEmpty: false);
            PiecesRemainingThisRound--;

            IReadOnlyList<Vector2Int> bossLockedCells = System.Array.Empty<Vector2Int>();
            bool cellLockActive = _challenge.BossActiveEveryRound || CurrentBossEffect == BossEffect.ProgressiveCellLock;
            if (cellLockActive)
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
                DrawFreshHand();
                // EvaluateRoundEnd's stuck-check above deliberately skips an empty
                // hand - but the fresh hand just drawn might itself have no legal
                // placement anywhere on the board, so it needs its own check here.
                EvaluateRoundEnd();
            }

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound, bossLockedCells, chameleonOriginalColor);
        }

        /// <summary>
        /// Boss round mechanic (see ChallengeDefinition.BossLockPiecesInterval):
        /// locks _challenge.BossLockCellsPerInterval more random empty cells
        /// and folds in any score that locking produces (see
        /// GridManager.LockFreeCellsAndCheckClears). Returns the newly locked
        /// cells so the presentation layer can refresh their visuals.
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

        /// <summary>Removes point events tied to the cursed color while preserving play legality, clears, Lueur, and non-point multiplier effects.</summary>
        private void ApplyCursedColorScoreRule(PlacementResult placement, PieceColor cursedColor)
        {
            var clearedColors = new Dictionary<Vector2Int, PieceColor>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                clearedColors[placement.ClearedCells[i]] = placement.ClearedCellColors[i];
            }
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                if (placement.DestroyedCellColors[i].HasValue)
                {
                    clearedColors[placement.DestroyedCells[i]] = placement.DestroyedCellColors[i].Value;
                }
            }

            var keptEvents = new List<ScoreEvent>(placement.ScoreEvents.Count);
            placement.GroupBonus = 0;
            placement.GoldenBonus = 0;
            placement.LineClearScore = 0;
            placement.ModifierBonus = 0;
            placement.TraitBonus = 0;
            placement.ShapeMasteryBonus = 0;
            placement.ColorMasteryBonus = 0;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (IsPointEvent(scoreEvent.Type) && IsPositionColor(scoreEvent.Position, clearedColors, cursedColor))
                {
                    continue;
                }

                keptEvents.Add(scoreEvent);
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
            placement.ScoreEvents = keptEvents;
        }

        private static bool IsPointEvent(ScoreEventType type)
        {
            return type == ScoreEventType.Group || type == ScoreEventType.Golden ||
                type == ScoreEventType.LineClear || type == ScoreEventType.Bastion ||
                type == ScoreEventType.Modifier || type == ScoreEventType.Trait ||
                type == ScoreEventType.ShapeMastery || type == ScoreEventType.ColorMastery;
        }

        private bool IsPositionColor(Vector2Int position, Dictionary<Vector2Int, PieceColor> clearedColors, PieceColor cursedColor)
        {
            if (clearedColors.TryGetValue(position, out var clearedColor))
            {
                return clearedColor == cursedColor;
            }
            var cell = Grid.GetCell(position);
            return cell.IsFilled && cell.FilledColor == cursedColor;
        }

        /// <summary>"Shape Hater" - <see cref="ApplyCursedColorScoreRule"/>'s exact mirror, keyed by <see cref="Cell.FilledShapeId"/> instead of color.</summary>
        private void ApplyShapeHaterScoreRule(PlacementResult placement, ShapeId hatedShape)
        {
            var clearedShapes = new Dictionary<Vector2Int, ShapeId>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                if (placement.ClearedCellShapes[i].HasValue)
                {
                    clearedShapes[placement.ClearedCells[i]] = placement.ClearedCellShapes[i].Value;
                }
            }
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                if (placement.DestroyedCellShapes[i].HasValue)
                {
                    clearedShapes[placement.DestroyedCells[i]] = placement.DestroyedCellShapes[i].Value;
                }
            }

            var keptEvents = new List<ScoreEvent>(placement.ScoreEvents.Count);
            placement.GroupBonus = 0;
            placement.GoldenBonus = 0;
            placement.LineClearScore = 0;
            placement.ModifierBonus = 0;
            placement.TraitBonus = 0;
            placement.ShapeMasteryBonus = 0;
            placement.ColorMasteryBonus = 0;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (IsPointEvent(scoreEvent.Type) && IsPositionShape(scoreEvent.Position, clearedShapes, hatedShape))
                {
                    continue;
                }

                keptEvents.Add(scoreEvent);
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
            placement.ScoreEvents = keptEvents;
        }

        private bool IsPositionShape(Vector2Int position, Dictionary<Vector2Int, ShapeId> clearedShapes, ShapeId hatedShape)
        {
            if (clearedShapes.TryGetValue(position, out var clearedShape))
            {
                return clearedShape == hatedShape;
            }
            var cell = Grid.GetCell(position);
            return cell.IsFilled && cell.FilledShapeId == hatedShape;
        }

        /// <summary>Applies Color Hater's and/or Shape Hater's score-cancellation rule for this placement, if either is alive in the current encounter.</summary>
        private void ApplyHaterScoreRules(PlacementResult placement)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                if (enemy.Definition.Id == EnemyId.ColorHater && enemy.HatedColor.HasValue)
                {
                    ApplyCursedColorScoreRule(placement, enemy.HatedColor.Value);
                }
                else if (enemy.Definition.Id == EnemyId.ShapeHater && enemy.HatedShape.HasValue)
                {
                    ApplyShapeHaterScoreRule(placement, enemy.HatedShape.Value);
                }
            }
        }

        /// <summary>
        /// Stamps every cell of this placement's own piece with whatever flat
        /// Mastery bonus its exact shape/color currently carries (0 for an
        /// axis never leveled up). Must run before <see
        /// cref="GridManager.PlacePiece"/>, which reads <see
        /// cref="Cell.ShapeMasteryBonus"/>/<see cref="Cell.ColorMasteryBonus"/>
        /// in its group-scoring loop. The stamp is frozen at whatever level
        /// applied at placement time, so a later Mastery purchase never
        /// retroactively changes it, but it does rescore every time this
        /// cell's group scores again.
        /// </summary>
        private void StampMasteryBonuses(PieceShape shape, PieceColor color, int x, int y)
        {
            int shapeBonus = GetShapeMasteryLevel(shape.Id) - 1;
            int colorBonus = GetColorMasteryLevel(color) - 1;
            if (shapeBonus <= 0 && colorBonus <= 0)
            {
                return;
            }

            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var pos = shape.Cells[i] + new Vector2Int(x, y);
                var cell = Grid.GetCell(pos);
                cell.ShapeMasteryBonus = shapeBonus;
                cell.ColorMasteryBonus = colorBonus;
            }
        }

        /// <summary>
        /// If <paramref name="trait"/> is present, stamps the grid cell(s) its
        /// effect touches with the matching golden/tinted/multiplier flag(s)
        /// just before <see cref="GridManager.PlacePiece"/> scores this
        /// placement. Returns every cell that should be un-stamped again once
        /// scoring is done (see <see cref="ClearTokenTraitCells"/>) - every
        /// kind except <see cref="PieceTraitKind.Seeder"/> (stays until
        /// Cell.ResetForNewRound) and the kinds whose effects resolve before
        /// Grid.PlacePiece runs (Chameleon) or after it returns (see <see
        /// cref="ApplyPostPlacementTraitBonus"/>), none of which stamp a cell
        /// at all.
        /// </summary>
        private List<Cell> ApplyTokenTrait(PieceTrait? trait, Vector2Int? traitCellPos, PieceShape shape, int anchorX, int anchorY)
        {
            var transientCells = new List<Cell>();
            if (!trait.HasValue)
            {
                return transientCells;
            }

            var pos = traitCellPos.Value;
            var cell = Grid.GetCell(pos.x, pos.y);

            // Cosmetic, persists for the rest of the round regardless of trait
            // kind - not added to transientCells (see Cell.OriginTrait).
            cell.OriginTrait = trait.Value;

            // Joker's own combat traits badge every cell of the piece, not just
            // this one - purely cosmetic; the one-shot combat effect still
            // resolves once per placement regardless (see
            // RunManager.ApplyJokerCombatDamage).
            if (PieceTrait.IsJokerCombatKind(trait.Value.Kind))
            {
                for (int i = 0; i < shape.Cells.Count; i++)
                {
                    var otherPos = new Vector2Int(anchorX, anchorY) + shape.Cells[i];
                    if (otherPos != pos)
                    {
                        Grid.GetCell(otherPos.x, otherPos.y).OriginTrait = trait.Value;
                    }
                }
            }

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
                    // ClearTokenTraitCells never un-stamps it after this placement's own
                    // scoring - unlike every other trait, this one keeps scoring as a
                    // normal golden grid cell for the rest of the current round.
                    // Grid.ResetForNewRound (via Cell.ResetForNewRound) eventually
                    // clears it.
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
                    // None of these stamp a Cell flag before placement - each is either
                    // resolved before Grid.PlacePiece runs (Chameleon, via
                    // placementColor above) or computed after it returns (see
                    // ApplyPostPlacementTraitBonus). Bastion in particular can't lock its
                    // cell yet: Grid.PlacePiece's own internal CanPlace re-check would
                    // then see this placement's own cell as already locked and reject
                    // it.
                    break;

                case PieceTraitKind.Bombe:
                case PieceTraitKind.Range:
                case PieceTraitKind.Eclat:
                case PieceTraitKind.Precision:
                case PieceTraitKind.Sangsue:
                    // Joker-exclusive combat traits never score anything of their own
                    // (the OriginTrait stamp is purely cosmetic); their effect is
                    // resolved from PlacePiece's own HasActiveEncounter block, after
                    // Grid.PlacePiece returns the final TotalScore to redirect (see
                    // ApplyJokerCombatDamage).
                    break;
            }
            return transientCells;
        }

        /// <summary>
        /// "Chameleon Tile": resolves the color the whole piece should place
        /// as - of every already-filled orthogonal neighbor color around the
        /// enchanted cell, picks whichever would make this placement's own
        /// resulting group score the most (same treatment as Joker - see
        /// GridManager.ResolveBestJokerGroup). Falls back to the piece's own
        /// color when no neighbor is filled yet (checked before
        /// Grid.PlacePiece runs, so only pre-existing board state can match).
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

        /// <summary>Reverts every stamp <see cref="ApplyTokenTrait"/> made - most enchantments fire once, on this placement's own scoring, not as a lasting grid modifier.</summary>
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
        /// GridManager's own per-cell scoring loop: Mirror/Catalyst/Twin need
        /// the group as it stood right after scoring, Detonator needs the
        /// line-clear outcome, Spark needs the pre-placement no-clear streak,
        /// and Void mutates the grid outside this placement's own cells.
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

        /// <summary>Adds a flat trait bonus to <paramref name="placement"/> and appends a matching <see cref="ScoreEventType.Trait"/> event.</summary>
        private static void AddTraitBonus(PlacementResult placement, Vector2Int pos, int bonus)
        {
            placement.TraitBonus += bonus;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.Trait, pos, bonus));
            placement.ScoreEvents = events;
        }

        /// <summary>Records one cell a trait effect destroyed - see <see cref="PlacementResult.DestroyedCells"/> for why this is separate from <see cref="PlacementResult.ClearedCells"/>.</summary>
        private static void AddDestroyedCell(PlacementResult placement, Vector2Int pos, PieceColor? color, ShapeId? shapeId)
        {
            var cells = new List<Vector2Int>(placement.DestroyedCells);
            cells.Add(pos);
            placement.DestroyedCells = cells;
            var colors = new List<PieceColor?>(placement.DestroyedCellColors);
            colors.Add(color);
            placement.DestroyedCellColors = colors;
            var shapes = new List<ShapeId?>(placement.DestroyedCellShapes);
            shapes.Add(shapeId);
            placement.DestroyedCellShapes = shapes;
        }

        // Which SlotUn/Deux/Trois modifier corresponds to each 0-based hand index.
        private static readonly ModifierId?[] HandSlotModifiers = { ModifierId.SlotUn, ModifierId.SlotDeux, ModifierId.SlotTrois };

        /// <summary>
        /// "Slot N Loyalty": flat +Mult (additive, see
        /// PlacementResult.AdditiveMultBonus, ScoringConstants.SlotLoyaltyBonus)
        /// when the piece was played from hand slot <paramref
        /// name="handIndex"/> (0-based) and the matching modifier is active.
        /// GridManager can't evaluate this itself - it has no idea which hand
        /// slot a piece came from, only this method's caller does - so it's
        /// resolved here post-hoc, same pattern as ApplyPostPlacementTraitBonus.
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

            // Its own position in _activeModifiers, needed so
            // PlacementResult.Mult's ordered left-to-right fold places this
            // correctly relative to every other Mult modifier - plus this slot's
            // own level factor (see ModifierLevelUtility).
            int slotIndex = _activeModifiers.IndexOf(slotModifier.Value);
            int bonus = Mathf.RoundToInt(ScoringConstants.SlotLoyaltyBonus * GetModifierLevelFactor(slotIndex));
            placement.AdditiveMultBonus += bonus;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            var scoreEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], bonus);
            scoreEvent.TriggeringModifier = slotModifier.Value;
            scoreEvent.TriggeringModifierIndex = slotIndex;
            events.Add(scoreEvent);
            placement.ScoreEvents = events;
        }


        /// <summary>
        /// Enchanted Cards, Multitude and Experience all depend on
        /// RunManager-only state (upgraded-card count, deck size,
        /// special-pieces-played count), not grid/placement state - like
        /// ApplyHandSlotModifierBonus, GridManager can't evaluate these
        /// itself, so they're resolved here post-hoc. Loops over every held
        /// copy of each so holding more than one stacks, same convention as
        /// every other modifier.
        /// </summary>
        private void ApplyDeckStateModifierBonuses(PlacementResult placement)
        {
            if (!_activeModifiers.Contains(ModifierId.CartesEnchantees) && !_activeModifiers.Contains(ModifierId.Multitude) && !_activeModifiers.Contains(ModifierId.Experience) && !_activeModifiers.Contains(ModifierId.Arsenal))
            {
                return;
            }

            var events = new List<ScoreEvent>(placement.ScoreEvents);
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i] == ModifierId.CartesEnchantees)
                {
                    int upgradedCount = CountUpgradedDeckCards();
                    // True float, not floored to a whole "+1 Mult" step. Always at
                    // least 0.1 so this always fires; PreciseAmount lets the badge
                    // popup show the exact value rather than a misleadingly rounded
                    // one. Scaled by this slot's own level factor (see
                    // ModifierLevelUtility) same as every other modifier.
                    float trueMult = (1 + upgradedCount) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep * GetModifierLevelFactor(i);
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.CartesEnchantees;
                    // Its own position in _activeModifiers - feeds PlacementResult.Mult's
                    // ordered fold (see ApplyHandSlotModifierBonus).
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Multitude)
                {
                    int bonus = Mathf.RoundToInt(Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard * GetModifierLevelFactor(i));
                    placement.ModifierBonus += bonus;
                    var ptsEvent = new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus);
                    ptsEvent.TriggeringModifier = ModifierId.Multitude;
                    ptsEvent.TriggeringModifierIndex = i;
                    events.Add(ptsEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Experience)
                {
                    // Same true-float treatment (and level scaling) as CartesEnchantees above.
                    float trueMult = (1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep * GetModifierLevelFactor(i);
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.Experience;
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Arsenal)
                {
                    // Whole-number count (at most 5), so this joins the regular
                    // AdditiveMultBonus pool like Solidarite/MultUn, not the fractional
                    // ProgressiveAdditiveMult pool CartesEnchantees/Experience use.
                    int bonus = Mathf.RoundToInt(CountDistinctCombatKindsInDeck() * ScoringConstants.ArsenalMultPerDistinctCombatKind * GetModifierLevelFactor(i));
                    placement.AdditiveMultBonus += bonus;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], bonus);
                    multEvent.TriggeringModifier = ModifierId.Arsenal;
                    multEvent.TriggeringModifierIndex = i;
                    events.Add(multEvent);
                }
            }
            placement.ScoreEvents = events;
        }

        /// <summary>How many tokens in the whole deck currently carry a PieceTrait - the "upgraded card" count Enchanted Cards scales with.</summary>
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

        /// <summary>Arsenal's driver: how many distinct Joker-exclusive combat trait kinds (see PieceTrait.JokerCombatKinds) are currently anywhere in the deck - duplicates of the same kind count once.</summary>
        private int CountDistinctCombatKindsInDeck()
        {
            var kindsSeen = new HashSet<PieceTraitKind>();
            var deck = Deck.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Trait.HasValue && PieceTrait.IsJokerCombatKind(deck[i].Trait.Value.Kind))
                {
                    kindsSeen.Add(deck[i].Trait.Value.Kind);
                }
            }
            return kindsSeen.Count;
        }

        /// <summary>Polyvalence's tooltip driver - same distinct-category count as GridManager.ApplyPolyvalence, duplicated here for this read-only "currently" display.</summary>
        private int CountDistinctModifierCategoriesHeld()
        {
            var categoriesSeen = new HashSet<ModifierCategory>();
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                categoriesSeen.Add(ModifierCatalog.Get(_activeModifiers[i]).Category);
            }
            return categoriesSeen.Count;
        }

        /// <summary>
        /// Tallies every <see cref="ScoreEventType.Modifier"/> and <see
        /// cref="ScoreEventType.ModifierMultiplier"/> event in this
        /// placement's final score events, after every modifier bonus has
        /// already been appended. Powers the "used N times" tooltip stat -
        /// a modifier only counts as "used" the instant it actually scores.
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
        /// The scored group's total cell count, and the specific amount the
        /// cell at <paramref name="cellPos"/> itself earned (0 if not part of
        /// the scored group). Group scoring is progressive (the Nth cell is
        /// worth N*GroupBonusPerCell), so a specific cell's share can't be
        /// read off any Group event - it must be the one at that exact
        /// position.
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
        /// share onto one random other cell in the scored group. No-op if
        /// the group is just this placement's own cell.
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

        /// <summary>"Void Tile": clears one random already-filled, unlocked cell elsewhere on the grid (excludes this placement's own cells) and scores ScoringConstants.VoidBonusPerDestroyedCell for it. No-op if nothing else is eligible.</summary>
        private void ApplyVoidEffect(PlacementResult placement)
        {
            var cleared = Grid.ClearRandomFilledCell(_rng, placement.PlacedCells, out var clearedColor, out var clearedShape);
            if (cleared.HasValue)
            {
                AddDestroyedCell(placement, cleared.Value, clearedColor, clearedShape);
                AddTraitBonus(placement, cleared.Value, ScoringConstants.VoidBonusPerDestroyedCell);
            }
        }

        /// <summary>
        /// "Bastion Tile": once this placement has fully resolved (its own
        /// line clears included), the enchanted cell locks in place for the
        /// rest of the round - GridManager.CheckAndClearLines then skips it
        /// forever after, but still credits it the line-clear bonus every
        /// time its row/column completes. No-op if this same placement's own
        /// line clear already wiped the cell before we got here.
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
        /// "Kamikaze Tile": destroys the enchanted cell itself plus every
        /// already-filled, unlocked cell in its 8 surrounding tiles (Moore
        /// neighborhood), including this same placement's own other cells.
        /// Scores ScoringConstants.KamikazeBonusPerDestroyedCell per tile
        /// actually destroyed, the trait cell included.
        /// </summary>
        private void ApplyKamikazeEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            int destroyed = 0;

            var centerCell = Grid.GetCell(traitCellPos.x, traitCellPos.y);
            if (centerCell.IsFilled && !centerCell.IsLocked)
            {
                var centerColor = centerCell.FilledColor;
                var centerShape = centerCell.FilledShapeId;
                centerCell.ClearFill();
                destroyed++;
                AddDestroyedCell(placement, traitCellPos, centerColor, centerShape);
            }

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
                    if (!cell.IsFilled || cell.IsLocked)
                    {
                        continue;
                    }

                    var color = cell.FilledColor;
                    var shapeId = cell.FilledShapeId;
                    cell.ClearFill();
                    destroyed++;
                    AddDestroyedCell(placement, pos, color, shapeId);
                }
            }

            if (destroyed > 0)
            {
                AddTraitBonus(placement, traitCellPos, destroyed * ScoringConstants.KamikazeBonusPerDestroyedCell);
            }
        }

        /// <summary>
        /// Debug-only helper: instantly completes the current round as if its
        /// quota had just been reached. No-op if the run isn't InProgress.
        /// Pure core logic (no UnityEditor dependency), so the method ships
        /// in real builds; only its call site is gated behind #if
        /// UNITY_EDITOR. Still sets RoundScore to CurrentQuota
        /// unconditionally so every caller/test reading it back sees the same
        /// value - encounter rounds additionally need every enemy actually
        /// dead (see DebugDefeatAllEnemies), since EvaluateRoundEnd ignores
        /// RoundScore once HasActiveEncounter is true.
        /// </summary>
        public RunState DebugForceRoundComplete()
        {
            if (State != RunState.InProgress)
            {
                return State;
            }
            RoundScore = CurrentQuota;
            if (HasActiveEncounter)
            {
                DebugDefeatAllEnemies();
            }
            EvaluateRoundEnd();
            return State;
        }

        /// <summary>Debug-only helper: kills every enemy in <see cref="CurrentEncounter"/> outright (same cleanup as a real kill - see CleanUpDefeatedEnemy).</summary>
        private void DebugDefeatAllEnemies()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                enemy.ApplyDamage(enemy.CurrentMaxHp);
                CleanUpDefeatedEnemy(enemy);
            }
        }

        /// <summary>
        /// Debug-only helper: adds <paramref name="modifierId"/> straight to
        /// the player's active set, bypassing the shop entirely. Used by
        /// EditMode tests that need a specific modifier active without
        /// fighting shop RNG. Still respects
        /// EconomyConstants.MaxActiveModifiers and never duplicates a
        /// modifier already held.
        /// </summary>
        public bool DebugGrantModifier(ModifierId modifierId)
        {
            if (_activeModifiers.Contains(modifierId) || _activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            AddActiveModifier(modifierId);
            return true;
        }

        /// <summary>Debug-only helper: replaces the current encounter outright with brand-new instances of exactly these ids, bypassing EncounterCatalog.</summary>
        public void DebugSetEncounter(params EnemyId[] ids)
        {
            var instances = new List<EnemyInstance>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                instances.Add(new EnemyInstance(EnemyCatalog.Get(ids[i])));
            }
            _currentEncounter = instances;
        }

        /// <summary>Debug-only helper: adds Lueur directly, bypassing gameplay.</summary>
        public void DebugGrantLueur(int amount)
        {
            Lueur += amount;
        }

        /// <summary>Debug-only helper: pins Lueur to an exact value, bypassing gameplay - lets EditMode tests establish a known baseline before asserting purchase/reroll outcomes.</summary>
        public void DebugSetLueur(int amount)
        {
            Lueur = amount;
        }

        /// <summary>Debug-only helper: pins ShufflesRemaining to an exact value, bypassing gameplay - avoids re-dealing the hand via ShuffleHand for a deliberately set-up deterministic scenario.</summary>
        public void DebugSetShufflesRemaining(int amount)
        {
            ShufflesRemaining = amount;
        }

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
            for (int i = 0; i < ids.Count; i++)
            {
                var instance = new EnemyInstance(EnemyCatalog.Get(ids[i]));
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
            int boostedDamage = _activeModifiers.Contains(ModifierId.RenfortJoker)
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
            if (boostedDamage > 0 && _activeModifiers.Contains(ModifierId.Siphon))
            {
                Lueur += Mathf.FloorToInt(boostedDamage * ScoringConstants.SiphonLueurFraction);
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
                Lueur += Mathf.FloorToInt(damage * ScoringConstants.SangsueLueurFraction);
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

        /// <summary>When an enemy dies, its active effects are cancelled/cleaned up immediately: Locker's current lock is released, and every tile Poisoner poisoned is normalized.</summary>
        private void CleanUpDefeatedEnemy(EnemyInstance enemy)
        {
            if (enemy.LockedCell.HasValue)
            {
                var lockedCell = Grid.GetCell(enemy.LockedCell.Value);
                lockedCell.IsLocked = false;
                lockedCell.IsLineClearObstacle = false;
                enemy.LockedCell = null;
            }
            for (int i = 0; i < enemy.PoisonedCells.Count; i++)
            {
                Grid.GetCell(enemy.PoisonedCells[i]).IsPoisoned = false;
            }
            enemy.ClearPoisonedCells();
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
        private bool ResolveThiefShuffleEffect()
        {
            bool stole = false;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (!enemy.IsDead && enemy.Definition.Id == EnemyId.Thief)
                {
                    if (Deck.StealRandomHandTile(_rng))
                    {
                        stole = true;
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

        /// <summary>Rolls every slot fresh — called once, the instant the shop opens (see EvaluateRoundEnd).</summary>
        private void OpenShop()
        {
            _purchasesThisVisit = 0;
            _rerollsThisVisit = 0;
            PendingUpgrade = null;
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                _blisterSlots[i] = RollBlisterSlot();
            }
            for (int i = 0; i < _upgradeSlots.Length; i++)
            {
                _upgradeSlots[i] = RollUpgradeSlot();
            }
        }

        /// <summary>
        /// One "Blister" slot's roll - a single flat weighted bag holding
        /// every not-yet-held/not-already-offered-this-visit modifier
        /// (uniform weight, matching the Common upgrade-rarity weight)
        /// alongside every eligible upgrade (its own rarity weight, see
        /// UpgradeRarityUtility, multiplied by
        /// EconomyConstants.BlisterUpgradeWeightMultiplier). An upgrade
        /// needing a prerequisite the player doesn't meet yet (Modifier
        /// Upgrade with no modifiers owned, Random Modifier already at the
        /// cap) is excluded from the bag entirely - unlike a Casino slot, a
        /// Blister slot's exact identity is always visible, so an
        /// obviously-dead card would just read as broken.
        /// </summary>
        private ShopSlot RollBlisterSlot()
        {
            var modifierCandidates = new List<ModifierDefinition>();
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var candidate = ModifierCatalog.All[i];
                if (_activeModifiers.Contains(candidate.Id) || IsModifierAlreadyOfferedInBlister(candidate.Id))
                {
                    continue;
                }
                modifierCandidates.Add(candidate);
            }
            var upgradeCandidates = new List<UpgradeDefinition>();
            for (int i = 0; i < UpgradeCatalog.All.Length; i++)
            {
                var candidate = UpgradeCatalog.All[i];
                if (!IsUpgradeEligibleForOffer(candidate) || IsUpgradeAlreadyOfferedInBlister(candidate.Id))
                {
                    continue;
                }
                upgradeCandidates.Add(candidate);
            }

            const int ModifierBlisterWeight = 8; // matches UpgradeRarityUtility.GetDraftWeight(Common)
            int totalWeight = modifierCandidates.Count * ModifierBlisterWeight;
            for (int i = 0; i < upgradeCandidates.Count; i++)
            {
                totalWeight += UpgradeRarityUtility.GetDraftWeight(upgradeCandidates[i].Rarity) * EconomyConstants.BlisterUpgradeWeightMultiplier;
            }
            if (totalWeight <= 0)
            {
                // Only possible once held+offered modifiers AND every
                // eligible upgrade together cover both whole catalogs —
                // extremely unlikely with 70+/21+ entries, but stay
                // defensive rather than throw.
                for (int i = 0; i < ModifierCatalog.All.Length; i++)
                {
                    if (!_activeModifiers.Contains(ModifierCatalog.All[i].Id))
                    {
                        return ShopSlot.ForModifier(ModifierCatalog.All[i].Id);
                    }
                }
                return ShopSlot.ForModifier(ModifierCatalog.All[0].Id);
            }

            int roll = _rng.Next(totalWeight);
            int cumulative = 0;
            for (int i = 0; i < modifierCandidates.Count; i++)
            {
                cumulative += ModifierBlisterWeight;
                if (roll < cumulative)
                {
                    return ShopSlot.ForModifier(modifierCandidates[i].Id);
                }
            }
            for (int i = 0; i < upgradeCandidates.Count; i++)
            {
                cumulative += UpgradeRarityUtility.GetDraftWeight(upgradeCandidates[i].Rarity) * EconomyConstants.BlisterUpgradeWeightMultiplier;
                if (roll < cumulative)
                {
                    return ShopSlot.ForUpgrade(upgradeCandidates[i]);
                }
            }
            // Shouldn't happen given roll < totalWeight computed the same
            // way, but stay defensive rather than fall off the end.
            return ShopSlot.ForUpgrade(upgradeCandidates[upgradeCandidates.Count - 1]);
        }

        private bool IsModifierAlreadyOfferedInBlister(ModifierId id)
        {
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                if (_blisterSlots[i] != null && _blisterSlots[i].Kind == ShopSlotKind.Modifier && _blisterSlots[i].ModifierId == id)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsUpgradeAlreadyOfferedInBlister(UpgradeId id)
        {
            for (int i = 0; i < _blisterSlots.Length; i++)
            {
                if (_blisterSlots[i] != null && _blisterSlots[i].Kind == ShopSlotKind.Upgrade && _blisterSlots[i].HiddenUpgrade.Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether <paramref name="candidate"/> could actually be applied right now - see RollBlisterSlot's own doc comment for why this only matters for Blister, not Casino.</summary>
        private bool IsUpgradeEligibleForOffer(UpgradeDefinition candidate)
        {
            if (candidate.Id == UpgradeId.RandomModifier && _activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return false;
            }
            if (candidate.Id == UpgradeId.ModifierUpgrade && _activeModifiers.Count == 0)
            {
                return false;
            }
            return true;
        }

        private ShopSlot RollUpgradeSlot()
        {
            int roll = _rng.Next(100);
            UpgradePool pool;
            int modifierCutoff = EconomyConstants.ModifierUpgradePoolChancePercent;
            int masteryCutoff = modifierCutoff + EconomyConstants.MasteryUpgradePoolChancePercent;
            int bankCutoff = masteryCutoff + EconomyConstants.BankUpgradePoolChancePercent;
            if (roll < modifierCutoff)
            {
                pool = UpgradePool.Modifier;
            }
            else if (roll < masteryCutoff)
            {
                pool = UpgradePool.Mastery;
            }
            else if (roll < bankCutoff)
            {
                pool = UpgradePool.Bank;
            }
            else
            {
                pool = UpgradePool.Grid;
            }
            var upgrade = Upgrades.RollFromPool(pool);
            return ShopSlot.ForUpgrade(upgrade);
        }

        /// <summary>Current Lueur price of Blister slot <paramref name="index"/> - a Modifier-kind slot varies per modifier (see ModifierPricing); an Upgrade-kind slot uses the same Bank/Grid base price as a Casino slot (see GetUpgradeSlotPrice). Includes this visit's escalation (see EconomyConstants.ShopPriceEscalationPerPurchase).</summary>
        public int GetBlisterSlotPrice(int index)
        {
            if (index < 0 || index >= _blisterSlots.Length || _blisterSlots[index] == null)
            {
                return 0;
            }
            var slot = _blisterSlots[index];
            int basePrice = slot.Kind == ShopSlotKind.Modifier
                ? ModifierPricing.GetPrice(slot.ModifierId)
                : (slot.Pool == UpgradePool.Grid ? EconomyConstants.GridUpgradeShopBasePrice : EconomyConstants.BankUpgradeShopBasePrice);
            return ComputePrice(basePrice);
        }

        /// <summary>Current Lueur price of upgrade slot <paramref name="index"/> - Grid-pool slots cost more than Bank-pool ones, including this visit's escalation. Mastery-pool slots share Bank's (lower) price.</summary>
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
            return Mathf.RoundToInt(EconomyConstants.ShopRerollBasePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _rerollsThisVisit));
        }

        private int ComputePrice(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _purchasesThisVisit));
        }

        /// <summary>
        /// Buys Blister slot <paramref name="index"/> outright - a
        /// Modifier-kind slot applies immediately; an Upgrade-kind slot goes
        /// through the exact same resolution as a Casino purchase (see
        /// ApplyPurchasedUpgrade/BuyUpgradeSlot). Fails (no charge, no state
        /// change) if the shop isn't open, the slot is invalid/already
        /// bought, the player can't afford it, or (Modifier-kind only)
        /// they're already at EconomyConstants.MaxActiveModifiers.
        /// </summary>
        /// </summary>
        public bool BuyBlisterSlot(int index)
        {
            if (State != RunState.AwaitingShop || PendingUpgrade != null)
            {
                return false;
            }
            if (index < 0 || index >= _blisterSlots.Length || _blisterSlots[index] == null || _blisterSlots[index].Purchased)
            {
                return false;
            }
            var slot = _blisterSlots[index];

            if (slot.Kind == ShopSlotKind.Modifier)
            {
                if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
                {
                    return false;
                }
                int price = GetBlisterSlotPrice(index);
                if (Lueur < price)
                {
                    return false;
                }

                Lueur -= price;
                _purchasesThisVisit++;
                slot.Purchased = true;

                // "Mimic" (Copieur) isn't a real modifier of its own - buying it
                // adds another copy of whichever modifier was purchased immediately
                // before it instead. A no-op (still costs Lueur, still marks the
                // slot sold) if nothing has been purchased yet this run.
                // _lastPurchasedModifierId only ever tracks a real purchase, never
                // Copieur itself, so buying several Mimics in a row all copy the
                // same underlying modifier rather than chaining off each other.
                if (slot.ModifierId == ModifierId.Copieur)
                {
                    if (_lastPurchasedModifierId.HasValue)
                    {
                        AddActiveModifier(_lastPurchasedModifierId.Value);
                    }
                }
                else
                {
                    AddActiveModifier(slot.ModifierId);
                    _lastPurchasedModifierId = slot.ModifierId;
                }
                return true;
            }

            var hiddenUpgrade = slot.HiddenUpgrade;
            if (!IsUpgradeEligibleForOffer(hiddenUpgrade))
            {
                return false;
            }
            int upgradePrice = GetBlisterSlotPrice(index);
            if (Lueur < upgradePrice)
            {
                return false;
            }
            Lueur -= upgradePrice;
            _purchasesThisVisit++;
            slot.Purchased = true;
            return ApplyPurchasedUpgrade(hiddenUpgrade);
        }

        /// <summary>
        /// Buys Casino upgrade slot <paramref name="index"/> - the specific
        /// upgrade underneath (only its <see cref="UpgradePool"/> was ever
        /// shown) gets revealed as <see cref="PendingUpgrade"/> (see
        /// ApplyPurchasedUpgrade). Fails (no charge) under the same
        /// conditions as <see cref="BuyBlisterSlot"/> (minus the modifier
        /// cap, which doesn't apply to upgrades in general) except for
        /// "Random Modifier" specifically, which does respect the cap:
        /// buying it while already at
        /// EconomyConstants.MaxActiveModifiers still charges Lueur and
        /// grants nothing (see GrantRandomModifier).
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
            var hiddenUpgrade = _upgradeSlots[index].HiddenUpgrade;
            if (!IsUpgradeEligibleForOffer(hiddenUpgrade))
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
            return ApplyPurchasedUpgrade(slot.HiddenUpgrade);
        }

        /// <summary>
        /// Shared resolution for a just-paid-for upgrade purchase, whichever
        /// section it came from (Blister or Casino). A Bank-pool upgrade
        /// with no sub-choice (Joker) applies immediately and leaves
        /// PendingUpgrade null; one that needs a sub-choice
        /// (Replace/Dupliquer/Recolorer) or a Grid-pool upgrade leaves
        /// PendingUpgrade set until <see cref="ResolveUpgradeSubChoice"/>/
        /// <see cref="ResolveUpgradeTileChoice"/> finishes it.
        /// </summary>
        private bool ApplyPurchasedUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade.Pool == UpgradePool.Grid)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTileCandidates = Upgrades.GetCandidateTilesFor(upgrade, Deck);
                return true;
            }

            // Random Piece is Bank pool with a sub-choice, like Replace/
            // Dupliquer/Recolorer below, but its candidates are freshly rolled
            // pieces rather than existing deck types - handled by its own
            // Pending*/Resolve* pair (see PendingUpgradePieceCandidates/
            // ResolveUpgradePieceChoice).
            if (upgrade.Id == UpgradeId.RandomPiece)
            {
                PendingUpgrade = upgrade;
                PendingUpgradePieceCandidates = Upgrades.GetCandidatePiecesFor(upgrade);
                return true;
            }

            // Modifier Upgrade is a third distinct Bank sub-choice shape - its
            // candidates are simply every slot the player already owns
            // (ActiveModifiers itself), so there's no Pending*Candidates list
            // to populate here (see ResolveModifierUpgradeChoice).
            if (upgrade.Id == UpgradeId.ModifierUpgrade)
            {
                PendingUpgrade = upgrade;
                return true;
            }

            if (upgrade.RequiresSubChoice)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTypeCandidates = Upgrades.GetCandidateTypesFor(upgrade, Deck);
                return true;
            }

            // The remaining upgrades with RequiresSubChoice false apply through
            // their own dedicated path (ApplyJoker / GrantRandomModifier /
            // GrantShapeMastery / GrantColorMastery, not a generic Apply) so
            // the actual thing rolled can be surfaced for the reveal to show
            // (see UpgradeRevealView / ShapeCarouselView / ColorCarouselView)
            // instead of just naming the upgrade.
            if (upgrade.Id == UpgradeId.JokerPiece)
            {
                LastJokerShapeAdded = Upgrades.ApplyJoker(Deck, out var combatKind);
                LastJokerCombatKindAdded = combatKind;
            }
            else if (upgrade.Id == UpgradeId.PieceMastery)
            {
                LastShapeMasteryGranted = GrantShapeMastery();
            }
            else if (upgrade.Id == UpgradeId.ColorMastery)
            {
                LastColorMasteryGranted = GrantColorMastery();
            }
            else
            {
                LastRandomModifierGranted = GrantRandomModifier();
            }
            return true;
        }

        /// <summary>
        /// Debug-only helper: exercises the exact same effect + reveal-
        /// surfacing (LastRandomModifierGranted) that buying the "Random
        /// Modifier" upgrade for real would, without needing to
        /// find/afford/click it in the shop first.
        /// </summary>
        /// </summary>
        public ModifierId? DebugTriggerRandomModifierGrant()
        {
            LastRandomModifierGranted = GrantRandomModifier();
            return LastRandomModifierGranted;
        }

        /// <summary>Debug-only helper: exercises "Piece Mastery"'s exact grant (GrantShapeMastery), bypassing the shop entirely.</summary>
        public ShapeId DebugTriggerShapeMasteryGrant()
        {
            LastShapeMasteryGranted = GrantShapeMastery();
            return LastShapeMasteryGranted.Value;
        }

        /// <summary>Debug-only helper: exercises "Color Mastery"'s exact grant (GrantColorMastery), bypassing the shop entirely.</summary>
        public PieceColor DebugTriggerColorMasteryGrant()
        {
            LastColorMasteryGranted = GrantColorMastery();
            return LastColorMasteryGranted.Value;
        }

        /// <summary>
        /// Debug-only helper: overwrites upgrade slot <paramref
        /// name="index"/> (default 0) to be "Random Modifier" outright,
        /// bypassing the shop's own roll - unlike
        /// DebugTriggerRandomModifierGrant above, this goes through the real
        /// purchase button (BuyUpgradeSlot), exercising its exact branching
        /// instead of skipping straight to GrantRandomModifier. No-op
        /// (false) if the shop isn't currently open.
        /// </summary>
        public bool DebugForceUpgradeSlotToRandomModifier(int index = 0)
        {
            if (State != RunState.AwaitingShop || index < 0 || index >= _upgradeSlots.Length)
            {
                return false;
            }
            _upgradeSlots[index] = ShopSlot.ForUpgrade(UpgradeCatalog.RandomModifier);
            return true;
        }

        /// <summary>
        /// Debug-only helper: overwrites Blister slot <paramref
        /// name="index"/> (default 0) to hold <paramref
        /// name="modifierId"/> outright, bypassing the shop's own roll -
        /// lets a test buy a specific modifier through the real purchase
        /// path (BuyBlisterSlot), which RerollShop can't reach into since it
        /// only ever touches Casino. No-op (false) if the shop isn't
        /// currently open.
        /// </summary>
        public bool DebugForceBlisterSlotToModifier(ModifierId modifierId, int index = 0)
        {
            if (State != RunState.AwaitingShop || index < 0 || index >= _blisterSlots.Length)
            {
                return false;
            }
            _blisterSlots[index] = ShopSlot.ForModifier(modifierId);
            return true;
        }

        /// <summary>
        /// "Random Modifier" upgrade's actual effect - grants one uniformly
        /// random modifier the player doesn't already hold, from the same
        /// catalog RollBlisterSlot draws from, minus its "already offered
        /// this shop visit" exclusion. Updates _lastPurchasedModifierId the
        /// same way a direct modifier-slot purchase does. Returns null -
        /// grants nothing, though the Lueur already spent is not refunded -
        /// if the player is already at EconomyConstants.MaxActiveModifiers
        /// or already holds every modifier in the catalog.
        /// </summary>
        private ModifierId? GrantRandomModifier()
        {
            if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers)
            {
                return null;
            }
            var available = new List<ModifierDefinition>(ModifierCatalog.All.Length);
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                if (!_activeModifiers.Contains(ModifierCatalog.All[i].Id))
                {
                    available.Add(ModifierCatalog.All[i]);
                }
            }
            if (available.Count == 0)
            {
                return null;
            }

            var picked = available[_rng.Next(available.Count)].Id;
            AddActiveModifier(picked);
            _lastPurchasedModifierId = picked;
            return picked;
        }

        /// <summary>
        /// "Piece Mastery": picks a random exact shape (uniform over all 8 -
        /// no exclusion, since a shape can always be leveled up further) and
        /// levels it up by a random amount from 1 to <see
        /// cref="EconomyConstants.MasteryUpgradeMaxLevelGain"/>. Always
        /// succeeds - there's no cap to hit.
        /// </summary>
        private ShapeId GrantShapeMastery()
        {
            var shapes = (ShapeId[])System.Enum.GetValues(typeof(ShapeId));
            var picked = shapes[_rng.Next(shapes.Length)];
            int gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain);
            _shapeMasteryLevels[picked] = GetShapeMasteryLevel(picked) + gain;
            return picked;
        }

        /// <summary>"Color Mastery": Piece Mastery's exact sibling - picks a random base color (PieceColorUtility.BaseColors, never Joker) and levels it up by the same random amount. Always succeeds.</summary>
        private PieceColor GrantColorMastery()
        {
            var colors = PieceColorUtility.BaseColors;
            var picked = colors[_rng.Next(colors.Count)];
            int gain = 1 + _rng.Next(EconomyConstants.MasteryUpgradeMaxLevelGain);
            _colorMasteryLevels[picked] = GetColorMasteryLevel(picked) + gain;
            return picked;
        }

        /// <summary>Resolves a Bank-pool <see cref="PendingUpgrade"/> that needed a sub-choice (which piece type, and for Recolorer which target color, or for Replace which SECOND piece type to duplicate in its place). No-op (false) if nothing is pending or it's actually a Grid-pool upgrade.</summary>
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

        /// <summary>Resolves "Random Piece"'s sub-choice — <paramref name="candidateIndex"/> must index into <see cref="PendingUpgradePieceCandidates"/> (not validated beyond range here; the presentation layer only ever offers those). No-op (false) if nothing is pending or it's actually a different upgrade.</summary>
        public bool ResolveUpgradePieceChoice(int candidateIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.RandomPiece)
            {
                return false;
            }
            if (candidateIndex < 0 || candidateIndex >= PendingUpgradePieceCandidates.Count)
            {
                return false;
            }

            Upgrades.ApplyChosenPiece(PendingUpgradePieceCandidates[candidateIndex], Deck);
            PendingUpgrade = null;
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            return true;
        }

        /// <summary>Resolves "Modifier Upgrade"'s sub-choice — <paramref name="slotIndex"/> indexes directly into <see cref="ActiveModifiers"/> (not a separate Pending*Candidates list, since every owned modifier is eligible — see BuyUpgradeSlot). Raises that slot's level by one (see GetModifierLevel/ModifierLevelUtility); no-op (false) if nothing is pending, it's actually a different upgrade, or the index is out of range.</summary>
        public bool ResolveModifierUpgradeChoice(int slotIndex)
        {
            if (PendingUpgrade == null || PendingUpgrade.Id != UpgradeId.ModifierUpgrade)
            {
                return false;
            }
            if (!IsValidModifierIndex(slotIndex))
            {
                return false;
            }

            _modifierLevels[slotIndex]++;
            PendingUpgrade = null;
            return true;
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
        /// Refreshes every Casino slot - purchased or not - with a new
        /// random offer. Never touches the Blister section: it already
        /// shows its exact contents up front, so a reroll there would just
        /// be "pay Lueur to see 3 different exact items," a very different
        /// kind of purchase from Casino's "pay Lueur to try your luck at the
        /// mystery box again." Buying a slot still permanently grants
        /// whatever it held (the upgrade is already applied by then) - this
        /// only replaces the slot offer itself, giving the player a fresh
        /// purchasable pick where a spent one used to sit. Costs Lueur (see
        /// GetRerollPrice), escalating only from further rerolls this same
        /// visit (see _rerollsThisVisit), not from slot purchases.
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
            _rerollsThisVisit++;
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
