using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Shared look for the seed pickers (PieceChoiceView, TileChoiceView, DraftView's type grid): every candidate sits in
    /// one big parchment box (like the hand's slots), large previews, no per-cell backgrounds, thin dividers between
    /// cells, a soft tint on hover and on the chosen cell. The box shrinks its cells with the candidate count so it always
    /// fits between the side panels (fertilizers / next round) that stay visible next to it.
    /// </summary>
    public static class PieceBox
    {
        private const float MaxCellSize = 150f;
        private const float MinCellSize = 84f;
        private const float BoxPadding = 18f;
        private const float DividerWidth = 2f;
        private const float SideGap = 20f;
        // Width left between the fertilizer and next-round panels, in canvas units (see SetSidePanelCenterX).
        private static float _availableWidth = 620f;

        /// <summary>The two side panels sit at +/-<paramref name="sidePanelCenterX"/>, each <paramref name="panelWidth"/> wide; the box may use what is left between them.</summary>
        public static void SetSidePanelCenterX(float sidePanelCenterX, float panelWidth)
        {
            SidePanelCenterX = sidePanelCenterX;
            _availableWidth = Mathf.Max(300f, 2f * sidePanelCenterX - panelWidth - 2f * SideGap);
        }

        /// <summary>Distance of each side panel's center from the screen center (the seeds can also be displayed on boards at the panels' place).</summary>
        public static float SidePanelCenterX { get; private set; } = 355f;

        public static float CellSize { get; private set; } = MaxCellSize;
        public static float PreviewSize { get { return CellSize * 0.8f; } }
        public static float BoxHeight { get { return CellSize + BoxPadding * 2f; } }

        public static readonly Color Idle = new Color(1f, 1f, 1f, 0f);
        public static readonly Color Hover = new Color(0.35f, 0.65f, 0.30f, 0.16f);
        public static readonly Color Selected = new Color(0.35f, 0.65f, 0.30f, 0.38f);
        private static readonly Color DividerColor = new Color(0.45f, 0.33f, 0.2f, 0.45f);

        /// <summary>Sizes the cells for <paramref name="count"/> candidates; call before building the cells and measuring the block.</summary>
        public static void Configure(int count)
        {
            int n = Mathf.Max(1, count);
            float usable = _availableWidth - BoxPadding * 2f - (n - 1) * DividerWidth;
            CellSize = Mathf.Clamp(usable / n, MinCellSize, MaxCellSize);
        }

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

        /// <summary>A transparent, clickable cell with hover feedback; a divider is added first when the box already holds a cell.</summary>
        public static Image CreateCell(RectTransform container, string name)
        {
            return CreateCell(container, name, CellSize, true);
        }

        /// <summary>A cell of an explicit size, with or without a leading divider (a side board's grid has none).</summary>
        public static Image CreateCell(RectTransform container, string name, float cellSize, bool withDivider)
        {
            if (withDivider && container.childCount > 0)
            {
                var divider = UIFactory.CreatePanel(container, "Divider", DividerColor);
                divider.raycastTarget = false;
                var dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
                dividerLayout.preferredWidth = DividerWidth;
                dividerLayout.preferredHeight = cellSize * 0.7f;
            }
            var cell = UIFactory.CreatePanel(container, name, Idle);
            cell.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = cellSize;
            cellLayout.preferredHeight = cellSize;
            cell.gameObject.AddComponent<PieceBoxCellHover>();
            return cell;
        }
    }

    /// <summary>Hover feedback for a PieceBox cell: a faint tint (only while the cell is not selected) and a slight grow of its preview.</summary>
    public sealed class PieceBoxCellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.08f;
        private Image _cell;
        private bool _hovered;

        private void Awake()
        {
            _cell = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (_cell != null && _cell.color == PieceBox.Idle)
            {
                _cell.color = PieceBox.Hover;
            }
            SetPreviewScale(HoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            if (_cell != null && _cell.color == PieceBox.Hover)
            {
                _cell.color = PieceBox.Idle;
            }
            SetPreviewScale(1f);
        }

        private void OnDisable()
        {
            if (_hovered)
            {
                _hovered = false;
                SetPreviewScale(1f);
            }
        }

        private void SetPreviewScale(float scale)
        {
            var preview = transform.Find("Preview");
            if (preview != null)
            {
                preview.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
