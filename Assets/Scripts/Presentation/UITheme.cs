using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Chrome colors for panels/buttons, separate from piece/cell colors (see
    /// Data.VisualDefaults). Built from the v1 8-color game palette (menu, tile
    /// and grid colors all come from this same set — see VisualDefaults for the
    /// piece/grid side): #372e4d, #5f699c, #65aed6, #a4ebcc, #effae6, #f0b38d,
    /// #b56d7f, #614363. With only 8 colors covering ~20 UI roles, several roles
    /// deliberately reuse the same hex — safe wherever the two roles never sit
    /// directly on top of each other (e.g. a button fill and a grid tile).
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.216f, 0.180f, 0.302f); // #372e4d
        public static readonly Color Panel = new Color(0.380f, 0.263f, 0.388f); // #614363
        public static readonly Color PanelLight = new Color(0.373f, 0.412f, 0.612f); // #5f699c
        public static readonly Color ButtonIdle = new Color(0.373f, 0.412f, 0.612f); // #5f699c
        public static readonly Color ButtonSelected = new Color(0.396f, 0.682f, 0.839f); // #65aed6
        public static readonly Color TextPrimary = new Color(0.937f, 0.980f, 0.902f); // #effae6
        // Same hex as TextPrimary, dimmed via alpha rather than a separate flat
        // color — a distinct-but-flat "muted" hex risked landing exactly on
        // PanelLight/ButtonIdle's color (#5f699c), which would make muted text
        // invisible against a card or button of that color.
        public static readonly Color TextMuted = new Color(0.937f, 0.980f, 0.902f, 0.68f); // #effae6 @ 68%
        public static readonly Color Success = new Color(0.643f, 0.922f, 0.800f); // #a4ebcc
        public static readonly Color Danger = new Color(0.710f, 0.427f, 0.498f); // #b56d7f
        public static readonly Color HoverValid = new Color(0.643f, 0.922f, 0.800f, 0.85f); // #a4ebcc
        public static readonly Color HoverInvalid = new Color(0.710f, 0.427f, 0.498f, 0.85f); // #b56d7f
    }
}
