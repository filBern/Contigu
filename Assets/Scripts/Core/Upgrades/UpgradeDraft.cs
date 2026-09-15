namespace Contigu.Core
{
    /// <summary>
    /// A single draft offer of exactly 3 options: one guaranteed Bank pick, one
    /// guaranteed Grid pick, one random pick from either pool (spec 5.2).
    /// </summary>
    public sealed class UpgradeDraft
    {
        public readonly UpgradeDefinition[] Options;

        public UpgradeDraft(UpgradeDefinition[] options)
        {
            Options = options;
        }
    }
}
