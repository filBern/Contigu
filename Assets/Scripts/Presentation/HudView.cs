using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Two progress bars, flush against the top and bottom edges of the
    /// screen and spanning its full width (no margin), built from flat
    /// geometric rectangles in UITheme colors (see BuildBar) — the top
    /// bar tracks round score against the round's quota, the bottom bar
    /// tracks remaining piece budget for the round. Replaces the old
    /// text-only readout (round number, round score, total score) — the
    /// run's overall progress isn't shown moment-to-moment, just what the
    /// player needs to finish the current round.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float BarHeight = 68f;
        private RectTransform _scoreFillRect;
        private Text _scoreLabel;
        private RectTransform _piecesFillRect;
        private Text _piecesLabel;
        private RectTransform _lueurContainer;
        private Text _lueurLabel;
        // Folded into the score bar's own label (see SetRound/SetScores)
        // rather than a separate readout — on explicit request: "il faut
        // mettre a quelle round on est rendu sur le nombre total a
        // réussir". Defaults match round 1 of an 8-round run so the very
        // first SetScores call (before Refresh's own SetRound runs) never
        // shows a stale 0/0.
        private int _roundNumber = 1;
        private int _roundCount = RunConfig.RoundCount;
        private bool _isEndless;

        public void Build(Transform parent, Transform lueurParent)
        {
            BuildBar(parent, "ScoreBar", UITheme.ButtonSelected, top: true, out _scoreFillRect, out _scoreLabel);
            BuildBar(parent, "PiecesBar", UITheme.Success, top: false, out _piecesFillRect, out _piecesLabel);

            // Persistent readout displayed outside and above the combo card
            // (moved here from below the status text on explicit request —
            // was a small top-right corner readout, then 18->22->44->66->99;
            // now slightly reduced to 84 after the layout review) — Lueur is a
            // whole-run currency (see
            // RunManager.Lueur), not tied to either bar's own round-scoped
            // progress, so it gets its own spot rather than folding into the
            // score bar's label. The "Lueur: " text prefix is gone (explicit
            // request, after seeing the itch page mockups: "au lieu de
            // marquer Lueur: ... mettre le petit losange orange") — a small
            // rotated-square "diamond" icon stands in for it instead, since
            // no gem/diamond sprite exists in the Colorful UI pack. Icon and
            // number use a horizontal layout group and size themselves to
            // their contents, staying centered as the number's digit count grows.
            _lueurContainer = UIFactory.CreateUIObject("LueurContainer", lueurParent);
            var lueurLayout = _lueurContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            lueurLayout.spacing = 19f;
            lueurLayout.childAlignment = TextAnchor.MiddleCenter;
            lueurLayout.childControlWidth = true;
            lueurLayout.childControlHeight = true;
            lueurLayout.childForceExpandWidth = false;
            lueurLayout.childForceExpandHeight = false;
            var lueurFitter = _lueurContainer.gameObject.AddComponent<ContentSizeFitter>();
            lueurFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lueurFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lueurIcon = UIFactory.CreatePanel(_lueurContainer, "LueurIcon", VisualDefaults.GoldenColor);
            lueurIcon.rectTransform.sizeDelta = new Vector2(35f, 35f);
            lueurIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            // Plain Image has no ILayoutElement, so without this the
            // HorizontalLayoutGroup gives it zero width to work with (same
            // gotcha as HandView's Shuffle button — see BuildShuffleButton).
            // Sized a bit larger than the 18px square itself to leave room
            // for the diamond's rotated corners (18 * sqrt(2) ≈ 25px
            // diagonal) without crowding the number next to it.
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 54f;
            lueurIconLayout.preferredHeight = 54f;

            _lueurLabel = UIFactory.CreateText(_lueurContainer, "LueurLabel", "", 84, VisualDefaults.GoldenColor);
            _lueurLabel.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// Own flat geometric bar (a plain cream track rectangle plus a
        /// solid-color fill rectangle) instead of the "Colorful UI" pack's
        /// rounded-pill slider sprites — on explicit request, after seeing
        /// them next to the new DA ("refaire l'asset ... des deux progress
        /// bar"): those sprites' own baked-in purple/blue art doesn't follow
        /// UITheme at all (CreateSlicedImage tints sprites white, i.e. not
        /// at all), so they kept showing their old colors no matter what
        /// the rest of the reskin changed. A plain rectangle also fits this
        /// bar's own "flush against the screen edge, full width" shape
        /// better than a rounded pill did.
        /// </summary>
        private static void BuildBar(Transform parent, string name, Color fillColor, bool top,
            out RectTransform fillRect, out Text label)
        {
            float edgeY = top ? 1f : 0f;
            // Both bars share the same flat track color and only differ by
            // fill color.
            var bg = UIFactory.CreatePanel(parent, name, UITheme.PanelLight);
            UIFactory.AddThickOutline(bg, UITheme.Border);
            // Stretched full-width (anchor min/max x = 0/1) and flush against
            // the top or bottom edge (anchor, pivot and anchoredPosition all
            // pinned to that same edge — zero anchoredPosition means no gap).
            bg.rectTransform.anchorMin = new Vector2(0f, edgeY);
            bg.rectTransform.anchorMax = new Vector2(1f, edgeY);
            bg.rectTransform.pivot = new Vector2(0.5f, edgeY);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, BarHeight);

            // The fill's RIGHT edge is driven directly by anchorMax.x (see
            // SetRatio) — a pure layout resize, not Image.Type.Filled — so
            // the bar's width is guaranteed to track the ratio with no
            // dependency on fill-shader/mesh behavior.
            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dark text — the track/fill are both light (cream/mustard/mint)
            // now, unlike the old dark-purple sprite this used to sit on.
            var text = UIFactory.CreateText(bg.transform, "Label", "", 32, UITheme.TextPrimary);
            UIFactory.StretchFull(text.rectTransform);

            fillRect = rt;
            label = text;
        }

        /// <summary>Resizes a bar's fill rect so its right edge sits at <paramref name="ratio"/> (0-1) of the bar's width.</summary>
        private static void SetRatio(RectTransform fillRect, float ratio)
        {
            var max = fillRect.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            fillRect.anchorMax = max;
        }

        /// <summary>Anchor for the Lueur readout (icon + number together) — the presentation layer flies each Lueur group's popup toward this point (see GameBootstrap.PlayPlacementSequence) instead of just adding the total in one lump sum.</summary>
        public RectTransform LueurLabelTransform
        {
            get { return _lueurContainer; }
        }

        /// <summary>Anchor on the pieces bar — the presentation layer flies each round-end "unused piece -> Lueur" popup FROM this point (see GameBootstrap.PlayRoundEndLueurBonusSequence), the mirror image of LueurLabelTransform above as a destination.</summary>
        public RectTransform PiecesBarTransform
        {
            get { return _piecesLabel.rectTransform; }
        }

        public void Refresh(RunManager run)
        {
            SetRound(run.CurrentRoundNumber, run.Challenge.RoundCount, run.IsEndless);
            SetPieces(run.PiecesRemainingThisRound, run.CurrentBudget);
            SetScores(run.RoundScore, run.CurrentQuota);
            SetLueur(run.Lueur);
        }

        /// <summary>Which round is currently in progress, folded into the score bar's own label by SetScores (on explicit request: "il faut mettre a quelle round on est rendu sur le nombre total a réussir") — stored rather than passed to SetScores directly since the round itself never changes across that method's own many progressive-update calls within a single placement's score cascade. <paramref name="isEndless"/> drops the "/roundCount" denominator (explicit request added Endless mode — a fixed cap no longer applies once the player keeps going past it, so "Round 9/8" would misread as overshooting a limit).</summary>
        public void SetRound(int roundNumber, int roundCount, bool isEndless)
        {
            _roundNumber = roundNumber;
            _roundCount = roundCount;
            _isEndless = isEndless;
        }

        /// <summary>
        /// Updates just the Lueur label, without touching anything else —
        /// same idea as <see cref="SetScores"/>, lets the presentation layer
        /// animate Lueur up progressively (one group at a time) instead of
        /// always jumping straight to the final value. Used to also pulse
        /// the label on every increase, removed outright on explicit
        /// request ("Enlève le pulse complètement sur l'effet lueur en
        /// haut de la grille") after several rounds of trying to tune its
        /// intensity/positioning/timing down to something that still read
        /// as too much.
        /// </summary>
        public void SetLueur(int lueur)
        {
            _lueurLabel.text = lueur.ToString();
        }

        /// <summary>
        /// Updates just the score bar, without touching the pieces bar — lets
        /// the presentation layer animate the score up progressively in sync
        /// with score popups instead of always jumping straight to the final
        /// value.
        /// </summary>
        public void SetScores(int roundScore, int quota)
        {
            string roundLabel = _isEndless ? "Round " + _roundNumber : "Round " + _roundNumber + "/" + _roundCount;
            _scoreLabel.text = roundLabel + "  —  " + roundScore + " / " + quota;
            SetRatio(_scoreFillRect, quota > 0 ? (float)roundScore / quota : 0f);
        }

        /// <summary>
        /// Updates just the pieces bar, without touching the score bar — same
        /// "presentation layer animates progressively" idea as <see
        /// cref="SetScores"/>, used by GameBootstrap.PlayRoundEndLueurBonusSequence
        /// to count the bar down one unused piece at a time as each converts
        /// into +1 Lueur, instead of jumping straight to empty.
        /// </summary>
        public void SetPieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = piecesRemaining + " / " + budget;
            SetRatio(_piecesFillRect, budget > 0 ? (float)piecesRemaining / budget : 0f);
        }
    }
}
