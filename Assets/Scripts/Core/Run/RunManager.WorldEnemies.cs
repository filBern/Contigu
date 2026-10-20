using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>The residents of each challenge's world (see ChallengeDefinition.WorldEnemy): Hoarfrost, Locust Swarm, Rotgrub, Fruitworm and Whitefly Cloud, all acting on each Shuffle.</summary>
    public sealed partial class RunManager
    {
        /// <summary>The world enemy that acted on the most recent Shuffle (manual, or the auto-refill a placement triggers) — null when none did. Presentation pulses its portrait and says what happened.</summary>
        public EnemyId? LastWorldEnemyAct { get; private set; }

        /// <summary>One line describing what <see cref="LastWorldEnemyAct"/> just did.</summary>
        public string LastWorldEnemyMessage { get; private set; } = string.Empty;

        /// <summary>Pollen the Locust Swarm devoured on the most recent Shuffle (0 otherwise).</summary>
        public int LastPollenEaten { get; private set; }

        private void ResetWorldEnemyReport()
        {
            LastPollenEaten = 0;
            LastWorldEnemyAct = null;
            LastWorldEnemyMessage = string.Empty;
        }

        private void ReportWorldEnemy(EnemyId id, string message)
        {
            LastWorldEnemyAct = id;
            LastWorldEnemyMessage = message;
        }

        /// <summary>Hoarfrost: freezes 1 random empty plot; only the newest <see cref="ScoringConstants.HoarfrostMaxFrozenPlots"/> stay frozen, the oldest thaws.</summary>
        private void ResolveHoarfrostShuffleEffect(EnemyInstance hoarfrost)
        {
            var frozen = Grid.FreezeRandomCells(1, _rng);
            if (frozen.Count == 0)
            {
                return;
            }
            hoarfrost.FrozenCells.Add(frozen[0]);
            while (hoarfrost.FrozenCells.Count > ScoringConstants.HoarfrostMaxFrozenPlots)
            {
                ThawCell(hoarfrost.FrozenCells[0]);
                hoarfrost.FrozenCells.RemoveAt(0);
            }
            ReportWorldEnemy(EnemyId.Hoarfrost, "Hoarfrost freezes a plot.");
        }

        private void ThawCell(Vector2Int pos)
        {
            var cell = Grid.GetCell(pos);
            if (cell.IsFrozen)
            {
                cell.IsFrozen = false;
                cell.IsLocked = false;
            }
        }

        /// <summary>Hoarfrost died: every plot it froze thaws (and is reported as released, like a dying Bramble's lock).</summary>
        private void ThawHoarfrostCells(EnemyInstance hoarfrost)
        {
            for (int i = 0; i < hoarfrost.FrozenCells.Count; i++)
            {
                var pos = hoarfrost.FrozenCells[i];
                if (Grid.GetCell(pos).IsFrozen)
                {
                    ThawCell(pos);
                    _deathReleasedCellsThisPlacement.Add(pos);
                }
            }
            hoarfrost.FrozenCells.Clear();
        }

        /// <summary>Locust Swarm: devours Pollen (as much as there is, up to <see cref="ScoringConstants.LocustPollenPerShuffle"/>).</summary>
        private void ResolveLocustShuffleEffect()
        {
            int eaten = Mathf.Min(Lueur, ScoringConstants.LocustPollenPerShuffle);
            if (eaten <= 0)
            {
                return;
            }
            Lueur -= eaten;
            LastPollenEaten = eaten;
            ReportWorldEnemy(EnemyId.Locust, "The Locust Swarm devours " + eaten + " Pollen.");
        }

        /// <summary>Rotgrub: ages the fertilizer closest to withering by a round (only when this challenge's fertilizers wither).</summary>
        private void ResolveRotgrubShuffleEffect()
        {
            if (_challenge.FertilizerLifespanRounds <= 0 || _activeModifiers.Count == 0)
            {
                return;
            }
            int target = -1;
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (target < 0 || _modifierAges[i] > _modifierAges[target])
                {
                    target = i;
                }
            }
            if (target < 0)
            {
                return;
            }
            _modifierAges[target]++;
            ReportWorldEnemy(EnemyId.Rotgrub, "Rotgrub gnaws at " + ModifierCatalog.Get(_activeModifiers[target]).Name + ".");
        }

        /// <summary>Fruitworm: turns one planted tile into the other color.</summary>
        private void ResolveFruitwormShuffleEffect()
        {
            var recolored = Grid.RecolorRandomTile(_rng);
            if (recolored.HasValue)
            {
                ReportWorldEnemy(EnemyId.Fruitworm, "The Fruitworm turns a tile into another color.");
            }
        }

        /// <summary>Whitefly Cloud: swaps two planted tiles.</summary>
        private void ResolveWhiteflyShuffleEffect()
        {
            var swapped = Grid.SwapRandomTiles(_rng);
            if (swapped.HasValue)
            {
                ReportWorldEnemy(EnemyId.Whitefly, "The Whitefly Cloud swaps two tiles.");
            }
        }
    }
}
