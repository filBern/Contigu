namespace Contigu.Core
{
    /// <summary>
    /// The difficulty levels of a challenge: winning at level N opens level N+1. Each level adds one rule on top of the previous ones.
    /// 1 Normal; 2 enemies +15% HP; 3 one Shuffle less; 4 enemies +15% HP again and one seed less per round; 5 fertilizers cost +1 Pollen.
    /// </summary>
    public static class DifficultyLevels
    {
        public const int Max = 5;

        public static string Describe(int level)
        {
            switch (level)
            {
                case 1: return "Normal.";
                case 2: return "Enemies have 15% more HP.";
                case 3: return "One Shuffle less per run.";
                case 4: return "Enemies have 15% more HP again, and one seed less each round.";
                case 5: return "Fertilizers cost 1 more Pollen.";
                default: return string.Empty;
            }
        }
    }
}
