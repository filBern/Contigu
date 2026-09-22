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

        public void Init(TooltipView tooltip, ModifierDefinition def, System.Func<ModifierId, int> usageCountProvider = null, System.Func<ModifierId, string> progressiveStateProvider = null)
        {
            _tooltip = tooltip;
            _def = def;
            _usageCountProvider = usageCountProvider;
            _progressiveStateProvider = progressiveStateProvider;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Queried fresh on every hover rather than passed in at Init —
            // both keep changing (every placement that scores, or for the
            // progressive line every placement at all) for as long as this
            // same badge instance stays on screen.
            string subtitle = _usageCountProvider != null
                ? "Used " + _usageCountProvider(_def.Id) + "x this run"
                : null;
            string description = DescriptionTextFormatter.Colorize(_def.Description);
            // Progressive/incremental modifiers (Gradient, Repetition,
            // Synergie, Densité, Épuisement, Cartes Enchantées, Multitude,
            // Solidarité, Experience) get an extra line showing their
            // CURRENT effective state (on explicit request: "il faut
            // afficher dans le tooltip l'état progressif du modifier (ex:
            // Currently x2.3)") — appended to the description rather than
            // the fixed-height subtitle slot, since the description label
            // already auto-sizes its height around whatever text it holds.
            string progressiveState = _progressiveStateProvider != null ? _progressiveStateProvider(_def.Id) : null;
            if (!string.IsNullOrEmpty(progressiveState))
            {
                description = description + "\n\n" + DescriptionTextFormatter.Colorize(progressiveState);
            }
            _tooltip.Show(_def.Name, description, (RectTransform)transform, subtitle);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip.Hide();
        }
    }
}
