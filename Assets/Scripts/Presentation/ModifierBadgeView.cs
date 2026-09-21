using Contigu.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>Attached to one modifier badge — shows the shared <see cref="TooltipView"/> with that modifier's full name/description on hover, since the badge itself only carries a 2-letter abbreviation.</summary>
    public sealed class ModifierBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _tooltip;
        private ModifierDefinition _def;
        private System.Func<ModifierId, int> _usageCountProvider;

        public void Init(TooltipView tooltip, ModifierDefinition def, System.Func<ModifierId, int> usageCountProvider = null)
        {
            _tooltip = tooltip;
            _def = def;
            _usageCountProvider = usageCountProvider;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Queried fresh on every hover rather than passed in at Init —
            // the count keeps changing (every placement that scores) for as
            // long as this same badge instance stays on screen.
            string subtitle = _usageCountProvider != null
                ? "Used " + _usageCountProvider(_def.Id) + "x this run"
                : null;
            _tooltip.Show(_def.Name, DescriptionTextFormatter.Colorize(_def.Description), (RectTransform)transform, subtitle);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip.Hide();
        }
    }
}
