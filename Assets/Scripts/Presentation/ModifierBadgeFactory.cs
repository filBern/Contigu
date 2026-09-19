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
    /// The Forme* "Specialist" modifiers are the one exception: instead of an
    /// abbreviation they show a literal black-square preview of the shape
    /// they target (see ModifierVisualDefaults.GetSpecialistShape), on
    /// explicit request — spelling out a domino/tromino/tetromino name read
    /// as unclear jargon.
    /// </summary>
    public static class ModifierBadgeFactory
    {
        /// <summary>
        /// <paramref name="usageCountProvider"/> is optional — when given, the
        /// badge's tooltip additionally shows how many times this modifier
        /// has fired this run (queried live on each hover, not baked in at
        /// creation time, since it keeps changing after the badge is built).
        /// Only the persistent side panel passes one; draft-card badges
        /// (modifiers not picked yet) leave it null and show no usage line.
        /// </summary>
        public static Image Create(Transform parent, ModifierDefinition def, float size, TooltipView tooltip, System.Func<ModifierId, int> usageCountProvider = null)
        {
            var badge = UIFactory.CreatePanel(parent, "Badge_" + def.Id, ModifierVisualDefaults.GetCategoryColor(def.Category));
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            var badgeOutline = badge.gameObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var specialistShape = ModifierVisualDefaults.GetSpecialistShape(def.Id);
            if (specialistShape.HasValue)
            {
                var preview = UIFactory.CreateUIObject("ShapePreview", badge.transform);
                preview.anchorMin = new Vector2(0.5f, 0.5f);
                preview.anchorMax = new Vector2(0.5f, 0.5f);
                preview.pivot = new Vector2(0.5f, 0.5f);
                preview.anchoredPosition = Vector2.zero;
                preview.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
                ShapePreviewFactory.BuildMono(preview, PieceShapeCatalog.Get(specialistShape.Value), Color.black);
            }
            else
            {
                var label = UIFactory.CreateText(badge.transform, "Label", ModifierVisualDefaults.GetAbbreviation(def.Id), Mathf.RoundToInt(size * 0.34f), UITheme.TextPrimary);
                label.raycastTarget = false;
                UIFactory.StretchFull(label.rectTransform);
            }

            var hover = badge.gameObject.AddComponent<ModifierBadgeView>();
            hover.Init(tooltip, def, usageCountProvider);

            return badge;
        }
    }
}
