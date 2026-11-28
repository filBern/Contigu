using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>"Something happens": every <see cref="RunConfig.EventRoundInterval"/>rd round, the shop opens on an event card (see <see cref="EventCatalog"/>).</summary>
    public sealed partial class RunManager
    {
        /// <summary>The event waiting for the player's choice (the shop is locked until it is resolved), or null.</summary>
        public EventDefinition PendingEvent { get; private set; }

        /// <summary>What the last chosen option did, one line per effect (shown after the choice); empty when nothing was resolved yet.</summary>
        public IReadOnlyList<string> LastEventResult { get; private set; } = new List<string>();

        /// <summary>
        /// Whether events happen in this run. On in the game (GameBootstrap); off by default so tests, the simulator and tools that clear rounds in a loop
        /// are not stopped by an event card they never answer.
        /// </summary>
        public bool EventsEnabled { get; set; }

        private readonly List<int> _seenEvents = new List<int>();
        // Effects on the NEXT round, keyed by that round's index so they expire by themselves (and survive Save and quit through the snapshot).
        private int _eventSeedsRound = -1;
        private int _eventSeedsDelta;
        private int _eventHpRound = -1;
        private float _eventHpFactor = 1f;

        internal IReadOnlyList<int> SeenEvents { get { return _seenEvents; } }

        /// <summary>Called when the shop opens after a cleared round: every third round (3, 6, 9...), never after the last one of a non-endless run, an event comes first.</summary>
        private void RollEventForShop()
        {
            PendingEvent = null;
            LastEventResult = new List<string>();
            if (!EventsEnabled)
            {
                return;
            }
            bool lastRound = !IsEndless && CurrentRoundIndex >= _challenge.RoundCount - 1;
            if (lastRound || (CurrentRoundIndex + 1) % RunConfig.EventRoundInterval != 0)
            {
                return;
            }
            PendingEvent = EventCatalog.Pick(_rng, _seenEvents);
            _seenEvents.Add(PendingEvent.Id);
        }

        // Fertilizers lost earlier in the SAME choice: a swap never hands the lost one straight back.
        private readonly List<ModifierId> _eventLostModifiers = new List<ModifierId>();

        /// <summary>Whether this outcome can happen as things stand: whatever it takes away or adds must exist or fit.</summary>
        private bool IsEventOutcomeFeasible(EventOutcome outcome)
        {
            foreach (var effect in outcome.Effects)
            {
                switch (effect.Kind)
                {
                    case EventEffectKind.LoseFertilizer:
                    case EventEffectKind.LevelUpFertilizer:
                        if (_activeModifiers.Count == 0) { return false; }
                        break;
                    case EventEffectKind.GrantFertilizer:
                        if (_activeModifiers.Count >= EconomyConstants.MaxActiveModifiers && !OptionAlsoLosesFertilizer(outcome)) { return false; }
                        break;
                    case EventEffectKind.RemoveSeeds:
                        if (Deck.DeckCount - effect.Amount < DeckManager.MinDeckSize) { return false; }
                        break;
                }
            }
            return true;
        }

        /// <summary>The outcomes of option <paramref name="index"/> that can happen right now (the card shows, and the draw uses, only these).</summary>
        public IList<EventOutcome> EventOptionOutcomes(int index)
        {
            var result = new List<EventOutcome>();
            if (PendingEvent == null || index < 0 || index >= PendingEvent.Options.Length) { return result; }
            foreach (var outcome in PendingEvent.Options[index].Outcomes)
            {
                if (IsEventOutcomeFeasible(outcome)) { result.Add(outcome); }
            }
            return result;
        }

        /// <summary>The option's card text with the real chances among what can happen now.</summary>
        public string DescribeEventOption(int index)
        {
            if (PendingEvent == null || index < 0 || index >= PendingEvent.Options.Length) { return string.Empty; }
            return PendingEvent.Options[index].Describe(EventOptionOutcomes(index));
        }

        /// <summary>Why option <paramref name="index"/> cannot be picked right now, in a sentence for the card; null when it can.</summary>
        public string EventOptionBlockedReason(int index)
        {
            if (PendingEvent == null || index < 0 || index >= PendingEvent.Options.Length) { return null; }
            var option = PendingEvent.Options[index];
            if (Lueur < option.RequiredPollen) { return "Needs " + option.RequiredPollen + " Pollen."; }
            if (IsEventOptionAvailable(index)) { return null; }
            foreach (var effect in option.Outcomes[0].Effects)
            {
                switch (effect.Kind)
                {
                    case EventEffectKind.Pollen: return "You do not have enough Pollen.";
                    case EventEffectKind.LoseFertilizer:
                    case EventEffectKind.LevelUpFertilizer: return "You hold no fertilizer.";
                    case EventEffectKind.GrantFertilizer: return "Your fertilizer slots are full.";
                    case EventEffectKind.RemoveSeeds: return "Your bag is already as small as it can be.";
                }
            }
            return "Not possible right now.";
        }

        /// <summary>Whether the player can pick option <paramref name="index"/> of the pending event (enough Pollen, and anything it surely takes away must exist).</summary>
        public bool IsEventOptionAvailable(int index)
        {
            if (PendingEvent == null || index < 0 || index >= PendingEvent.Options.Length)
            {
                return false;
            }
            var option = PendingEvent.Options[index];
            if (Lueur < option.RequiredPollen)
            {
                return false;
            }
            if (EventOptionOutcomes(index).Count == 0)
            {
                return false;
            }
            if (option.Outcomes.Length == 1)
            {
                // A sure Pollen loss the player cannot pay makes the option unavailable (a risky one just takes what is left).
                foreach (var effect in option.Outcomes[0].Effects)
                {
                    if (effect.Kind == EventEffectKind.Pollen && Lueur + effect.Amount < 0) { return false; }
                }
            }
            return true;
        }

        private static bool OptionAlsoLosesFertilizer(EventOutcome outcome)
        {
            foreach (var e in outcome.Effects)
            {
                if (e.Kind == EventEffectKind.LoseFertilizer) { return true; }
            }
            return false;
        }

        /// <summary>Resolves the pending event with the chosen option: draws one outcome by weight among those that can happen, applies it and keeps what happened in <see cref="LastEventResult"/>. False when nothing was pending or the option is unavailable.</summary>
        public bool ResolveEvent(int optionIndex)
        {
            if (State != RunState.AwaitingShop || PendingEvent == null || !IsEventOptionAvailable(optionIndex))
            {
                return false;
            }
            var feasible = EventOptionOutcomes(optionIndex);
            int totalWeight = 0;
            foreach (var o in feasible) { totalWeight += o.Weight; }
            int roll = _rng.Next(totalWeight);
            EventOutcome chosen = feasible[feasible.Count - 1];
            foreach (var outcome in feasible)
            {
                if (roll < outcome.Weight) { chosen = outcome; break; }
                roll -= outcome.Weight;
            }
            var result = new List<string>();
            _eventLostModifiers.Clear();
            foreach (var effect in chosen.Effects)
            {
                result.Add(ApplyEventEffect(effect));
            }
            _eventLostModifiers.Clear();
            if (result.Count == 0)
            {
                result.Add("Nothing happens.");
            }
            LastEventResult = result;
            PendingEvent = null;
            return true;
        }

        private string ApplyEventEffect(EventEffect effect)
        {
            switch (effect.Kind)
            {
                case EventEffectKind.Pollen:
                {
                    int before = Lueur;
                    Lueur = Mathf.Max(0, Lueur + effect.Amount);
                    if (effect.Amount >= 0) { return "You gain " + (Lueur - before) + " Pollen."; }
                    return before == Lueur ? "You have no Pollen to lose." : "You lose " + (before - Lueur) + " Pollen.";
                }
                case EventEffectKind.GrantFertilizer:
                {
                    var granted = GrantRandomModifier();
                    return granted.HasValue ? "You gain " + ModifierCatalog.Get(granted.Value).Name + "." : "Nothing grows for you this time.";
                }
                case EventEffectKind.LoseFertilizer:
                {
                    if (_activeModifiers.Count == 0) { return "You have nothing to lose."; }
                    int i = _rng.Next(_activeModifiers.Count);
                    string name = ModifierCatalog.Get(_activeModifiers[i]).Name;
                    _eventLostModifiers.Add(_activeModifiers[i]);
                    RemoveActiveModifierAt(i);
                    return "You lose " + name + ".";
                }
                case EventEffectKind.LevelUpFertilizer:
                {
                    if (_activeModifiers.Count == 0) { return "You have nothing to grow."; }
                    int i = _rng.Next(_activeModifiers.Count);
                    _modifierLevels[i] = _modifierLevels[i] + 1;
                    return ModifierCatalog.Get(_activeModifiers[i]).Name + " gains a level.";
                }
                case EventEffectKind.AddJoker:
                    Deck.AddJoker(_rng);
                    return "A Joker seed joins your bag.";
                case EventEffectKind.RemoveSeeds:
                {
                    int removed = 0;
                    for (int n = 0; n < effect.Amount && Deck.DeckCount > DeckManager.MinDeckSize; n++)
                    {
                        if (Deck.RemoveTokenAt(_rng.Next(Deck.DeckCount))) { removed++; }
                    }
                    return "You lose " + removed + (removed == 1 ? " seed." : " seeds.");
                }
                case EventEffectKind.NextRoundSeeds:
                    _eventSeedsRound = CurrentRoundIndex + 1;
                    _eventSeedsDelta = effect.Amount;
                    return effect.Amount >= 0 ? "Next round: " + effect.Amount + (effect.Amount == 1 ? " more seed." : " more seeds.") : "Next round: " + (-effect.Amount) + (effect.Amount == -1 ? " fewer seed." : " fewer seeds.");
                default:
                    _eventHpRound = CurrentRoundIndex + 1;
                    _eventHpFactor = Mathf.Max(0.1f, 1f + effect.Amount / 100f);
                    return effect.Amount >= 0 ? "Next round: enemies are " + effect.Amount + "% tougher." : "Next round: enemies are " + (-effect.Amount) + "% weaker.";
            }
        }

        /// <summary>The seeds an event added or removed for <paramref name="roundIndex"/> (0 for any other round).</summary>
        private int EventSeedsDeltaFor(int roundIndex)
        {
            return roundIndex == _eventSeedsRound ? _eventSeedsDelta : 0;
        }

        /// <summary>The HP factor an event applies to <paramref name="roundIndex"/>'s enemies (1 for any other round).</summary>
        private float EventHpFactorFor(int roundIndex)
        {
            return roundIndex == _eventHpRound ? _eventHpFactor : 1f;
        }
    }
}
