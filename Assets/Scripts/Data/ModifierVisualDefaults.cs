using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for modifier badges. A modifier without bespoke icon
    /// art falls back to a colored chip (accent = its <see cref="ModifierCategory"/>)
    /// showing a 2-letter abbreviation; the full name/description shows in a
    /// hover tooltip (see Presentation.TooltipView). Icon art is loaded via
    /// <see cref="GetIcon"/>, which returns null for any modifier without art yet.
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
            { ModifierId.Collectionneur, "CO" },
            { ModifierId.Tricolore, "TC" },
            { ModifierId.Complementaire, "CX" },
            { ModifierId.Ilot, "IL" },
            { ModifierId.Couronne, "CR" },
            { ModifierId.Carrefour, "XR" },
            { ModifierId.Macon, "MA" },
            { ModifierId.Demolisseur, "DM" },
            { ModifierId.CercleChromatique, "CC" },
            { ModifierId.Monochrome, "MO" },
            { ModifierId.Contraste, "CN" },
            { ModifierId.Degrade, "DG" },
            { ModifierId.Emmitouflee, "EM" },
            { ModifierId.Jardinier, "JA" },
            { ModifierId.ArcEnCiel, "AC" },
            { ModifierId.Alternance, "AL" },
            { ModifierId.Palindrome, "PA" },
            { ModifierId.Gradient, "GR" },
            { ModifierId.Bloc, "BL" },
            { ModifierId.MonochromeLigne, "ML" },
            { ModifierId.DevotionCoral, "OC" },
            { ModifierId.DevotionTeal, "OT" },
            { ModifierId.DevotionViolet, "OV" },
            { ModifierId.DevotionLime, "OL" },
            { ModifierId.SlotUn, "1S" },
            { ModifierId.SlotDeux, "2S" },
            { ModifierId.SlotTrois, "3S" },
            { ModifierId.GrandFormat, "GF" },
            { ModifierId.HorsNorme, "HN" },
            { ModifierId.EclatCoral, "EC" },
            { ModifierId.EclatTeal, "ET" },
            { ModifierId.EclatViolet, "EV" },
            { ModifierId.EclatLime, "EL" },
            { ModifierId.Diagonale, "DI" },
            { ModifierId.Nid, "NI" },
            { ModifierId.Solitaire, "SO" },
            { ModifierId.EspaceLibre, "SL" },
            { ModifierId.Rafale, "RA" },
            { ModifierId.PetitFormat, "PF" },
            { ModifierId.Fraicheur, "FR" },
            { ModifierId.Pont, "PT" },
            { ModifierId.Encerclement, "EN" },
            { ModifierId.Boucher, "BO" },
            { ModifierId.GrosseFamille, "GX" },
            { ModifierId.Repetition, "RE" },
            { ModifierId.AlternancePieces, "AP" },
            { ModifierId.Combo, "CB" },
            { ModifierId.Precision, "PC" },
            { ModifierId.Surpopulation, "SP" },
            { ModifierId.Minimaliste, "MN" },
            { ModifierId.Joker, "JK" },
            { ModifierId.Densite, "DS" },
            { ModifierId.ArcEnCielLueur, "RG" },
            { ModifierId.AlternanceLueur, "GA" },
            { ModifierId.MonochromeLigneLueur, "RD" },
            { ModifierId.CollectionneurLueur, "GC" },
            { ModifierId.RepetitionLueur, "GL" },
            { ModifierId.MultUn, "M1" },
            { ModifierId.MultDeux, "M2" },
            { ModifierId.MultQuatre, "M4" },
            { ModifierId.Solidarite, "SD" },
            { ModifierId.Copieur, "CP" },
            { ModifierId.MultCinqRisque, "M5" },
            { ModifierId.CartesEnchantees, "CE" },
            { ModifierId.Epuisement, "EP" },
            { ModifierId.Multitude, "MT" },
            { ModifierId.Experience, "XP" },
            // Format* size tiers cover several shapes at once, so no single
            // silhouette represents them; falls back to this abbreviation instead.
            { ModifierId.FormatPetitSpecialiste, "F1" },
            { ModifierId.FormatMoyenSpecialiste, "F2" },
            { ModifierId.FormatGrandSpecialiste, "F3" },
            { ModifierId.FormatPetitGlow, "G1" },
            { ModifierId.FormatMoyenGlow, "G2" },
            { ModifierId.FormatGrandGlow, "G3" },
            { ModifierId.Pair, "PI" },
            { ModifierId.Impair, "IM" },
            { ModifierId.Polyvalence, "PV" },
            { ModifierId.RenfortJoker, "RJ" },
            { ModifierId.Arsenal, "AS" },
            { ModifierId.CollectionChromatique, "CC" },
            { ModifierId.Cadence, "CD" },
            { ModifierId.Echo, "EC" },
            { ModifierId.Siphon, "SI" }
        };

        // One accent per category — kept as its own literal set here rather than
        // referencing Presentation.UITheme, since Data must not depend on Presentation.
        private static readonly Dictionary<ModifierCategory, Color> CategoryColors = new Dictionary<ModifierCategory, Color>
        {
            { ModifierCategory.Couleurs, new Color(0.941f, 0.702f, 0.553f) }, // #f0b38d
            { ModifierCategory.Voisinage, new Color(0.396f, 0.682f, 0.839f) }, // #65aed6
            { ModifierCategory.Connexions, new Color(0.643f, 0.922f, 0.800f) }, // #a4ebcc
            { ModifierCategory.Destruction, new Color(0.710f, 0.427f, 0.498f) }, // #b56d7f
            { ModifierCategory.Roguelike, new Color(0.216f, 0.180f, 0.302f) }, // #372e4d
            { ModifierCategory.Formes, new Color(0.380f, 0.263f, 0.388f) } // #614363
        };

        public static string GetAbbreviation(ModifierId id)
        {
            return Abbreviations.TryGetValue(id, out var s) ? s : "?";
        }

        public static Color GetCategoryColor(ModifierCategory category)
        {
            return CategoryColors.TryGetValue(category, out var c) ? c : Color.gray;
        }

        // Éclat and Devotion are both per-color families, so each shows an
        // actual colored tile instead of an opaque 2-letter code.
        private static readonly Dictionary<ModifierId, PieceColor> ColorTileColors = new Dictionary<ModifierId, PieceColor>
        {
            { ModifierId.EclatCoral, PieceColor.Coral },
            { ModifierId.EclatTeal, PieceColor.Teal },
            { ModifierId.EclatViolet, PieceColor.Violet },
            { ModifierId.EclatLime, PieceColor.Lime },
            { ModifierId.DevotionCoral, PieceColor.Coral },
            { ModifierId.DevotionTeal, PieceColor.Teal },
            { ModifierId.DevotionViolet, PieceColor.Violet },
            { ModifierId.DevotionLime, PieceColor.Lime }
        };

        /// <summary>The color an Éclat/Devotion (per-color) modifier is about, or null for every other modifier.</summary>
        public static PieceColor? GetColorTileColor(ModifierId id)
        {
            return ColorTileColors.TryGetValue(id, out var color) ? color : (PieceColor?)null;
        }

        // One PNG per modifier, named after its own ModifierDefinition.Name with
        // spaces stripped (e.g. "Mega Chain" -> MegaChain.png). Resources.Load
        // returns null for any modifier without art yet rather than throwing,
        // so GetIcon below can be called for every modifier unconditionally.
        private static readonly Dictionary<ModifierId, Sprite> Icons = new Dictionary<ModifierId, Sprite>
        {
            { ModifierId.Prisme, Resources.Load<Sprite>("Icons/Modifiers/Prism") },
            { ModifierId.Chaine, Resources.Load<Sprite>("Icons/Modifiers/Chain") },
            { ModifierId.MegaChaine, Resources.Load<Sprite>("Icons/Modifiers/MegaChain") },
            { ModifierId.Forteresse, Resources.Load<Sprite>("Icons/Modifiers/Fortress") },
            { ModifierId.Prisonnier, Resources.Load<Sprite>("Icons/Modifiers/Prisoner") },
            { ModifierId.Architecte, Resources.Load<Sprite>("Icons/Modifiers/Architect") },
            { ModifierId.Collectionneur, Resources.Load<Sprite>("Icons/Modifiers/Collector") },
            { ModifierId.Tricolore, Resources.Load<Sprite>("Icons/Modifiers/Tricolor") }
        };

        /// <summary>Real icon art for a modifier, or null when none exists yet — takes priority over every placeholder treatment below (Forme* specialist shape, Éclat/Devotion color tile, plain abbreviation) in <see cref="Presentation.ModifierBadgeFactory"/>.</summary>
        public static Sprite GetIcon(ModifierId id)
        {
            return Icons.TryGetValue(id, out var sprite) ? sprite : null;
        }
    }
}
