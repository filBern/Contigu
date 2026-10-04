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
        // Basic appended to most rounds (explicit request: "Le jeu est
        // beaucoup trop facile, je ne perd jamais, peux-tu ajouter des
        // basics enemies dans les niveaux. Ça va aider le joueur a se
        // sentir plus puissant au lieu") — more total HP to clear per
        // round (objectively less easy), but since Basic itself is a
        // trivial, no-special-effect kill, it reads as an easy mop-up
        // rather than a harder puzzle, which is the "feel more powerful"
        // part of the request. Always appended LAST, never leading — for
        // Round5/Round6 that's also load-bearing: Poisoner must stay the
        // FRONT (first-targeted) enemy for their own order-dependent
        // tension (see Round5's own doc comment), and appending Basic
        // after Reclaimer never disturbs that. Round1 (the very first
        // introduction) and Round8 (explicitly "un seul ennemi costaud")
        // are deliberately left alone.
        private static readonly EnemyId[] Round1 = { EnemyId.Basic };
        private static readonly EnemyId[] Round2 = { EnemyId.Locker, EnemyId.Basic };
        private static readonly EnemyId[] Round3 = { EnemyId.Poisoner, EnemyId.Basic };

        /// <summary>Round 4 is Classic's first scheduled boss round (RunConfig.BossRoundInterval) — stands in for a real Boss-tagged enemy with the two Creators fought together instead.</summary>
        private static readonly EnemyId[] Round4 = { EnemyId.Locker, EnemyId.Poisoner, EnemyId.Basic };

        /// <summary>
        /// Pairs the poison creator with its own counter. Poisoner MUST
        /// stay first — explicit request, follow-up to the Reclaimer grow
        /// mechanic (see EnemyInstance.HealOrGrow): "Il faudra donc tuer
        /// l'empoisonneur sans trop heal le reclaimer" only works as an
        /// actual tension if the player's damage lands on Poisoner first
        /// (RunManager.ApplyDamageToEncounter always hits the front alive
        /// enemy) — Reclaimer sitting second just grows quietly in the
        /// background off whatever poison-scoring happens while Poisoner
        /// is still alive, exactly the risk the player is meant to manage.
        /// </summary>
        private static readonly EnemyId[] Round5 = { EnemyId.Poisoner, EnemyId.Reclaimer };

        /// <summary>
        /// Same pairing as Round5 — used to be deliberately REVERSED here
        /// to teach the GDD's original Shuffle-order dependency, but that
        /// dependency is gone now that Reclaimer heals reactively off
        /// poison-negative scoring instead of consuming poison on its own
        /// Shuffle (see HealReclaimer). Explicit bug report once the grow
        /// mechanic made order matter again for a NEW reason (targeting,
        /// not Shuffle sequencing): "la round avec le reclaimer, il doit se
        /// trouver après l'empoisonneur" — fixed to match Round5's own
        /// order, now escalated with an extra Basic for this later round.
        /// </summary>
        private static readonly EnemyId[] Round6 = { EnemyId.Poisoner, EnemyId.Reclaimer, EnemyId.Basic };

        /// <summary>GDD §07's own example progression: "...multi-pressure encounters involving Thief and Leech."</summary>
        private static readonly EnemyId[] Round7 = { EnemyId.Thief, EnemyId.Leech, EnemyId.Basic };

        /// <summary>Round 8 is Classic's second scheduled boss round — a single beefy enemy (explicit request: "Un seul ennemi costaud"); Heavy Locker is the cleanest standalone fight of the 4 Boss-tagged candidates, since its lock-and-kill loop doesn't depend on any other enemy to be meaningful. Deliberately NOT padded with a Basic like the other rounds — a dedicated solo boss round stays solo.</summary>
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
