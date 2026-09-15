using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>One clickable/hoverable cell inside <see cref="GridView"/>.</summary>
    public sealed class GridCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public Image Background { get; private set; }
        private Image _badgeGolden;
        private Image _badgeSpecial;

        private GridView _owner;

        public void Init(GridView owner, int x, int y, Image background, Image badgeGolden, Image badgeSpecial)
        {
            _owner = owner;
            X = x;
            Y = y;
            Background = background;
            _badgeGolden = badgeGolden;
            _badgeSpecial = badgeSpecial;
        }

        public void ApplyState(Cell cell)
        {
            if (cell.IsLocked)
            {
                Background.color = VisualDefaults.LockedColor;
            }
            else if (cell.IsFilled && cell.FilledColor.HasValue)
            {
                Background.color = VisualDefaults.GetColor(cell.FilledColor.Value);
            }
            else if (cell.IsGolden)
            {
                Background.color = Color.Lerp(VisualDefaults.EmptyCellColor, VisualDefaults.GoldenColor, 0.55f);
            }
            else
            {
                Background.color = VisualDefaults.EmptyCellColor;
            }

            _badgeGolden.gameObject.SetActive(cell.IsGolden);
            if (cell.IsGolden)
            {
                _badgeGolden.color = VisualDefaults.GoldenColor;
            }

            bool showSpecial = cell.IsTinted || cell.IsMultiplierZone;
            _badgeSpecial.gameObject.SetActive(showSpecial);
            if (cell.IsMultiplierZone)
            {
                // Multiplier wins the badge slot visually when a cell stacks both
                // modifiers; both bonuses still apply to scoring regardless.
                _badgeSpecial.color = VisualDefaults.MultiplierOutline;
            }
            else if (cell.IsTinted)
            {
                _badgeSpecial.color = VisualDefaults.GetColor(cell.TintedColor);
            }
        }

        public void SetHoverTint(Color? overlay)
        {
            if (overlay.HasValue)
            {
                Background.color = Color.Lerp(Background.color, overlay.Value, 0.6f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellHoverEnter(X, Y);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellHoverExit(X, Y);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellClicked(X, Y);
        }
    }
}
