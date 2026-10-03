using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Which enemies (in Shuffle-resolution/targeting order) show up in a
    /// given round's encounter (spec extension, explicit request: "ajouter
    /// un petit peu d'autobattling" — see GDD §07). Vertical slice, scoped
    /// deliberately small to prove the combat loop first: only Classic's
    /// first 4 rounds have an authored encounter here. Every other round/
    /// challenge returns an empty list, which RunManager reads as "no
    /// encounter" and falls back to its original quota-based round-end
    /// rule (see RunManager.HasActiveEncounter) — rounds 5-8, Endless, and
    /// Marathon/Chaos are explicitly left untouched until the rest of the
    /// roster (Heavy Locker, Plague, Thief, Reclaimer, Leech) is built.
    /// </summary>
    public static class EncounterCatalog
    {
        private static readonly EnemyId[] Round1 = { EnemyId.Basic };
        private static readonly EnemyId[] Round2 = { EnemyId.Locker };
        private static readonly EnemyId[] Round3 = { EnemyId.Poisoner };

        /// <summary>Round 4 is Classic's first scheduled boss round (RunConfig.BossRoundInterval) — stands in for a real Boss-tagged enemy with the two Creators fought together instead, until Heavy Locker/Plague exist.</summary>
        private static readonly EnemyId[] Round4 = { EnemyId.Locker, EnemyId.Poisoner };

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
                default: return System.Array.Empty<EnemyId>();
            }
        }
    }
}
