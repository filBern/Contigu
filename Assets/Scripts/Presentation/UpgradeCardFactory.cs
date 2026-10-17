using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Plain white text revealing an upgrade (name, rarity+pool,
    /// description) once a mystery shop slot is bought. Positioned and
    /// measured entirely by hand (no VerticalLayoutGroup/ContentSizeFitter,
    /// which only resolve on a later layout pass) so <see cref="Build"/>
    /// hands back the real total height synchronously (in the returned
    /// RectTransform's sizeDelta.y); callers use it to place whatever comes
    /// below without guessing at a fixed offset. Width stays fixed; the
    /// description wraps to more lines instead of wider ones.
    /// </summary>
    public static class UpgradeCardFactory
    {
        public const float Width = 560f;
        private const float NameHeight = 42f;
        private const float RarityHeight = 30f;
        private const float LineSpacing = 6f;
        private const float IconSize = 120f;

        /// <summary><paramref name="showRarity"/> defaults to true; UpgradeRevealView's Random Modifier reveal passes false to drop the rarity/pool line since it already shows the granted modifier as a full card right below.</summary>
        public static RectTransform Build(Transform parent, UpgradeDefinition def, bool showRarity = true)
        {
            var container = UIFactory.CreateUIObject("UpgradeReveal_" + def.Id, parent);
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);

            // The upgrade's wooden-sign icon on top (a tall signpost, instantly different from a fertilizer's sack).
            float iconBlock = 0f;
            var signIcon = UpgradeVisualDefaults.GetIcon(def.Id);
            if (signIcon != null)
            {
                var icon = UIFactory.CreatePanel(container, "Icon", Color.white);
                icon.sprite = signIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                icon.rectTransform.pivot = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
                iconBlock = IconSize + LineSpacing;
            }

            // No FontStyle.Bold: the Digitalt font has no true bold face, so Unity's legacy Text synthesizes one by double-drawing a shifted copy, which reads as blurry. Size alone carries the emphasis.
            var nameLabel = UIFactory.CreateText(container, "Name", def.Name, 33, Color.white);
            PositionRow(nameLabel, -iconBlock, NameHeight);

            float descY = -(iconBlock + NameHeight + LineSpacing);
            if (showRarity)
            {
                var rarityLabel = UIFactory.CreateText(container, "Rarity",
                    UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool),
                    21, Color.white);
                rarityLabel.fontStyle = FontStyle.Italic;
                PositionRow(rarityLabel, descY, RarityHeight);
                descY -= (RarityHeight + LineSpacing);
            }

            var descLabel = UIFactory.CreateText(container, "Desc", DescriptionTextFormatter.Colorize(def.Description, 23), 23, Color.white);
            float descHeight = PreferredHeight(descLabel, Width);
            PositionRow(descLabel, descY, descHeight);

            container.sizeDelta = new Vector2(Width, -descY + descHeight);
            return container;
        }

        private static void PositionRow(Text text, float y, float height)
        {
            text.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = new Vector2(0f, y);
            text.rectTransform.sizeDelta = new Vector2(Width, height);
        }

        private static float PreferredHeight(Text text, float width)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(text.text, settings);
        }
    }
}
