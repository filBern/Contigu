using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Shared look for the seed pickers (PieceChoiceView, TileChoiceView, DraftView's type grid): every candidate sits in
    /// one big parchment box (like the hand's slots), large previews, no per-cell backgrounds, thin dividers between
    /// cells, selection shown as a soft tint behind the chosen cell.
    /// </summary>
    public static class PieceBox
    {
        public const float CellSize = 150f;
        public const float PreviewSize = 118f;
        public const float BoxPadding = 18f;
        public const float BoxHeight = CellSize + BoxPadding * 2f;

        public static readonly Color Idle = new Color(1f, 1f, 1f, 0f);
        public static readonly Color Selected = new Color(0.35f, 0.65f, 0.30f, 0.38f);
        private static readonly Color DividerColor = new Color(0.45f, 0.33f, 0.2f, 0.45f);

        /// <summary>Turns an (already laid out) horizontal container into the big parchment box.</summary>
        public static void Setup(RectTransform container)
        {
            var background = container.gameObject.AddComponent<Image>();
            background.sprite = UISprites.CardBackground;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset((int)BoxPadding, (int)BoxPadding, (int)BoxPadding, (int)BoxPadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        /// <summary>A transparent, clickable cell; a divider is added first when the box already holds a cell.</summary>
        public static Image CreateCell(RectTransform container, string name)
        {
            if (container.childCount > 0)
            {
                var divider = UIFactory.CreatePanel(container, "Divider", DividerColor);
                divider.raycastTarget = false;
                var dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
                dividerLayout.preferredWidth = 2f;
                dividerLayout.preferredHeight = CellSize * 0.7f;
            }
            var cell = UIFactory.CreatePanel(container, name, Idle);
            cell.rectTransform.sizeDelta = new Vector2(CellSize, CellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = CellSize;
            cellLayout.preferredHeight = CellSize;
            return cell;
        }
    }
}
