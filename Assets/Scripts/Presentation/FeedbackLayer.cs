using System.Collections;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Spawns short-lived floating "+N" popups for score feedback (spec 9.7).</summary>
    public sealed class FeedbackLayer : MonoBehaviour
    {
        private const float ScoreBackdropOpacity = 0.5f;
        private const float ScoreBackdropSpinDegreesPerSecond = 720f;
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

        /// <summary><paramref name="floatDown"/> flips the usual float-up direction to float down instead — used for popups anchored near the top of the screen, which would otherwise run off-screen.</summary>
        public void SpawnPopup(RectTransform anchor, string text, Color color, bool floatDown = false)
        {
            if (anchor == null)
            {
                return;
            }

            // Always render above everything else on screen, so a popup anchored behind an opaque panel still shows.
            _root.SetAsLastSibling();

            var container = UIFactory.CreateUIObject("PopupContainer", _root);
            // Small random horizontal jitter so several popups landing on the
            // same cell (e.g. two group-bonus hits in a row) stay legible
            // instead of perfectly overlapping.
            float jitterX = Random.Range(-16f, 16f);
            container.position = anchor.position + new Vector3(jitterX, 0f, 0f);
            container.sizeDelta = new Vector2(160f, 40f);

            // Plain square backdrop — the 45°-rotated "diamond" shape is reserved for HudView/ShopView's Lueur icon.
            var backdrop = UIFactory.CreatePanel(container, "Backdrop", UITheme.Panel);
            backdrop.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.anchoredPosition = Vector2.zero;
            backdrop.rectTransform.sizeDelta = new Vector2(26f, 26f);
            var backdropColor = backdrop.color;
            backdropColor.a = ScoreBackdropOpacity;
            backdrop.color = backdropColor;

            var popup = UIFactory.CreateText(container, "Popup", text, 22, color);
            UIFactory.StretchFull(popup.rectTransform);

            StartCoroutine(AnimatePopup(container, backdrop, popup, floatDown));
        }

        /// <summary>Spawns a popup at <paramref name="fromWorldPosition"/> that flies toward <paramref name="toAnchor"/> and fades out on arrival, unlike <see cref="SpawnPopup"/>'s float-up-in-place.</summary>
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

        /// <summary>
        /// Thief's steal: spawns a non-interactive preview of the stolen piece at <paramref name="fromWorldPosition"/>
        /// (its hand slot — already empty by the time this plays, see GameBootstrap.PlayThiefStealEffect) that
        /// flies toward <paramref name="toAnchor"/> (Thief's own icon) and fades out on arrival, same flight
        /// curve as <see cref="SpawnFlyingPopup"/>. Uses ShapePreviewFactory.Build with no trait/tooltip — the
        /// ghost is purely visual, never clickable or hoverable.
        /// </summary>
        public void SpawnFlyingPiece(Vector3 fromWorldPosition, RectTransform toAnchor, PieceShape shape, PieceColor color)
        {
            if (toAnchor == null)
            {
                return;
            }

            _root.SetAsLastSibling();

            var container = UIFactory.CreateUIObject("FlyingPiece", _root);
            container.position = fromWorldPosition;
            container.sizeDelta = new Vector2(100f, 110f);
            var canvasGroup = container.gameObject.AddComponent<CanvasGroup>();
            ShapePreviewFactory.Build(container, shape, color, null, null, null);
            StartCoroutine(AnimateFlyingPiece(container, canvasGroup, toAnchor));
        }

        private IEnumerator AnimateFlyingPiece(RectTransform rect, CanvasGroup canvasGroup, RectTransform toAnchor)
        {
            const float duration = 0.5f;
            float t = 0f;
            Vector3 startPos = rect.position;
            Vector3 startScale = rect.localScale;

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
                rect.localScale = Vector3.Lerp(startScale, startScale * 0.4f, eased);

                float fadeP = Mathf.Clamp01((p - 0.7f) / 0.3f);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeP);

                yield return null;
            }

            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }

        private IEnumerator AnimateFlyingPopup(Text text, RectTransform toAnchor)
        {
            var rect = text.rectTransform;
            // Ease-in (accelerating) rather than a linear float — reads as being pulled toward a fixed destination.
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

        private IEnumerator AnimatePopup(RectTransform container, Image backdrop, Text text, bool floatDown)
        {
            const float duration = 1.3f;
            const float holdFraction = 0.35f; // stay fully opaque before fading
            float t = 0f;
            Vector3 startPos = container.position;
            Color textColor = text.color;
            Color backdropColor = backdrop.color;
            float verticalSign = floatDown ? -1f : 1f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                container.position = startPos + new Vector3(0f, verticalSign * 60f * p, 0f);

                float fadeP = Mathf.Clamp01((p - holdFraction) / (1f - holdFraction));
                float alpha = Mathf.Lerp(1f, 0f, fadeP);

                var tc = textColor;
                tc.a = alpha;
                text.color = tc;

                var dc = backdropColor;
                dc.a = backdropColor.a * alpha;
                backdrop.color = dc;
                backdrop.rectTransform.Rotate(0f, 0f, ScoreBackdropSpinDegreesPerSecond * Time.deltaTime);

                yield return null;
            }

            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }
    }
}
