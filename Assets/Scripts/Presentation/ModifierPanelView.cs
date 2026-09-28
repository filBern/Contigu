using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Persistent panel pinned to the left edge of the screen, listing the
    /// player's currently active modifiers as a 2-column grid of bare badges
    /// (no per-row card background). The panel uses a STATIC height sized
    /// for exactly 10 modifiers (2x5) — on explicit request, after several
    /// dynamic-resize approaches each ran into their own 9-slice rendering
    /// glitch at one panel height or another (see README). Active modifiers
    /// are otherwise unlimited (RunManager has no cap), so beyond 10 the
    /// grid simply keeps growing past the card's own visible area.
    /// A readout refreshed by the caller whenever the active set changes, plus
    /// a <see cref="Pulse"/> effect the caller triggers on a badge whenever it
    /// actually scores points on a placement.
    /// </summary>
    public sealed class ModifierPanelView : MonoBehaviour
    {
        private const float PanelWidth = 230f;
        private const float BadgeSize = 90f;
        private const float BadgeSpacing = 10f;
        private const float HeaderHeight = 58f;
        // Gap between the header band and the first badge row (explicit
        // report, after seeing the two flush against each other with no
        // breathing room: "il y a un petit overlap") — the header band's
        // own thick outline and the first row's badges' own thick outlines
        // used to sit right on top of each other with zero space between.
        private const float HeaderRowGap = 12f;
        private const float BottomPadding = 16f;
        private const int DisplayRows = 5; // 2 columns x 5 rows = 10 modifiers
        private const float PanelHeight = HeaderHeight + HeaderRowGap + DisplayRows * BadgeSize + (DisplayRows - 1) * BadgeSpacing + BottomPadding;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;
        // Add/remove feedback (explicit request: "Lorsqu'un modifier est
        // ajouté ou retiré de la liste il faut une animation") — a newly
        // added badge pops in from nothing with a slight overshoot (same
        // "ease past 1x, settle back" shape as PulsePeakScale above, just
        // starting from 0 instead of 1 since there's no prior size to
        // return to); a removed one shrinks and fades out in place instead
        // of just vanishing the instant Refresh rebuilds the grid.
        private const float AddedPopDuration = 0.35f;
        private const float AddedPopPeakScale = 1.15f;
        private const float AddedPopPeakFraction = 0.5f;
        private const float RemovedShrinkDuration = 0.3f;
        // How much a tap-selected badge lightens toward white (same "just a
        // touch lighter" language as HandView's own selected-slot tint,
        // never a border/frame — on the same earlier explicit request that
        // ruled those out: "j'aime pas le cadre de sélection... juste
        // mettre légèrement plus clair") — and how transparent a badge
        // goes while being dragged, so its own row still reads through as
        // a visible drop target underneath the cursor.
        private const float SelectedTintAmount = 0.35f;
        private const float DraggingAlpha = 0.4f;
        private const float IndexLabelSize = 22f;

        private RectTransform _root;
        private RectTransform _rowsContainer;
        private TooltipView _tooltip;
        private System.Func<ModifierId, int> _usageCountProvider;
        private System.Func<ModifierId, string> _progressiveStateProvider;
        private bool _interactable = true;

        // Tap-to-swap: the first-tapped badge's row, armed and waiting for
        // a second tap on a different badge to swap with — -1 when nothing
        // is armed. Drag-to-move: the row currently being dragged, -1 when
        // no drag is in flight. Independent of each other (a tap-selection
        // survives a drag elsewhere) — see OnBadgeClicked/OnBadgeBeginDrag.
        private int _selectedRowIndex = -1;
        private int _draggingRowIndex = -1;

        // Which row the pointer currently rests over, -1 when none — feeds
        // the "sell the modifier under the cursor" shortcut (explicit
        // request: "Le joueur devrait pouvoir sell modifier lorsqu'il hover
        // dessus"). See OnBadgeHoverEnter/Exit and HoveredRowIndex.
        private int _hoveredRowIndex = -1;

        /// <summary>The row index the pointer currently hovers, or -1 if none/the panel is blocked (see SetInteractable) — GameBootstrap reads this on its sell key press rather than tracking hover itself.</summary>
        public int HoveredRowIndex
        {
            get { return _interactable ? _hoveredRowIndex : -1; }
        }

        /// <summary>Fired when the player taps two different badges in a row — RunManager.SwapModifiers(a, b) is the expected response, followed by a Refresh.</summary>
        public event System.Action<int, int> SwapRequested;

        /// <summary>Fired when the player drags a badge and drops it onto another — RunManager.MoveModifier(from, to) is the expected response, followed by a Refresh.</summary>
        public event System.Action<int, int> MoveRequested;

        // Parallel to the active-modifiers list passed to the last Refresh —
        // lets Pulse(id)/GetBadgeTransform find the badge(s) currently
        // showing that modifier. Copieur ("Mimic") duplicates an existing
        // modifier id in RunManager.ActiveModifiers rather than being its
        // own distinct id, so a given id CAN appear more than once here —
        // see GetBadgeTransform's own doc comment for how that's resolved.
        private readonly List<ModifierId> _rowIds = new List<ModifierId>();
        private readonly List<Image> _rowBadges = new List<Image>();
        // Each row's TRUE resting color, captured once when its badge is
        // built — PulseBadge lerps against THIS, never against the badge's
        // own live .color, because that can be mid-lerp from a still-running
        // earlier pulse on the same badge (see _rowPulseCoroutines).
        private readonly List<Color> _rowBaseColors = new List<Color>();
        // The currently-running pulse coroutine for each row, if any — a
        // modifier that scores more than once in a single placement (e.g. a
        // per-cell bonus with several qualifying cells) fires Pulse(id)
        // once per event, and without this, a new pulse starting before the
        // previous one finished would capture the badge's CURRENT (already
        // part-way-to-white) color as its own "base" and restore THAT
        // instead of the true color when it ends — each overlapping pulse
        // nudging the badge permanently whiter. Fixed on explicit report
        // ("des modifiers qui deviennent progressivement plus blanc à force
        // d'être utilisé"): a new pulse now stops any pulse already running
        // on that same row first, so at most one ever animates a row's
        // color at a time and _rowBaseColors' true value is always what
        // gets restored.
        private readonly List<Coroutine> _rowPulseCoroutines = new List<Coroutine>();

        /// <summary>
        /// <paramref name="usageCountProvider"/> (e.g. RunManager.GetModifierUsageCount)
        /// lets each badge's tooltip show how many times it's fired this
        /// run — see ModifierBadgeFactory. <paramref name="progressiveStateProvider"/>
        /// (RunManager.GetProgressiveModifierStateText) lets a progressive/
        /// incremental modifier's tooltip show its current live state (on
        /// explicit request, e.g. "Currently x2.3") — null for every other
        /// modifier. Only this owned-modifiers panel passes either one; the
        /// shop's own cards skip the tooltip entirely (see ShopView).
        /// </summary>
        public RectTransform Build(Transform parent, TooltipView tooltip, System.Func<ModifierId, int> usageCountProvider, System.Func<ModifierId, string> progressiveStateProvider = null)
        {
            _tooltip = tooltip;
            _usageCountProvider = usageCountProvider;
            _progressiveStateProvider = progressiveStateProvider;
            // Own flat geometric panel (a plain cream rectangle plus a
            // mustard header band) instead of the "Colorful UI" pack's
            // "panel_bg" sprite — on explicit request, after seeing it next
            // to the new DA ("refaire l'asset de la liste de modifiers
            // toi-même avec la bonne palette de couleur et des formes
            // géométriques"): that sprite's baked-in cyan art doesn't
            // follow UITheme at all (CreateSlicedImage tints sprites white,
            // i.e. not at all), so it kept showing its own colors no matter
            // what the rest of the reskin changed.
            var panel = UIFactory.CreatePanel(parent, "ModifierPanel", UITheme.Panel);
            UIFactory.AddThickOutline(panel, UITheme.Border);
            _root = panel.rectTransform;
            // Vertically centered, STATIC size — the panel never resizes at
            // runtime anymore (see the class doc comment), so there's no
            // longer a header-jump or 9-slice-at-small-size risk from a
            // center pivot the way there was while sizeDelta.y changed
            // every Refresh.
            _root.anchorMin = new Vector2(0f, 0.5f);
            _root.anchorMax = new Vector2(0f, 0.5f);
            _root.pivot = new Vector2(0f, 0.5f);
            _root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _root.anchoredPosition = new Vector2(40f, 0f);

            // Flat mustard header band standing in for panel_bg's own baked
            // header art — same idea, built from a plain rectangle in the
            // new DA's accent color instead of pre-made sprite art.
            var headerBand = UIFactory.CreatePanel(_root, "HeaderBand", UITheme.ButtonSelected);
            headerBand.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerBand.rectTransform.anchorMax = new Vector2(1f, 1f);
            headerBand.rectTransform.pivot = new Vector2(0.5f, 1f);
            headerBand.rectTransform.anchoredPosition = Vector2.zero;
            headerBand.rectTransform.sizeDelta = new Vector2(0f, HeaderHeight);

            var header = UIFactory.CreateText(headerBand.transform, "Header", "Modifiers", 30, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            UIFactory.StretchFull(header.rectTransform);

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

            return _root;
        }

        public void Refresh(IReadOnlyList<ModifierId> activeModifiers)
        {
            // Snapshotted BEFORE the rebuild below so add/remove feedback
            // (explicit request: "Lorsqu'un modifier est ajouté ou retiré
            // de la liste il faut une animation") can diff against what
            // was actually on screen a moment ago, rather than just
            // wiping and redrawing with no transition every single time —
            // including on a plain reorder (swap/drag), which must NOT
            // read as anything being added or removed.
            var previousIds = new List<ModifierId>(_rowIds);
            var previousBadges = new List<Image>(_rowBadges);

            // Greedy multiset match: each NEW entry claims the first
            // still-unclaimed OLD row with the same id. Matching by VALUE
            // (not position) is what makes a reorder a no-op here — the
            // same badges just claim each other regardless of where they
            // moved to. Duplicate ids (Copieur) are handled correctly too,
            // since each occurrence can only claim one specific old row.
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

            // Every unclaimed OLD row is a genuine removal — animated away
            // instead of destroyed outright; every claimed one is either
            // staying or just moving, so it's safe to destroy immediately
            // since a fresh badge for it is about to be built below anyway.
            for (int j = 0; j < previousBadges.Count; j++)
            {
                if (oldClaimed[j])
                {
                    Destroy(previousBadges[j].gameObject);
                }
                else
                {
                    PlayRemovedBadge(previousBadges[j]);
                }
            }

            _rowIds.Clear();
            _rowBadges.Clear();
            _rowBaseColors.Clear();
            _rowPulseCoroutines.Clear();
            // Both refer to rows that no longer exist once the list is
            // rebuilt below — a stale index here would either point at
            // nothing or, worse, at some UNRELATED modifier that now
            // happens to sit at that same position.
            _selectedRowIndex = -1;
            _draggingRowIndex = -1;
            _hoveredRowIndex = -1;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var badge = ModifierBadgeFactory.Create(_rowsContainer, ModifierCatalog.Get(activeModifiers[i]), BadgeSize, _tooltip, _usageCountProvider, progressiveStateProvider: _progressiveStateProvider);
                _rowIds.Add(activeModifiers[i]);
                _rowBadges.Add(badge);
                _rowBaseColors.Add(badge.color);
                _rowPulseCoroutines.Add(null);
                if (newIsAdded[i])
                {
                    StartCoroutine(PlayAddedBadge(badge.rectTransform));
                }

                // Visual position number (on explicit request: "il va
                // falloir les numéroter visuellement aussi" — scoring order
                // now follows this exact list order, see
                // PlacementResult.Mult, so the player needs to see it to
                // arrange x-modifiers after +modifiers). Top-left corner, on
                // a small rotated-square "diamond" backdrop (explicit
                // request: "mettre un petit losange sous le chiffre" — same
                // motif as HudView's Lueur icon) instead of the dark text
                // Outline this used before, for legibility over any badge
                // color/icon underneath.
                var indexBg = UIFactory.CreatePanel(badge.transform, "IndexBg", VisualDefaults.GoldenColor);
                indexBg.rectTransform.anchorMin = new Vector2(0f, 1f);
                indexBg.rectTransform.anchorMax = new Vector2(0f, 1f);
                indexBg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                indexBg.rectTransform.anchoredPosition = new Vector2(15f, -15f);
                indexBg.rectTransform.sizeDelta = new Vector2(30f, 30f);
                indexBg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

                var indexLabel = UIFactory.CreateText(badge.transform, "Index", (i + 1).ToString(), Mathf.RoundToInt(IndexLabelSize), UITheme.TextPrimary);
                indexLabel.raycastTarget = false;
                indexLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
                indexLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
                indexLabel.rectTransform.pivot = new Vector2(0f, 1f);
                indexLabel.rectTransform.anchoredPosition = new Vector2(2f, -2f);
                indexLabel.rectTransform.sizeDelta = new Vector2(26f, 26f);

                // Drag-and-drop OR tap-tap swap reordering (on explicit
                // request: "qu'on puisse les réorganiser avec un drag and
                // drop OU avec un tap") — same forwarding pattern as
                // HandSlotDragHandler.
                var dragHandler = badge.gameObject.AddComponent<ModifierBadgeDragHandler>();
                dragHandler.Init(this, i);
            }
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
                return;
            }

            int armed = _selectedRowIndex;
            _selectedRowIndex = -1;
            ApplyRestingColor(armed);

            if (armed == index)
            {
                return;
            }

            if (SwapRequested != null)
            {
                SwapRequested(armed, index);
            }
        }

        /// <summary>Marks <paramref name="index"/> as the row currently under the pointer — see HoveredRowIndex. Not gated on _interactable here (that's checked by the getter instead), so hover tracking itself never gets out of sync with what the pointer is actually over.</summary>
        public void OnBadgeHoverEnter(int index)
        {
            _hoveredRowIndex = index;
        }

        /// <summary>Clears the hover only if it's still THIS row — a fast pointer move can fire the next badge's OnPointerEnter before this one's OnPointerExit, and blindly clearing here would wipe out that newer hover.</summary>
        public void OnBadgeHoverExit(int index)
        {
            if (_hoveredRowIndex == index)
            {
                _hoveredRowIndex = -1;
            }
        }

        /// <summary>Drag start — fades the dragged badge so its own row still reads as an available drop target underneath the cursor, and disarms any tap-selection in progress (avoids a confusing "armed AND dragging" combined state).</summary>
        public void OnBadgeBeginDrag(int index)
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
            }

            _draggingRowIndex = index;
            var c = _rowBadges[index].color;
            _rowBadges[index].color = new Color(c.r, c.g, c.b, DraggingAlpha);
        }

        /// <summary>Fired by ModifierBadgeDragHandler.OnDrop when a drag lands on badge <paramref name="targetIndex"/> — fires <see cref="MoveRequested"/> immediately and clears the drag state right away, so the OnEndDrag cleanup that follows (see below) is always a safe no-op even if the caller's Refresh() already tore down every row by then.</summary>
        public void OnBadgeDrop(int targetIndex)
        {
            if (!_interactable || _draggingRowIndex < 0 || _draggingRowIndex == targetIndex)
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

        /// <summary>Always fires after a drag ends, whether or not it landed on a valid drop target — restores the dragged badge's normal opacity when the drag didn't result in a move (OnBadgeDrop above already cleared _draggingRowIndex when it did, making this a no-op).</summary>
        public void OnBadgeEndDrag()
        {
            if (_draggingRowIndex < 0 || _draggingRowIndex >= _rowBadges.Count)
            {
                _draggingRowIndex = -1;
                return;
            }

            ApplyRestingColor(_draggingRowIndex);
            _draggingRowIndex = -1;
        }

        /// <summary>A row's normal resting color — its true base color, lightened toward white while it's the tap-armed selection (see <see cref="SelectedTintAmount"/>).</summary>
        private Color RestingColor(int i)
        {
            var baseColor = _rowBaseColors[i];
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
        /// row <paramref name="occurrenceIndex"/> — the modifier's position
        /// within RunManager.ActiveModifiers, i.e. the same index GridManager's
        /// own activeModifiers[i] loop used when it produced this event (see
        /// <see cref="ScoreEvent.TriggeringModifierIndex"/>). Rows are built
        /// from that exact list in that exact order (see <see cref="Refresh"/>),
        /// so the index lines up directly; falls back to the first badge
        /// showing <paramref name="id"/> if it doesn't (a stale index from a
        /// Refresh that happened in between), or null if none is active.
        /// Copieur ("Mimic") duplicates an existing id rather than being its
        /// own, so the SAME id can occupy more than one row — without this
        /// index every copy's popup would always land on the very first row
        /// showing that id instead of the specific copy that actually scored
        /// (on explicit report: "le texte de bonus est sur le modifier copié
        /// et non la copie créé").
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
            // A modifier that scores more than once in one placement (e.g. a
            // per-cell bonus with several qualifying cells) pulses its row
            // once per event — stop any pulse already running on it first,
            // rather than letting two coroutines animate the same Image at
            // once (see _rowPulseCoroutines' own doc comment for why that
            // used to permanently whiten the badge).
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
            var rect = badge.rectTransform;
            rect.SetParent(_root, worldPositionStays: true);
            rect.SetAsLastSibling();
            var canvasGroup = badge.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            StartCoroutine(RemovedBadgeRoutine(rect, canvasGroup));
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
