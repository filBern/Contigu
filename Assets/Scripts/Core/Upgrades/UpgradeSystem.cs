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
        /// <summary>How many distinct deck tokens a Golden-cells upgrade enchants (spec 5.4 redesign — each enchantment is one-shot, fired only when that specific token is placed, so this is no longer the permanent-grid-cell count it used to be).</summary>
        public const int GoldenCellsCount = 3;

        /// <summary>How many distinct deck tokens a Tinted-cells upgrade enchants. See GoldenCellsCount.</summary>
        public const int TintedCellsCount = 3;

        /// <summary>How many distinct deck tokens a Multiplier-zone upgrade enchants. See GoldenCellsCount.</summary>
        public const int MultiplierZoneCount = 3;

        /// <summary>How many distinct deck tokens a Blast-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int BlastTileCount = 3;

        /// <summary>How many distinct deck tokens a Multiplier-beacon upgrade enchants. See GoldenCellsCount.</summary>
        public const int MultiplierBeaconCount = 3;

        /// <summary>How many distinct deck tokens a Mirror-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int MirrorTileCount = 3;

        /// <summary>How many distinct deck tokens a Seeder upgrade enchants. See GoldenCellsCount.</summary>
        public const int SeederCount = 3;

        /// <summary>How many distinct deck tokens a Catalyst-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int CatalystTileCount = 3;

        /// <summary>How many distinct deck tokens a Twin-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int TwinTileCount = 3;

        /// <summary>How many distinct deck tokens a Detonator-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int DetonatorTileCount = 3;

        /// <summary>How many distinct deck tokens a Chameleon-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int ChameleonTileCount = 3;

        /// <summary>How many distinct deck tokens a Spark-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int SparkTileCount = 3;

        /// <summary>How many distinct deck tokens a Void-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int VoidTileCount = 3;

        /// <summary>How many distinct deck tokens a Bastion-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int BastionTileCount = 3;

        /// <summary>How many distinct deck tokens a Kamikaze-tile upgrade enchants. See GoldenCellsCount.</summary>
        public const int KamikazeTileCount = 3;

        private readonly IRandomProvider _rng;

        public UpgradeSystem(IRandomProvider rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Rolls a round-end draft of exactly 3 options: one guaranteed Bank
        /// pick, one guaranteed Grid pick, one random pick from either pool
        /// (Bank and Grid mixed together) — the player picks exactly one. Each
        /// pick is weighted by rarity (see PickWeighted), not uniform.
        /// </summary>
        public UpgradeDraft RollDraft()
        {
            var bankPick = PickWeighted(UpgradeCatalog.BankPool, _rng);
            var gridPick = PickWeighted(UpgradeCatalog.GridPool, _rng);

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

            var thirdPick = PickWeighted(remainingPool, _rng);

            return new UpgradeDraft(new[] { bankPick, gridPick, thirdPick });
        }

        /// <summary>
        /// Weighted random pick from <paramref name="pool"/> — each entry's
        /// odds are proportional to its rarity's draft weight (see
        /// UpgradeRarityUtility.GetDraftWeight), so Common upgrades come up
        /// more often than Rare ones. Falls back to the last entry if
        /// floating-point/rounding somehow leaves the roll unmatched (should
        /// never happen with integer weights, kept defensive).
        /// </summary>
        private static UpgradeDefinition PickWeighted(IReadOnlyList<UpgradeDefinition> pool, IRandomProvider rng)
        {
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += UpgradeRarityUtility.GetDraftWeight(pool[i].Rarity);
            }

            int roll = rng.Next(totalWeight);
            int cumulative = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += UpgradeRarityUtility.GetDraftWeight(pool[i].Rarity);
                if (roll < cumulative)
                {
                    return pool[i];
                }
            }
            return pool[pool.Count - 1];
        }

        /// <summary>Picks up to <paramref name="count"/> distinct entries at random from <paramref name="pool"/>, without replacement (uniform — used for modifiers, which don't carry a rarity).</summary>
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
        public bool Apply(UpgradeDefinition upgrade, UpgradeSubChoice subChoice, DeckManager deck)
        {
            switch (upgrade.Id)
            {
                case UpgradeId.RemovePiece:
                    return deck.RemoveOneOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.DuplicatePiece:
                    return deck.DuplicateOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.JokerPiece:
                    deck.AddJoker(_rng);
                    return true;

                case UpgradeId.RecolorPiece:
                    return deck.RecolorOneOfType(subChoice.Shape, subChoice.Color, subChoice.TargetColor);

                case UpgradeId.GoldenCells:
                    deck.TagGoldenTokensRandom(GoldenCellsCount, _rng);
                    return true;

                case UpgradeId.TintedCells:
                    deck.TagTintedTokensRandom(TintedCellsCount, _rng);
                    return true;

                case UpgradeId.MultiplierZone:
                    deck.TagMultiplierTokensRandom(MultiplierZoneCount, _rng);
                    return true;

                case UpgradeId.BlastTile:
                    deck.TagBlastTokensRandom(BlastTileCount, _rng);
                    return true;

                case UpgradeId.MultiplierBeacon:
                    deck.TagBeaconTokensRandom(MultiplierBeaconCount, _rng);
                    return true;

                case UpgradeId.MirrorTile:
                    deck.TagMirrorTokensRandom(MirrorTileCount, _rng);
                    return true;

                case UpgradeId.Seeder:
                    deck.TagSeederTokensRandom(SeederCount, _rng);
                    return true;

                case UpgradeId.CatalystTile:
                    deck.TagCatalystTokensRandom(CatalystTileCount, _rng);
                    return true;

                case UpgradeId.TwinTile:
                    deck.TagTwinTokensRandom(TwinTileCount, _rng);
                    return true;

                case UpgradeId.DetonatorTile:
                    deck.TagDetonatorTokensRandom(DetonatorTileCount, _rng);
                    return true;

                case UpgradeId.ChameleonTile:
                    deck.TagChameleonTokensRandom(ChameleonTileCount, _rng);
                    return true;

                case UpgradeId.SparkTile:
                    deck.TagSparkTokensRandom(SparkTileCount, _rng);
                    return true;

                case UpgradeId.VoidTile:
                    deck.TagVoidTokensRandom(VoidTileCount, _rng);
                    return true;

                case UpgradeId.BastionTile:
                    deck.TagBastionTokensRandom(BastionTileCount, _rng);
                    return true;

                case UpgradeId.KamikazeTile:
                    deck.TagKamikazeTokensRandom(KamikazeTileCount, _rng);
                    return true;

                default:
                    return false;
            }
        }
    }
}
