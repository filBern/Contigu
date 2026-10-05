using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Renders the 8x8 <see cref="GridManager"/> as a grid of clickable cells and
    /// previews the footprint of the currently selected hand piece on hover.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        public event Action<int, int> CellClicked;

        /// <summary>
        /// Fires whenever the hovered footprint's validity changes (including
        /// to "false" when hover leaves the grid entirely) — used by
        /// <see cref="HandView"/> to hide its drag ghost while it sits over a
        /// droppable spot, since the grid's own green/red footprint tint
        /// already shows that; the ghost only needs to be visible while the
        /// player hasn't found a valid spot yet.
        /// </summary>
        public event Action<bool> HoverValidityChanged;

        private GridManager _grid;
        private GridCellView[,] _cells;
        private PieceShape _selectedShape;
        private PieceColor? _selectedColor;
        private PieceTrait? _selectedTrait;
        private readonly List<Vector2Int> _hoveredFootprint = new List<Vector2Int>();
        // Separate from _hoveredFootprint — the line-clear preview covers
        // whichever WHOLE rows/columns would complete, which is mostly
        // pre-existing cells outside the piece's own footprint (see
        // OnCellHoverEnter/ClearHover).
        private readonly List<Vector2Int> _lineClearPreviewCells = new List<Vector2Int>();
        private TooltipView _tooltip;
        private RectTransform _container;
        private float _cellStride;

        public RectTransform Build(Transform parent, GridManager grid, float cellSize, TooltipView tooltip)
        {
            _grid = grid;
            _tooltip = tooltip;
            _cells = new GridCellView[GridManager.Size, GridManager.Size];

            var container = UIFactory.CreateUIObject("GridContainer", parent);
            _container = container;
            // A visible gap between tiles now that each one is its own
            // card-shaped sprite rather than a flat color square touching its
            // neighbors (on explicit request: "une petite margin entre chaque
            // tuile") — was 3f, barely readable as a margin at this scale.
            float spacing = 6f;
            _cellStride = cellSize + spacing;
            float total = GridManager.Size * cellSize + (GridManager.Size - 1) * spacing;
            container.sizeDelta = new Vector2(total, total);

            var layout = container.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(spacing, spacing);
            layout.startCorner = GridLayoutGroup.Corner.LowerLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridManager.Size;

            // Invisible, full-grid-size raycast target directly on the
            // container itself — a child cell's own Image always wins the
            // raycast over its ancestor's when the pointer is precisely on
            // that cell, so this only ever catches a pointer event that
            // misses every cell: the few-pixel spacing gap between adjacent
            // tiles, which used to be a dead zone (on explicit report: "je
            // suis a un ou deux pixel de la case, je ne peux pas déposer de
            // pièce... rajoute une règle pour lorsque le curseur n'est pas
            // sur une case"). See GridGapCatcher/OnGapPointerEnter/
            // OnGapPointerClick for how a gap point resolves to its nearest
            // cell instead of just doing nothing.
            var gapCatcherImage = container.gameObject.AddComponent<Image>();
            gapCatcherImage.color = new Color(0f, 0f, 0f, 0f);
            var gapCatcher = container.gameObject.AddComponent<GridGapCatcher>();
            gapCatcher.Init(this);

            // Instantiated bottom row (y=0) first so LowerLeft start corner
            // produces an on-screen layout where y increases upward.
            for (int y = 0; y < GridManager.Size; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    CreateCell(container, x, y);
                }
            }

            Refresh();
            return container;
        }

        private void CreateCell(Transform parent, int x, int y)
        {
            var cellGo = UIFactory.CreateUIObject("Cell_" + x + "_" + y, parent);
            var background = cellGo.gameObject.AddComponent<Image>();
            background.color = Color.white;
            // Sliced so VisualDefaults.TileSprite's rounded card border stays
            // sharp at cell size instead of being stretched (GridCellView.
            // ApplyState assigns the actual sprite/tint per cell state).
            background.type = Image.Type.Sliced;

            // Neutral overlay drawn on top of the flat color fill and below
            // every badge — gives a filled piece cell a distinct "block" look
            // instead of a flat rect. Only shown for an actually-filled cell
            // (see GridCellView.ApplyState); built as the first child so it
            // sits above Background but below every badge/label below.
            var fillTile = UIFactory.CreatePanel(cellGo, "FillTile", Color.white);
            UIFactory.StretchFull(fillTile.rectTransform);
            fillTile.gameObject.SetActive(false);

            // Gold border hugging the cell's own edges, shown on every cell
            // of a row/column that would clear if the currently-hovered
            // piece landed here (see GridManager.PreviewClearedLineCells) —
            // explicit request: "j'aimerais qu'on fasse un highlight de la
            // ligne qui serait cleared", refined on a follow-up request to
            // outline instead of wash the whole tile: "au lieu de highlight
            // la tuile au complet, est-ce qu'on peut highlight seulement le
            // contour". 4 thin bars rather than an Outline component (same
            // reasoning as ModifierCarouselView's own highlight frame — see
            // its BuildHighlightFrame) — most of a cleared line's cells are
            // pre-existing tiles that need to keep showing their own true
            // fill color underneath, so a full-cell tint (or anything that
            // relies on the base Graphic's alpha, like Outline/Shadow) would
            // either obscure or simply not render over them.
            var lineClearOverlay = BuildLineClearBorder(cellGo);

            var badgeGolden = UIFactory.CreatePanel(cellGo, "BadgeGolden", Color.yellow);
            UIFactory.SetAnchor(badgeGolden.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f));
            badgeGolden.rectTransform.pivot = new Vector2(0f, 1f);
            badgeGolden.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeGolden.rectTransform.anchoredPosition = new Vector2(3f, -3f);
            // Dark outline so the badge stays readable regardless of the cell's
            // fill color (a plain gold square can blend into a light piece color).
            var badgeGoldenOutline = badgeGolden.gameObject.AddComponent<Outline>();
            badgeGoldenOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeGoldenOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeGolden.gameObject.SetActive(false);

            // Persistent reminder of which deck upgrade originally enchanted
            // this cell (see Cell.OriginTrait) — top-right corner, the one
            // spot the other three badges (top-left/bottom-right/center)
            // leave free.
            var badgeTraitOrigin = UIFactory.CreatePanel(cellGo, "BadgeTraitOrigin", Color.cyan);
            UIFactory.SetAnchor(badgeTraitOrigin.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            badgeTraitOrigin.rectTransform.pivot = new Vector2(1f, 1f);
            badgeTraitOrigin.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeTraitOrigin.rectTransform.anchoredPosition = new Vector2(-3f, -3f);
            var badgeTraitOriginOutline = badgeTraitOrigin.gameObject.AddComponent<Outline>();
            badgeTraitOriginOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeTraitOriginOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeTraitOrigin.gameObject.SetActive(false);

            var badgeSpecial = UIFactory.CreatePanel(cellGo, "BadgeSpecial", Color.magenta);
            UIFactory.SetAnchor(badgeSpecial.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f));
            badgeSpecial.rectTransform.pivot = new Vector2(1f, 0f);
            badgeSpecial.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgeSpecial.rectTransform.anchoredPosition = new Vector2(-3f, 3f);
            var badgeSpecialOutline = badgeSpecial.gameObject.AddComponent<Outline>();
            badgeSpecialOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeSpecialOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgeSpecial.gameObject.SetActive(false);

            // "Poisoner" enemy mechanic (spec extension, explicit request:
            // "ajouter un petit peu d'autobattling" — see Cell.IsPoisoned/
            // RunManager.ResolvePoisonerShuffleEffect) — bottom-left corner,
            // the one spot the other three corner badges above leave free.
            var badgePoison = UIFactory.CreatePanel(cellGo, "BadgePoison", UITheme.Danger);
            UIFactory.SetAnchor(badgePoison.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f));
            badgePoison.rectTransform.pivot = new Vector2(0f, 0f);
            badgePoison.rectTransform.sizeDelta = new Vector2(16f, 16f);
            badgePoison.rectTransform.anchoredPosition = new Vector2(3f, 3f);
            var badgePoisonOutline = badgePoison.gameObject.AddComponent<Outline>();
            badgePoisonOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgePoisonOutline.effectDistance = new Vector2(1.5f, -1.5f);
            badgePoison.gameObject.SetActive(false);

            // Colorblind-mode shape (see ColorblindMode/
            // ColorblindShapeFactory) — centered, created before
            // InvalidMarker below so a transient hover-invalid preview
            // still draws on top of it and reads clearly; above every
            // corner badge above (no real overlap in practice, they never
            // reach the cell's center).
            var colorblindShape = UIFactory.CreatePanel(cellGo, "ColorblindShape", Color.black);
            UIFactory.SetAnchor(colorblindShape.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            colorblindShape.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            colorblindShape.rectTransform.sizeDelta = new Vector2(26f, 26f);
            colorblindShape.rectTransform.anchoredPosition = Vector2.zero;
            colorblindShape.gameObject.SetActive(false);

            // Solid marker shown instead of the color-icon preview while
            // hovering an invalid placement — a plain colored square (no
            // sprite needed), small and central so it reads as a clear "not
            // here" rather than blending into the red background tint alone.
            var invalidMarker = UIFactory.CreatePanel(cellGo, "InvalidMarker", UITheme.Danger);
            UIFactory.SetAnchor(invalidMarker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            invalidMarker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            invalidMarker.rectTransform.sizeDelta = new Vector2(20f, 20f);
            invalidMarker.rectTransform.anchoredPosition = Vector2.zero;
            var invalidMarkerOutline = invalidMarker.gameObject.AddComponent<Outline>();
            invalidMarkerOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            invalidMarkerOutline.effectDistance = new Vector2(1.5f, -1.5f);
            invalidMarker.gameObject.SetActive(false);

            // Spells out a modifier cell's effect ("+18", "x2", "x4") as text,
            // on top of the color badges above.
            var effectLabel = UIFactory.CreateText(cellGo, "EffectLabel", "", 11, Color.white, TextAnchor.LowerCenter);
            effectLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            effectLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            effectLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            effectLabel.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            effectLabel.rectTransform.sizeDelta = new Vector2(-4f, 14f);
            effectLabel.gameObject.SetActive(false);

            var cellView = cellGo.gameObject.AddComponent<GridCellView>();
            cellView.Init(this, x, y, background, fillTile, badgeGolden, badgeSpecial, badgePoison, invalidMarker, effectLabel, badgeTraitOrigin, colorblindShape, lineClearOverlay, _tooltip);
            _cells[x, y] = cellView;
        }

        /// <summary>4 thin bars, anchor-stretched along the cell's own edges, forming a hollow border — see the comment at its call site in CreateCell for why a wash/Outline don't work here. Toggled as one unit by GridCellView.SetLineClearPreview.</summary>
        private static GameObject BuildLineClearBorder(Transform parent)
        {
            const float BarThickness = 3f;
            var borderColor = new Color(VisualDefaults.GoldenColor.r, VisualDefaults.GoldenColor.g, VisualDefaults.GoldenColor.b, 0.9f);

            var wrapper = UIFactory.CreateUIObject("LineClearOverlay", parent);
            UIFactory.StretchFull(wrapper);

            var top = UIFactory.CreatePanel(wrapper, "Top", borderColor);
            top.raycastTarget = false;
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.pivot = new Vector2(0.5f, 1f);
            top.rectTransform.sizeDelta = new Vector2(0f, BarThickness);
            top.rectTransform.anchoredPosition = Vector2.zero;

            var bottom = UIFactory.CreatePanel(wrapper, "Bottom", borderColor);
            bottom.raycastTarget = false;
            bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottom.rectTransform.sizeDelta = new Vector2(0f, BarThickness);
            bottom.rectTransform.anchoredPosition = Vector2.zero;

            var left = UIFactory.CreatePanel(wrapper, "Left", borderColor);
            left.raycastTarget = false;
            left.rectTransform.anchorMin = new Vector2(0f, 0f);
            left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f);
            left.rectTransform.sizeDelta = new Vector2(BarThickness, 0f);
            left.rectTransform.anchoredPosition = Vector2.zero;

            var right = UIFactory.CreatePanel(wrapper, "Right", borderColor);
            right.raycastTarget = false;
            right.rectTransform.anchorMin = new Vector2(1f, 0f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(BarThickness, 0f);
            right.rectTransform.anchoredPosition = Vector2.zero;

            wrapper.gameObject.SetActive(false);
            return wrapper.gameObject;
        }

        /// <summary>Points this view at a different (e.g. freshly restarted) GridManager instance.</summary>
        public void Rebind(GridManager grid)
        {
            _grid = grid;
            _lastHoverOrigin = null;
            ClearHover();
            Refresh();
        }

        public void Refresh()
        {
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    _cells[x, y].ApplyState(_grid.GetCell(x, y));
                }
            }
        }

        /// <summary>
        /// Same as <see cref="Refresh"/>, except the cells at <paramref name="heldCells"/>
        /// are painted as still filled with their given color (and, if it had
        /// one, its pre-clear trait-origin badge — see <see
        /// cref="GridCellView.ApplyState"/>) instead of their actual
        /// (already-cleared) grid state — used to hold a just-completed line
        /// visually filled while its score is still playing out, before
        /// <see cref="ClearCellVisual"/> empties each cell in turn.
        /// <paramref name="deferredLockCells"/> are left entirely untouched
        /// (on explicit report: "Les X du boss devrait apparaitre après
        /// avoir calculé tous les points de la pièce posé, pas avant de
        /// comptabiliser les points") — a boss-round lock tick already
        /// flipped these cells' Cell.IsLocked in Core by the time this
        /// runs (GameBootstrap calls this right after RunManager.PlacePiece
        /// returns, before the score-popup sequence even starts), so
        /// redrawing them here would show their "X" obstacle instantly
        /// instead of only once GridView.Refresh() reveals them at the end
        /// of that sequence. Skipping them leaves their prior (still empty,
        /// unlocked) visual in place until then — safe since a lock tick
        /// only ever targets cells that were already empty. EXCEPT when a
        /// deferred cell is ALSO one of this same placement's own
        /// <paramref name="heldCells"/> (its own line clear just emptied a
        /// cell whose poison/lock ALSO happened to move elsewhere this same
        /// tick, via an auto-refill-triggered Shuffle) — that one still
        /// needs the normal held-filled treatment now, so its clear
        /// animation plays in step with its neighbors instead of freezing
        /// on a stale pre-clear draw until the final reveal.
        /// </summary>
        public void RefreshHoldingClearedCells(IReadOnlyList<Vector2Int> heldCells, IReadOnlyList<PieceColor> heldColors, IReadOnlyList<PieceTrait?> heldTraits, IReadOnlyList<Vector2Int> deferredLockCells = null)
        {
            var overrideColor = new Dictionary<Vector2Int, PieceColor>();
            var overrideTrait = new Dictionary<Vector2Int, PieceTrait?>();
            for (int i = 0; i < heldCells.Count; i++)
            {
                overrideColor[heldCells[i]] = heldColors[i];
                overrideTrait[heldCells[i]] = heldTraits[i];
            }

            var deferred = new HashSet<Vector2Int>();
            if (deferredLockCells != null)
            {
                for (int i = 0; i < deferredLockCells.Count; i++)
                {
                    deferred.Add(deferredLockCells[i]);
                }
            }

            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var pos = new Vector2Int(x, y);
                    // The held-fill override always wins over a deferred
                    // skip, even for a cell that happens to be in BOTH sets
                    // — e.g. a poisoned cell this placement's own line clear
                    // just emptied, where that SAME placement also auto-
                    // refilled the hand and triggered a Shuffle that moved
                    // Poisoner's poison elsewhere, releasing THIS cell's
                    // flag in the same tick (still the exact same recurring
                    // report: "la tuile empoisonné est présentement
                    // supprimé trop tôt visuellement"). Checking deferred
                    // FIRST used to skip such a cell entirely, leaving it
                    // frozen on whatever ClearHover had already drawn
                    // instead of the proper "held filled" look the rest of
                    // this same line clear gets — it needs its clear
                    // animation to play like its neighbors, not to also wait
                    // for the final end-of-sequence Refresh().
                    if (overrideColor.TryGetValue(pos, out var color))
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y), color, overrideTrait[pos]);
                    }
                    else if (deferred.Contains(pos))
                    {
                        continue;
                    }
                    else
                    {
                        _cells[x, y].ApplyState(_grid.GetCell(x, y));
                    }
                }
            }
        }

        /// <summary>Re-renders one cell from the grid's actual current state — used to visually empty a single cell of a line as it clears.</summary>
        public void ClearCellVisual(int x, int y)
        {
            RefreshCell(x, y);
        }

        private void RefreshCell(int x, int y)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].ApplyState(_grid.GetCell(x, y));
            }
        }

        /// <summary>Instantly sets every cell's malus alpha, with no animation (see GridCellView.SetMalusAlpha) — used right before/after Refresh() so FadeMalus can fade from/to a clean, pop-free state.</summary>
        public void SetMalusAlpha(float alpha)
        {
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    _cells[x, y].SetMalusAlpha(alpha);
                }
            }
        }

        /// <summary>
        /// Fades every currently-shown "malus" (Locker's locked-obstacle
        /// cells + Poisoner's poison badges) ONE CELL AT A TIME, each
        /// taking <paramref name="duration"/> seconds, rather than all of
        /// them moving together (explicit request: "Les fade in et fade
        /// out doivent se faire un élément a la fois ... et non les 3 a la
        /// fois") — on a Shuffle (manual or automatic), ALL of the board's
        /// existing enemy debuffs fade out one by one before the new hand
        /// is dealt, then whichever ones the Shuffle just left behind
        /// (possibly the exact same cells, possibly moved by a Locker/
        /// Poisoner On-Shuffle effect) fade back in the same way, rather
        /// than popping straight to their new state. This never decides
        /// WHICH cells are a malus — only how visible the ones that
        /// already are show up as — so GameBootstrap always calls it right
        /// around a Refresh() (see PlayManualShuffleSequence /
        /// PlayPlacementSequence's own handWasAboutToAutoRefill branch).
        /// The set of cells to fade is captured up front, once, before the
        /// first one starts — exactly the cells IsShowingMalus() finds true
        /// at that instant, same as a snapshot.
        /// </summary>
        public System.Collections.IEnumerator FadeMalus(float from, float to, float duration)
        {
            var malusCells = new List<GridCellView>();
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (_cells[x, y].IsShowingMalus())
                    {
                        malusCells.Add(_cells[x, y]);
                    }
                }
            }
            for (int i = 0; i < malusCells.Count; i++)
            {
                yield return FadeOneCellMalus(malusCells[i], from, to, duration);
            }
        }

        private static System.Collections.IEnumerator FadeOneCellMalus(GridCellView cell, float from, float to, float duration)
        {
            cell.SetMalusAlpha(from);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cell.SetMalusAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            cell.SetMalusAlpha(to);
        }

        public void SetSelectedShape(PieceShape shape, PieceColor? color = null, PieceTrait? trait = null)
        {
            _selectedShape = shape;
            _selectedColor = color;
            _selectedTrait = trait;
            _lastHoverOrigin = null;
            ClearHover();
        }

        /// <summary>
        /// Same state reset as <see cref="SetSelectedShape"/>(null), but
        /// WITHOUT ClearHover's own per-cell <see cref="RefreshCell"/> calls
        /// — those read each hover-footprint cell's LIVE grid state, which
        /// leaks a just-triggered "contamination" spread (see RunManager.
        /// ContaminateAdjacentCell) or an auto-refill-triggered lock/poison
        /// move early whenever it happens to land on the piece's own
        /// just-placed cell (almost always also the hover footprint, since
        /// a click places where it was last hovered) — before the score
        /// cascade has even started (follow-up explicit request: "on
        /// attend la fin du comptage de point avant de la faire reproduire
        /// à une tuile adjacente"). Used by OnCellClicked right before its
        /// own RefreshHoldingClearedCells call, which is about to redraw
        /// EVERY cell anyway (this one included) with the correct
        /// held/deferred treatment — so the state reset here only needs to
        /// clear the bookkeeping, not pre-empt that redraw.
        /// </summary>
        public void ClearSelectionStateOnly()
        {
            _selectedShape = null;
            _selectedColor = null;
            _selectedTrait = null;
            _lastHoverOrigin = null;
            _hoveredFootprint.Clear();
            _lineClearPreviewCells.Clear();
        }

        /// <summary>
        /// The last placement ORIGIN actually rendered by OnCellHoverEnter
        /// below (not the raw x/y it was called with — several raw cells
        /// can resolve to the same origin, e.g. via GetPlacementOrigin's own
        /// centering/snap logic) — null once nothing has been hovered yet,
        /// or right after ClearHover/SetSelectedShape force the next call
        /// to redo the work regardless.
        /// </summary>
        private Vector2Int? _lastHoverOrigin;

        public void OnCellHoverEnter(int x, int y)
        {
            if (_selectedShape == null)
            {
                ClearHover();
                _lastHoverOrigin = null;
                HoverValidityChanged?.Invoke(false);
                return;
            }

            var origin = GetPlacementOrigin(x, y);
            if (_lastHoverOrigin.HasValue && _lastHoverOrigin.Value == origin)
            {
                // The resolved placement spot hasn't actually changed since
                // last time (e.g. the pointer only moved a few pixels
                // within the same cell's gap-side margin, re-firing
                // GridGapCatcher's per-frame OnPointerMove for the exact
                // same nearest cell) — skip redoing the tint/pulse work so
                // the previewed group's cells don't restart their pulse
                // animation every single frame the mouse merely twitches
                // (on explicit report: "les groupe bloc pulse à chaque
                // frame que je bouge ma souris, ils ne devrait pulse que
                // lorsque la potentielle position valide change").
                return;
            }
            _lastHoverOrigin = origin;
            ClearHover();

            bool valid = _grid.CanPlace(_selectedShape, origin.x, origin.y);
            var overlay = valid ? UITheme.HoverValid : UITheme.HoverInvalid;
            var offsets = _selectedShape.Cells;
            var footprint = new HashSet<Vector2Int>();
            for (int i = 0; i < offsets.Count; i++)
            {
                int cx = origin.x + offsets[i].x;
                int cy = origin.y + offsets[i].y;
                if (GridManager.InBounds(cx, cy))
                {
                    // Only the one cell matching the selected piece's own
                    // enchanted local index previews the trait badge.
                    bool isTraitCell = _selectedTrait.HasValue && _selectedTrait.Value.LocalCellIndex == i;
                    _cells[cx, cy].SetHoverTint(overlay, valid, isTraitCell ? _selectedTrait : null);
                    _hoveredFootprint.Add(new Vector2Int(cx, cy));
                    footprint.Add(new Vector2Int(cx, cy));
                }
            }

            // Also pulses every pre-existing cell that would be pulled into
            // the same scored group as this placement (on explicit request —
            // a static color tint here read as too subtle against an
            // already-saturated piece color) — lets the player see the full
            // extent of what they're about to (re)score, not just the
            // piece's own footprint, before committing to a spot. Only
            // meaningful for a valid placement — GridManager.PreviewGroup
            // assumes CanPlace already passed. Pulse() is self-resetting, so
            // unlike the footprint's tint there's nothing to undo in
            // ClearHover.
            if (valid)
            {
                var previewGroup = _grid.PreviewGroup(_selectedShape, _selectedColor.Value, origin.x, origin.y);
                for (int i = 0; i < previewGroup.Count; i++)
                {
                    var pos = previewGroup[i];
                    if (footprint.Contains(pos))
                    {
                        continue;
                    }
                    _cells[pos.x, pos.y].Pulse();
                }

                // Highlights every cell of any row/column this placement
                // would complete and clear (explicit request: "j'aimerais
                // qu'on fasse un highlight de la ligne qui serait cleared")
                // — unlike Pulse() above, this tint needs explicit cleanup
                // (see ClearHover), so every cell touched here is tracked in
                // _lineClearPreviewCells first.
                var clearedLineCells = _grid.PreviewClearedLineCells(_selectedShape, origin.x, origin.y);
                for (int i = 0; i < clearedLineCells.Count; i++)
                {
                    var pos = clearedLineCells[i];
                    _cells[pos.x, pos.y].SetLineClearPreview(true);
                    _lineClearPreviewCells.Add(pos);
                }
            }

            HoverValidityChanged?.Invoke(valid);
        }

        // How far (in cells, each direction) GetPlacementOrigin looks for a
        // nearby spot the hovered piece WOULD fit when it doesn't fit right
        // at the cursor (on explicit request: "je suis tellement proche de
        // pouvoir le déposer, il faudrait être plus permissif... si le
        // joueur est proche de pouvoir déposer, on le lui propose").
        private const int SnapSearchRadius = 2;

        /// <summary>
        /// A shape's cells are always stored with their origin at the
        /// bottom-left of the bounding box (see PieceShapeCatalog), so using
        /// the hovered/clicked cell directly as that origin made the piece
        /// hang up-and-right of the cursor — it read as if the cursor was at
        /// the piece's bottom-left corner rather than its middle. Shifts by
        /// half the shape's bounding box (rounded down) so the piece centers
        /// on the cursor's cell instead. If that exact spot doesn't fit,
        /// snaps to the closest spot within <see cref="SnapSearchRadius"/>
        /// cells that DOES (see FindNearestValidOrigin) — falls back to the
        /// unsnapped, still-invalid spot if nothing nearby fits either, so
        /// the usual red "can't place here" preview still shows. Used
        /// identically by the hover preview and the click/drop placement
        /// path so what's previewed is exactly what gets placed.
        /// </summary>
        private Vector2Int GetPlacementOrigin(int x, int y)
        {
            if (_selectedShape == null)
            {
                return new Vector2Int(x, y);
            }
            int maxX = 0;
            int maxY = 0;
            var offsets = _selectedShape.Cells;
            for (int i = 0; i < offsets.Count; i++)
            {
                if (offsets[i].x > maxX) maxX = offsets[i].x;
                if (offsets[i].y > maxY) maxY = offsets[i].y;
            }
            var naiveOrigin = new Vector2Int(x - maxX / 2, y - maxY / 2);
            if (_grid.CanPlace(_selectedShape, naiveOrigin.x, naiveOrigin.y))
            {
                return naiveOrigin;
            }
            return FindNearestValidOrigin(naiveOrigin) ?? naiveOrigin;
        }

        /// <summary>Closest origin (by straight-line distance) within SnapSearchRadius cells of <paramref name="naiveOrigin"/> where the selected shape actually fits, or null if nothing in that radius does.</summary>
        private Vector2Int? FindNearestValidOrigin(Vector2Int naiveOrigin)
        {
            Vector2Int? best = null;
            int bestDistSq = int.MaxValue;
            for (int dy = -SnapSearchRadius; dy <= SnapSearchRadius; dy++)
            {
                for (int dx = -SnapSearchRadius; dx <= SnapSearchRadius; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int candidateX = naiveOrigin.x + dx;
                    int candidateY = naiveOrigin.y + dy;
                    if (!_grid.CanPlace(_selectedShape, candidateX, candidateY))
                    {
                        continue;
                    }
                    int distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = new Vector2Int(candidateX, candidateY);
                    }
                }
            }
            return best;
        }

        public void OnCellHoverExit(int x, int y)
        {
            _lastHoverOrigin = null;
            ClearHover();
            HoverValidityChanged?.Invoke(false);
        }

        /// <summary>Fired by GridGapCatcher (see Build) whenever the pointer moves within the grid's overall bounds without landing on any individual cell — resolves to the nearest cell instead of leaving the hover preview stuck or blank.</summary>
        public void OnGapPointerEnter(Vector2 localPoint)
        {
            var cell = NearestCell(localPoint);
            OnCellHoverEnter(cell.x, cell.y);
        }

        /// <summary>Fired by GridGapCatcher (see Build) for a click or drag-drop that lands in the gap between cells — same "snap to the nearest cell" fallback as OnGapPointerEnter, reusing the exact same placement path a precise cell hit already uses.</summary>
        public void OnGapPointerClick(Vector2 localPoint)
        {
            var cell = NearestCell(localPoint);
            OnCellClicked(cell.x, cell.y);
        }

        /// <summary>
        /// <paramref name="localPoint"/> is relative to the grid container's
        /// own RectTransform (see GridGapCatcher.LocalPoint). Each cell owns
        /// a "slot" of width/height <see cref="_cellStride"/> (its own
        /// cellSize plus the full spacing around it) — dividing the point's
        /// offset from the container's bottom-left corner by that stride
        /// naturally assigns half of any gap to whichever cell is on that
        /// side of it, then clamps to the grid's actual bounds so a point
        /// right at the very edge (or a hair outside it, float precision)
        /// still resolves to a real cell instead of an out-of-range index.
        /// </summary>
        private Vector2Int NearestCell(Vector2 localPoint)
        {
            var rect = _container.rect;
            float gx = localPoint.x - rect.xMin;
            float gy = localPoint.y - rect.yMin;
            int cellX = Mathf.Clamp(Mathf.FloorToInt(gx / _cellStride), 0, GridManager.Size - 1);
            int cellY = Mathf.Clamp(Mathf.FloorToInt(gy / _cellStride), 0, GridManager.Size - 1);
            return new Vector2Int(cellX, cellY);
        }

        private void ClearHover()
        {
            for (int i = 0; i < _hoveredFootprint.Count; i++)
            {
                var pos = _hoveredFootprint[i];
                RefreshCell(pos.x, pos.y);
            }
            _hoveredFootprint.Clear();

            for (int i = 0; i < _lineClearPreviewCells.Count; i++)
            {
                var pos = _lineClearPreviewCells[i];
                RefreshCell(pos.x, pos.y);
            }
            _lineClearPreviewCells.Clear();
        }

        public void OnCellClicked(int x, int y)
        {
            var origin = GetPlacementOrigin(x, y);
            CellClicked?.Invoke(origin.x, origin.y);
        }

        /// <summary>World/screen anchored position of a cell's center, for spawning floating score popups.</summary>
        public RectTransform GetCellTransform(int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return null;
            }
            return _cells[x, y].GetComponent<RectTransform>();
        }

        /// <summary>Plays a brief pulse on one cell — used when it scores points.</summary>
        public void PulseCell(int x, int y)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].Pulse();
            }
        }

        /// <summary>Plays a small radial burst on one cell, in <paramref name="color"/> — used right as it empties, whether from completing a line/column or from a trait effect destroying it (see GridCellView.PlayClearBurst).</summary>
        public void PlayClearBurst(int x, int y, Color color)
        {
            if (GridManager.InBounds(x, y))
            {
                _cells[x, y].PlayClearBurst(color);
            }
        }
    }
}
