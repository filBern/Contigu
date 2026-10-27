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

        private string _footer;
        private bool _pinned;

        /// <summary>Touch: keeps the tooltip up after the finger lifts (a release normally counts as the pointer leaving), with a hint line at its foot. <see cref="Unpin"/> hides it.</summary>
        public void Pin(string footer)
        {
            _pinned = true;
            _footer = footer;
            ShowNow();
        }

        public void Unpin()
        {
            if (!_pinned)
            {
                return;
            }
            _pinned = false;
            _footer = null;
            HideNow();
        }

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
            ShowNow();
        }

        /// <summary>Shows the tooltip without a pointer: keyboard/controller focus or a touch long-press.</summary>
        public void ShowNow()
        {
            if (_tooltip != null)
            {
                _tooltip.Show(_name, _description, (RectTransform)transform, _subtitle, _subtitleColor, footer: _footer, placeAbove: true);
            }
        }

        public void HideNow()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pinned)
            {
                return;
            }
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        /// <summary>A card rebuilt (or purchased and removed) while hovered never gets an exit event.</summary>
        private void OnDisable()
        {
            _pinned = false;
            _footer = null;
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}
