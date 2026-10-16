using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Which enemies (in Shuffle-resolution/targeting order) show up in a
    /// given round's encounter. Marathon/Chaos return an empty list for
    /// every round, which RunManager reads as "no encounter" and falls back
    /// to the original quota-based round-end rule (see
    /// RunManager.HasActiveEncounter).
    /// </summary>
    public static class EncounterCatalog
    {
        // Every round from here on leads with a Basic (RunManager.
        // ApplyDamageToEncounter always hits the front alive enemy, so
        // Basic — trivial, no special effect — soaks the first few
        // placements' damage, buying the enemy(ies) behind it a few
        // Shuffles to actually establish their lock/poison/steal before
        // the player can touch them). Enemy count escalates round over
        // round instead of a single big-HP boss carrying a round alone.
        private static readonly EnemyId[] Round1 = { EnemyId.Basic };
        private static readonly EnemyId[] Round2 = { EnemyId.Basic, EnemyId.Locker };
        private static readonly EnemyId[] Round3 = { EnemyId.Basic, EnemyId.Poisoner };

        /// <summary>Round 4 is Classic's first scheduled boss round (RunConfig.BossRoundInterval) — stands in for a real Boss-tagged enemy with the two Creators fought together instead.</summary>
        private static readonly EnemyId[] Round4 = { EnemyId.Basic, EnemyId.Locker, EnemyId.Poisoner };

        /// <summary>
        /// Pairs the poison creator with its own counter, both behind the
        /// leading Basic. Poisoner must stay in front of Reclaimer (see
        /// EnemyInstance.HealOrGrow): the player's damage needs to land on
        /// Poisoner before Reclaimer (RunManager.ApplyDamageToEncounter
        /// always hits the front alive enemy) — Reclaimer sitting behind
        /// it grows quietly in the background off whatever poison-scoring
        /// happens while Poisoner is still alive.
        /// </summary>
        private static readonly EnemyId[] Round5 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Reclaimer };

        /// <summary>Same Poisoner-before-Reclaimer pairing as Round5, escalated with a second Poisoner, doubling the poison pressure ahead of Reclaimer.</summary>
        private static readonly EnemyId[] Round6 = { EnemyId.Basic, EnemyId.Poisoner, EnemyId.Poisoner, EnemyId.Reclaimer };

        private static readonly EnemyId[] Round7 = { EnemyId.Basic, EnemyId.Locker, EnemyId.Thief, EnemyId.Leech };

        /// <summary>A full 5-enemy gauntlet reusing every Creator/Boss introduced so far behind the leading Basic — the midpoint of the run; Act 2 (Round9-15 below) picks back up from here.</summary>
        private static readonly EnemyId[] Round8 = { EnemyId.Basic, EnemyId.HeavyLocker, EnemyId.Poisoner, EnemyId.Thief, EnemyId.Leech };

        // Act 2 (Round9-15): same escalating-count, Basic-always-leads
        // conventions as Round1-8 above, debuting the 2 enemies Round1-8
        // never used (Plague, then the ColorHater/ShapeHater boss pair —
        // see EnemyCatalog), never more than one ColorHater or one
        // ShapeHater in the same round.

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
