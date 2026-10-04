using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to a grid cell's poison badge (see GridCellView.ApplyState/
    /// GridView.CreateCell) — shows the shared <see cref="TooltipView"/>
    /// explaining <see cref="Contigu.Core.Cell.IsPoisoned"/> on hover
    /// (explicit request: "Il n'y a pas de tooltip pour la tile
    /// empoisonné, il en faut un"). Forwards clicks to the cell itself
    /// (<see cref="_clickForwardTarget"/>) so hovering/clicking this small
    /// corner badge never swallows the click that would otherwise place a
    /// piece here — same pattern as <see cref="TraitBadgeView"/>.
    /// </summary>
    public sealed class PoisonBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string TooltipName = "Poisoned";
        private const string TooltipDescription = "Any points this tile scores count negative instead of positive, until the Poisoner that cursed it is defeated.";

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

        /// <summary>Same "don't leave the tooltip stuck open" fix as TraitBadgeView.OnDisable — GridCellView never destroys this badge, it just SetActive(false)s it once the cell's no longer poisoned, which doesn't fire OnPointerExit.</summary>
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
