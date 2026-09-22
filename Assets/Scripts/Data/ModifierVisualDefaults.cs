using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Display defaults for modifier badges. Most modifiers still have no
    /// bespoke icon art, so their badge falls back to a colored chip (accent
    /// = its <see cref="ModifierCategory"/>) showing a 2-letter abbreviation
    /// instead of a picture — the full name/description only shows in a
    /// hover tooltip (see Presentation.TooltipView). The first 9 catalog
    /// entries (Prisme..Tricolore) DO have real art now (player-supplied
    /// PNGs dropped into Assets/Resources/Icons/Modifiers, named after each
    /// one's own <see cref="ModifierDefinition.Name"/> with spaces
    /// stripped), loaded via <see cref="GetIcon"/> — same null-means-no-icon-
    /// yet fallback convention as VisualDefaults.GetColorIcon. Adding art
    /// for another modifier later only needs one more entry in the
    /// <see cref="Icons"/> dictionary below.
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
            { ModifierId.FormeSingle, "S1" },
            { ModifierId.FormeDomH, "SH" },
            { ModifierId.FormeDomV, "SV" },
            { ModifierId.FormeTriL, "S3" },
            { ModifierId.FormeTriIH, "S4" },
            { ModifierId.FormeTriIV, "S5" },
            { ModifierId.FormeSq2, "SQ" },
            { ModifierId.FormeLTetro, "S6" },
            { ModifierId.FormeTTetro, "S7" },
            { ModifierId.FormeSTetro, "S8" },
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
            { ModifierId.Synergie, "SY" },
            { ModifierId.Densite, "DS" }
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

        private static readonly Dictionary<ModifierId, ShapeId> SpecialistShapes = new Dictionary<ModifierId, ShapeId>
        {
            { ModifierId.FormeSingle, ShapeId.Single },
            { ModifierId.FormeDomH, ShapeId.DomH },
            { ModifierId.FormeDomV, ShapeId.DomV },
            { ModifierId.FormeTriL, ShapeId.TriL },
            { ModifierId.FormeTriIH, ShapeId.TriIH },
            { ModifierId.FormeTriIV, ShapeId.TriIV },
            { ModifierId.FormeSq2, ShapeId.Sq2 },
            { ModifierId.FormeLTetro, ShapeId.LTetro },
            { ModifierId.FormeTTetro, ShapeId.TTetro },
            { ModifierId.FormeSTetro, ShapeId.STetro }
        };

        /// <summary>The shape a Forme* "Specialist" modifier targets, or null for every other modifier — lets the badge show an actual shape preview instead of naming a domino/tromino/tetromino (see Presentation.ModifierBadgeFactory, on explicit request: "je n'aime pas qu'on ait les nom des tetromino").</summary>
        public static ShapeId? GetSpecialistShape(ModifierId id)
        {
            return SpecialistShapes.TryGetValue(id, out var shape) ? shape : (ShapeId?)null;
        }

        // Originally just the 4 "Glow" (Éclat) modifiers — extended to the 4
        // "Devotion" ones too (explicit request: "Violet devotion manque le
        // preview single piece comme icon de modifier", i.e. Devotion was
        // missing the same treatment its Éclat sibling already got) since
        // both families are per-color and read exactly the same way: an
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

        /// <summary>The color an Éclat/Devotion (per-color) modifier is about, or null for every other modifier — same idea as <see cref="GetSpecialistShape"/>: the badge shows an actual colored tile instead of an opaque 2-letter code, on explicit request ("Coral glow et les 3 autres du genre, on peut mettre l'icon d'une simple tuile... ce sera rapidement clair", later extended to Devotion the same way).</summary>
        public static PieceColor? GetColorTileColor(ModifierId id)
        {
            return ColorTileColors.TryGetValue(id, out var color) ? color : (PieceColor?)null;
        }

        // Real per-modifier icon art (player-authored, on explicit request:
        // "J'ai fait un icon pour les 9 premier modifiers, je les ai nommé
        // par leur nom dans le dossier Assets/Resources/Icons/Modifiers") —
        // one PNG per modifier, named after its own ModifierDefinition.Name
        // with spaces stripped (e.g. "Mega Chain" -> MegaChain.png). Loaded
        // once here, same lazy-at-startup convention as VisualDefaults'
        // IconMap; Resources.Load returns null for any modifier without art
        // yet rather than throwing, so GetIcon below can be called for every
        // modifier unconditionally.
        private static readonly Dictionary<ModifierId, Sprite> Icons = new Dictionary<ModifierId, Sprite>
        {
            { ModifierId.Prisme, Resources.Load<Sprite>("Icons/Modifiers/Prism") },
            { ModifierId.Chaine, Resources.Load<Sprite>("Icons/Modifiers/Chain") },
            { ModifierId.MegaChaine, Resources.Load<Sprite>("Icons/Modifiers/MegaChain") },
            { ModifierId.Forteresse, Resources.Load<Sprite>("Icons/Modifiers/Fortress") },
            { ModifierId.Prisonnier, Resources.Load<Sprite>("Icons/Modifiers/Prisoner") },
            { ModifierId.Architecte, Resources.Load<Sprite>("Icons/Modifiers/Architect") },
            { ModifierId.Puriste, Resources.Load<Sprite>("Icons/Modifiers/Purist") },
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
