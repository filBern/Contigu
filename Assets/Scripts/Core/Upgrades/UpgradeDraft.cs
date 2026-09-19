namespace Contigu.Core
{
    /// <summary>
    /// A single draft offer of exactly 3 options, Bank and Grid pools mixed
    /// together: one guaranteed Bank pick, one guaranteed Grid pick, one random
    /// pick from either pool. The player picks exactly one.
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
