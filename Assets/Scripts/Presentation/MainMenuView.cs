using System;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// First screen shown on launch: the game's name spelled out in the same
    /// colored square tiles gameplay is built from, each one a random color
    /// at build time and re-rolled to one of the other 3 colors on hover
    /// (see TitleTileView), shown in front of AnimatedBackgroundView's
    /// drifting shapes, before the pre-run flow (ChallengeSelectView)
    /// starts. No background fill of its own, so the animated background
    /// shows through behind the title; the "Play" button hands off to
    /// ChallengeSelectView.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string Title = "CONTIGU";
        private const float TileSize = 28f;
        private const float TileGap = 2f;
        // Each filled glyph cell in TitleTileFont's 5x7 dot-matrix grid renders as a Thickness x Thickness cluster of same-size tiles.
        private const int Thickness = 1;

        /// <summary>Fired once the player clicks through to actually start playing.</summary>
        public event Action PlayClicked;

        /// <summary>Fired when the player opens the Settings overlay from the main menu.</summary>
        public event Action SettingsClicked;

        /// <summary>Fired when the player asks to quit the game entirely.</summary>
        public event Action ExitClicked;

        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("MainMenuOverlay", parent);
            UIFactory.StretchFull(_root);

            var titleContainer = BuildTitle(_root);
            titleContainer.anchorMin = new Vector2(0.5f, 0.5f);
            titleContainer.anchorMax = new Vector2(0.5f, 0.5f);
            titleContainer.pivot = new Vector2(0.5f, 0.5f);
            titleContainer.anchoredPosition = new Vector2(0f, 130f);

            var playButton = UIFactory.CreateButton(_root, "Play", "Play", UISprites.ChooseButtonBackground, 22);
            var playRect = playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.5f);
            playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = new Vector2(0f, -60f);
            playRect.sizeDelta = new Vector2(220f, 56f);
            playButton.onClick.AddListener(OnPlayClicked);

            var settingsButton = UIFactory.CreateButton(_root, "Settings", "Settings", UISprites.ChooseButtonBackground, 22);
            var settingsRect = settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.pivot = new Vector2(0.5f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(0f, -136f);
            settingsRect.sizeDelta = new Vector2(220f, 56f);
            settingsButton.onClick.AddListener(OnSettingsClicked);

            var exitButton = UIFactory.CreateButton(_root, "Exit", "Exit", UISprites.CancelButtonBackground, 22);
            var exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.5f, 0.5f);
            exitRect.anchorMax = new Vector2(0.5f, 0.5f);
            exitRect.pivot = new Vector2(0.5f, 0.5f);
            exitRect.anchoredPosition = new Vector2(0f, -212f);
            exitRect.sizeDelta = new Vector2(220f, 56f);
            exitButton.onClick.AddListener(OnExitClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Lays out a Thickness x Thickness cluster of tiles per filled glyph cell, letter by letter with a 1-column gap between letters, centered on <paramref name="parent"/>. Each tile starts at its own random color and recolors independently on hover (see TitleTileView).</summary>
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

        private void OnSettingsClicked()
        {
            if (SettingsClicked != null)
            {
                SettingsClicked();
            }
        }

        private void OnExitClicked()
        {
            if (ExitClicked != null)
            {
                ExitClicked();
            }
        }
    }
}
