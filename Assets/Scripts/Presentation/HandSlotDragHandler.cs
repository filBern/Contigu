using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to one hand slot (see HandView) — forwards uGUI drag events to
    /// the owning HandView, which handles selecting the slot, showing a ghost
    /// preview that follows the pointer, and (via GridCellView.OnDrop) placing
    /// the piece when the pointer is released over a valid grid cell. Coexists
    /// with the slot's own Button/click handling: Unity's EventSystem only
    /// fires OnBeginDrag once the pointer moves past its drag threshold, so a
    /// quick tap still resolves as a plain click (the older toggle-select flow
    /// some players may still reach for) rather than a drag.
    /// </summary>
    public sealed class HandSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private HandView _owner;
        private int _index;

        public void Init(HandView owner, int index)
        {
            _owner = owner;
            _index = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _owner.BeginSlotDrag(_index);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _owner.DragSlot(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner.EndSlotDrag(eventData);
        }
    }
}
