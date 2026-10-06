using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Which enemies (in Shuffle-resolution/targeting order) show up in a
    /// given round's encounter (spec extension, explicit request: "ajouter
    /// un petit peu d'autobattling" — see GDD §07). Classic's now all 15
    /// rounds are authored (follow-up explicit requests: "Ajoutons de
    /// nouveaux ennemies et boss", then "Est-ce que tu peux faire 15
    /// rounds ... Je te fais confiance sur l'enchainement des enemy
    /// encounters"); Marathon/Chaos return an empty list for every round,
    /// which RunManager reads as "no encounter" and falls back to the
    /// original quota-based round-end rule (see
    /// RunManager.HasActiveEncounter) — deliberately left untouched.
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

        /// <summary>
        /// Same Poisoner-before-Reclaimer pairing as Round5, escalated with
        /// a 4th enemy — a SECOND Poisoner instead of Locker (explicit
        /// request: "round 6, remplace le locker par un autre poisoner et
        /// met le avant le reclaimer"), doubling the poison pressure ahead
        /// of Reclaimer rather than adding an unrelated lock mechanic.
        /// </summary>
        private static readonly EnemyId[] Round6 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Poisoner, EnemyId.Reclaimer };

        /// <summary>GDD §07's own example progression: "...multi-pressure encounters involving Thief and Leech" — escalated with a 4th enemy for this later round, Locker moved ahead of Thief (explicit request: "round 7, met le locket devant le thief").</summary>
        private static readonly EnemyId[] Round7 = { EnemyId.Basic, EnemyId.Locker, EnemyId.Thief, EnemyId.Leech };

        /// <summary>Round 8 was Classic's old finale (8-round run) — a full 5-enemy gauntlet instead of a lone HeavyLocker (explicit report: "Le boss heavy locker je le trouve pas extraordinaire"), reusing every Creator/Boss introduced so far behind the leading Basic. Now the midpoint of a 15-round run (explicit request: "Est-ce que tu peux faire 15 rounds") — Act 2 (Round9-15 below) picks back up from here.</summary>
        private static readonly EnemyId[] Round8 = { EnemyId.Basic, EnemyId.HeavyLocker, EnemyId.Poisoner, EnemyId.Thief, EnemyId.Leech };

        // ---- Act 2 (Round9-15), added on explicit request: "Est-ce que tu
        // peux faire 15 rounds ... Je te fais confiance sur l'enchainement
        // des enemy encounters" — same escalating-count, Basic-always-
        // leads conventions as Round1-8 above, debuting the 2 enemies
        // Round1-8 never used (Plague, then the newly-added ColorHater/
        // ShapeHater boss pair — see EnemyCatalog), and never more than
        // one ColorHater or one ShapeHater in the same round (explicit
        // constraint: "Il ne peut pas y avoir plus d'un de chaque par
        // round").

        /// <summary>Act 2 opens with Plague (never used in Round1-8), paired with Locker for a lock+poison combo — same leading Basic as ever.</summary>
        private static readonly EnemyId[] Round9 = { EnemyId.Basic, EnemyId.Locker, EnemyId.Plague };

        /// <summary>Color Hater's debut, paired with Thief — losing a hand piece on top of a whole color scoring nothing.</summary>
        private static readonly EnemyId[] Round10 = { EnemyId.Basic, EnemyId.ColorHater, EnemyId.Thief };

        /// <summary>Shape Hater's debut, Color Hater's own mirror, paired with Leech instead — a healing sponge behind the shape penalty.</summary>
        private static readonly EnemyId[] Round11 = { EnemyId.Basic, EnemyId.ShapeHater, EnemyId.Leech };

        /// <summary>Doubles down on poison pressure — Poisoner AND Plague, both still ahead of Reclaimer (same convention as Round5/6: the poison source must land the player's damage before Reclaimer, so it keeps growing quietly off whatever poison-scoring slips through).</summary>
        private static readonly EnemyId[] Round12 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Plague, EnemyId.Reclaimer };

        /// <summary>Both new boss Haters at once (still only one of each, satisfying the "never more than one of each per round" constraint) plus Thief — three independent ways to lose points/pieces in the same round.</summary>
        private static readonly EnemyId[] Round13 = { EnemyId.Basic, EnemyId.ColorHater, EnemyId.ShapeHater, EnemyId.Thief };

        /// <summary>HeavyLocker returns alongside Poisoner/Plague's full poison pressure and Leech healing off the line clears the player needs to survive the lock — the heaviest lock/poison round yet.</summary>
        private static readonly EnemyId[] Round14 = { EnemyId.Basic, EnemyId.HeavyLocker, EnemyId.Poisoner, EnemyId.Plague, EnemyId.Leech };

        /// <summary>The run's true finale — every mechanic introduced since Round8's own gauntlet (HeavyLocker's lock, both new Haters, Thief, Leech) together behind the leading Basic, the biggest roster of the whole run.</summary>
        private static readonly EnemyId[] Round15 = { EnemyId.Basic, EnemyId.HeavyLocker, EnemyId.ColorHater, EnemyId.ShapeHater, EnemyId.Thief, EnemyId.Leech };

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
                case 8: return Round9;
                case 9: return Round10;
                case 10: return Round11;
                case 11: return Round12;
                case 12: return Round13;
                case 13: return Round14;
                case 14: return Round15;
                default: return System.Array.Empty<EnemyId>();
            }
        }
    }
}
