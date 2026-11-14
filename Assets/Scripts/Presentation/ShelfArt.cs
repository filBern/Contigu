using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>The shop's faux-3D touches: a wooden shelf seen from slightly above (top surface, lit front edge, darker underside, soft shadow on the wall) and the contact shadow under an item standing on it.</summary>
    public static class ShelfArt
    {
        private static readonly Color Surface = new Color(0.62f, 0.43f, 0.24f, 1f);
        private static readonly Color BackEdge = new Color(0.30f, 0.19f, 0.10f, 1f);
        private static readonly Color FrontHighlight = new Color(0.80f, 0.60f, 0.37f, 1f);
        private static readonly Color Underside = new Color(0.29f, 0.18f, 0.095f, 1f);
        private const float SurfaceDepth = 14f;

        /// <summary>Dresses a shelf board (an Image with its pivot at the top edge) as a 3D plank; the items stand on the top surface.</summary>
        public static void Decorate(RectTransform board)
        {
            // The top surface, running back from the board's front edge (behind the items standing on it).
            var surface = Bar(board, "Surface", Surface, 1f, 1f, SurfaceDepth, 0f, 0f);
            surface.rectTransform.pivot = new Vector2(0.5f, 0f);
            Bar(board, "BackEdge", BackEdge, 1f, 1f, 2.5f, SurfaceDepth - 2.5f, 0f).rectTransform.pivot = new Vector2(0.5f, 0f);
            // The front face: a lit edge on top, a darker band under it.
            Bar(board, "Lip", FrontHighlight, 1f, 1f, 4f, 0f, 1f);
            Bar(board, "Underside", Underside, 1f, 0f, 4f, 0f, 0f).rectTransform.pivot = new Vector2(0.5f, 0f);
            // A soft shadow on the wall under the shelf, in three steps (a gradient without an image).
            float[] heights = { 8f, 15f, 24f };
            float[] alphas = { 0.30f, 0.17f, 0.08f };
            for (int i = heights.Length - 1; i >= 0; i--)
            {
                var step = Bar(board, "Shadow" + i, new Color(0f, 0f, 0f, alphas[i]), 1f, 0f, heights[i], 0f, 0f);
                step.rectTransform.pivot = new Vector2(0.5f, 1f);
            }
        }

        private static Image Bar(RectTransform parent, string name, Color color, float width, float anchorY, float height, float offsetY, float pivotY)
        {
            var bar = UIFactory.CreatePanel(parent, name, color);
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = new Vector2(0f, anchorY);
            bar.rectTransform.anchorMax = new Vector2(width, anchorY);
            bar.rectTransform.pivot = new Vector2(0.5f, pivotY);
            bar.rectTransform.anchoredPosition = new Vector2(0f, offsetY);
            bar.rectTransform.sizeDelta = new Vector2(0f, height);
            return bar;
        }

        /// <summary>A flat, soft dark ellipse at the foot of an item standing on a shelf, so it sits on the wood instead of floating.</summary>
        public static void AddContactShadow(RectTransform item, float width, float height, float footOffset = 0f)
        {
            var shadow = UIFactory.CreatePanel(item, "ContactShadow", new Color(0f, 0f, 0f, 0.5f));
            shadow.raycastTarget = false;
            shadow.sprite = GrowthVisuals.FruitGlow; // a soft radial falloff, tinted black here
            shadow.enabled = shadow.sprite != null;
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, footOffset);
            shadow.rectTransform.sizeDelta = new Vector2(width, height);
            shadow.transform.SetAsFirstSibling();
        }
    }
}
