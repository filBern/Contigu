using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The Collection: every fertilizer, sprout, enemy and weather of the game as a grid, the ones not met yet shown as dark question marks.
    /// Hover a discovered entry for what it does. Progress per tab and overall at the top.
    /// </summary>
    public sealed class CollectionView : MonoBehaviour
    {
        private const float GridWidth = 1180f;
        private const float GridTop = 190f;

        private sealed class Entry
        {
            public string Name;
            public string Description;
            public string Subtitle;
            public Sprite Icon;
            public int Id;
        }

        private static readonly CollectionCategory[] Tabs = { CollectionCategory.Fertilizer, CollectionCategory.Sprout, CollectionCategory.Enemy, CollectionCategory.Weather };

        private TooltipView _tooltip;
        private RectTransform _root;
        private RectTransform _gridHost;
        private Text _overallLabel;
        private Image _overallFill;
        private readonly List<Image> _tabImages = new List<Image>();
        private readonly List<Text> _tabLabels = new List<Text>();
        private CollectionCategory _current = CollectionCategory.Fertilizer;
        private readonly Dictionary<CollectionCategory, List<Entry>> _entries = new Dictionary<CollectionCategory, List<Entry>>();

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            var overlay = UIFactory.CreatePanel(parent, "CollectionOverlay", UITheme.Background);
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "COLLECTION", 34, UITheme.TextOnBackground);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(700f, 46f));

            _overallLabel = UIFactory.CreateText(_root, "Overall", "", 20, UITheme.TextMutedOnBackground);
            Place(_overallLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 28f));

            var barBack = UIFactory.CreatePanel(_root, "OverallBack", new Color(0f, 0f, 0f, 0.35f));
            Place(barBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(520f, 14f));
            _overallFill = UIFactory.CreatePanel(barBack.transform, "OverallFill", UITheme.ButtonSelected);
            _overallFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _overallFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _overallFill.rectTransform.offsetMin = Vector2.zero;
            _overallFill.rectTransform.offsetMax = Vector2.zero;

            for (int i = 0; i < Tabs.Length; i++)
            {
                var category = Tabs[i];
                var tab = UIFactory.CreateButton(_root, "Tab_" + category, "", UITheme.PanelLight, 18);
                var rect = (RectTransform)tab.transform;
                Place(rect, new Vector2(0.5f, 1f), new Vector2((i - (Tabs.Length - 1) / 2f) * 270f, -128f), new Vector2(260f, 44f));
                _tabImages.Add(tab.GetComponent<Image>());
                var label = tab.GetComponentInChildren<Text>();
                label.color = UITheme.TextPrimary;
                _tabLabels.Add(label);
                tab.onClick.AddListener(() => { SfxManager.Play(SfxId.CarouselTick); SelectTab(category); });
            }

            _gridHost = UIFactory.CreateUIObject("Grid", _root);
            _gridHost.anchorMin = new Vector2(0.5f, 1f);
            _gridHost.anchorMax = new Vector2(0.5f, 1f);
            _gridHost.pivot = new Vector2(0.5f, 1f);
            _gridHost.anchoredPosition = new Vector2(0f, -GridTop);
            _gridHost.sizeDelta = new Vector2(GridWidth, 580f);

            var close = UIFactory.CreateButton(_root, "Close", "Close", UISprites.CancelButtonBackground);
            var closeRect = (RectTransform)close.transform;
            Place(closeRect, new Vector2(1f, 1f), new Vector2(-100f, -34f), new Vector2(160f, 44f));
            close.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor == new Vector2(1f, 1f) ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void Show()
        {
            SfxManager.Play(SfxId.Overlay);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            OverlayLift.SetPanelsHidden(true);
            SelectTab(_current);
        }

        public void Hide()
        {
            SfxManager.Play(SfxId.Overlay);
            _tooltip.Hide();
            _root.gameObject.SetActive(false);
            OverlayLift.SetPanelsHidden(false);
        }

        private void SelectTab(CollectionCategory category)
        {
            _current = category;
            RefreshHeader();
            RebuildGrid();
        }

        private void RefreshHeader()
        {
            var progress = CollectionTracker.Progress;
            int found = 0;
            int total = 0;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int count = progress.Count(Tabs[i]);
                int size = EntriesFor(Tabs[i]).Count;
                found += Mathf.Min(count, size);
                total += size;
                _tabLabels[i].text = TabName(Tabs[i]) + "  " + Mathf.Min(count, size) + "/" + size;
                _tabImages[i].color = Tabs[i] == _current ? UITheme.ButtonSelected : UITheme.PanelLight;
            }
            int percent = total > 0 ? Mathf.FloorToInt(100f * found / total) : 0;
            _overallLabel.text = "Discovered " + found + " / " + total + "  (" + percent + "%)";
            _overallFill.rectTransform.anchorMax = new Vector2(total > 0 ? (float)found / total : 0f, 1f);
        }

        private static string TabName(CollectionCategory category)
        {
            switch (category)
            {
                case CollectionCategory.Fertilizer: return "Fertilizers";
                case CollectionCategory.Sprout: return "Sprouts";
                case CollectionCategory.Enemy: return "Pests";
                default: return "Weather";
            }
        }

        private void RebuildGrid()
        {
            for (int i = _gridHost.childCount - 1; i >= 0; i--)
            {
                Destroy(_gridHost.GetChild(i).gameObject);
            }
            var entries = EntriesFor(_current);
            bool large = _current == CollectionCategory.Enemy || _current == CollectionCategory.Weather;
            float cell = large ? 104f : 58f;
            float gap = large ? 14f : 6f;
            int columns = Mathf.Max(1, Mathf.FloorToInt((GridWidth + gap) / (cell + gap)));
            int rows = Mathf.CeilToInt(entries.Count / (float)columns);
            float gridHeight = rows * (cell + gap) - gap;
            float startY = Mathf.Min(0f, 0f);
            var progress = CollectionTracker.Progress;
            for (int i = 0; i < entries.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int inRow = Mathf.Min(columns, entries.Count - row * columns);
                float rowWidth = inRow * (cell + gap) - gap;
                BuildCell(entries[i], progress.IsDiscovered(_current, entries[i].Id),
                    new Vector2(-rowWidth / 2f + col * (cell + gap) + cell / 2f, startY - row * (cell + gap) - cell / 2f), cell);
            }
            _gridHost.sizeDelta = new Vector2(GridWidth, Mathf.Max(gridHeight, 10f));
        }

        private void BuildCell(Entry entry, bool discovered, Vector2 center, float size)
        {
            var cell = UIFactory.CreatePanel(_gridHost, "Cell", discovered ? UITheme.PanelLight : new Color(0f, 0f, 0f, 0.28f));
            cell.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            cell.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cell.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cell.rectTransform.anchoredPosition = center;
            cell.rectTransform.sizeDelta = new Vector2(size, size);
            if (discovered)
            {
                UIFactory.AddThickOutline(cell, UITheme.Border, 2f);
            }

            if (entry.Icon != null)
            {
                var icon = UIFactory.CreatePanel(cell.transform, "Icon", discovered ? Color.white : new Color(0.1f, 0.1f, 0.12f, 0.9f));
                icon.sprite = entry.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UIFactory.StretchFull(icon.rectTransform);
                icon.rectTransform.offsetMin = new Vector2(3f, 3f);
                icon.rectTransform.offsetMax = new Vector2(-3f, -3f);
            }
            if (!discovered)
            {
                var mark = UIFactory.CreateText(cell.transform, "Mark", "?", Mathf.RoundToInt(size * 0.5f), new Color(1f, 1f, 1f, 0.55f));
                mark.raycastTarget = false;
                UIFactory.StretchFull(mark.rectTransform);
            }

            var hover = cell.gameObject.AddComponent<HoverTooltip>();
            if (discovered)
            {
                hover.Init(_tooltip, entry.Name, DescriptionTextFormatter.Colorize(entry.Description, 14), entry.Subtitle, UITheme.TextMuted);
            }
            else
            {
                hover.Init(_tooltip, "???", "Not discovered yet. Keep playing to find it.", entry.Subtitle, UITheme.TextMuted);
            }
        }

        private List<Entry> EntriesFor(CollectionCategory category)
        {
            List<Entry> list;
            if (_entries.TryGetValue(category, out list))
            {
                return list;
            }
            list = new List<Entry>();
            switch (category)
            {
                case CollectionCategory.Fertilizer:
                    foreach (var def in ModifierCatalog.All)
                    {
                        list.Add(new Entry { Id = (int)def.Id, Name = def.Name, Description = def.Description, Subtitle = "Fertilizer · " + ModifierVisualDefaults.GetCategoryLabel(def.Category), Icon = ModifierVisualDefaults.GetIcon(def.Id) });
                    }
                    break;
                case CollectionCategory.Sprout:
                    foreach (var def in UpgradeCatalog.GridPool)
                    {
                        if (!UpgradeSystem.TraitKindFor(def.Id).HasValue || def.Id == UpgradeId.PermanentBonusSquare)
                        {
                            continue;
                        }
                        list.Add(new Entry { Id = (int)def.Id, Name = def.Name, Description = def.Description, Subtitle = "Sprout · " + UpgradeVisualDefaults.GetRarityLabel(def.Rarity), Icon = UpgradeVisualDefaults.GetGlyph(def.Id) ?? UpgradeVisualDefaults.GetIcon(def.Id) });
                    }
                    break;
                case CollectionCategory.Enemy:
                    foreach (EnemyId id in Enum.GetValues(typeof(EnemyId)))
                    {
                        var def = EnemyCatalog.Get(id);
                        if (def.Id != id)
                        {
                            continue;
                        }
                        list.Add(new Entry { Id = (int)id, Name = def.Name, Description = def.Description, Subtitle = "Pest · " + def.Role, Icon = HudView.EnemyIconSprite(id) });
                    }
                    break;
                default:
                    foreach (WeatherId id in Enum.GetValues(typeof(WeatherId)))
                    {
                        if (id == WeatherId.Clear)
                        {
                            continue;
                        }
                        list.Add(new Entry { Id = (int)id, Name = WeatherCatalog.GetName(id), Description = WeatherCatalog.GetDescription(id), Subtitle = "Weather · " + WeatherCatalog.GetRarityLabel(id), Icon = VisualDefaults.GetWeatherSprite(id) });
                    }
                    break;
            }
            _entries[category] = list;
            return list;
        }
    }
}
