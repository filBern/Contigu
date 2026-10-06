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
        // Redesign (explicit request: "on devrait juste faire des niveaux
        // avec de plus en plus d'ennemis avec des combinaisons différentes.
        // Peut-être mettre des basic enemi en début de file pour laisser le
        // temps aux autre d'instaurer des malus" — follow-up to "Le boss
        // heavy locker je le trouve pas extraordinaire", i.e. a lone
        // HeavyLocker HP-sponge round wasn't interesting enough on its
        // own). Every round from here on LEADS with a Basic (RunManager.
        // ApplyDamageToEncounter always hits the front ALIVE enemy, so
        // Basic — trivial, no special effect — soaks the first few
        // placements' damage, buying the enemy(ies) behind it a few
        // Shuffles to actually establish their lock/poison/steal before
        // the player can touch them) instead of being appended last as
        // filler. Enemy COUNT escalates round over round instead of a
        // single big-HP boss carrying a round alone; HeavyLocker still
        // shows up (Round8), just as part of a real multi-enemy gauntlet
        // rather than a solo tank fight.
        private static readonly EnemyId[] Round1 = { EnemyId.Basic };
        private static readonly EnemyId[] Round2 = { EnemyId.Basic, EnemyId.Locker };
        private static readonly EnemyId[] Round3 = { EnemyId.Basic, EnemyId.Poisoner };

        /// <summary>Round 4 is Classic's first scheduled boss round (RunConfig.BossRoundInterval) — stands in for a real Boss-tagged enemy with the two Creators fought together instead.</summary>
        private static readonly EnemyId[] Round4 = { EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner };

        /// <summary>
        /// Pairs the poison creator with its own counter, both now behind
        /// the leading Basic. Poisoner must stay in front of Reclaimer —
        /// follow-up to the Reclaimer grow mechanic (see EnemyInstance.
        /// HealOrGrow): "Il faudra donc tuer l'empoisonneur sans trop heal
        /// le reclaimer" only works as an actual tension if the player's
        /// damage lands on Poisoner before Reclaimer (RunManager.
        /// ApplyDamageToEncounter always hits the front alive enemy) —
        /// Reclaimer sitting behind it just grows quietly in the
        /// background off whatever poison-scoring happens while Poisoner
        /// is still alive, exactly the risk the player is meant to manage.
        /// </summary>
        private static readonly EnemyId[] Round5 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Reclaimer };

        /// <summary>Same Poisoner-before-Reclaimer pairing as Round5, escalated with a 4th enemy for this later round.</summary>
        private static readonly EnemyId[] Round6 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Reclaimer, EnemyId.Locker };

        /// <summary>GDD §07's own example progression: "...multi-pressure encounters involving Thief and Leech" — escalated with a 4th enemy for this later round.</summary>
        private static readonly EnemyId[] Round7 = { EnemyId.Basic, EnemyId.Thief, EnemyId.Leech, EnemyId.Locker };

        /// <summary>Round 8 is Classic's second scheduled boss round and the Classic run's finale — a full 5-enemy gauntlet instead of a lone HeavyLocker (explicit report: "Le boss heavy locker je le trouve pas extraordinaire"), reusing every Creator/Boss introduced so far behind the leading Basic.</summary>
        private static readonly EnemyId[] Round8 = { EnemyId.Basic, EnemyId.HeavyLocker, EnemyId.Poisoner, EnemyId.Thief, EnemyId.Leech };

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
