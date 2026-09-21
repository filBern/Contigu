using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The upgrade "card" visual from the old round-end draft (name banner,
    /// rarity+pool subtitle, description) — reused as the reveal moment for
    /// a shop upgrade slot once the specific UpgradeDefinition underneath is
    /// uncovered (explicit request: seeing just a name wasn't enough to
    /// understand the upgrade, and this old card visual read well). No
    /// Choose button here, unlike the original — the shop's own purchase
    /// flow, not this card, is what a slot gets bought through.
    /// </summary>
    public static class UpgradeCardFactory
    {
        public const float CardWidth = 200f;
        public const float CardHeight = 234f;

        public static RectTransform Build(Transform parent, UpgradeDefinition def)
        {
            var card = UIFactory.CreateSlicedImage(parent, "Card_" + def.Id, UISprites.UpgradeCardBackground);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            // Plain Image has no ILayoutElement, so pin the size explicitly or
            // a parent layout group collapses it toward zero.
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;

            var nameBanner = UIFactory.CreateSlicedImage(card.transform, "NameBanner", UISprites.UpgradeNameBanner);
            nameBanner.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameBanner.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            nameBanner.rectTransform.sizeDelta = new Vector2(CardWidth - 12f, 40f);

            var nameLabel = UIFactory.CreateText(nameBanner.transform, "Name", def.Name, 18, UITheme.TextPrimary);
            nameLabel.raycastTarget = false;
            UIFactory.StretchFull(nameLabel.rectTransform);

            // GetRarityColorOnLight (not GetRarityColor) — the dark-panel
            // rarity colors read as near-invisible pale-on-pale against this
            // card's light lavender art.
            var rarityLabel = UIFactory.CreateText(card.transform, "Rarity",
                UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool),
                14, UpgradeVisualDefaults.GetRarityColorOnLight(def.Rarity));
            rarityLabel.fontStyle = FontStyle.Italic;
            rarityLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            rarityLabel.rectTransform.anchoredPosition = new Vector2(0f, -52f);
            rarityLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, 24f);

            var descLabel = UIFactory.CreateText(card.transform, "Desc", def.Description, 13, Color.black);
            descLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            descLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            descLabel.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            descLabel.rectTransform.sizeDelta = new Vector2(CardWidth - 16f, CardHeight - 90f);

            return card.rectTransform;
        }
    }
}
