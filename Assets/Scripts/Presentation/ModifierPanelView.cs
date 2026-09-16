using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers (up to <see cref="RunManager.MaxActiveModifiers"/>).
    /// Purely a readout — refreshed by the caller whenever the active set changes.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        private const float PanelWidth = 190f;
        private const float RowHeight = 64f;

        private RectTransform _root;
        private RectTransform _rowsContainer;

        public RectTransform Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "ModifierPanel", UITheme.Panel);
            _root = panel.rectTransform;
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, 640f);
            _root.anchoredPosition = new Vector2(10f, 0f);

            var header = UIFactory.CreateText(_root, "Header", "Modificateurs", 15, UITheme.Modifier);
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

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                BuildRow(ModifierCatalog.Get(activeModifiers[i]));
            }
        }

        private void BuildRow(ModifierDefinition def)
        {
            var row = UIFactory.CreatePanel(_rowsContainer, "Row_" + def.Id, UITheme.PanelLight);
            row.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, RowHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // the parent VerticalLayoutGroup collapses it toward zero.
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = PanelWidth - 16f;
            rowLayout.preferredHeight = RowHeight;

            var nameLabel = UIFactory.CreateText(row.transform, "Name", def.Name, 13, UITheme.Modifier);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            nameLabel.rectTransform.sizeDelta = new Vector2(PanelWidth - 28f, 18f);

            var descLabel = UIFactory.CreateText(row.transform, "Desc", def.Description, 9, UITheme.TextMuted);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            descLabel.rectTransform.sizeDelta = new Vector2(PanelWidth - 28f, 38f);
        }
    }
}
