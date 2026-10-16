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
        private System.Func<ModifierId, string> _progressiveStateProvider;
        private System.Func<ModifierId, string> _levelStateProvider;
        private bool _showSellValue;

        // Whether the pointer is currently resting over this badge — while
        // true, Update() below keeps re-pushing fresh content into the
        // tooltip every frame, since OnPointerEnter only fires once and the
        // underlying values (usage count, progressive state) keep changing.
        private bool _hovering;

        public void Init(TooltipView tooltip, ModifierDefinition def, System.Func<ModifierId, int> usageCountProvider = null, System.Func<ModifierId, string> progressiveStateProvider = null, System.Func<ModifierId, string> levelStateProvider = null, bool showSellValue = false)
        {
            _tooltip = tooltip;
            _def = def;
            _usageCountProvider = usageCountProvider;
            _progressiveStateProvider = progressiveStateProvider;
            _levelStateProvider = levelStateProvider;
            _showSellValue = showSellValue;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            _tooltip.Hide();
        }

        private void Update()
        {
            if (_hovering)
            {
                ShowTooltip();
            }
        }

        private void ShowTooltip()
        {
            // Queried fresh every time this runs rather than cached — both
            // keep changing (every placement that scores, or for the
            // progressive line every placement at all) for as long as this
            // same badge instance stays on screen.
            string subtitle = _usageCountProvider != null
                ? "Used " + _usageCountProvider(_def.Id) + "x this run"
                : null;
            string description = DescriptionTextFormatter.Colorize(_def.Description, 14);
            // Progressive/incremental modifiers get an extra line showing
            // their current effective state, appended to the description
            // rather than the fixed-height subtitle slot, since the
            // description label already auto-sizes around its text.
            string progressiveState = _progressiveStateProvider != null ? _progressiveStateProvider(_def.Id) : null;
            if (!string.IsNullOrEmpty(progressiveState))
            {
                description = description + "\n\n" + DescriptionTextFormatter.Colorize(progressiveState, 14);
            }
            else
            {
                // Every other (non-progressive) modifier's Description is
                // static text baked at base (level-1) numbers, so this
                // generic level line fills in the level-scaled value.
                // Skipped when progressiveState already fired above, since
                // that line already reports the true value.
                string levelState = _levelStateProvider != null ? _levelStateProvider(_def.Id) : null;
                if (!string.IsNullOrEmpty(levelState))
                {
                    description = description + "\n\n" + DescriptionTextFormatter.Colorize(levelState, 14);
                }
            }
            // Same formula as RunManager.SellModifier (base price minus 1),
            // shown only for badges the side panel builds (already owned
            // and sellable); draft/shop-candidate badges never pass
            // showSellValue.
            int? sellValue = _showSellValue ? ModifierPricing.GetPrice(_def.Id) - 1 : (int?)null;
            _tooltip.Show(_def.Name, description, (RectTransform)transform, subtitle, sellValue: sellValue);
        }
    }
}
