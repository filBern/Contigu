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
            // GridCellView.SetHoverTint activates this badge to preview a placement without calling Init() (only ApplyState does), so _tooltip can be null here.
            if (_tooltip == null)
            {
                return;
            }
            // Joker-exclusive combat kinds never go through the shop's rarity-weighted draft (every Joker piece is tagged at random when added; see DeckManager.AddJoker), so "Common · Grid pool" would be misleading here.
            if (PieceTrait.IsJokerCombatKind(_trait.Kind))
            {
                var jokerColor = VisualDefaults.GetColor(PieceColor.Joker);
                _tooltip.Show(PieceTraitVisualDefaults.GetName(_trait.Kind), DescriptionTextFormatter.Colorize(PieceTraitVisualDefaults.GetDescription(_trait), 14), (RectTransform)transform, "Joker-exclusive", jokerColor);
                return;
            }
            var rarity = PieceTraitVisualDefaults.GetRarity(_trait.Kind);
            string subtitle = UpgradeVisualDefaults.GetRarityLabel(rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(UpgradePool.Grid);
            _tooltip.Show(PieceTraitVisualDefaults.GetName(_trait.Kind), DescriptionTextFormatter.Colorize(PieceTraitVisualDefaults.GetDescription(_trait), 14), (RectTransform)transform, subtitle, UpgradeVisualDefaults.GetRarityColor(rarity));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }
            _tooltip.Hide();
        }

        /// <summary>A badge can be destroyed while still hovered without OnPointerExit firing first, leaving the tooltip stuck open.</summary>
        private void OnDestroy()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        /// <summary>Same fix as <see cref="OnDestroy"/>: GridCellView SetActive(false)s this badge via ApplyState when the cell's OriginTrait clears, which also doesn't fire OnPointerExit.</summary>
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
