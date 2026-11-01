namespace Contigu.Presentation
{
    /// <summary>"Reduced motion" preference (Settings): the shop's pops, the hit-stop, the combo and button pulses, the choice-screen lifts and the decorative sparkles are skipped; feedback stays readable as text and state.</summary>
    public static class MotionSettings
    {
        private const string Key = "ReducedMotion";

        public static bool Reduced
        {
            get { return GamePrefs.GetInt(Key, 0) == 1; }
            set
            {
                GamePrefs.SetInt(Key, value ? 1 : 0);
                GamePrefs.Save();
            }
        }
    }
}
