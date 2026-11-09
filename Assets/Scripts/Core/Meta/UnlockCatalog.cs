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
            Add("C", (int)ChallengeId.Summer, UnlockMetric.RoundReached, UnlockScope.Single, 8, "Reach round 8");
            Add("C", (int)ChallengeId.Autumn, UnlockMetric.FertilizersHeld, UnlockScope.Single, 4, "Hold 4 fertilizers at once");
            Add("C", (int)ChallengeId.Orchard, UnlockMetric.GroupSize, UnlockScope.Single, 10, "Form a group of 10 tiles");
            Add("C", (int)ChallengeId.Greenhouse, UnlockMetric.Lines, UnlockScope.Single, 2, "Harvest 2 lines with a single seed");
        // Sprouts (everything not listed is available from the first run)
            Add("S", (int)UpgradeId.MultiplierZone, UnlockMetric.Score, UnlockScope.Single, 400, "Score 400 points with a single seed");
            Add("S", (int)UpgradeId.BlastTile, UnlockMetric.Lines, UnlockScope.Single, 2, "Harvest 2 lines with a single seed");
            Add("S", (int)UpgradeId.MultiplierBeacon, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 4, "Have 4 different upgrades on the garden at once");
            Add("S", (int)UpgradeId.MirrorTile, UnlockMetric.TraitCells, UnlockScope.Single, 2, "Score 2 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.Seeder, UnlockMetric.GoldenScored, UnlockScope.Run, 10, "Score 10 golden plots in one run");
            Add("S", (int)UpgradeId.TwinTile, UnlockMetric.TraitCells, UnlockScope.Single, 5, "Score 5 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.VoidTile, UnlockMetric.GardenTiles, UnlockScope.Single, 20, "Have 20 tiles on the garden at once");
            Add("S", (int)UpgradeId.KamikazeTile, UnlockMetric.Destroyed, UnlockScope.Single, 5, "Destroy 5 tiles with a single seed");
            Add("S", (int)UpgradeId.PermanentBonusSquare, UnlockMetric.RoundReached, UnlockScope.Single, 6, "Reach round 6");
            Add("S", (int)UpgradeId.CornerTile, UnlockMetric.CornersFilled, UnlockScope.Single, 4, "Fill all 4 corners of the garden at once");
            Add("S", (int)UpgradeId.HermitTile, UnlockMetric.Isolated, UnlockScope.Run, 5, "Plant 5 seeds that touch nothing in one run");
            Add("S", (int)UpgradeId.DuskTile, UnlockMetric.HarvestOnLastSeedOfHand, UnlockScope.Run, 1, "Harvest a line with the last seed of a hand");
            Add("S", (int)UpgradeId.MiserTile, UnlockMetric.Pollen, UnlockScope.Single, 30, "Hold 30 Pollen at once");
            Add("S", (int)UpgradeId.StormyTile, UnlockMetric.RoundClearedWeather, UnlockScope.Run, 1, "Clear a round on a day with weather");
            Add("S", (int)UpgradeId.SunseekerTile, UnlockMetric.RoundClearedSunny, UnlockScope.Run, 1, "Clear a round on a Sunshine day");
            Add("S", (int)UpgradeId.HarvesterTile, UnlockMetric.Lines, UnlockScope.Round, 4, "Harvest 4 lines in one round");
            Add("S", (int)UpgradeId.PrismTile, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("S", (int)UpgradeId.GardenerTile, UnlockMetric.FertilizersHeld, UnlockScope.Single, 3, "Hold 3 fertilizers at once");
            Add("S", (int)UpgradeId.ClusterTile, UnlockMetric.TraitCells, UnlockScope.Single, 3, "Score 3 upgraded tiles in the same group");
            Add("S", (int)UpgradeId.RainbowRowTile, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("S", (int)UpgradeId.ExecutionerTile, UnlockMetric.EnemiesDefeated, UnlockScope.Total, 10, "Defeat 10 enemies in total");
            Add("S", (int)UpgradeId.SpilloverTile, UnlockMetric.Lines, UnlockScope.Run, 10, "Harvest 10 lines in one run");
            Add("S", (int)UpgradeId.DewdropTile, UnlockMetric.Lines, UnlockScope.Round, 3, "Harvest 3 lines in one round");
            Add("S", (int)UpgradeId.KeystoneTile, UnlockMetric.Corners, UnlockScope.Run, 4, "Plant 4 tiles on corners in one run");
            Add("S", (int)UpgradeId.GrowthTile, UnlockMetric.RoundClearedNoShuffle, UnlockScope.Run, 3, "Clear 3 rounds without a Shuffle in one run");
            Add("S", (int)UpgradeId.GamblerTile, UnlockMetric.RoundReached, UnlockScope.Single, 3, "Reach round 3");
            Add("S", (int)UpgradeId.WardenTile, UnlockMetric.PlotsAfflicted, UnlockScope.Total, 20, "Suffer 20 locked, frozen or blighted plots in total");
            Add("S", (int)UpgradeId.AegisTile, UnlockMetric.AfflictedPlots, UnlockScope.Single, 3, "Have 3 plots locked, frozen or blighted at once");
            Add("S", (int)UpgradeId.SowerTile, UnlockMetric.Seeds, UnlockScope.Round, 20, "Plant 20 seeds in one round");
            Add("S", (int)UpgradeId.KinTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 2, "Have 2 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.AmplifierTile, UnlockMetric.TraitPoints, UnlockScope.Single, 100, "Score 100 points from upgrades with a single seed");
            Add("S", (int)UpgradeId.EchoTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 4, "Have 4 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.CollectorTile, UnlockMetric.BagUpgradeKinds, UnlockScope.Single, 6, "Have 6 different upgrades in your bag");
            Add("S", (int)UpgradeId.ResonanceTile, UnlockMetric.GardenUpgraded, UnlockScope.Single, 8, "Have 8 upgraded tiles on the garden at once");
            Add("S", (int)UpgradeId.PerennialTile, UnlockMetric.RoundReached, UnlockScope.Single, 9, "Reach round 9");
            Add("S", (int)UpgradeId.CrescendoTile, UnlockMetric.Seeds, UnlockScope.Round, 12, "Plant 12 seeds in one round");
            Add("S", (int)UpgradeId.ScholarTile, UnlockMetric.MasteryLevels, UnlockScope.Single, 3, "Earn 3 mastery levels");
            Add("S", (int)UpgradeId.FullHouseTile, UnlockMetric.GardenTiles, UnlockScope.Single, 30, "Have 30 tiles on the garden at once");
            Add("S", (int)UpgradeId.CompoundTile, UnlockMetric.Pollen, UnlockScope.Single, 50, "Hold 50 Pollen at once");
            Add("S", (int)UpgradeId.GapTile, UnlockMetric.SeedsLeftAtClear, UnlockScope.Single, 5, "Clear a round with 5 seeds left");
            Add("S", (int)UpgradeId.WildrootTile, UnlockMetric.BagJokers, UnlockScope.Single, 1, "Own a Joker seed");
            Add("S", (int)UpgradeId.DyeTile, UnlockMetric.ColorsTouched, UnlockScope.Single, 2, "Plant a seed touching 2 other colors");
            Add("S", (int)UpgradeId.FertileTile, UnlockMetric.GoldenPlots, UnlockScope.Single, 3, "Have 3 golden plots on the garden at once");
            Add("S", (int)UpgradeId.EvergreenTile, UnlockMetric.Lines, UnlockScope.Total, 30, "Harvest 30 lines in total");
            Add("S", (int)UpgradeId.PrunerTile, UnlockMetric.Uprooted, UnlockScope.Run, 5, "Uproot 5 tiles in one run");
            Add("S", (int)UpgradeId.GlowTile, UnlockMetric.GoldenPlots, UnlockScope.Single, 5, "Have 5 golden plots on the garden at once");
            Add("S", (int)UpgradeId.BridgeTile, UnlockMetric.MergedGroups, UnlockScope.Single, 3, "Join 3 groups together with one seed");
            Add("S", (int)UpgradeId.SeedpodTile, UnlockMetric.GroupSize, UnlockScope.Single, 8, "Form a group of 8 tiles");
            Add("S", (int)UpgradeId.MimicTile, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 6, "Have 6 different upgrades on the garden at once");
            Add("S", (int)UpgradeId.DominantTile, UnlockMetric.SameColorStreak, UnlockScope.Single, 6, "Plant 6 seeds of the same color in a row");
            Add("S", (int)UpgradeId.HeritageTile, UnlockMetric.BagOneColor, UnlockScope.Single, 20, "Own 20 seeds of one color in your bag");
            Add("S", (int)UpgradeId.ScytheTile, UnlockMetric.Lines, UnlockScope.Single, 3, "Harvest 3 lines with a single seed");
            Add("S", (int)UpgradeId.TallyTile, UnlockMetric.Lines, UnlockScope.Round, 6, "Harvest 6 lines in one round");
            Add("S", (int)UpgradeId.SunbeamTile, UnlockMetric.ClearedWinterRound, UnlockScope.Run, 1, "Clear a round in Winter");
            Add("S", (int)UpgradeId.AntidoteTile, UnlockMetric.DefeatedBlightspore, UnlockScope.Total, 1, "Defeat Blightspore");
            Add("S", (int)UpgradeId.LullTile, UnlockMetric.DefeatedBramble, UnlockScope.Total, 1, "Defeat Bramble");
            Add("S", (int)UpgradeId.DazzleTile, UnlockMetric.DefeatedHaterBoss, UnlockScope.Total, 1, "Defeat a boss that hates a color or a shape");
            Add("S", (int)UpgradeId.WiltTile, UnlockMetric.DefeatedLeechOrVine, UnlockScope.Total, 1, "Defeat Root Leech or Creeping Vine");
            Add("S", (int)UpgradeId.CleanseTile, UnlockMetric.RoundClearedClean, UnlockScope.Run, 1, "Clear a round from round 3 on with no locked, frozen or blighted plot left");
            Add("S", (int)UpgradeId.CloneTile, UnlockMetric.BagSize, UnlockScope.Single, 30, "Own 30 seeds in your bag");
            Add("S", (int)UpgradeId.MirrorWardTile, UnlockMetric.BlightScored, UnlockScope.Total, 5, "Score through a blighted plot 5 times in total");
            Add("S", (int)UpgradeId.SeedsackTile, UnlockMetric.HandEmptied, UnlockScope.Run, 20, "Empty your hand 20 times in one run");
            Add("S", (int)UpgradeId.HeirloomTile, UnlockMetric.RunWon, UnlockScope.Total, 1, "Win a run");
            Add("S", (int)UpgradeId.AnchorTile, UnlockMetric.DefeatedResident, UnlockScope.Total, 1, "Defeat a challenge's resident enemy");
            Add("S", (int)UpgradeId.LateBloomTile, UnlockMetric.RoundClearedNoHarvest, UnlockScope.Run, 1, "Clear a round without harvesting a line");
        // Fertilizers (everything not listed is available from the first run)
            Add("F", (int)ModifierId.Prisme, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.MegaChaine, UnlockMetric.GroupSize, UnlockScope.Single, 12, "Form a group of 12 tiles");
            Add("F", (int)ModifierId.Forteresse, UnlockMetric.NeighborsTouched, UnlockScope.Single, 5, "Plant a seed touching 5 tiles");
            Add("F", (int)ModifierId.Architecte, UnlockMetric.SquarePlayed, UnlockScope.Run, 5, "Plant 5 square seeds in one run");
            Add("F", (int)ModifierId.Collectionneur, UnlockMetric.GardenColors, UnlockScope.Single, 3, "Have 3 colors on the garden at once");
            Add("F", (int)ModifierId.Tricolore, UnlockMetric.ColorsTouched, UnlockScope.Single, 2, "Plant a seed touching 2 other colors");
            Add("F", (int)ModifierId.Complementaire, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("F", (int)ModifierId.Ilot, UnlockMetric.Isolated, UnlockScope.Round, 3, "Plant 3 seeds that touch nothing in one round");
            Add("F", (int)ModifierId.Carrefour, UnlockMetric.NeighborsTouched, UnlockScope.Single, 4, "Plant a seed touching 4 tiles");
            Add("F", (int)ModifierId.Demolisseur, UnlockMetric.Lines, UnlockScope.Round, 5, "Harvest 5 lines in one round");
            Add("F", (int)ModifierId.CercleChromatique, UnlockMetric.ColorsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 other colors");
            Add("F", (int)ModifierId.Monochrome, UnlockMetric.SameColorStreak, UnlockScope.Single, 4, "Plant 4 seeds of the same color in a row");
            Add("F", (int)ModifierId.Contraste, UnlockMetric.NeighborsTouched, UnlockScope.Single, 3, "Plant a seed touching 3 tiles");
            Add("F", (int)ModifierId.Degrade, UnlockMetric.BiggerGroupStreak, UnlockScope.Single, 4, "Score a bigger group 4 placements in a row");
            Add("F", (int)ModifierId.Emmitouflee, UnlockMetric.GroupSize, UnlockScope.Single, 6, "Form a group of 6 tiles");
            Add("F", (int)ModifierId.Jardinier, UnlockMetric.GardenUpgraded, UnlockScope.Single, 3, "Have 3 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.ArcEnCiel, UnlockMetric.GardenColors, UnlockScope.Single, 4, "Have all 4 colors on the garden at once");
            Add("F", (int)ModifierId.Alternance, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 4, "Plant 4 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Palindrome, UnlockMetric.Lines, UnlockScope.Run, 6, "Harvest 6 lines in one run");
            Add("F", (int)ModifierId.Gradient, UnlockMetric.Lines, UnlockScope.Run, 12, "Harvest 12 lines in one run");
            Add("F", (int)ModifierId.Bloc, UnlockMetric.Lines, UnlockScope.Round, 3, "Harvest 3 lines in one round");
            Add("F", (int)ModifierId.MonochromeLigne, UnlockMetric.SameColorStreak, UnlockScope.Single, 3, "Plant 3 seeds of the same color in a row");
            Add("F", (int)ModifierId.SlotUn, UnlockMetric.SlotPlayed1, UnlockScope.Round, 10, "Play 10 seeds from hand slot 1 in one round");
            Add("F", (int)ModifierId.SlotDeux, UnlockMetric.SlotPlayed2, UnlockScope.Round, 10, "Play 10 seeds from hand slot 2 in one round");
            Add("F", (int)ModifierId.SlotTrois, UnlockMetric.SlotPlayed3, UnlockScope.Round, 10, "Play 10 seeds from hand slot 3 in one round");
            Add("F", (int)ModifierId.HorsNorme, UnlockMetric.SeedSize12, UnlockScope.Round, 10, "Play 10 seeds of 1 or 2 cells in one round");
            Add("F", (int)ModifierId.Solitaire, UnlockMetric.Isolated, UnlockScope.Run, 8, "Plant 8 seeds that touch nothing in one run");
            Add("F", (int)ModifierId.EspaceLibre, UnlockMetric.GardenFillPercentAtClear, UnlockScope.Single, 33, "Clear a round with the garden a third full or less", atMost: true);
            Add("F", (int)ModifierId.Rafale, UnlockMetric.HarvestStreak, UnlockScope.Single, 3, "Harvest a line on 3 placements in a row");
            Add("F", (int)ModifierId.Fraicheur, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 3, "Plant 3 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Pont, UnlockMetric.MergedGroups, UnlockScope.Single, 2, "Join 2 groups together with one seed");
            Add("F", (int)ModifierId.Encerclement, UnlockMetric.NeighborsTouched, UnlockScope.Single, 8, "Plant a seed touching 8 tiles");
            Add("F", (int)ModifierId.Boucher, UnlockMetric.NeighborsTouched, UnlockScope.Single, 7, "Plant a seed touching 7 tiles");
            Add("F", (int)ModifierId.GrosseFamille, UnlockMetric.GroupSize, UnlockScope.Single, 8, "Form a group of 8 tiles");
            Add("F", (int)ModifierId.Repetition, UnlockMetric.SameShapeStreak, UnlockScope.Single, 4, "Plant the same shape 4 times in a row");
            Add("F", (int)ModifierId.AlternancePieces, UnlockMetric.DifferentColorStreak, UnlockScope.Single, 6, "Plant 6 seeds in a row, each of a different color than the last");
            Add("F", (int)ModifierId.Combo, UnlockMetric.HarvestStreak, UnlockScope.Single, 2, "Harvest a line on 2 placements in a row");
            Add("F", (int)ModifierId.Precision, UnlockMetric.Seeds, UnlockScope.Round, 15, "Plant 15 seeds in one round");
            Add("F", (int)ModifierId.Surpopulation, UnlockMetric.NeighborsTouched, UnlockScope.Single, 6, "Plant a seed touching 6 tiles");
            Add("F", (int)ModifierId.Minimaliste, UnlockMetric.Isolated, UnlockScope.Run, 12, "Plant 12 seeds that touch nothing in one run");
            Add("F", (int)ModifierId.Joker, UnlockMetric.BagJokers, UnlockScope.Single, 2, "Own 2 Joker seeds at once");
            Add("F", (int)ModifierId.Densite, UnlockMetric.GardenTiles, UnlockScope.Single, 25, "Have 25 tiles on the garden at once");
            Add("F", (int)ModifierId.MultQuatre, UnlockMetric.RoundReached, UnlockScope.Single, 7, "Reach round 7");
            Add("F", (int)ModifierId.Solidarite, UnlockMetric.FertilizersHeld, UnlockScope.Single, 4, "Hold 4 fertilizers at once");
            Add("F", (int)ModifierId.Copieur, UnlockMetric.FertilizerCategories, UnlockScope.Single, 3, "Hold fertilizers of 3 different categories");
            Add("F", (int)ModifierId.MultCinqRisque, UnlockMetric.RoundReached, UnlockScope.Single, 4, "Reach round 4");
            Add("F", (int)ModifierId.CartesEnchantees, UnlockMetric.BagUpgraded, UnlockScope.Single, 8, "Have 8 upgraded seeds in your bag");
            Add("F", (int)ModifierId.Epuisement, UnlockMetric.RoundReached, UnlockScope.Single, 5, "Reach round 5");
            Add("F", (int)ModifierId.Multitude, UnlockMetric.BagSize, UnlockScope.Single, 40, "Own 40 seeds in your bag");
            Add("F", (int)ModifierId.Experience, UnlockMetric.SeedUpgraded, UnlockScope.Run, 30, "Play 30 upgraded seeds in one run");
            Add("F", (int)ModifierId.FormatPetitSpecialiste, UnlockMetric.SeedSize12, UnlockScope.Round, 15, "Play 15 seeds of 1 or 2 cells in one round");
            Add("F", (int)ModifierId.FormatMoyenSpecialiste, UnlockMetric.SeedSize3, UnlockScope.Round, 10, "Play 10 three-cell seeds in one round");
            Add("F", (int)ModifierId.FormatGrandSpecialiste, UnlockMetric.SeedSize4, UnlockScope.Round, 10, "Play 10 four-cell seeds in one round");
            Add("F", (int)ModifierId.FormatPetitGlow, UnlockMetric.SeedSize12, UnlockScope.Run, 40, "Play 40 seeds of 1 or 2 cells in one run");
            Add("F", (int)ModifierId.FormatMoyenGlow, UnlockMetric.SeedSize3, UnlockScope.Run, 30, "Play 30 three-cell seeds in one run");
            Add("F", (int)ModifierId.FormatGrandGlow, UnlockMetric.SeedSize4, UnlockScope.Run, 30, "Play 30 four-cell seeds in one run");
            Add("F", (int)ModifierId.Polyvalence, UnlockMetric.FertilizerCategories, UnlockScope.Single, 4, "Hold fertilizers of 4 different categories");
            Add("F", (int)ModifierId.CollectionChromatique, UnlockMetric.DevotionsHeld, UnlockScope.Single, 3, "Hold 3 Devotion fertilizers at once");
            Add("F", (int)ModifierId.Cadence, UnlockMetric.FertilizersHeld, UnlockScope.Single, 3, "Hold 3 fertilizers at once");
            Add("F", (int)ModifierId.Echo, UnlockMetric.BagUpgradeKinds, UnlockScope.Single, 4, "Have 4 different upgrades in your bag");
            Add("F", (int)ModifierId.QuatriemeEmplacement, UnlockMetric.RunWon, UnlockScope.Total, 1, "Win a run");
            Add("F", (int)ModifierId.Barometre, UnlockMetric.RoundClearedWeather, UnlockScope.Run, 3, "Clear 3 rounds with weather in one run");
            Add("F", (int)ModifierId.ChasseurTempete, UnlockMetric.RoundClearedHarshWeather, UnlockScope.Total, 1, "Clear a round in Frost, Heatwave or Hurricane");
            Add("F", (int)ModifierId.Impermeable, UnlockMetric.RoundClearedWeather, UnlockScope.Total, 6, "Clear 6 rounds with weather in total");
            Add("F", (int)ModifierId.Hate, UnlockMetric.SeedsLeftAtClear, UnlockScope.Single, 7, "Clear a round with 7 seeds left");
            Add("F", (int)ModifierId.PierreAngulaire, UnlockMetric.Corners, UnlockScope.Run, 8, "Plant 8 tiles on corners in one run");
            Add("F", (int)ModifierId.Coeur, UnlockMetric.Middle, UnlockScope.Run, 5, "Plant on the middle of the garden 5 times in one run");
            Add("F", (int)ModifierId.Treillis, UnlockMetric.VerticalSeed, UnlockScope.Round, 10, "Play 10 vertical seeds in one round");
            Add("F", (int)ModifierId.Sillon, UnlockMetric.HorizontalSeed, UnlockScope.Round, 10, "Play 10 horizontal seeds in one round");
            Add("F", (int)ModifierId.Haricot, UnlockMetric.Straight3, UnlockScope.Round, 5, "Play 5 straight seeds of 3 or more tiles in one round");
            Add("F", (int)ModifierId.Racines, UnlockMetric.GroupSize, UnlockScope.Single, 9, "Form a group of 9 tiles");
            Add("F", (int)ModifierId.Emboite, UnlockMetric.NeighborsTouched, UnlockScope.Single, 5, "Plant a seed touching 5 tiles");
            Add("F", (int)ModifierId.Mosaique, UnlockMetric.ColorsTouched, UnlockScope.Single, 2, "Plant a seed touching 2 other colors");
            Add("F", (int)ModifierId.Foule, UnlockMetric.GardenFillPercent, UnlockScope.Single, 66, "Have two thirds of the garden planted at once");
            Add("F", (int)ModifierId.Prairie, UnlockMetric.GardenTiles, UnlockScope.Single, 28, "Have 28 tiles on the garden at once");
            Add("F", (int)ModifierId.Guirlande, UnlockMetric.NeighborsTouched, UnlockScope.Single, 6, "Plant a seed touching 6 tiles");
            Add("F", (int)ModifierId.MoissonCroisee, UnlockMetric.Lines, UnlockScope.Single, 2, "Harvest 2 lines with a single seed");
            Add("F", (int)ModifierId.TableRase, UnlockMetric.GardenCleared, UnlockScope.Run, 1, "Harvest every tile of the garden at once");
            Add("F", (int)ModifierId.BriseGlace, UnlockMetric.FrozenBroken, UnlockScope.Total, 1, "Break a frozen plot with a line");
            Add("F", (int)ModifierId.Saisons, UnlockMetric.RunWonSpring, UnlockScope.Total, 1, "Win a Spring run");
            Add("F", (int)ModifierId.CoupFinal, UnlockMetric.EnemyDefeatedSeedsLeft, UnlockScope.Single, 3, "Defeat an enemy with 3 seeds left");
            Add("F", (int)ModifierId.DerniereGraine, UnlockMetric.RoundClearedOnLastSeed, UnlockScope.Run, 1, "Clear a round on its last seed");
            Add("F", (int)ModifierId.PremiereGraine, UnlockMetric.HarvestOnFirstSeedOfRound, UnlockScope.Run, 1, "Harvest a line with the first seed of a round");
            Add("F", (int)ModifierId.Tirelire, UnlockMetric.Pollen, UnlockScope.Single, 60, "Hold 60 Pollen at once");
            Add("F", (int)ModifierId.BanqueGraines, UnlockMetric.BagSize, UnlockScope.Single, 25, "Own 25 seeds in your bag");
            Add("F", (int)ModifierId.Rustique, UnlockMetric.RoundClearedFrost, UnlockScope.Run, 1, "Clear a round in Frost");
            Add("F", (int)ModifierId.CavalierVent, UnlockMetric.RoundClearedWind, UnlockScope.Run, 1, "Clear a round in Wind");
            Add("F", (int)ModifierId.Marathonien, UnlockMetric.RoundReached, UnlockScope.Single, 10, "Reach round 10");
            Add("F", (int)ModifierId.Pourfendeur, UnlockMetric.DefeatedBoss, UnlockScope.Total, 1, "Defeat a boss");
            Add("F", (int)ModifierId.CharmedPatch, UnlockMetric.TraitCells, UnlockScope.Single, 4, "Score 4 upgraded tiles in the same group");
            Add("F", (int)ModifierId.TwinGraft, UnlockMetric.SeedTwoUpgrades, UnlockScope.Run, 1, "Play a seed with 2 upgrades");
            Add("F", (int)ModifierId.Botanist, UnlockMetric.GardenUpgradeKinds, UnlockScope.Single, 5, "Have 5 different upgrades on the garden at once");
            Add("F", (int)ModifierId.Hothouse, UnlockMetric.GardenUpgraded, UnlockScope.Single, 6, "Have 6 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.GoodNeighbors, UnlockMetric.GardenUpgraded, UnlockScope.Single, 5, "Have 5 upgraded tiles on the garden at once");
            Add("F", (int)ModifierId.Gleaner, UnlockMetric.Lines, UnlockScope.Run, 8, "Harvest 8 lines in one run");
            Add("F", (int)ModifierId.WildCluster, UnlockMetric.BagJokers, UnlockScope.Single, 2, "Own 2 Joker seeds at once");
            Add("F", (int)ModifierId.WildReserve, UnlockMetric.BagJokers, UnlockScope.Single, 3, "Own 3 Joker seeds at once");
            Add("F", (int)ModifierId.WildSurge, UnlockMetric.BagJokers, UnlockScope.Single, 4, "Own 4 Joker seeds at once");
            Add("F", (int)ModifierId.CropRotation, UnlockMetric.ShapeChanges, UnlockScope.Round, 8, "Change the shape you plant 8 times in one round");
            Add("F", (int)ModifierId.GreenhouseLamp, UnlockMetric.ClearedGreenhouseRound, UnlockScope.Total, 1, "Clear a round in Greenhouse");
            Add("F", (int)ModifierId.SeedLibrary, UnlockMetric.BagShapes, UnlockScope.Single, 8, "Own all 8 shapes in your bag at once");
            Add("F", (int)ModifierId.TwinBloom, UnlockMetric.SameColorStreak, UnlockScope.Single, 5, "Plant 5 seeds of the same color in a row");
            Add("F", (int)ModifierId.Outsider, UnlockMetric.EnemiesDefeated, UnlockScope.Total, 5, "Defeat 5 enemies in total");
        }
    }
}
