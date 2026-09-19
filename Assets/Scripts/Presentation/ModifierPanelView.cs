using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers (up to <see cref="RunManager.MaxActiveModifiers"/>).
    /// A readout refreshed by the caller whenever the active set changes, plus
    /// a <see cref="Pulse"/> effect the caller triggers on a modifier's row
    /// whenever it actually scores points on a placement.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        private const float PanelWidth = 230f;
        private const float RowHeight = 64f;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;

        private RectTransform _root;
        private RectTransform _rowsContainer;
        private TooltipView _tooltip;

        // Parallel to the active-modifiers list passed to the last Refresh —
        // lets Pulse(id) find the row currently showing that modifier (each
        // modifier can only be active once per run, see
        // RunManager.RollModifierDraftOptions).
        private readonly List<ModifierId> _rowIds = new List<ModifierId>();
        private readonly List<Image> _rowBadges = new List<Image>();

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            var panel = UIFactory.CreateSlicedImage(parent, "ModifierPanel", UISprites.ModifierPanelBackground);
            _root = panel.rectTransform;
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, 460f);
            _root.anchoredPosition = new Vector2(40f, 0f);

            var header = UIFactory.CreateText(_root, "Header", "Modifiers", 15, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            header.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, 24f);

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            _rowsContainer.anchoredPosition = new Vector2(0f, -40f);

            var layout = _rowsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = _rowsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
                var badge = BuildRow(ModifierCatalog.Get(activeModifiers[i]));
                _rowIds.Add(activeModifiers[i]);
                _rowBadges.Add(badge);
            }
        }

        private Image BuildRow(ModifierDefinition def)
        {
            var row = UIFactory.CreateSlicedImage(_rowsContainer, "Row_" + def.Id, UISprites.ModifierCardBackground);
            row.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, RowHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent VerticalLayoutGroup collapses it toward zero.
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = PanelWidth - 16f;
            rowLayout.preferredHeight = RowHeight;
            // No Outline component here anymore — the card art already
            // carries its own edge/shadow, and a hard black outline on top
            // of it just looked muddy.

            var badge = ModifierBadgeFactory.Create(row.transform, def, RowHeight - 12f, _tooltip);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchoredPosition = Vector2.zero;

            return badge;
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
