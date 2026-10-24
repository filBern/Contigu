using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to a grid cell's poison badge (see GridCellView.ApplyState/GridView.CreateCell) — shows the
    /// shared <see cref="TooltipView"/> explaining <see cref="Contigu.Core.Cell.IsPoisoned"/> on hover.
    /// Forwards clicks to the cell itself (<see cref="_clickForwardTarget"/>) so clicking this small corner
    /// badge never swallows the click that would otherwise place a piece here — same pattern as <see cref="TraitBadgeView"/>.
    /// </summary>
    public sealed class PoisonBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string TooltipName = "Blight";
        private const string TooltipDescription = "Points scored on a blighted plot are negative, and the blight spreads to 1 adjacent plot when it happens. It is removed on the next Shuffle, or when the enemy that spread it is defeated.";

        private TooltipView _tooltip;
        private GameObject _clickForwardTarget;

        public void Init(TooltipView tooltip, GameObject clickForwardTarget)
        {
            _tooltip = tooltip;
            _clickForwardTarget = clickForwardTarget;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Show(TooltipName, TooltipDescription, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Hide();
        }

        /// <summary>GridCellView never destroys this badge, just SetActive(false)s it, which doesn't fire OnPointerExit — same fix as TraitBadgeView.OnDisable.</summary>
        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_clickForwardTarget != null)
            {
                ExecuteEvents.Execute(_clickForwardTarget, eventData, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
