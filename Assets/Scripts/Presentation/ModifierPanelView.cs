using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
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
        private const float BottomPadding = 16f;
        private const int DisplayRows = 5; // 2 columns x 5 rows = 10 modifiers
        private const float PanelHeight = HeaderHeight + DisplayRows * BadgeSize + (DisplayRows - 1) * BadgeSpacing + BottomPadding;
        private const float PulseDuration = 0.5f;
        private const float PulsePeakScale = 1.1f;
        private const float PulsePeakFraction = 0.3f;
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
            var panel = UIFactory.CreateSlicedImage(parent, "ModifierPanel", UISprites.ModifierPanelBackground);
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

            // "Modifiers" sits inside panel_bg's own header band near the
            // top of the card (on explicit request) — panel_bg's header
            // band art is exactly what this whole detour (card_bg_2 +
            // banner + floating text) was trying to work around, but with a
            // static panel height it never stretches/distorts, so the plain
            // original approach is safe again.
            var header = UIFactory.CreateText(_root, "Header", "Modifiers", 30, UITheme.TextPrimary, TextAnchor.UpperCenter);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            header.rectTransform.sizeDelta = new Vector2(PanelWidth - 16f, 48f);

            _rowsContainer = UIFactory.CreateUIObject("Rows", _root);
            _rowsContainer.anchorMin = new Vector2(0.5f, 1f);
            _rowsContainer.anchorMax = new Vector2(0.5f, 1f);
            _rowsContainer.pivot = new Vector2(0.5f, 1f);
            _rowsContainer.anchoredPosition = new Vector2(0f, -HeaderHeight);

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
            for (int i = _rowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowsContainer.GetChild(i).gameObject);
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

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var badge = ModifierBadgeFactory.Create(_rowsContainer, ModifierCatalog.Get(activeModifiers[i]), BadgeSize, _tooltip, _usageCountProvider, progressiveStateProvider: _progressiveStateProvider);
                _rowIds.Add(activeModifiers[i]);
                _rowBadges.Add(badge);
                _rowBaseColors.Add(badge.color);
                _rowPulseCoroutines.Add(null);

                // Visual position number (on explicit request: "il va
                // falloir les numéroter visuellement aussi" — scoring order
                // now follows this exact list order, see
                // PlacementResult.Mult, so the player needs to see it to
                // arrange x-modifiers after +modifiers). Top-left corner,
                // outlined for legibility over any badge color/icon
                // underneath.
                var indexLabel = UIFactory.CreateText(badge.transform, "Index", (i + 1).ToString(), Mathf.RoundToInt(IndexLabelSize), UITheme.TextPrimary);
                indexLabel.raycastTarget = false;
                indexLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
                indexLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
                indexLabel.rectTransform.pivot = new Vector2(0f, 1f);
                indexLabel.rectTransform.anchoredPosition = new Vector2(2f, -2f);
                indexLabel.rectTransform.sizeDelta = new Vector2(26f, 26f);
                var indexOutline = indexLabel.gameObject.AddComponent<Outline>();
                indexOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                indexOutline.effectDistance = new Vector2(1.5f, -1.5f);

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
    }
}
