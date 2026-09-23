using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Data
{
    /// <summary>
    /// Built-in display defaults (colors and names) so the game is fully playable
    /// without anyone having to create <see cref="PieceColorDatabase"/> /
    /// <see cref="PieceShapeDatabase"/> assets in the editor first. A database
    /// asset, if assigned on <c>GameBootstrap</c>, overrides these per-entry.
    ///
    /// The 4 base piece colors (on explicit request — a graphical overhaul of
    /// the grid/tiles, "les 4 types de tuiles deviennent rouge, bleu, vert et
    /// jaune") are now a fixed red/blue/green/yellow set instead of the v1
    /// 8-color palette's own hues — chosen for the closest match to each
    /// color's old hue (Coral's orange-red → Red, Teal's blue → Blue, Lime's
    /// green → Green, Violet left over → Yellow) so a player's existing sense
    /// of "which piece is which" carries over as much as possible. The
    /// underlying <see cref="PieceColor"/> enum names (Coral/Teal/Violet/Lime)
    /// are untouched — renaming those would ripple through ~100 modifier ids
    /// (DevotionCoral, EclatTeal, ...) for a purely cosmetic change — only
    /// their rendered <see cref="ColorMap"/> value and displayed
    /// <see cref="ColorNames"/> string changed. Joker's own color went
    /// through 2 more rounds after that, on further explicit reports —
    /// first "il se blend vraiment trop avec un fond gris" (its original
    /// mid-tone slate, #5f699c, is the exact same hex as Presentation.
    /// UITheme.PanelLight/ButtonIdle chrome, so it read as barely-there
    /// against any gray/purple UI surface), tried a near-black (#1b1b1b)
    /// next — then "le noir ne fonctionne pas mieux" (dark UI chrome/
    /// outlines everywhere meant near-black blended in almost as badly as
    /// the old slate did). Landed on a vivid purple (#9b59b6) instead — the
    /// one primary/secondary hue neither the 4 piece colors (red/yellow/
    /// green/blue) nor the old v1 chrome palette's own muted purples
    /// (#372e4d/#614363/#5f699c, all far darker/desaturated) come anywhere
    /// near, and — unlike black or white — nowhere close to a "neutral" a
    /// dark UI background or an empty tile's own pale card art could ever
    /// be mistaken for.
    /// </summary>
    public static class VisualDefaults
    {
        /// <summary>
        /// Pixel size of one actual grid cell (see GameBootstrap.CellSize,
        /// which reads this instead of its own literal) — the single source
        /// of truth Presentation.ShapePreviewFactory caps its own per-cell
        /// size at (Data must not depend on Presentation, so it can't
        /// reference that type directly, only document the relationship
        /// here), so a piece preview (hand slot, cursor ghost, draft/deck
        /// rows) never renders a square LARGER than it will actually be
        /// once placed on the grid. Without the cap, a small shape (most
        /// visibly Single, a lone 1x1 cell) stretched to fill its whole
        /// preview box instead, several times too big — on explicit player
        /// report ("le preview dans la slot et le ghost ne sont pas à
        /// taille réelle").
        /// </summary>
        public const float GridCellSize = 54f;

        private static readonly Dictionary<PieceColor, Color> ColorMap = new Dictionary<PieceColor, Color>
        {
            { PieceColor.Coral, new Color(0.906f, 0.298f, 0.235f) }, // #e74c3c (Red)
            { PieceColor.Teal, new Color(0.204f, 0.596f, 0.859f) }, // #3498db (Blue)
            { PieceColor.Violet, new Color(0.945f, 0.769f, 0.059f) }, // #f1c40f (Yellow)
            { PieceColor.Lime, new Color(0.180f, 0.800f, 0.443f) }, // #2ecc71 (Green)
            { PieceColor.Joker, new Color(0.608f, 0.349f, 0.714f) } // #9b59b6 (vivid purple)
        };

        private static readonly Dictionary<PieceColor, string> ColorNames = new Dictionary<PieceColor, string>
        {
            { PieceColor.Coral, "Red" },
            { PieceColor.Teal, "Blue" },
            { PieceColor.Violet, "Yellow" },
            { PieceColor.Lime, "Green" },
            { PieceColor.Joker, "Joker" }
        };

        private static readonly Dictionary<ShapeId, string> ShapeNames = new Dictionary<ShapeId, string>
        {
            { ShapeId.Single, "Single" },
            { ShapeId.DomH, "Domino" },
            { ShapeId.TriL, "L-Tromino" },
            { ShapeId.TriIH, "I-Tromino" },
            { ShapeId.Sq2, "Square" },
            { ShapeId.LTetro, "L-Tetromino" },
            { ShapeId.TTetro, "T-Tetromino" },
            { ShapeId.STetro, "S-Tetromino" }
        };

        public static readonly Sprite GoldenTileSprite = Resources.Load<Sprite>("Icons/GoldenTile");
        public static readonly Sprite LockedTileSprite = Resources.Load<Sprite>("Icons/LockedTile");

        // Overlay drawn on top of a filled cell's flat color fill and below
        // its color-icon badge — a neutral (untinted) frame/bevel so a filled
        // piece cell reads as a distinct "block" rather than a flat rect,
        // regardless of which of the 5 piece colors fills it. No longer drawn
        // now that TileSprite below gives every cell its own card-shaped
        // border directly (see GridCellView.ApplyState) — kept here rather
        // than deleted in case a future look wants it back.
        public static readonly Sprite FillTileSprite = Resources.Load<Sprite>("Tiles/fill-tile-piece");

        /// <summary>
        /// Every grid cell and piece-preview square's shared base look (on
        /// explicit request — "une empty tile ressemble a card_bg_3.png",
        /// "le preview de chaque tuile est card_bg_3.png teinté de la couleur
        /// correspondante"): the same 9-sliced card art already used for hand
        /// slots and shop cards (see Presentation.UISprites.CardBackground —
        /// duplicated here rather than referenced, since Data must not depend
        /// on Presentation), shown at its own natural pale color for an empty
        /// cell and tinted with a piece's color for a filled one. Now the ONLY
        /// way a filled tile shows its color (on further explicit request:
        /// "je veux seulement card_bg_3.png teinté... pour remplacer les
        /// icons") — replaces the old per-color colorblind-accessibility
        /// badge (Assets/Resources/Icons/Coral.png etc., deleted) that used
        /// to sit on top of the fill; GetColorIcon/IconMap removed along with
        /// it, since the tinted card shape alone is what distinguishes a
        /// filled tile now.
        /// </summary>
        public static readonly Sprite TileSprite = Resources.Load<Sprite>("Colorful_UI/colorful/sprites/gameUI/card_bg_3");

        public static Color GetColor(PieceColor color)
        {
            return ColorMap.TryGetValue(color, out var c) ? c : Color.gray;
        }

        public static string GetColorName(PieceColor color)
        {
            return ColorNames.TryGetValue(color, out var n) ? n : color.ToString();
        }

        public static string GetShapeName(ShapeId shape)
        {
            return ShapeNames.TryGetValue(shape, out var n) ? n : shape.ToString();
        }

        public static readonly Color GoldenColor = new Color(0.941f, 0.702f, 0.553f); // #f0b38d — warmest palette color; blends into a pale gold over EmptyCellColor
        public static readonly Color MultiplierOutline = new Color(0.396f, 0.682f, 0.839f, 0.85f); // #65aed6 — kept distinct from Golden/Tinted so the badge types stay distinguishable on a cell
        public static readonly Color LockedColor = new Color(0.216f, 0.180f, 0.302f); // #372e4d — same as UITheme.Background: locked cells recede into the void
        public static readonly Color EmptyCellColor = new Color(0.937f, 0.980f, 0.902f); // #effae6
    }
}
