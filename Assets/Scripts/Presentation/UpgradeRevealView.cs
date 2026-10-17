using System;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals the Joker
    /// upgrade: the only Bank-pool upgrade with no sub-choice and no
    /// dedicated reveal of its own (Random Modifier uses
    /// ModifierCarouselView's spin instead; see GameBootstrap.OnUpgradeBuyClicked).
    /// Joker is already applied by the time this shows; it displays
    /// UpgradeCardFactory's card plus a preview of the actual piece it
    /// added.
    /// </summary>
    public sealed class UpgradeRevealView : MonoBehaviour
    {
        private const float TitleHeight = 40f;
        private const float PreviewSize = 96f;
        private const float OkHeight = 44f;
        private const float BlockSpacing = 24f;
        // Canvas is always this tall in local units regardless of window size (CanvasScaler matches on height; see GameBootstrap.BuildCanvas), so centering math here holds at any resolution.
        private const float CanvasHeight = 800f;

        /// <summary>Fires once the player dismisses the reveal.</summary>
        public event Action Dismissed;

        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _titleRect;
        private RectTransform _cardContainer;
        private RectTransform _previewContainer;
        private RectTransform _okRect;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "UpgradeRevealOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var title = UIFactory.CreateText(_root, "Title", "You got:", 22, UITheme.TextOnBackground);
            _titleRect = title.rectTransform;
            _titleRect.anchorMin = new Vector2(0.5f, 1f);
            _titleRect.anchorMax = new Vector2(0.5f, 1f);
            _titleRect.pivot = new Vector2(0.5f, 1f);
            _titleRect.sizeDelta = new Vector2(600f, TitleHeight);

            // Pivot (0.5, 1) must match UpgradeCardFactory.Build's returned container's own pivot, or the card ends up offset by half its default size.
            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);

            _previewContainer = UIFactory.CreateUIObject("Preview", _root);
            _previewContainer.anchorMin = new Vector2(0.5f, 1f);
            _previewContainer.anchorMax = new Vector2(0.5f, 1f);
            _previewContainer.pivot = new Vector2(0.5f, 1f);
            _previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);

            var okButton = UIFactory.CreateButton(_root, "Ok", "OK", UISprites.ChooseButtonBackground, 18);
            _okRect = okButton.GetComponent<RectTransform>();
            _okRect.anchorMin = new Vector2(0.5f, 1f);
            _okRect.anchorMax = new Vector2(0.5f, 1f);
            _okRect.pivot = new Vector2(0.5f, 1f);
            _okRect.sizeDelta = new Vector2(160f, OkHeight);
            okButton.onClick.AddListener(OnOkClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>
        /// <paramref name="pieceShape"/>/<paramref name="pieceColor"/> is the
        /// specific piece Joker added (see RunManager.LastJokerShapeAdded),
        /// previewed below the card. <paramref name="trait"/> is Joker's
        /// combat trait (see RunManager.LastJokerCombatKindAdded), passed
        /// through to ShapePreviewFactory, which badges every cell of the
        /// preview for a Joker-exclusive kind.
        /// </summary>
        public void Show(UpgradeDefinition def, ShapeId pieceShape, PieceColor pieceColor, PieceTrait? trait = null)
        {
            ShowInternal(def, () =>
            {
                _previewContainer.sizeDelta = new Vector2(PreviewSize, PreviewSize);
                ShapePreviewFactory.Build(_previewContainer, PieceShapeCatalog.Get(pieceShape), pieceColor, trait, _tooltip, null);
                return PreviewSize;
            });
        }

        /// <summary>
        /// Reveal for "Permanent Bonus Square" (no piece or fertilizer to show): the card plus a small map of the board with the
        /// freshly marked square enlarged, so the purchase visibly does something and the player knows where the x2 landed.
        /// </summary>
        public void ShowBoard(UpgradeDefinition def, Vector2Int marked)
        {
            ShowInternal(def, () =>
            {
                const float cell = 14f;
                const float gap = 1.5f;
                int n = GridManager.Size;
                float total = n * cell + (n - 1) * gap;
                _previewContainer.sizeDelta = new Vector2(total, total);
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        var img = UIFactory.CreatePanel(_previewContainer, "c_" + x + "_" + y, Color.white);
                        img.raycastTarget = false;
                        img.sprite = VisualDefaults.EmptyTileSprite;
                        img.color = img.sprite != null ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(0.25f, 0.2f, 0.15f, 1f);
                        var rt = img.rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                        rt.pivot = new Vector2(0f, 1f);
                        rt.sizeDelta = new Vector2(cell, cell);
                        rt.anchoredPosition = new Vector2(x * (cell + gap), -(n - 1 - y) * (cell + gap));
                    }
                }
                // The marked square, drawn bigger on top so it can't be missed.
                var marker = UIFactory.CreatePanel(_previewContainer, "Marked", Color.white);
                marker.raycastTarget = false;
                marker.sprite = VisualDefaults.BonusTileSprite != null ? VisualDefaults.BonusTileSprite : VisualDefaults.EmptyTileSprite;
                if (marker.sprite == null)
                {
                    marker.color = VisualDefaults.GoldenColor;
                }
                var mrt = marker.rectTransform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 1f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.sizeDelta = new Vector2(cell * 2.4f, cell * 2.4f);
                mrt.anchoredPosition = new Vector2(marked.x * (cell + gap) + cell * 0.5f, -(n - 1 - marked.y) * (cell + gap) - cell * 0.5f);
                return total;
            });
        }

        private void ShowInternal(UpgradeDefinition def, Func<float> buildPreview)
        {
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }
            var card = UpgradeCardFactory.Build(_cardContainer, def);

            for (int i = _previewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewContainer.GetChild(i).gameObject);
            }
            float previewHeight = buildPreview();

            // Card height varies with description length, so title/card/preview/OK are stacked and centered using the measured height rather than fixed offsets.
            float cardHeight = card.sizeDelta.y;
            float totalHeight = TitleHeight + BlockSpacing + cardHeight + BlockSpacing + previewHeight + BlockSpacing + OkHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _titleRect.anchoredPosition = new Vector2(0f, topY);
            float y = topY - TitleHeight - BlockSpacing;

            _cardContainer.anchoredPosition = new Vector2(0f, y);
            y -= cardHeight + BlockSpacing;

            _previewContainer.anchoredPosition = new Vector2(0f, y);
            y -= previewHeight + BlockSpacing;

            _okRect.anchoredPosition = new Vector2(0f, y);

            _root.gameObject.SetActive(true);
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
