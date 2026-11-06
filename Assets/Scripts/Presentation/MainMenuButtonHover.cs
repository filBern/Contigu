using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Gentle scale-up on hover for the main menu's own Play/Settings/Exit buttons, on top of the color tint
    /// and click punch every button already gets from UIFactory.FinishButton/ButtonPunchEffect. Scoped to
    /// just these three rather than added globally there: several buttons elsewhere already drive their own
    /// transform.localScale continuously (e.g. HandView's Shuffle button pulse), and a global hover scale
    /// would fight them for the same value.
    /// </summary>
    public sealed class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.06f;
        private const float HoverDuration = 0.12f;

        private Coroutine _routine;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerId >= 0) { return; } // touch has no hover
            PlayTo(HoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayTo(1f);
        }

        private void PlayTo(float target)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }
            _routine = StartCoroutine(ScaleRoutine(target));
        }

        private IEnumerator ScaleRoutine(float target)
        {
            var rt = (RectTransform)transform;
            float start = rt.localScale.x;
            float t = 0f;
            while (t < HoverDuration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(start, target, Mathf.Clamp01(t / HoverDuration));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = new Vector3(target, target, 1f);
            _routine = null;
        }
    }
}
