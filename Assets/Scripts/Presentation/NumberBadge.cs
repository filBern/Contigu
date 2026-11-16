using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>The small round number on a hand slot or a fertilizer: a golden bed around dark soil, the number in gold lettering (the same recipe as the plot buttons).</summary>
    public static class NumberBadge
    {
        private static readonly Color Gold = UITheme.Ink; // ink digit on the yellow crayon badge

        /// <summary>Builds the badge as a child of <paramref name="parent"/>; the caller places it.</summary>
        public static RectTransform Create(Transform parent, string number, float size, int fontSize)
        {
            var badge = UIFactory.CreatePanel(parent, "NumberBadge", Color.white);
            badge.raycastTarget = false;
            badge.sprite = Resources.Load<Sprite>("Buttons/Plot/SlotBadge");
            badge.preserveAspect = true;
            badge.rectTransform.sizeDelta = new Vector2(size, size);
            UIFactory.AddDropShadow(badge, 2.5f, 0.4f);
            var label = UIFactory.CreateText(badge.transform, "Number", number, fontSize, Gold);
            label.raycastTarget = false;
            UIFactory.StretchFull(label.rectTransform);
            return badge.rectTransform;
        }
    }
}
