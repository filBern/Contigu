using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Spawns short-lived floating "+N" popups for score feedback (spec 9.7).</summary>
    public sealed class FeedbackLayer : MonoBehaviour
    {
        /// <summary>How long one popup stays on screen (float + fade), for callers that need to time a sequence around it (see GameBootstrap).</summary>
        public const float PopupDurationSeconds = 1.3f;

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

            var popup = UIFactory.CreateText(_root, "Popup", text, 22, color);
            popup.fontStyle = FontStyle.Bold;
            // Dark outline so light/white popup text (e.g. the group-bonus
            // color) stays legible against light pastel piece colors instead of
            // disappearing into them.
            var outline = popup.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            // Small random horizontal jitter so several popups landing on the
            // same cell (e.g. two group-bonus hits in a row) stay legible
            // instead of perfectly overlapping.
            float jitterX = Random.Range(-16f, 16f);
            popup.rectTransform.position = anchor.position + new Vector3(jitterX, 0f, 0f);
            popup.rectTransform.sizeDelta = new Vector2(160f, 40f);
            StartCoroutine(AnimatePopup(popup, outline));
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

        private IEnumerator AnimatePopup(Text text, Outline outline)
        {
            var rect = text.rectTransform;
            // Slow, readable float+fade — several of these play in a staggered
            // sequence per placement, so each one needs enough time on screen to
            // actually be read before the next appears.
            const float duration = PopupDurationSeconds;
            const float holdFraction = 0.35f; // stay fully opaque before fading
            float t = 0f;
            Vector3 startPos = rect.position;
            Color startColor = text.color;
            Color startOutlineColor = outline.effectColor;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                rect.position = startPos + new Vector3(0f, 60f * p, 0f);

                float fadeP = Mathf.Clamp01((p - holdFraction) / (1f - holdFraction));
                float alpha = Mathf.Lerp(1f, 0f, fadeP);

                var c = startColor;
                c.a = alpha;
                text.color = c;

                var oc = startOutlineColor;
                oc.a = startOutlineColor.a * alpha;
                outline.effectColor = oc;

                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
    }
}
