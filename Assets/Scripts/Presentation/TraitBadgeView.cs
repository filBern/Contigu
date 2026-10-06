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
            // GridCellView.SetHoverTint activates this badge to preview the
            // trait a valid placement would grant, without ever calling
            // Init() on it (only ApplyState does, once the trait is
            // actually placed) — so _tooltip can still be null here if the
            // player's cursor happens to sit on the badge's corner while
            // only hovering a placement preview (NullReferenceException
            // bug report).
            if (_tooltip == null)
            {
                return;
            }
            // Joker-exclusive combat kinds never go through the shop's
            // rarity-weighted draft at all (every Joker piece is tagged at
            // random the moment it's added — see DeckManager.AddJoker), so
            // "Common · Grid pool" would be actively misleading here.
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

        /// <summary>
        /// A badge can be destroyed while still hovered — e.g.
        /// TileChoiceView rebuilds a preview the instant its cell is
        /// clicked to deselect it, which never fires OnPointerExit first —
        /// leaving the tooltip stuck open for a trait that's no longer even
        /// there. Same unconditional Hide() as OnPointerExit above.
        /// </summary>
        private void OnDestroy()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        /// <summary>
        /// Same fix as <see cref="OnDestroy"/>, for the more common case on
        /// the grid: GridCellView never destroys this badge, it just
        /// SetActive(false)s it via ApplyState once the cell's OriginTrait
        /// clears (a line/column clear, or a Void/Kamikaze destruction) —
        /// which doesn't fire OnPointerExit either, so a tooltip left open
        /// while hovering a tile upgrade badge stayed stuck even after the
        /// tile it described was gone (explicit bug report: "Lorsqu'une
        /// tuile est cleared pendant qu'on hover sur son upgrade, le
        /// tooltip devrait être hidden").
        /// </summary>
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
