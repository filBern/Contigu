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
        private const float PanelWidth = 190f;
        private const float RowHeight = 64f;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;

        private RectTransform _root;
        private RectTransform _rowsContainer;

        // Parallel to the active-modifiers list passed to the last Refresh —
        // lets Pulse(id) find every row currently showing that modifier (there
        // can be more than one if the player holds duplicates).
        private readonly List<ModifierId> _rowIds = new List<ModifierId>();
        private readonly List<Text> _rowNameLabels = new List<Text>();

        public RectTransform Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "ModifierPanel", UITheme.Panel);
            _root = panel.rectTransform;
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, 640f);
            _root.anchoredPosition = new Vector2(10f, 0f);

            var header = UIFactory.CreateText(_root, "Header", "Modifiers", 15, UITheme.TextPrimary);
            header.fontStyle = FontStyle.Bold;
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            header.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, 24f);
            AddTextOutline(header);

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
            _rowNameLabels.Clear();

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var nameLabel = BuildRow(ModifierCatalog.Get(activeModifiers[i]));
                _rowIds.Add(activeModifiers[i]);
                _rowNameLabels.Add(nameLabel);
            }
        }

        private Text BuildRow(ModifierDefinition def)
        {
            var row = UIFactory.CreatePanel(_rowsContainer, "Row_" + def.Id, UITheme.PanelLight);
            row.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, RowHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent VerticalLayoutGroup collapses it toward zero.
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = PanelWidth - 16f;
            rowLayout.preferredHeight = RowHeight;
            // The row's own fill is close in luminance to UITheme.Modifier text
            // and to the panel behind it, so without a rim it reads as a
            // formless smudge — a dark outline gives the row a defined edge.
            var rowOutline = row.gameObject.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            rowOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var nameLabel = UIFactory.CreateText(row.transform, "Name", def.Name, 13, UITheme.TextPrimary);
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            nameLabel.rectTransform.sizeDelta = new Vector2(PanelWidth - 28f, 18f);
            AddTextOutline(nameLabel);

            var descLabel = UIFactory.CreateText(row.transform, "Desc", def.Description, 9, UITheme.TextPrimary);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            descLabel.rectTransform.sizeDelta = new Vector2(PanelWidth - 28f, 38f);
            AddTextOutline(descLabel);

            return nameLabel;
        }

        /// <summary>Flashes the name text (and gives its row a small scale pulse) of every row currently showing <paramref name="id"/> — called when that modifier actually scores on a placement.</summary>
        public void Pulse(ModifierId id)
        {
            for (int i = 0; i < _rowIds.Count; i++)
            {
                if (_rowIds[i] == id)
                {
                    StartCoroutine(PulseLabel(_rowNameLabels[i]));
                }
            }
        }

        private IEnumerator PulseLabel(Text label)
        {
            var baseColor = UITheme.TextPrimary;
            var highlightColor = UITheme.Modifier;
            var rt = label.rectTransform;
            float t = 0f;
            while (t < PulseDuration)
            {
                // The panel can be refreshed (rows destroyed/rebuilt) mid-pulse
                // if a new draft/round starts right as this plays — bail out
                // rather than touching a destroyed row.
                if (label == null)
                {
                    yield break;
                }

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                label.color = Color.Lerp(highlightColor, baseColor, p);
                yield return null;
            }

            if (label != null)
            {
                rt.localScale = Vector3.one;
                label.color = baseColor;
            }
        }

        private static void AddTextOutline(Text label)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
