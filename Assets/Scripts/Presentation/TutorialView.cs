using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Full-screen "How to Play" overlay. Shown automatically once, the
    /// first time the game is ever launched (see GameBootstrap.Awake, gated
    /// on a PlayerPrefs flag like ColorblindMode), and reachable again any
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
            rulesCard.rectTransform.anchoredPosition = new Vector2(0f, 15f);
            rulesCard.rectTransform.sizeDelta = new Vector2(1120f, 700f);

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
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(false);
        }

        // Larger than the body text's own size (see Build). A <size=> tag is a true rescale, unlike <b>, which Unity synthesizes by redrawing the glyph and can read as blurry.
        private const int HeaderFontSize = 24;

        /// <summary>Seven short sections covering everything a new player needs before their first placement. Headers are kept out of DescriptionTextFormatter.Colorize entirely (only each section's body line goes through it) — otherwise a header like "LUEUR AND THE SHOP" would have its own leading &lt;size=&gt; tag glued onto "LUEUR", which stops the keyword match from firing on the very word it's supposed to recognize.</summary>
        private static string BuildRulesText()
        {
            (string Header, string Body)[] sections =
            {
                ("GOAL", "Clear each round — reach its score quota or defeat its enemies before you run out of seeds. Clear all 15 rounds to win the run."),
                ("PLACING SEEDS", "Drag or click a seed from your hand onto the grid. Cells of the same color that end up touching score together as one group: 1st sprout 1 pt, 2nd sprout 2 pts, 3rd sprout 3 pts, and so on."),
                ("HARVESTS", "Fill an entire row or column and it's harvested: +3 pts per sprout for the first line, +6 per sprout for a second line harvested by the same move, +9 for a third, and so on. Emptying the whole grid with one move doubles your Mult. Harvesting a line containing several distinct colors also earns Pollen (+2 Pollen per distinct color in it)."),
                ("POLLEN AND THE SHOP", "Between rounds, spend Pollen in the shop on fertilizers and upgrades. Fertilizers apply to every placement, in the order you arrange them (drag or tap two to swap) — some add flat bonuses, some multiply."),
                ("PESTS FIGHT ON YOUR GARDEN", "Your score is damage against the pests, but they strike back on the board itself: they lock plots, blight tiles (points scored there turn negative and the blight spreads), steal seeds from your hand, even blight a fertilizer so it does nothing. Read each pest's tooltip and plan around it."),
                ("BOSS ROUNDS", "Every 4 rounds, a random boss rule applies: the grid locks 1 cell every 5 seeds, one hand slot is locked, or one color scores no points."),
                ("DEFEAT", "If none of your 3 hand seeds can be placed anywhere on the grid before reaching the quota, the run ends there.")
            };

            string text = null;
            for (int i = 0; i < sections.Length; i++)
            {
                string block = "<size=" + HeaderFontSize + ">" + sections[i].Header + "</size>\n" + sections[i].Body;
                text = text == null ? block : text + "\n\n" + block;
            }
            text += "\n\nTab: view your seed bag · C: colorblind mode · H: show this screen again · Esc: settings";
            return text;
        }
    }
}
