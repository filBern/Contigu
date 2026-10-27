using System;
using System.Collections.Generic;
using Contigu.Core;
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
        private const string Title = "LASTBLOOM";
        // Sized so the 9-letter title (53 tile columns) fits within the 1280-wide reference resolution with margin.
        private const float TileSize = 18f;
        private const float TileGap = 2f;
        // Each filled glyph cell in TitleTileFont's 5x7 dot-matrix grid renders as a Thickness x Thickness cluster of same-size tiles.
        private const int Thickness = 1;
        // Left-to-right cascade for the title's entrance (see TitleTileView.PlayEntrance) — keyed off each
        // tile's own column so it reads as one wave sweeping across the title regardless of row, rather than
        // every tile of a letter popping in together. The column count (and so the total cascade time) scales
        // with Title's length automatically via BuildTitle, so this delay-per-column stays easy to reason about
        // without having to re-derive a fixed total each time the title text changes.
        private const float TitleCascadeDelayPerColumn = 0.015f;

        /// <summary>Fired once the player clicks through to actually start playing.</summary>
        public event Action PlayClicked;

        /// <summary>Fired when the player opens the Collection from the main menu.</summary>
        public event Action CollectionClicked;

        /// <summary>Fired when the player opens the Settings overlay from the main menu.</summary>
        public event Action SettingsClicked;

        /// <summary>Fired when the player asks to quit the game entirely.</summary>
        public event Action ExitClicked;

        private RectTransform _root;
        // Entrance (see TitleTileView.PlayEntrance) can't play during Build(): the whole menu is still
        // SetActive(false) at the very end of that same call, which kills any coroutine already started on
        // a now-inactive GameObject rather than pausing it — so every tile's own PlayEntrance is deferred to
        // here and fired from Show() instead, once the hierarchy is actually active again.
        private readonly List<(TitleTileView View, float Delay)> _titleTiles = new List<(TitleTileView, float)>();

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("MainMenuOverlay", parent);
            UIFactory.StretchFull(_root);

            var titleContainer = BuildLogo(_root) ?? BuildTitle(_root);
            titleContainer.anchorMin = new Vector2(0.5f, 0.5f);
            titleContainer.anchorMax = new Vector2(0.5f, 0.5f);
            titleContainer.pivot = new Vector2(0.5f, 0.5f);
            titleContainer.anchoredPosition = new Vector2(0f, _logoRect != null ? 195f : 170f);

            // Short tagline under the title, setting up the game's premise (an alchemist tending the
            // last blooming garden at the edge of a dying world) before the player ever sees a board.
            var subtitle = UIFactory.CreateText(_root, "Subtitle", "The last garden still blooms, at the edge of ruin.", 18, UITheme.TextMutedOnBackground);
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 58f);
            subtitleRect.sizeDelta = new Vector2(480f, 30f);

            // Play/Settings tinted with two of the same piece colors the title tiles themselves cycle
            // through (see TitleTileView.Colors) instead of the plain light-blue every other "Choose"
            // button in the game uses — on a screen with nothing else going on but the title, that generic
            // tint read as flat next to it. Exit keeps the usual danger-red pill: it's not a game color,
            // it's the one button whose job is to read as "leaving", so it stays visually distinct from
            // Play/Settings rather than joining their palette. A thick comic outline (see
            // UIFactory.AddThickOutline, used everywhere else for cards/badges but deliberately skipped by
            // buttons in general — FinishButton's own doc comment) gives all three a bit more presence too.
            var playButton = UIFactory.CreateButton(_root, "Play", "Play", UISprites.Garden("Gold") ?? UISprites.ChooseButtonBackground, 33);
            var playRect = playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.5f);
            playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = new Vector2(0f, -34f);
            playRect.sizeDelta = new Vector2(330f, 84f);
            var playImage = playButton.GetComponent<Image>();
            playImage.color = UISprites.Garden("Gold") != null ? Color.white : VisualDefaults.GetColor(PieceColor.Lime);
            UIFactory.AddThickOutline(playImage, UITheme.Border);
            playButton.onClick.AddListener(OnPlayClicked);
            playButton.gameObject.AddComponent<MainMenuButtonHover>();
            DisableFeedbackScale(playButton);

            var collectionButton = UIFactory.CreateButton(_root, "Collection", "Collection", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 33);
            var collectionRect = collectionButton.GetComponent<RectTransform>();
            collectionRect.anchorMin = new Vector2(0.5f, 0.5f);
            collectionRect.anchorMax = new Vector2(0.5f, 0.5f);
            collectionRect.pivot = new Vector2(0.5f, 0.5f);
            collectionRect.anchoredPosition = new Vector2(0f, -134f);
            collectionRect.sizeDelta = new Vector2(330f, 84f);
            var collectionImage = collectionButton.GetComponent<Image>();
            collectionImage.color = UISprites.Garden("Green") != null ? Color.white : VisualDefaults.GetColor(PieceColor.Violet);
            UIFactory.AddThickOutline(collectionImage, UITheme.Border);
            collectionButton.onClick.AddListener(() => { if (CollectionClicked != null) { CollectionClicked(); } });
            collectionButton.gameObject.AddComponent<MainMenuButtonHover>();
            DisableFeedbackScale(collectionButton);

            var settingsButton = UIFactory.CreateButton(_root, "Settings", "Settings", UISprites.Garden("Blue") ?? UISprites.ChooseButtonBackground, 33);
            var settingsRect = settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.pivot = new Vector2(0.5f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(0f, -234f);
            settingsRect.sizeDelta = new Vector2(330f, 84f);
            var settingsImage = settingsButton.GetComponent<Image>();
            settingsImage.color = UISprites.Garden("Blue") != null ? Color.white : VisualDefaults.GetColor(PieceColor.Teal);
            UIFactory.AddThickOutline(settingsImage, UITheme.Border);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            settingsButton.gameObject.AddComponent<MainMenuButtonHover>();
            DisableFeedbackScale(settingsButton);

            var exitButton = UIFactory.CreateButton(_root, "Exit", "Exit", UISprites.CancelButtonBackground, 33);
            var exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.5f, 0.5f);
            exitRect.anchorMax = new Vector2(0.5f, 0.5f);
            exitRect.pivot = new Vector2(0.5f, 0.5f);
            exitRect.anchoredPosition = new Vector2(0f, -334f);
            exitRect.sizeDelta = new Vector2(330f, 84f);
            UIFactory.AddThickOutline(exitButton.GetComponent<Image>(), UITheme.Border);
            exitButton.onClick.AddListener(OnExitClicked);
