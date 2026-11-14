using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    public enum WorldGridChangeKind { Freeze, Recolor, Swap }

    /// <summary>One resident enemy's grid change: Freeze (A frozen, B thawed if HasB), Recolor (A: ColorA to ColorB), Swap (A and B traded places; ColorA/ColorB are what each plot held BEFORE).</summary>
    public struct WorldGridChange
    {
        public WorldGridChangeKind Kind;
        public Vector2Int A;
        public Vector2Int B;
        public bool HasB;
        public PieceColor ColorA;
        public PieceColor ColorB;
    }

    /// <summary>The residents of each challenge's world (see ChallengeDefinition.WorldEnemy): Hoarfrost, Locust Swarm, Rotgrub, Fruitworm and Whitefly Cloud, all acting on each Shuffle.</summary>
    public sealed partial class RunManager
    {
        /// <summary>The world enemy that acted on the most recent Shuffle (manual, or the auto-refill a placement triggers) — null when none did. Presentation pulses its portrait and says what happened.</summary>
        public EnemyId? LastWorldEnemyAct { get; private set; }

        /// <summary>One line describing what <see cref="LastWorldEnemyAct"/> just did.</summary>
        public string LastWorldEnemyMessage { get; private set; } = string.Empty;

        /// <summary>Pollen the Locust Swarm devoured on the most recent Shuffle (0 otherwise).</summary>
        public int LastPollenEaten { get; private set; }

        /// <summary>What a world enemy changed on the grid on the most recent Shuffle (null when it changed nothing there), so Presentation can keep the old look on screen until the score has been counted, then play the change.</summary>
        public WorldGridChange? LastWorldGridChange { get; private set; }

        private void ResetWorldEnemyReport()
        {
            LastWorldGridChange = null;
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
            Vector2Int? thawed = null;
            while (hoarfrost.FrozenCells.Count > ScoringConstants.HoarfrostMaxFrozenPlots)
            {
                thawed = hoarfrost.FrozenCells[0];
                ThawCell(hoarfrost.FrozenCells[0]);
                hoarfrost.FrozenCells.RemoveAt(0);
            }
            LastWorldGridChange = new WorldGridChange { Kind = WorldGridChangeKind.Freeze, A = frozen[0], B = thawed ?? frozen[0], HasB = thawed.HasValue };
            ReportWorldEnemy(EnemyId.Hoarfrost, "Hoarfrost freezes a plot.");
        }

        /// <summary>A line that harvests a frozen plot breaks its ice behind the Hoarfrost's back: the Hoarfrost must stop counting that plot toward its limit.</summary>
        private void ForgetBrokenIce()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var frozen = _currentEncounter[i].FrozenCells;
                for (int c = frozen.Count - 1; c >= 0; c--)
                {
                    if (!Grid.GetCell(frozen[c]).IsFrozen)
                    {
                        frozen.RemoveAt(c);
                    }
                }
            }
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
                LastWorldGridChange = new WorldGridChange { Kind = WorldGridChangeKind.Recolor, A = recolored.Value.Pos, ColorA = recolored.Value.From, ColorB = recolored.Value.To };
                ReportWorldEnemy(EnemyId.Fruitworm, "The Fruitworm turns a tile into another color.");
            }
        }

        /// <summary>Whitefly Cloud: swaps two planted tiles.</summary>
        private void ResolveWhiteflyShuffleEffect()
        {
            var swapped = Grid.SwapRandomTiles(_rng);
            if (swapped.HasValue)
            {
                // After the swap each plot holds the other's old color, so the old ones are read back from the opposite plot.
                LastWorldGridChange = new WorldGridChange
                {
                    Kind = WorldGridChangeKind.Swap,
                    A = swapped.Value.A,
                    B = swapped.Value.B,
                    HasB = true,
                    ColorA = Grid.GetCell(swapped.Value.B).FilledColor ?? PieceColor.Coral,
                    ColorB = Grid.GetCell(swapped.Value.A).FilledColor ?? PieceColor.Coral
                };
                ReportWorldEnemy(EnemyId.Whitefly, "The Whitefly Cloud swaps two tiles.");
            }
        }
    }
}
