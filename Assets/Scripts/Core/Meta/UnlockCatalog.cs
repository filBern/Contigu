using System.Collections.Generic;

namespace Contigu.Core
{
    public enum UnlockKind
    {
        Challenge,
        Sprout,
        Fertilizer
    }

    /// <summary>One unlockable thing and what the player must do to get it.</summary>
    public sealed class UnlockEntry
    {
        public readonly UnlockKind Kind;
        public readonly int Id;
        public readonly UnlockCondition Condition;

        public UnlockEntry(UnlockKind kind, int id, UnlockCondition condition)
        {
            Kind = kind;
            Id = id;
            Condition = condition;
        }

        /// <summary>The key used in saves and lookups: "C", "S" or "F" followed by the integer id.</summary>
        public string Key
        {
            get { return UnlockCatalog.KeyOf(Kind, Id); }
        }
    }

    /// <summary>
    /// What is locked at the start and how each locked thing is earned (docs/PROGRESSION.md). Anything NOT listed here (the starting core of
    /// sprouts and fertilizers, Spring, every non tile-upgrade shop item) is available from the very first run. Pure data, no state.
    /// </summary>
    public static class UnlockCatalog
    {
        private static readonly List<UnlockEntry> Entries = new List<UnlockEntry>();
        private static readonly Dictionary<string, UnlockEntry> ByKey = new Dictionary<string, UnlockEntry>();

        public static string KeyOf(UnlockKind kind, int id)
        {
            return (kind == UnlockKind.Challenge ? "C" : kind == UnlockKind.Sprout ? "S" : "F") + id;
        }

        public static IReadOnlyList<UnlockEntry> All
        {
            get { return Entries; }
        }

        /// <summary>The entry for a locked item, or null when it is available from the start.</summary>
        public static UnlockEntry Find(UnlockKind kind, int id)
        {
            UnlockEntry entry;
            return ByKey.TryGetValue(KeyOf(kind, id), out entry) ? entry : null;
        }

        public static UnlockEntry FindByKey(string key)
        {
            UnlockEntry entry;
            return ByKey.TryGetValue(key, out entry) ? entry : null;
        }

        private static void Add(string kind, int id, UnlockMetric metric, UnlockScope scope, int threshold, string text, bool atMost = false)
        {
            var k = kind == "C" ? UnlockKind.Challenge : kind == "S" ? UnlockKind.Sprout : UnlockKind.Fertilizer;
            var entry = new UnlockEntry(k, id, new UnlockCondition(metric, scope, threshold, text, atMost));
            Entries.Add(entry);
            ByKey[entry.Key] = entry;
        }

