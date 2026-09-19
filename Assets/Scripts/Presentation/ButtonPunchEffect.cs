using System.Collections;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Brief scale-punch (squish down, spring back) played on click — same
    /// scale-bounce technique as GridCellView.Pulse()/ComboView.Pulse(), just
    /// triggered by a button click instead of a score event. Added to every
    /// button by UIFactory.FinishButton so all of them (Choose, Cancel,
    /// color picks, restart, ...) get the same tactile click feedback
    /// without each call site wiring it up itself.
    /// </summary>
    public sealed class ButtonPunchEffect : MonoBehaviour
    {
        private const float PunchDuration = 0.16f;
        private const float PunchMinScale = 0.9f;

        private Coroutine _routine;

        public void Punch()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }
            _routine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            var rt = (RectTransform)transform;
            float t = 0f;
            while (t < PunchDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PunchDuration);
                float scale = p < 0.5f
                    ? Mathf.Lerp(1f, PunchMinScale, p / 0.5f)
                    : Mathf.Lerp(PunchMinScale, 1f, (p - 0.5f) / 0.5f);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _routine = null;
        }
    }
}
