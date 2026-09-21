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
    /// description en texte blanc"). Positioned and measured entirely by
    /// hand (no VerticalLayoutGroup/ContentSizeFitter, which only resolve
    /// on a later layout pass) so <see cref="Build"/> hands back the real
    /// total height synchronously (in the returned RectTransform's own
    /// sizeDelta.y) — callers use it to place whatever comes below without
    /// guessing at a fixed offset, since descriptions vary a lot in length
    /// (~190 characters at the longest) and text sizes have grown since the
    /// first version of this (see below). Text sizes are 1.5x that first
    /// version's (explicit request); Width stays fixed rather than growing
    /// with them, on the same request ("le texte de la description ne soit
    /// pas trop large") — the description just wraps to more lines instead
    /// of wider ones.
    /// </summary>
    public static class UpgradeCardFactory
    {
        public const float Width = 560f;
        private const float NameHeight = 42f;
        private const float RarityHeight = 30f;
        private const float LineSpacing = 6f;

        public static RectTransform Build(Transform parent, UpgradeDefinition def)
        {
            var container = UIFactory.CreateUIObject("UpgradeReveal_" + def.Id, parent);
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);

            // No FontStyle.Bold here (used nowhere else in the codebase) —
            // the Digitalt font has no true bold face, so Unity's legacy
            // Text synthesizes one by double-drawing a shifted copy, which
            // is what was actually making the name read as blurry rather
            // than bold. Size alone (already the largest of the three
            // lines) carries the emphasis instead, same convention as
            // every other label in the game.
            var nameLabel = UIFactory.CreateText(container, "Name", def.Name, 33, Color.white);
            PositionRow(nameLabel, 0f, NameHeight);

            var rarityLabel = UIFactory.CreateText(container, "Rarity",
                UpgradeVisualDefaults.GetRarityLabel(def.Rarity) + " · " + UpgradeVisualDefaults.GetPoolLabel(def.Pool),
                21, Color.white);
            rarityLabel.fontStyle = FontStyle.Italic;
            float rarityY = -(NameHeight + LineSpacing);
            PositionRow(rarityLabel, rarityY, RarityHeight);

            var descLabel = UIFactory.CreateText(container, "Desc", def.Description, 23, Color.white);
            float descY = rarityY - (RarityHeight + LineSpacing);
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
