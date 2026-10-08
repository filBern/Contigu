using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Chrome colors for panels/buttons, separate from piece/cell colors
    /// (see Data.VisualDefaults). Palette: cream card + navy page +
    /// mustard/coral accents + thick dark outline. Text is dark (#13212e)
    /// almost everywhere, with light (#fff7e8) text reserved for spots
    /// sitting directly on the bare navy page background (see
    /// TextOnBackground). <see cref="Border"/>, the thick-outline color
    /// <see cref="UIFactory.AddThickOutline"/> applies, shares TextPrimary's
    /// hex intentionally.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.071f, 0.188f, 0.290f); // #12304a
        public static readonly Color Panel = new Color(1f, 0.969f, 0.910f); // #fff7e8
        // Same cream as Panel; "selected/primary" is distinguished from "idle" via which elements get the mustard/coral fill, not a second panel tone.
        public static readonly Color PanelLight = new Color(1f, 0.969f, 0.910f); // #fff7e8
        public static readonly Color ButtonIdle = new Color(1f, 0.969f, 0.910f); // #fff7e8
        public static readonly Color ButtonSelected = new Color(1f, 0.773f, 0.239f); // #ffc53d
        public static readonly Color LightBlue = new Color(0.396f, 0.682f, 0.839f); // #65AED6
        public static readonly Color TextPrimary = new Color(0.075f, 0.129f, 0.180f); // #13212e
        // Same hex as TextPrimary, dimmed via alpha rather than a separate flat color.
        public static readonly Color TextMuted = new Color(0.075f, 0.129f, 0.180f, 0.68f); // #13212e @ 68%
        // Light text for spots that sit directly on the bare Background with no panel/button underneath (e.g. HudView's status text and Lueur readout).
        public static readonly Color TextOnBackground = new Color(1f, 0.969f, 0.910f); // #fff7e8
        // Muted counterpart to TextOnBackground.
        public static readonly Color TextMutedOnBackground = new Color(1f, 0.969f, 0.910f, 0.68f); // #fff7e8 @ 68%
        public static readonly Color Success = new Color(0.643f, 0.922f, 0.800f); // #a4ebcc
        public static readonly Color Danger = new Color(1f, 0.420f, 0.341f); // #ff6b57
        public static readonly Color HoverValid = new Color(0.643f, 0.922f, 0.800f, 0.85f); // #a4ebcc
        public static readonly Color HoverInvalid = new Color(1f, 0.420f, 0.341f, 0.85f); // #ff6b57
        public static readonly Color Border = new Color(0.075f, 0.129f, 0.180f); // #13212e
    }
}
