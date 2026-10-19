using System.Collections;
using System.Collections.Generic;
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
        private const float PulseShakeDegrees = 5f;
        private const float PulseShakeWobbles = 2.5f;
        private const float ClearBurstDuration = 0.32f;
        private const int ClearBurstParticleCount = 6;
        private const float ClearBurstParticleSize = 10f;
        private const int ClearBurstPetalCount = 9;
        private const float ClearBurstPetalSize = 26f;
        private const float PetalBurstDuration = 1.0f;
        private const float PetalGravity = 520f;
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
        // Pulse animates this cell's own localScale; starting a new one stops the previous.
        private Coroutine _scaleCoroutine;

        // Which of this cell's visuals, if any, are currently an enemy-placed malus — set each time ApplyState
        // renders, read by SetMalusAlpha so GridView.FadeMalus can fade exactly those and nothing else.
        // What this cell's special flags mean, shown on hover (null when it has none).
        private string _infoName;
        private string _infoDescription;
        private bool _isShowingLockedObstacle;
        /// <summary>Locked/frozen look drawn over the empty-cell Background, so fading the malus out reveals an empty cell instead of nothing.</summary>
        private Image _lockedOverlay;
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
            var overlayGo = new GameObject("LockedOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(background.transform, false);
            overlayGo.transform.SetSiblingIndex(0);
            var overlayRect = (RectTransform)overlayGo.transform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _lockedOverlay = overlayGo.AddComponent<Image>();
            _lockedOverlay.type = Image.Type.Sliced;
            _lockedOverlay.raycastTarget = false;
            _lockedOverlay.gameObject.SetActive(false);
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
        public void ApplyState(Cell cell, PieceColor? fillColorOverride = null, PieceTrait? originTraitOverride = null, bool suppressMalus = false, bool forceShowLocked = false, bool forceShowPoisoned = false, bool forceShowFrozen = false)
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

            _lockedOverlay.gameObject.SetActive(renderAsLockedObstacle);
            if (renderAsLockedObstacle)
            {
                Sprite lockedSprite = (cell.IsFrozen || forceShowFrozen) && VisualDefaults.FrozenTileSprite != null ? VisualDefaults.FrozenTileSprite : VisualDefaults.LockedTileSprite;
                _lockedOverlay.sprite = lockedSprite;
                _lockedOverlay.color = lockedSprite != null ? Color.white : VisualDefaults.LockedColor;
                // The empty cell underneath is what remains once the lock fades out.
                Background.sprite = VisualDefaults.EmptyTileSprite;
                Background.color = VisualDefaults.EmptyTileSprite != null ? new Color(0.6f, 0.6f, 0.6f, 1f) : VisualDefaults.EmptyCellColor;
            }
            else if (isFilled)
            {
                // Shared card art tinted to the pure piece color — a golden cell's background is never
                // tinted itself, its golden status is conveyed by the badge/effect label instead.
                Background.sprite = VisualDefaults.GetTileSprite(filledColor.Value);
                Background.color = VisualDefaults.GetTileTint(filledColor.Value);
            }
            else if ((cell.IsPermanentMultiplier || cell.IsMultiplierZone) && VisualDefaults.BonusTileSprite != null)
            {
                // An empty plot that doubles what scores through it: its own big "x2" tile (a filled cell shows the x2 chip instead).
                Background.sprite = VisualDefaults.BonusTileSprite;
                Background.color = Color.white;
            }
            else if (cell.IsGolden)
            {
                Background.sprite = VisualDefaults.GoldenCellSprite;
                Background.color = VisualDefaults.GoldenCellSprite != null
                    ? Color.white
                    : Color.Lerp(VisualDefaults.EmptyCellColor, VisualDefaults.GoldenColor, 0.55f);
            }
            else
            {
                Background.sprite = VisualDefaults.EmptyTileSprite;
                // Dimmed so a planted (colored) tile pops out of the empty plots around it.
                Background.color = VisualDefaults.EmptyTileSprite != null ? new Color(0.6f, 0.6f, 0.6f, 1f) : VisualDefaults.EmptyCellColor;
            }

            // Fallback overlay for a filled, non-obstacle cell when TileSprite itself failed to load.
            bool showFillTile = isFilled && !renderAsLockedObstacle && VisualDefaults.TileSprite == null && VisualDefaults.FillTileSprite != null;
            _fillTile.gameObject.SetActive(showFillTile);
            if (showFillTile)
            {
                _fillTile.sprite = VisualDefaults.FillTileSprite;
            }

            // A Seeder cell is golden too, but its own trait badge already says so — one marker, not three.
            bool seederCell = originTrait.HasValue && originTrait.Value.Kind == PieceTraitKind.Seeder;
            bool showGoldenBadge = cell.IsGolden && !seederCell;
            _badgeGolden.gameObject.SetActive(showGoldenBadge);
            if (showGoldenBadge)
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

            // IsPermanentMultiplier reuses the same badge as IsMultiplierZone — shown here even while the
            // cell is empty, unlike every other badge in this method, since it's a property of the board
            // POSITION itself rather than whatever piece currently fills it (see Cell.IsPermanentMultiplier).
            bool bonusTileShown = !isFilled && !renderAsLockedObstacle && (cell.IsPermanentMultiplier || cell.IsMultiplierZone) && VisualDefaults.BonusTileSprite != null;
            bool showSpecial = (cell.IsTinted || cell.IsMultiplierZone || cell.IsPermanentMultiplier) && !bonusTileShown;
            _infoName = null;
            _infoDescription = null;
            if (cell.IsPermanentMultiplier)
            {
                _infoName = "Permanent Bonus Square";
                _infoDescription = "Doubles the whole score of any seed planted on this square, for the rest of the run.";
            }
            else if (cell.IsMultiplierZone)
            {
                _infoName = "Multiplier Zone";
                _infoDescription = "Doubles the whole placement's score — harvest included — when a seed is planted here.";
            }
            else if (cell.IsGolden)
            {
                _infoName = "Golden plot";
                _infoDescription = "+" + ScoringConstants.GoldenCellBonus + " points when a seed is planted here.";
            }
            else if (cell.IsTinted)
            {
                _infoName = "Tinted plot";
                _infoDescription = "Doubles the group and golden score of a seed planted here (not harvest).";
            }
            _badgeSpecial.gameObject.SetActive(showSpecial);
            if (cell.IsMultiplierZone || cell.IsPermanentMultiplier)
            {
                // Multiplier wins the badge slot visually when a cell stacks both
                // modifiers; both bonuses still apply to scoring regardless.
                // The "x2" chip (same icon as the Multiplier trait) — a bare blue square told the player nothing.
                var multiplierIcon = PieceTraitVisualDefaults.GetBadgeSprite(PieceTraitKind.Multiplier);
                _badgeSpecial.sprite = multiplierIcon;
                _badgeSpecial.color = multiplierIcon != null ? Color.white : VisualDefaults.MultiplierOutline;
            }
            else if (cell.IsTinted)
            {
                _badgeSpecial.sprite = null;
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
                ApplyTraitBadgeArt(originTrait.Value);
                _traitOriginBadgeView.Init(_tooltip, originTrait.Value, gameObject);
            }

            string effectText = BuildEffectLabel(cell, seederCell);
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
                var c = _lockedOverlay.color;
                _lockedOverlay.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_isShowingPoisonBadge)
            {
                var c = _badgePoison.color;
                _badgePoison.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>Spells out a modifier cell's effect as text (golden's fixed bonus, tinted/multiplier's factor) instead of relying on badge color alone.</summary>
        private static string BuildEffectLabel(Cell cell, bool hideGolden)
        {
            bool isGolden = cell.IsGolden && !hideGolden;
            string multiplierPart = null;
            if (cell.IsTinted && cell.IsMultiplierZone)
            {
                multiplierPart = "x4";
            }
            else if (cell.IsTinted || cell.IsMultiplierZone)
            {
                multiplierPart = "x2";
            }

            if (isGolden && multiplierPart != null)
            {
                return "+" + ScoringConstants.GoldenCellBonus + " " + multiplierPart;
            }
            if (isGolden)
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
        /// <summary>Shows the trait's own icon on the origin badge, or the old flat trait color when that art is missing.</summary>
        private void ApplyTraitBadgeArt(PieceTrait trait)
        {
            var sprite = PieceTraitVisualDefaults.GetBadgeSprite(trait.Kind);
            _badgeTraitOrigin.sprite = sprite;
            _badgeTraitOrigin.color = sprite != null ? Color.white : PieceTraitVisualDefaults.GetBadgeColor(trait);
        }

        public void SetHoverTint(Color? overlay, bool isValid, PieceTrait? previewTrait = null, PieceColor? ghostColor = null)
        {
            if (isValid && ghostColor.HasValue)
            {
                // A see-through copy of the piece's own tile instead of a green wash: green over the brown soil
                // read as muddy olive, whereas the real tile art is unmistakable (and shows the color being placed).
                Background.sprite = VisualDefaults.GetTileSprite(ghostColor.Value);
                var tint = VisualDefaults.GetTileTint(ghostColor.Value);
                Background.color = new Color(tint.r, tint.g, tint.b, 0.8f);
            }
            else if (overlay.HasValue)
            {
                Background.color = Color.Lerp(Background.color, overlay.Value, 0.6f);
            }

            _invalidMarker.gameObject.SetActive(!isValid);

            if (isValid && previewTrait.HasValue)
            {
                var trait = previewTrait.Value;
                _badgeTraitOrigin.gameObject.SetActive(true);
                ApplyTraitBadgeArt(trait);
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
        public void Pulse(bool shake = false)
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                ((RectTransform)transform).localRotation = Quaternion.identity;
            }
            _scaleCoroutine = StartCoroutine(PulseRoutine(shake));
        }

        private IEnumerator PulseRoutine(bool shake)
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
                if (shake)
                {
                    // A quick wobble that dies out with the pulse.
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * PulseShakeWobbles * Mathf.PI * 2f) * PulseShakeDegrees * (1f - p));
                }
                yield return null;
            }
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            _scaleCoroutine = null;
        }

        private const int IceShardCount = 9;
        private const float IceShatterDuration = 0.7f;
        private const float IceGravity = 640f;

        /// <summary>A frozen plot breaking: a bright flash and a spray of ice shards that fall with gravity (drawn above neighbouring cells).</summary>
        public void PlayIceShatter()
        {
            StartCoroutine(IceShatterRoutine());
        }

        private IEnumerator IceShatterRoutine()
        {
            var shardSprite = VisualDefaults.GetIconSprite("IceShard");
            var flashSprite = VisualDefaults.GetIconSprite("IceFlash");
            var parts = new List<Image>();
            var velocities = new List<Vector2>();
            var spins = new List<float>();
            var sorting = new GameObject("IceShatter", typeof(RectTransform));
            sorting.transform.SetParent(transform, false);
            var host = (RectTransform)sorting.transform;
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.sizeDelta = Vector2.zero;
            var canvas = sorting.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31;

            Image flash = null;
            if (flashSprite != null)
            {
                flash = UIFactory.CreatePanel(host, "IceFlash", Color.white);
                flash.raycastTarget = false;
                flash.sprite = flashSprite;
                flash.rectTransform.sizeDelta = new Vector2(120f, 120f);
            }
            if (shardSprite != null)
            {
                for (int i = 0; i < IceShardCount; i++)
                {
                    var shard = UIFactory.CreatePanel(host, "IceShard", Color.white);
                    shard.raycastTarget = false;
                    shard.sprite = shardSprite;
                    shard.preserveAspect = true;
                    float size = Random.Range(18f, 34f);
                    shard.rectTransform.sizeDelta = new Vector2(size, size);
                    shard.rectTransform.anchoredPosition = new Vector2(Random.Range(-14f, 14f), Random.Range(-14f, 14f));
                    shard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    float angle = (360f / IceShardCount * i + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                    velocities.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.6f) * Random.Range(120f, 230f));
                    spins.Add(Random.Range(-420f, 420f));
                    parts.Add(shard);
                }
            }

            float t = 0f;
            while (t < IceShatterDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / IceShatterDuration);
                if (flash != null)
                {
                    float f = Mathf.Clamp01(1f - p * 3.2f);
                    flash.color = new Color(1f, 1f, 1f, f);
                    float s = 0.7f + p * 1.1f;
                    flash.rectTransform.localScale = new Vector3(s, s, 1f);
                }
                for (int i = 0; i < parts.Count; i++)
                {
                    var v = velocities[i];
                    v.y -= IceGravity * Time.deltaTime;
                    velocities[i] = v;
                    parts[i].rectTransform.anchoredPosition += v * Time.deltaTime;
                    parts[i].rectTransform.Rotate(0f, 0f, spins[i] * Time.deltaTime);
                    parts[i].color = new Color(1f, 1f, 1f, 1f - p * p);
                }
                yield return null;
            }
            Destroy(sorting);
        }

        /// <summary>
        /// Small radial burst of fading squares in <paramref name="color"/>, played as this cell empties.
        /// Self-contained fire-and-forget (unlike <see cref="Pulse"/> it's never re-triggered mid-flight) —
        /// spawns its own particle children rather than animating this cell's transform, so it plays
        /// independently of any concurrent Pulse.
        /// </summary>
        public void PlayClearBurst(Color color, PieceColor? petalColor = null)
        {
            StartCoroutine(ClearBurstRoutine(color, petalColor));
        }

        private IEnumerator ClearBurstRoutine(Color color, PieceColor? petalColor)
        {
            // A harvested tile bursts into petals of its own color (Icons/Petals); anything else keeps the plain squares.
            Sprite petal = petalColor.HasValue ? VisualDefaults.GetPetalSprite(petalColor.Value) : null;
            int count = petal != null ? ClearBurstPetalCount : ClearBurstParticleCount;
            float size = petal != null ? ClearBurstPetalSize : ClearBurstParticleSize;
            var particles = new Image[count];
            var directions = new Vector2[count];
            var spins = new float[count];
            for (int i = 0; i < count; i++)
            {
                var particle = UIFactory.CreatePanel(transform, "ClearBurst", petal != null ? Color.white : color);
                particle.raycastTarget = false;
                if (petal != null)
                {
                    particle.sprite = petal;
                    particle.preserveAspect = true;
                    particle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    spins[i] = Random.Range(-260f, 260f);
                    // Falling petals leave their own cell, so they get their own sorting layer to stay visible over the cells below.
                    var sorting = particle.gameObject.AddComponent<Canvas>();
                    sorting.overrideSorting = true;
                    sorting.sortingOrder = 30;
                }
                particle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                particle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                particle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                particle.rectTransform.anchoredPosition = Vector2.zero;
                particle.rectTransform.sizeDelta = new Vector2(size, size);

                // Evenly spread around the circle, with a little jitter so a
                // burst never looks like a perfectly mechanical rosette.
                float angle = (360f / count) * i + Random.Range(-15f, 15f);
                directions[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                particles[i] = particle;
            }

            if (petal != null)
            {
                // Petals launch outward and up, then fall under gravity while spinning, fading out only near the end.
                var velocities = new Vector2[count];
                var positions = new Vector2[count];
                for (int i = 0; i < count; i++)
                {
                    velocities[i] = directions[i] * Random.Range(110f, 210f) + Vector2.up * Random.Range(60f, 140f);
                }
                float elapsed = 0f;
                while (elapsed < PetalBurstDuration)
                {
                    float dt = Time.deltaTime;
                    elapsed += dt;
                    float p = Mathf.Clamp01(elapsed / PetalBurstDuration);
                    float alpha = p < 0.55f ? 1f : 1f - (p - 0.55f) / 0.45f;
                    for (int i = 0; i < count; i++)
                    {
                        velocities[i].y -= PetalGravity * dt;
                        velocities[i].x *= 1f - 1.2f * dt; // light air drag so they drift rather than shoot off
                        positions[i] += velocities[i] * dt;
                        var rt = particles[i].rectTransform;
                        rt.anchoredPosition = positions[i];
                        rt.localRotation *= Quaternion.Euler(0f, 0f, spins[i] * dt);
                        float scale = Mathf.Lerp(1f, 0.6f, p);
                        rt.localScale = new Vector3(scale, scale, 1f);
                        particles[i].color = new Color(1f, 1f, 1f, alpha);
                    }
                    yield return null;
                }
            }
            else
            {
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
                        if (petal != null)
                        {
                            rt.localRotation *= Quaternion.Euler(0f, 0f, spins[i] * Time.deltaTime);
                        }
                        var c = particles[i].color;
                        particles[i].color = new Color(c.r, c.g, c.b, 1f - p);
                    }
                    yield return null;
                }

            }

            for (int i = 0; i < particles.Length; i++)
            {
                Destroy(particles[i].gameObject);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellHoverEnter(X, Y);
            // Explains a special plot (bonus square, golden, tinted, multiplier zone); stays quiet while a seed is being placed.
            if (_tooltip != null && _infoName != null && (_owner == null || !_owner.HasSelectedShape))
            {
                _tooltip.Show(_infoName, DescriptionTextFormatter.Colorize(_infoDescription, 14), (RectTransform)transform);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_owner != null) _owner.OnCellHoverExit(X, Y);
            if (_tooltip != null && _infoName != null)
            {
                _tooltip.Hide();
            }
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
