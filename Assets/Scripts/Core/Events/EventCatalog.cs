using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>Every event the garden can throw at the player (ids are saved: never renumber or reuse one).</summary>
    public static class EventCatalog
    {
        private static EventEffect Pollen(int n) { return new EventEffect(EventEffectKind.Pollen, n); }
        private static EventEffect Seeds(int n) { return new EventEffect(EventEffectKind.NextRoundSeeds, n); }
        private static EventEffect EnemyHp(int percent) { return new EventEffect(EventEffectKind.NextRoundEnemyHp, percent); }
        private static EventEffect Grant { get { return new EventEffect(EventEffectKind.GrantFertilizer); } }
        private static EventEffect Lose { get { return new EventEffect(EventEffectKind.LoseFertilizer); } }
        private static EventEffect LevelUp { get { return new EventEffect(EventEffectKind.LevelUpFertilizer); } }
        private static EventEffect Joker { get { return new EventEffect(EventEffectKind.AddJoker); } }
        private static EventEffect Remove(int n) { return new EventEffect(EventEffectKind.RemoveSeeds, n); }
        private static EventOutcome Do(int weight, params EventEffect[] effects) { return new EventOutcome(weight, effects); }

        public static readonly EventDefinition[] All =
        {
            new EventDefinition(1, "The Wandering Gardener",
                "A gardener with a barrow full of cuttings stops at your fence. \"Trade?\"",
                new EventOption("Buy a cutting", 8, Do(1, Pollen(-8), Grant)),
                new EventOption("Sell one of yours", 0, Do(1, Lose, Pollen(14))),
                new EventOption("Walk on", 0, Do(4, new EventEffect[0]), Do(1, EnemyHp(10)))),

            new EventDefinition(2, "A Strange Bloom",
                "Something glowing has pushed up between the rows. It smells of honey and thunder.",
                new EventOption("Taste the petals", 0, Do(1, LevelUp), Do(1, Pollen(-6))),
                new EventOption("Plant it in your bag", 0, Do(1, Joker, Seeds(-2))),
                new EventOption("Walk on", 0, Do(7, new EventEffect[0]), Do(3, Pollen(-3)))),

            new EventDefinition(3, "The Scarecrow Speaks",
                "The old scarecrow turns its head. \"I have seen what is coming. Do you want to know?\"",
                new EventOption("Listen", 0, Do(1, Seeds(3), EnemyHp(15))),
                new EventOption("Mock it", 0, Do(1, Pollen(10)), Do(1, Seeds(-2))),
                new EventOption("Walk on", 0, Do(17, new EventEffect[0]), Do(3, Pollen(-4)))),

            new EventDefinition(4, "Sudden Rain",
                "Fat drops, all at once. The whole garden sits up.",
                new EventOption("Collect the rainwater", 0, Do(1, Pollen(8), Seeds(-1))),
                new EventOption("Let the roots drink", 0, Do(3, LevelUp), Do(2, new EventEffect[0])),
                new EventOption("Walk on", 0, Do(9, new EventEffect[0]), Do(1, Pollen(-2)))),

            new EventDefinition(5, "The Compost Heap",
                "The heap behind the shed has been steaming all week. Something is moving in it.",
                new EventOption("Dig deep", 0, Do(2, Joker), Do(2, Pollen(6)), Do(1, Remove(3))),
                new EventOption("Burn it", 0, Do(1, Remove(3), Pollen(12))),
                new EventOption("Walk on", 0, Do(4, new EventEffect[0]), Do(1, Seeds(-1)))),

            new EventDefinition(6, "A Crow's Bargain",
                "A crow lands on the gatepost, a shiny seed in its beak. It tilts its head.",
                new EventOption("Feed the crow", 5, Do(1, Pollen(-5), EnemyHp(-15))),
                new EventOption("Swap a fertilizer", 0, Do(1, Lose, Grant)),
                new EventOption("Walk on", 0, Do(3, new EventEffect[0]), Do(1, EnemyHp(10)))),

            new EventDefinition(7, "Moonlit Greenhouse",
                "A seed on the greenhouse bench is glowing in the dark.",
                new EventOption("Water it", 10, Do(1, Pollen(-10), LevelUp)),
                new EventOption("Pick it", 0, Do(1, Grant), Do(1, Pollen(-5))),
                new EventOption("Walk on", 0, Do(1, new EventEffect[0]))),

            new EventDefinition(8, "The Old Well",
                "A coin glints at the bottom of the well. The water looks very still.",
                new EventOption("Make a wish", 6, Do(1, Pollen(-6), Grant), Do(1, Pollen(9)), Do(1, Pollen(-6))),
                new EventOption("Drink", 0, Do(3, Seeds(2)), Do(1, Seeds(-2))),
                new EventOption("Walk on", 0, Do(9, new EventEffect[0]), Do(1, Pollen(-2))))
        };

        public static EventDefinition Get(int id)
        {
            foreach (var e in All)
            {
                if (e.Id == id) { return e; }
            }
            return null;
        }

        /// <summary>A random event the run has not seen yet; once every event has been seen, they all become possible again.</summary>
        public static EventDefinition Pick(IRandomProvider rng, ICollection<int> seen)
        {
            var pool = new List<EventDefinition>();
            foreach (var e in All)
            {
                if (!seen.Contains(e.Id)) { pool.Add(e); }
            }
            if (pool.Count == 0)
            {
                seen.Clear();
                pool.AddRange(All);
            }
            return pool[rng.Next(pool.Count)];
        }
    }
}
