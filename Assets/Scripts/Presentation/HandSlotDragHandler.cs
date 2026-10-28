using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to one hand slot (see HandView), forwarding uGUI drag events
    /// to the owning HandView, which handles selecting the slot, showing a
    /// ghost preview that follows the pointer, and (via GridCellView.OnDrop)
    /// placing the piece when the pointer is released over a valid grid
    /// cell. Coexists with the slot's own Button/click handling: Unity's
    /// EventSystem only fires OnBeginDrag once the pointer moves past its
    /// drag threshold, so a quick tap still resolves as a plain click.
    /// </summary>
    public sealed class HandSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private HandView _owner;
        private int _index;

        public void Init(HandView owner, int index)
        {
            _owner = owner;
            _index = index;
        }

        /// <summary>Tells the hand whether the press was a finger (a tap then only shows the piece's upgrades; pieces are placed by dragging).</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            _owner.NotePress(eventData.pointerId >= 0);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _owner.SetSlotHover(_index, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _owner.SetSlotHover(_index, false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _owner.BeginSlotDrag(_index);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _owner.DragSlot(_index, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner.EndSlotDrag(eventData);
        }
    }
}
