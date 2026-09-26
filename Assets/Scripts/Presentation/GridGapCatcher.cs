using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached directly to the grid container itself (see GridView.Build), as
    /// an invisible full-grid-size raycast target sitting BEHIND every
    /// individual GridCellView in the hierarchy — a child's own Image always
    /// wins a raycast over its ancestor's when the pointer is precisely over
    /// that child, so this only ever receives a pointer event that missed
    /// every cell: the small spacing gap between adjacent tiles, previously a
    /// dead zone where a click or drag-drop did nothing at all (on explicit
    /// report: "je suis a un ou deux pixel de la case, je ne peux pas déposer
    /// de pièce... rajoute une règle pour lorsque le curseur n'est pas sur une
    /// case"). Forwards to GridView, which resolves the gap point to its
    /// nearest cell instead (see OnGapPointerEnter/OnGapPointerClick).
    /// </summary>
    public sealed class GridGapCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler, IDropHandler
    {
        private GridView _owner;
        private RectTransform _rect;

        public void Init(GridView owner)
        {
            _owner = owner;
            _rect = (RectTransform)transform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _owner.OnGapPointerEnter(LocalPoint(eventData));
        }

        /// <summary>Keeps the hover preview snapped to the nearest cell while the pointer slides around WITHIN the gap network itself (Enter/Exit alone only fire on the transition into/out of it as a whole).</summary>
        public void OnPointerMove(PointerEventData eventData)
        {
            _owner.OnGapPointerEnter(LocalPoint(eventData));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _owner.OnCellHoverExit(0, 0);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner.OnGapPointerClick(LocalPoint(eventData));
        }

        /// <summary>Fired by Unity's EventSystem when a drag (see HandSlotDragHandler) is released over the gap — same fallback as a plain click.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            _owner.OnGapPointerClick(LocalPoint(eventData));
        }

        private Vector2 LocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, null, out var localPoint);
            return localPoint;
        }
    }
}
