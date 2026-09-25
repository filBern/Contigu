using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Resolves upgrades for the Lueur shop (see RunManager.ShopUpgradeSlots)
    /// and applies a chosen one's effect. The old round-end draft (RollDraft,
    /// a guaranteed-Bank/guaranteed-Grid/wildcard pick of 3) is gone — every
    /// upgrade now comes from a purchased shop slot instead (spec extension,
    /// explicit request: "je ne veux plus du tout du système actuel").
    /// </summary>
    public sealed class UpgradeSystem
    {
        private readonly IRandomProvider _rng;

        public UpgradeSystem(IRandomProvider rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Rolls one specific upgrade from <paramref name="pool"/>, weighted
        /// by rarity (see UpgradeRarityUtility.GetDraftWeight) — used to
        /// decide what a shop Upgrade slot actually grants the moment it's
        /// rolled (see RunManager.RollUpgradeSlot), independent of whether
        /// the player ever sees which one it is before buying.
        /// </summary>
        public UpgradeDefinition RollFromPool(UpgradePool pool)
        {
            var options = pool == UpgradePool.Bank ? UpgradeCatalog.BankPool : UpgradeCatalog.GridPool;
            return PickWeighted(options, _rng);
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
                totalWeight += GetWeight(pool[i]);
            }

            int roll = rng.Next(totalWeight);
            int cumulative = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += GetWeight(pool[i]);
                if (roll < cumulative)
                {
                    return pool[i];
                }
            }
            return pool[pool.Count - 1];
        }

        /// <summary>
        /// Draft weight for one upgrade — its rarity's weight (see
        /// UpgradeRarityUtility.GetDraftWeight), except Random Modifier,
        /// which gets a small permanent bump on top of its own Common
        /// weight (explicit request: "On peut augmenter un peu les chances
        /// d'avoir un random modifier"). At 12 (vs. Common's own 8, and
        /// 2/4/8 for the rest of the Bank pool), it goes from ~13.3% to
        /// ~17.6% of any upgrade-slot roll — a modest push past Duplicate/
        /// Joker rather than the earlier x12.5 boost to 100 used purely to
        /// validate the reveal pipeline, which has since been reverted.
        /// </summary>
        private static int GetWeight(UpgradeDefinition upgrade)
        {
            if (upgrade.Id == UpgradeId.RandomModifier)
            {
                return 12;
            }
            return UpgradeRarityUtility.GetDraftWeight(upgrade.Rarity);
        }

        /// <summary>
        /// Applies a Bank-pool upgrade that needs a sub-choice (Retirer/
        /// Dupliquer/Recolorer) directly to the deck. Joker — the one Bank
        /// upgrade with no sub-choice — goes through <see cref="ApplyJoker"/>
        /// instead, not here (see RunManager.BuyUpgradeSlot). Grid-pool
        /// (tile-trait) upgrades never go through here either — the shop
        /// always resolves them via <see cref="ApplyToChosenTiles"/> instead,
        /// since the player picks which deck tokens receive the trait rather
        /// than it being assigned at random (spec: "un choix de 5 tiles").
        /// Returns false if the sub-choice couldn't be resolved (e.g.
        /// removing the last copy of a type while the deck is at its floor)
        /// or if <paramref name="upgrade"/> isn't one of the three above.
        /// </summary>
        public bool Apply(UpgradeDefinition upgrade, UpgradeSubChoice subChoice, DeckManager deck)
        {
            switch (upgrade.Id)
            {
                case UpgradeId.RemovePiece:
                    return deck.RemoveOneOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.DuplicatePiece:
                    return deck.DuplicateOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.RecolorPiece:
                    return deck.RecolorOneOfType(subChoice.Shape, subChoice.Color, subChoice.TargetColor);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Applies Joker — returns the shape actually rolled (see
        /// DeckManager.AddJoker) so RunManager can surface it as
        /// LastJokerShapeAdded, letting the reveal (UpgradeRevealView) show
        /// the real piece that got added instead of just naming the
        /// upgrade.
        /// </summary>
        public ShapeId ApplyJoker(DeckManager deck)
        {
            return deck.AddJoker(_rng);
        }

        /// <summary>
        /// Tags EXACTLY <paramref name="deckIndices"/> (the tokens the player
        /// picked from the candidates <see cref="GetCandidateTilesFor"/>
        /// offered) with the <see cref="PieceTrait"/> that <paramref
        /// name="upgrade"/> grants. False (no-op) for a Bank-pool upgrade,
        /// which has no trait to apply this way at all.
        /// </summary>
        public bool ApplyToChosenTiles(UpgradeDefinition upgrade, IReadOnlyList<int> deckIndices, DeckManager deck)
        {
            var kind = TraitKindFor(upgrade.Id);
            if (!kind.HasValue)
            {
                return false;
            }
            deck.TagSpecificTokens(deckIndices, kind.Value, _rng);
            return true;
        }

        /// <summary>
        /// The candidate deck tokens to show the player for <paramref
        /// name="upgrade"/> (see <see cref="DeckManager.GetCandidateTokenIndices"/>)
        /// — empty for a Bank-pool upgrade, which never needs a tile choice.
        /// Tinted excludes Joker-colored tokens from candidacy, same as the
        /// old random tagging did (a Joker cell's stored color never resolves
        /// to a real one, so Tinted could never fire on it either way).
        /// </summary>
        public IReadOnlyList<int> GetCandidateTilesFor(UpgradeDefinition upgrade, DeckManager deck)
        {
            var kind = TraitKindFor(upgrade.Id);
            if (!kind.HasValue)
            {
                return System.Array.Empty<int>();
            }
            System.Func<PieceToken, bool> eligible = kind.Value == PieceTraitKind.Tinted
                ? (System.Func<PieceToken, bool>)(token => token.Color != PieceColor.Joker)
                : null;
            return deck.GetCandidateTokenIndices(EconomyConstants.ShopTileCandidateCount, _rng, eligible);
        }

        /// <summary>
        /// The candidate piece TYPES to show for <paramref name="upgrade"/>'s
        /// sub-choice (Retirer/Dupliquer/Recolorer) — empty for anything else
        /// (Grid-pool, or Bank with no sub-choice like Joker). Retirer
        /// additionally filters to types the deck can actually still remove
        /// (see DeckManager.CanRemove), same floor Apply itself enforces.
        /// </summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> GetCandidateTypesFor(UpgradeDefinition upgrade, DeckManager deck)
        {
            if (upgrade.Pool != UpgradePool.Bank || !upgrade.RequiresSubChoice)
            {
                return System.Array.Empty<(ShapeId, PieceColor)>();
            }
            System.Func<(ShapeId Shape, PieceColor Color), bool> eligible = upgrade.Id == UpgradeId.RemovePiece
                ? (System.Func<(ShapeId Shape, PieceColor Color), bool>)(t => deck.CanRemove(t.Shape, t.Color))
                : null;
            return deck.GetCandidateTypes(EconomyConstants.ShopTileCandidateCount, _rng, eligible);
        }

        /// <summary>Public so Presentation can preview a Grid upgrade's trait on a candidate piece before it's actually applied (see TileChoiceView) — everything else about resolving/applying an upgrade still goes through an UpgradeSystem instance.</summary>
        public static PieceTraitKind? TraitKindFor(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.GoldenCells: return PieceTraitKind.Golden;
                case UpgradeId.TintedCells: return PieceTraitKind.Tinted;
                case UpgradeId.MultiplierZone: return PieceTraitKind.Multiplier;
                case UpgradeId.BlastTile: return PieceTraitKind.Blast;
                case UpgradeId.MultiplierBeacon: return PieceTraitKind.Beacon;
                case UpgradeId.MirrorTile: return PieceTraitKind.Mirror;
                case UpgradeId.Seeder: return PieceTraitKind.Seeder;
                case UpgradeId.CatalystTile: return PieceTraitKind.Catalyst;
                case UpgradeId.TwinTile: return PieceTraitKind.Twin;
                case UpgradeId.DetonatorTile: return PieceTraitKind.Detonator;
                case UpgradeId.ChameleonTile: return PieceTraitKind.Chameleon;
                case UpgradeId.SparkTile: return PieceTraitKind.Spark;
                case UpgradeId.VoidTile: return PieceTraitKind.Void;
                case UpgradeId.BastionTile: return PieceTraitKind.Bastion;
                case UpgradeId.KamikazeTile: return PieceTraitKind.Kamikaze;
                default: return null; // Bank pool — no trait
            }
        }
    }
}
