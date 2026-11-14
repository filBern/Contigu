using Contigu.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to each enemy icon in <see cref="HudView"/>'s enemy band;
    /// shows the shared <see cref="TooltipView"/> with that enemy's name
    /// and On-Shuffle effect on hover. Unlike <see cref="PoisonBadgeView"/>/<see
    /// cref="TraitBadgeView"/>, this icon never sits on top of another
    /// clickable element, so there's no click to forward. <see cref="Init"/>
    /// is called fresh every time <see cref="HudView.SetEncounter"/>
    /// refreshes this slot.
    /// </summary>
    public sealed class EnemyIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _tooltip;
        private string _name;
        private string _description;
        private string _lore;
        private string _footer;
        private TooltipChip? _badge;

        /// <summary>The enemy's role as a small colored box at the tooltip's corner: Boss (red), Creator (amber), Reactor (violet) or Basic (green).</summary>
        private static TooltipChip RoleBadge(EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Boss: return new TooltipChip("Boss", new Color(0.86f, 0.36f, 0.32f));
                case EnemyRole.Creator: return new TooltipChip("Creator", new Color(0.93f, 0.72f, 0.30f));
                case EnemyRole.Reactor: return new TooltipChip("Reactor", new Color(0.66f, 0.50f, 0.80f));
                default: return new TooltipChip("Basic", new Color(0.55f, 0.78f, 0.45f));
            }
        }

        public void Init(TooltipView tooltip, string name, string description, string lore = null, string footer = null, EnemyRole? role = null)
        {
            _badge = role.HasValue ? RoleBadge(role.Value) : (TooltipChip?)null;
            _tooltip = tooltip;
            _name = name;
            _description = description;
            _lore = lore;
            _footer = footer;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null || !MobileSetup.PassiveHoverAllowed)
            {
                return;
            }
            _tooltip.Show(_name, _description, (RectTransform)transform, lore: _lore, footer: _footer, badge: _badge);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Hide();
        }

        /// <summary>Same fix as TraitBadgeView/PoisonBadgeView: HudView SetActive(false)s this icon's slot when the enemy is gone, which doesn't fire OnPointerExit.</summary>
        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}
