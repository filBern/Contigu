using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Chrome colors for panels/buttons, separate from piece/cell colors (see Data.VisualDefaults).</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.09f, 0.10f, 0.13f);
        public static readonly Color Panel = new Color(0.14f, 0.15f, 0.19f);
        public static readonly Color PanelLight = new Color(0.20f, 0.21f, 0.26f);
        public static readonly Color ButtonIdle = new Color(0.24f, 0.26f, 0.33f);
        public static readonly Color ButtonSelected = new Color(0.35f, 0.55f, 0.95f);
        public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.97f);
        public static readonly Color TextMuted = new Color(0.65f, 0.66f, 0.72f);
        public static readonly Color Success = new Color(0.35f, 0.85f, 0.45f);
        public static readonly Color Danger = new Color(0.92f, 0.35f, 0.35f);
        public static readonly Color HoverValid = new Color(0.35f, 0.85f, 0.45f, 0.85f);
        public static readonly Color HoverInvalid = new Color(0.92f, 0.35f, 0.35f, 0.85f);
    }
}
