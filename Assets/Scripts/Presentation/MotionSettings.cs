namespace Contigu.Presentation
{
    /// <summary>"Reduced motion" preference (Settings): the shop's pops, pulses, swells and shakes are skipped; feedback stays readable as text and state.</summary>
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
