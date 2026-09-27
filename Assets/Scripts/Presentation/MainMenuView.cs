using System;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// First screen shown on launch (spec extension, explicit request): the
    /// game's own name spelled out in the same colored square tiles
    /// gameplay is built from, each one a random color at build time and
    /// re-rolled to one of the other 3 colors on hover (see TitleTileView)
    /// — a small playable flourish in front of AnimatedBackgroundView's
    /// already-drifting shapes, shown before the existing pre-run flow
    /// (ChallengeSelectView) even starts. No background fill of its own on
    /// purpose, so the animated background shows through behind the title
    /// exactly as it does everywhere else; the "Jouer" button just hands
    /// off to ChallengeSelectView, which is unchanged otherwise.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string Title = "CONTIGU";
        private const float TileSize = 14f;
        private const float TileGap = 2f;
        // "deux blocs d'épaisseur" (explicit follow-up request) — every
        // filled glyph cell in TitleTileFont's 5x7 dot-matrix grid renders
        // as a Thickness x Thickness cluster of tiles instead of a single
        // one, so every stroke reads as 2 tiles thick instead of 1 (a plain
        // 2x nearest-neighbor upscale of the glyph bitmap, not a redesign of
        // the letterforms themselves). Doubles the whole logo's footprint
        // as a side effect, which reads fine for a title screen with no
        // competing HUD elements around it.
        private const int Thickness = 2;

        /// <summary>Fired once the player clicks through to actually start playing.</summary>
        public event Action PlayClicked;

        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("MainMenuOverlay", parent);
            UIFactory.StretchFull(_root);

            var titleContainer = BuildTitle(_root);
            titleContainer.anchorMin = new Vector2(0.5f, 0.5f);
            titleContainer.anchorMax = new Vector2(0.5f, 0.5f);
            titleContainer.pivot = new Vector2(0.5f, 0.5f);
            // Raised from the old single-thickness offset (60) to keep the
            // same breathing room above the Jouer button now that the title
            // is twice as tall (Thickness = 2).
            titleContainer.anchoredPosition = new Vector2(0f, 110f);

            var playButton = UIFactory.CreateButton(_root, "Play", "Jouer", UISprites.ChooseButtonBackground, 22);
            var playRect = playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.5f);
            playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = new Vector2(0f, -100f);
            playRect.sizeDelta = new Vector2(220f, 56f);
            playButton.onClick.AddListener(OnPlayClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Lays out a Thickness x Thickness cluster of tiles per filled glyph cell, letter by letter with a (scaled) 1-column gap between letters, centered on <paramref name="parent"/>. Each individual tile in the cluster still starts at its own independently-random color and still recolors independently on its own hover (see TitleTileView) — same per-tile interactivity as before, just more, smaller tiles making up each stroke.</summary>
        private RectTransform BuildTitle(Transform parent)
        {
            var container = UIFactory.CreateUIObject("Title", parent);

            float stride = TileSize + TileGap;
            int glyphCols = TitleTileFont.GlyphWidth * Thickness;
            int glyphRows = TitleTileFont.GlyphHeight * Thickness;
            int letterGapCols = Thickness; // scaled version of the original 1-column gap between letters
            int totalCols = Title.Length * (glyphCols + letterGapCols) - letterGapCols; // no trailing gap after the last letter
            float totalWidth = totalCols * stride - TileGap;
            float totalHeight = glyphRows * stride - TileGap;
            container.sizeDelta = new Vector2(totalWidth, totalHeight);

            float startX = -totalWidth / 2f + TileSize / 2f;
            float startY = totalHeight / 2f - TileSize / 2f;

            int letterStartCol = 0;
            for (int i = 0; i < Title.Length; i++)
            {
                char letter = Title[i];
                for (int row = 0; row < TitleTileFont.GlyphHeight; row++)
                {
                    for (int col = 0; col < TitleTileFont.GlyphWidth; col++)
                    {
                        if (!TitleTileFont.IsCellFilled(letter, col, row))
                        {
                            continue;
                        }

                        for (int subRow = 0; subRow < Thickness; subRow++)
                        {
                            for (int subCol = 0; subCol < Thickness; subCol++)
                            {
                                int globalCol = letterStartCol + col * Thickness + subCol;
                                int globalRow = row * Thickness + subRow;

                                var tileImage = UIFactory.CreateSlicedImage(container, "Tile_" + i + "_" + row + "_" + col + "_" + subRow + "_" + subCol, VisualDefaults.TileSprite);
                                tileImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                                tileImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                                tileImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                                tileImage.rectTransform.sizeDelta = new Vector2(TileSize, TileSize);
                                tileImage.rectTransform.anchoredPosition = new Vector2(startX + globalCol * stride, startY - globalRow * stride);

                                var tileView = tileImage.gameObject.AddComponent<TitleTileView>();
                                tileView.Init(tileImage, TitleTileView.RandomColor());
                            }
                        }
                    }
                }
                letterStartCol += glyphCols + letterGapCols;
            }

            return container;
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        private void OnPlayClicked()
        {
            if (PlayClicked != null)
            {
                PlayClicked();
            }
        }
    }
}
