using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to one modifier badge in <see cref="ModifierPanelView"/> —
    /// forwards uGUI click/drag/hover events to the owning panel, which
    /// resolves click/drag into a reorder: EITHER a drag-and-drop move OR a
    /// tap-tap swap (on explicit request: "qu'on puisse les réorganiser
    /// avec un drag and drop OU avec un tap (tap 2 modifiers pour les inter
    /// changer de position)"). Same coexistence as HandSlotDragHandler:
    /// Unity's EventSystem only promotes a pointer-down-then-move past its
    /// drag threshold to OnBeginDrag, so a quick tap still resolves as a
    /// plain OnPointerClick. Hover tracking (see OnPointerEnter/Exit) feeds
    /// the "sell the modifier under the cursor" shortcut (explicit request:
    /// "Le joueur devrait pouvoir sell modifier lorsqu'il hover dessus" —
    /// see GameBootstrap's key binding).
    /// </summary>
    public sealed class ModifierBadgeDragHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ModifierPanelView _owner;
        private int _index;

        public void Init(ModifierPanelView owner, int index)
        {
            _owner = owner;
            _index = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner.OnBadgeClicked(_index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _owner.OnBadgeHoverEnter(_index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _owner.OnBadgeHoverExit(_index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _owner.OnBadgeBeginDrag(_index);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner.OnBadgeEndDrag();
        }

        /// <summary>Fired by Unity's EventSystem when a drag (see OnBeginDrag above) is released over this badge — resolves as a move to this badge's own position.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            _owner.OnBadgeDrop(_index);
        }
    }
}
