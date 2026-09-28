using System.Collections;
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
        // Below GameBootstrap's status text ("Select or drag a piece onto
        // the grid.", anchored top-center at y=-80, 26 tall — see
        // GameBootstrap.BuildUI) — on explicit request: "j'aimerais qu'il
        // soit sous le texte Drag or click". Kept as a documented constant
        // rather than read from that Text directly, same
        // cross-referenced-magic-number precedent as BarHeight above. Gap
        // tightened 10->2 when the label doubled in size (below) — the grid
        // itself starts at y≈-163 (8*54 + 7*6 = 474 total, centered on an
        // 800-tall canvas — see GridView.Build/VisualDefaults.GridCellSize),
        // so there's only ~83px of headroom below the status text for this
        // label to grow into before it'd start overlapping the board.
        private const float LueurLabelY = -(80f + 26f + 2f);
        private const float LueurPulseDuration = 0.25f;
        private const float LueurPulsePeakScale = 1.3f;
        private const float LueurPulsePeakFraction = 0.35f;

        private RectTransform _scoreFillRect;
        private Text _scoreLabel;
        private RectTransform _piecesFillRect;
        private Text _piecesLabel;
        private RectTransform _lueurContainer;
        private Text _lueurLabel;
        private int _lastLueur;
        private Coroutine _lueurPulseCoroutine;

        public void Build(Transform parent)
        {
            BuildBar(parent, "ScoreBar", UITheme.ButtonSelected, top: true, out _scoreFillRect, out _scoreLabel);
            BuildBar(parent, "PiecesBar", UITheme.Success, top: false, out _piecesFillRect, out _piecesLabel);

            // Persistent readout centered below the status text (moved there
            // and enlarged on explicit request — was a small top-right
            // corner readout, then 18->22, now doubled again to 44 on
            // further explicit request: "Le compteur de lueur devrait être
            // 2x plus gros") — Lueur is a whole-run currency (see
            // RunManager.Lueur), not tied to either bar's own round-scoped
            // progress, so it gets its own spot rather than folding into the
            // score bar's label. The "Lueur: " text prefix is gone (explicit
            // request, after seeing the itch page mockups: "au lieu de
            // marquer Lueur: ... mettre le petit losange orange") — a small
            // rotated-square "diamond" icon stands in for it instead, since
            // no gem/diamond sprite exists in the Colorful UI pack. Icon and
            // number live in their own HorizontalLayoutGroup container
            // (ContentSizeFitter-driven, same auto-width-stays-centered
            // pattern as HandView's hand container) so the pair re-centers
            // itself as the number's digit count changes, rather than the
            // icon sitting at a fixed offset from a text block whose width
            // varies.
            _lueurContainer = UIFactory.CreateUIObject("LueurContainer", parent);
            _lueurContainer.anchorMin = new Vector2(0.5f, 1f);
            _lueurContainer.anchorMax = new Vector2(0.5f, 1f);
            _lueurContainer.pivot = new Vector2(0.5f, 1f);
            _lueurContainer.anchoredPosition = new Vector2(0f, LueurLabelY);
            var lueurLayout = _lueurContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            lueurLayout.spacing = 10f;
            lueurLayout.childAlignment = TextAnchor.MiddleCenter;
            lueurLayout.childForceExpandWidth = false;
            lueurLayout.childForceExpandHeight = false;
            var lueurFitter = _lueurContainer.gameObject.AddComponent<ContentSizeFitter>();
            lueurFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lueurFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lueurIcon = UIFactory.CreatePanel(_lueurContainer, "LueurIcon", VisualDefaults.GoldenColor);
            lueurIcon.rectTransform.sizeDelta = new Vector2(22f, 22f);
            lueurIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            // Plain Image has no ILayoutElement, so without this the
            // HorizontalLayoutGroup gives it zero width to work with (same
            // gotcha as HandView's Shuffle button — see BuildShuffleButton).
            // Sized a bit larger than the 22px square itself to leave room
            // for the diamond's rotated corners (22 * sqrt(2) ≈ 31px
            // diagonal) without crowding the number next to it.
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 34f;
            lueurIconLayout.preferredHeight = 34f;

            _lueurLabel = UIFactory.CreateText(_lueurContainer, "LueurLabel", "", 44, VisualDefaults.GoldenColor);
            _lueurLabel.alignment = TextAnchor.MiddleCenter;

            BuildScoringBaseline(parent);
        }

        /// <summary>
        /// Static bullet-point reference for the scoring rules that always
        /// apply (independent of any modifier), how Lueur is earned/spent,
        /// and the deck-view hint, in the top-right corner (on explicit
        /// request: "un texte bullet point avec la baseline du pointage" +
        /// "une mention tab to open piece deck ... quelque part dans
        /// l'écran" + "il manque aussi une mention sur comment on gagne des
        /// points de lueur et à quoi ça sert") — moved up to sit right below
        /// the top bar now that the Lueur readout that used to occupy that
        /// spot moved to below the status text instead (see LueurLabelY).
        /// Built once and never refreshed — none of this ever changes mid-run.
        /// </summary>
        private static void BuildScoringBaseline(Transform parent)
        {
            string[] lines =
            {
                "• Group: 1st tile 1 pt, 2nd 2 pts, 3rd 3 pts...",
                "• Line/column clear: 3 pts per tile",
                "• Golden tile: +18 pts",
                "• Line/column clear: +2 Lueur per distinct color in it",
                "• Lueur: spend it in the shop on modifiers/upgrades",
                "• Shuffle: re-roll your hand, limited uses per run",
                "• Tab: view piece deck",
                "• C: toggle colorblind mode",
                "• H: show the rules again",
                "• Esc: settings"
            };
            string text = DescriptionTextFormatter.Colorize(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                text += "\n" + DescriptionTextFormatter.Colorize(lines[i]);
            }

            var label = UIFactory.CreateText(parent, "ScoringBaseline", text, 15, UITheme.TextMutedOnBackground, TextAnchor.UpperRight);
            label.rectTransform.anchorMin = new Vector2(1f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(1f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(-16f, -(BarHeight + 8f));
            label.rectTransform.sizeDelta = new Vector2(280f, 200f);
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

        public void Refresh(RunManager run)
        {
            UpdatePieces(run.PiecesRemainingThisRound, run.CurrentBudget);
            SetScores(run.RoundScore, run.CurrentQuota);
            SetLueur(run.Lueur);
        }

        /// <summary>
        /// Updates just the Lueur label, without touching anything else —
        /// same idea as <see cref="SetScores"/>, lets the presentation layer
        /// animate Lueur up progressively (one group at a time) instead of
        /// always jumping straight to the final value; each of those
        /// intermediate catch-up steps that actually raises the total also
        /// pulses the label (on explicit request: "qu'il pulse chaque fois
        /// qu'il augmente"), not just the final one. A drop (spent in the
        /// shop) or unchanged value never pulses.
        /// </summary>
        public void SetLueur(int lueur)
        {
            _lueurLabel.text = lueur.ToString();
            if (lueur > _lastLueur)
            {
                PulseLueur();
            }
            _lastLueur = lueur;
        }

        private void PulseLueur()
        {
            // Stops any pulse already in flight before starting a fresh one
            // rather than letting two coroutines animate the same
            // RectTransform's scale at once (same defensive pattern as
            // ModifierPanelView.PulseRow, which fixed a color-corruption bug
            // from exactly this kind of overlap).
            if (_lueurPulseCoroutine != null)
            {
                StopCoroutine(_lueurPulseCoroutine);
            }
            _lueurPulseCoroutine = StartCoroutine(PulseLueurRoutine());
        }

        private IEnumerator PulseLueurRoutine()
        {
            var rt = _lueurContainer;
            float t = 0f;
            while (t < LueurPulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / LueurPulseDuration);
                float scale = p < LueurPulsePeakFraction
                    ? Mathf.Lerp(1f, LueurPulsePeakScale, p / LueurPulsePeakFraction)
                    : Mathf.Lerp(LueurPulsePeakScale, 1f, (p - LueurPulsePeakFraction) / (1f - LueurPulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _lueurPulseCoroutine = null;
        }

        /// <summary>
        /// Updates just the score bar, without touching the pieces bar — lets
        /// the presentation layer animate the score up progressively in sync
        /// with score popups instead of always jumping straight to the final
        /// value.
        /// </summary>
        public void SetScores(int roundScore, int quota)
        {
            _scoreLabel.text = roundScore + " / " + quota;
            SetRatio(_scoreFillRect, quota > 0 ? (float)roundScore / quota : 0f);
        }

        private void UpdatePieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = piecesRemaining + " / " + budget;
            SetRatio(_piecesFillRect, budget > 0 ? (float)piecesRemaining / budget : 0f);
        }
    }
}
