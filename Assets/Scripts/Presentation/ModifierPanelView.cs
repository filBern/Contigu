using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers as a 2-column grid of bare badges
    /// (no per-row card background). The panel uses a STATIC height sized
    /// for exactly 10 modifiers (2x5) — on explicit request, after several
    /// dynamic-resize approaches each ran into their own 9-slice rendering
    /// glitch at one panel height or another (see README). Active modifiers
    /// are otherwise unlimited (RunManager has no cap), so beyond 10 the
    /// grid simply keeps growing past the card's own visible area.
    /// A readout refreshed by the caller whenever the active set changes, plus
    /// a <see cref="Pulse"/> effect the caller triggers on a badge whenever it
    /// actually scores points on a placement.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        private const float PanelWidth = 230f;
        private const float BadgeSize = 90f;
        private const float BadgeSpacing = 10f;
        private const float HeaderHeight = 58f;
        private const float BottomPadding = 16f;
        private const int DisplayRows = 5; // 2 columns x 5 rows = 10 modifiers
        private const float PanelHeight = HeaderHeight + DisplayRows * BadgeSize + (DisplayRows - 1) * BadgeSpacing + BottomPadding;
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
            // Vertically centered, STATIC size — the panel never resizes at
            // runtime anymore (see the class doc comment), so there's no
            // longer a header-jump or 9-slice-at-small-size risk from a
            // center pivot the way there was while sizeDelta.y changed
            // every Refresh.
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _root.anchoredPosition = new Vector2(40f, 0f);

            // "Modifiers" sits inside panel_bg's own header band near the
            // top of the card (on explicit request) — panel_bg's header
            // band art is exactly what this whole detour (card_bg_2 +
            // banner + floating text) was trying to work around, but with a
            // static panel height it never stretches/distorts, so the plain
            // original approach is safe again.
            var header = UIFactory.CreateText(_root, "Header", "Modifiers", 30, UITheme.TextPrimary, TextAnchor.UpperCenter);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            header.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, 48f);

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            _rowsContainer.anchoredPosition = new Vector2(0f, -HeaderHeight);

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
