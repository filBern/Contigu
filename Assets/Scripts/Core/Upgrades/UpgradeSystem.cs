using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Rolls draft offers and applies the chosen upgrade's effect. Pools never get
    /// exhausted: the same upgrade can be offered/picked multiple times in a run,
    /// stacking its effect.
    /// </summary>
    public sealed class UpgradeSystem
    {
        /// <summary>How many free/unmodified cells a Golden-cells upgrade adds. Kept at 1 (down from 3) — 3 at once was overpowered.</summary>
        public const int GoldenCellsCount = 1;

        /// <summary>How many free/unmodified cells a Tinted-cells upgrade adds. Kept at 1 (down from 2) — see GoldenCellsCount.</summary>
        public const int TintedCellsCount = 1;

        /// <summary>How many free/unmodified cells a Multiplier-zone upgrade adds. Kept at 1 (down from 3) — see GoldenCellsCount.</summary>
        public const int MultiplierZoneCount = 1;

        private readonly IRandomProvider _rng;

        public UpgradeSystem(IRandomProvider rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Rolls a round-end draft of exactly 3 options: one guaranteed Bank
        /// pick, one guaranteed Grid pick, one random pick from either pool
        /// (Bank and Grid mixed together) — the player picks exactly one.
        /// </summary>
        public UpgradeDraft RollDraft()
        {
            var bankPick = UpgradeCatalog.BankPool[_rng.Next(UpgradeCatalog.BankPool.Length)];
            var gridPick = UpgradeCatalog.GridPool[_rng.Next(UpgradeCatalog.GridPool.Length)];

            var remainingPool = new List<UpgradeDefinition>();
            for (int i = 0; i < UpgradeCatalog.All.Length; i++)
            {
                var candidate = UpgradeCatalog.All[i];
                if (candidate != bankPick && candidate != gridPick)
                {
                    remainingPool.Add(candidate);
                }
            }
            // Pools never run out, but keep this defensive in case the catalog
            // ever shrinks to just 2 entries.
            if (remainingPool.Count == 0)
            {
                remainingPool.AddRange(UpgradeCatalog.All);
            }

            var thirdPick = remainingPool[_rng.Next(remainingPool.Count)];

            return new UpgradeDraft(new[] { bankPick, gridPick, thirdPick });
        }

        /// <summary>Picks up to <paramref name="count"/> distinct entries at random from <paramref name="pool"/>, without replacement.</summary>
        public static T[] PickDistinct<T>(IReadOnlyList<T> pool, int count, IRandomProvider rng)
        {
            var remaining = new List<T>(pool);
            int take = count < remaining.Count ? count : remaining.Count;
            var result = new T[take];
            for (int i = 0; i < take; i++)
            {
                int idx = rng.Next(remaining.Count);
                result[i] = remaining[idx];
                remaining.RemoveAt(idx);
            }
            return result;
        }

        /// <summary>
        /// Applies the chosen upgrade's permanent effect. Returns false if a
        /// sub-choice-requiring upgrade could not be resolved (e.g. removing the
        /// last copy of a type while the deck is at its floor).
        /// </summary>
        public bool Apply(UpgradeDefinition upgrade, UpgradeSubChoice subChoice, GridManager grid, DeckManager deck)
        {
            switch (upgrade.Id)
            {
                case UpgradeId.RemovePiece:
                    return deck.RemoveOneOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.DuplicatePiece:
                    return deck.DuplicateOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.JokerPiece:
                    deck.AddJoker();
                    return true;

                case UpgradeId.RecolorPiece:
                    return deck.RecolorOneOfType(subChoice.Shape, subChoice.Color, subChoice.TargetColor);

                case UpgradeId.GoldenCells:
                    grid.ApplyGoldenCellsRandom(GoldenCellsCount, _rng);
                    return true;

                case UpgradeId.TintedCells:
                    grid.ApplyTintedCellsRandom(TintedCellsCount, _rng);
                    return true;

                case UpgradeId.MultiplierZone:
                    grid.ApplyMultiplierCellsRandom(MultiplierZoneCount, _rng);
                    return true;

                default:
                    return false;
            }
        }
    }
}
