using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>Shows the shared <see cref="TooltipView"/> while the pointer is over this element (a shop card's body). Buttons inside the element keep their own clicks.</summary>
    public sealed class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _tooltip;
        private string _name;
        private string _description;
        private string _subtitle;
        private Color? _subtitleColor;

        private string _footer;
        private bool _pinned;
        // A fertilizer's card: category pill (+ a reason pill) under the title, rarity box in the title band (see SetModifierCard).
        private System.Collections.Generic.List<TooltipChip> _chips;
        private TooltipChip? _badge;

        /// <summary>The element whose tooltip is up right now. A parent that re-pushes its own tooltip every frame (a fertilizer's sack) stands aside while one of its children (growth, Roots) owns it.</summary>
        public static HoverTooltip Active { get; private set; }

        /// <summary>Touch: keeps the tooltip up after the finger lifts (a release normally counts as the pointer leaving), with a hint line at its foot. <see cref="Unpin"/> hides it.</summary>
        public void Pin(string footer)
        {
            _pinned = true;
            _footer = footer;
            ShowNow();
        }

        public void Unpin()
        {
            if (!_pinned)
            {
                return;
            }
            _pinned = false;
            _footer = null;
            HideNow();
        }

        /// <summary>Gives this tooltip the fertilizer layout shared by every place a fertilizer is shown: its rarity in a colored box at the title's corner, its category as a pill, and (optionally) why it cannot be bought as a red pill.</summary>
        public void SetModifierCard(ModifierDefinition def, string reason = null)
        {
            _chips = new System.Collections.Generic.List<TooltipChip>
            {
                new TooltipChip(ModifierVisualDefaults.GetCategoryLabel(def.Category), ModifierVisualDefaults.GetCategoryColor(def.Category))
            };
            if (!string.IsNullOrEmpty(reason))
            {
                _chips.Add(new TooltipChip(reason, UITheme.Danger));
            }
            _badge = new TooltipChip(ModifierVisualDefaults.GetRarityLabel(def.Id), ModifierVisualDefaults.GetRarityColor(def.Id));
        }

        /// <summary>The upgrade (sprout) version of <see cref="SetModifierCard"/>: rarity in a colored box at the title's corner, the pool as a colored box under it, and the reason it cannot be bought as a red one.</summary>
        public void SetUpgradeCard(UpgradeDefinition def, string reason = null)
        {
            _chips = new System.Collections.Generic.List<TooltipChip>
            {
                new TooltipChip(UpgradeVisualDefaults.GetPoolLabel(def.Pool), UpgradeVisualDefaults.GetPoolColor(def.Pool))
            };
            if (!string.IsNullOrEmpty(reason))
            {
                _chips.Add(new TooltipChip(reason, UITheme.Danger));
            }
            _badge = new TooltipChip(UpgradeVisualDefaults.GetRarityLabel(def.Rarity), UpgradeVisualDefaults.GetRarityColor(def.Rarity));
        }

        public void Init(TooltipView tooltip, string name, string description, string subtitle = null, Color? subtitleColor = null)
        {
            _tooltip = tooltip;
            _name = name;
            _description = description;
            _subtitle = subtitle;
            _subtitleColor = subtitleColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!MobileSetup.PassiveHoverAllowed)
            {
                return;
            }
            // Unity sends the enter to the deepest element first, then to its parents: a child's tooltip is already up, keep it.
            if (Active != null && Active != this && Active.transform.IsChildOf(transform))
            {
                return;
            }
            ShowNow();
        }

        /// <summary>Shows the tooltip without a pointer: keyboard/controller focus or a touch long-press.</summary>
        public void ShowNow()
        {
            if (_tooltip != null)
            {
                Active = this;
                _tooltip.Show(_name, _description, (RectTransform)transform, _subtitle, _subtitleColor, footer: _footer, placeAbove: true, chips: _chips, badge: _badge);
            }
        }

        public void HideNow()
        {
            if (Active == this) { Active = null; }
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pinned)
            {
                return;
            }
            if (Active == this) { Active = null; }
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        /// <summary>A card rebuilt (or purchased and removed) while hovered never gets an exit event.</summary>
        private void OnDisable()
        {
            _pinned = false;
            _footer = null;
            if (Active == this) { Active = null; }
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}
