using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Balatro-style "chips x mult" readout sitting in the gap between the
    /// grid and the hand — shows the running chips/mult of whichever
    /// placement is currently animating (see GameBootstrap.PlayPlacementSequence
    /// and PlacementResult.Chips/.Mult, which this mirrors progressively as
    /// the sequence plays out). On explicit request ("le décompte du
    /// pointage de la pièce posé [devrait être] comme Balatro avec le
    /// visuel présenté en screenshot où le bleu est le score et le rouge le
    /// multiplicateur") — replaces the old plain "Combo: +N" text with a
    /// blue chips pill and a red mult pill, reusing the existing button
    /// sprites (no new art) rather than the flat colored-chip look used
    /// elsewhere in this project, since a rounded pill is what the
    /// screenshot actually shows. Kept separate from <see cref="HudView"/>
    /// since it needs to sit at a very specific screen position, not inside
    /// the top bar's horizontal layout.
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        private const float PulseDuration = 0.35f;
        private const float PulsePeakScale = 1.4f;
        private const float PulsePeakFraction = 0.4f;
        private const float PillWidth = 84f;
        private const float PillHeight = 46f;
        private const float PillSpacing = 8f;

        private RectTransform _root;
        private Text _chipsText;
        private Text _multText;
        private RectTransform _multPill;
        private Coroutine _pulseCoroutine;
        private Coroutine _multPulseCoroutine;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("ComboView", parent);
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PillSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var chipsPill = UIFactory.CreateSlicedImage(_root, "ChipsPill", UISprites.ChooseButtonBackground);
            chipsPill.rectTransform.sizeDelta = new Vector2(PillWidth, PillHeight);
            var chipsLayout = chipsPill.gameObject.AddComponent<LayoutElement>();
            chipsLayout.preferredWidth = PillWidth;
            chipsLayout.preferredHeight = PillHeight;
            _chipsText = UIFactory.CreateText(chipsPill.transform, "ChipsText", "0", 26, Color.white);
            UIFactory.StretchFull(_chipsText.rectTransform);

            var xLabel = UIFactory.CreateText(_root, "XLabel", "x", 22, UITheme.TextPrimary);
            var xLayout = xLabel.gameObject.AddComponent<LayoutElement>();
            xLayout.preferredWidth = 16f;
            xLayout.preferredHeight = PillHeight;

            var multPill = UIFactory.CreateSlicedImage(_root, "MultPill", UISprites.CancelButtonBackground);
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

        /// <summary>Updates both pills — <paramref name="chips"/> mirrors <see cref="Core.PlacementResult.Chips"/>, <paramref name="mult"/> mirrors <see cref="Core.PlacementResult.Mult"/>, both accumulated progressively by the placement sequence rather than only shown at the very end.</summary>
        public void Show(int chips, int mult)
        {
            _root.gameObject.SetActive(true);
            _chipsText.text = chips.ToString();
            _multText.text = mult.ToString();
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Brief scale-bounce on the whole readout to draw the eye — used for line-clear/modifier score moments (see GameBootstrap.PlayPlacementSequence).</summary>
        public void Pulse()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            _pulseCoroutine = StartCoroutine(PulseRoutine(_root));
        }

        /// <summary>Same bounce as <see cref="Pulse"/>, but only on the red mult pill — used the instant <see cref="Core.PlacementResult.ModifierMultiplier"/> or .ComboMultiplier actually grows it, so the "mult went up" moment reads distinctly from an ordinary chips gain.</summary>
        public void PulseMult()
        {
            if (_multPulseCoroutine != null)
            {
                StopCoroutine(_multPulseCoroutine);
            }
            _multPulseCoroutine = StartCoroutine(PulseRoutine(_multPill));
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
            if (target == _root)
            {
                _pulseCoroutine = null;
            }
            else
            {
                _multPulseCoroutine = null;
            }
        }
    }
}
