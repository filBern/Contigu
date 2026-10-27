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
    public sealed partial class RunManager
    {
        public GridManager Grid { get; }
        public DeckManager Deck { get; }
        public UpgradeSystem Upgrades { get; }

        private readonly List<ModifierId> _activeModifiers = new List<ModifierId>();

        // Slots currently blighted by an alive Blightmoth (see ResolveBlightmothShuffleEffect): they stay in _activeModifiers but score nothing.
        private readonly HashSet<int> _blightedModifiers = new HashSet<int>();

        /// <summary>True when the modifier at <paramref name="index"/> is blighted and therefore has no effect.</summary>
        public bool IsModifierBlighted(int index)
        {
            return _blightedModifiers.Contains(index);
        }

        /// <summary>_activeModifiers with every blighted slot replaced by the effect-less ModifierId.Blighted placeholder, so slot order and indices stay intact for scoring.</summary>
        private IReadOnlyList<ModifierId> EffectiveModifiers()
        {
            if (_blightedModifiers.Count == 0)
            {
                return _activeModifiers;
            }
            var copy = new List<ModifierId>(_activeModifiers);
            foreach (int index in _blightedModifiers)
            {
                if (index >= 0 && index < copy.Count)
                {
                    copy[index] = ModifierId.Blighted;
                }
            }
            return copy;
        }

        /// <summary>True when <paramref name="id"/> is held in at least one slot that is not blighted.</summary>
        private bool HasEffectiveModifier(ModifierId id)
        {
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i] == id && !_blightedModifiers.Contains(i))
                {
                    return true;
                }
            }
            return false;
        }

        private void RemapBlightAfterRemove(int removedIndex)
        {
            if (_blightedModifiers.Count == 0)
            {
                return;
            }
            var remapped = new List<int>();
            foreach (int index in _blightedModifiers)
            {
                if (index == removedIndex)
                {
                    continue;
                }
                remapped.Add(index > removedIndex ? index - 1 : index);
            }
            _blightedModifiers.Clear();
            for (int i = 0; i < remapped.Count; i++)
            {
                _blightedModifiers.Add(remapped[i]);
            }
        }

        // Parallel to _activeModifiers (same index == same slot). Every
        // mutation of _activeModifiers (add/remove/swap/move) must mirror the
        // same operation here - see AddActiveModifier/RemoveActiveModifierAt,
        // and the inline swaps in SwapModifiers/MoveModifier.
        private readonly List<int> _modifierLevels = new List<int>();

        // Also parallel to _activeModifiers: how many full rounds each fertilizer has been held (Autumn withers them, see AgeAndWitherModifiers).
        private readonly List<int> _modifierAges = new List<int>();

        /// <summary>How many cleared rounds the fertilizer at <paramref name="index"/> has been held (0 for a fresh one).</summary>
        public int GetModifierAge(int index)
        {
            return index >= 0 && index < _modifierAges.Count ? _modifierAges[index] : 0;
        }

        /// <summary>Rounds the fertilizer at <paramref name="index"/> has left before it withers, or 0 when this challenge's fertilizers never wither.</summary>
        public int GetModifierRoundsLeft(int index)
        {
            if (_challenge.FertilizerLifespanRounds <= 0)
            {
                return 0;
            }
            return System.Math.Max(0, _challenge.FertilizerLifespanRounds - GetModifierAge(index));
        }

        /// <summary>The fertilizers that withered away at the end of the round just cleared (Autumn), in the order they were held — Presentation plays their farewell once the shop opens. Empty otherwise.</summary>
        public IReadOnlyList<ModifierId> LastWitheredModifiers { get; private set; } = new List<ModifierId>();

        /// <summary>What sprouted in place of each fertilizer in <see cref="LastWitheredModifiers"/> (same order, same slots).</summary>
        public IReadOnlyList<ModifierId> LastSproutedModifiers { get; private set; } = new List<ModifierId>();

        /// <summary>
        /// Autumn: every held fertilizer grows a round older when a round is cleared; any that reached the lifespan withers away now, and a
        /// random fertilizer you don't hold yet sprouts in its slot (the garden renews rather than emptying).
        /// </summary>
        private void AgeAndWitherModifiers()
        {
            var withered = new List<ModifierId>();
            var sprouted = new List<ModifierId>();
            if (_challenge.FertilizerLifespanRounds > 0)
            {
                for (int i = 0; i < _modifierAges.Count; i++)
                {
                    _modifierAges[i]++;
                }
                for (int i = 0; i < _activeModifiers.Count; i++)
                {
                    if (_modifierAges[i] < _challenge.FertilizerLifespanRounds)
                    {
                        continue;
                    }
                    withered.Add(_activeModifiers[i]);
                    RemoveActiveModifierAt(i);
                    var replacement = RollSproutedModifier();
                    if (replacement.HasValue)
                    {
                        AddActiveModifier(replacement.Value);
                        MoveModifier(_activeModifiers.Count - 1, i);
                        sprouted.Add(replacement.Value);
                    }
                    else
                    {
                        // Nothing left to sprout: the slot simply closes up, so the next one slides into index i.
                        i--;
                    }
                }
            }
            LastWitheredModifiers = withered;
            LastSproutedModifiers = sprouted;
        }

        /// <summary>A random usable fertilizer the player doesn't hold (null if there is none).</summary>
        private ModifierId? RollSproutedModifier()
        {
            var candidates = new List<ModifierId>();
            for (int i = 0; i < ModifierCatalog.All.Length; i++)
            {
                var id = ModifierCatalog.All[i].Id;
                if (!_activeModifiers.Contains(id) && id != ModifierId.Copieur && IsModifierUsableThisRun(id))
                {
                    candidates.Add(id);
                }
            }
            return candidates.Count == 0 ? (ModifierId?)null : candidates[_rng.Next(candidates.Count)];
        }

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

        /// <summary>The only place that adds a slot to <see cref="_activeModifiers"/> - keeps a fresh slot's level in sync. Also grows the hand by one slot the moment QuatriemeEmplacement is gained (see DeckManager.SetEffectiveHandSize), rather than waiting for the next DrawNewHand.</summary>
        private void AddActiveModifier(ModifierId id)
        {
            _activeModifiers.Add(id);
            _modifierLevels.Add(1);
            _modifierAges.Add(0);
            if (id == ModifierId.QuatriemeEmplacement)
            {
                Deck.SetEffectiveHandSize(DeckManager.MaxHandSize);
            }
        }

        /// <summary>The only place that removes a slot from <see cref="_activeModifiers"/> - keeps its level entry in sync. Shrinks the hand back by one slot once the LAST copy of QuatriemeEmplacement is gone (Copieur can duplicate a modifier id, so a second copy could still be held).</summary>
        private void RemoveActiveModifierAt(int index)
        {
            var id = _activeModifiers[index];
            _activeModifiers.RemoveAt(index);
            _modifierLevels.RemoveAt(index);
            _modifierAges.RemoveAt(index);
            RemapBlightAfterRemove(index);
            if (id == ModifierId.QuatriemeEmplacement && !_activeModifiers.Contains(ModifierId.QuatriemeEmplacement))
            {
                Deck.SetEffectiveHandSize(DeckManager.HandSize);
            }
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
            var tempAge = _modifierAges[indexA];
            _modifierAges[indexA] = _modifierAges[indexB];
            _modifierAges[indexB] = tempAge;

            // The blight travels with the modifier too.
            bool blightA = _blightedModifiers.Remove(indexA);
            bool blightB = _blightedModifiers.Remove(indexB);
            if (blightA) { _blightedModifiers.Add(indexB); }
            if (blightB) { _blightedModifiers.Add(indexA); }
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
            var movedAge = _modifierAges[fromIndex];
            _modifierAges.RemoveAt(fromIndex);
            _modifierAges.Insert(toIndex, movedAge);

            if (_blightedModifiers.Count > 0)
            {
                bool movedWasBlighted = _blightedModifiers.Contains(fromIndex);
                RemapBlightAfterRemove(fromIndex);
                var shifted = new List<int>();
                foreach (int index in _blightedModifiers)
                {
                    shifted.Add(index >= toIndex ? index + 1 : index);
                }
                _blightedModifiers.Clear();
                for (int i = 0; i < shifted.Count; i++)
                {
                    _blightedModifiers.Add(shifted[i]);
                }
                if (movedWasBlighted)
                {
                    _blightedModifiers.Add(toIndex);
                }
            }
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

        /// <summary>The "Workshop" section (it replaced the Casino): 2 packs, a Compost service and a Forge, all shown plainly. The only section RerollShop touches.</summary>
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

        /// <summary>The 3 tile upgrades a Sprout Pack revealed (empty otherwise) — pick one with <see cref="ResolvePackUpgradeChoice"/> (or skip the pack).</summary>
        public IReadOnlyList<UpgradeDefinition> PendingPackUpgrades { get; private set; } = System.Array.Empty<UpgradeDefinition>();

        /// <summary>The 3 fertilizers a Fertilizer Pack revealed (empty otherwise) — keep one with <see cref="ResolvePackModifierChoice"/> (or skip the pack).</summary>
        public IReadOnlyList<ModifierId> PendingPackModifiers { get; private set; } = System.Array.Empty<ModifierId>();

        /// <summary>How many Compost services were bought this run (each one makes the next dearer).</summary>
        private int _compostsBought;

        /// <summary>Where the most recent Permanent Bonus Square landed — read once by the presentation layer to show the player which square was marked. Null before any such purchase.</summary>
        public Vector2Int? LastPermanentBonusPosition { get; private set; }

        /// <summary>Piece/Color Mastery's two offered levelings while <see cref="PendingUpgrade"/> is one of them (empty otherwise) — the player picks one (see <see cref="ResolveMasteryChoice"/>) instead of getting a random result.</summary>
        public IReadOnlyList<MasteryOption> PendingMasteryOptions { get; private set; } = System.Array.Empty<MasteryOption>();

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
                    return "Currently +" + ScoringConstants.PointsToMult(Mathf.RoundToInt(Grid.EpuisementCurrentBonus * levelFactor)) + " Mult";
                case ModifierId.Solidarite:
                    return "Currently +" + FormatMultDisplay((_activeModifiers.Count / ScoringConstants.SolidariteModifierCountDivisor) * levelFactor) + " Mult";
                case ModifierId.Multitude:
                    return "Currently +" + FormatMultDisplay(Deck.DeckCount / (float)ScoringConstants.MultitudeCardsPerMult * levelFactor) + " Mult";
                case ModifierId.CartesEnchantees:
                    return "Currently +" + FormatMultDisplay((1 + CountUpgradedDeckCards()) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep * levelFactor) + " Mult";
                case ModifierId.Experience:
                    return "Currently +" + FormatMultDisplay((1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep * levelFactor) + " Mult";
                case ModifierId.Arsenal:
                    return "Currently +" + Mathf.RoundToInt(CountDistinctCombatKindsInDeck() * ScoringConstants.ArsenalMultPerDistinctCombatKind * levelFactor) + " Mult";
                case ModifierId.Hate:
                    return "Currently +" + FormatMultDisplay(PiecesRemainingThisRound * ScoringConstants.HasteMultPerSeedLeft * levelFactor) + " Mult";
                case ModifierId.Tourbillon:
                    return "Currently +" + FormatMultDisplay(ShufflesRemaining * ScoringConstants.WhirlwindMultPerShuffle * levelFactor) + " Mult";
                case ModifierId.Exterminateur:
                    return "Currently +" + Mathf.RoundToInt(CountDefeatedEnemies() * ScoringConstants.PestControlMultPerKill * levelFactor) + " Mult";
                case ModifierId.Epines:
                    return AnyEnemyHasActiveMalus() ? "Active: +" + Mathf.RoundToInt(ScoringConstants.ThornsMultBonus * levelFactor) + " Mult" : "Inactive: no enemy lock or blight right now";
                case ModifierId.Barometre:
                    return CurrentWeather != WeatherId.Clear ? "Active: +" + Mathf.RoundToInt(ScoringConstants.BarometerMultBonus * levelFactor) + " Mult" : "Inactive: calm skies";
                case ModifierId.ChasseurTempete:
                    return WeatherCatalog.IsHarsh(CurrentWeather) ? "Active: x" + ScoringConstants.StormChaserMultiplier : "Inactive: the weather isn't harsh";
                case ModifierId.Polyvalence:
                    return "Currently +" + Mathf.RoundToInt(CountDistinctModifierCategoriesHeld() * ScoringConstants.PolyvalenceMultPerCategory * levelFactor) + " Mult";
                default:
                    return GetNewModifierStateText(id, levelFactor);
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

        /// <summary>True once the player kept going past the challenge's final round (see <see cref="ContinueEndless"/>): rounds keep coming, ever harder, until the run is lost.</summary>
        public bool IsEndless { get; private set; }

        /// <summary>Whether a round follows the current one — always in Endless, otherwise until the challenge's final round.</summary>
        public bool HasNextRound
        {
            get { return IsEndless || CurrentRoundIndex + 1 < _challenge.RoundCount; }
        }

        /// <summary>
        /// Sets the process-wide rules this challenge changes before anything is built from them: the plot's side (Greenhouse) and the seed
        /// colors that exist (Orchard keeps a random 2 of the 4). Colors are drawn only when the challenge trims them, so every other
        /// challenge consumes exactly the randomness it always did.
        /// </summary>
        private void ConfigureChallengeWorld()
        {
            GridManager.SetSize(_challenge.GridSize);
            if (_challenge.ActiveColorCount >= PieceColorUtility.AllBaseColors.Count)
            {
                PieceColorUtility.ResetBaseColors();
                return;
            }
            var pool = new List<PieceColor>(PieceColorUtility.AllBaseColors);
            var chosen = new List<PieceColor>();
            while (chosen.Count < _challenge.ActiveColorCount)
            {
                int pick = _rng.Next(pool.Count);
                chosen.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            chosen.Sort();
            PieceColorUtility.SetActiveBaseColors(chosen);
        }

        public int CurrentRoundNumber
        {
            get { return CurrentRoundIndex + 1; }
        }

        /// <summary>
        /// Which enemies the round after this one will contain, read straight from EncounterCatalog (no RNG
        /// involved — Color/Shape Hater's own random pick happens at BuildEncounter time, not here) so the
        /// shop can preview them before the player leaves. CurrentRoundIndex only advances in AdvanceRound
        /// (see LeaveShop), so while the shop is open it still names the round that just ended, one behind
        /// the round being previewed. Empty if that round has no authored encounter (a plain quota round) or
        /// doesn't exist (last round of the run).
        /// </summary>
        public IReadOnlyList<EnemyId> UpcomingEncounterIds
        {
            get
            {
                int nextRoundIndex = CurrentRoundIndex + 1;
                if (!HasNextRound)
                {
                    return System.Array.Empty<EnemyId>();
                }
                return EncounterCatalog.GetEncounter(_challenge, nextRoundIndex);
            }
        }

        /// <summary>This round's score target. Still read for display even once <see cref="HasActiveEncounter"/> makes "every enemy dead" the real win condition.</summary>
        public int CurrentQuota
        {
            get { return _challenge.QuotaFor(CurrentRoundIndex); }
        }

        public int CurrentBudget
        {
            get { return _challenge.BudgetFor(CurrentRoundIndex); }
        }

        /// <summary>Rounds 4 and 8 (every RunConfig.BossRoundInterval).</summary>
        public bool IsBossRound
        {
            get { return (CurrentRoundIndex + 1) % RunConfig.BossRoundInterval == 0; }
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
            ConfigureChallengeWorld();
            Grid = new GridManager();
            var startingDeck = _challenge.Id == ChallengeId.Marathon
                ? InitialDeckFactory.BuildMarathon()
                : InitialDeckFactory.Build(4 / PieceColorUtility.BaseColors.Count);
            Deck = new DeckManager(startingDeck, rng);
            Upgrades = new UpgradeSystem(rng);
            PendingUpgradeTileCandidates = System.Array.Empty<int>();
            PendingUpgradeTypeCandidates = System.Array.Empty<(ShapeId, PieceColor)>();
            PendingUpgradePieceCandidates = System.Array.Empty<PieceToken>();
            ShufflesRemaining = RunConfig.StartingShuffleCount;
            CurrentRoundIndex = 0;
            StartRound();
            RollWeatherForRun();
            // StartRound ran before the weather existed: a challenge whose first round isn't clear applies it now (a no-op under Clear).
            ApplyWeatherRoundStart();
            // The weather forecast only exists now, after the first StartRound took its snapshot.
            RoundStartSnapshot = CaptureSnapshot();
            CheckpointSnapshot = RoundStartSnapshot;
        }

        private void StartRound()
        {
            Grid.ResetForNewRound();
            _blightedModifiers.Clear();
            _currentEncounter = BuildEncounter(CurrentRoundIndex);
            ApplyWeatherRoundStart();
            if (HasActiveEncounter)
            {
                // The two systems never run at once - see HasActiveEncounter's own
                // doc comment.
                CurrentBossEffect = BossEffect.None;
                BossLockedHandSlotIndex = null;
                BossCursedColor = null;
                ApplyPlagueRoundStartFreeze();
            }
            else
            {
                RollBossEffectForRound();
            }
            // The boss round lock ratchets up gradually instead of all at once -
            // see ApplyBossLockTick, called from PlacePiece every
            // _challenge.BossLockPiecesInterval pieces.
            // The round's starting hand is dealt here, always (never carried over). Deliberately a plain
            // deal, not DrawFreshHand: this is not a Shuffle triggered mid-round, so no enemy here should
            // get an on-Shuffle tick before the player has placed a single piece against it.
            // Every round starts with a fresh hand: seeds left over from the previous round go back to the draw pile.
            Deck.RedealHand();
            RoundScore = 0;
            PiecesRemainingThisRound = CurrentBudget;
            LastRoundEndLueurBonus = 0;
            State = RunState.InProgress;
            RoundStartSnapshot = CaptureSnapshot();
            CheckpointSnapshot = RoundStartSnapshot;

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
                BossLockedHandSlotIndex = _rng.Next(Deck.Hand.Count);
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
            _deathReleasedCellsThisPlacement.Clear();
            ResetWorldEnemyReport();
            if (State != RunState.InProgress)
            {
                return new PlacementOutcome(PlacementResult.Failure("Run is not in progress"), State, RoundScore, TotalScore, PiecesRemainingThisRound);
            }

            if (handIndex < 0 || handIndex >= Deck.Hand.Count || !Deck.Hand[handIndex].HasValue)
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

            // A piece can carry several upgrades, each on its own cell: every one fires.
            var tokenTraits = new List<(PieceTrait Trait, Vector2Int Pos)>();
            foreach (var ownTrait in token.Traits)
            {
                tokenTraits.Add((ownTrait, new Vector2Int(x, y) + shape.Cells[ownTrait.LocalCellIndex]));
            }

            // Chameleon and Spark both need grid state read BEFORE Grid.PlacePiece
            // mutates it: Chameleon overrides the color the piece places as, and
            // Spark needs the no-clear streak as it stood before this placement
            // (Grid.PlacePiece updates that streak for the NEXT placement to
            // read).
            PieceColor placementColor = token.Color;
            PieceColor? chameleonOriginalColor = null;
            int sparkStreakBeforePlacement = 0;
            if (tokenTraits.Count > 0)
            {
                // Experience's driver - every trait kind counts as "special" (once per piece, however many upgrades it carries).
                _specialPiecesPlayedCount++;
            }
            for (int t = 0; t < tokenTraits.Count; t++)
            {
                var kind = tokenTraits[t].Trait.Kind;
                if (kind == PieceTraitKind.Chameleon && placementColor == token.Color)
                {
                    placementColor = ResolveChameleonColor(shape, x, y, tokenTraits[t].Pos, token.Color);
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

            // Tile upgrades are permanent: every upgraded tile already on the garden counts again whenever it is scored (see ApplyPersistentTraits).
            var persistentTraits = CollectPersistentTraits();
            int streakBeforePlacement = Grid.PlacementsSinceLastClear;

            StampMasteryBonuses(shape, placementColor, x, y);
            var transientTraitCells = new List<Cell>();
            for (int t = 0; t < tokenTraits.Count; t++)
            {
                transientTraitCells.AddRange(ApplyTokenTrait(tokenTraits[t].Trait, tokenTraits[t].Pos, shape, x, y));
            }
            StampPersistentTraits(persistentTraits, shape, x, y, transientTraitCells);
            // Snapshot BEFORE Grid.PlacePiece, same reasoning as the
            // Chameleon/Spark reads above: a poisoned cell this placement clears
            // would read back "not poisoned" once Cell.ClearFill resets the flag.
            var poisonedPositions = HasActiveEncounter ? GetPoisonedPositionsSnapshot() : null;
            // Sower Sprouts plant their free tiles BEFORE the scoring, so a planted tile counts: it can join the group or complete a line.
            var prePlantedTiles = PlantSowerTiles(tokenTraits, persistentTraits, shape, placementColor, x, y);
            var placement = Grid.PlacePiece(shape, placementColor, x, y, EffectiveModifiers(), _modifierLevels);
            placement.PrePlantedTiles = prePlantedTiles;
            ForgetBrokenIce();
            ClearTokenTraitCells(transientTraitCells);
            for (int t = 0; t < tokenTraits.Count; t++)
            {
                ApplyPostPlacementTraitBonus(tokenTraits[t].Trait, tokenTraits[t].Pos, sparkStreakBeforePlacement, placement);
            }
            ApplyPersistentTraitBonuses(persistentTraits, streakBeforePlacement, placement);
            ApplyNeighborCopyUpgrades(placement);
            ApplyHandSlotModifierBonus(handIndex, placement);
            ApplyDeckStateModifierBonuses(placement);
            ApplyWeatherPlacementBonus(placement, placementColor);
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
            bool cellLockActive = CurrentBossEffect == BossEffect.ProgressiveCellLock;
            if (cellLockActive)
            {
                int piecesPlayedThisRound = CurrentBudget - PiecesRemainingThisRound;
                if (piecesPlayedThisRound > 0 && piecesPlayedThisRound % _challenge.BossLockPiecesInterval == 0)
                {
                    bossLockedCells = ApplyBossLockTick();
                }
            }

            // Tricks that also fire on a timer (Bramble, Blightmoth), counted in seeds placed this round.
            if (HasActiveEncounter && PlacementsUntilEnemyTick == EnemyTickInterval)
            {
                ResolveEnemyTickEffects();
            }

            var weatherMovedTiles = ApplyHurricaneTick();

            EvaluateRoundEnd();

            if (State == RunState.InProgress && Deck.IsHandFullyEmpty())
            {
                DrawFreshHand();
                // EvaluateRoundEnd's stuck-check above deliberately skips an empty
                // hand - but the fresh hand just drawn might itself have no legal
                // placement anywhere on the board, so it needs its own check here.
                EvaluateRoundEnd();
            }

            return new PlacementOutcome(placement, State, RoundScore, TotalScore, PiecesRemainingThisRound, bossLockedCells, chameleonOriginalColor, new List<Vector2Int>(_deathReleasedCellsThisPlacement), weatherMovedTiles);
        }
    }
}
