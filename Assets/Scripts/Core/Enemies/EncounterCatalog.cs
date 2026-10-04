using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Which enemies (in Shuffle-resolution/targeting order) show up in a
    /// given round's encounter (spec extension, explicit request: "ajouter
    /// un petit peu d'autobattling" — see GDD §07). Classic rounds 1-8 are
    /// now all authored (follow-up explicit request: "Ajoutons de nouveaux
    /// ennemies et boss"); every other round/challenge returns an empty
    /// list, which RunManager reads as "no encounter" and falls back to
    /// its original quota-based round-end rule (see
    /// RunManager.HasActiveEncounter) — Endless and Marathon/Chaos are
    /// deliberately left untouched for now.
    /// </summary>
    public static class EncounterCatalog
    {
        private static readonly EnemyId[] Round1 = { EnemyId.Basic };
        private static readonly EnemyId[] Round2 = { EnemyId.Locker };
        private static readonly EnemyId[] Round3 = { EnemyId.Poisoner };

        /// <summary>Round 4 is Classic's first scheduled boss round (RunConfig.BossRoundInterval) — stands in for a real Boss-tagged enemy with the two Creators fought together instead.</summary>
        private static readonly EnemyId[] Round4 = { EnemyId.Locker, EnemyId.Poisoner };

        /// <summary>Teaches the GDD's own "Order-based interactions" example (§07: "Poisoner creates poison before Reclaimer acts, allowing Reclaimer to consume it and heal") — Poisoner resolves first in encounter order, feeding Reclaimer.</summary>
        private static readonly EnemyId[] Round5 = { EnemyId.Poisoner, EnemyId.Reclaimer };

        /// <summary>Same pairing as Round5, reversed — "Reclaimer acts before the new poison exists, so it heals less or not at all; Poisoner then creates the poison" (GDD §07), teaching that order itself is the puzzle.</summary>
        private static readonly EnemyId[] Round6 = { EnemyId.Reclaimer, EnemyId.Poisoner };

        /// <summary>GDD §07's own example progression: "...multi-pressure encounters involving Thief and Leech."</summary>
        private static readonly EnemyId[] Round7 = { EnemyId.Thief, EnemyId.Leech };

        /// <summary>Round 8 is Classic's second scheduled boss round — a single beefy enemy (explicit request: "Un seul ennemi costaud"); Heavy Locker is the cleanest standalone fight of the 4 Boss-tagged candidates, since its lock-and-kill loop doesn't depend on any other enemy to be meaningful.</summary>
        private static readonly EnemyId[] Round8 = { EnemyId.HeavyLocker };

        public static IReadOnlyList<EnemyId> GetEncounter(ChallengeId challenge, int roundIndex)
        {
            if (challenge != ChallengeId.Classic)
            {
                return System.Array.Empty<EnemyId>();
            }

            switch (roundIndex)
            {
                case 0: return Round1;
                case 1: return Round2;
                case 2: return Round3;
                case 3: return Round4;
                case 4: return Round5;
                case 5: return Round6;
                case 6: return Round7;
                case 7: return Round8;
                default: return System.Array.Empty<EnemyId>();
            }
        }
    }
}
