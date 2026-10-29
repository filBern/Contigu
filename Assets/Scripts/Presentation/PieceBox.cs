using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Cell sizing and creation shared by the shelf choices (ShelfChoiceView, TileChoiceView): the cells shrink with the number of options
    /// on a row so they always fit between the side panels, and each cell is a transparent, clickable square (the shelf is the only backdrop;
    /// hover and selection are drawn by ShelfCellFeedback).
    /// </summary>
    public static class PieceBox
    {
        private const float MaxCellSize = 150f;
        private const float MinCellSize = 84f;
        private const float BoxPadding = 18f;

        public static float CellSize { get; private set; } = MaxCellSize;

        /// <summary>Sizes the cells for <paramref name="count"/> candidates on one row within <paramref name="availableWidth"/>, never above <paramref name="maxCellSize"/> (a few options on a shelf must not balloon).</summary>
        public static void Configure(int count, float availableWidth, float maxCellSize)
        {
            int n = Mathf.Max(1, count);
            float usable = availableWidth - BoxPadding * 2f;
            CellSize = Mathf.Clamp(usable / n, MinCellSize, Mathf.Max(MinCellSize, maxCellSize));
        }

        /// <summary>A transparent, clickable cell of an explicit size.</summary>
        public static Image CreateCell(RectTransform container, string name, float cellSize)
        {
            var cell = UIFactory.CreatePanel(container, name, new Color(1f, 1f, 1f, 0f));
            cell.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = cellSize;
            cellLayout.preferredHeight = cellSize;
            return cell;
        }
    }
}
