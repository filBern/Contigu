using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>Shows the shared <see cref="TooltipView"/> while the pointer is over this element (a shop card's body). Buttons inside the element keep their own clicks.</summary>
    public sealed class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _tooltip;
        private string _name;
        private string _description;
        private string _subtitle;
        private Color? _subtitleColor;

        public void Init(TooltipView tooltip, string name, string description, string subtitle = null, Color? subtitleColor = null)
        {
            _tooltip = tooltip;
            _name = name;
            _description = description;
            _subtitle = subtitle;
            _subtitleColor = subtitleColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip != null)
            {
                _tooltip.Show(_name, _description, (RectTransform)transform, _subtitle, _subtitleColor);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        /// <summary>A card rebuilt (or purchased and removed) while hovered never gets an exit event.</summary>
        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}
