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

        public bool IsDead
        {
            get { return CurrentHp <= 0; }
        }

        /// <summary>Locker only: the cell it currently has locked, or null before its first Shuffle tick or once it's dead (see RunManager.ResolveLockerShuffleEffect/CleanUpDefeatedEnemy).</summary>
        public Vector2Int? LockedCell;

        private readonly List<Vector2Int> _poisonedCells = new List<Vector2Int>();

        /// <summary>Poisoner/Plague: every cell THIS instance has poisoned so far, including any extra cell "contamination" spread onto a neighbor in between Shuffles (see RunManager.ContaminateAdjacentCell) — unlike Locker's single roaming lock, poison accumulates until this instance's next Shuffle releases the whole list at once, or until it dies (see RunManager.ResolvePoisonerShuffleEffect/CleanUpDefeatedEnemy).</summary>
        public IReadOnlyList<Vector2Int> PoisonedCells
        {
            get { return _poisonedCells; }
        }

        public EnemyInstance(EnemyDefinition definition)
        {
            Definition = definition;
            CurrentHp = definition.MaxHp;
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
        /// cref="EnemyDefinition.MaxHp"/>, same ceiling <see
        /// cref="ApplyDamage"/>'s own negative-damage healing already
        /// respects.
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, Definition.MaxHp);
        }

        /// <summary>
        /// Applies <paramref name="damage"/> (this run's own placement score
        /// — see RunManager.ApplyDamageToEncounter) clamped to [0, MaxHp].
        /// Can be negative: a placement that scored through one of this
        /// instance's own poisoned tiles comes out net-negative (see
        /// RunManager.ApplyPoisonScoreRule), which HEALS this enemy back up
        /// instead of damaging it — intentional, the cost of playing into
        /// poison. Returns true if this hit just brought it from alive to
        /// dead (false if it was already dead, or if it survives).
        /// </summary>
        public bool ApplyDamage(int damage)
        {
            if (IsDead)
            {
                return false;
            }
            CurrentHp = Mathf.Clamp(CurrentHp - damage, 0, Definition.MaxHp);
            return IsDead;
        }
    }
}
