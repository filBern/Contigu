using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// "Turning the page" between the round and the shop (both ways): a sheet of paper with an ink edge slides in from the right and covers the screen,
    /// the swap happens behind it, then it slides out to the left. With reduced motion the swap is immediate. Input is blocked while it runs.
    /// </summary>
    public static class PageTurn
    {
        private const float CoverSeconds = 0.26f;
        private const float HoldSeconds = 0.06f;
        private const float UncoverSeconds = 0.3f;

        public static void Play(MonoBehaviour host, Transform canvasRoot, Action atCover)
        {
            if (host == null || canvasRoot == null || MotionSettings.Reduced)
            {
                if (atCover != null) { atCover(); }
                return;
            }
            host.StartCoroutine(Run(canvasRoot, atCover));
        }

        private static IEnumerator Run(Transform canvasRoot, Action atCover)
        {
            var paper = UIFactory.CreatePanel(canvasRoot, "PageTurn", UITheme.Background);
            paper.raycastTarget = true;                       // blocks clicks while the page is down
            var rect = paper.rectTransform;
            UIFactory.StretchFull(rect);
            rect.SetAsLastSibling();
            // The page's leading (left) edge: an ink line and a soft shadow cast on what is still visible.
            var edge = UIFactory.CreatePanel(rect, "Edge", UITheme.Ink);
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = new Vector2(0f, 0f);
            edge.rectTransform.anchorMax = new Vector2(0f, 1f);
            edge.rectTransform.pivot = new Vector2(1f, 0.5f);
            edge.rectTransform.sizeDelta = new Vector2(6f, 0f);
            edge.rectTransform.anchoredPosition = Vector2.zero;
            var shadow = UIFactory.CreatePanel(rect, "Shadow", new Color(UITheme.Ink.r, UITheme.Ink.g, UITheme.Ink.b, 0.16f));
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = new Vector2(0f, 0f);
            shadow.rectTransform.anchorMax = new Vector2(0f, 1f);
            shadow.rectTransform.pivot = new Vector2(1f, 0.5f);
            shadow.rectTransform.sizeDelta = new Vector2(46f, 0f);
            shadow.rectTransform.anchoredPosition = new Vector2(-6f, 0f);

            float width = Mathf.Max(1f, ((RectTransform)canvasRoot).rect.width);
            float t = 0f;
            while (t < CoverSeconds)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / CoverSeconds);
                rect.anchoredPosition = new Vector2(width * (1f - Ease(p)), 0f);
                yield return null;
            }
            rect.anchoredPosition = Vector2.zero;
            if (atCover != null) { atCover(); }
            rect.SetAsLastSibling();
            t = 0f;
            while (t < HoldSeconds)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }
            paper.raycastTarget = false;
            t = 0f;
            while (t < UncoverSeconds)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / UncoverSeconds);
                rect.anchoredPosition = new Vector2(-width * Ease(p), 0f);
                yield return null;
            }
            UnityEngine.Object.Destroy(paper.gameObject);
        }

        private static float Ease(float p)
        {
            return p * p * (3f - 2f * p);
        }
    }
}
