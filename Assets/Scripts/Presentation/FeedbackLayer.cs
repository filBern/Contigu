using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Spawns short-lived floating "+N" popups for score feedback (spec 9.7).</summary>
    public sealed class FeedbackLayer : MonoBehaviour
    {
        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            var root = UIFactory.CreateUIObject("FeedbackLayer", parent);
            UIFactory.StretchFull(root);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _root = root;
            return root;
        }

        public void SpawnPopup(RectTransform anchor, string text, Color color)
        {
            if (anchor == null)
            {
                return;
            }

            var popup = UIFactory.CreateText(_root, "Popup", text, 20, color);
            popup.fontStyle = FontStyle.Bold;
            // Small random horizontal jitter so several popups landing on the
            // same cell (e.g. two neighbor-bonus hits in a row) stay legible
            // instead of perfectly overlapping.
            float jitterX = Random.Range(-16f, 16f);
            popup.rectTransform.position = anchor.position + new Vector3(jitterX, 0f, 0f);
            popup.rectTransform.sizeDelta = new Vector2(160f, 40f);
            StartCoroutine(AnimatePopup(popup));
        }

        /// <summary>
        /// Same as <see cref="SpawnPopup"/> but waits <paramref name="delaySeconds"/>
        /// first — used to play a sequence of score events one after another
        /// instead of dumping them all on screen at once (see GameBootstrap).
        /// </summary>
        public void SpawnPopupDelayed(RectTransform anchor, string text, Color color, float delaySeconds)
        {
            if (anchor == null)
            {
                return;
            }

            if (delaySeconds <= 0f)
            {
                SpawnPopup(anchor, text, color);
                return;
            }

            StartCoroutine(SpawnPopupAfterDelay(anchor, text, color, delaySeconds));
        }

        private IEnumerator SpawnPopupAfterDelay(RectTransform anchor, string text, Color color, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            SpawnPopup(anchor, text, color);
        }

        private IEnumerator AnimatePopup(Text text)
        {
            var rect = text.rectTransform;
            const float duration = 0.9f;
            float t = 0f;
            Vector3 startPos = rect.position;
            Color startColor = text.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                rect.position = startPos + new Vector3(0f, 48f * p, 0f);
                var c = startColor;
                c.a = Mathf.Lerp(1f, 0f, p);
                text.color = c;
                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
    }
}
