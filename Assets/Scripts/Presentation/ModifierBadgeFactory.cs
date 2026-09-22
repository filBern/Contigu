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
    /// A modifier with real icon art (see ModifierVisualDefaults.GetIcon)
    /// shows that instead, taking priority over everything else below. Absent
    /// that, two exceptions, both on explicit request: the Forme* "Specialist"
    /// modifiers show a literal black-square preview of the shape they
    /// target (see ModifierVisualDefaults.GetSpecialistShape) instead of an
    /// abbreviation — spelling out a domino/tromino/tetromino name read as
    /// unclear jargon — and the "Glow" (Éclat) and "Devotion" per-color
    /// modifiers show an actual colored tile in their own color (see
    /// ModifierVisualDefaults.GetColorTileColor) instead of their opaque
    /// 2-letter code, for the same reason.
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
        /// <paramref name="showBackground"/> defaults to true (the usual
        /// category-colored chip + outline); the shop's own modifier cards
        /// pass false to show just the bare icon/preview/abbreviation
        /// instead, on explicit request ("Peux-tu enlever le carré coloré
        /// derrière l'icon aussi?" — the card now carries the name/
        /// description as its own text, so the chip read as redundant).
        /// <paramref name="attachTooltip"/> defaults to true; the shop's
        /// modifier cards pass false to skip it entirely, on explicit
        /// request ("Pas besoin du tooltip sur les modifiers qu'on peut
        /// acheter dans le shop, seulement dans notre liste de modifiers
        /// possédé") — a shop card already shows its own name/description
        /// as static text, so a hover tooltip there was pure redundancy;
        /// only the persistent side panel (badges with no text of their
        /// own) still needs it. <paramref name="progressiveStateProvider"/>
        /// mirrors <paramref name="usageCountProvider"/> — only the side
        /// panel passes one, so a progressive/incremental modifier's
        /// tooltip can show its current live state (see
        /// RunManager.GetProgressiveModifierStateText).
        /// </summary>
        public static Image Create(Transform parent, ModifierDefinition def, float size, TooltipView tooltip, System.Func<ModifierId, int> usageCountProvider = null, bool showBackground = true, bool attachTooltip = true, System.Func<ModifierId, string> progressiveStateProvider = null)
        {
            var badge = UIFactory.CreatePanel(parent, "Badge_" + def.Id, showBackground ? ModifierVisualDefaults.GetCategoryColor(def.Category) : Color.clear);
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            if (showBackground)
            {
                var badgeOutline = badge.gameObject.AddComponent<Outline>();
                badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var icon = ModifierVisualDefaults.GetIcon(def.Id);
            var specialistShape = ModifierVisualDefaults.GetSpecialistShape(def.Id);
            var colorTileColor = ModifierVisualDefaults.GetColorTileColor(def.Id);
            if (icon != null)
            {
                var iconImage = UIFactory.CreatePanel(badge.transform, "Icon", Color.white);
                iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.anchoredPosition = Vector2.zero;
                iconImage.rectTransform.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
                iconImage.sprite = icon;
            }
            else if (specialistShape.HasValue)
            {
                var preview = UIFactory.CreateUIObject("ShapePreview", badge.transform);
                preview.anchorMin = new Vector2(0.5f, 0.5f);
                preview.anchorMax = new Vector2(0.5f, 0.5f);
                preview.pivot = new Vector2(0.5f, 0.5f);
                preview.anchoredPosition = Vector2.zero;
                preview.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
                ShapePreviewFactory.BuildMono(preview, PieceShapeCatalog.Get(specialistShape.Value), Color.black);
            }
            else if (colorTileColor.HasValue)
            {
                var preview = UIFactory.CreateUIObject("TilePreview", badge.transform);
                preview.anchorMin = new Vector2(0.5f, 0.5f);
                preview.anchorMax = new Vector2(0.5f, 0.5f);
                preview.pivot = new Vector2(0.5f, 0.5f);
                preview.anchoredPosition = Vector2.zero;
                preview.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
                ShapePreviewFactory.Build(preview, PieceShapeCatalog.Get(ShapeId.Single), colorTileColor.Value, null, tooltip, badge.gameObject);
            }
            else
            {
                var label = UIFactory.CreateText(badge.transform, "Label", ModifierVisualDefaults.GetAbbreviation(def.Id), Mathf.RoundToInt(size * 0.34f), UITheme.TextPrimary);
                label.raycastTarget = false;
                UIFactory.StretchFull(label.rectTransform);
            }

            if (attachTooltip)
            {
                var hover = badge.gameObject.AddComponent<ModifierBadgeView>();
                hover.Init(tooltip, def, usageCountProvider, progressiveStateProvider);
            }

            return badge;
        }
    }
}
