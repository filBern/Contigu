using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to each enemy icon in <see cref="HudView"/>'s enemy band —
    /// shows the shared <see cref="TooltipView"/> with that enemy's name
    /// and On-Shuffle effect on hover (explicit request: "il faut aussi
    /// pouvoir hover sur l'ennemi pour avoir plus de détails sur ce qu'il
    /// fait comme effet lorsqu'on shuffle"). Unlike <see
    /// cref="PoisonBadgeView"/>/<see cref="TraitBadgeView"/>, this icon
    /// never sits on top of another clickable element, so there's no click
    /// to forward. <see cref="Init"/> is called fresh every time <see
    /// cref="HudView.SetEncounter"/> refreshes this slot, since the same
    /// icon slot shows a different enemy from one round to the next.
    /// </summary>
    public sealed class EnemyIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _tooltip;
        private string _name;
        private string _description;

        public void Init(TooltipView tooltip, string name, string description)
        {
            _tooltip = tooltip;
            _name = name;
            _description = description;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Show(_name, _description, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Hide();
        }

        /// <summary>Same "don't leave the tooltip stuck open" fix as TraitBadgeView/PoisonBadgeView — HudView never destroys this icon, it just SetActive(false)s its slot once the enemy is gone, which doesn't fire OnPointerExit.</summary>
        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}
