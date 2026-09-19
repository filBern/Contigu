using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers (no cap — the active set is
    /// unlimited). Laid out as a 2-column grid of bare badges (no per-row
    /// card background) so a long list still fits the panel reasonably —
    /// the panel itself grows/shrinks to fit however many rows that takes.
    /// A readout refreshed by the caller whenever the active set changes, plus
    /// a <see cref="Pulse"/> effect the caller triggers on a badge whenever it
    /// actually scores points on a placement.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        private const float PanelWidth = 230f;
        private const float BadgeSize = 90f;
        private const float BadgeSpacing = 10f;
        private const float TopPadding = 16f;
        private const float BottomPadding = 16f;
        // card_bg_2's own 9-slice border is 24 (bottom) + 6 (top) = 30 tall —
        // below that the sprite has no room left for its stretchable middle
        // and the top/bottom border chunks visually overlap/glitch (seen with
        // zero active modifiers, where TopPadding + BottomPadding alone is
        // only 32, barely above that). Panel height is clamped to never go
        // below this, comfortably clear of the glitch threshold.
        private const float MinPanelHeight = 64f;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;

        private RectTransform _root;
        private RectTransform _rowsContainer;
        private TooltipView _tooltip;

        // Parallel to the active-modifiers list passed to the last Refresh —
        // lets Pulse(id) find the badge currently showing that modifier (each
        // modifier can only be active once per run, see
        // RunManager.RollModifierDraftOptions).
        private readonly List<ModifierId> _rowIds = new List<ModifierId>();
        private readonly List<Image> _rowBadges = new List<Image>();

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            var panel = UIFactory.CreateSlicedImage(parent, "ModifierPanel", UISprites.ModifierPanelBackground);
            _root = panel.rectTransform;
            // Anchored/pivoted from the TOP (not vertically centered like
            // before) — sizeDelta.y now changes every Refresh to fit however
            // many modifiers are active, and a center pivot would grow the
            // panel symmetrically in both directions instead of just
            // extending downward as content is added.
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(PanelWidth, MinPanelHeight);
            _root.anchoredPosition = new Vector2(40f, -170f);

            // "Modifiers" floats ABOVE the card entirely (on explicit
            // request) instead of sitting on top of it — white text directly
            // on card_bg_2's near-white body had zero contrast (and a flat
            // color bar behind it, tried first, was asked to be removed
            // again). Anchored to the card's top edge with a BOTTOM pivot and
            // a small positive offset, so the text sits just above the card,
            // over the game's own dark background, where white reads fine
            // with no extra element needed at all.
            var header = UIFactory.CreateText(_root, "Header", "Modifiers", 28, Color.white);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 0f);
            header.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            header.rectTransform.sizeDelta = new Vector2(PanelWidth, 36f);

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            _rowsContainer.anchoredPosition = new Vector2(0f, -TopPadding);

            var layout = _rowsContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(BadgeSize, BadgeSize);
            layout.spacing = new Vector2(BadgeSpacing, BadgeSpacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            return _root;
        }

        public void Refresh(IReadOnlyList<ModifierId> activeModifiers)
        {
            for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowsContainer.GetChild(i).gameObject);
            }
            _rowIds.Clear();
            _rowBadges.Clear();

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var badge = ModifierBadgeFactory.Create(_rowsContainer, ModifierCatalog.Get(activeModifiers[i]), BadgeSize, _tooltip);
                _rowIds.Add(activeModifiers[i]);
                _rowBadges.Add(badge);
            }

            int rowCount = activeModifiers.Count == 0 ? 0 : Mathf.CeilToInt(activeModifiers.Count / 2f);
            float contentHeight = rowCount == 0 ? 0f : rowCount * BadgeSize + (rowCount - 1) * BadgeSpacing;
            float panelHeight = Mathf.Max(MinPanelHeight, TopPadding + contentHeight + BottomPadding);
            _root.sizeDelta = new Vector2(PanelWidth, panelHeight);
        }

        /// <summary>Flashes the badge (and gives its row a small scale pulse) of every row currently showing <paramref name="id"/> — called when that modifier actually scores on a placement.</summary>
        public void Pulse(ModifierId id)
        {
            for (int i = 0; i < _rowIds.Count; i++)
            {
                if (_rowIds[i] == id)
                {
                    StartCoroutine(PulseBadge(_rowBadges[i]));
                }
            }
        }

        private IEnumerator PulseBadge(Image badge)
        {
            var baseColor = badge.color;
            var highlightColor = Color.white;
            var rt = badge.rectTransform;
            float t = 0f;
            while (t < PulseDuration)
            {
                // The panel can be refreshed (rows destroyed/rebuilt) mid-pulse
                // if a new draft/round starts right as this plays — bail out
                // rather than touching a destroyed row.
                if (badge == null)
                {
                    yield break;
                }

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                badge.color = Color.Lerp(highlightColor, baseColor, p);
                yield return null;
            }

            if (badge != null)
            {
                rt.localScale = Vector3.one;
                badge.color = baseColor;
            }
        }
    }
}
