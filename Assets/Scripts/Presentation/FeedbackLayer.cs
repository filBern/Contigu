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

            var popup = UIFactory.CreateText(_root, "Popup", text, 22, color);
            popup.fontStyle = FontStyle.Bold;
            popup.rectTransform.position = anchor.position;
            popup.rectTransform.sizeDelta = new Vector2(160f, 40f);
            StartCoroutine(AnimatePopup(popup));
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
