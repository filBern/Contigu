using System;
using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Full-screen grid of colored square tiles that sweeps in diagonally to cover the screen when leaving
    /// the main menu for gameplay (see MainMenuBootstrap.OnPlayClicked), then — once the gameplay scene has
    /// finished building its own UI (see GameBootstrap.Awake) — sweeps back out toward the top-right corner
    /// to reveal it, continuing the same animation across the scene swap rather than cutting straight to a
    /// static board. Same tile look (VisualDefaults.TileSprite) and pop overshoot curve as the title's own
    /// entrance (see TitleTileView.PlayEntrance).
    ///
    /// Built on its own standalone Canvas (not nested under either scene's own canvas) so the whole thing
    /// can survive the scene load via DontDestroyOnLoad — a child of a scene's canvas would be destroyed
    /// along with everything else in that scene the moment SceneManager.LoadScene runs.
    /// </summary>
    public sealed class MainMenuTransitionView : MonoBehaviour
    {
        private const float TileSize = 48f;
        private const float PopDuration = 0.22f;
        private const float PopPeakScale = 1.25f;
        private const float PopPeakFraction = 0.55f;
        // How long the diagonal sweep takes to reach the far corner, on top of each tile's own PopDuration.
        private const float SweepDuration = 0.45f;
        // Pause once every tile is fully opaque, so the covered screen reads clearly before the scene swap.
        private const float HoldDuration = 0.15f;
        private const int SortingOrder = 1000;

        private static readonly PieceColor[] Colors =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime
        };

        /// <summary>
        /// Set once a cover sweep has fully covered the screen and survived (DontDestroyOnLoad) into the
        /// scene load it triggered. The next scene's own bootstrap (see GameBootstrap.Awake) checks this at
        /// startup and, if set, calls PlayUncoverSweep to continue the animation rather than leaving the
        /// screen covered forever; it then clears this field itself.
        /// </summary>
        public static MainMenuTransitionView PendingReveal;

        private struct SweepTile
        {
            public RectTransform Rect;
            // Distance-from-top-left delay, used by the cover sweep (tiles nearest the title's own top-left
            // corner pop in first, same direction the title's own entrance cascades).
            public float CoverDelay;
            // Diagonal-toward-top-right delay, used by the uncover sweep: tiles nearest the bottom-left
            // vanish first, the last ones to go are those nearest the top-right corner — reads as the whole
            // covering mass retreating off toward the top-right rather than just rewinding the cover.
            public float UncoverDelay;
        }

        private SweepTile[] _tiles;

        /// <summary>
        /// Builds a brand-new, independent full-screen Canvas and tile grid (sized to match <paramref
        /// name="referenceCanvasRect"/>'s current actual size — CanvasScaler only matches on height, so
        /// width varies with screen aspect) and plays the cover-sweep. Called on Play, not at Build/Awake
        /// time, so that reference size is already settled. Once fully covered, the whole thing is marked to
        /// survive the scene load it's about to trigger, and <paramref name="onCovered"/> fires.
        /// </summary>
        public static void PlayCoverSweep(RectTransform referenceCanvasRect, Action onCovered)
        {
            var root = new GameObject("MainMenuTransitionSweep", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();

            var view = root.AddComponent<MainMenuTransitionView>();
            view.BuildAndPlayCover(referenceCanvasRect.rect.width, referenceCanvasRect.rect.height, onCovered);
        }

        private void BuildAndPlayCover(float width, float height, Action onCovered)
        {
            int cols = Mathf.CeilToInt(width / TileSize) + 1;
            int rows = Mathf.CeilToInt(height / TileSize) + 1;
            float startX = -width / 2f + TileSize / 2f;
            float startY = height / 2f - TileSize / 2f;
            float maxCornerDistance = Mathf.Sqrt(cols * cols + rows * rows);
            // Projection onto the "toward top-right" direction (right is +col, up is -row): ranges from
            // -(rows-1) at the bottom-left corner to (cols-1) at the top-right corner.
            float minProjection = -(rows - 1);
            float maxProjection = cols - 1;
            float projectionRange = Mathf.Max(0.0001f, maxProjection - minProjection);

            var tiles = new SweepTile[cols * rows];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var tileImage = UIFactory.CreateSlicedImage(transform, "SweepTile_" + row + "_" + col, VisualDefaults.TileSprite);
                    var rt = tileImage.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(TileSize, TileSize);
                    rt.anchoredPosition = new Vector2(startX + col * TileSize, startY - row * TileSize);
                    rt.localScale = Vector3.zero;
                    // Pseudo-random, deterministic color mosaic — no two orthogonal neighbors share a color
                    // often enough to read as a real pattern rather than a flat wash.
                    tileImage.color = VisualDefaults.GetColor(Colors[(row * 7 + col * 13) % Colors.Length]);

                    float cornerDistance = Mathf.Sqrt(col * col + row * row);
                    float projection = col - row;
                    tiles[index] = new SweepTile
                    {
                        Rect = rt,
                        CoverDelay = cornerDistance / maxCornerDistance * SweepDuration,
                        UncoverDelay = (projection - minProjection) / projectionRange * SweepDuration
                    };
                    index++;
                }
            }
            _tiles = tiles;

            StartCoroutine(SweepRoutine(forward: true, () =>
            {
                DontDestroyOnLoad(gameObject);
                PendingReveal = this;
                onCovered?.Invoke();
            }));
        }

        /// <summary>
        /// Continues the cover animation in reverse, sweeping the tiles away toward the top-right corner
        /// (see UncoverDelay) instead of simply rewinding the cover, then destroys this whole GameObject
        /// (including its standalone Canvas) once every tile is gone.
        /// </summary>
        public void PlayUncoverSweep(Action onRevealed)
        {
            StartCoroutine(SweepRoutine(forward: false, () =>
            {
                onRevealed?.Invoke();
                Destroy(gameObject);
            }));
        }

        private IEnumerator SweepRoutine(bool forward, Action onDone)
        {
            float elapsed = 0f;
            float totalDuration = SweepDuration + PopDuration;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < _tiles.Length; i++)
                {
                    float delay = forward ? _tiles[i].CoverDelay : _tiles[i].UncoverDelay;
                    float t = elapsed - delay;
                    if (t <= 0f)
                    {
                        continue;
                    }
                    float p = Mathf.Clamp01(t / PopDuration);
                    float scale;
                    if (forward)
                    {
                        // Pop in: 0 -> peak -> 1.
                        scale = p < PopPeakFraction
                            ? Mathf.Lerp(0f, PopPeakScale, p / PopPeakFraction)
                            : Mathf.Lerp(PopPeakScale, 1f, (p - PopPeakFraction) / (1f - PopPeakFraction));
                    }
                    else
                    {
                        // Pop out: 1 -> peak -> 0 — same overshoot shape, mirrored.
                        scale = p < PopPeakFraction
                            ? Mathf.Lerp(1f, PopPeakScale, p / PopPeakFraction)
                            : Mathf.Lerp(PopPeakScale, 0f, (p - PopPeakFraction) / (1f - PopPeakFraction));
                    }
                    _tiles[i].Rect.localScale = new Vector3(scale, scale, 1f);
                }
                yield return null;
            }
            float finalScale = forward ? 1f : 0f;
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].Rect.localScale = new Vector3(finalScale, finalScale, 1f);
            }

            if (forward)
            {
                yield return new WaitForSeconds(HoldDuration);
            }
            onDone?.Invoke();
        }
    }
}
