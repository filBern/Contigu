using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>The shop's back wall (dark wooden planks), for the full-screen views that should look like the same stall (the seed bag).</summary>
    public static class StallBackdrop
    {
        public static void AddPlanks(RectTransform root)
        {
            const int Planks = 16;
            for (int i = 0; i < Planks; i++)
            {
                var plank = UIFactory.CreatePanel(root, "Plank_" + i, i % 2 == 0 ? new Color(0.22f, 0.15f, 0.11f, 1f) : new Color(0.17f, 0.115f, 0.085f, 1f));
                plank.raycastTarget = false;
                plank.rectTransform.anchorMin = new Vector2(i / (float)Planks, 0f);
                plank.rectTransform.anchorMax = new Vector2((i + 1) / (float)Planks, 1f);
                plank.rectTransform.offsetMin = Vector2.zero;
                plank.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
