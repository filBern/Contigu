using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Builds one modifier badge (a category-colored chip showing its 2-letter
    /// abbreviation, wired to reveal a hover tooltip with the full name/
    /// description) — shared by the modifier draft cards and the persistent
    /// modifier side panel so both stay visually and behaviorally consistent.
    /// </summary>
    public static class ModifierBadgeFactory
    {
        public static Image Create(Transform parent, ModifierDefinition def, float size, TooltipView tooltip)
        {
            var badge = UIFactory.CreatePanel(parent, "Badge_" + def.Id, ModifierVisualDefaults.GetCategoryColor(def.Category));
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            var badgeOutline = badge.gameObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var label = UIFactory.CreateText(badge.transform, "Label", ModifierVisualDefaults.GetAbbreviation(def.Id), Mathf.RoundToInt(size * 0.34f), UITheme.TextPrimary);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            UIFactory.StretchFull(label.rectTransform);
            var labelOutline = label.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var hover = badge.gameObject.AddComponent<ModifierBadgeView>();
            hover.Init(tooltip, def);

            return badge;
        }
    }
}
