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

        private RectTransform _root;
        private RectTransform _panel;
        private Text _nameLabel;
        private Text _descLabel;

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("Tooltip", parent);
            UIFactory.StretchFull(_root);

            var panelImg = UIFactory.CreatePanel(_root, "Panel", UITheme.Panel);
            panelImg.raycastTarget = false;
            _panel = panelImg.rectTransform;
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
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

            _descLabel = UIFactory.CreateText(_panel, "Desc", "", 12, UITheme.TextPrimary, TextAnchor.UpperLeft);
            _descLabel.raycastTarget = false;
            _descLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _descLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _descLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _descLabel.rectTransform.anchoredPosition = new Vector2(Padding, -Padding - 26f);
            _descLabel.rectTransform.sizeDelta = new Vector2(-Padding * 2f, Height - Padding * 2f - 26f);
            AddOutline(_descLabel);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(string name, string description, RectTransform anchor)
        {
            _nameLabel.text = name;
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
