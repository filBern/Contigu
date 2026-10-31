using Contigu.Core;

namespace Contigu.Presentation
{
    /// <summary>
    /// The extra italic line under a tile upgrade's tooltip text for the upgrades whose score is already known before they are planted
    /// (Scholar Sprout and friends, see RunManager.GetTileUpgradePreviewText). Reads the live run through <see cref="Provider"/>.
    /// </summary>
    public static class TileUpgradePreview
    {
        /// <summary>Set by GameBootstrap to the current run; null before any run exists.</summary>
        public static System.Func<PieceTraitKind, string> Provider;

        public static string Append(string body, PieceTraitKind kind)
        {
            string preview = Provider != null ? Provider(kind) : null;
            if (string.IsNullOrEmpty(preview))
            {
                return body;
            }
            return body + "\n<i>" + DescriptionTextFormatter.Colorize(preview, 14) + "</i>";
        }

        public static string Append(string body, UpgradeId id)
        {
            var kind = UpgradeSystem.TraitKindFor(id);
            return kind.HasValue ? Append(body, kind.Value) : body;
        }
    }
}
