using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>One clickable/hoverable/droppable cell inside <see cref="GridView"/>.</summary>
    public sealed class GridCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDropHandler
    {
        private const float PulseDuration = 0.28f;
        private const float PulsePeakScale = 1.18f;
        private const float PulsePeakFraction = 0.4f;
        private const float ClearBurstDuration = 0.32f;
        private const int ClearBurstParticleCount = 6;
        private const float ClearBurstParticleSize = 10f;
        private const float ClearBurstTravelDistance = 42f;

        public int X { get; private set; }
        public int Y { get; private set; }

        public Image Background { get; private set; }
        private Image _fillTile;
        private Image _badgeGolden;
        private Image _badgeSpecial;
        private Image _badgePoison;
        private PoisonBadgeView _poisonBadgeView;
        private Image _invalidMarker;
        private Text _effectLabel;
        private Image _badgeTraitOrigin;
        private TraitBadgeView _traitOriginBadgeView;
        private Image _colorblindShape;
        // A GameObject, not an Image — wraps 4 border bars (see GridView.BuildLineClearBorder), toggled as one unit.
        private GameObject _lineClearOverlay;
        private TooltipView _tooltip;

        private GridView _owner;
        private Coroutine _pulseCoroutine;

        // Which of this cell's visuals, if any, are currently an enemy-placed malus — set each time ApplyState
        // renders, read by SetMalusAlpha so GridView.FadeMalus can fade exactly those and nothing else.
        private bool _isShowingLockedObstacle;
        private bool _isShowingPoisonBadge;

        public void Init(GridView owner, int x, int y, Image background, Image fillTile, Image badgeGolden, Image badgeSpecial, Image badgePoison, Image invalidMarker, Text effectLabel, Image badgeTraitOrigin, Image colorblindShape, GameObject lineClearOverlay, TooltipView tooltip)
        {
            _owner = owner;
            X = x;
            Y = y;
            Background = background;
            _fillTile = fillTile;
            _badgeGolden = badgeGolden;
            _badgeSpecial = badgeSpecial;
            _badgePoison = badgePoison;
            _poisonBadgeView = badgePoison.gameObject.AddComponent<PoisonBadgeView>();
            _invalidMarker = invalidMarker;
            _effectLabel = effectLabel;
            _badgeTraitOrigin = badgeTraitOrigin;
            _colorblindShape = colorblindShape;
            _lineClearOverlay = lineClearOverlay;
            _tooltip = tooltip;
            _traitOriginBadgeView = badgeTraitOrigin.gameObject.AddComponent<TraitBadgeView>();
        }

        /// <summary>
        /// Renders this cell from <paramref name="cell"/>'s current state, unless <paramref name="fillColorOverride"/>
        /// is given, in which case it's painted as filled with that color regardless of the cell's actual
        /// (possibly already-cleared) state — used to hold a just-completed line visually filled while its
        /// score plays out, before the clear animation empties it. <paramref name="originTraitOverride"/>
        /// follows the same convention for the trait-origin badge.
        /// <paramref name="suppressMalus"/> renders the cell's true fill state while hiding its locked-obstacle
        /// look and poison badge, used for a deferred cell whose lock/poison this same placement just added.
        /// <paramref name="forceShowLocked"/>/<paramref name="forceShowPoisoned"/> render as if still
        /// locked/poisoned even though the underlying state already released, until the real reveal, and keep
        /// the cell flagged for GridView.FadeMalus to fade out along with the rest.
        /// </summary>
        public void ApplyState(Cell cell, PieceColor? fillColorOverride = null, PieceTrait? originTraitOverride = null, bool suppressMalus = false, bool forceShowLocked = false, bool forceShowPoisoned = false)
        {
            bool isFilled = fillColorOverride.HasValue || (cell.IsFilled && cell.FilledColor.HasValue);
            PieceColor? filledColor = fillColorOverride ?? cell.FilledColor;
            PieceTrait? originTrait = fillColorOverride.HasValue ? originTraitOverride : cell.OriginTrait;

            // A Bastion cell (see Cell.IsBastion) is locked AND filled at the same time — it renders like any
            // other filled tile rather than the empty "locked obstacle" look other locked cells get.
            // Also excluded while fillColorOverride is set (the "held" rendering during a score cascade),
            // so a lock added to a just-cleared cell this same placement doesn't break that illusion.
            bool renderAsLockedObstacle = (cell.IsLocked || forceShowLocked) && !cell.IsBastion && !fillColorOverride.HasValue && !suppressMalus;
            _isShowingLockedObstacle = renderAsLockedObstacle;

            if (renderAsLockedObstacle)
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
                // Shared card art tinted to the pure piece color — a golden cell's background is never
                // tinted itself, its golden status is conveyed by the badge/effect label instead.
                Background.sprite = VisualDefaults.TileSprite;
                Background.color = VisualDefaults.GetColor(filledColor.Value);
            }
            else if (cell.IsGolden)
            {
                Background.sprite = null;
                Background.color = Color.Lerp(VisualDefaults.EmptyCellColor, VisualDefaults.GoldenColor, 0.55f);
            }
            else
            {
                Background.sprite = VisualDefaults.TileSprite;
                Background.color = VisualDefaults.TileSprite != null ? Color.white : VisualDefaults.EmptyCellColor;
            }

            // Fallback overlay for a filled, non-obstacle cell when TileSprite itself failed to load.
            bool showFillTile = isFilled && !renderAsLockedObstacle && VisualDefaults.TileSprite == null && VisualDefaults.FillTileSprite != null;
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

            // Poisoner mechanic (see Cell.IsPoisoned/RunManager.ApplyPoisonScoreRule): points scored here
            // count negative instead of positive. Tooltip Init is lazy since the badge's own Init above
            // runs before _tooltip is assigned.
            bool showPoisonBadge = (cell.IsPoisoned || forceShowPoisoned) && !suppressMalus;
            _badgePoison.gameObject.SetActive(showPoisonBadge);
            _isShowingPoisonBadge = showPoisonBadge;
            if (showPoisonBadge)
            {
                _poisonBadgeView.Init(_tooltip, gameObject);
            }

            // Marks which deck upgrade originally enchanted this cell (see Cell.OriginTrait), independent
            // of the Golden/Tinted/MultiplierZone badges above.
            bool showTraitOrigin = originTrait.HasValue;
            _badgeTraitOrigin.gameObject.SetActive(showTraitOrigin);
            if (showTraitOrigin)
            {
                _badgeTraitOrigin.color = PieceTraitVisualDefaults.GetBadgeColor(originTrait.Value);
                _traitOriginBadgeView.Init(_tooltip, originTrait.Value, gameObject);
            }

            string effectText = BuildEffectLabel(cell);
            _effectLabel.text = effectText;
            _effectLabel.gameObject.SetActive(effectText.Length > 0);

            bool showColorblindShape = ColorblindMode.IsEnabled && isFilled && !renderAsLockedObstacle;
            _colorblindShape.gameObject.SetActive(showColorblindShape);
            if (showColorblindShape)
            {
                _colorblindShape.sprite = ColorblindShapeFactory.GetShape(filledColor.Value);
            }

            // Hover-only decorations — never part of a cell's actual state,
            // so every real render (including the one ClearHover triggers)
            // hides them again.
            _invalidMarker.gameObject.SetActive(false);
            _lineClearOverlay.SetActive(false);
        }

        /// <summary>Whether this cell is currently showing any malus visual — used by GridView.FadeMalus to collect just the cells worth animating.</summary>
        public bool IsShowingMalus()
        {
            return _isShowingLockedObstacle || _isShowingPoisonBadge;
        }

        /// <summary>Sets the alpha of just this cell's malus visuals (locked-obstacle look and/or poison badge), leaving its normal tile fill untouched otherwise.</summary>
        public void SetMalusAlpha(float alpha)
        {
            if (_isShowingLockedObstacle)
            {
                var c = Background.color;
                Background.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_isShowingPoisonBadge)
            {
                var c = _badgePoison.color;
                _badgePoison.color = new Color(c.r, c.g, c.b, alpha);
            }
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

        /// <summary>
        /// Tints the cell green/red for valid/invalid placement preview, since the tile's own tinted card
        /// shape only appears once actually placed. When invalid, also shows a solid red marker.
        /// <paramref name="previewTrait"/> previews the same top-right trait-origin badge <see cref="ApplyState"/>
        /// shows once placed, on the cell that would carry the piece's enchantment. ClearHover's follow-up
        /// ApplyState call resets everything once the hover ends.
        /// </summary>
        public void SetHoverTint(Color? overlay, bool isValid, PieceTrait? previewTrait = null)
        {
            if (overlay.HasValue)
            {
                Background.color = Color.Lerp(Background.color, overlay.Value, 0.6f);
            }

            _invalidMarker.gameObject.SetActive(!isValid);

            if (isValid && previewTrait.HasValue)
            {
                var trait = previewTrait.Value;
                _badgeTraitOrigin.gameObject.SetActive(true);
                _badgeTraitOrigin.color = PieceTraitVisualDefaults.GetBadgeColor(trait);
            }
        }

        /// <summary>
        /// Gold border marking this cell as part of a row/column that would clear if the hovered piece were
        /// placed here (see GridManager.PreviewClearedLineCells/GridView.OnCellHoverEnter). A separate overlay
        /// layer rather than folding into SetHoverTint's own green/red Lerp, since a cleared line's pre-existing
        /// cells need to keep showing their true fill color underneath. Reset by the next ApplyState render.
        /// </summary>
        public void SetLineClearPreview(bool active)
        {
            _lineClearOverlay.SetActive(active);
        }

        /// <summary>Brief scale-up-then-back-down pulse, played when this cell scores points, or when it's previewed as part of the prospective group while hovering a valid placement (see GridView.OnCellHoverEnter).</summary>
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

        /// <summary>
        /// Small radial burst of fading squares in <paramref name="color"/>, played as this cell empties.
        /// Self-contained fire-and-forget (unlike <see cref="Pulse"/> it's never re-triggered mid-flight) —
        /// spawns its own particle children rather than animating this cell's transform, so it plays
        /// independently of any concurrent Pulse.
        /// </summary>
        public void PlayClearBurst(Color color)
        {
            StartCoroutine(ClearBurstRoutine(color));
        }

        private IEnumerator ClearBurstRoutine(Color color)
        {
            var particles = new Image[ClearBurstParticleCount];
            var directions = new Vector2[ClearBurstParticleCount];
            for (int i = 0; i < ClearBurstParticleCount; i++)
            {
                var particle = UIFactory.CreatePanel(transform, "ClearBurst", color);
                particle.raycastTarget = false;
                particle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                particle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                particle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                particle.rectTransform.anchoredPosition = Vector2.zero;
                particle.rectTransform.sizeDelta = new Vector2(ClearBurstParticleSize, ClearBurstParticleSize);

                // Evenly spread around the circle, with a little jitter so a
                // burst never looks like a perfectly mechanical rosette.
                float angle = (360f / ClearBurstParticleCount) * i + Random.Range(-15f, 15f);
                directions[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                particles[i] = particle;
            }

            float t = 0f;
            while (t < ClearBurstDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / ClearBurstDuration);
                for (int i = 0; i < particles.Length; i++)
                {
                    var rt = particles[i].rectTransform;
                    rt.anchoredPosition = directions[i] * ClearBurstTravelDistance * p;
                    float scale = Mathf.Lerp(1f, 0.2f, p);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    var c = particles[i].color;
                    particles[i].color = new Color(c.r, c.g, c.b, 1f - p);
                }
                yield return null;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                Destroy(particles[i].gameObject);
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

        /// <summary>Fired by Unity's EventSystem when a drag (see HandSlotDragHandler) is released over this cell — reuses the exact same placement path as a plain click.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellClicked(X, Y);
        }
    }
}
