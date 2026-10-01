using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Orchestrates the 8-round run: quotas, piece budgets, boss round locking,
    /// and the win/lose transition into the upgrade draft (spec sections 1 and 6).
    ///
    /// A round ends the instant its quota is reached (success), or as soon as
    /// either its piece budget runs out or its hand becomes unplayable AND
    /// out of shuffles — no legal placement left anywhere on the grid for
    /// any of the 3 hand pieces, and no ShufflesRemaining charge left to
    /// re-roll them — without having reached the quota (defeat). See
    /// ShuffleHand.
    /// </summary>
    public sealed class RunManager
    {
        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

        private readonly List<ModifierId> _activeModifiers = new List<ModifierId>();

        // Parallel to _activeModifiers (same index == same slot), never
        // exposed as its own list — kept a plain List<int> rather than
        // promoting _activeModifiers itself to hold (ModifierId, int)
        // pairs, since ActiveModifiers's type is read directly by ~150
        // GridManagerModifierTests call sites and several RunManagerTests
        // collection-equality asserts that would all break for a change
        // completely unrelated to what they're actually testing. Every
        // mutation of _activeModifiers (add/remove/swap/move) MUST mirror
        // the same operation here in the same call — see AddActiveModifier/
        // RemoveActiveModifierAt below, the only two places that touch
        // _activeModifiers.Add/RemoveAt directly; Swap/MoveModifier mirror
        // inline since they're simple index swaps, not a count change.
        private readonly List<int> _modifierLevels = new List<int>();

        /// <summary>Modifiers currently held by the player, persisting for the whole run (never reset between rounds). Capped at EconomyConstants.MaxActiveModifiers now that the shop lets Lueur buy them far more freely than the old one-per-round draft ever could.</summary>
        public IReadOnlyList<ModifierId> ActiveModifiers
        {
            get { return _activeModifiers; }
        }

        /// <summary>
        /// The level of the modifier at <paramref name="index"/> in <see
        /// cref="ActiveModifiers"/> — 1 (today's exact, un-leveled
        /// behavior) for anything never touched by the "Modifier Upgrade"
        /// shop upgrade (see ResolveModifierUpgradeChoice), or an
        /// out-of-range index. See ModifierLevelUtility for how a level
        /// translates into a scoring multiplier.
        /// </summary>
        public int GetModifierLevel(int index)
        {
            return index >= 0 && index < _modifierLevels.Count ? _modifierLevels[index] : 1;
        }

        /// <summary>The scoring multiplier for modifier slot <paramref name="index"/> (see ModifierLevelUtility) — used by ApplyHandSlotModifierBonus/ApplyDeckStateModifierBonuses below, the 4 modifiers resolved here in RunManager rather than through GridManager's own dispatch tables (see GridManager.GetModifierLevelFactor for its own, otherwise-identical counterpart).</summary>
        private float GetModifierLevelFactor(int index)
        {
            return ModifierLevelUtility.LevelToFactor(GetModifierLevel(index));
        }

        /// <summary>Every place that adds a new modifier slot (a real purchase, Copieur's copy, the Random Modifier gamble, or the free starting grant) goes through here — the ONLY place _activeModifiers.Add is called — so a fresh slot's level (always 1) can never be forgotten.</summary>
        private void AddActiveModifier(ModifierId id)
        {
            _activeModifiers.Add(id);
            _modifierLevels.Add(1);
        }

        /// <summary>Counterpart to AddActiveModifier — the ONLY place _activeModifiers.RemoveAt is called, so a removed slot's level always disappears along with it instead of leaking into whatever modifier happens to reflow into that index afterward.</summary>
        private void RemoveActiveModifierAt(int index)
        {
            _activeModifiers.RemoveAt(index);
            _modifierLevels.RemoveAt(index);
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

            // Level belongs to the specific granted copy, not the slot
            // position — it must travel WITH the modifier it was spent on,
            // not stay behind at the old index.
            var tempLevel = _modifierLevels[indexA];
            _modifierLevels[indexA] = _modifierLevels[indexB];
            _modifierLevels[indexB] = tempLevel;
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

            // Same "level travels with the modifier, not the slot" reasoning
            // as SwapModifiers above.
            var movedLevel = _modifierLevels[fromIndex];
            _modifierLevels.RemoveAt(fromIndex);
            _modifierLevels.Insert(toIndex, movedLevel);
        }

        /// <summary>
        /// Sells the modifier at <paramref name="index"/> in <see
        /// cref="ActiveModifiers"/> for Lueur (spec extension, explicit
        /// request: "Le joueur devrait pouvoir sell modifier lorsqu'il
        /// hover dessus" — see Presentation.GameBootstrap's key binding).
        /// Refunds <see cref="ModifierPricing"/>'s BASE catalog price minus
        /// 1 ("Le prix de vente d'un modifier est prix initial-1"), never
        /// whatever escalated price a shop purchase of it might actually
        /// have cost, and the same amount regardless of how it was
        /// obtained (bought, Copieur-copied, or the free starting
        /// modifier). No-op (returns false, nothing sold or refunded) for
        /// an out-of-range index.
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

        /// <summary>The last REAL modifier actually added by a shop purchase this run (never Copieur/Mimic itself, see BuyBlisterSlot) — null until the player's first purchase. "Mimic" (Copieur) reads this to decide which modifier it copies.</summary>
        private ModifierId? _lastPurchasedModifierId;

        /// <summary>
        /// "Lueur" currency (spec extension, explicit request — a Balatro-
        /// style economy): earned by clearing lines with DIVERSE colors (see
        /// GridManager.PlacementResult.LueurEarned), persists for the whole
        /// run like <see cref="TotalScore"/>, and spent in the between-round
        /// shop (see <see cref="ShopBlisterSlots"/>/<see cref="ShopUpgradeSlots"/>).
        /// </summary>
        public int Lueur { get; private set; }

        /// <summary>How many hand shuffles the player has left this run (spec extension, explicit request — see RunConfig.StartingShuffleCount/ShuffleHand). Persists across rounds like Lueur, never reset by StartRound.</summary>
        public int ShufflesRemaining { get; private set; }

        private readonly ShopSlot[] _blisterSlots = new ShopSlot[EconomyConstants.ShopBlisterSlotCount];
        private readonly ShopSlot[] _upgradeSlots = new ShopSlot[EconomyConstants.ShopUpgradeSlotCount];

        /// <summary>How many slot purchases (Blister or Casino) have happened in the CURRENT shop visit — every one raises the price of every OTHER slot still on offer (see GetBlisterSlotPrice/GetUpgradeSlotPrice), reset to 0 each time the shop opens. Kept separate from <see cref="_rerollsThisVisit"/> (on explicit report: "Les reroll devraient augmenter de prix seulement lorsqu'on reroll") — a slot purchase no longer escalates the reroll price, only rerolling itself does.</summary>
        private int _purchasesThisVisit;

        /// <summary>How many times the shop has been rerolled this visit — drives ONLY GetRerollPrice's own escalation (see _purchasesThisVisit's doc comment for why this is a separate counter), reset to 0 each time the shop opens.</summary>
        private int _rerollsThisVisit;

        /// <summary>The "Blister" section (see EconomyConstants.ShopBlisterSlotCount) — a modifier OR upgrade, drawn from one shared bag, always fully revealed. Never touched by RerollShop.</summary>
        public IReadOnlyList<ShopSlot> ShopBlisterSlots
        {
            get { return _blisterSlots; }
        }

        /// <summary>The "Casino" section (see EconomyConstants.ShopUpgradeSlotCount) — always an Upgrade-kind slot, only its UpgradePool shown until purchased. The only section RerollShop touches.</summary>
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

        /// <summary>Candidate freshly-rolled PIECES (trait included) for <see cref="PendingUpgrade"/>'s sub-choice — only populated for "Random Piece" (see UpgradeSystem.GetCandidatePiecesFor); empty otherwise. A separate list from PendingUpgradeTypeCandidates since these don't exist in the deck yet — there's no deck index to hand back, the token itself IS the candidate.</summary>
        public IReadOnlyList<PieceToken> PendingUpgradePieceCandidates { get; private set; }

        /// <summary>The shape most recently rolled by a Joker purchase (see BuyUpgradeSlot/UpgradeSystem.ApplyJoker) — read once by the presentation layer (UpgradeRevealView) right after the purchase to show the real piece that got added instead of just describing the upgrade in text. Meaningless before any Joker purchase this run.</summary>
        public ShapeId LastJokerShapeAdded { get; private set; }

        /// <summary>The modifier most recently granted by a "Random Modifier" purchase (see BuyUpgradeSlot/GrantRandomModifier) — read once by the presentation layer (UpgradeRevealView) right after the purchase. Null if the gamble didn't pay off (already at EconomyConstants.MaxActiveModifiers, or — practically impossible — every modifier already held), in which case the purchase still cost its Lueur but granted nothing. Meaningless before any Random Modifier purchase this run.</summary>
        public ModifierId? LastRandomModifierGranted { get; private set; }

        /// <summary>The modifier granted for free by <see cref="GrantStartingModifier"/> at the start of this run — read once by the presentation layer (ModifierCarouselView) to know which badge the spin has to land on. Null until that's called.</summary>
        public ModifierId? StartingModifier { get; private set; }

        /// <summary>The shape most recently leveled up by a "Piece Mastery" purchase (see BuyUpgradeSlot/GrantShapeMastery) — read once by the presentation layer (ShapeCarouselView) to know which shape the spin has to land on. Null before any Piece Mastery purchase this run.</summary>
        public ShapeId? LastShapeMasteryGranted { get; private set; }

        /// <summary>
        /// Current level per exact shape, from "Piece Mastery" purchases
        /// (see GrantShapeMastery/StampMasteryBonuses) — 1 (no bonus)
        /// for any shape never leveled up. Unlike the modifier-level system
        /// (<see cref="_modifierLevels"/>, parallel to <see
        /// cref="_activeModifiers"/>), this isn't tied to any held modifier
        /// slot — a shape's level persists independently of anything the
        /// player currently holds, keyed directly by <see cref="ShapeId"/>.
        /// </summary>
        private readonly Dictionary<ShapeId, int> _shapeMasteryLevels = new Dictionary<ShapeId, int>();

        /// <summary>Current level of <paramref name="shape"/> (1 if never leveled up by a Piece Mastery purchase).</summary>
        public int GetShapeMasteryLevel(ShapeId shape)
        {
            return _shapeMasteryLevels.TryGetValue(shape, out var level) ? level : 1;
        }

        /// <summary>Piece Mastery's exact sibling (on explicit request: "Il faudrait faire la même chose avec les couleurs") — same mechanics, keyed by PieceColor instead of ShapeId.</summary>
        public PieceColor? LastColorMasteryGranted { get; private set; }

        /// <summary>Current level per color, from "Color Mastery" purchases (see GrantColorMastery/StampMasteryBonuses) — 1 (no bonus) for any color never leveled up.</summary>
        private readonly Dictionary<PieceColor, int> _colorMasteryLevels = new Dictionary<PieceColor, int>();

        /// <summary>Current level of <paramref name="color"/> (1 if never leveled up by a Color Mastery purchase).</summary>
        public int GetColorMasteryLevel(PieceColor color)
        {
            return _colorMasteryLevels.TryGetValue(color, out var level) ? level : 1;
        }

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
        /// placement already applied. Densite/CartesEnchantees/Experience
        /// are ADDITIVE contributors to the SAME "+Mult" pool as
        /// Solidarite/MultUn (see PlacementResult.ProgressiveAdditiveMult),
        /// not a multiplier of their own, so they show "+N Mult", NOT "xN"
        /// — showing "x" here was a bug for CartesEnchantees/Experience
        /// (on explicit report: "tu as oublié la baseline de 1 et non de
        /// 0"): their own raw contribution starts at +0.1 (never +0,
        /// "counting from a baseline of 1" card), which read as a NERF
        /// ("x0.1") under the old "x" phrasing instead of the bonus it
        /// actually is. Densite joined this same "+N Mult" family later,
        /// converted from an "xN" multiplier (explicit request: "Density
        /// modifier devrait +n mult au lieu de xn mult ET devrait être un
        /// float au lieu d'un int") — unlike the other two, it has no
        /// "start at 1" baseline (a genuinely empty board correctly shows
        /// "+0 Mult"). The separate "+1" that makes the OVERALL Mult never
        /// drop below x1 is PlacementResult.Mult's own baseline, added
        /// once, game-wide, not specific to any of these three modifiers.
        ///
        /// <paramref name="index"/> is this slot's own position in <see
        /// cref="ActiveModifiers"/> (optional, -1 by default) — when given,
        /// every case below is scaled by that slot's own level factor (see
        /// ModifierLevelUtility), matching the scaling every one of these
        /// already gets for real at score time (ApplyDeckStateModifierBonuses'
        /// own <c>* GetModifierLevelFactor(i)</c>, or the generic per-event
        /// rescale in GridManager.ApplyPreClearModifiers/ApplyPostClearModifiers
        /// for the 3 Grid-tracked ones). Left at -1 (factor 1, today's exact
        /// behavior) by every caller that doesn't know its own slot index —
        /// bug fix (on explicit report: "Enchanted cards modifier on dirait
        /// que le max est 1.0"): without this, a leveled-up copy's tooltip
        /// kept showing the UN-leveled value, which for Enchanted Cards
        /// naturally caps just under +1.0 Mult at level 1 (its formula only
        /// exceeds 1.0 past 9 upgraded deck cards) — reading exactly like a
        /// hard ceiling even though the modifier was actually scoring more.
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

        /// <summary>How much Lueur the round that just ended converted its unused piece budget into (see EvaluateRoundEnd) — 0 until the first round ends, read once by GameBootstrap right after transitioning to AwaitingShop to replay it as a popup sequence, then meaningless again until the next round ends.</summary>
        public int LastRoundEndLueurBonus { get; private set; }

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
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            ShufflesRemaining = RunConfig.StartingShuffleCount;
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
            LastRoundEndLueurBonus = 0;
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

            StampMasteryBonuses(shape, placementColor, x, y);
            var transientTraitCells = ApplyTokenTrait(token.Trait, traitCellPos);
            var placement = Grid.PlacePiece(shape, placementColor, x, y, _activeModifiers, _modifierLevels);
            ClearTokenTraitCells(transientTraitCells);
            if (token.Trait.HasValue)
            {
                ApplyPostPlacementTraitBonus(token.Trait.Value, traitCellPos.Value, sparkStreakBeforePlacement, placement);
            }
            ApplyHandSlotModifierBonus(handIndex, placement);
            ApplyDeckStateModifierBonuses(placement);
            CountModifierUsage(placement);
            RemoveDepletedEpuisement();
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
        /// Stamps EVERY cell of this placement's own piece (shape.Cells
        /// offsets against the (x,y) anchor) with whatever flat Mastery
        /// bonus its exact shape/color currently carries — 0 (no-op) for
        /// either axis never leveled up. Must run before <see
        /// cref="GridManager.PlacePiece"/>, which reads <see
        /// cref="Cell.ShapeMasteryBonus"/>/<see cref="Cell.ColorMasteryBonus"/>
        /// in its own per-cell group-scoring loop — same "stamp the cell
        /// before Grid.PlacePiece runs its scoring pass" ordering as <see
        /// cref="ApplyTokenTrait"/>, and the same reason: a per-cell stamp
        /// (frozen at whatever level applied at placement time, never
        /// retroactively bumped by a later Mastery purchase) rescores every
        /// time this cell's group scores again, not just now — on explicit
        /// report that a flat once-per-PLACEMENT bonus undercounted a
        /// multi-cell piece ("Chaque tuile devient niveau 2. Donc chaque
        /// fois que cette tuile est comptabilisé on fait +1").
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
        /// entirely (scoring a flat bonus for the one tile it breaks, if
        /// any). Golden/Tinted/Multiplier/Blast/
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

        /// <summary>Records one cell a trait effect (Void Tile/Kamikaze Tile) destroyed, same mutate-the-lists-directly convention as <see cref="AddTraitBonus"/> — see <see cref="PlacementResult.DestroyedCells"/> for why this is separate from <see cref="PlacementResult.ClearedCells"/>.</summary>
        private static void AddDestroyedCell(PlacementResult placement, Vector2Int pos, PieceColor? color)
        {
            var cells = new List<Vector2Int>(placement.DestroyedCells);
            cells.Add(pos);
            placement.DestroyedCells = cells;
            var colors = new List<PieceColor?>(placement.DestroyedCellColors);
            colors.Add(color);
            placement.DestroyedCellColors = colors;
        }

        // Which SlotUn/Deux/Trois modifier corresponds to each 0-based hand index.
        private static readonly ModifierId?[] HandSlotModifiers = { ModifierId.SlotUn, ModifierId.SlotDeux, ModifierId.SlotTrois };

        /// <summary>
        /// "Slot N Loyalty": flat +Mult (additive, see
        /// PlacementResult.AdditiveMultBonus, see ScoringConstants.SlotLoyaltyBonus)
        /// when the piece was played from hand slot <paramref name="handIndex"/>
        /// (0-based) and the matching modifier is active. History: "doubles just
        /// the group bonus" (additive), then a genuine xN multiplier on the
        /// whole score ("au lieu de double group placement, on va tout
        /// doubler"), then converted back to additive on explicit request
        /// ("converting some multiplicative sources to additive") — with 3
        /// slots this fires reliably enough (1-in-3 placements) that the old
        /// xN was compounding too consistently with the game's other
        /// "always-on" multiplicative modifiers. Unlike every other modifier,
        /// GridManager.PlacePiece can't evaluate this itself — it has no idea
        /// which of the 3 hand slots a piece came from, only this method's
        /// caller (PlacePiece(handIndex, x, y)) does — so it's resolved here,
        /// the same post-hoc pattern already used for the second-batch
        /// PieceTrait kinds (see ApplyPostPlacementTraitBonus).
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

            // Its own position in _activeModifiers — needed so PlacementResult.Mult's
            // ordered left-to-right fold (see its own doc comment) places this
            // correctly relative to every other Mult modifier instead of
            // always applying it last regardless of where the player put it.
            // Also this slot's own level factor (see ModifierLevelUtility) —
            // Mult is events-driven, so scaling this event's Amount is all a
            // leveled Slot Loyalty needs.
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
                    // float"). Scaled by this slot's own level factor (see
                    // ModifierLevelUtility) same as every other modifier.
                    float trueMult = (1 + upgradedCount) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep * GetModifierLevelFactor(i);
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
                    int bonus = Mathf.RoundToInt(Deck.DeckCount * ScoringConstants.MultitudeBonusPerDeckCard * GetModifierLevelFactor(i));
                    placement.ModifierBonus += bonus;
                    var ptsEvent = new ScoreEvent(ScoreEventType.Modifier, placement.PlacedCells[0], bonus);
                    ptsEvent.TriggeringModifier = ModifierId.Multitude;
                    ptsEvent.TriggeringModifierIndex = i;
                    events.Add(ptsEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Experience)
                {
                    // Same TRUE-float treatment (and level scaling) as CartesEnchantees above.
                    float trueMult = (1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep * GetModifierLevelFactor(i);
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

        /// <summary>"Void Tile": clears one random already-filled, unlocked cell elsewhere on the grid — excludes this placement's own cells (only pre-existing board state is eligible). Scores ScoringConstants.VoidBonusPerDestroyedCell for the broken tile (on explicit request: "Le void tile, quand elle est triggered dans la grille, faire +10 pour la tuile brisé" — previously pure risk/utility with no score of its own); a no-op if nothing else on the grid is eligible.</summary>
        private void ApplyVoidEffect(PlacementResult placement)
        {
            var cleared = Grid.ClearRandomFilledCell(_rng, placement.PlacedCells, out var clearedColor);
            if (cleared.HasValue)
            {
                AddDestroyedCell(placement, cleared.Value, clearedColor);
                AddTraitBonus(placement, cleared.Value, ScoringConstants.VoidBonusPerDestroyedCell);
            }
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
        /// "Kamikaze Tile": destroys the enchanted cell ITSELF plus every
        /// already-filled, unlocked cell in its 8 surrounding tiles (Moore
        /// neighborhood) — including this SAME placement's own other
        /// cells, if the piece it's part of has more than one (on explicit
        /// request: "The kamikaze tile shouldn't exclude it's own tiles"
        /// — no longer the "protect what was just placed" convention Void
        /// Tile still uses). The trait cell's own destruction was added on
        /// a further explicit request ("L'upgrade kamikaze devrait
        /// détruire sa propre tuile aussi, pas juste les 8 autour") — it
        /// was previously spared, being the center of the 8-neighbor
        /// search rather than one of the 8 neighbors. Scores
        /// ScoringConstants.KamikazeBonusPerDestroyedCell per tile actually
        /// destroyed, the trait cell included.
        /// </summary>
        private void ApplyKamikazeEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            int destroyed = 0;

            var centerCell = Grid.GetCell(traitCellPos.x, traitCellPos.y);
            if (centerCell.IsFilled && !centerCell.IsLocked)
            {
                var centerColor = centerCell.FilledColor;
                centerCell.ClearFill();
                destroyed++;
                AddDestroyedCell(placement, traitCellPos, centerColor);
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
                    cell.ClearFill();
                    destroyed++;
                    AddDestroyedCell(placement, pos, color);
                }
            }

            if (destroyed > 0)
            {
                AddTraitBonus(placement, traitCellPos, destroyed * ScoringConstants.KamikazeBonusPerDestroyedCell);
            }
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
            AddActiveModifier(modifierId);
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

        /// <summary>Debug-only helper: pins ShufflesRemaining to an exact value, bypassing gameplay — mirrors DebugSetLueur, for EditMode tests that need to exercise the stuck-hand defeat check (see EvaluateRoundEnd) without spending real shuffles first, which would re-deal the hand via ShuffleHand and ruin a deliberately set-up deterministic scenario (a specific seed's hand, or one found by searching for a specific shape).</summary>
        public void DebugSetShufflesRemaining(int amount)
        {
            ShufflesRemaining = amount;
        }

        /// <summary>
        /// Spends one shuffle charge to re-roll all 3 hand slots at once
        /// (spec extension, explicit request — see RunConfig.
        /// StartingShuffleCount/ShufflesRemaining). No-op (returns false,
        /// nothing spent) when the run isn't InProgress or no charges are
        /// left. Re-runs EvaluateRoundEnd right after re-rolling since a
        /// shuffle can turn a stuck hand into a playable one — or, if the
        /// fresh hand is ALSO unplayable and this was the last charge,
        /// immediately confirm the loss instead of waiting for the
        /// player's next placement attempt (there won't be a legal one).
        /// </summary>
        public bool ShuffleHand()
        {
            if (State != RunState.InProgress || ShufflesRemaining <= 0)
            {
                return false;
            }
            ShufflesRemaining--;
            Deck.DrawNewHand();
            EvaluateRoundEnd();
            return true;
        }

        private void EvaluateRoundEnd()
        {
            if (RoundScore >= CurrentQuota)
            {
                // Balance fix (spec extension, explicit request): a strong
                // early modifier can reach quota almost instantly, leaving
                // most of the round's piece budget unused and starving the
                // player of Lueur income for the shop. Converting every
                // unused piece into +1 Lueur means finishing a round FAST
                // still pays out close to what grinding it out fully would
                // have. LastRoundEndLueurBonus is read once by the
                // presentation layer (GameBootstrap.PlayRoundEndLueurBonusSequence)
                // to replay this as a "+1 Lueur" popup per unused piece,
                // flying from the pieces bar to the Lueur counter, before
                // the shop actually opens — Lueur itself is already final
                // here, same "core computes the end state instantly,
                // presentation fakes the gradual reveal" convention as
                // every other scoring event in PlacePiece.
                LastRoundEndLueurBonus = PiecesRemainingThisRound;
                Lueur += LastRoundEndLueurBonus;
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
            //
            // A stuck hand (no legal placement for any of its 3 pieces) is
            // only a genuine loss once ShufflesRemaining is ALSO exhausted
            // (spec extension, explicit request: "il va falloir tweak la
            // condition de défaite pour valider si le joueur ne peut plus
            // jouer de pièce ET qu'il n'a plus de shuffle en banque") — with
            // shuffles still in the bank, the player can re-roll the hand
            // instead of losing outright (see ShuffleHand, which re-runs
            // this exact check right after re-rolling).
            if (!Deck.IsHandFullyEmpty() && !HasAnyHandPlacement() && ShufflesRemaining <= 0)
            {
                State = RunState.RunDefeat;
            }
        }

        /// <summary>
        /// Dwindling (Epuisement): once its decaying bonus (see
        /// GridManager.EpuisementCurrentBonus) has fully bottomed out at 0,
        /// it can never earn another point for the rest of the run (its
        /// value is permanent and never resets) — removed the instant that
        /// happens, on explicit request ("Dwilding modifier devrait être
        /// détruit lorsqu'il est rendu a 0"), freeing its modifier slot
        /// instead of leaving a permanently-dead entry sitting in it.
        /// _activeModifiers can never hold more than one copy of Epuisement
        /// (RollBlisterSlot excludes modifiers already owned), so IndexOf is
        /// unambiguous. A no-op — including on every placement before
        /// Epuisement is ever held, since GridManager.EpuisementCurrentBonus
        /// only starts decaying once it's actually dispatched — whenever the
        /// bonus is still above 0 or Epuisement isn't currently held.
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
        /// One "Blister" slot's roll (see EconomyConstants.ShopBlisterSlotCount)
        /// — a single FLAT weighted bag holding every not-yet-held/not-
        /// already-offered-this-visit modifier (uniform weight, matching
        /// the Common upgrade-rarity weight) alongside every eligible
        /// upgrade (its own rarity weight, see UpgradeRarityUtility,
        /// multiplied by EconomyConstants.BlisterUpgradeWeightMultiplier) —
        /// explicit request: "mélanger modifiers et upgrades dans un sac et
        /// en tirer 3 au hasard", resolved (after clarifying the options) as
        /// one real shared bag rather than an artificial 50/50 split. With
        /// ~70 modifiers to ~21 upgrades, a perfectly flat weight-8-per-item
        /// bag put an upgrade's odds around 15-16% per slot — on a follow-up
        /// report that this read as far too rare ("ils n'apparaissent
        /// vraiment pas assez souvent"), upgrade weight alone gained the
        /// multiplier above, bringing it to roughly 35-40% without touching
        /// modifiers' own (still perfectly uniform) odds among themselves.
        /// An upgrade needing a prerequisite the player doesn't meet yet
        /// (Modifier Upgrade with no modifiers owned, Random Modifier
        /// already at the cap) is excluded from the bag entirely — unlike a
        /// Casino slot, a Blister slot's exact identity is always visible,
        /// so an obviously-dead card would just read as broken.
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

        /// <summary>Whether <paramref name="candidate"/> could actually be applied right now — see RollBlisterSlot's own doc comment for why this only matters for Blister, not Casino.</summary>
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
            var pool = _rng.Next(100) < EconomyConstants.BankUpgradePoolChancePercent
                ? UpgradePool.Bank
                : UpgradePool.Grid;
            var upgrade = Upgrades.RollFromPool(pool);
            return ShopSlot.ForUpgrade(upgrade);
        }

        /// <summary>Current Lueur price of Blister slot <paramref name="index"/> — a Modifier-kind slot varies per modifier (see ModifierPricing, on explicit request); an Upgrade-kind slot uses the same Bank/Grid base price as a Casino slot (see GetUpgradeSlotPrice). Either way includes this visit's escalation (see EconomyConstants.ShopPriceEscalationPerPurchase).</summary>
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
            return Mathf.RoundToInt(EconomyConstants.ShopRerollBasePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _rerollsThisVisit));
        }

        private int ComputePrice(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * (1f + EconomyConstants.ShopPriceEscalationPerPurchase * _purchasesThisVisit));
        }

        /// <summary>
        /// Buys Blister slot <paramref name="index"/> outright — a
        /// Modifier-kind slot applies immediately (modifiers are never a
        /// mystery, so this is the whole purchase, no follow-up needed); an
        /// Upgrade-kind slot goes through the exact same resolution as a
        /// Casino purchase (see ApplyPurchasedUpgrade/BuyUpgradeSlot),
        /// since a Blister slot's upgrade is just as fully rolled — only
        /// its PRE-purchase visibility differs. Fails (no charge, no state
        /// change) if the shop isn't open, the slot is invalid/already
        /// bought, the player can't afford it, or (Modifier-kind only)
        /// they're already at EconomyConstants.MaxActiveModifiers — an
        /// Upgrade-kind slot's own prerequisite checks (Random Modifier at
        /// the cap, Modifier Upgrade with nothing owned) never actually
        /// fire here in practice since RollBlisterSlot excludes those
        /// upgrades from the bag entirely, but are kept for defense in
        /// depth (the player's modifier count can change between the roll
        /// and the purchase, e.g. buying two Modifier-kind Blister slots
        /// first).
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

                // "Mimic" (Copieur) isn't a real modifier of its own —
                // buying it adds another copy of whichever modifier was
                // purchased immediately before it instead (on explicit
                // request: "un modifier qui copy le modifier précédemment
                // acheté"). A no-op (still costs Lueur, still marks the
                // slot sold) if nothing has been purchased yet this run.
                // _lastPurchasedModifierId only ever tracks a REAL
                // purchase, never Copieur itself, so buying several Mimics
                // in a row all copy the same underlying modifier rather
                // than chaining off each other.
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
        /// Buys Casino upgrade slot <paramref name="index"/> — the specific
        /// upgrade underneath (only its <see cref="UpgradePool"/> was ever
        /// shown) gets revealed as <see cref="PendingUpgrade"/> (see
        /// ApplyPurchasedUpgrade for the actual resolution, shared with a
        /// Blister-kind upgrade purchase). Fails (no charge) under the same
        /// conditions as <see cref="BuyBlisterSlot"/> (minus the modifier
        /// cap, which doesn't apply to upgrades in general) — EXCEPT for
        /// the "Random Modifier" upgrade specifically (explicit report: "si
        /// le joueur a un random modifier comme upgrade et qu'il est full
        /// il ne devrait pas pouvoir l'acheter"), which DOES respect the
        /// cap: buying it while already at EconomyConstants.MaxActiveModifiers
        /// used to still charge Lueur and simply grant nothing (see
        /// GrantRandomModifier), silently wasting the purchase.
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
        /// section it came from (Blister or Casino — see BuyBlisterSlot/
        /// BuyUpgradeSlot, which each handle their OWN slot bookkeeping and
        /// eligibility/price checks before calling this). A Bank-pool
        /// upgrade with no sub-choice (Joker) applies immediately and
        /// leaves PendingUpgrade null; one that needs a sub-choice
        /// (Retirer/Dupliquer/Recolorer) or a Grid-pool upgrade (needs a
        /// tile choice, see PendingUpgradeTileCandidates) leaves
        /// PendingUpgrade set until <see cref="ResolveUpgradeSubChoice"/>/
        /// <see cref="ResolveUpgradeTileChoice"/> finishes it — nothing
        /// else in the shop can be done meanwhile.
        /// </summary>
        private bool ApplyPurchasedUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade.Pool == UpgradePool.Grid)
            {
                PendingUpgrade = upgrade;
                PendingUpgradeTileCandidates = Upgrades.GetCandidateTilesFor(upgrade, Deck);
                return true;
            }

            // Random Piece is Bank pool WITH a sub-choice, like Retirer/
            // Dupliquer/Recolorer below, but its candidates are freshly
            // rolled pieces rather than existing deck types — handled by
            // its own Pending*/Resolve* pair (see PendingUpgradePieceCandidates/
            // ResolveUpgradePieceChoice) instead of falling into the
            // generic RequiresSubChoice branch just below, which only
            // knows how to populate PendingUpgradeTypeCandidates.
            if (upgrade.Id == UpgradeId.RandomPiece)
            {
                PendingUpgrade = upgrade;
                PendingUpgradePieceCandidates = Upgrades.GetCandidatePiecesFor(upgrade);
                return true;
            }

            // Modifier Upgrade is a THIRD distinct Bank sub-choice shape —
            // unlike Retirer/Dupliquer/Recolorer (pick a deck TYPE) and
            // Random Piece (pick from freshly-rolled candidates), its
            // candidates are simply every slot the player already owns
            // (ActiveModifiers itself), so there's no Pending*Candidates
            // list to populate here at all — see ResolveModifierUpgradeChoice.
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

            // The four Bank-pool upgrades with RequiresSubChoice false —
            // see UpgradeCatalog.BankPool — are Joker, Random Modifier,
            // Piece Mastery and Color Mastery. Each applies through its own
            // dedicated path (ApplyJoker / GrantRandomModifier /
            // GrantShapeMastery / GrantColorMastery, not a generic Apply) so
            // the actual thing rolled can be surfaced (LastJokerShapeAdded /
            // LastRandomModifierGranted / LastShapeMasteryGranted /
            // LastColorMasteryGranted) for the reveal to show (see
            // UpgradeRevealView / ShapeCarouselView / ColorCarouselView)
            // instead of just naming the upgrade.
            if (upgrade.Id == UpgradeId.JokerPiece)
            {
                LastJokerShapeAdded = Upgrades.ApplyJoker(Deck);
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
        /// Modifier" upgrade for real would (see BuyUpgradeSlot), without
        /// needing to find/afford/click it in the shop first — on explicit
        /// report ("Encore une fois je ne les ai pas vu en plus d'une
        /// vingtaine [rerolls]") lets the purchase+grant+reveal pipeline be
        /// tested directly, decoupled from the shop's own roll odds and its
        /// "mystery box" card (which never shows an upgrade's real name
        /// before purchase in the first place — see ShopView.BuildUpgradeCard).
        /// </summary>
        public ModifierId? DebugTriggerRandomModifierGrant()
        {
            LastRandomModifierGranted = GrantRandomModifier();
            return LastRandomModifierGranted;
        }

        /// <summary>Debug-only helper: exercises "Piece Mastery"'s exact grant (GrantShapeMastery), bypassing the shop entirely — same rationale as DebugTriggerRandomModifierGrant, and useful here too since which shape gets offered is otherwise left to the shop's own roll.</summary>
        public ShapeId DebugTriggerShapeMasteryGrant()
        {
            LastShapeMasteryGranted = GrantShapeMastery();
            return LastShapeMasteryGranted.Value;
        }

        /// <summary>Debug-only helper: exercises "Color Mastery"'s exact grant (GrantColorMastery), bypassing the shop entirely — same rationale as DebugTriggerShapeMasteryGrant.</summary>
        public PieceColor DebugTriggerColorMasteryGrant()
        {
            LastColorMasteryGranted = GrantColorMastery();
            return LastColorMasteryGranted.Value;
        }

        /// <summary>
        /// Debug-only helper: overwrites upgrade slot <paramref name="index"/>
        /// (default 0) to be "Random Modifier" outright, bypassing the
        /// shop's own roll — unlike DebugTriggerRandomModifierGrant above,
        /// this goes through the REAL purchase button (BuyUpgradeSlot, via
        /// ShopView's own "Buy" click), exercising its exact branching
        /// instead of skipping straight to GrantRandomModifier — on explicit
        /// report that it still never showed up after nearly 20 real
        /// purchases even though the debug-triggered grant worked fine, to
        /// rule in/out a bug specific to BuyUpgradeSlot's own code path.
        /// No-op (false) if the shop isn't currently open.
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
        /// Debug-only helper: overwrites Blister slot <paramref name="index"/>
        /// (default 0) to hold <paramref name="modifierId"/> outright,
        /// bypassing the shop's own roll — lets a test buy a SPECIFIC
        /// modifier through the real purchase path (BuyBlisterSlot,
        /// exercising e.g. Copieur's purchase-time copy logic) without
        /// rerolling into it, which the Blister section no longer supports
        /// once rolled (RerollShop only ever touches Casino — explicit
        /// request: "le bouton reroll ne reroll pas la section 'blister'").
        /// No-op (false) if the shop isn't currently open.
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
        /// Grants one uniformly random modifier for free, right at the start
        /// of a run — a slot-machine-style "carousel" (see Presentation.
        /// ModifierCarouselView) spins through several modifier badges
        /// before landing on this one, meant to dictate an initial strategy
        /// the player has to build the run around (explicit request:
        /// "un carousel qui choisissent un modifier au hasard, comme pour
        /// dicter une stratégie initiale que le joueur devra utiliser").
        /// Called explicitly by GameBootstrap right after constructing the
        /// RunManager, never from the constructor itself, so every existing
        /// test asserting a freshly-built run holds zero modifiers keeps
        /// passing unless it opts into this call. Picks from the whole
        /// catalog with no "already held" exclusion needed — a brand new run
        /// holds nothing yet — unlike GrantRandomModifier below. Doesn't
        /// touch _lastPurchasedModifierId: this is a freebie, not a
        /// purchase, so Copieur's very first shop pick still has nothing to
        /// copy, same as before this feature existed.
        /// </summary>
        public ModifierId GrantStartingModifier()
        {
            var picked = ModifierCatalog.All[_rng.Next(ModifierCatalog.All.Length)].Id;
            AddActiveModifier(picked);
            StartingModifier = picked;
            return picked;
        }

        /// <summary>
        /// "Random Modifier" upgrade's actual effect (see UpgradeCatalog.
        /// RandomModifier) — grants one uniformly random modifier the player
        /// doesn't already hold, from the same catalog RollBlisterSlot
        /// draws from, minus its "already offered this shop visit" exclusion
        /// (irrelevant here — nothing is being offered for sale, it's
        /// granted outright). Updates _lastPurchasedModifierId the same way
        /// a direct modifier-slot purchase does, so a Copieur bought right
        /// after can copy whatever this gamble happened to grant. Returns
        /// null — grants nothing, though the Lueur already spent on the
        /// upgrade itself is not refunded — if the player is already at
        /// EconomyConstants.MaxActiveModifiers or (practically impossible)
        /// already holds every modifier in the catalog.
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
        /// "Piece Mastery": picks a random exact shape (uniform over all 8 —
        /// no exclusion, unlike GrantRandomModifier, since a shape can
        /// always be leveled up further) and levels it up by 1. Always
        /// succeeds — there's no cap to hit, so unlike GrantRandomModifier
        /// this never returns null.
        /// </summary>
        private ShapeId GrantShapeMastery()
        {
            var shapes = (ShapeId[])System.Enum.GetValues(typeof(ShapeId));
            var picked = shapes[_rng.Next(shapes.Length)];
            _shapeMasteryLevels[picked] = GetShapeMasteryLevel(picked) + 1;
            return picked;
        }

        /// <summary>"Color Mastery": Piece Mastery's exact sibling — picks a random BASE color (PieceColorUtility.BaseColors, so never Joker) and levels it up by 1. Always succeeds, same as GrantShapeMastery.</summary>
        private PieceColor GrantColorMastery()
        {
            var colors = PieceColorUtility.BaseColors;
            var picked = colors[_rng.Next(colors.Count)];
            _colorMasteryLevels[picked] = GetColorMasteryLevel(picked) + 1;
            return picked;
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
        /// Refreshes every CASINO slot — purchased or not — with a new
        /// random offer. Never touches the Blister section (explicit
        /// request: "le bouton reroll ne reroll pas la section 'blister'")
        /// — Blister already shows its exact contents up front, so a
        /// reroll there would just be "pay Lueur to see 3 different exact
        /// items," a very different (and not requested) kind of purchase
        /// from Casino's "pay Lueur to try your luck at the mystery box
        /// again." Used to only touch still-unsold CASINO slots (leaving a
        /// "SOLD" one exactly as it was), but on explicit feedback that
        /// read as reroll silently doing nothing whenever most of the shop
        /// had already been bought: "mes upgrades et modifiers que j'ai
        /// acheté sont encore marqué sold, il faut que j'aie tout de
        /// disponible". Buying a slot still permanently grants whatever it
        /// held (the upgrade is already applied by then) — this only
        /// replaces the SLOT OFFER itself, giving the player a fresh
        /// purchasable pick where a spent one used to sit. Costs Lueur (see
        /// GetRerollPrice), escalating only from further rerolls this same
        /// visit (see _rerollsThisVisit) — no longer from slot purchases,
        /// on explicit report: "Les reroll devraient augmenter de prix
        /// seulement lorsqu'on reroll".
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
