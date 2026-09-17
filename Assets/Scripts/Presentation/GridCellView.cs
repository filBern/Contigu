using System.Collections;
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
        private const float PulseDuration = 0.28f;
        private const float PulsePeakScale = 1.18f;
        private const float PulsePeakFraction = 0.4f;

        public int X { get; private set; }
        public int Y { get; private set; }

        public Image Background { get; private set; }
        private Image _fillTile;
        private Image _badgeGolden;
        private Image _badgeSpecial;
        private Image _badgeColorIcon;
        private Text _effectLabel;

        private GridView _owner;
        private Coroutine _pulseCoroutine;

        public void Init(GridView owner, int x, int y, Image background, Image fillTile, Image badgeGolden, Image badgeSpecial, Image badgeColorIcon, Text effectLabel)
        {
            _owner = owner;
            X = x;
            Y = y;
            Background = background;
            _fillTile = fillTile;
            _badgeGolden = badgeGolden;
            _badgeSpecial = badgeSpecial;
            _badgeColorIcon = badgeColorIcon;
            _effectLabel = effectLabel;
        }

        /// <summary>
        /// Renders this cell from <paramref name="cell"/>'s current state, unless
        /// <paramref name="fillColorOverride"/> is given — then it's painted as
        /// filled with that color regardless of the cell's actual (possibly
        /// already-cleared) fill state. Used to hold a just-completed line
        /// visually filled while its score is still being shown, before the
        /// clear animation actually empties it (see GridView).
        /// </summary>
        public void ApplyState(Cell cell, PieceColor? fillColorOverride = null)
        {
            bool isFilled = fillColorOverride.HasValue || (cell.IsFilled && cell.FilledColor.HasValue);
            PieceColor? filledColor = fillColorOverride ?? cell.FilledColor;

            if (cell.IsLocked)
            {
                if (VisualDefaults.LockedTileSprite != null)
                {
                    Background.sprite = VisualDefaults.LockedTileSprite;
                    Background.color = Color.white;
                }
                else
                {
                    Background.sprite = null;
                    Background.color = VisualDefaults.LockedColor;
                }
            }
            else if (isFilled)
            {
                // Always the pure piece color once filled — a golden cell's own
                // background is never tinted (that would shift the piece's actual
                // color); its golden status is conveyed by the badge and effect
                // label below instead, which persist regardless of fill state.
                Background.sprite = null;
                Background.color = VisualDefaults.GetColor(filledColor.Value);
            }
            else if (cell.IsGolden)
            {
                Background.sprite = null;
                Background.color = Color.Lerp(VisualDefaults.EmptyCellColor, VisualDefaults.GoldenColor, 0.55f);
            }
            else
            {
                Background.sprite = null;
                Background.color = VisualDefaults.EmptyCellColor;
            }

            // Neutral frame/bevel overlay on top of the flat fill, below every
            // badge — only for an actually-filled, unlocked cell.
            bool showFillTile = isFilled && !cell.IsLocked && VisualDefaults.FillTileSprite != null;
            _fillTile.gameObject.SetActive(showFillTile);
            if (showFillTile)
            {
                _fillTile.sprite = VisualDefaults.FillTileSprite;
            }

            _badgeGolden.gameObject.SetActive(cell.IsGolden);
            if (cell.IsGolden)
            {
                if (VisualDefaults.GoldenTileSprite != null)
                {
                    _badgeGolden.sprite = VisualDefaults.GoldenTileSprite;
                    _badgeGolden.color = Color.white;
                }
                else
                {
                    _badgeGolden.sprite = null;
                    _badgeGolden.color = VisualDefaults.GoldenColor;
                }
            }

            // Colorblind-accessibility badge: shows the piece color's icon on
            // top of the fill so color isn't the only signal. Hidden for any
            // color that has no icon yet (e.g. Coral) rather than showing a
            // blank/broken image.
            Sprite colorIcon = isFilled && !cell.IsLocked ? VisualDefaults.GetColorIcon(filledColor.Value) : null;
            _badgeColorIcon.gameObject.SetActive(colorIcon != null);
            if (colorIcon != null)
            {
                _badgeColorIcon.sprite = colorIcon;
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

            string effectText = BuildEffectLabel(cell);
            _effectLabel.text = effectText;
            _effectLabel.gameObject.SetActive(effectText.Length > 0);
        }

        /// <summary>Spells out a modifier cell's effect as text (golden's fixed bonus, tinted/multiplier's factor) instead of relying on badge color alone.</summary>
        private static string BuildEffectLabel(Cell cell)
        {
            string multiplierPart = null;
            if (cell.IsTinted && cell.IsMultiplierZone)
            {
                multiplierPart = "x4";
            }
            else if (cell.IsTinted || cell.IsMultiplierZone)
            {
                multiplierPart = "x2";
            }

            if (cell.IsGolden && multiplierPart != null)
            {
                return "+" + ScoringConstants.GoldenCellBonus + " " + multiplierPart;
            }
            if (cell.IsGolden)
            {
                return "+" + ScoringConstants.GoldenCellBonus;
            }
            return multiplierPart ?? string.Empty;
        }

        public void SetHoverTint(Color? overlay)
        {
            if (overlay.HasValue)
            {
                Background.color = Color.Lerp(Background.color, overlay.Value, 0.6f);
            }
        }

        /// <summary>Brief scale-up-then-back-down pulse, played when this cell scores points.</summary>
        public void Pulse()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            var rt = (RectTransform)transform;
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _pulseCoroutine = null;
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