        static UnlockCatalog()
        {
            // Challenges (Spring is always open)
            Add("C", (int)ChallengeId.Marathon, UnlockMetric.RunWonSpring, UnlockScope.Total, 1, "Win a Spring run");
            Add("C", (int)ChallengeId.Summer, UnlockMetric.RoundReached, UnlockScope.Single, 6, "Reach round 6");
            Add("C", (int)ChallengeId.Autumn, UnlockMetric.FertilizersHeld, UnlockScope.Single, 4, "Hold 4 fertilizers at once");
            Add("C", (int)ChallengeId.Orchard, UnlockMetric.GroupSize, UnlockScope.Single, 15, "Form a group of 15 tiles");
            Add("C", (int)ChallengeId.Greenhouse, UnlockMetric.Lines, UnlockScope.Single, 3, "Harvest 3 lines with a single seed");
            // Sprouts (everything not listed is available from the first run)
            Add("S", (int)UpgradeId.MultiplierZone, UnlockMetric.Score, UnlockScope.Single, 10000, "Score 10000 points with a single seed");
            Add("S", (int)UpgradeId.BlastTile, UnlockMetric.Lines, UnlockScope.Single, 3, "Harvest 3 lines with a single seed");
            Add("S", (int)UpgradeId.MultiplierBeacon, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 12, "Have 12 different upgrades on the garden at once");
            Add("S", (int)UpgradeId.MirrorTile, UnlockMetric.TraitCells, UnlockScope.Single, 5, "Score 5 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.Seeder, UnlockMetric.GoldenScored, UnlockScope.Run, 70, "Score 70 golden plots in one run");
            Add("S", (int)UpgradeId.TwinTile, UnlockMetric.TraitCells, UnlockScope.Single, 6, "Score 6 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.VoidTile, UnlockMetric.GardenTiles, UnlockScope.Single, 25, "Have 25 tiles on the garden at once");
            Add("S", (int)UpgradeId.KamikazeTile, UnlockMetric.GardenTiles, UnlockScope.Single, 26, "Have 26 tiles on the garden at once");
            Add("S", (int)UpgradeId.PermanentBonusSquare, UnlockMetric.RoundReached, UnlockScope.Single, 7, "Reach round 7");
            Add("S", (int)UpgradeId.CornerTile, UnlockMetric.CornersFilled, UnlockScope.Single, 4, "Fill all 4 corners of the garden at once");
            Add("S", (int)UpgradeId.HermitTile, UnlockMetric.Isolated, UnlockScope.Run, 25, "Plant 25 seeds that touch nothing in one run");
            Add("S", (int)UpgradeId.DuskTile, UnlockMetric.HarvestOnLastSeedOfHand, UnlockScope.Run, 25, "Harvest a line with the last seed of a hand (25 times in one run)");
            Add("S", (int)UpgradeId.MiserTile, UnlockMetric.Pollen, UnlockScope.Single, 300, "Hold 300 Pollen at once");
            Add("S", (int)UpgradeId.StormyTile, UnlockMetric.RoundClearedWeather, UnlockScope.Run, 9, "Clear a round on a day with weather (9 times in one run)");
            Add("S", (int)UpgradeId.SunseekerTile, UnlockMetric.RoundClearedSunny, UnlockScope.Run, 2, "Clear a round on a Sunshine day (2 times in one run)");
            Add("S", (int)UpgradeId.HarvesterTile, UnlockMetric.Lines, UnlockScope.Round, 18, "Harvest 18 lines in one round");
            Add("S", (int)UpgradeId.PrismTile, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("S", (int)UpgradeId.GardenerTile, UnlockMetric.FertilizersHeld, UnlockScope.Single, 3, "Hold 3 fertilizers at once");
            Add("S", (int)UpgradeId.ClusterTile, UnlockMetric.TraitCells, UnlockScope.Single, 5, "Score 5 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.RainbowRowTile, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("S", (int)UpgradeId.ExecutionerTile, UnlockMetric.EnemiesDefeated, UnlockScope.Total, 500, "Defeat 500 enemies in total");
            Add("S", (int)UpgradeId.SpilloverTile, UnlockMetric.Lines, UnlockScope.Run, 100, "Harvest 100 lines in one run");
            Add("S", (int)UpgradeId.DewdropTile, UnlockMetric.Lines, UnlockScope.Round, 18, "Harvest 18 lines in one round");
            Add("S", (int)UpgradeId.KeystoneTile, UnlockMetric.Corners, UnlockScope.Run, 80, "Plant 80 tiles on corners in one run");
            Add("S", (int)UpgradeId.GrowthTile, UnlockMetric.RoundClearedNoShuffle, UnlockScope.Run, 10, "Clear 10 rounds without a Shuffle in one run");
            Add("S", (int)UpgradeId.GamblerTile, UnlockMetric.RoundReached, UnlockScope.Single, 3, "Reach round 3");
            Add("S", (int)UpgradeId.WardenTile, UnlockMetric.PlotsAfflicted, UnlockScope.Total, 1500, "Suffer 1500 locked, frozen or blighted plots in total");
            Add("S", (int)UpgradeId.AegisTile, UnlockMetric.AfflictedPlots, UnlockScope.Single, 18, "Have 18 plots locked, frozen or blighted at once");
            Add("S", (int)UpgradeId.SowerTile, UnlockMetric.Seeds, UnlockScope.Round, 35, "Plant 35 seeds in one round");
            Add("S", (int)UpgradeId.KinTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 15, "Have 15 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.AmplifierTile, UnlockMetric.TraitPoints, UnlockScope.Single, 500, "Score 500 points from upgrades with a single seed");
            Add("S", (int)UpgradeId.EchoTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 15, "Have 15 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.CollectorTile, UnlockMetric.BagUpgradeKinds, UnlockScope.Single, 25, "Have 25 different upgrades in your bag");
            Add("S", (int)UpgradeId.ResonanceTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 15, "Have 15 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.PerennialTile, UnlockMetric.RoundReached, UnlockScope.Single, 10, "Reach round 10");
            Add("S", (int)UpgradeId.CrescendoTile, UnlockMetric.Seeds, UnlockScope.Round, 35, "Plant 35 seeds in one round");
            Add("S", (int)UpgradeId.ScholarTile, UnlockMetric.MasteryLevels, UnlockScope.Single, 10, "Earn 10 mastery levels");
            Add("S", (int)UpgradeId.FullHouseTile, UnlockMetric.GardenTiles, UnlockScope.Single, 24, "Have 24 tiles on the garden at once");
            Add("S", (int)UpgradeId.CompoundTile, UnlockMetric.Pollen, UnlockScope.Single, 400, "Hold 400 Pollen at once");
            Add("S", (int)UpgradeId.GapTile, UnlockMetric.SeedsLeftAtClear, UnlockScope.Single, 30, "Clear a round with 30 seeds left");
            Add("S", (int)UpgradeId.WildrootTile, UnlockMetric.BagJokers, UnlockScope.Single, 2, "Own a Joker seed (2)");
            Add("S", (int)UpgradeId.DyeTile, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("S", (int)UpgradeId.FertileTile, UnlockMetric.GoldenScored, UnlockScope.Run, 15, "Score 15 golden plots in one run");
            Add("S", (int)UpgradeId.EvergreenTile, UnlockMetric.Lines, UnlockScope.Total, 2500, "Harvest 2500 lines in total");
            Add("S", (int)UpgradeId.PrunerTile, UnlockMetric.NeighborsTouched, UnlockScope.Single, 6, "Plant a seed touching 6 tiles");
            Add("S", (int)UpgradeId.GlowTile, UnlockMetric.GoldenScored, UnlockScope.Run, 30, "Score 30 golden plots in one run");
            Add("S", (int)UpgradeId.BridgeTile, UnlockMetric.MergedGroups, UnlockScope.Single, 3, "Join 3 groups together with one seed");
            Add("S", (int)UpgradeId.SeedpodTile, UnlockMetric.GroupSize, UnlockScope.Single, 15, "Form a group of 15 tiles");
            Add("S", (int)UpgradeId.MimicTile, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 12, "Have 12 different upgrades on the garden at once");
            Add("S", (int)UpgradeId.DominantTile, UnlockMetric.SameColorStreak, UnlockScope.Single, 5, "Plant 5 seeds of the same color in a row");
            Add("S", (int)UpgradeId.HeritageTile, UnlockMetric.BagOneColor, UnlockScope.Single, 11, "Own 11 seeds of one color in your bag");
            Add("S", (int)UpgradeId.ScytheTile, UnlockMetric.Lines, UnlockScope.Single, 3, "Harvest 3 lines with a single seed");
            Add("S", (int)UpgradeId.TallyTile, UnlockMetric.Lines, UnlockScope.Round, 18, "Harvest 18 lines in one round");
            Add("S", (int)UpgradeId.SunbeamTile, UnlockMetric.ClearedWinterRound, UnlockScope.Run, 1, "Clear a round in Winter");
            Add("S", (int)UpgradeId.AntidoteTile, UnlockMetric.DefeatedBlightspore, UnlockScope.Total, 3, "Defeat Blightspore (3 times in total)");
            Add("S", (int)UpgradeId.LullTile, UnlockMetric.DefeatedBramble, UnlockScope.Total, 3, "Defeat Bramble (3 times in total)");
            Add("S", (int)UpgradeId.DazzleTile, UnlockMetric.DefeatedHaterBoss, UnlockScope.Total, 2, "Defeat a boss that hates a color or a shape (2 times in total)");
            Add("S", (int)UpgradeId.WiltTile, UnlockMetric.DefeatedLeechOrVine, UnlockScope.Total, 3, "Defeat Root Leech or Creeping Vine (3 times in total)");
            Add("S", (int)UpgradeId.CleanseTile, UnlockMetric.RoundClearedClean, UnlockScope.Run, 9, "Clear a round from round 3 on with no locked, frozen or blighted plot left (9 times in one run)");
            Add("S", (int)UpgradeId.CloneTile, UnlockMetric.BagSize, UnlockScope.Single, 40, "Own 40 seeds in your bag");
            Add("S", (int)UpgradeId.MirrorWardTile, UnlockMetric.BlightScored, UnlockScope.Total, 800, "Score through a blighted plot 800 times in total");
            Add("S", (int)UpgradeId.SeedsackTile, UnlockMetric.HandEmptied, UnlockScope.Run, 80, "Empty your hand 80 times in one run");
            Add("S", (int)UpgradeId.HeirloomTile, UnlockMetric.RunWon, UnlockScope.Total, 1, "Win a run");
            Add("S", (int)UpgradeId.AnchorTile, UnlockMetric.DefeatedResident, UnlockScope.Total, 1, "Defeat a challenge's resident enemy");
            Add("S", (int)UpgradeId.LateBloomTile, UnlockMetric.RoundClearedNoHarvest, UnlockScope.Run, 1, "Clear a round without harvesting a line");
            // Fertilizers (everything not listed is available from the first run)
            Add("F", (int)ModifierId.Prisme, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.MegaChaine, UnlockMetric.GroupSize, UnlockScope.Single, 18, "Form a group of 18 tiles");
            Add("F", (int)ModifierId.Forteresse, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Architecte, UnlockMetric.SquarePlayed, UnlockScope.Run, 25, "Plant 25 square seeds in one run");
            Add("F", (int)ModifierId.Collectionneur, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have 4 colors on the garden at once");
            Add("F", (int)ModifierId.Tricolore, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.Complementaire, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("F", (int)ModifierId.Ilot, UnlockMetric.Isolated, UnlockScope.Round, 5, "Plant 5 seeds that touch nothing in one round");
            Add("F", (int)ModifierId.Carrefour, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Demolisseur, UnlockMetric.Lines, UnlockScope.Round, 20, "Harvest 20 lines in one round");
            Add("F", (int)ModifierId.CercleChromatique, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.Monochrome, UnlockMetric.SameColorStreak, UnlockScope.Single, 4, "Plant 4 seeds of the same color in a row");
            Add("F", (int)ModifierId.Contraste, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Degrade, UnlockMetric.BiggerGroupStreak, UnlockScope.Single, 5, "Score a bigger group 5 placements in a row");
            Add("F", (int)ModifierId.Emmitouflee, UnlockMetric.GroupSize, UnlockScope.Single, 15, "Form a group of 15 tiles");
            Add("F", (int)ModifierId.Jardinier, UnlockMetric.GardenUpgraded, UnlockScope.Single, 10, "Have 10 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.ArcEnCiel, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("F", (int)ModifierId.Alternance, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 18, "Plant 18 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Palindrome, UnlockMetric.Lines, UnlockScope.Run, 100, "Harvest 100 lines in one run");
            Add("F", (int)ModifierId.Gradient, UnlockMetric.Lines, UnlockScope.Run, 120, "Harvest 120 lines in one run");
            Add("F", (int)ModifierId.Bloc, UnlockMetric.Lines, UnlockScope.Round, 18, "Harvest 18 lines in one round");
            Add("F", (int)ModifierId.MonochromeLigne, UnlockMetric.SameColorStreak, UnlockScope.Single, 5, "Plant 5 seeds of the same color in a row");
            Add("F", (int)ModifierId.SlotUn, UnlockMetric.SlotPlayed1, UnlockScope.Round, 12, "Play 12 seeds from hand slot 1 in one round");
            Add("F", (int)ModifierId.SlotDeux, UnlockMetric.SlotPlayed2, UnlockScope.Round, 12, "Play 12 seeds from hand slot 2 in one round");
            Add("F", (int)ModifierId.SlotTrois, UnlockMetric.SlotPlayed3, UnlockScope.Round, 12, "Play 12 seeds from hand slot 3 in one round");
            Add("F", (int)ModifierId.HorsNorme, UnlockMetric.SeedSize12, UnlockScope.Round, 10, "Play 10 seeds of 1 or 2 cells in one round");
            Add("F", (int)ModifierId.Solitaire, UnlockMetric.Isolated, UnlockScope.Run, 20, "Plant 20 seeds that touch nothing in one run");
            Add("F", (int)ModifierId.EspaceLibre, UnlockMetric.GardenFillPercentAtClear, UnlockScope.Single, 33, "Clear a round with the garden a third full or less", atMost: true);
            Add("F", (int)ModifierId.Rafale, UnlockMetric.HarvestStreak, UnlockScope.Single, 5, "Harvest a line on 5 placements in a row");
            Add("F", (int)ModifierId.Fraicheur, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 15, "Plant 15 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Pont, UnlockMetric.MergedGroups, UnlockScope.Single, 3, "Join 3 groups together with one seed");
            Add("F", (int)ModifierId.Encerclement, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Boucher, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.GrosseFamille, UnlockMetric.GroupSize, UnlockScope.Single, 15, "Form a group of 15 tiles");
            Add("F", (int)ModifierId.Repetition, UnlockMetric.SameShapeStreak, UnlockScope.Single, 4, "Plant the same shape 4 times in a row");
            Add("F", (int)ModifierId.AlternancePieces, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 15, "Plant 15 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Combo, UnlockMetric.HarvestStreak, UnlockScope.Single, 5, "Harvest a line on 5 placements in a row");
            Add("F", (int)ModifierId.Precision, UnlockMetric.Seeds, UnlockScope.Round, 35, "Plant 35 seeds in one round");
            Add("F", (int)ModifierId.Surpopulation, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Minimaliste, UnlockMetric.Isolated, UnlockScope.Run, 25, "Plant 25 seeds that touch nothing in one run");
            Add("F", (int)ModifierId.Joker, UnlockMetric.BagJokers, UnlockScope.Single, 3, "Own 3 Joker seeds at once");
            Add("F", (int)ModifierId.Densite, UnlockMetric.GardenTiles, UnlockScope.Single, 25, "Have 25 tiles on the garden at once");
            Add("F", (int)ModifierId.MultQuatre, UnlockMetric.RoundReached, UnlockScope.Single, 8, "Reach round 8");
            Add("F", (int)ModifierId.Solidarite, UnlockMetric.FertilizersHeld, UnlockScope.Single, 4, "Hold 4 fertilizers at once");
            Add("F", (int)ModifierId.Copieur, UnlockMetric.FertilizerCategories, UnlockScope.Single, 3, "Hold fertilizers of 3 different categories");
            Add("F", (int)ModifierId.MultCinqRisque, UnlockMetric.RoundReached, UnlockScope.Single, 5, "Reach round 5");
            Add("F", (int)ModifierId.CartesEnchantees, UnlockMetric.BagUpgraded, UnlockScope.Single, 40, "Have 40 upgraded seeds in your bag");
            Add("F", (int)ModifierId.Epuisement, UnlockMetric.RoundReached, UnlockScope.Single, 4, "Reach round 4");
            Add("F", (int)ModifierId.Multitude, UnlockMetric.BagSize, UnlockScope.Single, 40, "Own 40 seeds in your bag");
            Add("F", (int)ModifierId.Experience, UnlockMetric.SeedUpgraded, UnlockScope.Run, 200, "Play 200 upgraded seeds in one run");
            Add("F", (int)ModifierId.FormatPetitSpecialiste, UnlockMetric.SeedSize12, UnlockScope.Round, 9, "Play 9 seeds of 1 or 2 cells in one round");
            Add("F", (int)ModifierId.FormatMoyenSpecialiste, UnlockMetric.SeedSize3, UnlockScope.Round, 12, "Play 12 three-cell seeds in one round");
            Add("F", (int)ModifierId.FormatGrandSpecialiste, UnlockMetric.SeedSize4, UnlockScope.Round, 20, "Play 20 four-cell seeds in one round");
            Add("F", (int)ModifierId.FormatPetitGlow, UnlockMetric.SeedSize12, UnlockScope.Run, 50, "Play 50 seeds of 1 or 2 cells in one run");
            Add("F", (int)ModifierId.FormatMoyenGlow, UnlockMetric.SeedSize3, UnlockScope.Run, 50, "Play 50 three-cell seeds in one run");
            Add("F", (int)ModifierId.FormatGrandGlow, UnlockMetric.SeedSize4, UnlockScope.Run, 120, "Play 120 four-cell seeds in one run");
            Add("F", (int)ModifierId.Polyvalence, UnlockMetric.FertilizerCategories, UnlockScope.Single, 4, "Hold fertilizers of 4 different categories");
            Add("F", (int)ModifierId.CollectionChromatique, UnlockMetric.DevotionsHeld, UnlockScope.Single, 2, "Hold 2 Devotion fertilizers at once");
            Add("F", (int)ModifierId.Cadence, UnlockMetric.FertilizersHeld, UnlockScope.Single, 3, "Hold 3 fertilizers at once");
            Add("F", (int)ModifierId.Echo, UnlockMetric.BagUpgradeKinds, UnlockScope.Single, 30, "Have 30 different upgrades in your bag");
            Add("F", (int)ModifierId.QuatriemeEmplacement, UnlockMetric.RunWon, UnlockScope.Total, 1, "Win a run");
            Add("F", (int)ModifierId.Barometre, UnlockMetric.RoundClearedWeather, UnlockScope.Run, 7, "Clear 7 rounds with weather in one run");
            Add("F", (int)ModifierId.ChasseurTempete, UnlockMetric.RoundClearedHarshWeather, UnlockScope.Total, 12, "Clear a round in Frost, Heatwave or Hurricane (12 times in total)");
            Add("F", (int)ModifierId.Impermeable, UnlockMetric.RoundClearedWeather, UnlockScope.Total, 12, "Clear 12 rounds with weather in total");
            Add("F", (int)ModifierId.Hate, UnlockMetric.SeedsLeftAtClear, UnlockScope.Single, 30, "Clear a round with 30 seeds left");
            Add("F", (int)ModifierId.PierreAngulaire, UnlockMetric.Corners, UnlockScope.Run, 70, "Plant 70 tiles on corners in one run");
            Add("F", (int)ModifierId.Coeur, UnlockMetric.Middle, UnlockScope.Run, 20, "Plant on the middle of the garden 20 times in one run");
            Add("F", (int)ModifierId.Treillis, UnlockMetric.VerticalSeed, UnlockScope.Round, 6, "Play 6 vertical seeds in one round");
            Add("F", (int)ModifierId.Sillon, UnlockMetric.HorizontalSeed, UnlockScope.Round, 6, "Play 6 horizontal seeds in one round");
            Add("F", (int)ModifierId.Haricot, UnlockMetric.Straight3, UnlockScope.Round, 6, "Play 6 straight seeds of 3 or more tiles in one round");
            Add("F", (int)ModifierId.Racines, UnlockMetric.GroupSize, UnlockScope.Single, 15, "Form a group of 15 tiles");
            Add("F", (int)ModifierId.Emboite, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.Mosaique, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.Foule, UnlockMetric.GardenFillPercent, UnlockScope.Single, 66, "Have two thirds of the garden planted at once");
            Add("F", (int)ModifierId.Prairie, UnlockMetric.GardenTiles, UnlockScope.Single, 22, "Have 22 tiles on the garden at once");
            Add("F", (int)ModifierId.Guirlande, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.MoissonCroisee, UnlockMetric.Lines, UnlockScope.Single, 3, "Harvest 3 lines with a single seed");
            Add("F", (int)ModifierId.TableRase, UnlockMetric.GardenCleared, UnlockScope.Run, 1, "Harvest every tile of the garden at once");
            Add("F", (int)ModifierId.BriseGlace, UnlockMetric.FrozenBroken, UnlockScope.Total, 9, "Break a frozen plot with a line (9 times in total)");
            Add("F", (int)ModifierId.Saisons, UnlockMetric.RunWonSpring, UnlockScope.Total, 1, "Win a Spring run");
            Add("F", (int)ModifierId.CoupFinal, UnlockMetric.EnemyDefeatedSeedsLeft, UnlockScope.Single, 35, "Defeat an enemy with 35 seeds left");
            Add("F", (int)ModifierId.DerniereGraine, UnlockMetric.RoundClearedOnLastSeed, UnlockScope.Run, 1, "Clear a round on its last seed");
            Add("F", (int)ModifierId.PremiereGraine, UnlockMetric.HarvestOnFirstSeedOfRound, UnlockScope.Run, 1, "Harvest a line with the first seed of a round");
            Add("F", (int)ModifierId.Tirelire, UnlockMetric.Pollen, UnlockScope.Single, 1000, "Hold 1000 Pollen at once");
            Add("F", (int)ModifierId.BanqueGraines, UnlockMetric.BagSize, UnlockScope.Single, 40, "Own 40 seeds in your bag");
            Add("F", (int)ModifierId.Rustique, UnlockMetric.RoundClearedFrost, UnlockScope.Run, 1, "Clear a round in Frost");
            Add("F", (int)ModifierId.CavalierVent, UnlockMetric.RoundClearedWind, UnlockScope.Run, 1, "Clear a round in Wind");
            Add("F", (int)ModifierId.Marathonien, UnlockMetric.RoundReached, UnlockScope.Single, 9, "Reach round 9");
            Add("F", (int)ModifierId.Pourfendeur, UnlockMetric.DefeatedBoss, UnlockScope.Total, 2, "Defeat a boss (2 times in total)");
            Add("F", (int)ModifierId.CharmedPatch, UnlockMetric.TraitCells, UnlockScope.Single, 5, "Score 5 upgraded tiles in the same group");
            Add("F", (int)ModifierId.TwinGraft, UnlockMetric.SeedTwoUpgrades, UnlockScope.Run, 70, "Play a seed with 2 upgrades (70 times in one run)");
            Add("F", (int)ModifierId.Botanist, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 10, "Have 10 different upgrades on the garden at once");
            Add("F", (int)ModifierId.Hothouse, UnlockMetric.GardenUpgraded, UnlockScope.Single, 15, "Have 15 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.GoodNeighbors, UnlockMetric.GardenUpgraded, UnlockScope.Single, 15, "Have 15 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.Gleaner, UnlockMetric.Lines, UnlockScope.Run, 100, "Harvest 100 lines in one run");
            Add("F", (int)ModifierId.WildCluster, UnlockMetric.BagJokers, UnlockScope.Single, 2, "Own 2 Joker seeds at once");
            Add("F", (int)ModifierId.WildReserve, UnlockMetric.BagJokers, UnlockScope.Single, 2, "Own 2 Joker seeds at once");
            Add("F", (int)ModifierId.WildSurge, UnlockMetric.BagJokers, UnlockScope.Single, 4, "Own 4 Joker seeds at once");
            Add("F", (int)ModifierId.CropRotation, UnlockMetric.ShapeChanges, UnlockScope.Round, 30, "Change the shape you plant 30 times in one round");
            Add("F", (int)ModifierId.GreenhouseLamp, UnlockMetric.ClearedGreenhouseRound, UnlockScope.Total, 1, "Clear a round in Greenhouse");
            Add("F", (int)ModifierId.SeedLibrary, UnlockMetric.BagShapes, UnlockScope.Single, 8, "Own all 8 shapes in your bag at once");
            Add("F", (int)ModifierId.TwinBloom, UnlockMetric.SameColorStreak, UnlockScope.Single, 5, "Plant 5 seeds of the same color in a row");
            Add("F", (int)ModifierId.Outsider, UnlockMetric.EnemiesDefeated, UnlockScope.Total, 100, "Defeat 100 enemies in total");
        }
    }
}
