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
        private const float Width = 300f;
        private const float Height = 150f;
        private const float Padding = 10f;
        private const float ShowMargin = 16f;
        private const float NameHeight = 24f;
        private const float SubtitleHeight = 18f;

        private RectTransform _root;
        private RectTransform _panel;
        private Text _nameLabel;
        private Text _subtitleLabel;
        private Text _descLabel;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("Tooltip", parent);
            UIFactory.StretchFull(_root);

            var panelImg = UIFactory.CreatePanel(_root, "Panel", UITheme.Panel);
            panelImg.raycastTarget = false;
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
            _panel.sizeDelta = new Vector2(Width, Height);
            var panelOutline = panelImg.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            _nameLabel = UIFactory.CreateText(_panel, "Name", "", 15, UITheme.TextPrimary, TextAnchor.UpperLeft);
            _nameLabel.fontStyle = FontStyle.Bold;
            _nameLabel.raycastTarget = false;
            _nameLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _nameLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _nameLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _nameLabel.rectTransform.anchoredPosition = new Vector2(Padding, -Padding);
            _nameLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 22f);
            AddOutline(_nameLabel);

            _subtitleLabel = UIFactory.CreateText(_panel, "Subtitle", "", 12, UITheme.TextMuted, TextAnchor.UpperLeft);
            _subtitleLabel.fontStyle = FontStyle.BoldAndItalic;
            _subtitleLabel.raycastTarget = false;
            _subtitleLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _subtitleLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _subtitleLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _subtitleLabel.rectTransform.anchoredPosition = new Vector2(Padding, -Padding - NameHeight);
            _subtitleLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, SubtitleHeight);
            AddOutline(_subtitleLabel);
            _subtitleLabel.gameObject.SetActive(false);

            // No outline on the description (unlike Name/Subtitle above) —
            // on top of a solid opaque panel background it only muddied the
            // text at this size; bigger + bold reads more clearly instead.
            _descLabel = UIFactory.CreateText(_panel, "Desc", "", 14, UITheme.TextPrimary, TextAnchor.UpperLeft);
            _descLabel.fontStyle = FontStyle.Bold;
            _descLabel.raycastTarget = false;
            _descLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _descLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _descLabel.rectTransform.pivot = new Vector2(0f, 1f);

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
        public void Show(string name, string description, RectTransform anchor, string subtitle = null, Color? subtitleColor = null)
        {
            _nameLabel.text = name;

            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            _subtitleLabel.gameObject.SetActive(hasSubtitle);
            if (hasSubtitle)
            {
                _subtitleLabel.text = subtitle;
                _subtitleLabel.color = subtitleColor ?? UITheme.TextMuted;
            }

            float usedHeight = NameHeight + (hasSubtitle ? SubtitleHeight : 0f);
            _descLabel.rectTransform.anchoredPosition = new Vector2(Padding, -Padding - usedHeight);
            _descLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, Height - Padding * 2f - usedHeight);
            _descLabel.text = description;

            _root.gameObject.SetActive(true);
            // Always render above whatever else is on screen, including
            // overlays (draft/removal cards) built after this tooltip.
            _root.SetAsLastSibling();
            PositionNear(anchor);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        private void PositionNear(RectTransform anchor)
        {
            // Screen Space - Overlay canvas, so camera is null for both calls.
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, null, out var localPoint);

            float halfW = _root.rect.width / 2f;
            float halfH = _root.rect.height / 2f;

            float x = Mathf.Clamp(localPoint.x + ShowMargin, -halfW, halfW - Width);
            float y = Mathf.Clamp(localPoint.y + ShowMargin, -halfH + Height, halfH);
            _panel.anchoredPosition = new Vector2(x, y);
        }

        private static void AddOutline(Text label)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
