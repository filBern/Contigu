using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Single shared floating tooltip built once by <see cref="GameBootstrap"/>
    /// and reused by every hoverable modifier badge (draft cards, side panel
    /// rows) — badges only carry a 2-letter abbreviation, so this is where a
    /// modifier's full name and description actually show, on hover.
    /// </summary>
    public sealed class TooltipView : MonoBehaviour
    {
        private const float Width = 320f;
        // Dark title band across the top, with a thin accent line (the subtitle's color) under it.
        private const float HeaderHeight = 34f;
        private const float AccentHeight = 3f;
        private const float ChipHeight = 22f;
        private const float ChipGap = 8f;
        private const float BodyTop = 10f;
        // Only used as a placeholder before the first real Show() call sets
        // an actual (dynamic) height — see Show().
        private const float InitialHeight = 150f;
        private const float Padding = 10f;
        private const float ShowMargin = 16f;
        // Reserved on the right of the Name row whenever a sell value is
        // shown (see Show()'s sellValue parameter), so a long modifier name
        // never runs under it.
        private const float SellValueWidth = 56f;

        private RectTransform _root;
        private RectTransform _panel;
        private Text _nameLabel;
        private Text _subtitleLabel;
        private Image _headerBand;
        private Image _accentLine;
        private Image _subtitleChip;
        private Coroutine _popRoutine;
        private Text _descLabel;
        private Text _sellValueLabel;
        // Optional third section (enemy story line): a thin divider plus an italic muted sentence under the description.
        private Image _loreDivider;
        private Text _loreLabel;
        private const float LoreGap = 8f;
        // Optional last section (enemy HP): a divider plus a right-aligned line at the bottom-right of the panel.
        private Image _footerDivider;
        private Text _footerLabel;
        private const float FooterHeight = 20f;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("Tooltip", parent);
            UIFactory.StretchFull(_root);

            var panelImg = UIFactory.CreateSlicedImage(_root, "Panel", UISprites.CardBackground);
            panelImg.color = UITheme.Panel;
            panelImg.raycastTarget = false;
            var shadow = panelImg.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(3f, -5f);
            _panel = panelImg.rectTransform;
            // Anchored to _root's CENTER (matching the center-origin local
            // space that ScreenPointToLocalPointInRectangle returns points in
            // — _root itself has pivot (0.5, 0.5) via StretchFull) so the
            // anchoredPosition computed in PositionNear can be used directly
            // as an offset from center, without also needing to correct for
            // a top-left anchor. Pivot stays top-left so the panel grows
            // right/down from that anchored point, like a normal tooltip.
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = new Vector2(Width, InitialHeight);
            UIFactory.AddThickOutline(panelImg, UITheme.Border);

            // Title band: dark strip across the top (clipped by nothing; inset so it hugs the card's rounded frame), accent line below.
            _headerBand = UIFactory.CreatePanel(_panel, "HeaderBand", UITheme.Background);
            _headerBand.raycastTarget = false;
            _headerBand.rectTransform.anchorMin = new Vector2(0f, 1f);
            _headerBand.rectTransform.anchorMax = new Vector2(1f, 1f);
            _headerBand.rectTransform.pivot = new Vector2(0.5f, 1f);
            _headerBand.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            _headerBand.rectTransform.sizeDelta = new Vector2(-8f, HeaderHeight);

            _accentLine = UIFactory.CreatePanel(_panel, "AccentLine", VisualDefaults.GoldenColor);
            _accentLine.raycastTarget = false;
            _accentLine.rectTransform.anchorMin = new Vector2(0f, 1f);
            _accentLine.rectTransform.anchorMax = new Vector2(1f, 1f);
            _accentLine.rectTransform.pivot = new Vector2(0.5f, 1f);
            _accentLine.rectTransform.anchoredPosition = new Vector2(0f, -4f - HeaderHeight);
            _accentLine.rectTransform.sizeDelta = new Vector2(-8f, AccentHeight);

            _nameLabel = UIFactory.CreateText(_panel, "Name", "", 17, UITheme.TextOnBackground, TextAnchor.MiddleLeft);
            _nameLabel.fontStyle = FontStyle.Bold;
            _nameLabel.raycastTarget = false;
            _nameLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _nameLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _nameLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _nameLabel.rectTransform.anchoredPosition = new Vector2(Padding + 4f, -4f);
            _nameLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f - 8f, HeaderHeight);

            // Sell value: top-right corner of the panel, same row as the
            // Name. Only the persistent modifier side panel passes a value
            // into Show(); every other caller leaves it null and this stays
            // hidden, same opt-in pattern as the subtitle line.
            _sellValueLabel = UIFactory.CreateText(_panel, "SellValue", "", 13, VisualDefaults.GoldenColor, TextAnchor.UpperRight);
            _sellValueLabel.raycastTarget = false;
            _sellValueLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
            _sellValueLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _sellValueLabel.rectTransform.pivot = new Vector2(1f, 1f);
            _sellValueLabel.rectTransform.anchoredPosition = new Vector2(-Padding - 4f, -4f - (HeaderHeight - 22f) * 0.5f);
            _sellValueLabel.rectTransform.sizeDelta = new Vector2(SellValueWidth, 22f);
            _sellValueLabel.gameObject.SetActive(false);

            // Subtitle (rarity / category): a small pill tinted with its own color, under the title band.
            _subtitleChip = UIFactory.CreatePanel(_panel, "SubtitleChip", new Color(0f, 0f, 0f, 0.1f));
            _subtitleChip.raycastTarget = false;
            _subtitleChip.rectTransform.anchorMin = new Vector2(0f, 1f);
            _subtitleChip.rectTransform.anchorMax = new Vector2(0f, 1f);
            _subtitleChip.rectTransform.pivot = new Vector2(0f, 1f);
            _subtitleChip.gameObject.SetActive(false);

            _subtitleLabel = UIFactory.CreateText(_subtitleChip.transform, "Subtitle", "", 12, UITheme.TextMuted, TextAnchor.MiddleCenter);
            _subtitleLabel.fontStyle = FontStyle.Bold;
            _subtitleLabel.raycastTarget = false;
            UIFactory.StretchFull(_subtitleLabel.rectTransform);

            _descLabel = UIFactory.CreateText(_panel, "Desc", "", 15, UITheme.TextPrimary, TextAnchor.UpperLeft);
            _descLabel.lineSpacing = 1.1f;
            _descLabel.raycastTarget = false;
            _descLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _descLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _descLabel.rectTransform.pivot = new Vector2(0f, 1f);
            // Width fixed here (never changes — only height varies per Show()
            // call) so it's already correct the very first time Show() reads
            // preferredHeight, before that call gets to set it itself.
            _descLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 0f);

            _loreDivider = UIFactory.CreatePanel(_panel, "LoreDivider", UITheme.Border);
            _loreDivider.raycastTarget = false;
            _loreDivider.rectTransform.anchorMin = new Vector2(0f, 1f);
            _loreDivider.rectTransform.anchorMax = new Vector2(1f, 1f);
            _loreDivider.rectTransform.pivot = new Vector2(0f, 1f);
            _loreDivider.gameObject.SetActive(false);

            _loreLabel = UIFactory.CreateText(_panel, "Lore", "", 13, UITheme.TextMuted, TextAnchor.UpperLeft);
            _loreLabel.fontStyle = FontStyle.Italic;
            _loreLabel.raycastTarget = false;
            _loreLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _loreLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _loreLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _loreLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 0f);
            _loreLabel.gameObject.SetActive(false);

            _footerDivider = UIFactory.CreatePanel(_panel, "FooterDivider", UITheme.Border);
            _footerDivider.raycastTarget = false;
            _footerDivider.rectTransform.anchorMin = new Vector2(0f, 1f);
            _footerDivider.rectTransform.anchorMax = new Vector2(1f, 1f);
            _footerDivider.rectTransform.pivot = new Vector2(0f, 1f);
            _footerDivider.gameObject.SetActive(false);

            _footerLabel = UIFactory.CreateText(_panel, "Footer", "", 14, UITheme.TextPrimary, TextAnchor.MiddleRight);
            _footerLabel.raycastTarget = false;
            _footerLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _footerLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _footerLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _footerLabel.gameObject.SetActive(false);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>
        /// Shows the tooltip anchored near <paramref name="anchor"/>. An
        /// optional <paramref name="subtitle"/> (e.g. "Rare · Tile Upgrade")
        /// renders as a small colored line between the name and description
        /// — omitted entirely (and the description shifted up to fill the
        /// gap) when null/empty, so existing callers that don't pass one
        /// (modifier badges) keep their original, more compact layout.
        /// </summary>
        public void Show(string name, string description, RectTransform anchor, string subtitle = null, Color? subtitleColor = null, int? sellValue = null, string lore = null, string footer = null, bool placeAbove = false)
        {
            _nameLabel.text = name;

            bool hasSellValue = sellValue.HasValue;
            _sellValueLabel.gameObject.SetActive(hasSellValue);
            if (hasSellValue)
            {
                _sellValueLabel.text = "Sell " + sellValue.Value;
            }
            _nameLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f - 8f - (hasSellValue ? SellValueWidth : 0f), HeaderHeight);

            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            _subtitleChip.gameObject.SetActive(hasSubtitle);
            Color accent = subtitleColor ?? VisualDefaults.GoldenColor;
            if (hasSubtitle && !subtitleColor.HasValue)
            {
                accent = VisualDefaults.GoldenColor;
            }
            _accentLine.color = new Color(accent.r, accent.g, accent.b, 1f);
            float contentTop = 4f + HeaderHeight + AccentHeight + BodyTop;
            if (hasSubtitle)
            {
                _subtitleLabel.text = subtitle;
                // Muted text colors are near-black translucent: on the chip the accent itself reads better when it is not the muted default.
                var chipText = subtitleColor.HasValue && subtitleColor.Value != UITheme.TextMuted ? subtitleColor.Value : UITheme.TextPrimary;
                _subtitleLabel.color = new Color(chipText.r * 0.75f, chipText.g * 0.75f, chipText.b * 0.75f, 1f);
                _subtitleChip.color = new Color(accent.r, accent.g, accent.b, subtitleColor.HasValue && subtitleColor.Value != UITheme.TextMuted ? 0.28f : 0.12f);
                float chipWidth = Mathf.Min(Width - Padding * 2f, _subtitleLabel.preferredWidth + 18f);
                _subtitleChip.rectTransform.sizeDelta = new Vector2(chipWidth, ChipHeight);
                _subtitleChip.rectTransform.anchoredPosition = new Vector2(Padding, -contentTop);
                contentTop += ChipHeight + ChipGap;
            }

            float usedHeight = contentTop - Padding;
            _descLabel.rectTransform.anchoredPosition = new Vector2(Padding, -contentTop);
            _descLabel.text = description;

            // Height fits the actual description length. Text.preferredHeight
            // already reflects wrapping at the label's current fixed width,
            // since UIFactory.CreateText sets horizontalOverflow = Wrap.
            float descHeight = _descLabel.preferredHeight;
            _descLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, descHeight);
            float loreBlockHeight = 0f;
            bool hasLore = !string.IsNullOrEmpty(lore);
            _loreDivider.gameObject.SetActive(hasLore);
            _loreLabel.gameObject.SetActive(hasLore);
            if (hasLore)
            {
                _loreLabel.text = lore;
                float loreHeight = _loreLabel.preferredHeight;
                float dividerY = -Padding - usedHeight - descHeight - LoreGap;
                _loreDivider.rectTransform.anchoredPosition = new Vector2(Padding, dividerY);
                _loreDivider.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 1f);
                _loreLabel.rectTransform.anchoredPosition = new Vector2(Padding, dividerY - LoreGap);
                _loreLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, loreHeight);
                loreBlockHeight = LoreGap * 2f + 1f + loreHeight;
            }
            float footerBlockHeight = 0f;
            bool hasFooter = !string.IsNullOrEmpty(footer);
            _footerDivider.gameObject.SetActive(hasFooter);
            _footerLabel.gameObject.SetActive(hasFooter);
            if (hasFooter)
            {
                _footerLabel.text = footer;
                float footerDividerY = -Padding - usedHeight - descHeight - loreBlockHeight - LoreGap;
                _footerDivider.rectTransform.anchoredPosition = new Vector2(Padding, footerDividerY);
                _footerDivider.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 1f);
                _footerLabel.rectTransform.anchoredPosition = new Vector2(Padding, footerDividerY - LoreGap + 2f);
                _footerLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, FooterHeight);
                footerBlockHeight = LoreGap * 2f + 1f + FooterHeight - 4f;
            }
            _panel.sizeDelta = new Vector2(Width, Padding * 2f + usedHeight + descHeight + loreBlockHeight + footerBlockHeight);
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
            }

            _root.gameObject.SetActive(true);
            // Always render above whatever else is on screen, including
            // overlays (draft/removal cards) built after this tooltip.
            _root.SetAsLastSibling();
            PositionNear(anchor, placeAbove);
            _popRoutine = StartCoroutine(PopIn());
        }

        /// <summary>A quick scale-up from slightly smaller so the tooltip settles into place instead of blinking on.</summary>
        private System.Collections.IEnumerator PopIn()
        {
            const float duration = 0.09f;
            float t = 0f;
            _panel.localScale = new Vector3(0.94f, 0.94f, 1f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(0.94f, 1f, Mathf.Clamp01(t / duration));
                _panel.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _panel.localScale = Vector3.one;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        private void PositionNear(RectTransform anchor, bool placeAbove = false)
        {
            // Screen Space - Overlay canvas, so camera is null for both calls.
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, null, out var localPoint);

            float panelHeight = _panel.rect.height;
            float halfW = _root.rect.width / 2f;
            float halfH = _root.rect.height / 2f;

            // Never sit on top of the icon being inspected: clear the
            // anchor's bounds entirely rather than nudging by a flat margin
            // from its center, and flip to its left side when there isn't
            // room to its right.
            float anchorHalfW = anchor.rect.width * 0.5f;
            float anchorHalfH = anchor.rect.height * 0.5f;

            if (placeAbove)
            {
                // Centered over the anchor, sitting on its top edge; below it instead when there is no room above.
                float centeredX = Mathf.Clamp(localPoint.x - Width * 0.5f, -halfW, halfW - Width);
                float aboveTop = localPoint.y + anchorHalfH + ShowMargin + panelHeight;
                float topY = aboveTop <= halfH ? aboveTop : localPoint.y - anchorHalfH - ShowMargin;
                topY = Mathf.Clamp(topY, -halfH + panelHeight, halfH);
                _panel.anchoredPosition = new Vector2(centeredX, topY);
                return;
            }

            float rightX = localPoint.x + anchorHalfW + ShowMargin;
            float leftX = localPoint.x - anchorHalfW - ShowMargin - Width;
            float x = rightX + Width <= halfW ? rightX : leftX;
            x = Mathf.Clamp(x, -halfW, halfW - Width);

            float y = Mathf.Clamp(localPoint.y + anchorHalfH + ShowMargin, -halfH + panelHeight, halfH);
            _panel.anchoredPosition = new Vector2(x, y);
        }
    }
}
