using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Brief scale-punch (squish down, spring back) played on click — same scale-bounce technique as
    /// GridCellView.Pulse()/ComboView.PulseChips(). Added to every button by UIFactory.FinishButton, and the
    /// single place every button's hover/click SFX plays from (see SfxManager).
    /// </summary>
    public sealed class ButtonPunchEffect : MonoBehaviour, IPointerEnterHandler
    {
        private const float PunchDuration = 0.16f;
        private const float PunchMinScale = 0.9f;

        private Coroutine _routine;

        public void OnPointerEnter(PointerEventData eventData)
        {
            SfxManager.Play(SfxId.ButtonHover);
        }

        public void Punch()
        {
            SfxManager.Play(SfxId.ButtonClick);
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
