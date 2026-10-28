using System;
using System.Collections.Generic;
using UnityEngine;
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
        /// <summary>Drawn smaller than a standard preview (mastery pack).</summary>
        public bool Compact;
        /// <summary>For a seed option: how many tiles wide the piece is (0 for the icon-like options), so the shelf can space the pieces evenly whatever their width.</summary>
        public int Columns;
        /// <summary>How many tiles tall the piece is, so it can stand on the shelf by its own bottom edge.</summary>
        public int Rows;
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
            var cell = PieceBox.CreateCell(_stage.RowFor(index), "Option_" + index, cellSize, false);
            float cellWidth = _stage.CellWidthFor(index);
            if (!Mathf.Approximately(cellWidth, cellSize))
            {
                cell.rectTransform.sizeDelta = new Vector2(cellWidth, cellSize);
                cell.GetComponent<LayoutElement>().preferredWidth = cellWidth;
            }
            _cellByIndex[index] = cell;
            var button = cell.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnCellClicked(index));

            var host = UIFactory.CreateUIObject("Preview", cell.transform);
            host.anchorMin = new Vector2(0.5f, 0.5f);
            host.anchorMax = new Vector2(0.5f, 0.5f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.anchoredPosition = Vector2.zero;
            float visualSize = entry.FillCell ? cellSize * 0.64f : cellSize * (entry.Compact ? 0.72f : 0.9f);
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
                previous.color = PieceBox.Idle;
                var previousTip = previous.GetComponent<HoverTooltip>();
                if (previousTip != null)
                {
                    previousTip.Unpin();
                }
            }
            _selectedIndex = index;
            _cellByIndex[index].color = PieceBox.Selected;
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
}