#if UNITY_WEBGL && !UNITY_EDITOR
            // A web page has no window of its own to quit: the button would do nothing.
            exitButton.gameObject.SetActive(false);
#endif
            exitButton.gameObject.AddComponent<MainMenuButtonHover>();
            DisableFeedbackScale(exitButton);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private RectTransform _logoRect;
        private Image _logoImage;

        // The vector LASTBLOOM logo (Resources/Title/LastBloom.png, 1080x300). Shown at this width instead of the dot-matrix tile title when the art exists.
        private const float LogoWidth = 820f;
        private const float LogoAspect = 300f / 1080f;
        private const float LogoEntranceDuration = 0.55f;

        /// <summary>Builds the logo image, or returns null (so the caller falls back to the tile title) if its art is missing.</summary>
        private RectTransform BuildLogo(Transform parent)
        {
            var sprite = Resources.Load<Sprite>("Title/LastBloom");
            if (sprite == null)
            {
                return null;
            }
            var logo = UIFactory.CreatePanel(parent, "Logo", Color.white);
            logo.sprite = sprite;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            logo.rectTransform.sizeDelta = new Vector2(LogoWidth, LogoWidth * LogoAspect);
            _logoImage = logo;
            _logoRect = logo.rectTransform;
            return _logoRect;
        }

        /// <summary>Pop-in like the old tile title's cascade: fades in while scaling up past 1 and settling.</summary>
        private System.Collections.IEnumerator PlayLogoEntrance()
        {
            float t = 0f;
            _logoRect.localScale = Vector3.one * 0.6f;
            _logoImage.color = new Color(1f, 1f, 1f, 0f);
            while (t < LogoEntranceDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / LogoEntranceDuration);
                float scale = p < 0.65f ? Mathf.Lerp(0.6f, 1.08f, p / 0.65f) : Mathf.Lerp(1.08f, 1f, (p - 0.65f) / 0.35f);
                _logoRect.localScale = new Vector3(scale, scale, 1f);
                _logoImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p / 0.4f));
                yield return null;
            }
            _logoRect.localScale = Vector3.one;
            _logoImage.color = Color.white;
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
                                _titleTiles.Add((tileView, globalCol * TitleCascadeDelayPerColumn));
                            }
                        }
                    }
                }
                letterStartCol += glyphCols + letterGapCols;
            }

            return container;
        }

        /// <summary>The menu buttons already scale on hover (MainMenuButtonHover); keep ButtonFeedback's glow, punch and deny but not its own hover scale.</summary>
        private static void DisableFeedbackScale(Button button)
        {
            var feedback = button.GetComponent<ButtonFeedback>();
            if (feedback != null)
            {
                feedback.ScaleOnHover = false;
            }
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
            if (_logoRect != null)
            {
                StartCoroutine(PlayLogoEntrance());
            }
            for (int i = 0; i < _titleTiles.Count; i++)
            {
                _titleTiles[i].View.PlayEntrance(_titleTiles[i].Delay);
            }
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
