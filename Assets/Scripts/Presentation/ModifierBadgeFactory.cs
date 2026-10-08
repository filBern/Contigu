using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Builds one modifier badge (a category-colored chip showing its 2-letter abbreviation, wired to reveal
    /// a hover tooltip with the full name/description) — shared by the modifier draft cards and the
    /// persistent modifier side panel. A modifier with real icon art (see ModifierVisualDefaults.GetIcon)
    /// shows that instead; absent that, per-color modifiers like "Glow"/"Devotion" show an actual colored
    /// tile (see ModifierVisualDefaults.GetColorTileColor) instead of the 2-letter code.
    /// </summary>
    public static class ModifierBadgeFactory
    {
        /// <summary>
        /// <paramref name="usageCountProvider"/> is optional — when given, the badge's tooltip additionally
        /// shows how many times this modifier has fired this run, queried live on each hover. Only the
        /// persistent side panel passes one. <paramref name="showBackground"/> defaults to true; the shop's
        /// modifier cards pass false since their own card text makes the chip redundant.
        /// <paramref name="attachTooltip"/> defaults to true; the shop's cards pass false since they already
        /// show name/description as static text. <paramref name="progressiveStateProvider"/> and
        /// <paramref name="levelStateProvider"/> let the side panel's tooltip show a modifier's current live
        /// or leveled state instead of its static Description. <paramref name="showSellValue"/> defaults to
        /// false; only the side panel passes true, since only modifiers in that list are sellable.
        /// </summary>
        public static Image Create(Transform parent, ModifierDefinition def, float size, TooltipView tooltip, System.Func<ModifierId, int> usageCountProvider = null, bool showBackground = true, bool attachTooltip = true, System.Func<ModifierId, string> progressiveStateProvider = null, System.Func<ModifierId, string> levelStateProvider = null, bool showSellValue = false)
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
                hover.Init(tooltip, def, usageCountProvider, progressiveStateProvider, levelStateProvider, showSellValue);
            }

            return badge;
        }
    }
}
