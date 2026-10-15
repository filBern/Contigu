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
    /// the main menu for gameplay (see MainMenuBootstrap.OnPlayClicked) — same tile look
    /// (VisualDefaults.TileSprite) and pop-in overshoot curve as the title's own entrance (see
    /// TitleTileView.PlayEntrance), but swept corner-to-corner across the whole screen instead of just
    /// spelling "CONTIGU". Built lazily on Play, not at Awake time, so the canvas RectTransform's actual
    /// size (which varies with screen aspect — CanvasScaler matches only on height) is already settled.
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

        private static readonly PieceColor[] Colors =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime
        };

        private struct SweepTile
        {
            public RectTransform Rect;
            public float Delay;
        }

        /// <summary>Builds the tile grid (sized to <paramref name="canvasRect"/>'s current actual size) and plays the cover-sweep, invoking <paramref name="onCovered"/> once the whole screen is opaque and held for a beat.</summary>
        public void PlayCoverSweep(RectTransform canvasRect, Action onCovered)
        {
            var container = UIFactory.CreateUIObject("TransitionSweep", canvasRect);
            UIFactory.StretchFull(container);
            container.SetAsLastSibling();

            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            int cols = Mathf.CeilToInt(width / TileSize) + 1;
            int rows = Mathf.CeilToInt(height / TileSize) + 1;
            float startX = -width / 2f + TileSize / 2f;
            float startY = height / 2f - TileSize / 2f;
            float maxDistance = Mathf.Sqrt(cols * cols + rows * rows);

            var tiles = new SweepTile[cols * rows];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var tileImage = UIFactory.CreateSlicedImage(container, "SweepTile_" + row + "_" + col, VisualDefaults.TileSprite);
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

                    float distance = Mathf.Sqrt(col * col + row * row);
                    tiles[index] = new SweepTile { Rect = rt, Delay = distance / maxDistance * SweepDuration };
                    index++;
                }
            }

            StartCoroutine(SweepRoutine(tiles, onCovered));
        }

        private IEnumerator SweepRoutine(SweepTile[] tiles, Action onCovered)
        {
            float elapsed = 0f;
            float totalDuration = SweepDuration + PopDuration;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < tiles.Length; i++)
                {
                    float t = elapsed - tiles[i].Delay;
                    if (t <= 0f)
                    {
                        continue;
                    }
                    float p = Mathf.Clamp01(t / PopDuration);
                    float scale = p < PopPeakFraction
                        ? Mathf.Lerp(0f, PopPeakScale, p / PopPeakFraction)
                        : Mathf.Lerp(PopPeakScale, 1f, (p - PopPeakFraction) / (1f - PopPeakFraction));
                    tiles[i].Rect.localScale = new Vector3(scale, scale, 1f);
                }
                yield return null;
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].Rect.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(HoldDuration);
            onCovered?.Invoke();
        }
    }
}
