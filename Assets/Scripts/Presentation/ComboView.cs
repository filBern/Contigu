using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Balatro-style "chips x mult" readout sitting in the gap between the
    /// grid and the hand — shows the running chips/mult (and their product,
    /// the placement's running total) of whichever placement is currently
    /// animating (see GameBootstrap.PlayPlacementSequence and
    /// PlacementResult.Chips/.Mult, which this mirrors progressively as the
    /// sequence plays out). On explicit request ("le décompte du pointage
    /// de la pièce posé [devrait être] comme Balatro avec le visuel
    /// présenté en screenshot où le bleu est le score et le rouge le
    /// multiplicateur") — a blue chips pill and a red mult pill, reusing
    /// the existing button sprites (no new art) rather than the flat
    /// colored-chip look used elsewhere in this project, since a rounded
    /// pill is what the screenshot actually shows. A plain total-score
    /// readout sits above the two pills (explicit follow-up request).
    /// Kept separate from <see cref="HudView"/> since it needs to sit at a
    /// very specific screen position, not inside the top bar's horizontal
    /// layout.
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        private const float PulseDuration = 0.35f;
        // The overshoot above 1x halved (explicit request: "réduit son
        // pulse de moitié") — was +0.4 (1.4x peak), now +0.2 (1.2x peak).
        private const float PulsePeakScale = 1.2f;
        private const float PulsePeakFraction = 0.4f;
        private const float PillWidth = 84f;
        private const float PillHeight = 46f;
        private const float PillSpacing = 8f;
        private const float TotalRowSpacing = 3f;

        private RectTransform _root;
        private Text _totalText;
        private Text _chipsText;
        private Text _multText;
        private RectTransform _chipsPill;
        private RectTransform _multPill;
        private Coroutine _chipsPulseCoroutine;
        private Coroutine _multPulseCoroutine;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("ComboView", parent);
            var rootLayout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = TotalRowSpacing;
            rootLayout.childAlignment = TextAnchor.MiddleCenter;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            // AddComponent<VerticalLayoutGroup>() defaults BOTH of these to
            // false, which means the group only used each child's own
            // (unset, near-zero) RectTransform height to POSITION it, while
            // the ContentSizeFitter above still sized the whole root off
            // each child's LayoutElement.preferredHeight — a mismatch that
            // left a large, empty gap between _totalText and the pill row
            // below it once preferredHeight grew past whatever the text's
            // actual rect happened to be (explicit report, after the 50%
            // font bump above: "Réduit la distance entre le total combo et
            // la ligne avec mult"). Forcing control here makes the group
            // actually resize each child to its preferred size, so the two
            // stay in sync and the gap is just the spacing below.
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            // A few pixels of top padding (explicit request: "le texte de
            // combo total devrait être ... quelques pixel plus bas") so the
            // total sits a touch lower instead of flush against the top of
            // this block. Trimmed from an initial 6px (together with
            // totalLayout.preferredHeight below and TotalRowSpacing above)
            // once fixing the internal gap (see childControlHeight above)
            // revealed the whole block had grown taller than the fixed
            // 105.5px gap between the grid's bottom edge and the pieces bar
            // (see GameBootstrap's comboRect comment) — enough to overlap
            // the bar (explicit report with screenshot: "il y a un overlap
            // là avec mult et addition de combo"). Trimmed once more, 3->2,
            // to free up a bit more slack for the position nudge below.
            rootLayout.padding = new RectOffset(0, 0, 2, 0);
            var rootFitter = _root.gameObject.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Running total (chips x mult) — sits above the breakdown, on
            // explicit follow-up request ("rajouter en dessous ou en haut
            // de ces deux chiffres le score total du placement"). Beige
            // (UITheme.Panel — explicit request: "les textes de scores en
            // bas de la grille soit de la couleur beige"), since this
            // floats directly over the bare navy Background rather than a
            // light panel like most of the reskinned text.
            // Font size 50% bigger than the original 30 (explicit request:
            // "Le texte de combo total devrait être 50% plus gros").
            _totalText = UIFactory.CreateText(_root, "TotalText", "0", 45, UITheme.Panel);
            var totalLayout = _totalText.gameObject.AddComponent<LayoutElement>();
            totalLayout.preferredWidth = PillWidth * 2f + PillSpacing * 2f;
            // Trimmed from 54 to 48 (see the padding comment above) — still
            // comfortably tall enough for the 45pt digits, which have no
            // descenders to clear.
            totalLayout.preferredHeight = 48f;

            var row = UIFactory.CreateUIObject("Row", _root);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = PillSpacing;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var rowFitter = row.gameObject.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredWidth = PillWidth * 2f + PillSpacing + 16f;
            rowLayoutElement.preferredHeight = PillHeight;

            var chipsPill = UIFactory.CreateSlicedImage(row, "ChipsPill", UISprites.ChooseButtonBackground);
            chipsPill.rectTransform.sizeDelta = new Vector2(PillWidth, PillHeight);
            var chipsLayout = chipsPill.gameObject.AddComponent<LayoutElement>();
            chipsLayout.preferredWidth = PillWidth;
            chipsLayout.preferredHeight = PillHeight;
            _chipsPill = chipsPill.rectTransform;
            _chipsText = UIFactory.CreateText(chipsPill.transform, "ChipsText", "0", 26, Color.white);
            UIFactory.StretchFull(_chipsText.rectTransform);

            var xLabel = UIFactory.CreateText(row, "XLabel", "x", 22, UITheme.Panel);
            var xLayout = xLabel.gameObject.AddComponent<LayoutElement>();
            xLayout.preferredWidth = 16f;
            xLayout.preferredHeight = PillHeight;

            var multPill = UIFactory.CreateSlicedImage(row, "MultPill", UISprites.CancelButtonBackground);
            multPill.rectTransform.sizeDelta = new Vector2(PillWidth, PillHeight);
            var multLayout = multPill.gameObject.AddComponent<LayoutElement>();
            multLayout.preferredWidth = PillWidth;
            multLayout.preferredHeight = PillHeight;
            _multPill = multPill.rectTransform;
            _multText = UIFactory.CreateText(multPill.transform, "MultText", "1", 26, Color.white);
            UIFactory.StretchFull(_multText.rectTransform);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>
        /// Updates both pills plus the total readout above them (chips *
        /// mult) — <paramref name="chips"/> mirrors <see
        /// cref="Core.PlacementResult.Chips"/>, <paramref name="mult"/>
        /// mirrors <see cref="Core.PlacementResult.Mult"/>, both accumulated
        /// progressively by the placement sequence rather than only shown
        /// at the very end. <paramref name="mult"/> is a float (progressive
        /// modifiers — Densité, Cartes Enchantées, Expérience — keep their
        /// true fractional value all the way through, on explicit request:
        /// "on doit multiplier comme si c'était un float"); the pill shows
        /// it as a plain whole number when it happens to be one, one
        /// decimal otherwise, and the total readout rounds the product
        /// ONCE, matching <see cref="Core.PlacementResult.TotalScore"/>.
        /// </summary>
        public void Show(int chips, float mult)
        {
            _root.gameObject.SetActive(true);
            _chipsText.text = chips.ToString();
            _multText.text = FormatMult(mult);
            _totalText.text = Mathf.RoundToInt(chips * mult).ToString();
        }

        private static string FormatMult(float mult)
        {
            float rounded = Mathf.Round(mult);
            if (Mathf.Abs(mult - rounded) < 0.05f)
            {
                return Mathf.RoundToInt(mult).ToString();
            }
            return mult.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Brief scale-bounce on the blue chips pill — used every time <see cref="Core.PlacementResult.Chips"/> actually grows (on explicit request: "faisons pulse le texte pour point... lorsque celui-ci est augmenté"), so a chips gain reads distinctly from the rest of the readout.</summary>
        public void PulseChips()
        {
            _chipsPulseCoroutine = RestartPulse(_chipsPulseCoroutine, _chipsPill);
        }

        /// <summary>Same bounce as <see cref="PulseChips"/>, but on the red mult pill — used every time <see cref="Core.PlacementResult.Mult"/>'s ordered per-modifier catch-up (see GameBootstrap's placement sequence) actually grows it, so the "mult went up" moment reads distinctly from an ordinary chips gain.</summary>
        public void PulseMult()
        {
            _multPulseCoroutine = RestartPulse(_multPulseCoroutine, _multPill);
        }

        private Coroutine RestartPulse(Coroutine running, RectTransform target)
        {
            if (running != null)
            {
                StopCoroutine(running);
            }
            return StartCoroutine(PulseRoutine(target));
        }

        private IEnumerator PulseRoutine(RectTransform target)
        {
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
