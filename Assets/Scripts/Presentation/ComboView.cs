using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Big, prominent "Combo: +N" readout sitting in the gap between the grid
    /// and the hand — shows the running total of whichever placement is
    /// currently animating (see GameBootstrap.PlayPlacementSequence). Kept
    /// separate from <see cref="HudView"/> since it needs to sit at a very
    /// specific screen position, not inside the top bar's horizontal layout.
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        private const float PulseDuration = 0.35f;
        private const float PulsePeakScale = 1.4f;
        private const float PulsePeakFraction = 0.4f;

        private Text _text;
        private Coroutine _pulseCoroutine;

        public RectTransform Build(Transform parent)
        {
            _text = UIFactory.CreateText(parent, "Combo", "", 30, UITheme.Modifier);
            _text.fontStyle = FontStyle.Bold;
            var outline = _text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = _text.rectTransform;
            rt.gameObject.SetActive(false);
            return rt;
        }

        public void Show(int amount)
        {
            _text.gameObject.SetActive(true);
            _text.text = "Combo: +" + amount;
        }

        public void Hide()
        {
            _text.gameObject.SetActive(false);
        }

        /// <summary>Brief scale-bounce to draw the eye to the readout — used for the Balatro-style "xN" multiplier moment (see GameBootstrap.PlayPlacementSequence).</summary>
        public void Pulse()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            var rt = _text.rectTransform;
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _pulseCoroutine = null;
        }
    }
}
