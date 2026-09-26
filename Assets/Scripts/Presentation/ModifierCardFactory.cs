using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Builds one modifier's card contents (name, bare icon, description) onto
    /// an already-created card background — the shared visual ShopView's own
    /// modifier slots use (background/outline/buy button stay the caller's
    /// responsibility, since only the shop needs those). Factored out so
    /// UpgradeRevealView's "you got a Random Modifier" reveal can show the
    /// granted modifier the same way instead of the bare text label it used
    /// before (explicit request: "tu peux afficher comme une carte du shop").
    /// </summary>
    public static class ModifierCardFactory
    {
        public const float TopPadding = 10f;
        public const float NameHeight = 26f;
        public const float Gap = 6f;
        public const float BadgeSize = 90f;
        private const int NameFontSize = 16;
        private const int DescFontSize = 12;

        /// <summary>Adds the name label, bare badge, and description text onto <paramref name="cardTransform"/> at <paramref name="width"/>, and returns the description's own natural (unclamped) preferred height — same contract ShopView.BuildModifierCard used before this was extracted, so a caller placing several cards in a row can still sync them to a shared max height. <paramref name="badgeSize"/>/<paramref name="nameHeight"/>/<paramref name="nameFontSize"/>/<paramref name="descFontSize"/> default to the shop's own card sizing; UpgradeRevealView's Random Modifier reveal — the only place showing one modifier card alone rather than a row of several — passes bigger values (explicit request: "grossir la carte modifier dans cet écran là").</summary>
        public static float BuildContents(Transform cardTransform, ModifierDefinition def, TooltipView tooltip, float width, out RectTransform descRect,
            float badgeSize = BadgeSize, float nameHeight = NameHeight, int nameFontSize = NameFontSize, int descFontSize = DescFontSize)
        {
            var nameLabel = UIFactory.CreateText(cardTransform, "Name", def.Name, nameFontSize, UITheme.TextPrimary);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, -TopPadding);
            nameLabel.rectTransform.sizeDelta = new Vector2(width - 16f, nameHeight);

            var badge = ModifierBadgeFactory.Create(cardTransform, def, badgeSize, tooltip, showBackground: false, attachTooltip: false);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            badge.rectTransform.pivot = new Vector2(0.5f, 1f);
            float badgeY = -(TopPadding + nameHeight + Gap);
            badge.rectTransform.anchoredPosition = new Vector2(0f, badgeY);

            float descWidth = width - 16f;
            var descLabel = UIFactory.CreateText(cardTransform, "Desc", DescriptionTextFormatter.Colorize(def.Description), descFontSize, UITheme.TextPrimary);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, badgeY - (badgeSize + Gap));
            descLabel.rectTransform.sizeDelta = new Vector2(descWidth, 0f);
            descRect = descLabel.rectTransform;

            return PreferredHeight(descLabel, descWidth);
        }

        /// <summary>Total card height (background + outline sizeDelta) for a card whose description ended up <paramref name="descHeight"/> tall — top padding, name, badge and description stacked, plus the same top padding again as bottom margin. <paramref name="badgeSize"/>/<paramref name="nameHeight"/> must match whatever was passed to <see cref="BuildContents"/>.</summary>
        public static float TotalHeight(float descHeight, float badgeSize = BadgeSize, float nameHeight = NameHeight)
        {
            return TopPadding + nameHeight + Gap + badgeSize + Gap + descHeight + TopPadding;
        }

        private static float PreferredHeight(Text text, float width)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(text.text, settings);
        }
    }
}
