using System;
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Overlay shown once a shop upgrade purchase reveals "Modifier Upgrade".
    /// The player picks one of their currently active modifiers (every owned
    /// modifier is eligible, see RunManager.BuyUpgradeSlot) to raise its
    /// level (see ModifierLevelUtility). Structurally the same measured,
    /// centered card+title+picker+Confirm block as PieceChoiceView/
    /// TileChoiceView, but the picker is a multi-row grid (up to
    /// EconomyConstants.MaxActiveModifiers candidates) instead of a single
    /// horizontal row, and each cell shows the modifier's own badge (icon +
    /// current level, if any) rather than a piece preview.
    /// </summary>
    public sealed class ModifierUpgradeChoiceView : MonoBehaviour
    {
        private const int Columns = 5;
        private const float CellSize = 100f;
        private const float CellSpacing = 10f;
        private const float TitleHeight = 40f;
        private const float ConfirmHeight = 46f;
        private const float BlockSpacing = 24f;
        // The canvas is always exactly this tall in its own local units
        // regardless of actual window size (CanvasScaler matches on height
        // — see GameBootstrap.BuildCanvas), so centering math done in this
        // space holds for any resolution.
        private const float CanvasHeight = 800f;

        /// <summary>Fires with the chosen slot's index into ActiveModifiers once the player confirms.</summary>
        public event Action<int> ModifierUpgradeChoiceConfirmed;

        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _cardContainer;
        private Text _title;
        private RectTransform _pickerContainer;
        private Button _confirmButton;
        private RectTransform _confirmRect;

        private readonly List<ModifierId> _candidateIds = new List<ModifierId>();
        private readonly Dictionary<int, Image> _cellBackgroundByIndex = new Dictionary<int, Image>();
        private int _selectedIndex = -1;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            var overlay = UIFactory.CreatePanel(parent, "ModifierUpgradeChoiceOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            _cardContainer = UIFactory.CreateUIObject("CardContainer", _root);
            _cardContainer.anchorMin = new Vector2(0.5f, 1f);
            _cardContainer.anchorMax = new Vector2(0.5f, 1f);
            _cardContainer.pivot = new Vector2(0.5f, 1f);
            // Vertical position set in Show(), as part of the whole block's
            // layout — see LayoutBlock.

            _title = UIFactory.CreateText(_root, "Title", "Choose a modifier to upgrade", 22, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            _title.rectTransform.sizeDelta = new Vector2(700f, 40f);

            _pickerContainer = UIFactory.CreateUIObject("Picker", _root);
            _pickerContainer.anchorMin = new Vector2(0.5f, 1f);
            _pickerContainer.anchorMax = new Vector2(0.5f, 1f);
            _pickerContainer.pivot = new Vector2(0.5f, 1f);
            var layout = _pickerContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CellSize, CellSize);
            layout.spacing = new Vector2(CellSpacing, CellSpacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;
            var fitter = _pickerContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _confirmButton = UIFactory.CreateButton(_root, "Confirm", "Confirm", UISprites.ChooseButtonBackground, 18);
            _confirmRect = _confirmButton.GetComponent<RectTransform>();
            _confirmRect.anchorMin = new Vector2(0.5f, 1f);
            _confirmRect.anchorMax = new Vector2(0.5f, 1f);
            _confirmRect.pivot = new Vector2(0.5f, 1f);
            _confirmRect.sizeDelta = new Vector2(200f, ConfirmHeight);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(IReadOnlyList<ModifierId> activeModifiers, System.Func<int, int> levelProvider, UpgradeDefinition def)
        {
            _candidateIds.Clear();
            _candidateIds.AddRange(activeModifiers);
            _selectedIndex = -1;
            _cellBackgroundByIndex.Clear();

            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardContainer.GetChild(i).gameObject);
            }
            var card = UpgradeCardFactory.Build(_cardContainer, def);

            for (int i = _pickerContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_pickerContainer.GetChild(i).gameObject);
            }
            for (int i = 0; i < _candidateIds.Count; i++)
            {
                BuildCell(i, levelProvider != null ? levelProvider(i) : 1);
            }

            // Rows needed to lay out _candidateIds.Count cells at Columns
            // wide — the picker's own true height, same "measure, don't
            // guess" discipline as every other card+block layout in this
            // game (see PieceChoiceView/TileChoiceView.LayoutBlock).
            int rows = Mathf.CeilToInt(_candidateIds.Count / (float)Columns);
            float pickerHeight = rows * CellSize + Mathf.Max(0, rows - 1) * CellSpacing;

            LayoutBlock(card.sizeDelta.y, pickerHeight);

            RefreshConfirmInteractable();
            _root.gameObject.SetActive(true);
        }

        /// <summary>Same "measure everything, guess nothing, center the whole block" layout as TileChoiceView/PieceChoiceView.LayoutBlock — see there for why.</summary>
        private void LayoutBlock(float cardHeight, float pickerHeight)
        {
            float totalHeight = cardHeight + BlockSpacing + TitleHeight + BlockSpacing + pickerHeight + BlockSpacing + ConfirmHeight;
            float topY = -Mathf.Max(20f, (CanvasHeight - totalHeight) / 2f);

            _cardContainer.anchoredPosition = new Vector2(0f, topY);
            float y = topY - cardHeight - BlockSpacing;

            _title.rectTransform.anchoredPosition = new Vector2(0f, y);
            y -= TitleHeight + BlockSpacing;

            _pickerContainer.anchoredPosition = new Vector2(0f, y);
            y -= pickerHeight + BlockSpacing;

            _confirmRect.anchoredPosition = new Vector2(0f, y);
        }

        private void BuildCell(int index, int level)
        {
            var cell = UIFactory.CreatePanel(_pickerContainer, "ModifierCell_" + index, UITheme.ButtonIdle);
            cell.rectTransform.sizeDelta = new Vector2(CellSize, CellSize);
            _cellBackgroundByIndex[index] = cell;

            const float badgeSize = 80f;
            var badge = ModifierBadgeFactory.Create(cell.transform, ModifierCatalog.Get(_candidateIds[index]), badgeSize, _tooltip);
            badge.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchoredPosition = Vector2.zero;
            // Click handling lives on the BADGE's own Image (already a
            // raycast target, same GameObject ModifierBadgeFactory's own
            // tooltip hover component sits on) rather than the surrounding
            // cell panel — a Button here would just get shadowed by the
            // badge drawn on top of it, since nothing marks the badge as
            // click-through.
            var cellBtn = badge.gameObject.AddComponent<Button>();
            // Selectable only self-assigns this via Reset(), which Unity
            // skips for a component added through script (see UIFactory.
            // FinishButton's own identical comment) — left null, clicks
            // would still register (raycasting reads the Image's own
            // raycastTarget, not this), but no hover/press tint would ever
            // show.
            cellBtn.targetGraphic = badge;
            cellBtn.onClick.AddListener(() => OnCellClicked(index));

            if (level > 1)
            {
                var levelLabel = UIFactory.CreateText(cell.transform, "Level", "Lv" + level, 16, UITheme.TextPrimary);
                levelLabel.raycastTarget = false;
                levelLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
                levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
                levelLabel.rectTransform.pivot = new Vector2(1f, 0f);
                levelLabel.rectTransform.anchoredPosition = new Vector2(-4f, 4f);
                levelLabel.rectTransform.sizeDelta = new Vector2(40f, 20f);
            }
        }

        private void OnCellClicked(int index)
        {
            if (_selectedIndex >= 0 && _cellBackgroundByIndex.TryGetValue(_selectedIndex, out var previous))
            {
                previous.color = UITheme.ButtonIdle;
            }
            _selectedIndex = index;
            _cellBackgroundByIndex[index].color = UITheme.ButtonSelected;
            RefreshConfirmInteractable();
        }

        private void RefreshConfirmInteractable()
        {
            _confirmButton.interactable = _selectedIndex >= 0;
        }

        private void OnConfirmClicked()
        {
            if (_selectedIndex < 0)
            {
                return;
            }
            _root.gameObject.SetActive(false);
            if (ModifierUpgradeChoiceConfirmed != null)
            {
                ModifierUpgradeChoiceConfirmed(_selectedIndex);
            }
        }
    }
}
