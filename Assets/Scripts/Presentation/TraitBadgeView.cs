using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// Attached to a piece's enchanted-tile badge (see HandView) — shows the
    /// shared <see cref="TooltipView"/> with that trait's full name/effect on
    /// hover, since the badge itself only carries a small color chip. Also
    /// forwards clicks to <see cref="_clickForwardTarget"/> (the hand slot's
    /// own Button) so hovering/clicking the tiny badge never swallows the
    /// click that would otherwise select the piece.
    /// </summary>
    public sealed class TraitBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private TooltipView _tooltip;
        private PieceTrait _trait;
        private GameObject _clickForwardTarget;

        public void Init(TooltipView tooltip, PieceTrait trait, GameObject clickForwardTarget)
        {
            _tooltip = tooltip;
            _trait = trait;
            _clickForwardTarget = clickForwardTarget;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var rarity = PieceTraitVisualDefaults.GetRarity(_trait.Kind);
            string subtitle = UpgradeVisualDefaults.GetRarityLabel(rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(UpgradePool.Grid);
            _tooltip.Show(PieceTraitVisualDefaults.GetName(_trait.Kind), PieceTraitVisualDefaults.GetDescription(_trait), (RectTransform)transform, subtitle, UpgradeVisualDefaults.GetRarityColor(rarity));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip.Hide();
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
