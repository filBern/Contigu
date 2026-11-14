using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers as a 2-column grid of bare badges.
    /// The panel has a static height sized for EconomyConstants.MaxActiveModifiers
    /// modifiers (2x2); RunManager enforces that same cap, so the grid never
    /// needs to grow past this size. Refreshed by the caller whenever the
    /// active set changes, plus a <see cref="Pulse"/> effect triggered on a
    /// badge when it scores points on a placement.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        // internal (not private) so ShopView's own "Next round" preview box can match this panel's exact
        // size instead of carrying its own separate magic numbers — see ShopView.UpcomingBoxWidth etc.
        // The whole left column (score card, fertilizers, Seed bag, Rules) is this wide.
        internal const float PanelWidth = 260f;
        private const float BadgeSize = 90f;
        private const float BadgeSpacing = 10f;
        internal const float HeaderHeight = 58f;
        // Gap between the header band and the first badge row, so their thick
        // outlines don't sit flush against each other.
        internal const float HeaderRowGap = 12f;
        internal const float BottomPadding = 16f;
        private const int DisplayRows = 2; // 2 columns x 2 rows = 4 modifiers (EconomyConstants.MaxActiveModifiers)
        // internal so ShopView's "Next round" box can match this panel's exact height too (see PanelWidth).
        internal const float PanelHeight = HeaderHeight + HeaderRowGap + DisplayRows * BadgeSize + (DisplayRows - 1) * BadgeSpacing + BottomPadding;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;
        // A newly added badge pops in from nothing with a slight overshoot;
        // a removed one shrinks and fades out in place instead of vanishing
        // instantly when Refresh rebuilds the grid.
        private const float AddedPopDuration = 0.35f;
        private const float AddedPopPeakScale = 1.15f;
        private const float AddedPopPeakFraction = 0.5f;
        private const float RemovedShrinkDuration = 0.3f;
        // How much a tap-selected badge lightens toward white, and how
        // transparent a badge goes while being dragged so its row still
        // reads as a visible drop target underneath the cursor.
        private const float SelectedTintAmount = 0.35f;
        private const float DraggingAlpha = 0.4f;
        // How strongly the badge currently under the cursor tints gold as a
        // live preview of the drag's drop target.
        private const float DropTargetTintAmount = 0.5f;
        private const float IndexLabelSize = 22f;

        private RectTransform _root;
        private RectTransform _rowsContainer;
        private TooltipView _tooltip;
        private System.Func<ModifierId, int> _usageCountProvider;
        // Takes the row's index alongside the id to look up that row's level factor.
        private System.Func<ModifierId, int, string> _progressiveStateProvider;
        // Keyed by index, not id — level is per-slot (see RunManager.GetModifierLevel),
        // not per-modifier-type, so two copies of the same id via Copieur can
        // sit at different levels.
        private System.Func<int, int> _levelProvider;
        private bool _interactable = true;

        /// <summary>Whether the modifier at a slot index is currently blighted (set by GameBootstrap); blighted badges are greyed with a blight mark and say so in their tooltip.</summary>
        public System.Func<int, bool> BlightProvider;

        /// <summary>Rounds the fertilizer at a slot has left before it withers (Autumn); 0 or null means it never withers.</summary>
        public System.Func<int, int> RoundsLeftProvider;

        /// <summary>Growth steps and past harvests of the fertilizer at a slot (Maturation, see FertilizerGrowth); null means no growth bar.</summary>
        public System.Func<int, int> GrowthProvider;
        public System.Func<int, int> HarvestsProvider;

        /// <summary>Roots (see FertilizerGrowth): the sap a slot receives from its left neighbor, and how many neighbors of its category resonate with it.</summary>
        public System.Func<int, float> SapProvider;
        public System.Func<int, int> ResonanceProvider;
        private static readonly Color RootsBg = new Color(0.55f, 0.36f, 0.20f, 1f);

        private const float StageMedallionSize = 30f;

        // Tap-to-swap: the first-tapped badge's row, armed and waiting for
        // a second tap on a different badge to swap with — -1 when nothing
        // is armed. Drag-to-move: the row currently being dragged, -1 when
        // no drag is in flight. Independent of each other.
        private int _selectedRowIndex = -1;
        private int _draggingRowIndex = -1;

        // Which row the pointer currently rests over, -1 when none.
        private int _hoveredRowIndex = -1;
        // The Roots links drawn between neighboring badges (sap, resonance); rebuilt at every Refresh.
        private readonly List<GameObject> _rootLinks = new List<GameObject>();

        // Which row is tinted gold as the drag's live drop-target preview,
        // -1 when none — only ever set while _draggingRowIndex >= 0, tracked
        // separately since _hoveredRowIndex also keeps working outside of a drag.
        private int _dropTargetRowIndex = -1;

        /// <summary>The row index the pointer currently hovers, or -1 if none/the panel is blocked (see SetInteractable) — GameBootstrap reads this on its sell key press rather than tracking hover itself.</summary>
        public int HoveredRowIndex
        {
            get { return _interactable ? _hoveredRowIndex : -1; }
        }

        /// <summary>Fired when the player taps two different badges in a row — RunManager.SwapModifiers(a, b) is the expected response, followed by a Refresh.</summary>
        public event System.Action<int, int> SwapRequested;

        /// <summary>Fired by the Sell button that appears under a tapped (armed) badge: the row to sell. The touch-friendly twin of the X key.</summary>
        public event System.Action<int> SellRequested;

        private UnityEngine.UI.Button _sellButton;
        private UnityEngine.UI.Text _sellLabel;
        private readonly List<ModifierId> _heldModifiers = new List<ModifierId>();

        /// <summary>Fired when the player drags a badge and drops it onto another — RunManager.MoveModifier(from, to) is the expected response, followed by a Refresh.</summary>
        public event System.Action<int, int> MoveRequested;

        // Parallel to the active-modifiers list passed to the last Refresh —
        // lets Pulse(id)/GetBadgeTransform find the badge(s) currently
        // showing that modifier. Copieur duplicates an existing modifier id
        // rather than being its own distinct id, so a given id can appear
        // more than once here.
        private readonly List<ModifierId> _rowIds = new List<ModifierId>();
        private readonly List<Image> _rowBadges = new List<Image>();
        // Each row's true resting color, captured once when its badge is
        // built — PulseBadge lerps against this, never the badge's live
        // .color, which can be mid-lerp from a still-running earlier pulse.
        private readonly List<Color> _rowBaseColors = new List<Color>();
        // The currently-running pulse coroutine for each row, if any. A new
        // pulse stops any pulse already running on that row first, so at
        // most one ever animates a row's color at a time and _rowBaseColors'
        // true value is always what gets restored.
        private readonly List<Coroutine> _rowPulseCoroutines = new List<Coroutine>();

        /// <summary>
        /// <paramref name="usageCountProvider"/> lets each badge's tooltip show
        /// how many times it's fired this run. <paramref name="progressiveStateProvider"/>
        /// lets a progressive/incremental modifier's tooltip show its current
        /// live state; null for every other modifier. Both take the row's
        /// index (not just the id) so the row's level factor can be folded
        /// in, since Copieur can make the same id occupy more than one row,
        /// each independently leveled. <paramref name="levelProvider"/> feeds
        /// the small "Lv.N" badge shown on any row leveled past 1.
        /// </summary>
        public RectTransform Build(Transform parent, TooltipView tooltip, System.Func<ModifierId, int> usageCountProvider, System.Func<ModifierId, int, string> progressiveStateProvider = null, System.Func<int, int> levelProvider = null, float horizontalCenterX = 0f)
        {
            _tooltip = tooltip;
            _usageCountProvider = usageCountProvider;
            _progressiveStateProvider = progressiveStateProvider;
            _levelProvider = levelProvider;
            // Garden parchment card (its own dark outline, so no extra outline component).
            // A wooden planter like the hand slots and the buttons: a wooden bed around dark soil.
            var panel = UIFactory.CreateSlicedImage(parent, "ModifierPanel", UISprites.Garden("WoodLight") ?? UISprites.Garden("Wood") ?? UISprites.CardBackground);
            UIFactory.AddDropShadow(panel, 8f, 0.42f);
            _root = panel.rectTransform;
            // Static size, centered vertically and horizontally in the gap
            // between the grid and screen edge; never resizes at runtime.
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _root.anchoredPosition = new Vector2(horizontalCenterX, 0f);

            // A wooden sign hanging from the top of the card, not a glossy pill (that reads as a button).
            ShopSign.Build(_root, "Fertilizers", 24);

            _emptyHint = UIFactory.CreateText(_root, "EmptyHint", "No fertilizer yet.\nBuy a sack in the Blister.", 16, new Color(0.16f, 0.10f, 0.05f, 0.9f), TextAnchor.UpperCenter);
            _emptyHint.rectTransform.anchorMin = new Vector2(0f, 1f);
            _emptyHint.rectTransform.anchorMax = new Vector2(1f, 1f);
            _emptyHint.rectTransform.pivot = new Vector2(0.5f, 1f);
            _emptyHint.rectTransform.anchoredPosition = new Vector2(0f, -(HeaderHeight + 24f));
            _emptyHint.rectTransform.sizeDelta = new Vector2(-24f, 60f);
            _emptyHint.raycastTarget = false;

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            _rowsContainer.anchoredPosition = new Vector2(0f, -(HeaderHeight + HeaderRowGap));

            var layout = _rowsContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(BadgeSize, BadgeSize);
            layout.spacing = new Vector2(BadgeSpacing, BadgeSpacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            _sellButton = UIFactory.CreateButton(_root, "SellButton", "Sell", UISprites.CancelButtonBackground, 18);
            UIFactory.DisablePlants(_sellButton);
            var sellRect = (RectTransform)_sellButton.transform;
            // In place of the "Fertilizers" sign, at the top of the panel: never over a badge (a tap meant to swap or disarm a fertilizer must not
            // land on Sell) and never over the Seed bag and Menu buttons stacked under the panel, which it used to hide.
            sellRect.anchorMin = new Vector2(0.5f, 1f);
            sellRect.anchorMax = new Vector2(0.5f, 1f);
            sellRect.pivot = new Vector2(0.5f, 1f);
            sellRect.anchoredPosition = new Vector2(0f, -14f);
            sellRect.sizeDelta = new Vector2(PanelWidth - 30f, MobileSetup.TouchMin(44f));
            _sellLabel = _sellButton.GetComponentInChildren<UnityEngine.UI.Text>();
            _sellButton.onClick.AddListener(() =>
            {
                if (_selectedRowIndex >= 0 && SellRequested != null)
                {
                    SellRequested(_selectedRowIndex);
                }
            });
            _sellButton.gameObject.SetActive(false);

            return _root;
        }

        /// <summary>Pins the tooltip of row <paramref name="index"/> open on touch (-1 unpins every row).</summary>
        private void PinTooltip(int index)
        {
            for (int i = 0; i < _rowBadges.Count; i++)
            {
                var view = _rowBadges[i] != null ? _rowBadges[i].GetComponent<ModifierBadgeView>() : null;
                if (view == null)
                {
                    continue;
                }
                if (i == index && MobileSetup.IsTouchDevice)
                {
                    view.Pin();
                }
                else
                {
                    view.Unpin();
                }
            }
        }

        /// <summary>A tap anywhere but on a badge or the Sell button drops the armed badge, which also closes its pinned tooltip.</summary>
        private void DisarmOnOutsideTap()
        {
            if (_selectedRowIndex < 0 || UnityEngine.EventSystems.EventSystem.current == null)
            {
                return;
            }
            Vector2 position;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
            }
            else
            {
                return;
            }
            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = position }, results);
            foreach (var hit in results)
            {
                var t = hit.gameObject.transform;
                if (t.IsChildOf(_rowsContainer) || (_sellButton != null && t.IsChildOf(_sellButton.transform)))
                {
                    return;
                }
            }
            int armed = _selectedRowIndex;
            _selectedRowIndex = -1;
            if (armed < _rowBadges.Count)
            {
                ApplyRestingColor(armed);
            }
            PinTooltip(-1);
        }

        /// <summary>The Sell button shows only while a badge is armed by a tap (and says what it would pay).</summary>
        private void Update()
        {
            UpdateLiveTilt();
            DisarmOnOutsideTap();
            if (_sellButton == null)
            {
                return;
            }
            bool show = _interactable && _selectedRowIndex >= 0 && _selectedRowIndex < _heldModifiers.Count;
            if (_sellButton.gameObject.activeSelf != show)
            {
                _sellButton.gameObject.SetActive(show);
            }
            if (show)
            {
                _sellLabel.text = "Sell +" + (ModifierPricing.GetPrice(_heldModifiers[_selectedRowIndex]) - 1);
            }
        }

        private Text _emptyHint;

        public void Refresh(IReadOnlyList<ModifierId> activeModifiers)
        {
            if (_emptyHint != null)
            {
                // The empty-panel hint waits for the last badge to finish shrinking away (see below): it never sits under a fading badge.
                _emptyHint.gameObject.SetActive(activeModifiers.Count == 0 && _rowBadges.Count == 0);
            }
            // Snapshotted before the rebuild below so add/remove feedback can
            // diff against what was on screen a moment ago; a plain reorder
            // (swap/drag) must not read as anything added or removed.
            var previousIds = new List<ModifierId>(_rowIds);
            var previousBadges = new List<Image>(_rowBadges);

            // Greedy multiset match: each new entry claims the first
            // still-unclaimed old row with the same id. Matching by value
            // (not position) makes a reorder a no-op here. Duplicate ids
            // (Copieur) are handled correctly since each occurrence can only
            // claim one specific old row.
            var oldClaimed = new bool[previousIds.Count];
            var newIsAdded = new bool[activeModifiers.Count];
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                bool matched = false;
                for (int j = 0; j < previousIds.Count; j++)
                {
                    if (!oldClaimed[j] && previousIds[j] == activeModifiers[i])
                    {
                        oldClaimed[j] = true;
                        matched = true;
                        break;
                    }
                }
                newIsAdded[i] = !matched;
            }

            // Every unclaimed old row is a genuine removal, animated away
            // instead of destroyed outright; every claimed one is staying or
            // moving, so it's safe to destroy immediately since a fresh
            // badge for it is about to be built below.
            for (int j = 0; j < previousBadges.Count; j++)
            {
                // A badge already destroyed from outside (the row was torn down by something else) has nothing left to animate or destroy.
                if (previousBadges[j] == null)
                {
                    continue;
                }
                if (oldClaimed[j])
                {
                    Destroy(previousBadges[j].gameObject);
                }
                else
                {
                    PlayRemovedBadge(previousBadges[j]);
                    if (activeModifiers.Count == 0 && _emptyHint != null)
                    {
                        StartCoroutine(ShowHintAfterRemoval());
                    }
                }
            }

            _heldModifiers.Clear();
            for (int h = 0; h < activeModifiers.Count; h++)
            {
                _heldModifiers.Add(activeModifiers[h]);
            }
            _rowIds.Clear();
            _rowBadges.Clear();
            _rowBaseColors.Clear();
            _rowPulseCoroutines.Clear();
            // A stale index here would point at nothing, or at an unrelated
            // modifier now sitting at that same position.
            _selectedRowIndex = -1;
            _draggingRowIndex = -1;
            _hoveredRowIndex = -1;
            for (int c = 0; c < _rootLinks.Count; c++)
            {
                if (_rootLinks[c] != null) { Destroy(_rootLinks[c]); }
            }
            _rootLinks.Clear();
            _dropTargetRowIndex = -1;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                // ModifierBadgeFactory/ModifierBadgeView only know a plain
                // Func<ModifierId,string>, shared with the shop's index-less
                // draft cards — this row's index is baked into the closure
                // here instead, so the badge never needs to know about levels.
                int rowIndex = i;
                System.Func<ModifierId, string> rowProgressiveState = _progressiveStateProvider != null
                    ? (System.Func<ModifierId, string>)(rowId => _progressiveStateProvider(rowId, rowIndex))
                    : null;
                int level = _levelProvider != null ? _levelProvider(i) : 1;
                // A leveled modifier's static Description text still states
                // its base, level-1 numbers, since free-text descriptions
                // can't safely be auto-rewritten per modifier. Shown only
                // past level 1 to avoid noise for the common unleveled case.
                System.Func<ModifierId, string> rowLevelState = level > 1
                    ? (System.Func<ModifierId, string>)(rowId => "Level " + level + " - x" + ModifierLevelUtility.LevelToFactor(level).ToString(System.Globalization.CultureInfo.InvariantCulture) + " effect")
                    : null;
                bool blighted = BlightProvider != null && BlightProvider(i);
                if (blighted)
                {
                    var baseLevelState = rowLevelState;
                    rowLevelState = rowId => "BLIGHTED: no effect until the next Shuffle." + (baseLevelState != null ? "\n" + baseLevelState(rowId) : string.Empty);
                }
                int growth = GrowthProvider != null ? GrowthProvider(i) : 0;
                int harvests = HarvestsProvider != null ? HarvestsProvider(i) : 0;
                float sap = SapProvider != null ? SapProvider(i) : 0f;
                int resonating = ResonanceProvider != null ? ResonanceProvider(i) : 0;
                float rootsBonus = sap + resonating * FertilizerGrowth.ResonancePerNeighbor;
                int roundsLeft = RoundsLeftProvider != null ? RoundsLeftProvider(i) : 0;
                if (roundsLeft > 0)
                {
                    var baseWitherState = rowLevelState;
                    string witherLine = "Withers away in " + roundsLeft + (roundsLeft == 1 ? " round (this one)." : " rounds.");
                    rowLevelState = rowId => witherLine + (baseWitherState != null ? "\n" + baseWitherState(rowId) : string.Empty);
                }
                // "With bonus": the whole effect as it stands (level + growth + Roots), against the base numbers of the description.
                string bonusLine = null;
                float growthBonus = FertilizerGrowth.Bonus(growth, harvests);
                if (!blighted && growthBonus + rootsBonus > 0f)
                {
                    float total = ModifierLevelUtility.LevelToFactor(level) + growthBonus + rootsBonus;
                    bonusLine = "With bonus: " + DescriptionTextFormatter.Amount(total - 1f, DescriptionTextFormatter.EffectUnitOf(ModifierCatalog.Get(activeModifiers[i]).Description));
                }
                System.Func<ModifierId, string> rowBonusLine = bonusLine != null ? (rowId => bonusLine) : (System.Func<ModifierId, string>)null;
                var badge = ModifierBadgeFactory.Create(_rowsContainer, ModifierCatalog.Get(activeModifiers[i]), BadgeSize, _tooltip, _usageCountProvider, progressiveStateProvider: rowProgressiveState, levelStateProvider: rowLevelState, showSellValue: true, bonusLineProvider: rowBonusLine);
                _rowIds.Add(activeModifiers[i]);
                _rowBadges.Add(badge);
                _rowBaseColors.Add(badge.color);
                _rowPulseCoroutines.Add(null);
                if (newIsAdded[i])
                {
                    StartCoroutine(PlayAddedBadge(badge.rectTransform));
                }

                if (blighted)
                {
                    var veil = UIFactory.CreatePanel(badge.transform, "BlightVeil", new Color(0.12f, 0.22f, 0.08f, 0.62f));
                    veil.raycastTarget = false;
                    UIFactory.StretchFull(veil.rectTransform);
                    var mark = UIFactory.CreatePanel(badge.transform, "BlightMark", Color.white);
                    mark.raycastTarget = false;
                    mark.preserveAspect = true;
                    mark.sprite = PieceTraitVisualDefaults.GetPoisonSprite();
                    mark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    mark.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    mark.rectTransform.sizeDelta = new Vector2(BadgeSize * 0.6f, BadgeSize * 0.6f);
                    mark.gameObject.SetActive(mark.sprite != null);
                }

                if (roundsLeft > 0)
                {
                    // Autumn's countdown pill, bottom-left: how many cleared rounds this fertilizer still has.
                    var pill = UIFactory.CreatePanel(badge.transform, "WitherPill", roundsLeft == 1 ? UITheme.Danger : new Color(0.86f, 0.55f, 0.16f, 1f));
                    pill.raycastTarget = false;
                    pill.rectTransform.anchorMin = new Vector2(0f, 0f);
                    pill.rectTransform.anchorMax = new Vector2(0f, 0f);
                    pill.rectTransform.pivot = new Vector2(0f, 0f);
                    pill.rectTransform.anchoredPosition = new Vector2(2f, 4f + StageMedallionSize);
                    pill.rectTransform.sizeDelta = new Vector2(24f, 24f);
                    var pillLabel = UIFactory.CreateText(pill.transform, "WitherLeft", roundsLeft.ToString(), 18, Color.white);
                    pillLabel.raycastTarget = false;
                    UIFactory.StretchFull(pillLabel.rectTransform);
                }

                // Visual position number: scoring order follows this exact
                // list order (see PlacementResult.Mult), so the player needs
                // to see it to arrange x-modifiers after +modifiers. Top-left
                // corner, on a small round backdrop for legibility over any
                // badge color/icon underneath.
                var indexBadge = NumberBadge.Create(badge.transform, (i + 1).ToString(), 30f, Mathf.RoundToInt(IndexLabelSize * 0.85f));
                indexBadge.anchorMin = indexBadge.anchorMax = new Vector2(0f, 1f);
                indexBadge.pivot = new Vector2(0.5f, 0.5f);
                indexBadge.anchoredPosition = new Vector2(15f, -15f);

                // Level badge (see ModifierLevelUtility), bottom-right
                // corner, shown only once a slot is leveled past 1 to avoid
                // visual noise for the common unleveled case.
                if (level > 1)
                {
                    var levelBg = UIFactory.CreatePanel(badge.transform, "LevelBg", UITheme.Success);
                    levelBg.rectTransform.anchorMin = new Vector2(1f, 0f);
                    levelBg.rectTransform.anchorMax = new Vector2(1f, 0f);
                    levelBg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    levelBg.rectTransform.anchoredPosition = new Vector2(-15f, 15f);
                    levelBg.rectTransform.sizeDelta = new Vector2(34f, 34f);

                    var levelLabel = UIFactory.CreateText(badge.transform, "Level", "Lv" + level, Mathf.RoundToInt(IndexLabelSize * 0.8f), UITheme.TextPrimary);
                    levelLabel.raycastTarget = false;
                    levelLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
                    levelLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
                    levelLabel.rectTransform.pivot = new Vector2(1f, 0f);
                    levelLabel.rectTransform.anchoredPosition = new Vector2(-4f, 4f);
                    levelLabel.rectTransform.sizeDelta = new Vector2(30f, 20f);
                }

                if (GrowthProvider != null)
                {
                    GrowthVisuals.AttachStage(badge, growth, harvests, StageMedallionSize, 150f, _tooltip, DescriptionTextFormatter.EffectUnitOf(ModifierCatalog.Get(activeModifiers[i]).Description));
                }
                if (rootsBonus > 0f)
                {
                    // Roots pill, top-right: what the neighbors add to this fertilizer's effect (sap + resonance).
                    var rootsPill = UIFactory.CreatePanel(badge.transform, "RootsPill", RootsBg);
                    // The Roots have a tooltip of their own, apart from the fertilizer's (pointer-downs and drags it does not handle bubble up to the sack).
                    rootsPill.raycastTarget = _tooltip != null;
                    if (_tooltip != null)
                    {
                        string myUnit = DescriptionTextFormatter.EffectUnitOf(ModifierCatalog.Get(activeModifiers[i]).Description);
                        string rootsBody = sap > 0f ? "Sap from the left: " + DescriptionTextFormatter.Amount(sap, myUnit) : string.Empty;
                        if (resonating > 0) { rootsBody += (rootsBody.Length > 0 ? "\n" : string.Empty) + ModifierVisualDefaults.GetCategoryLabel(ModifierCatalog.Get(activeModifiers[i]).Category) + " fertilizer next to it x" + resonating + ": " + DescriptionTextFormatter.Amount(resonating * FertilizerGrowth.ResonancePerNeighbor, myUnit); }
                        rootsPill.gameObject.AddComponent<HoverTooltip>().Init(_tooltip, "Roots +" + Mathf.RoundToInt(rootsBonus * 100f) + "%", rootsBody);
                    }
                    UIFactory.AddThickOutline(rootsPill, UITheme.Border, 1.5f);
                    rootsPill.rectTransform.anchorMin = rootsPill.rectTransform.anchorMax = new Vector2(1f, 1f);
                    rootsPill.rectTransform.pivot = new Vector2(1f, 1f);
                    rootsPill.rectTransform.anchoredPosition = new Vector2(-2f, -2f);
                    rootsPill.rectTransform.sizeDelta = new Vector2(44f, 22f);
                    var rootsLabel = UIFactory.CreateText(rootsPill.transform, "Roots", "+" + Mathf.RoundToInt(rootsBonus * 100f) + "%", 15, Color.white);
                    rootsLabel.raycastTarget = false;
                    UIFactory.StretchFull(rootsLabel.rectTransform);
                }

                // Supports both drag-and-drop and tap-tap swap reordering,
                // same forwarding pattern as HandSlotDragHandler.
                var dragHandler = badge.gameObject.AddComponent<ModifierBadgeDragHandler>();
                dragHandler.Init(this, i);
            }
            BuildRootLinks(activeModifiers);
        }

        private static readonly Color SapBloomTint = new Color(1f, 0.72f, 0.84f, 1f);
        private static readonly Color SapFruitTint = new Color(1f, 0.88f, 0.42f, 1f);

        /// <summary>
        /// Roots: a small chip in the gap between two neighboring badges where the Roots act: a drop and an arrow (sap, tinted by the giver's stage)
        /// when the left one is a Bloom or a Fruit, linked rings (resonance) when both are of one category. Hover or tap explains it.
        /// </summary>
        private void BuildRootLinks(IReadOnlyList<ModifierId> mods)
        {
            if (mods.Count < 2 || _rowBadges.Count < mods.Count)
            {
                return;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsContainer);
            for (int i = 0; i < mods.Count - 1; i++)
            {
                float sap = SapProvider != null ? SapProvider(i + 1) : 0f;
                bool resonate = ModifierCatalog.Get(mods[i]).Category == ModifierCatalog.Get(mods[i + 1]).Category;
                if (sap <= 0f && !resonate)
                {
                    continue;
                }
                var a = _rowBadges[i].rectTransform;
                var b = _rowBadges[i + 1].rectTransform;
                string giver = ModifierCatalog.Get(mods[i]).Name;
                string receiver = ModifierCatalog.Get(mods[i + 1]).Name;
                float offset = sap > 0f && resonate ? 15f : 0f;
                if (sap > 0f)
                {
                    bool fruit = FertilizerGrowth.StageOf(GrowthProvider != null ? GrowthProvider(i) : 0) == GrowthStage.Fruit;
                    string body = giver + " (" + (fruit ? "Fruit" : "Bloom") + ") feeds " + receiver + " on its right.\n" + DescriptionTextFormatter.Amount(sap, DescriptionTextFormatter.EffectUnitOf(ModifierCatalog.Get(mods[i + 1]).Description));
                    AddRootLink("Sap", Resources.Load<Sprite>("Icons/Growth/Roots_Sap"), fruit ? SapFruitTint : SapBloomTint, a, b, offset, "Sap", body);
                }
                if (resonate)
                {
                    string kind = ModifierVisualDefaults.GetCategoryLabel(ModifierCatalog.Get(mods[i]).Category);
                    string body = giver + " and " + receiver + " are both " + kind + " fertilizers (same category).\n" + DescriptionTextFormatter.Amount(FertilizerGrowth.ResonancePerNeighbor, "effect") + " each.";
                    AddRootLink("Resonance", Resources.Load<Sprite>("Icons/Growth/Roots_Resonance"), Color.white, a, b, -offset, "Resonance", body);
                }
            }
        }

        private void AddRootLink(string name, Sprite sprite, Color tint, RectTransform a, RectTransform b, float offset, string title, string body)
        {
            var chip = UIFactory.CreatePanel(_rowsContainer, "Root" + name, tint);
            chip.sprite = sprite;
            chip.enabled = sprite != null;
            chip.preserveAspect = true;
            chip.rectTransform.sizeDelta = new Vector2(30f, 30f);
            chip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var follower = chip.gameObject.AddComponent<RootLinkFollower>();
            follower.A = a;
            follower.B = b;
            follower.Offset = offset;
            chip.transform.SetAsLastSibling();
            chip.gameObject.AddComponent<GrowthGlowPulse>();
            var hover = chip.gameObject.AddComponent<HoverTooltip>();
            hover.Init(_tooltip, title, body);
            _rootLinks.Add(chip.gameObject);
        }

        /// <summary>Blocks reordering (tap-select/swap and drag-and-drop) while a placement's score sequence is animating — mirrors HandView.SetInteractable, since a mid-animation Refresh() would otherwise pull the rug out from under a badge popup/pulse still in flight.</summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        /// <summary>Tap-to-swap: the first tap on a badge arms it (lightens it, same convention as HandView's selected-slot tint); a second tap on a DIFFERENT badge fires <see cref="SwapRequested"/> and disarms; tapping the SAME badge again just disarms.</summary>
        public void OnBadgeClicked(int index)
        {
            if (!_interactable || index < 0 || index >= _rowBadges.Count)
            {
                return;
            }

            if (_selectedRowIndex < 0)
            {
                _selectedRowIndex = index;
                ApplyRestingColor(index);
                // Touch has no hover: the tap that arms a badge also keeps its tooltip up (tap it again, or anywhere else, to close it).
                PinTooltip(index);
                return;
            }

            int armed = _selectedRowIndex;
            _selectedRowIndex = -1;
            ApplyRestingColor(armed);
            PinTooltip(-1);

            if (armed == index)
            {
                return;
            }

            if (SwapRequested != null)
            {
                SwapRequested(armed, index);
            }
        }

        /// <summary>Marks <paramref name="index"/> as the row currently under the pointer — see HoveredRowIndex. Not gated on _interactable here (that's checked by the getter instead), so hover tracking itself never gets out of sync with what the pointer is actually over. While a drag is in flight, hovering over a DIFFERENT row also tints it gold as a live preview of where the dragged modifier would land if dropped right now (see DropTargetTintAmount) — dragging the same badge back over itself deliberately shows no preview, since dropping there is a no-op (see OnBadgeDrop).</summary>
        public void OnBadgeHoverEnter(int index)
        {
            _hoveredRowIndex = index;

            if (_draggingRowIndex >= 0 && index != _draggingRowIndex && _live == null)
            {
                _dropTargetRowIndex = index;
                ApplyRestingColor(index);
            }
        }

        /// <summary>Clears the hover only if it's still THIS row — a fast pointer move can fire the next badge's OnPointerEnter before this one's OnPointerExit, and blindly clearing here would wipe out that newer hover. Same guard for the drop-target preview: only clears (and un-tints) it if this row is still the one currently previewed.</summary>
        public void OnBadgeHoverExit(int index)
        {
            if (_hoveredRowIndex == index)
            {
                _hoveredRowIndex = -1;
            }

            if (_dropTargetRowIndex == index)
            {
                _dropTargetRowIndex = -1;
                ApplyRestingColor(index);
            }
        }

        /// <summary>Drag start — fades the dragged badge so its own row still reads as an available drop target underneath the cursor, and disarms any tap-selection in progress (avoids a confusing "armed AND dragging" combined state).</summary>
        public void OnBadgeBeginDrag(int index, PointerEventData eventData)
        {
            if (!_interactable || index < 0 || index >= _rowBadges.Count)
            {
                return;
            }

            if (_selectedRowIndex >= 0)
            {
                int armed = _selectedRowIndex;
                _selectedRowIndex = -1;
                ApplyRestingColor(armed);
                PinTooltip(-1);
            }

            _draggingRowIndex = index;
            BeginLiveDrag(index, eventData);
        }

        /// <summary>Balatro-style drag: the held fertilizer leaves the grid, rises a little and follows the pointer; a gap in the grid shows where it would land, and the others slide aside as it moves over them. Released, it lands in the gap (a move) or, if the gap never changed, settles back.</summary>
        private sealed class LiveDrag
        {
            public Image Badge;
            public RectTransform Gap;
            public List<Vector2> Centers = new List<Vector2>();
            public List<Image> Others = new List<Image>();
            public Vector3 GrabOffset;
            public int From;
            public int Target;
            public Vector3 RestScale;
            public Outline Glow;
            public Shadow Shadow;
        }

        private LiveDrag _live;
        private const float DragLiftScale = 1.1f;
        // The held fertilizer leans into the motion (like a held card) and settles back when the pointer rests.
        private float _liveTilt;
        private float _liveTiltTarget;
        private const float MaxDragTilt = 12f;
        private static readonly Color HeldGlow = new Color(1f, 0.82f, 0.30f, 0.95f);

        private void UpdateLiveTilt()
        {
            var live = _live;
            if (live == null || live.Badge == null || MotionSettings.Reduced)
            {
                return;
            }
            _liveTiltTarget = Mathf.MoveTowards(_liveTiltTarget, 0f, 60f * Time.unscaledDeltaTime);
            _liveTilt = Mathf.Lerp(_liveTilt, _liveTiltTarget, Mathf.Clamp01(12f * Time.unscaledDeltaTime));
            live.Badge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _liveTilt);
        }

        private void BeginLiveDrag(int index, PointerEventData eventData)
        {
            var badge = _rowBadges[index];
            var rect = badge.rectTransform;
            var live = new LiveDrag { Badge = badge, From = index, Target = index, RestScale = rect.localScale };
            for (int i = 0; i < _rowBadges.Count; i++)
            {
                live.Centers.Add(_rowBadges[i] != null ? (Vector2)_rowBadges[i].rectTransform.localPosition : Vector2.zero);
                if (i != index && _rowBadges[i] != null) { live.Others.Add(_rowBadges[i]); }
            }
            // The gap keeps the grabbed fertilizer's cell while it is away.
            live.Gap = UIFactory.CreateUIObject("DragGap", _rowsContainer);
            live.Gap.sizeDelta = rect.sizeDelta;
            live.Gap.SetSiblingIndex(rect.GetSiblingIndex());
            // Explicit == null (Unity's own check): in the Editor GetComponent returns a fake non-null object when the component is missing, which ?? would let through.
            var layoutElement = badge.GetComponent<LayoutElement>();
            if (layoutElement == null) { layoutElement = badge.gameObject.AddComponent<LayoutElement>(); }
            layoutElement.ignoreLayout = true;
            var group = badge.GetComponent<CanvasGroup>();
            if (group == null) { group = badge.gameObject.AddComponent<CanvasGroup>(); }
            group.blocksRaycasts = false;
            rect.SetAsLastSibling();
            if (!MotionSettings.Reduced)
            {
                rect.localScale = live.RestScale * DragLiftScale;
            }
            // "Held" look: a golden glow around the fertilizer and a deeper shadow under it (both still, so they stay with reduced motion).
            live.Glow = badge.gameObject.AddComponent<Outline>();
            live.Glow.effectColor = HeldGlow;
            live.Glow.effectDistance = new Vector2(3f, -3f);
            live.Shadow = badge.gameObject.AddComponent<Shadow>();
            live.Shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            live.Shadow.effectDistance = new Vector2(4f, -12f);
            _liveTilt = 0f;
            _liveTiltTarget = 0f;
            if (_tooltip != null) { _tooltip.Hide(); }
            for (int c = 0; c < _rootLinks.Count; c++)
            {
                if (_rootLinks[c] != null) { _rootLinks[c].SetActive(false); }
            }
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_rowsContainer, eventData.position, eventData.pressEventCamera, out var world);
            live.GrabOffset = rect.position - world;
            _live = live;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsContainer);
        }

        /// <summary>The pointer moved during a drag: the held fertilizer follows it, and the gap moves to the slot nearest the pointer.</summary>
        public void OnBadgeDrag(PointerEventData eventData)
        {
            var live = _live;
            if (live == null || live.Badge == null)
            {
                return;
            }
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_rowsContainer, eventData.position, eventData.pressEventCamera, out var world);
            live.Badge.rectTransform.position = world + live.GrabOffset;
            // Lean with the horizontal motion.
            _liveTiltTarget = Mathf.Clamp(_liveTiltTarget - eventData.delta.x * 0.5f, -MaxDragTilt, MaxDragTilt);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rowsContainer, eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }
            int nearest = live.Target;
            float best = float.MaxValue;
            for (int i = 0; i < live.Centers.Count; i++)
            {
                float distance = (live.Centers[i] - local).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            if (nearest == live.Target)
            {
                return;
            }
            live.Target = nearest;
            // The gap sits where the held fertilizer would land: before the one now at that slot, or after the last.
            live.Gap.SetAsLastSibling();
            if (nearest < live.Others.Count && live.Others[nearest] != null)
            {
                live.Gap.SetSiblingIndex(live.Others[nearest].rectTransform.GetSiblingIndex());
            }
            else if (live.Others.Count > 0 && live.Others[live.Others.Count - 1] != null)
            {
                live.Gap.SetSiblingIndex(live.Others[live.Others.Count - 1].rectTransform.GetSiblingIndex() + 1);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsContainer);
            SfxManager.Play(SfxId.CarouselTick);
        }

        /// <summary>Ends a live drag: back in the grid at the gap's place; if the slot changed, asks for the move.</summary>
        private void EndLiveDrag()
        {
            var live = _live;
            _live = null;
            if (live == null)
            {
                return;
            }
            if (live.Badge != null)
            {
                var rect = live.Badge.rectTransform;
                rect.localScale = live.RestScale;
                rect.localRotation = Quaternion.identity;
                if (live.Glow != null) { Destroy(live.Glow); }
                if (live.Shadow != null) { Destroy(live.Shadow); }
                var layoutElement = live.Badge.GetComponent<LayoutElement>();
                if (layoutElement != null) { layoutElement.ignoreLayout = false; }
                var group = live.Badge.GetComponent<CanvasGroup>();
                if (group != null) { group.blocksRaycasts = true; }
                if (live.Gap != null) { rect.SetSiblingIndex(live.Gap.GetSiblingIndex()); }
            }
            if (live.Gap != null) { Destroy(live.Gap.gameObject); }
            for (int c = 0; c < _rootLinks.Count; c++)
            {
                if (_rootLinks[c] != null) { _rootLinks[c].SetActive(true); }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsContainer);
            if (live.Target != live.From && MoveRequested != null)
            {
                MoveRequested(live.From, live.Target);
            }
        }

        /// <summary>Fired by ModifierBadgeDragHandler.OnDrop when a drag lands on badge <paramref name="targetIndex"/>. Clears the drag state immediately so the OnEndDrag cleanup that follows is always a safe no-op, even if Refresh() has already torn down every row.</summary>
        public void OnBadgeDrop(int targetIndex)
        {
            if (!_interactable || _draggingRowIndex < 0 || _draggingRowIndex == targetIndex || _live != null)
            {
                return;
            }

            int source = _draggingRowIndex;
            _draggingRowIndex = -1;
            if (MoveRequested != null)
            {
                MoveRequested(source, targetIndex);
            }
        }

        /// <summary>Always fires after a drag ends, whether or not it landed on a valid drop target. Restores opacity if the drag didn't result in a move, and cleans up a leftover drop-target preview if the pointer was released outside the panel.</summary>
        public void OnBadgeEndDrag()
        {
            EndLiveDrag();
            if (_draggingRowIndex >= 0 && _draggingRowIndex < _rowBadges.Count)
            {
                ApplyRestingColor(_draggingRowIndex);
            }
            _draggingRowIndex = -1;

            if (_dropTargetRowIndex >= 0 && _dropTargetRowIndex < _rowBadges.Count)
            {
                int target = _dropTargetRowIndex;
                _dropTargetRowIndex = -1;
                ApplyRestingColor(target);
            }
        }

        /// <summary>A row's resting color: tinted gold while it's the live drop-target preview during a drag, lightened toward white while it's the tap-armed selection, otherwise its true base color.</summary>
        private Color RestingColor(int i)
        {
            var baseColor = _rowBaseColors[i];
            if (i == _dropTargetRowIndex)
            {
                return Color.Lerp(baseColor, VisualDefaults.GoldenColor, DropTargetTintAmount);
            }
            return i == _selectedRowIndex ? Color.Lerp(baseColor, Color.white, SelectedTintAmount) : baseColor;
        }

        /// <summary>Snaps a row's badge to its current RestingColor — skipped while a pulse is actively animating that same row's color, so this never fights a pulse mid-flight.</summary>
        private void ApplyRestingColor(int i)
        {
            if (i < 0 || i >= _rowBadges.Count || _rowPulseCoroutines[i] != null)
            {
                return;
            }
            _rowBadges[i].color = RestingColor(i);
        }

        /// <summary>
        /// The screen anchor of the badge showing <paramref name="id"/> at
        /// row <paramref name="occurrenceIndex"/> (the modifier's position in
        /// RunManager.ActiveModifiers, see <see cref="ScoreEvent.TriggeringModifierIndex"/>).
        /// Falls back to the first badge showing <paramref name="id"/> if the
        /// index is stale, or null if none is active. Needed because Copieur
        /// can make the same id occupy more than one row.
        /// </summary>
        public RectTransform GetBadgeTransform(ModifierId id, int occurrenceIndex)
        {
            if (occurrenceIndex >= 0 && occurrenceIndex < _rowIds.Count && _rowIds[occurrenceIndex] == id)
            {
                return _rowBadges[occurrenceIndex].rectTransform;
            }
            for (int i = 0; i < _rowIds.Count; i++)
            {
                if (_rowIds[i] == id)
                {
                    return _rowBadges[i].rectTransform;
                }
            }
            return null;
        }

        /// <summary>Flashes the badge (and gives its row a small scale pulse) of every row currently showing <paramref name="id"/> — called when that modifier actually scores on a placement.</summary>
        public void Pulse(ModifierId id)
        {
            for (int i = 0; i < _rowIds.Count; i++)
            {
                if (_rowIds[i] == id)
                {
                    PulseRow(i);
                }
            }
        }

        private void PulseRow(int rowIndex)
        {
            // Stop any pulse already running on this row first, so two
            // coroutines never animate the same Image at once.
            if (_rowPulseCoroutines[rowIndex] != null)
            {
                StopCoroutine(_rowPulseCoroutines[rowIndex]);
            }
            _rowPulseCoroutines[rowIndex] = StartCoroutine(PulseBadge(rowIndex));
        }

        private IEnumerator PulseBadge(int rowIndex)
        {
            var badge = _rowBadges[rowIndex];
            // The row's CURRENT resting color (its true base, lightened if
            // it's the tap-armed selection — see RestingColor) rather than
            // always its true base — otherwise a pulse firing while a badge
            // is selected would restore it to the un-tinted color at the
            // end, silently discarding the selection highlight.
            var baseColor = RestingColor(rowIndex);
            var highlightColor = Color.white;
            var rt = badge.rectTransform;
            float t = 0f;
            while (t < PulseDuration)
            {
                // The panel can be refreshed (rows destroyed/rebuilt) mid-pulse
                // if a new draft/round starts right as this plays — bail out
                // rather than touching a destroyed row.
                if (badge == null)
                {
                    yield break;
                }

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                badge.color = Color.Lerp(highlightColor, baseColor, p);
                yield return null;
            }

            if (badge != null)
            {
                rt.localScale = Vector3.one;
                badge.color = baseColor;
            }
            _rowPulseCoroutines[rowIndex] = null;
        }

        /// <summary>A freshly built badge pops in from nothing with a slight overshoot, same "ease past peak, settle back" shape as PulseBadge above — called right after Refresh adds a genuinely new row (see the match/diff at the top of Refresh), never on one that's merely moved from a reorder.</summary>
        private IEnumerator PlayAddedBadge(RectTransform rect)
        {
            rect.localScale = Vector3.zero;
            float t = 0f;
            while (t < AddedPopDuration)
            {
                if (rect == null)
                {
                    yield break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / AddedPopDuration);
                float scale = p < AddedPopPeakFraction
                    ? Mathf.Lerp(0f, AddedPopPeakScale, p / AddedPopPeakFraction)
                    : Mathf.Lerp(AddedPopPeakScale, 1f, (p - AddedPopPeakFraction) / (1f - AddedPopPeakFraction));
                rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// A row that Refresh's diff found no match for in the new list
        /// (sold, or lost to some other removal) shrinks and fades away in
        /// place instead of just vanishing the instant the grid rebuilds.
        /// Reparented onto _root (worldPositionStays: true keeps its exact
        /// screen position/size, no coordinate math needed) so it plays out
        /// above the already-rebuilt grid without fighting the
        /// GridLayoutGroup, which is free to reflow every surviving badge
        /// into its new position immediately underneath it. A CanvasGroup
        /// fades the whole badge (icon/label/index diamond included, not
        /// just its own background Image) as one unit and blocks it from
        /// absorbing any stray hover/click/drag on its way out — it's
        /// already gone from _rowIds/_rowBadges, so any of those would
        /// resolve against a now-unrelated index in the rebuilt list.
        /// </summary>
        private void PlayRemovedBadge(Image badge)
        {
            if (badge == null)
            {
                return;
            }
            var rect = badge.rectTransform;
            rect.SetParent(_root, worldPositionStays: true);
            rect.SetAsLastSibling();
            // The fertilizer's flight into its slot already left a CanvasGroup on the badge: reuse it.
            var canvasGroup = badge.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = badge.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            StartCoroutine(RemovedBadgeRoutine(rect, canvasGroup));
        }

        private IEnumerator ShowHintAfterRemoval()
        {
            yield return new WaitForSeconds(RemovedShrinkDuration);
            if (_emptyHint != null && _heldModifiers.Count == 0)
            {
                _emptyHint.gameObject.SetActive(true);
            }
        }

        private IEnumerator RemovedBadgeRoutine(RectTransform rect, CanvasGroup canvasGroup)
        {
            Vector3 startScale = rect.localScale;
            float t = 0f;
            while (t < RemovedShrinkDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / RemovedShrinkDuration);
                rect.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                canvasGroup.alpha = 1f - p;
                yield return null;
            }
            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }
    }
}
