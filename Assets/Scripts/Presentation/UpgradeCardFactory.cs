using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Plain white text revealing an upgrade (name, rarity+pool,
    /// description) once a mystery shop slot is bought — replaces the
    /// bordered card visual this used to build, which ran a fixed 234px tall
    /// regardless of content and ran off the top of the screen on top of
    /// the shop content still showing behind it (explicit request: "c'est
    /// trop gros comme écran, au lieu d'une carte on va juste mettre la
    /// description en texte blanc"). No background image or banner sprite,
    /// so the block's actual height is just whatever the text needs —
    /// computed via Text.cachedTextGenerator rather than guessed, since
    /// descriptions vary a lot in length (~190 characters at the longest).
    /// </summary>
    public static class UpgradeCardFactory
    {
        public const float Width = 560f;

        public static RectTransform Build(Transform parent, UpgradeDefinition def)
        {
            var container = UIFactory.CreateUIObject("UpgradeReveal_" + def.Id, parent);
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 4f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            container.sizeDelta = new Vector2(Width, 0f);

            var nameLabel = UIFactory.CreateText(container, "Name", def.Name, 22, Color.white);
            nameLabel.fontStyle = FontStyle.Bold;
            PinSize(nameLabel, Width, 28f);

            var rarityLabel = UIFactory.CreateText(container, "Rarity",
                UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool),
                14, Color.white);
            rarityLabel.fontStyle = FontStyle.Italic;
            PinSize(rarityLabel, Width, 20f);

            var descLabel = UIFactory.CreateText(container, "Desc", def.Description, 15, Color.white);
            PinSize(descLabel, Width, PreferredHeight(descLabel, Width));

            return container;
        }

        // Plain Text has no ILayoutElement, so a parent VerticalLayoutGroup
        // would otherwise collapse each line toward zero width/height.
        private static void PinSize(Text text, float width, float height)
        {
            var layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }

        private static float PreferredHeight(Text text, float width)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(text.text, settings);
        }
    }
}
