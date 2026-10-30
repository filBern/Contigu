using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>One option of a shelf choice: how to draw it, and its tooltip.</summary>
    public struct ChoiceEntry
    {
        /// <summary>Fills the given square host (second argument: its side in canvas units).</summary>
        public Action<RectTransform, float> BuildVisual;
        public string Name;
        public string Body;
        public string Subtitle;
        /// <summary>Icon-like options (fertilizers, upgrade glyphs, masteries) fill their whole cell and may use bigger cells than seed previews.</summary>
        public bool FillCell;
        /// <summary>For a seed option: how many tiles wide the piece is (0 for the icon-like options), so the shelf can space the pieces evenly whatever their width.</summary>
        public int Columns;
        /// <summary>How many tiles tall the piece is, so it can stand on the shelf by its own bottom edge.</summary>
        public int Rows;
        /// <summary>The option goes well with the fertilizers held: it wears a star.</summary>
        public bool Fits;
    }

    /// <summary>
    /// Pack openings, Seed Pack, Mastery and Compost on the shop screen: the options stand on the shop's own shelves (see <see cref="ShelfStage"/>).
    /// Picking an option either resolves at once (a pack: the choice is already informed) or selects it for Confirm (a seed to add or remove).
    /// </summary>
    public sealed class ShelfChoiceView : MonoBehaviour
    {
        private TooltipView _tooltip;
        private RectTransform _root;
        private ShelfStage _stage;
        private ShelfLayout _layout;
        private Action<int> _onChosen;
        private bool _needsConfirm;
        private int _selectedIndex = -1;
        private readonly Dictionary<int, Image> _cellByIndex = new Dictionary<int, Image>();

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            var overlay = UIFactory.CreatePanel(parent, "ShelfChoiceOverlay", new Color(0f, 0f, 0f, 0f));
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);
            _stage = new ShelfStage(_root, tooltip);
            _stage.ConfirmButton.onClick.AddListener(OnConfirmClicked);
            _root.gameObject.SetActive(false);
            return _root;
        }

        public void SetShelfLayout(ShelfLayout layout)
        {
            _layout = layout;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Show(ChoiceSource source, string title, IReadOnlyList<ChoiceEntry> entries, bool needsConfirm, Action<int> onChosen)
        {
            _onChosen = onChosen;
            _needsConfirm = needsConfirm;
            _selectedIndex = -1;
            _cellByIndex.Clear();
            bool anyFill = false;
            for (int i = 0; i < entries.Count; i++)
            {
                anyFill |= entries[i].FillCell;
            }
            var columns = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                columns.Add(entries[i].Columns);
            }
            _stage.Begin(_layout, source, title, entries.Count, needsConfirm, anyFill ? ShelfStage.IconCellSize : ShelfStage.SeedCellSize, columns);
            float cell = _stage.CellSize;
            ShapePreviewFactory.TileOverride = _stage.Tile;
            for (int i = 0; i < entries.Count; i++)
            {
                BuildCell(i, entries[i], cell);
            }
            ShapePreviewFactory.TileOverride = 0f;
            _stage.ConfirmButton.interactable = false;
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        private void BuildCell(int index, ChoiceEntry entry, float cellSize)
        {
            var cell = PieceBox.CreateCell(_stage.RowFor(index), "Option_" + index, cellSize);
            float cellWidth = _stage.CellWidthFor(index);
            if (!Mathf.Approximately(cellWidth, cellSize))
            {
                cell.rectTransform.sizeDelta = new Vector2(cellWidth, cellSize);
                cell.GetComponent<LayoutElement>().preferredWidth = cellWidth;
            }
            _cellByIndex[index] = cell;
            // On the shelf there is no tinted box: hover lifts the piece a little, the chosen one lifts higher and glows on the shelf.
            cell.gameObject.AddComponent<ShelfCellFeedback>();
            var button = cell.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnCellClicked(index));

            var host = UIFactory.CreateUIObject("Preview", cell.transform);
            host.anchorMin = new Vector2(0.5f, 0.5f);
            host.anchorMax = new Vector2(0.5f, 0.5f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.anchoredPosition = Vector2.zero;
            float visualSize = entry.FillCell ? cellSize * 0.64f : cellSize * 0.9f;
            host.sizeDelta = new Vector2(visualSize, visualSize);
            if (entry.Columns > 0 && _stage.Tile > 0f)
            {
                // A piece stands on the shelf by its own bottom edge, whatever its height.
                host.anchorMin = host.anchorMax = new Vector2(0.5f, 0f);
                host.pivot = new Vector2(0.5f, 0f);
                host.anchoredPosition = new Vector2(0f, 2f);
                host.sizeDelta = new Vector2(entry.Columns * _stage.Tile, Mathf.Max(1, entry.Rows) * _stage.Tile);
            }
            if (entry.BuildVisual != null)
            {
                entry.BuildVisual(host, visualSize);
            }
            if (entry.Fits)
            {
                var star = UIFactory.CreateText(host, "FitsStar", "*", 40, VisualDefaults.GoldenColor);
                star.raycastTarget = false;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(1f, 1f);
                star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                star.rectTransform.anchoredPosition = new Vector2(-6f, -6f);
                star.rectTransform.sizeDelta = new Vector2(40f, 40f);
            }
            if (!string.IsNullOrEmpty(entry.Name))
            {
                var hover = cell.gameObject.AddComponent<HoverTooltip>();
                hover.Init(_tooltip, entry.Name, entry.Body, entry.Subtitle, UITheme.TextMuted);
            }
        }

        private void OnCellClicked(int index)
        {
            if (!_needsConfirm)
            {
                Resolve(index);
                return;
            }
            if (_selectedIndex >= 0 && _cellByIndex.TryGetValue(_selectedIndex, out var previous))
            {
                previous.GetComponent<ShelfCellFeedback>().SetSelected(false);
                var previousTip = previous.GetComponent<HoverTooltip>();
                if (previousTip != null)
                {
                    previousTip.Unpin();
                }
            }
            _selectedIndex = index;
            _cellByIndex[index].GetComponent<ShelfCellFeedback>().SetSelected(true);
            // Touch has no hover: the chosen option keeps its tooltip up, so it can be read before Confirm.
            var tip = _cellByIndex[index].GetComponent<HoverTooltip>();
            if (tip != null && MobileSetup.IsTouchDevice)
            {
                tip.Pin(null);
            }
            _stage.ConfirmButton.interactable = true;
        }

        private void OnConfirmClicked()
        {
            if (_selectedIndex >= 0)
            {
                Resolve(_selectedIndex);
            }
        }

        private void Resolve(int index)
        {
            _root.gameObject.SetActive(false);
            if (_onChosen != null)
            {
                _onChosen(index);
            }
        }
    }
    /// <summary>Feedback of a shelf option: the preview lifts and grows slightly on mouse hover, more when chosen, and a warm bar glows on the shelf under the chosen one.</summary>
    public sealed class ShelfCellFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverLift = 6f;
        private const float SelectedLift = 14f;
        private const float HoverScale = 1.04f;
        private const float SelectedScale = 1.08f;
        private const float Speed = 14f;
        private static readonly Color GlowColor = new Color(1f, 0.82f, 0.35f, 0.95f);
        private RectTransform _preview;
        private Image _glow;
        private bool _hovered;
        private bool _selected;
        private float _t;
        private float _baseY;

        public void SetSelected(bool selected)
        {
            _selected = selected;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = eventData.pointerId < 0; // touch has no hover
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void OnDisable()
        {
            _hovered = false;
        }

        private void Update()
        {
            if (_preview == null)
            {
                var found = transform.Find("Preview");
                if (found == null)
                {
                    return;
                }
                _preview = (RectTransform)found;
                _baseY = _preview.anchoredPosition.y;
                _glow = UIFactory.CreatePanel(transform, "Glow", GlowColor);
                _glow.raycastTarget = false;
                var rect = _glow.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(-6f, -5f);
                rect.offsetMax = new Vector2(6f, 1f);
            }
            float target = _selected ? 2f : _hovered ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, target, Speed * Time.unscaledDeltaTime);
            float lift = _t <= 1f ? Mathf.Lerp(0f, HoverLift, _t) : Mathf.Lerp(HoverLift, SelectedLift, _t - 1f);
            float scale = _t <= 1f ? Mathf.Lerp(1f, HoverScale, _t) : Mathf.Lerp(HoverScale, SelectedScale, _t - 1f);
            _preview.anchoredPosition = new Vector2(_preview.anchoredPosition.x, _baseY + lift);
            _preview.localScale = new Vector3(scale, scale, 1f);
            var c = GlowColor;
            c.a = GlowColor.a * Mathf.Clamp01(_t - 1f);
            _glow.color = c;
        }
    }
}
