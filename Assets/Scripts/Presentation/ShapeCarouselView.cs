using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Slot-machine-style overlay for the "Piece Mastery" upgrade: a
    /// grayed-out reel of all 8 shapes spins under a fixed highlight frame
    /// and decelerates onto the shape RunManager already leveled up
    /// (RunManager.GrantShapeMastery), then a reveal card shows its new
    /// level and the resulting per-placement bonus. Same spin/reel
    /// mechanics as ModifierCarouselView but driven by ShapeId instead of
    /// ModifierId, with a simpler fixed-size reveal card.
    /// </summary>
    public sealed class ShapeCarouselView : MonoBehaviour
    {
        private const float ReelWidth = 340f;
        private const float ReelHeight = 110f;
        private const float BadgeSize = 80f;
        private const float BadgeSpacing = 110f;
        private const int ReelLength = 22;
        private const int WinningIndex = ReelLength - 6;
        private const float SpinDuration = 2.4f;

        private const float TitleHeight = 40f;
        private const float CardWidth = 260f;
        private const float CardGlyphBoxSize = 90f;
        private const float CardHeight = 230f;
        private const float OkHeight = 44f;
        private const float BlockSpacing = 20f;
        private const float CanvasHeight = 800f;

        private static readonly Color MonoShapeColor = new Color(0.6f, 0.6f, 0.6f);

        public event System.Action Dismissed;

        private RectTransform _root;
        private Text _titleText;
        private RectTransform _titleRect;
        private RectTransform _viewportRect;
        private RectTransform _reelRect;
        private RectTransform _cardContainer;
        private RectTransform _okRect;
        private Button _okButton;
        private Coroutine _spinCoroutine;
        private readonly ShapeId[] _reelShapeIds = new ShapeId[ReelLength];

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "ShapeCarouselOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "Piece Mastery", 22, UITheme.TextOnBackground);
            _titleText = title;
            _titleRect = title.rectTransform;
            _titleRect.anchorMin = new Vector2(0.5f, 1f);
            _titleRect.anchorMax = new Vector2(0.5f, 1f);
            _titleRect.pivot = new Vector2(0.5f, 1f);
            _titleRect.sizeDelta = new Vector2(600f, TitleHeight);

            var viewport = UIFactory.CreatePanel(_root, "Viewport", UITheme.Panel);
            UIFactory.AddThickOutline(viewport, UITheme.Border);
            _viewportRect = viewport.rectTransform;
            _viewportRect.anchorMin = new Vector2(0.5f, 1f);
            _viewportRect.anchorMax = new Vector2(0.5f, 1f);
            _viewportRect.pivot = new Vector2(0.5f, 1f);
            _viewportRect.sizeDelta = new Vector2(ReelWidth, ReelHeight);
            viewport.gameObject.AddComponent<RectMask2D>();

            _reelRect = UIFactory.CreateUIObject("Reel", viewport.transform);
            _reelRect.anchorMin = new Vector2(0f, 0.5f);
            _reelRect.anchorMax = new Vector2(0f, 0.5f);
            _reelRect.pivot = new Vector2(0f, 0.5f);

            BuildHighlightFrame(viewport.transform);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);
            _cardContainer.sizeDelta = new Vector2(CardWidth, CardHeight);

            var okButton = UIFactory.CreateButton(_root, "Ok", "OK", UISprites.ChooseButtonBackground, 18);
            _okButton = okButton;
            _okRect = okButton.GetComponent<RectTransform>();
            _okRect.anchorMin = new Vector2(0.5f, 1f);
            _okRect.anchorMax = new Vector2(0.5f, 1f);
            _okRect.pivot = new Vector2(0.5f, 1f);
            _okRect.sizeDelta = new Vector2(160f, OkHeight);
            okButton.onClick.AddListener(OnOkClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Same 4-solid-bars technique as ModifierCarouselView.BuildHighlightFrame — see its own comment for why this replaces an Outline-on-Color.clear approach.</summary>
        private static void BuildHighlightFrame(Transform parent)
        {
            const float FrameSize = BadgeSize + 14f;
            const float BarThickness = 4f;

            var top = UIFactory.CreatePanel(parent, "HighlightTop", VisualDefaults.GoldenColor);
            top.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            top.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            top.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            top.rectTransform.sizeDelta = new Vector2(FrameSize, BarThickness);
            top.rectTransform.anchoredPosition = new Vector2(0f, FrameSize / 2f);

            var bottom = UIFactory.CreatePanel(parent, "HighlightBottom", VisualDefaults.GoldenColor);
            bottom.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bottom.rectTransform.sizeDelta = new Vector2(FrameSize, BarThickness);
            bottom.rectTransform.anchoredPosition = new Vector2(0f, -FrameSize / 2f);

            var left = UIFactory.CreatePanel(parent, "HighlightLeft", VisualDefaults.GoldenColor);
            left.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            left.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            left.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            left.rectTransform.sizeDelta = new Vector2(BarThickness, FrameSize);
            left.rectTransform.anchoredPosition = new Vector2(-FrameSize / 2f, 0f);

            var right = UIFactory.CreatePanel(parent, "HighlightRight", VisualDefaults.GoldenColor);
            right.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            right.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            right.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(BarThickness, FrameSize);
            right.rectTransform.anchoredPosition = new Vector2(FrameSize / 2f, 0f);
        }

        /// <summary><paramref name="granted"/> is RunManager.LastShapeMasteryGranted — already leveled up by the time this shows; the spin is purely presentational suspense. <paramref name="newLevel"/> is that shape's level AFTER the purchase (RunManager.GetShapeMasteryLevel), so the card can show the real resulting bonus.</summary>
        public void Show(ShapeId granted, int newLevel)
        {
            _titleText.text = "Piece Mastery";

            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }

            for (int i = _reelRect.childCount - 1; i >= 0; i--)
            {
                Destroy(_reelRect.GetChild(i).gameObject);
            }
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }

            _cardContainer.gameObject.SetActive(true);
            _okButton.gameObject.SetActive(false);
            _reelRect.anchoredPosition = Vector2.zero;

            // Purely decorative filler badges; a fresh System.Random is fine here since nothing Core resolves.
            var filler = new System.Random();
            var allShapes = (ShapeId[])System.Enum.GetValues(typeof(ShapeId));
            for (int i = 0; i < ReelLength; i++)
            {
                var id = i == WinningIndex ? granted : allShapes[filler.Next(allShapes.Length)];
                _reelShapeIds[i] = id;
                BuildReelBadge(id, i);
            }

            LayoutFixedPart();

            _root.gameObject.SetActive(true);
            _spinCoroutine = StartCoroutine(SpinRoutine(granted, newLevel));
        }

        private void BuildReelBadge(ShapeId id, int index)
        {
            var badge = UIFactory.CreatePanel(_reelRect, "Badge" + index, UITheme.Panel);
            UIFactory.AddThickOutline(badge, UITheme.Border, 2f);
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            badge.rectTransform.anchoredPosition = new Vector2(index * BadgeSpacing, 0f);

            var glyphBox = UIFactory.CreateUIObject("Glyph", badge.transform);
            glyphBox.anchorMin = new Vector2(0.5f, 0.5f);
            glyphBox.anchorMax = new Vector2(0.5f, 0.5f);
            glyphBox.pivot = new Vector2(0.5f, 0.5f);
            glyphBox.sizeDelta = new Vector2(BadgeSize - 16f, BadgeSize - 16f);
            ShapePreviewFactory.BuildMono(glyphBox, PieceShapeCatalog.Get(id), MonoShapeColor);
        }

        private IEnumerator SpinRoutine(ShapeId granted, int newLevel)
        {
            float endX = ReelWidth / 2f - WinningIndex * BadgeSpacing;
            float t = 0f;
            int lastShownIndex = -1;
            while (t < SpinDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SpinDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                float x = Mathf.Lerp(0f, endX, eased);
                _reelRect.anchoredPosition = new Vector2(x, 0f);

                int centeredIndex = Mathf.Clamp(Mathf.RoundToInt((ReelWidth / 2f - x) / BadgeSpacing), 0, ReelLength - 1);
                if (centeredIndex != lastShownIndex)
                {
                    lastShownIndex = centeredIndex;
                    UpdateCard(_reelShapeIds[centeredIndex], centeredIndex == WinningIndex ? newLevel : 1);
                    SfxManager.Play(SfxId.CarouselTick);
                }
                yield return null;
            }
            _reelRect.anchoredPosition = new Vector2(endX, 0f);

            UpdateCard(granted, newLevel);
            _okButton.gameObject.SetActive(true);
            _spinCoroutine = null;
        }

        /// <summary>(Re)builds the reveal card for whichever shape is currently centered under the highlight; filler badges show level 1 with no bonus line.</summary>
        private void UpdateCard(ShapeId id, int level)
        {
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }

            var cardImage = UIFactory.CreateSlicedImage(_cardContainer, "ShapeCard", UISprites.CardBackground);
            cardImage.color = UITheme.Panel;
            UIFactory.AddThickOutline(cardImage, UITheme.Border);
            UIFactory.StretchFull(cardImage.rectTransform);

            var glyphBox = UIFactory.CreateUIObject("Glyph", cardImage.transform);
            glyphBox.anchorMin = new Vector2(0.5f, 1f);
            glyphBox.anchorMax = new Vector2(0.5f, 1f);
            glyphBox.pivot = new Vector2(0.5f, 1f);
            glyphBox.sizeDelta = new Vector2(CardGlyphBoxSize, CardGlyphBoxSize);
            glyphBox.anchoredPosition = new Vector2(0f, -16f);
            ShapePreviewFactory.BuildMono(glyphBox, PieceShapeCatalog.Get(id), MonoShapeColor);

            // No FontStyle.Bold: the Digitalt font has no true bold face, so Unity's legacy Text synthesizes one by double-drawing a shifted copy, which reads as blurry. Size alone carries the emphasis.
            var name = UIFactory.CreateText(cardImage.transform, "Name", VisualDefaults.GetShapeName(id), 18, UITheme.TextPrimary);
            name.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            name.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            name.rectTransform.pivot = new Vector2(0.5f, 1f);
            name.rectTransform.sizeDelta = new Vector2(CardWidth - 32f, 26f);
            name.rectTransform.anchoredPosition = new Vector2(0f, -(16f + CardGlyphBoxSize + 8f));

            string levelLine = level > 1 ? "Level " + level : "Level 1";
            // UITheme.TextPrimary, not VisualDefaults.GoldenColor: gold text on this light Panel background is nearly invisible.
            var levelText = UIFactory.CreateText(cardImage.transform, "Level", levelLine, 16, UITheme.TextPrimary);
            levelText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            levelText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            levelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            levelText.rectTransform.sizeDelta = new Vector2(CardWidth - 32f, 22f);
            levelText.rectTransform.anchoredPosition = new Vector2(0f, -(16f + CardGlyphBoxSize + 8f + 26f));

            string descText = level > 1
                ? "+" + (level - 1) + " pts every time you place a " + VisualDefaults.GetShapeName(id) + "."
                : "Not leveled up yet.";
            var desc = UIFactory.CreateText(cardImage.transform, "Desc", descText, 13, UITheme.TextPrimary);
            desc.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            desc.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            desc.rectTransform.pivot = new Vector2(0.5f, 1f);
            desc.rectTransform.sizeDelta = new Vector2(CardWidth - 32f, 40f);
            desc.rectTransform.anchoredPosition = new Vector2(0f, -(16f + CardGlyphBoxSize + 8f + 26f + 22f + 6f));
        }

        private void LayoutFixedPart()
        {
            float totalHeight = TitleHeight + BlockSpacing + ReelHeight + BlockSpacing + CardHeight + BlockSpacing + OkHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _titleRect.anchoredPosition = new Vector2(0f, topY);
            float y = topY - TitleHeight - BlockSpacing;

            _viewportRect.anchoredPosition = new Vector2(0f, y);
            y -= ReelHeight + BlockSpacing;

            _cardContainer.anchoredPosition = new Vector2(0f, y);
            y -= CardHeight + BlockSpacing;

            _okRect.anchoredPosition = new Vector2(0f, y);
        }

        private void OnOkClicked()
        {
            _root.gameObject.SetActive(false);
            if (Dismissed != null)
            {
                Dismissed();
            }
        }
    }
}
