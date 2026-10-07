using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// One enemy actually present in the CURRENT round's encounter (see
    /// RunManager.CurrentEncounter/BuildEncounter) — built fresh every
    /// round from its EnemyDefinition, never carried over or reused across
    /// rounds even for the same EnemyId (a "Locker" fought in round 2 and
    /// another "Locker" fought in round 4 are two unrelated instances).
    /// </summary>
    public sealed class EnemyInstance
    {
        public EnemyDefinition Definition { get; }
        public int CurrentHp { get; private set; }

        /// <summary>
        /// This instance's own HP ceiling — starts equal to <see
        /// cref="EnemyDefinition.MaxHp"/> but can grow past it (see <see
        /// cref="HealOrGrow"/>), so it's tracked per-instance rather than
        /// read straight off the shared <see cref="Definition"/> (which
        /// every instance of the same EnemyId points at — mutating IT
        /// would grow every copy of this enemy across every round of every
        /// run at once).
        /// </summary>
        public int CurrentMaxHp { get; private set; }

        public bool IsDead
        {
            get { return CurrentHp <= 0; }
        }

        /// <summary>Locker only: the cell it currently has locked, or null before its first Shuffle tick or once it's dead (see RunManager.ResolveLockerShuffleEffect/CleanUpDefeatedEnemy).</summary>
        public Vector2Int? LockedCell;

        /// <summary>Color Hater only: the one base color it was randomly assigned on spawn (see RunManager.BuildEncounter), never changes for the rest of this instance's life. Null for every other EnemyId.</summary>
        public PieceColor? HatedColor;

        /// <summary>Shape Hater only: Color Hater's exact mirror, the one piece shape it was randomly assigned on spawn (see RunManager.BuildEncounter). Null for every other EnemyId.</summary>
        public ShapeId? HatedShape;

        private readonly List<Vector2Int> _poisonedCells = new List<Vector2Int>();

        /// <summary>Poisoner/Plague: every cell THIS instance has poisoned so far, including any extra cell "contamination" spread onto a neighbor in between Shuffles (see RunManager.ContaminateAdjacentCell) — unlike Locker's single roaming lock, poison accumulates until this instance's next Shuffle releases the whole list at once, or until it dies (see RunManager.ResolvePoisonerShuffleEffect/CleanUpDefeatedEnemy).</summary>
        public IReadOnlyList<Vector2Int> PoisonedCells
        {
            get { return _poisonedCells; }
        }

        public EnemyInstance(EnemyDefinition definition)
        {
            Definition = definition;
            CurrentMaxHp = definition.MaxHp;
            CurrentHp = CurrentMaxHp;
        }

        public void AddPoisonedCell(Vector2Int pos)
        {
            _poisonedCells.Add(pos);
        }

        public void ClearPoisonedCells()
        {
            _poisonedCells.Clear();
        }

        /// <summary>
        /// "Reclaimer" (redesign — explicit request: "Chaque points
        /// négatifs triggered par une tuile empoisonné, l'ennemi reclaimer
        /// récupère en point de vie ce montant là") — clamped at <see
        /// cref="CurrentMaxHp"/>, same ceiling <see cref="ApplyDamage"/>'s
        /// own negative-damage healing already respects — see <see
        /// cref="HealOrGrow"/> for the Reclaimer/Leech variant that grows
        /// the ceiling instead of capping out.
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, CurrentMaxHp);
        }

        /// <summary>
        /// "Reclaimer" grow mechanic (explicit request: "j'aimerais
        /// ajouter pour le reclaimer que s'il est heal ET qu'il est full
        /// health, il augmente son max health et son health pour devenir
        /// plus fort. Il faudra donc tuer l'empoisonneur sans trop heal le
        /// reclaimer") — a heal that arrives while ALREADY at <see
        /// cref="CurrentMaxHp"/> doesn't just cap out and go to waste: it
        /// raises BOTH CurrentMaxHp and CurrentHp by the same amount
        /// instead, making this instance permanently tougher. A heal that
        /// only PARTIALLY overflows (not yet full before this heal, but
        /// would exceed the cap) still just clamps normally like <see
        /// cref="Heal"/> — growth is specifically for being AT full
        /// already when more healing arrives, not for any excess. Used by
        /// RunManager.HealReclaimer and RunManager.HealLeech (explicit
        /// request: "le boss leech ... il devient de plus en plus fort s'il
        /// est déjà full, son max HP augmente aussi") — poison's own
        /// negative-damage-heals-the-front-enemy-back-up (via ApplyDamage)
        /// is the only healer left that stays capped at its ordinary
        /// CurrentMaxHp with no growth.
        /// </summary>
        public void HealOrGrow(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }
            if (CurrentHp >= CurrentMaxHp)
            {
                CurrentMaxHp += amount;
                CurrentHp += amount;
                return;
            }
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, CurrentMaxHp);
        }

        /// <summary>
        /// Applies <paramref name="damage"/> (this run's own placement score
        /// — see RunManager.ApplyDamageToEncounter) clamped to [0,
        /// CurrentMaxHp]. Can be negative: a placement that scored through
        /// one of this instance's own poisoned tiles comes out net-negative
        /// (see RunManager.ApplyPoisonScoreRule), which HEALS this enemy
        /// back up instead of damaging it — intentional, the cost of
        /// playing into poison; capped at the ordinary CurrentMaxHp, never
        /// triggers Reclaimer's own growth (see HealOrGrow — that's
        /// deliberately only reachable through RunManager.HealReclaimer's
        /// own poison-magnitude heal, not through absorbing damage as the
        /// front target). Returns true if this hit just brought it from
        /// alive to dead (false if it was already dead, or if it survives).
        /// </summary>
        public bool ApplyDamage(int damage)
        {
            if (IsDead)
            {
                return false;
            }
            CurrentHp = Mathf.Clamp(CurrentHp - damage, 0, CurrentMaxHp);
            return IsDead;
        }
    }
}
