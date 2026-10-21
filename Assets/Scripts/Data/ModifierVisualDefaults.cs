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
            { ModifierId.Haie, "HG" },
            { ModifierId.Coeur, "HW" },
            { ModifierId.Treillis, "TR" },
            { ModifierId.Sillon, "FU" },
            { ModifierId.Haricot, "BS" },
            { ModifierId.Patchwork, "PW" },
            { ModifierId.Racines, "TP" },
            { ModifierId.Emboite, "SF" },
            { ModifierId.Mosaique, "MS" },
            { ModifierId.Foule, "CW" },
            { ModifierId.Prairie, "WM" },
            { ModifierId.Guirlande, "WR" },
            { ModifierId.Pollinisateur, "PL" },
            { ModifierId.Miel, "HY" },
            { ModifierId.Nectar, "NC" },
            { ModifierId.MoissonCroisee, "XH" },
            { ModifierId.Faucheur, "RP" },
            { ModifierId.TableRase, "CL" },
            { ModifierId.Dime, "TT" },
            { ModifierId.CultureRang, "RW" },
            { ModifierId.CultureColonne, "CU" },
            { ModifierId.Prime, "BN" },
            { ModifierId.BriseGlace, "FB" },
            { ModifierId.Saisons, "SE" },
            { ModifierId.Outsider, "UD" },
            { ModifierId.CoupFinal, "FN" },
            { ModifierId.DerniereGraine, "LS" },
            { ModifierId.PremiereGraine, "FI" },
            { ModifierId.Tirelire, "PB" },
            { ModifierId.BanqueGraines, "SK" },
            { ModifierId.SousBois, "UB" },
            { ModifierId.DanseDePluie, "RN" },
            { ModifierId.DanseDuSoleil, "SN" },
            { ModifierId.Rustique, "FH" },
            { ModifierId.CavalierVent, "WD" },
            { ModifierId.Matinal, "EA" },
            { ModifierId.Marathonien, "MR" },
            { ModifierId.Pourfendeur, "BB" },
            { ModifierId.Tonneau, "RB" },
            { ModifierId.Grange, "GN" },
            { ModifierId.Barometre, "BR" },
            { ModifierId.ChasseurTempete, "SC" },
            { ModifierId.Impermeable, "WP" },
            { ModifierId.Epines, "TH" },
            { ModifierId.Exterminateur, "PC" },
            { ModifierId.Hate, "HA" },
            { ModifierId.Tourbillon, "WW" },
            { ModifierId.PierreAngulaire, "CS" },
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

        /// <summary>The player-facing name of a fertilizer's group (shown in its tooltip).</summary>
        public static string GetCategoryLabel(ModifierCategory category)
        {
            switch (category)
            {
                case ModifierCategory.Couleurs: return "Colors";
                case ModifierCategory.Voisinage: return "Neighbors";
                case ModifierCategory.Connexions: return "Connections";
                case ModifierCategory.Destruction: return "Destruction";
                case ModifierCategory.Roguelike: return "Run";
                case ModifierCategory.Formes: return "Shapes";
                default: return category.ToString();
            }
        }

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
            var sack = GetSackIcon(id);
            if (sack != null)
            {
                return sack;
            }
            return Icons.TryGetValue(id, out var sprite) ? sprite : null;
        }

        private static readonly Dictionary<ModifierId, Sprite> SackIcons = new Dictionary<ModifierId, Sprite>();

        /// <summary>
        /// The fertilizer's own burlap-sack icon (Resources/Icons/Fertilizers/&lt;ModifierId&gt;.png) — a sack silhouette
        /// that already carries its own background and effect pill, so it replaces the category-colored chip.
        /// Null if the art is missing.
        /// </summary>
        public static Sprite GetSackIcon(ModifierId id)
        {
            if (!SackIcons.TryGetValue(id, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Icons/Fertilizers/" + id);
                SackIcons[id] = sprite;
            }
            return sprite;
        }
    }
}
