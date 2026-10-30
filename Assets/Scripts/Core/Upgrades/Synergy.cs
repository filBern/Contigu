using System;
using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>The themes a fertilizer or a tile upgrade can belong to. A tile upgrade "fits" a held fertilizer when they share at least one.</summary>
    [Flags]
    public enum SynergyTag
    {
        None = 0,
        Colors = 1 << 0,
        Neighbors = 1 << 1,
        Lines = 1 << 2,
        Shapes = 1 << 3,
        Upgrades = 1 << 4,
        Economy = 1 << 5,
        Board = 1 << 6,
        Combat = 1 << 7,
        Tempo = 1 << 8
    }

    /// <summary>
    /// Which tile upgrades go well with which fertilizers: used to tilt the shop's tile-upgrade offers toward the player's build and to mark them
    /// ("fits your ..."). A fertilizer's tags follow its category; a tile upgrade's tags are listed below.
    /// </summary>
    public static class Synergy
    {
        /// <summary>How much more likely an offer becomes for each held fertilizer it fits (capped at <see cref="MaxFittingFertilizers"/>).</summary>
        public const float WeightBonusPerFittingFertilizer = 0.75f;
        public const int MaxFittingFertilizers = 3;

        public static SynergyTag TagsOf(ModifierId id)
        {
            var definition = ModifierCatalog.Get(id);
            if (definition == null)
            {
                return SynergyTag.None;
            }
            switch (definition.Category)
            {
                case ModifierCategory.Couleurs: return SynergyTag.Colors;
                case ModifierCategory.Voisinage: return SynergyTag.Neighbors;
                case ModifierCategory.Connexions: return SynergyTag.Lines;
                case ModifierCategory.Formes: return SynergyTag.Shapes;
                case ModifierCategory.Destruction: return SynergyTag.Combat | SynergyTag.Lines;
                default: return SynergyTag.Upgrades | SynergyTag.Economy;
            }
        }

        public static SynergyTag TagsOf(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.CompanionTile: case UpgradeId.HarmonyTile: case UpgradeId.PrismTile: case UpgradeId.RainbowRowTile:
                case UpgradeId.DyeTile: case UpgradeId.ChameleonTile: case UpgradeId.PrunerTile: case UpgradeId.TintedCells: case UpgradeId.SowerTile:
                    return SynergyTag.Colors;
                case UpgradeId.SeedpodTile:
                    return SynergyTag.Colors | SynergyTag.Lines;
                case UpgradeId.BlastTile: case UpgradeId.CuddlyTile: case UpgradeId.HermitTile: case UpgradeId.WardenTile: case UpgradeId.AegisTile:
                    return SynergyTag.Neighbors;
                case UpgradeId.KamikazeTile:
                    return SynergyTag.Neighbors | SynergyTag.Combat;
                case UpgradeId.KinTile: case UpgradeId.AmplifierTile: case UpgradeId.EchoTile: case UpgradeId.ClusterTile:
                    return SynergyTag.Neighbors | SynergyTag.Upgrades;
                case UpgradeId.HarvesterTile: case UpgradeId.DetonatorTile: case UpgradeId.FullHouseTile: case UpgradeId.GapTile: case UpgradeId.BridgeTile:
                case UpgradeId.SpilloverTile: case UpgradeId.EvergreenTile: case UpgradeId.BastionTile: case UpgradeId.CrossfireTile: case UpgradeId.MultiplierBeacon:
                    return SynergyTag.Lines;
                case UpgradeId.DewdropTile:
                    return SynergyTag.Lines | SynergyTag.Economy;
                case UpgradeId.ColonyTile: case UpgradeId.EvenTile: case UpgradeId.WildrootTile: case UpgradeId.MirrorTile: case UpgradeId.TwinTile: case UpgradeId.CatalystTile:
                    return SynergyTag.Shapes;
                case UpgradeId.HoneycombTile: case UpgradeId.MiserTile: case UpgradeId.CompoundTile: case UpgradeId.BreezeTile:
                    return SynergyTag.Economy;
                case UpgradeId.GardenerTile:
                    return SynergyTag.Upgrades | SynergyTag.Economy;
                case UpgradeId.CollectorTile: case UpgradeId.ResonanceTile: case UpgradeId.PouchTile: case UpgradeId.PerennialTile:
                    return SynergyTag.Upgrades;
                case UpgradeId.BorderTile: case UpgradeId.HeartTile: case UpgradeId.CornerTile: case UpgradeId.KeystoneTile: case UpgradeId.GoldenCells:
                case UpgradeId.MultiplierZone: case UpgradeId.Seeder: case UpgradeId.GlowTile: case UpgradeId.FertileTile: case UpgradeId.LocksmithTile:
                    return SynergyTag.Board;
                case UpgradeId.VenomTile: case UpgradeId.HunterTile: case UpgradeId.ExecutionerTile: case UpgradeId.VoidTile:
                    return SynergyTag.Combat;
                case UpgradeId.DawnTile: case UpgradeId.DuskTile: case UpgradeId.ElderTile: case UpgradeId.GrowthTile: case UpgradeId.CrescendoTile:
                case UpgradeId.ScholarTile: case UpgradeId.SparkTile: case UpgradeId.StormyTile: case UpgradeId.SunseekerTile:
                    return SynergyTag.Tempo;
                default:
                    return SynergyTag.None;
            }
        }

        /// <summary>The held fertilizers that go with <paramref name="upgrade"/> (they share a theme with it), in the order they are held.</summary>
        public static List<ModifierId> FittingModifiers(UpgradeDefinition upgrade, IReadOnlyList<ModifierId> held)
        {
            var result = new List<ModifierId>();
            var tags = TagsOf(upgrade.Id);
            if (tags == SynergyTag.None || held == null)
            {
                return result;
            }
            for (int i = 0; i < held.Count; i++)
            {
                if ((TagsOf(held[i]) & tags) != SynergyTag.None)
                {
                    result.Add(held[i]);
                }
            }
            return result;
        }

        /// <summary>Roll weight multiplier of an offer: 1, plus <see cref="WeightBonusPerFittingFertilizer"/> per fitting held fertilizer (up to <see cref="MaxFittingFertilizers"/>).</summary>
        public static float WeightMultiplier(UpgradeDefinition upgrade, IReadOnlyList<ModifierId> held)
        {
            int fits = Math.Min(FittingModifiers(upgrade, held).Count, MaxFittingFertilizers);
            return 1f + WeightBonusPerFittingFertilizer * fits;
        }
    }
}
