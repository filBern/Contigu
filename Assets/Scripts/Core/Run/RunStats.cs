using System.Globalization;

namespace Contigu.Core
{
    /// <summary>What happened over a whole run, for the end screens (and kept in the save so a resumed run keeps counting).</summary>
    public sealed class RunStats
    {
        public int Placements;
        public int BestPlacementScore;
        public int BestPlacementRound;
        public ShapeId BestPlacementShape;
        public PieceColor BestPlacementColor;
        /// <summary>The highest Mult any placement reached, times 100 (so it saves as an integer).</summary>
        public int BestMultTimes100;
        public int BestGroupSize;
        public int LinesHarvested;
        public int BestHarvestLines;
        public int PollenFromPlacements;
        public int ShufflesUsed;
        public int UndosUsed;
        public int RerollsUsed;
        public int MaxFertilizersHeld;

        public float BestMult { get { return BestMultTimes100 / 100f; } }

        /// <summary>Folds one finished placement into the run's records.</summary>
        public void Record(PlacementResult placement, int roundNumber, ShapeId shape, PieceColor color)
        {
            Placements++;
            if (placement.TotalScore > BestPlacementScore)
            {
                BestPlacementScore = placement.TotalScore;
                BestPlacementRound = roundNumber;
                BestPlacementShape = shape;
                BestPlacementColor = color;
            }
            int mult = (int)System.Math.Round(placement.Mult * 100f);
            if (mult > BestMultTimes100) { BestMultTimes100 = mult; }
            int group = placement.GroupCells != null ? placement.GroupCells.Count : 0;
            if (group > BestGroupSize) { BestGroupSize = group; }
            LinesHarvested += placement.ClearedLineCount;
            if (placement.ClearedLineCount > BestHarvestLines) { BestHarvestLines = placement.ClearedLineCount; }
            PollenFromPlacements += placement.LueurEarned + placement.ModifierLueurBonus;
        }

        public string ToText()
        {
            return string.Join(",", new[]
            {
                Placements, BestPlacementScore, BestPlacementRound, (int)BestPlacementShape, (int)BestPlacementColor, BestMultTimes100,
                BestGroupSize, LinesHarvested, BestHarvestLines, PollenFromPlacements, ShufflesUsed, UndosUsed, RerollsUsed, MaxFertilizersHeld
            }.ToStrings());
        }

        public static RunStats FromText(string text)
        {
            var stats = new RunStats();
            if (string.IsNullOrEmpty(text)) { return stats; }
            var f = text.Split(',');
            int At(int i) { return i < f.Length && int.TryParse(f[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0; }
            stats.Placements = At(0);
            stats.BestPlacementScore = At(1);
            stats.BestPlacementRound = At(2);
            stats.BestPlacementShape = (ShapeId)At(3);
            stats.BestPlacementColor = (PieceColor)At(4);
            stats.BestMultTimes100 = At(5);
            stats.BestGroupSize = At(6);
            stats.LinesHarvested = At(7);
            stats.BestHarvestLines = At(8);
            stats.PollenFromPlacements = At(9);
            stats.ShufflesUsed = At(10);
            stats.UndosUsed = At(11);
            stats.RerollsUsed = At(12);
            stats.MaxFertilizersHeld = At(13);
            return stats;
        }
    }

    internal static class RunStatsText
    {
        public static string[] ToStrings(this int[] values)
        {
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++) { result[i] = values[i].ToString(CultureInfo.InvariantCulture); }
            return result;
        }
    }

    /// <summary>How one cleared round went, shown between the round and the shop.</summary>
    public struct RoundSummary
    {
        public int RoundNumber;
        public int Score;
        public int Placements;
        public int BestPlacementScore;
        public int LinesHarvested;
        public int PollenFromPlacements;
        public int SeedsLeft;
        public int EndOfRoundPollen;
        public int StarsEarned;
    }
}
