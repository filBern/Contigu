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
        /// <summary>How many free/unmodified cells a Golden-cells upgrade adds.</summary>
        public const int GoldenCellsCount = 3;

        /// <summary>How many free/unmodified cells a Tinted-cells upgrade adds.</summary>
        public const int TintedCellsCount = 2;

        /// <summary>How many free/unmodified cells a Multiplier-zone upgrade adds.</summary>
        public const int MultiplierZoneCount = 3;

        private readonly IRandomProvider _rng;

        public UpgradeSystem(IRandomProvider rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Rolls a round-end draft: 3 distinct Bank (tile) options and 3 Grid
        /// options (the Grid pool only has 3 entries total, so all of them are
        /// always offered, in a shuffled order) — the player picks one of each.
        /// </summary>
        public UpgradeDraft RollDraft()
        {
            var tileOptions = PickDistinct(UpgradeCatalog.BankPool, 3);
            var gridOptions = PickDistinct(UpgradeCatalog.GridPool, 3);
            return new UpgradeDraft(tileOptions, gridOptions);
        }

        private UpgradeDefinition[] PickDistinct(UpgradeDefinition[] pool, int count)
        {
            var remaining = new List<UpgradeDefinition>(pool);
            int take = count < remaining.Count ? count : remaining.Count;
            var result = new UpgradeDefinition[take];
            for (int i = 0; i < take; i++)
            {
                int idx = _rng.Next(remaining.Count);
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
