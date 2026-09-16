using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for modifier badges. There is no bespoke icon art for
    /// any modifier yet, so each badge is a colored chip (accent = its
    /// <see cref="ModifierCategory"/>) showing a 2-letter abbreviation instead
    /// of a picture — the full name/description only shows in a hover tooltip
    /// (see Presentation.TooltipView). Swapping in real per-modifier art later
    /// only needs a sprite lookup added alongside GetAbbreviation, the same way
    /// VisualDefaults.GetColorIcon falls back cleanly for a color with no icon.
    /// </summary>
    public static class ModifierVisualDefaults
    {
        private static readonly Dictionary<ModifierId, string> Abbreviations = new Dictionary<ModifierId, string>
        {
            { ModifierId.Prisme, "PR" },
            { ModifierId.Chaine, "CH" },
            { ModifierId.MegaChaine, "MC" },
            { ModifierId.Forteresse, "FT" },
            { ModifierId.Prisonnier, "PN" },
            { ModifierId.Architecte, "AR" },
            { ModifierId.Puriste, "PU" },
            { ModifierId.Collectionneur, "CO" },
            { ModifierId.Tricolore, "TC" },
            { ModifierId.Complementaire, "CX" },
            { ModifierId.Ilot, "IL" },
            { ModifierId.Couronne, "CR" },
            { ModifierId.TrouDansLaGrille, "HL" },
            { ModifierId.Carrefour, "XR" },
            { ModifierId.Macon, "MA" },
            { ModifierId.Demolisseur, "DM" }
        };

        // One accent per category, reusing the same v1 8-color palette as
        // VisualDefaults/UITheme — kept as its own literal set here rather than
        // referencing Presentation.UITheme, since Data must not depend on
        // Presentation (Contigu.Core / Contigu.Data / Contigu.Presentation are
        // separate assemblies with dependencies flowing one way — see README).
        private static readonly Dictionary<ModifierCategory, Color> CategoryColors = new Dictionary<ModifierCategory, Color>
        {
            { ModifierCategory.Couleurs, new Color(0.941f, 0.702f, 0.553f) }, // #f0b38d
            { ModifierCategory.Voisinage, new Color(0.396f, 0.682f, 0.839f) }, // #65aed6
            { ModifierCategory.Connexions, new Color(0.643f, 0.922f, 0.800f) }, // #a4ebcc
            { ModifierCategory.Destruction, new Color(0.710f, 0.427f, 0.498f) }, // #b56d7f
            { ModifierCategory.Roguelike, new Color(0.216f, 0.180f, 0.302f) } // #372e4d
        };

        public static string GetAbbreviation(ModifierId id)
        {
            return Abbreviations.TryGetValue(id, out var s) ? s : "?";
        }

        public static Color GetCategoryColor(ModifierCategory category)
        {
            return CategoryColors.TryGetValue(category, out var c) ? c : Color.gray;
        }
    }
}
