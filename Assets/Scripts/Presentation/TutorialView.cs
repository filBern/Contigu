using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Full-screen "How to Play" overlay (spec extension, explicit request
    /// — "tutoriel / écran de règles", picked as the next step after
    /// colorblind mode: the game previously had zero in-game explanation
    /// of its rules anywhere, the biggest gap for a new player). Shown
    /// automatically once, the very first time the game is ever launched
    /// (see GameBootstrap.Awake, gated on the same PlayerPrefs-flag
    /// pattern as ColorblindMode), and reachable again any time via the H
    /// key — same "always-available, not editor-only" reasoning as
    /// Tab/C.
    /// </summary>
    public sealed class TutorialView : MonoBehaviour
    {
        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "TutorialOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "How to Play", 32, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 46f);

            var body = UIFactory.CreateText(_root, "Body", BuildRulesText(), 17, UITheme.TextPrimary, TextAnchor.UpperLeft);
            body.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -96f);
            body.rectTransform.sizeDelta = new Vector2(980f, 560f);
            body.lineSpacing = 1.2f;

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Got it", UISprites.ChooseButtonBackground, 18);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(220f, 52f);
            closeBtn.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        // Font size for a section header, up from the 17 the body text
        // itself renders at (see Build) — on explicit report the plain-
        // uppercase headers (see BuildRulesText's own doc comment for why
        // they're not <b>) still didn't stand out enough: "Les secondary
        // title devraient être plus gros". A <size=> tag is a true rescale
        // (not a faux style Unity has to synthesize by redrawing the
        // glyph, unlike <b>), so it carries none of that blur risk.
        private const int HeaderFontSize = 24;

        /// <summary>Six short sections covering everything a new player needs before their first placement. Headers are kept out of DescriptionTextFormatter.Colorize entirely (only each section's body line goes through it) — otherwise a header like "LUEUR AND THE SHOP" would have its own leading &lt;size=&gt; tag glued onto "LUEUR", which stops the keyword match from firing on the very word it's supposed to recognize.</summary>
        private static string BuildRulesText()
        {
            (string Header, string Body)[] sections =
            {
                ("GOAL", "Reach each round's score quota before you run out of pieces. Clear all 8 rounds — the 8th is the boss round — to win the run."),
                ("PLACING PIECES", "Drag or click a piece from your hand onto the grid. Cells of the same color that end up touching score together as one group: 1st tile 1 pt, 2nd tile 2 pts, 3rd tile 3 pts, and so on."),
                ("LINE AND COLUMN CLEARS", "Fill an entire row or column and it clears: +3 pts per tile. Clearing a line containing several distinct colors also earns Lueur (+2 Lueur per distinct color in it)."),
                ("LUEUR AND THE SHOP", "Between rounds, spend Lueur in the shop on modifiers and upgrades. Modifiers apply to every placement, in the order you arrange them (drag or tap two to swap) — some add flat bonuses, some multiply."),
                ("BOSS ROUND", "The final round locks 2 more random empty cells every 3 pieces you play — leave yourself room before it starts tightening up."),
                ("DEFEAT", "If none of your 3 hand pieces can be placed anywhere on the grid before reaching the quota, the run ends there.")
            };

            string text = null;
            for (int i = 0; i < sections.Length; i++)
            {
                string block = "<size=" + HeaderFontSize + ">" + sections[i].Header + "</size>\n" + DescriptionTextFormatter.Colorize(sections[i].Body);
                text = text == null ? block : text + "\n\n" + block;
            }
            text += "\n\n" + DescriptionTextFormatter.Colorize("Tab: view your deck · C: colorblind mode · H: show this screen again");
            return text;
        }
    }
}
