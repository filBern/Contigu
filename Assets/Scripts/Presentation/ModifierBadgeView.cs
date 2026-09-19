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

        public void Init(TooltipView tooltip, ModifierDefinition def)
        {
            _tooltip = tooltip;
            _def = def;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tooltip.Show(_def.Name, _def.Description, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip.Hide();
        }
    }
}
