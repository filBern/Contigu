using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// "Turning the page" between the round and the shop (both ways), quickly: crayon scribbles and big flowers (Icons/Doodles) pop in over the screen in a
    /// wave from left to right while the paper fills in underneath, until it is covered; the swap happens there; then the doodles pop back out in the same
    /// wave and the paper clears. With reduced motion the swap is immediate. Input is blocked while it runs.
    /// </summary>
    public static class PageTurn
    {
        private const int Columns = 9;
        private const int Rows = 5;
        private const float DoodleSize = 330f;
        private const float WaveSeconds = 0.18f;
        private const float PopSeconds = 0.14f;
        private const float HoldSeconds = 0.02f;
        private const int DoodleKinds = 8;

        public static void Play(MonoBehaviour host, Transform canvasRoot, Action atCover)
        {
            if (host == null || canvasRoot == null || MotionSettings.Reduced)
            {
                if (atCover != null) { atCover(); }
                return;
            }
            host.StartCoroutine(Run(canvasRoot, atCover));
        }

        private struct Piece
        {
            public RectTransform Rect;
            public float Delay;        // 0..WaveSeconds, left to right with a little disorder
            public float Spin;
        }

        private static IEnumerator Run(Transform canvasRoot, Action atCover)
        {
            var canvasRect = (RectTransform)canvasRoot;
            float width = Mathf.Max(1f, canvasRect.rect.width);
            float height = Mathf.Max(1f, canvasRect.rect.height);
            var paper = UIFactory.CreatePanel(canvasRoot, "PageTurn", new Color(UITheme.Background.r, UITheme.Background.g, UITheme.Background.b, 0f));
            paper.raycastTarget = true;                        // blocks clicks while the page is down
            UIFactory.StretchFull(paper.rectTransform);
            paper.rectTransform.SetAsLastSibling();

            var rng = new System.Random(7);
            var pieces = new List<Piece>();
            float scale = Mathf.Max(width / 1920f, 0.5f);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var sprite = Resources.Load<Sprite>("Icons/Doodles/Doodle_" + rng.Next(DoodleKinds));
                    var image = UIFactory.CreatePanel(paper.rectTransform, "Doodle", sprite != null ? Color.white : new Color(0.95f, 0.65f, 0.6f, 1f));
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    var rect = image.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    float jitterX = ((float)rng.NextDouble() - 0.5f) * width / Columns * 0.7f;
                    float jitterY = ((float)rng.NextDouble() - 0.5f) * height / Rows * 0.7f;
                    rect.anchoredPosition = new Vector2((c + 0.5f) * width / Columns + jitterX, (r + 0.5f) * height / Rows + jitterY);
                    float size = DoodleSize * scale * (0.85f + (float)rng.NextDouble() * 0.35f);
                    rect.sizeDelta = new Vector2(size, size);
                    rect.localScale = Vector3.zero;
                    float wave = (c / (float)(Columns - 1)) * 0.8f + (float)rng.NextDouble() * 0.2f;
                    pieces.Add(new Piece { Rect = rect, Delay = wave * WaveSeconds, Spin = ((float)rng.NextDouble() - 0.5f) * 50f });
                }
            }

            // Cover: the doodles pop in, the paper fills in under them.
            float t = 0f;
            float total = WaveSeconds + PopSeconds;
            while (t < total)
            {
                t += GameSpeed.AnimationDelta;
                Apply(pieces, t, true);
                paper.color = new Color(paper.color.r, paper.color.g, paper.color.b, Mathf.Clamp01(t / (total * 0.85f)));
                yield return null;
            }
            Apply(pieces, total, true);
            paper.color = new Color(paper.color.r, paper.color.g, paper.color.b, 1f);
            if (atCover != null) { atCover(); }
            paper.rectTransform.SetAsLastSibling();
            t = 0f;
            while (t < HoldSeconds)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }

            // Uncover: the same wave, the doodles shrink away and the paper clears.
            paper.raycastTarget = false;
            t = 0f;
            while (t < total)
            {
                t += GameSpeed.AnimationDelta;
                Apply(pieces, t, false);
                paper.color = new Color(paper.color.r, paper.color.g, paper.color.b, 1f - Mathf.Clamp01((t - total * 0.15f) / (total * 0.85f)));
                yield return null;
            }
            UnityEngine.Object.Destroy(paper.gameObject);
        }

        private static void Apply(List<Piece> pieces, float t, bool popIn)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece.Rect == null) { continue; }
                float p = Mathf.Clamp01((t - piece.Delay) / PopSeconds);
                float s;
                if (popIn)
                {
                    s = p < 0.7f ? Mathf.Lerp(0f, 1.15f, p / 0.7f) : Mathf.Lerp(1.15f, 1f, (p - 0.7f) / 0.3f);
                }
                else
                {
                    s = 1f - p * p;
                }
                piece.Rect.localScale = new Vector3(s, s, 1f);
                piece.Rect.localRotation = Quaternion.Euler(0f, 0f, piece.Spin * (popIn ? 1f - p : p));
            }
        }
    }
}
