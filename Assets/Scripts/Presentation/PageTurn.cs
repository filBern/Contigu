using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// "Turning the page" between the round and the shop (both ways): a green crayon sheet, led by Sprig, sweeps in from the right, a ruled notebook page
    /// (margin, doodles) follows a beat behind and covers the screen, the swap happens under it, then the page leaves to the left and the green sheet after it.
    /// With reduced motion the swap is immediate. Input is blocked while it runs. Art: Icons/PageTurn_Green and PageTurn_Paper (Design/Notebook/generate_page.py).
    /// </summary>
    public static class PageTurn
    {
        private const float CoverSeconds = 0.3f;
        private const float StaggerSeconds = 0.12f;
        private const float HoldSeconds = 0.08f;
        private const float UncoverSeconds = 0.34f;
        // The sprites are 1300 px wide, their leading 40 px being the edge: the 1260 px behind it span the screen.
        private const float SpriteWidth = 1300f;
        private const float EdgeWidth = 40f;
        private const float BodyWidth = 1260f;

        public static void Play(MonoBehaviour host, Transform canvasRoot, Action atCover)
        {
            if (host == null || canvasRoot == null || MotionSettings.Reduced)
            {
                if (atCover != null) { atCover(); }
                return;
            }
            host.StartCoroutine(Run(canvasRoot, atCover));
        }

        private static RectTransform BuildSheet(Transform parent, string name, string spritePath, Color fallback, float scale)
        {
            var sprite = Resources.Load<Sprite>(spritePath);
            var image = UIFactory.CreatePanel(parent, name, sprite != null ? Color.white : fallback);
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(SpriteWidth * scale, 0f);
            rect.SetAsLastSibling();
            return rect;
        }

        private static IEnumerator Run(Transform canvasRoot, Action atCover)
        {
            float screenWidth = Mathf.Max(1f, ((RectTransform)canvasRoot).rect.width);
            float scale = screenWidth / BodyWidth;
            float edge = EdgeWidth * scale;
            var green = BuildSheet(canvasRoot, "PageTurnGreen", "Icons/PageTurn_Green", new Color(0.62f, 0.83f, 0.49f, 1f), scale);
            var paper = BuildSheet(canvasRoot, "PageTurnPaper", "Icons/PageTurn_Paper", UITheme.Background, scale);
            green.SetSiblingIndex(paper.GetSiblingIndex() - 1);

            // Sprig runs ahead of the green sheet (a flipbook of her cheering pose).
            var sprigSprite = Resources.Load<Sprite>("Characters/Sprig_Cheer");
            if (sprigSprite != null)
            {
                var sprig = UIFactory.CreatePanel(green, "Sprig", Color.white);
                sprig.sprite = sprigSprite;
                sprig.preserveAspect = true;
                sprig.raycastTarget = false;
                sprig.rectTransform.anchorMin = sprig.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                sprig.rectTransform.pivot = new Vector2(1f, 0.5f);
                sprig.rectTransform.sizeDelta = new Vector2(300f, 300f);
                sprig.rectTransform.anchoredPosition = new Vector2(edge + 24f, 0f);
                SpriteFlipbook.Attach(sprig, "Characters");
            }

            float t = 0f;
            float total = CoverSeconds + StaggerSeconds;
            while (t < total)
            {
                t += GameSpeed.AnimationDelta;
                Place(green, Mathf.Clamp01(t / CoverSeconds), screenWidth, edge, true);
                Place(paper, Mathf.Clamp01((t - StaggerSeconds) / CoverSeconds), screenWidth, edge, true);
                yield return null;
            }
            Place(green, 1f, screenWidth, edge, true);
            Place(paper, 1f, screenWidth, edge, true);
            if (atCover != null) { atCover(); }
            green.SetAsLastSibling();
            paper.SetAsLastSibling();
            t = 0f;
            while (t < HoldSeconds)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }
            t = 0f;
            total = UncoverSeconds + StaggerSeconds;
            while (t < total)
            {
                t += GameSpeed.AnimationDelta;
                float q = Mathf.Clamp01(t / UncoverSeconds);
                Place(paper, q, screenWidth, edge, false);
                Place(green, Mathf.Clamp01((t - StaggerSeconds) / UncoverSeconds), screenWidth, edge, false);
                if (q >= 1f) { paper.GetComponent<Image>().raycastTarget = false; }
                yield return null;
            }
            UnityEngine.Object.Destroy(paper.gameObject);
            UnityEngine.Object.Destroy(green.gameObject);
        }

        /// <summary>Puts a sheet's leading edge at its place: covering, from the right edge of the screen to its left edge; leaving, on to fully past the left edge.</summary>
        private static void Place(RectTransform sheet, float progress, float screenWidth, float edge, bool covering)
        {
            float e = progress * progress * (3f - 2f * progress);
            float edgeX = covering ? screenWidth * (1f - e) : -(screenWidth + edge + 24f) * e;
            sheet.anchoredPosition = new Vector2(edgeX - edge, 0f);
        }
    }
}
