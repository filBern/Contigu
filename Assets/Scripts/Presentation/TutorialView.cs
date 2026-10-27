using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Full-screen "How to Play" overlay. Shown automatically once, the
    /// first time the game is ever launched (see GameBootstrap.Awake, gated
    /// on a PlayerPrefs flag), and reachable again any
    /// time via the H key.
    /// </summary>
    public sealed class TutorialView : MonoBehaviour
    {
        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "TutorialOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var rulesCard = UIFactory.CreatePanel(_root, "RulesCard", UITheme.Panel);
            UIFactory.AddThickOutline(rulesCard, UITheme.Border);
            rulesCard.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rulesCard.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rulesCard.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rulesCard.rectTransform.anchoredPosition = Vector2.zero;
            rulesCard.rectTransform.sizeDelta = new Vector2(1120f, 740f);

            // Everything lives inside the card (so it stays together at any aspect ratio), with the rules in two columns.
            var header = UIFactory.CreateText(rulesCard.rectTransform, "Header", "How to Play", 32, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 46f);

            string[] columns = BuildRulesColumns();
            for (int i = 0; i < columns.Length; i++)
            {
                var body = UIFactory.CreateText(rulesCard.rectTransform, "Body" + i, columns[i], BodyFontSize, UITheme.TextPrimary, TextAnchor.UpperLeft);
                body.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                body.rectTransform.pivot = new Vector2(0.5f, 1f);
                body.rectTransform.anchoredPosition = new Vector2(i == 0 ? -272f : 272f, -82f);
                body.rectTransform.sizeDelta = new Vector2(520f, 560f);
                body.lineSpacing = 1.15f;
            }

            var closeBtn = UIFactory.CreateButton(rulesCard.rectTransform, "Close", "Got it", UISprites.ChooseButtonBackground, 18);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 20f);
            closeRect.sizeDelta = new Vector2(220f, MobileSetup.TouchMin(52f));
            closeBtn.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(false);
        }

        // Larger than the body text's own size (see Build). A <size=> tag is a true rescale, unlike <b>, which Unity synthesizes by redrawing the glyph and can read as blurry.
        private const int HeaderFontSize = 22;
        private const int BodyFontSize = 16;

        /// <summary>Seven short sections covering everything a new player needs before their first placement. Headers are kept out of DescriptionTextFormatter.Colorize entirely (only each section's body line goes through it) — otherwise a header like "LUEUR AND THE SHOP" would have its own leading &lt;size=&gt; tag glued onto "LUEUR", which stops the keyword match from firing on the very word it's supposed to recognize.</summary>
        private static string[] BuildRulesColumns()
        {
            (string Header, string Body)[] sections =
            {
                ("GOAL", "Clear each round by defeating its pests before you run out of seeds: your score is the damage you deal them. Clear all 15 rounds to win the run."),
                ("PLACING SEEDS", "Drag or click a seed from your hand onto the grid. Cells of the same color that end up touching score together as one group: 1st tile 1 pt, 2nd tile 2 pts, 3rd tile 3 pts, and so on."),
                ("HARVESTS", "Fill an entire row or column and it's harvested: +3 pts per tile for the first line, +6 per tile for a second line harvested by the same move, +9 for a third, and so on. Emptying the whole grid with one move doubles your Mult. Harvesting a line containing several distinct colors also earns Pollen (+2 Pollen per distinct color in it)."),
                ("POLLEN AND THE SHOP", "Between rounds, spend Pollen in the shop on fertilizers and upgrades. Fertilizers apply to every placement, in the order you arrange them (drag or tap two to swap) - some add flat bonuses, some multiply."),
                ("PESTS FIGHT ON YOUR GARDEN", "Your score is damage against the pests, but they strike back on the board itself: they lock plots, blight tiles (points scored there turn negative and the blight spreads), steal seeds from your hand, even blight a fertilizer so it does nothing. Read each pest's tooltip and plan around it."),
                ("BOSS ROUNDS", "Every 4th round a boss pest shows up, with tricks of its own. Read its tooltip before you plant."),
                ("DEFEAT", "The run ends if your seeds run out while pests are still alive, or if none of the seeds in your hand can be placed and you have no Shuffle left.")
            };

            // Left column: the basics of a round; right column: the economy, the enemy and the controls.
            int[][] layout = { new[] { 0, 1, 2, 6 }, new[] { 3, 4, 5 } };
            var result = new string[layout.Length];
            for (int col = 0; col < layout.Length; col++)
            {
                string text = null;
                foreach (int i in layout[col])
                {
                    string block = "<size=" + HeaderFontSize + ">" + sections[i].Header + "</size>\n" + sections[i].Body;
                    text = text == null ? block : text + "\n\n" + block;
                }
                result[col] = text;
            }
            result[1] += MobileSetup.IsTouchDevice
                ? "\n\n<size=" + HeaderFontSize + ">CONTROLS</size>\nTap a seed then a plot to plant it, or drag it. Press and hold anything to read what it does. Tap a fertilizer, then Sell, to sell it; tap two fertilizers to swap them. Menu (top right): settings and Save & quit."
                : "\n\n<size=" + HeaderFontSize + ">CONTROLS</size>\nTab: view your seed bag · H: show this screen again · Esc: settings · X: sell the fertilizer you are pointing at";
            return result;
        }
    }
}
