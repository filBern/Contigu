using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Chrome colors for panels/buttons, separate from piece/cell colors (see
    /// Data.VisualDefaults, untouched by this palette — explicit request:
    /// "changer la DA du jeu ... je parle des couleurs, mais ne touche pas
    /// aux 4 couleurs de tuiles"). Reskinned from the original 8-color purple/
    /// blue palette to match a reference screenshot's "cream card + navy page
    /// + mustard/coral accents + thick dark outline" look (a journal-app UI,
    /// explicitly given only as a color/style reference, not anything to do
    /// with this game's own content). Exact hex values sampled from that
    /// screenshot with Pillow rather than eyeballed: #12304a (navy page),
    /// #fff7e8 (cream card), #13212e (near-black text/border — NOT pure
    /// black, a very dark navy that reads as black at normal size), #ffc53d
    /// (mustard gold, primary buttons), #ff6b57 (coral, secondary/danger
    /// buttons). Text is dark (#13212e) almost everywhere in that reference —
    /// on cream cards, on mustard buttons, even on the coral tab — with light
    /// (#fff7e8) text reserved for the one spot sitting directly on the bare
    /// navy page background (see TextOnBackground). <see cref="Border"/> is
    /// the new thick-outline color <see cref="UIFactory.AddThickOutline"/>
    /// applies (every button automatically via UIFactory.FinishButton, every
    /// major panel/card explicitly at its own build site) — same hex as
    /// TextPrimary, intentionally: the reference draws borders and body text
    /// in the exact same near-black tone.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.071f, 0.188f, 0.290f); // #12304a
        public static readonly Color Panel = new Color(1f, 0.969f, 0.910f); // #fff7e8
        // Same cream as Panel — the reference itself uses one flat cream for
        // both card backgrounds AND idle/unselected buttons (e.g. the
        // "Journée" inactive tab, the "Historique" pill), distinguishing
        // "selected/primary" from "idle" purely via which elements get the
        // mustard/coral fill instead, not via a second panel tone.
        public static readonly Color PanelLight = new Color(1f, 0.969f, 0.910f); // #fff7e8
        public static readonly Color ButtonIdle = new Color(1f, 0.969f, 0.910f); // #fff7e8
        public static readonly Color ButtonSelected = new Color(1f, 0.773f, 0.239f); // #ffc53d
        public static readonly Color TextPrimary = new Color(0.075f, 0.129f, 0.180f); // #13212e
        // Same hex as TextPrimary, dimmed via alpha rather than a separate flat
        // color — matches the original palette's own "same hex, lower alpha"
        // convention for this role.
        public static readonly Color TextMuted = new Color(0.075f, 0.129f, 0.180f, 0.68f); // #13212e @ 68%
        // Light text for the few spots that sit directly on the bare
        // Background with no panel/button underneath (e.g. HudView's status
        // text and Lueur readout) — TextPrimary is now dark, so those need
        // their own explicit light color instead, unlike before this
        // reskin when TextPrimary itself was light and worked everywhere.
        public static readonly Color TextOnBackground = new Color(1f, 0.969f, 0.910f); // #fff7e8
        // Muted counterpart to TextOnBackground, same "same hex, lower
        // alpha" convention as TextMuted.
        public static readonly Color TextMutedOnBackground = new Color(1f, 0.969f, 0.910f, 0.68f); // #fff7e8 @ 68%
        public static readonly Color Success = new Color(0.643f, 0.922f, 0.800f); // #a4ebcc — unchanged: a gameplay valid/invalid signal, not part of the reference's own palette
        public static readonly Color Danger = new Color(1f, 0.420f, 0.341f); // #ff6b57
        public static readonly Color HoverValid = new Color(0.643f, 0.922f, 0.800f, 0.85f); // #a4ebcc
        public static readonly Color HoverInvalid = new Color(1f, 0.420f, 0.341f, 0.85f); // #ff6b57
        // The reference's thick "comic" outline around every card/button —
        // see UIFactory.AddThickOutline.
        public static readonly Color Border = new Color(0.075f, 0.129f, 0.180f); // #13212e
    }
}
