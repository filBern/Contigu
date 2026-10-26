using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for a piece's enchanted-tile badge (see
    /// Presentation.HandView/GridCellView). Badges are too small to carry more
    /// than a color, so traits that behave similarly share a badge color
    /// (Golden/Blast/Seeder; Multiplier/Beacon) and the hover tooltip is what
    /// distinguishes them.
    /// </summary>
    public static class PieceTraitVisualDefaults
    {
        // Kept as its own literal set rather than referencing Presentation.UITheme —
        // Contigu.Data must not depend on Contigu.Presentation (see README).
        private static readonly Color GoldenBadgeColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d
        private static readonly Color MultiplierBadgeColor = new Color(0.396f, 0.682f, 0.839f); // #65aed6
        private static readonly Color TintedFallbackColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color MirrorBadgeColor = new Color(0.373f, 0.412f, 0.612f); // #5f699c
        private static readonly Color SeederBadgeColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
        private static readonly Color VoidBadgeColor = new Color(0.216f, 0.180f, 0.302f); // #372e4d
        private static readonly Color DetonatorBadgeColor = new Color(0.710f, 0.427f, 0.498f); // #b56d7f
        private static readonly Color ChameleonBadgeColor = new Color(0.643f, 0.922f, 0.800f); // #a4ebcc
        private static readonly Color BastionBadgeColor = new Color(0.380f, 0.263f, 0.388f); // #614363
        private static readonly Color KamikazeBadgeColor = new Color(0.710f, 0.427f, 0.498f); // #b56d7f (same family as Detonator/destruction)
        private static readonly Color VenomBadgeColor = new Color(0.373f, 0.624f, 0.373f); // #5f9f5f

        // The flat placement-point tile upgrades share one warm badge color; the glyph on the badge tells them apart.
        private static readonly Color PlacementPointBadgeColor = new Color(0.910f, 0.659f, 0.180f); // #e8a82e

        // Joker-exclusive combat traits all share Joker's own color
        // (VisualDefaults.ColorMap[PieceColor.Joker]); the hover tooltip
        // is what distinguishes them from each other.
        private static readonly Color JokerCombatBadgeColor = new Color(0.549f, 0.388f, 0.690f); // #8c63b0

        public static string GetName(PieceTraitKind kind)
        {
            switch (kind)
            {
                case PieceTraitKind.Golden: return "Golden Sprout";
                case PieceTraitKind.Tinted: return "Tinted Sprout";
                case PieceTraitKind.Multiplier: return "Multiplier Sprout";
                case PieceTraitKind.Blast: return "Blast Sprout";
                case PieceTraitKind.Beacon: return "Multiplier Beacon";
                case PieceTraitKind.Mirror: return "Mirror Sprout";
                case PieceTraitKind.Seeder: return "Seeder";
                case PieceTraitKind.Catalyst: return "Catalyst Sprout";
                case PieceTraitKind.Twin: return "Twin Sprout";
                case PieceTraitKind.Detonator: return "Detonator Sprout";
                case PieceTraitKind.Chameleon: return "Chameleon Sprout";
                case PieceTraitKind.Spark: return "Spark Sprout";
                case PieceTraitKind.Void: return "Void Sprout";
                case PieceTraitKind.Bastion: return "Bastion Sprout";
                case PieceTraitKind.Kamikaze: return "Kamikaze Sprout";
                case PieceTraitKind.Venom: return "Venom Sprout";
                case PieceTraitKind.Pip: return "Pip Sprout";
                case PieceTraitKind.Lucky: return "Lucky Sprout";
                case PieceTraitKind.Companion: return "Companion Sprout";
                case PieceTraitKind.Colony: return "Colony Sprout";
                case PieceTraitKind.Border: return "Border Sprout";
                case PieceTraitKind.Heart: return "Heart Sprout";
                case PieceTraitKind.Corner: return "Corner Sprout";
                case PieceTraitKind.Hermit: return "Hermit Sprout";
                case PieceTraitKind.Cuddly: return "Cuddly Sprout";
                case PieceTraitKind.Dawn: return "Dawn Sprout";
                case PieceTraitKind.Dusk: return "Dusk Sprout";
                case PieceTraitKind.Miser: return "Miser Sprout";
                case PieceTraitKind.Stormy: return "Stormy Sprout";
                case PieceTraitKind.Elder: return "Elder Sprout";
                case PieceTraitKind.Harvester: return "Harvester Sprout";
                case PieceTraitKind.Breeze: return "Breeze Sprout";
                case PieceTraitKind.Harmony: return "Harmony Sprout";
                case PieceTraitKind.Prism: return "Prism Sprout";
                case PieceTraitKind.Gardener: return "Gardener Sprout";
                case PieceTraitKind.Cluster: return "Cluster Sprout";
                case PieceTraitKind.Crossfire: return "Crossfire Sprout";
                case PieceTraitKind.Even: return "Even Sprout";
                case PieceTraitKind.RainbowRow: return "Rainbow Row Sprout";
                case PieceTraitKind.Sunseeker: return "Sunseeker Sprout";
                case PieceTraitKind.Hunter: return "Hunter Sprout";
                case PieceTraitKind.Executioner: return "Executioner Sprout";
                case PieceTraitKind.Spillover: return "Spillover Sprout";
                case PieceTraitKind.Honeycomb: return "Honeycomb Sprout";
                case PieceTraitKind.Dewdrop: return "Dewdrop Sprout";
                case PieceTraitKind.Keystone: return "Keystone Sprout";
                case PieceTraitKind.Growth: return "Growth Sprout";
                case PieceTraitKind.Gambler: return "Gambler Sprout";
                case PieceTraitKind.Warden: return "Warden Sprout";
                case PieceTraitKind.Aegis: return "Aegis Sprout";
                case PieceTraitKind.Sower: return "Sower Sprout";
                case PieceTraitKind.Kin: return "Kin Sprout";
                case PieceTraitKind.Amplifier: return "Amplifier Sprout";
                case PieceTraitKind.Echo: return "Echo Sprout";
                case PieceTraitKind.Collector: return "Collector Sprout";
                case PieceTraitKind.Resonance: return "Resonance Sprout";
                case PieceTraitKind.Pouch: return "Pouch Sprout";
                case PieceTraitKind.Perennial: return "Perennial Sprout";
                case PieceTraitKind.Crescendo: return "Crescendo Sprout";
                case PieceTraitKind.Scholar: return "Scholar Sprout";
                case PieceTraitKind.FullHouse: return "Full House Sprout";
                case PieceTraitKind.Compound: return "Compound Sprout";
                case PieceTraitKind.Gap: return "Gap Sprout";
                case PieceTraitKind.Wildroot: return "Wildroot Sprout";
                case PieceTraitKind.Dye: return "Dye Sprout";
                case PieceTraitKind.Fertile: return "Fertile Sprout";
                case PieceTraitKind.Evergreen: return "Evergreen Sprout";
                case PieceTraitKind.Pruner: return "Pruner Sprout";
                case PieceTraitKind.Glow: return "Glow Sprout";
                case PieceTraitKind.Bridge: return "Bridge Sprout";
                case PieceTraitKind.Seedpod: return "Seedpod Sprout";
                case PieceTraitKind.Locksmith: return "Locksmith Sprout";
                case PieceTraitKind.Bombe: return "Spore Burst";
                case PieceTraitKind.Range: return "Long Reach";
                case PieceTraitKind.Eclat: return "Overflow";
                case PieceTraitKind.Precision: return "Pruning";
                case PieceTraitKind.Sangsue: return "Sap Drain";
                default: return kind.ToString();
            }
        }

        /// <summary>The upgrade's effect; tile upgrades are permanent, so every non-Joker one also says it counts again each time its tile is scored.</summary>
        public static string GetDescription(PieceTrait trait)
        {
            string text = GetBaseDescription(trait);
            return PieceTrait.IsJokerCombatKind(trait.Kind) || text.Length == 0 ? text : text + " Counts again every time this tile is scored.";
        }

        private static string GetBaseDescription(PieceTrait trait)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Golden:
                    return "Scores a flat golden bonus when placed.";
                case PieceTraitKind.Tinted:
                    return "Doubles this placement's group and golden bonus (not harvest) — tinted to "
                        + (trait.TintedColor.HasValue ? VisualDefaults.GetColorName(trait.TintedColor.Value) : "its target color") + ", so it always fires.";
                case PieceTraitKind.Multiplier:
                    return "Doubles the placement's ENTIRE score (group, golden, and harvest) — broader than Tinted Sprout, which skips harvest.";
                case PieceTraitKind.Blast:
                    return "This sprout and its 4 orthogonal neighbors score golden if they land in the same scored group.";
                case PieceTraitKind.Beacon:
                    return "Every filled sprout in this sprout's row and column becomes a multiplier for this placement.";
                case PieceTraitKind.Mirror:
                    return "Copies this sprout's group bonus onto one random other sprout in the scored group.";
                case PieceTraitKind.Seeder:
                    return "Turns golden for the rest of the round, rescoring every time its group is scored again.";
                case PieceTraitKind.Catalyst:
                    return "Scores extra per pre-existing cell merged into its group — the bigger the merge, the bigger the bonus.";
                case PieceTraitKind.Twin:
                    return "Copies this sprout's group bonus onto EVERY other sprout in the group, not just one like Mirror Sprout.";
                case PieceTraitKind.Detonator:
                    return "Doubles this placement's whole harvest bonus, if it harvests at least one row or column.";
                case PieceTraitKind.Chameleon:
                    return "If this sprout has a filled neighbor, the WHOLE seed recolors to match it before scoring, merging into that group.";
                case PieceTraitKind.Spark:
                    return "Scores more the longer since the last harvest this round — resets once a harvest happens.";
                case PieceTraitKind.Void:
                    return "Also uproots one random filled sprout elsewhere on the grid, scoring +" + ScoringConstants.VoidBonusPerDestroyedCell + " for the sprout broken.";
                case PieceTraitKind.Bastion:
                    return "Locks in place instead of being harvested, but keeps scoring every line it completes for the rest of the round.";
                case PieceTraitKind.Kamikaze:
                    return "Destroys itself and its 8 surrounding sprouts (including this seed's own other sprouts), scoring +" + ScoringConstants.KamikazeBonusPerDestroyedCell + " per sprout destroyed.";
                case PieceTraitKind.Venom:
                    return "Poisons the front alive enemy for " + (int)ScoringConstants.VenomDamagePercentOfMaxHp + "% of its own max HP when placed — fires even if this placement scores nothing.";
                case PieceTraitKind.Pip:
                    return "Scores +" + ScoringConstants.PipBonus + " pt when planted.";
                case PieceTraitKind.Lucky:
                    return "Scores a random bonus from 0 to " + ScoringConstants.LuckyMaxBonus + " pts when planted.";
                case PieceTraitKind.Companion:
                    return "Scores +" + ScoringConstants.CompanionBonusPerOtherColor + " pts for each touching tile of another color, and +" + ScoringConstants.CompanionBonusPerChosenColor + " pts for each touching " + (trait.TintedColor.HasValue ? VisualDefaults.GetColorName(trait.TintedColor.Value) : "tile of its chosen color") + " tile.";
                case PieceTraitKind.Colony:
                    return "Scores +" + ScoringConstants.ColonyBonus + " pts when its scored group has more than " + ScoringConstants.ColonyMinGroupExclusive + " cells.";
                case PieceTraitKind.Border:
                    return "Scores +" + ScoringConstants.BorderBonus + " pts when planted on the edge of the garden.";
                case PieceTraitKind.Heart:
                    return "Scores +" + ScoringConstants.HeartBonus + " pts when planted in the middle of the garden.";
                case PieceTraitKind.Corner:
                    return "Scores +" + ScoringConstants.CornerBonus + " pts when planted on a corner of the garden.";
                case PieceTraitKind.Hermit:
                    return "Scores +" + ScoringConstants.HermitBonus + " pts when no other tile touches it.";
                case PieceTraitKind.Cuddly:
                    return "Scores +" + ScoringConstants.CuddlyBonusPerNeighbor + " pts for each planted tile around it, diagonals and the rest of its own seed included.";
                case PieceTraitKind.Dawn:
                    return "Scores +" + ScoringConstants.DawnBonus + " pts when planted among the first " + ScoringConstants.DawnSeeds + " seeds of the round.";
                case PieceTraitKind.Dusk:
                    return "Scores +" + ScoringConstants.DuskBonus + " pts when planted as the last seed of your hand.";
                case PieceTraitKind.Miser:
                    return "Scores +" + ScoringConstants.MiserBonusPerPollen + " pt per Pollen you hold, with no limit.";
                case PieceTraitKind.Stormy:
                    return "Scores +" + ScoringConstants.StormyBonus + " pts when the weather isn't Clear skies.";
                case PieceTraitKind.Elder:
                    return "Scores +" + ScoringConstants.ElderBonusPerRound + " pts per round already cleared this run.";
                case PieceTraitKind.Harvester:
                    return "Scores +" + ScoringConstants.HarvesterBonusPerLine + " pts for each line its seed harvests.";
                case PieceTraitKind.Breeze:
                    return "Scores +" + ScoringConstants.BreezeBonusPerShuffle + " pts per Shuffle you still have.";
                case PieceTraitKind.Harmony:
                    return "Scores +" + ScoringConstants.HarmonyBonus + " pts when every tile touching it shares its color.";
                case PieceTraitKind.Prism:
                    return "Scores +" + ScoringConstants.PrismBonus + " pts when it touches " + ScoringConstants.PrismMinColors + " or more different colors.";
                case PieceTraitKind.Gardener:
                    return "Scores +" + ScoringConstants.GardenerBonusPerFertilizer + " pts for each fertilizer you hold.";
                case PieceTraitKind.Cluster:
                    return "Scores +" + ScoringConstants.ClusterBonusPerUpgrade + " pts for each other upgraded tile in its scored group.";
                case PieceTraitKind.Crossfire:
                    return "Scores +" + ScoringConstants.CrossfireBonusPerPlot + " pt for each planted tile in its row and column.";
                case PieceTraitKind.Even:
                    return "Scores +" + ScoringConstants.EvenBonus + " pts when its scored group has an even number of cells.";
                case PieceTraitKind.RainbowRow:
                    return "Scores +" + ScoringConstants.RainbowRowBonus + " pts when its row holds every color of the run.";
                case PieceTraitKind.Sunseeker:
                    return "Scores +" + ScoringConstants.SunseekerBonus + " pts on Sunshine and Golden Hour days (and their indoor cousins).";
                case PieceTraitKind.Hunter:
                    return "Scores +" + ScoringConstants.HunterBonusPerEnemy + " pts for each enemy still alive.";
                case PieceTraitKind.Executioner:
                    return "Scores +" + ScoringConstants.ExecutionerBonus + " pts when the front enemy is down to a quarter of its HP or less.";
                case PieceTraitKind.Spillover:
                    return "Scores +" + ScoringConstants.SpilloverBonus + " pts when its tile is harvested by a line.";
                case PieceTraitKind.Honeycomb:
                    return "Earns +" + ScoringConstants.HoneycombPollen + " Pollen every time it scores.";
                case PieceTraitKind.Dewdrop:
                    return "Earns +" + ScoringConstants.DewdropPollenPerLine + " Pollen for each line its seed harvests.";
                case PieceTraitKind.Keystone:
                    return "Adds +" + ScoringConstants.KeystoneMultBonus + " Mult when it sits on a corner or in the middle of the garden.";
                case PieceTraitKind.Growth:
                    return "Scores +1 pt, then one more each time it scores again this round, with no limit.";
                case PieceTraitKind.Gambler:
                    return "A coin flip: +" + ScoringConstants.GamblerWinBonus + " pts or -" + ScoringConstants.GamblerLossPenalty + " pts.";
                case PieceTraitKind.Warden:
                    return "Frees the 4 plots around it from enemy effects (locks, ice, poison) and makes them immune for the round.";
                case PieceTraitKind.Aegis:
                    return "Frees its own plot from enemy effects (locks, ice, poison) and makes it immune for the round.";
                case PieceTraitKind.Sower:
                    return "Plants a free tile of its color on a neighboring empty plot.";
                case PieceTraitKind.Kin:
                    return "Scores +" + ScoringConstants.KinBonusPerNeighbor + " pts for every other upgraded tile touching it (stack upgrades on one seed!).";
                case PieceTraitKind.Amplifier:
                    return "Scores again the points of every upgraded tile touching it: their bonus counts twice.";
                case PieceTraitKind.Echo:
                    return "Copies the biggest points bonus scored by an upgraded tile touching it.";
                case PieceTraitKind.Collector:
                    return "Scores +" + ScoringConstants.CollectorBonusPerKind + " pts for each different kind of upgrade planted in the garden.";
                case PieceTraitKind.Resonance:
                    return "Scores +" + ScoringConstants.ResonanceBonusPerTile + " pts for every upgraded tile in the garden.";
                case PieceTraitKind.Pouch:
                    return "Scores +" + ScoringConstants.PouchBonusPerUpgradedSeed + " pt for every upgraded seed in your bag.";
                case PieceTraitKind.Perennial:
                    return "Scores +1 pt, then one more every time any Perennial Sprout scores, all run long (it never resets).";
                case PieceTraitKind.Crescendo:
                    return "Scores +" + ScoringConstants.CrescendoBonusPerSeed + " pts for every seed already planted this round.";
                case PieceTraitKind.Scholar:
                    return "Scores +" + ScoringConstants.ScholarBonusPerLevel + " pt for every mastery level (shapes and colors) you have earned.";
                case PieceTraitKind.FullHouse:
                    return "Scores +" + ScoringConstants.FullHouseBonus + " pts for its row and +" + ScoringConstants.FullHouseBonus + " for its column when at least " + ScoringConstants.FullHouseMinFilled + " of its plots are full.";
                case PieceTraitKind.Compound:
                    return "Earns " + ScoringConstants.CompoundPollenPercent + "% of the Pollen you hold (rounded down) every time it scores.";
                case PieceTraitKind.Gap:
                    return "A row or column holding it is harvested even with one plot still empty (the empty plot stays empty).";
                case PieceTraitKind.Wildroot:
                    return "Counts as one extra tile of its group: bigger groups, more group points, and group-size fertilizers fire sooner.";
                case PieceTraitKind.Dye:
                    return "After scoring, paints the tiles touching it in its own color, growing the group for the next seed.";
                case PieceTraitKind.Fertile:
                    return "When its tile is harvested by a line, its plot turns golden for the rest of the round.";
                case PieceTraitKind.Evergreen:
                    return "Its tile survives harvests: when its line is harvested it stays planted and still scores the line points.";
                case PieceTraitKind.Pruner:
                    return "After scoring, uproots every touching tile of another color (+" + ScoringConstants.PrunerBonusPerTile + " pts each), freeing the plots.";
                case PieceTraitKind.Glow:
                    return "After scoring, the empty plots touching it turn golden for the rest of the round.";
                case PieceTraitKind.Bridge:
                    return "Joins its group across one empty plot to a tile of the same color in a straight line.";
                case PieceTraitKind.Seedpod:
                    return "When its tile is harvested by a line, plants 2 free tiles of its color on the empty plots around it.";
                case PieceTraitKind.Locksmith:
                    return "Each time it scores, frees one locked, frozen or poisoned plot anywhere in the garden.";
                case PieceTraitKind.Bombe:
                    return "Splits this placement's damage equally across every enemy currently alive, instead of just the front one.";
                case PieceTraitKind.Range:
                    return "Damages the LAST alive enemy in the encounter order instead of the front one.";
                case PieceTraitKind.Eclat:
                    return "Damages the front alive enemy, but any overkill beyond its remaining HP cascades onto the next alive enemy, and so on.";
                case PieceTraitKind.Precision:
                    return "Always damages whichever alive enemy currently has the lowest HP, instead of the front one — a finishing blow.";
                case PieceTraitKind.Sangsue:
                    return "Damages the front alive enemy like usual, and also converts " + (int)(ScoringConstants.SangsueLueurFraction * 100) + "% of the damage dealt into bonus Pollen (max " + ScoringConstants.SiphonLueurCapPerTrigger + ").";
                default:
                    return string.Empty;
            }
        }

        /// <summary>How often this trait's upgrade shows up in a draft — mirrors the corresponding UpgradeDefinition.Rarity in UpgradeCatalog (kept separately since a PieceTrait doesn't carry a back-reference to the UpgradeDefinition that created it).</summary>
        public static UpgradeRarity GetRarity(PieceTraitKind kind)
        {
            switch (kind)
            {
                case PieceTraitKind.Golden: return UpgradeRarity.Common;
                case PieceTraitKind.Tinted: return UpgradeRarity.Common;
                case PieceTraitKind.Multiplier: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Blast: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Beacon: return UpgradeRarity.Rare;
                case PieceTraitKind.Mirror: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Seeder: return UpgradeRarity.Rare;
                case PieceTraitKind.Catalyst: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Twin: return UpgradeRarity.Rare;
                case PieceTraitKind.Detonator: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Chameleon: return UpgradeRarity.Common;
                case PieceTraitKind.Spark: return UpgradeRarity.Common;
                case PieceTraitKind.Void: return UpgradeRarity.Rare;
                case PieceTraitKind.Bastion: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Kamikaze: return UpgradeRarity.Rare;
                case PieceTraitKind.Venom: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Cluster: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Crossfire: return UpgradeRarity.Common;
                case PieceTraitKind.Even: return UpgradeRarity.Common;
                case PieceTraitKind.RainbowRow: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Sunseeker: return UpgradeRarity.Common;
                case PieceTraitKind.Hunter: return UpgradeRarity.Common;
                case PieceTraitKind.Executioner: return UpgradeRarity.Rare;
                case PieceTraitKind.Spillover: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Honeycomb: return UpgradeRarity.Common;
                case PieceTraitKind.Dewdrop: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Keystone: return UpgradeRarity.Rare;
                case PieceTraitKind.Growth: return UpgradeRarity.Rare;
                case PieceTraitKind.Gambler: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Warden: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Aegis: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Sower: return UpgradeRarity.Rare;
                case PieceTraitKind.Kin: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Amplifier: return UpgradeRarity.Rare;
                case PieceTraitKind.Echo: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Collector: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Resonance: return UpgradeRarity.Rare;
                case PieceTraitKind.Pouch: return UpgradeRarity.Common;
                case PieceTraitKind.Perennial: return UpgradeRarity.Rare;
                case PieceTraitKind.Crescendo: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Scholar: return UpgradeRarity.Uncommon;
                case PieceTraitKind.FullHouse: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Compound: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Gap: return UpgradeRarity.Rare;
                case PieceTraitKind.Wildroot: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Dye: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Fertile: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Evergreen: return UpgradeRarity.Rare;
                case PieceTraitKind.Pruner: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Glow: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Bridge: return UpgradeRarity.Rare;
                case PieceTraitKind.Seedpod: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Locksmith: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Pip: return UpgradeRarity.Common;
                case PieceTraitKind.Lucky: return UpgradeRarity.Common;
                case PieceTraitKind.Companion: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Colony: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Border: return UpgradeRarity.Common;
                case PieceTraitKind.Heart: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Corner: return UpgradeRarity.Rare;
                case PieceTraitKind.Hermit: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Cuddly: return UpgradeRarity.Common;
                case PieceTraitKind.Dawn: return UpgradeRarity.Common;
                case PieceTraitKind.Dusk: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Miser: return UpgradeRarity.Common;
                case PieceTraitKind.Stormy: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Elder: return UpgradeRarity.Common;
                case PieceTraitKind.Harvester: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Breeze: return UpgradeRarity.Common;
                case PieceTraitKind.Harmony: return UpgradeRarity.Uncommon;
                case PieceTraitKind.Prism: return UpgradeRarity.Rare;
                case PieceTraitKind.Gardener: return UpgradeRarity.Uncommon;
                default: return UpgradeRarity.Common;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> BadgeSprites = new System.Collections.Generic.Dictionary<string, Sprite>();

        private static Sprite LoadBadge(string name)
        {
            if (!BadgeSprites.TryGetValue(name, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Icons/Traits/" + name);
                BadgeSprites[name] = sprite;
            }
            return sprite;
        }

        /// <summary>The trait's own tiny icon (Resources/Icons/Traits/&lt;Kind&gt;.png) — a bold glyph on a colored chip, readable at ~20px. Null if missing (callers then fall back to the flat badge color).</summary>
        public static Sprite GetBadgeSprite(PieceTraitKind kind)
        {
            return LoadBadge(kind.ToString());
        }

        /// <summary>The poison marker shown on a blighted plot (a toothy green skull); null if the art is missing.</summary>
        public static Sprite GetPoisonSprite()
        {
            return LoadBadge("Poison");
        }

        public static Color GetBadgeColor(PieceTrait trait)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Golden:
                case PieceTraitKind.Blast:
                    return GoldenBadgeColor;
                case PieceTraitKind.Multiplier:
                case PieceTraitKind.Beacon:
                    return MultiplierBadgeColor;
                case PieceTraitKind.Tinted:
                    return trait.TintedColor.HasValue ? VisualDefaults.GetColor(trait.TintedColor.Value) : TintedFallbackColor;
                case PieceTraitKind.Mirror:
                case PieceTraitKind.Twin:
                    return MirrorBadgeColor;
                case PieceTraitKind.Seeder:
                    return SeederBadgeColor;
                case PieceTraitKind.Catalyst:
                case PieceTraitKind.Void:
                    return VoidBadgeColor;
                case PieceTraitKind.Detonator:
                    return DetonatorBadgeColor;
                case PieceTraitKind.Chameleon:
                    return ChameleonBadgeColor;
                case PieceTraitKind.Spark:
                    return GoldenBadgeColor;
                case PieceTraitKind.Bastion:
                    return BastionBadgeColor;
                case PieceTraitKind.Kamikaze:
                    return KamikazeBadgeColor;
                case PieceTraitKind.Venom:
                    return VenomBadgeColor;
                case PieceTraitKind.Companion:
                    return trait.TintedColor.HasValue ? VisualDefaults.GetColor(trait.TintedColor.Value) : PlacementPointBadgeColor;
                case PieceTraitKind.Pip:
                case PieceTraitKind.Lucky:
                case PieceTraitKind.Colony:
                case PieceTraitKind.Border:
                case PieceTraitKind.Heart:
                case PieceTraitKind.Corner:
                case PieceTraitKind.Hermit:
                case PieceTraitKind.Cuddly:
                case PieceTraitKind.Dawn:
                case PieceTraitKind.Dusk:
                case PieceTraitKind.Miser:
                case PieceTraitKind.Stormy:
                case PieceTraitKind.Elder:
                case PieceTraitKind.Harvester:
                case PieceTraitKind.Breeze:
                case PieceTraitKind.Harmony:
                case PieceTraitKind.Prism:
                case PieceTraitKind.Gardener:
                case PieceTraitKind.Cluster:
                case PieceTraitKind.Crossfire:
                case PieceTraitKind.Even:
                case PieceTraitKind.RainbowRow:
                case PieceTraitKind.Sunseeker:
                case PieceTraitKind.Hunter:
                case PieceTraitKind.Executioner:
                case PieceTraitKind.Spillover:
                case PieceTraitKind.Honeycomb:
                case PieceTraitKind.Dewdrop:
                case PieceTraitKind.Keystone:
                case PieceTraitKind.Growth:
                case PieceTraitKind.Gambler:
                case PieceTraitKind.Warden:
                case PieceTraitKind.Aegis:
                case PieceTraitKind.Sower:
                case PieceTraitKind.Kin:
                case PieceTraitKind.Amplifier:
                case PieceTraitKind.Echo:
                case PieceTraitKind.Collector:
                case PieceTraitKind.Resonance:
                case PieceTraitKind.Pouch:
                case PieceTraitKind.Perennial:
                case PieceTraitKind.Crescendo:
                case PieceTraitKind.Scholar:
                case PieceTraitKind.FullHouse:
                case PieceTraitKind.Compound:
                case PieceTraitKind.Gap:
                case PieceTraitKind.Wildroot:
                case PieceTraitKind.Dye:
                case PieceTraitKind.Fertile:
                case PieceTraitKind.Evergreen:
                case PieceTraitKind.Pruner:
                case PieceTraitKind.Glow:
                case PieceTraitKind.Bridge:
                case PieceTraitKind.Seedpod:
                case PieceTraitKind.Locksmith:
                    return PlacementPointBadgeColor;
                case PieceTraitKind.Bombe:
                case PieceTraitKind.Range:
                case PieceTraitKind.Eclat:
                case PieceTraitKind.Precision:
                case PieceTraitKind.Sangsue:
                    return JokerCombatBadgeColor;
                default:
                    return Color.gray;
            }
        }
    }
}
