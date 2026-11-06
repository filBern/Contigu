using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Resolves upgrades for the Lueur shop (see RunManager.ShopUpgradeSlots)
    /// and applies a chosen one's effect.
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
            IReadOnlyList<UpgradeDefinition> options;
            switch (pool)
            {
                case UpgradePool.Bank:
                    options = UpgradeCatalog.BankPool;
                    break;
                case UpgradePool.Mastery:
                    options = UpgradeCatalog.MasteryPool;
                    break;
                case UpgradePool.Modifier:
                    options = UpgradeCatalog.ModifierPool;
                    break;
                default:
                    options = UpgradeCatalog.GridPool;
                    break;
            }
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

        /// <summary>Draft weight for one upgrade — just its rarity's weight (see UpgradeRarityUtility.GetDraftWeight).</summary>
        private static int GetWeight(UpgradeDefinition upgrade)
        {
            return UpgradeRarityUtility.GetDraftWeight(upgrade.Rarity);
        }

        /// <summary>
        /// Applies a Bank-pool upgrade that needs a sub-choice (Replace/
        /// Dupliquer/Recolorer) directly to the deck. Joker — the one Bank
        /// upgrade with no sub-choice — goes through <see cref="ApplyJoker"/>
        /// instead, not here (see RunManager.BuyUpgradeSlot). Grid-pool
        /// (tile-trait) upgrades never go through here either — the shop
        /// always resolves them via <see cref="ApplyToChosenTiles"/> instead,
        /// since the player picks which deck tokens receive the trait rather
        /// than it being assigned at random. Returns false if the sub-choice
        /// couldn't be resolved (e.g. either of Replace's two chosen types
        /// no longer exists) or if <paramref name="upgrade"/> isn't one of
        /// the three above.
        /// </summary>
        public bool Apply(UpgradeDefinition upgrade, UpgradeSubChoice subChoice, DeckManager deck)
        {
            switch (upgrade.Id)
            {
                case UpgradeId.ReplacePiece:
                    return deck.ReplaceOneOfType(subChoice.Shape, subChoice.Color, subChoice.AddShape, subChoice.AddColor);

                case UpgradeId.DuplicatePiece:
                    return deck.DuplicateOfType(subChoice.Shape, subChoice.Color);

                case UpgradeId.RecolorPiece:
                    return deck.RecolorOneOfType(subChoice.Shape, subChoice.Color, subChoice.TargetColor);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Applies Joker — returns the shape actually rolled (see DeckManager.AddJoker) so RunManager can surface it as LastJokerShapeAdded,
        /// letting the reveal (UpgradeRevealView) show the real piece that got added instead of just naming the upgrade.
        /// </summary>
        public ShapeId ApplyJoker(DeckManager deck)
        {
            return deck.AddJoker(_rng);
        }

        /// <summary>
        /// The candidate pieces to show for "Random Piece"'s sub-choice —
        /// empty for anything else. Unlike every other Bank sub-choice
        /// (which offers TYPES already in the deck, see
        /// GetCandidateTypesFor), these are freshly rolled from the full
        /// shape/color space (same pool InitialDeckFactory seeds a new run
        /// from) and don't exist in the deck yet, so there's no "candidate
        /// deck index" to hand back — the full PieceToken is the candidate
        /// itself, trait included. Each candidate independently has a
        /// EconomyConstants.RandomPieceTraitChancePercent chance to already
        /// carry a Grid-pool tile trait, rolled the exact same rarity-
        /// weighted way a real Grid-pool upgrade slot would (<see
        /// cref="RollFromPool"/>) so a rarer trait (e.g. Seeder) stays
        /// proportionally rarer here too, with one random valid local cell
        /// index for the rolled shape (same convention as
        /// DeckManager.TagRandomTokens/TagSpecificTokens).
        /// </summary>
        public IReadOnlyList<PieceToken> GetCandidatePiecesFor(UpgradeDefinition upgrade)
        {
            if (upgrade.Id != UpgradeId.RandomPiece && upgrade.Id != UpgradeId.SeedPack && upgrade.Id != UpgradeId.WildPack)
            {
                return System.Array.Empty<PieceToken>();
            }
            if (upgrade.Id == UpgradeId.WildPack)
            {
                return RollWildSeeds(EconomyConstants.PackChoiceCount);
            }

            var candidates = new PieceToken[upgrade.Id == UpgradeId.SeedPack ? EconomyConstants.PackChoiceCount : EconomyConstants.ShopTileCandidateCount];
            for (int i = 0; i < candidates.Length; i++)
            {
                if (upgrade.Id == UpgradeId.SeedPack && _rng.Next(100) < EconomyConstants.SeedPackWildSeedChancePercent)
                {
                    candidates[i] = RollWildSeed();
                    continue;
                }
                var shape = InitialDeckFactory.ShapeOrder[_rng.Next(InitialDeckFactory.ShapeOrder.Length)];
                var color = PieceColorUtility.BaseColors[_rng.Next(PieceColorUtility.BaseColors.Count)];

                PieceToken token = new PieceToken(shape, color);
                if (_rng.Next(100) < EconomyConstants.RandomPieceTraitChancePercent)
                {
                    // Most candidates carry one upgrade; a small chance adds one more on another tile (and, rarer still, another one after it).
                    int cellCount = PieceShapeCatalog.Get(shape).Cells.Count;
                    while (token.TraitCount < cellCount)
                    {
                        var rolled = RollCandidateTrait(color, token, cellCount);
                        if (!rolled.HasValue)
                        {
                            break;
                        }
                        token = token.WithAddedTrait(rolled.Value);
                        if (_rng.Next(100) >= EconomyConstants.ExtraUpgradeChancePercent)
                        {
                            break;
                        }
                    }
                }
                candidates[i] = token;
            }
            return candidates;
        }

        /// <summary>A Wild Seed: a Joker of a random shape carrying one regular upgrade, rolled like a Seed Pack seed's (rarity-weighted, never one a Joker cannot carry).</summary>
        private PieceToken RollWildSeed()
        {
            var shape = InitialDeckFactory.ShapeOrder[_rng.Next(InitialDeckFactory.ShapeOrder.Length)];
            var plain = new PieceToken(shape, PieceColor.Joker);
            int cellCount = PieceShapeCatalog.Get(shape).Cells.Count;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                var rolled = RollCandidateTrait(PieceColor.Joker, plain, cellCount);
                if (rolled.HasValue && !PieceTrait.IsIncompatibleWithJoker(rolled.Value.Kind))
                {
                    return plain.WithAddedTrait(rolled.Value);
                }
            }
            return plain;
        }

        /// <summary>The Wild Pack's seeds: Wild Seeds that differ from each other by shape or upgrade.</summary>
        private PieceToken[] RollWildSeeds(int count)
        {
            var seeds = new PieceToken[count];
            for (int i = 0; i < count; i++)
            {
                PieceToken seed = RollWildSeed();
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    bool duplicate = false;
                    for (int j = 0; j < i; j++)
                    {
                        if (seeds[j].Equals(seed)) { duplicate = true; }
                    }
                    if (!duplicate) { break; }
                    seed = RollWildSeed();
                }
                seeds[i] = seed;
            }
            return seeds;
        }

        /// <summary>
        /// One rolled Grid-pool tile trait for a candidate piece, on a tile of <paramref name="token"/> that carries none yet. A small number of
        /// Grid-pool entries (currently just PermanentBonusSquare) have no PieceTraitKind at all — a board-position upgrade, not a piece
        /// enchantment — so it re-rolls rather than crash on a null kind (bounded, so a hypothetical pool of mostly such entries stays safe).
        /// </summary>
        private PieceTrait? RollCandidateTrait(PieceColor color, PieceToken token, int cellCount)
        {
            PieceTraitKind? kind = null;
            for (int attempt = 0; attempt < 50 && !kind.HasValue; attempt++)
            {
                kind = TraitKindFor(RollFromPool(UpgradePool.Grid).Id);
            }
            if (!kind.HasValue)
            {
                return null;
            }
            var free = new List<int>();
            for (int c = 0; c < cellCount; c++)
            {
                if (!token.UsesCell(c))
                {
                    free.Add(c);
                }
            }
            if (free.Count == 0)
            {
                return null;
            }
            int localIndex = free[_rng.Next(free.Count)];
            return new PieceTrait(kind.Value, localIndex, PieceTrait.TargetColorFor(kind.Value, color));
        }

        /// <summary>Adds the candidate the player picked (see GetCandidatePiecesFor) straight to the deck, trait included — the counterpart to ApplyToChosenTiles for a Bank-pool sub-choice whose "choice" is a whole token rather than an index into the existing deck.</summary>
        public void ApplyChosenPiece(PieceToken chosen, DeckManager deck)
        {
            deck.AddPreparedToken(chosen);
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
        public IReadOnlyList<int> GetCandidateTilesFor(UpgradeDefinition upgrade, DeckManager deck, int count = EconomyConstants.ForgeSeedCandidateCount)
        {
            var kind = TraitKindFor(upgrade.Id);
            if (!kind.HasValue)
            {
                return System.Array.Empty<int>();
            }
            // An upgrade that needs a real color or recolors the seed for good is never offered for a Joker.
            bool jokerCannot = PieceTrait.IsIncompatibleWithJoker(kind.Value);
            System.Func<PieceToken, bool> eligible = token => !jokerCannot || token.Color != PieceColor.Joker;
            return deck.GetCandidateTokenIndices(count, _rng, eligible);
        }

        /// <summary>
        /// The candidate piece TYPES to show for <paramref name="upgrade"/>'s
        /// FIRST sub-choice step (Replace/Dupliquer/Recolorer — which type
        /// to act on) — empty for anything else (Grid-pool, or Bank with no
        /// sub-choice like Joker). No eligibility filter needed any more:
        /// unlike the old "Remove a piece" (which had to stay above
        /// DeckManager.MinDeckSize, since it genuinely shrank the deck),
        /// Replace immediately adds a duplicate of another type right back
        /// (see GetReplacementCandidateTypesFor/DeckManager.
        /// ReplaceOneOfType), so the net count never changes and any type
        /// currently in the deck is always a valid pick.
        /// </summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> GetCandidateTypesFor(UpgradeDefinition upgrade, DeckManager deck)
        {
            if (upgrade.Pool != UpgradePool.Bank || !upgrade.RequiresSubChoice)
            {
                return System.Array.Empty<(ShapeId, PieceColor)>();
            }
            return deck.GetCandidateTypes(EconomyConstants.ShopTileCandidateCount, _rng);
        }

        /// <summary>
        /// "Replace a piece"'s SECOND sub-choice step: which existing deck
        /// type to duplicate in place of <paramref name="excludeShape"/>/
        /// <paramref name="excludeColor"/> (the type just picked to go away
        /// — see GetCandidateTypesFor for the first step). Excluded from
        /// its own candidate list so the player isn't offered the pointless
        /// no-op of "replacing" a type with itself; falls back to including
        /// it anyway if that exclusion would leave zero candidates (a
        /// deck that's down to a single type), so the picker never comes up
        /// empty.
        /// </summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> GetReplacementCandidateTypesFor(DeckManager deck, ShapeId excludeShape, PieceColor excludeColor)
        {
            System.Func<(ShapeId Shape, PieceColor Color), bool> excludeChosen =
                t => t.Shape != excludeShape || t.Color != excludeColor;
            var candidates = deck.GetCandidateTypes(EconomyConstants.ShopTileCandidateCount, _rng, excludeChosen);
            if (candidates.Count > 0)
            {
                return candidates;
            }
            return deck.GetCandidateTypes(EconomyConstants.ShopTileCandidateCount, _rng);
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
                case UpgradeId.VenomTile: return PieceTraitKind.Venom;
                case UpgradeId.PipTile: return PieceTraitKind.Pip;
                case UpgradeId.LuckyTile: return PieceTraitKind.Lucky;
                case UpgradeId.CompanionTile: return PieceTraitKind.Companion;
                case UpgradeId.ColonyTile: return PieceTraitKind.Colony;
                case UpgradeId.BorderTile: return PieceTraitKind.Border;
                case UpgradeId.HeartTile: return PieceTraitKind.Heart;
                case UpgradeId.CornerTile: return PieceTraitKind.Corner;
                case UpgradeId.HermitTile: return PieceTraitKind.Hermit;
                case UpgradeId.CuddlyTile: return PieceTraitKind.Cuddly;
                case UpgradeId.DawnTile: return PieceTraitKind.Dawn;
                case UpgradeId.DuskTile: return PieceTraitKind.Dusk;
                case UpgradeId.MiserTile: return PieceTraitKind.Miser;
                case UpgradeId.StormyTile: return PieceTraitKind.Stormy;
                case UpgradeId.ElderTile: return PieceTraitKind.Elder;
                case UpgradeId.HarvesterTile: return PieceTraitKind.Harvester;
                case UpgradeId.BreezeTile: return PieceTraitKind.Breeze;
                case UpgradeId.HarmonyTile: return PieceTraitKind.Harmony;
                case UpgradeId.PrismTile: return PieceTraitKind.Prism;
                case UpgradeId.GardenerTile: return PieceTraitKind.Gardener;
                case UpgradeId.ClusterTile: return PieceTraitKind.Cluster;
                case UpgradeId.CrossfireTile: return PieceTraitKind.Crossfire;
                case UpgradeId.EvenTile: return PieceTraitKind.Even;
                case UpgradeId.RainbowRowTile: return PieceTraitKind.RainbowRow;
                case UpgradeId.SunseekerTile: return PieceTraitKind.Sunseeker;
                case UpgradeId.HunterTile: return PieceTraitKind.Hunter;
                case UpgradeId.ExecutionerTile: return PieceTraitKind.Executioner;
                case UpgradeId.SpilloverTile: return PieceTraitKind.Spillover;
                case UpgradeId.HoneycombTile: return PieceTraitKind.Honeycomb;
                case UpgradeId.DewdropTile: return PieceTraitKind.Dewdrop;
                case UpgradeId.KeystoneTile: return PieceTraitKind.Keystone;
                case UpgradeId.GrowthTile: return PieceTraitKind.Growth;
                case UpgradeId.GamblerTile: return PieceTraitKind.Gambler;
                case UpgradeId.WardenTile: return PieceTraitKind.Warden;
                case UpgradeId.AegisTile: return PieceTraitKind.Aegis;
                case UpgradeId.SowerTile: return PieceTraitKind.Sower;
                case UpgradeId.KinTile: return PieceTraitKind.Kin;
                case UpgradeId.AmplifierTile: return PieceTraitKind.Amplifier;
                case UpgradeId.EchoTile: return PieceTraitKind.Echo;
                case UpgradeId.CollectorTile: return PieceTraitKind.Collector;
                case UpgradeId.ResonanceTile: return PieceTraitKind.Resonance;
                case UpgradeId.PouchTile: return PieceTraitKind.Pouch;
                case UpgradeId.PerennialTile: return PieceTraitKind.Perennial;
                case UpgradeId.CrescendoTile: return PieceTraitKind.Crescendo;
                case UpgradeId.ScholarTile: return PieceTraitKind.Scholar;
                case UpgradeId.FullHouseTile: return PieceTraitKind.FullHouse;
                case UpgradeId.CompoundTile: return PieceTraitKind.Compound;
                case UpgradeId.GapTile: return PieceTraitKind.Gap;
                case UpgradeId.WildrootTile: return PieceTraitKind.Wildroot;
                case UpgradeId.DyeTile: return PieceTraitKind.Dye;
                case UpgradeId.FertileTile: return PieceTraitKind.Fertile;
                case UpgradeId.EvergreenTile: return PieceTraitKind.Evergreen;
                case UpgradeId.PrunerTile: return PieceTraitKind.Pruner;
                case UpgradeId.GlowTile: return PieceTraitKind.Glow;
                case UpgradeId.BridgeTile: return PieceTraitKind.Bridge;
                case UpgradeId.SeedpodTile: return PieceTraitKind.Seedpod;
                case UpgradeId.LocksmithTile: return PieceTraitKind.Locksmith;
                case UpgradeId.MimicTile: return PieceTraitKind.Mimic;
                case UpgradeId.DominantTile: return PieceTraitKind.Dominant;
                case UpgradeId.HeritageTile: return PieceTraitKind.Heritage;
                case UpgradeId.ScytheTile: return PieceTraitKind.Scythe;
                case UpgradeId.TallyTile: return PieceTraitKind.Tally;
                case UpgradeId.SunbeamTile: return PieceTraitKind.Sunbeam;
                case UpgradeId.AntidoteTile: return PieceTraitKind.Antidote;
                case UpgradeId.LullTile: return PieceTraitKind.Lull;
                case UpgradeId.DazzleTile: return PieceTraitKind.Dazzle;
                case UpgradeId.WiltTile: return PieceTraitKind.Wilt;
                case UpgradeId.CleanseTile: return PieceTraitKind.Cleanse;
                default: return null; // Bank pool — no trait
            }
        }
    }
}
