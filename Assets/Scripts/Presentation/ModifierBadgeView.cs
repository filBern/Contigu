using Contigu.Core;
using Contigu.Data;
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
        // "With bonus: +70% Mult": the effect as it is right now with level, growth and Roots (empty when there is no bonus).
        private System.Func<ModifierId, string> _bonusLineProvider;

        // Whether the pointer is currently resting over this badge — while
        // true, Update() below keeps re-pushing fresh content into the
        // tooltip every frame, since OnPointerEnter only fires once and the
        // underlying values (usage count, progressive state) keep changing.
        private bool _hovering;

        // Touch: the tooltip stays up after the finger lifts, until the panel unpins it (a second tap on the badge, or a tap elsewhere).
        private bool _pinned;

        public void Pin()
        {
            _pinned = true;
            ShowTooltip();
        }

        public void Unpin()
        {
            if (!_pinned)
            {
                return;
            }
            _pinned = false;
            if (!_hovering)
            {
                _tooltip.Hide();
            }
        }

        private void OnDisable()
        {
            if (_pinned)
            {
                _pinned = false;
                _tooltip.Hide();
            }
        }

        public void Init(TooltipView tooltip, ModifierDefinition def, System.Func<ModifierId, int> usageCountProvider = null, System.Func<ModifierId, string> progressiveStateProvider = null, System.Func<ModifierId, string> levelStateProvider = null, bool showSellValue = false, System.Func<ModifierId, string> bonusLineProvider = null)
        {
            _bonusLineProvider = bonusLineProvider;
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
            // Unity sends the enter to the deepest element first, then to its parents: a child tooltip (growth, Roots) is already up, keep it.
            var child = HoverTooltip.Active;
            if (child != null && child.transform.IsChildOf(transform))
            {
                return;
            }
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (_pinned)
            {
                return;
            }
            _tooltip.Hide();
        }

        private void Update()
        {
            if (_hovering || _pinned)
            {
                // A child's own tooltip (growth, Roots) keeps the tooltip while the pointer is on it.
                var child = HoverTooltip.Active;
                if (child != null && child.transform.IsChildOf(transform))
                {
                    return;
                }
                ShowTooltip();
            }
        }

        private void ShowTooltip()
        {
            // Queried fresh every time this runs rather than cached — both
            // keep changing (every placement that scores, or for the
            // progressive line every placement at all) for as long as this
            // same badge instance stays on screen.
            string categoryLabel = ModifierVisualDefaults.GetCategoryLabel(_def.Category);
            string subtitle = ModifierVisualDefaults.GetRarityLabel(_def.Id) + " · " + categoryLabel;
            // The category as a colored pill under the title; the rarity is a small colored box in the title band's corner (where the sell value used to be).
            var chips = new System.Collections.Generic.List<TooltipChip>
            {
                new TooltipChip(categoryLabel, ModifierVisualDefaults.GetCategoryColor(_def.Category)),
            };
            var rarityBadge = new TooltipChip(ModifierVisualDefaults.GetRarityLabel(_def.Id), ModifierVisualDefaults.GetRarityColor(_def.Id));
            string description = DescriptionTextFormatter.Colorize(_def.Description, 14);
            // What the description cannot say (its numbers are the base ones): the live state, in a section of its own under the description.
            // Progressive/incremental modifiers report their current effective state; the others a level line ("Level 2 - x1.5 effect", only past
            // level 1, skipped when the progressive line already reports the true value); then "With bonus" (level + growth + Roots).
            var details = new System.Collections.Generic.List<string>();
            string progressiveState = _progressiveStateProvider != null ? _progressiveStateProvider(_def.Id) : null;
            if (!string.IsNullOrEmpty(progressiveState))
            {
                details.Add(progressiveState);
            }
            else
            {
                string levelState = _levelStateProvider != null ? _levelStateProvider(_def.Id) : null;
                if (!string.IsNullOrEmpty(levelState)) { details.Add(levelState); }
            }
            string bonusLine = _bonusLineProvider != null ? _bonusLineProvider(_def.Id) : null;
            if (!string.IsNullOrEmpty(bonusLine))
            {
                details.Add(bonusLine);
            }
            _tooltip.Show(_def.Name, description, (RectTransform)transform, subtitle, chips: chips, badge: rarityBadge, details: details.Count > 0 ? string.Join("\n", details) : null);
        }
    }
}
