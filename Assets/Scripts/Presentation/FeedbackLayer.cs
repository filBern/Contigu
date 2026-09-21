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

            // This layer is built before ModifierPanelView (see GameBootstrap.
            // BuildUI), so its popups were sitting BEHIND the panel's opaque
            // background — invisible whenever a modifier's score popup
            // anchors on its badge. Always render above everything else on
            // screen, same fix TooltipView already applies to itself.
            _root.SetAsLastSibling();

            var popup = UIFactory.CreateText(_root, "Popup", text, 22, color);
            // Small random horizontal jitter so several popups landing on the
            // same cell (e.g. two group-bonus hits in a row) stay legible
            // instead of perfectly overlapping.
            float jitterX = Random.Range(-16f, 16f);
            popup.rectTransform.position = anchor.position + new Vector3(jitterX, 0f, 0f);
            popup.rectTransform.sizeDelta = new Vector2(160f, 40f);
            StartCoroutine(AnimatePopup(popup));
        }

        /// <summary>
        /// Spawns a popup at <paramref name="fromWorldPosition"/> that flies
        /// toward <paramref name="toAnchor"/> and fades out on arrival —
        /// used for Lueur group popups, which travel from the middle of the
        /// scoring group to the Lueur HUD label (explicit request: "les
        /// points lueur partent du milieu du groupe... et aillent vers le
        /// texte du score de lueur"), unlike <see cref="SpawnPopup"/>'s
        /// float-up-in-place.
        /// </summary>
        public void SpawnFlyingPopup(Vector3 fromWorldPosition, RectTransform toAnchor, string text, Color color)
        {
            if (toAnchor == null)
            {
                return;
            }

            _root.SetAsLastSibling();

            var popup = UIFactory.CreateText(_root, "FlyingPopup", text, 20, color);
            popup.rectTransform.position = fromWorldPosition;
            popup.rectTransform.sizeDelta = new Vector2(120f, 36f);
            StartCoroutine(AnimateFlyingPopup(popup, toAnchor));
        }

        private IEnumerator AnimateFlyingPopup(Text text, RectTransform toAnchor)
        {
            var rect = text.rectTransform;
            // Faster and ease-IN (accelerating) rather than SpawnPopup's slow
            // linear float — this one is chasing a fixed destination, so it
            // should read as being pulled in rather than drifting.
            const float duration = 0.5f;
            float t = 0f;
            Vector3 startPos = rect.position;
            Color startColor = text.color;

            while (t < duration)
            {
                if (rect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p;
                rect.position = Vector3.Lerp(startPos, toAnchor.position, eased);

                // Only starts fading in the final stretch, so it reads clearly
                // for most of the flight and just disappears on arrival.
                float fadeP = Mathf.Clamp01((p - 0.7f) / 0.3f);
                var c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeP);
                text.color = c;

                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        private IEnumerator AnimatePopup(Text text)
        {
            var rect = text.rectTransform;
            // Slow, readable float+fade — several of these play in a staggered
            // sequence per placement, so each one needs enough time on screen to
            // actually be read before the next appears.
            const float duration = 1.3f;
            const float holdFraction = 0.35f; // stay fully opaque before fading
            float t = 0f;
            Vector3 startPos = rect.position;
            Color startColor = text.color;

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

                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
    }
}
