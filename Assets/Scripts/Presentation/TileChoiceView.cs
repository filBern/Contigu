using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Shown on the shop screen once a purchase leaves a tile upgrade to place (Forge, Sprout Pack, a Blister sprout): the player picks which
    /// candidate deck tokens receive it, on the shop's own shelves (see <see cref="ShelfStage"/>). Confirm enables once exactly
    /// EconomyConstants.ShopTileChoiceCount are selected (or fewer, if the deck has fewer candidates).
    /// </summary>
    public sealed class TileChoiceView : MonoBehaviour
    {
        private const float BadgeFadeDuration = 0.35f;

        /// <summary>Fires with the chosen deck indices once the player confirms.</summary>
        public event Action<IReadOnlyList<int>> TileChoiceConfirmed;

        private RunManager _run;
        private DeckManager _deck;
        private TooltipView _tooltip;
        private RectTransform _root;
        private ShelfStage _stage;
        private ShelfLayout _layout;

        private readonly List<int> _candidates = new List<int>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly Dictionary<int, Image> _cellBackgroundByIndex = new Dictionary<int, Image>();
        private readonly Dictionary<int, RectTransform> _previewContainerByIndex = new Dictionary<int, RectTransform>();
        private int _requiredCount;

        // Set once per Show() call from the upgrade being resolved, so OnCellClicked doesn't need to know about upgrades itself.
        private PieceTraitKind? _previewTraitKind;
        private Sprite _upgradeGlyph;
        private bool _applying;

        public RectTransform Build(Transform parent, TooltipView tooltip)
        {
            _tooltip = tooltip;

            // Fully transparent: the choice happens ON the shop screen (its stall backdrop stays visible, its shelves are swapped for these).
            var overlay = UIFactory.CreatePanel(parent, "TileChoiceOverlay", new Color(0f, 0f, 0f, 0f));
            _root = overlay.rectTransform;
            overlay.gameObject.AddComponent<OverlayLift>();
            UIFactory.StretchFull(_root);

            _stage = new ShelfStage(_root, tooltip);
            _stage.ConfirmButton.onClick.AddListener(OnConfirmClicked);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>Closes the overlay without resolving it (the purchase was abandoned).</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Rebind(DeckManager deck)
        {
            _deck = deck;
        }

        /// <summary>Where the shop's shelves stand, so the seeds rest on exactly the same ones.</summary>
        public void SetShelfLayout(ShelfLayout layout)
        {
            _layout = layout;
        }

        public void Show(RunManager run, DeckManager deck, IReadOnlyList<int> candidateDeckIndices, int requiredCount, UpgradeDefinition def, UpgradeId? source = null)
        {
            _run = run;
            _deck = deck;
            _candidates.Clear();
            _candidates.AddRange(candidateDeckIndices);
            _selected.Clear();
            _cellBackgroundByIndex.Clear();
            _previewContainerByIndex.Clear();
            _requiredCount = Mathf.Min(requiredCount, _candidates.Count);
            _previewTraitKind = UpgradeSystem.TraitKindFor(def.Id);
            _upgradeGlyph = UpgradeVisualDefaults.GetGlyph(def.Id) ?? UpgradeVisualDefaults.GetIcon(def.Id);
            _applying = false;

            var columns = new List<int>();
            for (int i = 0; i < _candidates.Count; i++)
            {
                columns.Add(ShapePreviewFactory.ColumnsOf(PieceShapeCatalog.Get(_deck.Deck[_candidates[i]].Shape)));
            }
            _stage.Begin(_layout, ShelfStage.ForUpgrade(def, source), Loc.T(_requiredCount == 1 ? "choice.selectSeed" : "choice.selectSeeds", _requiredCount, def.Name), _candidates.Count, true, ShelfStage.SeedCellSize, columns);
            for (int i = 0; i < _candidates.Count; i++)
            {
                BuildPreviewCell(_candidates[i], i);
            }

            RefreshConfirmInteractable();
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        private void BuildPreviewCell(int deckIndex, int position)
        {
            float cellSize = _stage.CellSize;
            var cell = PieceBox.CreateCell(_stage.RowFor(position), "Tile_" + deckIndex, cellSize);
            float cellWidth = _stage.CellWidthFor(position);
            if (!Mathf.Approximately(cellWidth, cellSize))
            {
                cell.rectTransform.sizeDelta = new Vector2(cellWidth, cellSize);
                cell.GetComponent<LayoutElement>().preferredWidth = cellWidth;
            }
            _cellBackgroundByIndex[deckIndex] = cell;
            cell.gameObject.AddComponent<ShelfCellFeedback>();

            var cellBtn = cell.gameObject.AddComponent<Button>();
            cellBtn.onClick.AddListener(() => OnCellClicked(deckIndex));

            var previewContainer = UIFactory.CreateUIObject("Preview", cell.transform);
            previewContainer.anchorMin = new Vector2(0.5f, 0.5f);
            previewContainer.anchorMax = new Vector2(0.5f, 0.5f);
            previewContainer.pivot = new Vector2(0.5f, 0.5f);
            previewContainer.anchoredPosition = Vector2.zero;
            previewContainer.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
            if (_stage.Tile > 0f)
            {
                // The seed stands on the shelf by its own bottom edge, whatever its height.
                var shape = PieceShapeCatalog.Get(_deck.Deck[deckIndex].Shape);
                previewContainer.anchorMin = previewContainer.anchorMax = new Vector2(0.5f, 0f);
                previewContainer.pivot = new Vector2(0.5f, 0f);
                previewContainer.anchoredPosition = new Vector2(0f, 2f);
                previewContainer.sizeDelta = new Vector2(ShapePreviewFactory.ColumnsOf(shape) * _stage.Tile, ShapePreviewFactory.RowsOf(shape) * _stage.Tile);
            }
            _previewContainerByIndex[deckIndex] = previewContainer;

            RebuildPreview(deckIndex, showTrait: false, animate: false);
        }

        /// <summary>(Re)draws the piece preview for <paramref name="deckIndex"/> — plain when not selected, or with the actual trait this upgrade grants when <paramref name="showTrait"/> is true. The trait shown is a preview only — DeckManager.TagSpecificTokens still picks the real cell/color once confirmed.</summary>
        private void RebuildPreview(int deckIndex, bool showTrait, bool animate)
        {
            var token = _deck.Deck[deckIndex];
            var previewContainer = _previewContainerByIndex[deckIndex];
            for (int i = previewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(previewContainer.GetChild(i).gameObject);
            }

            // The seed's upgrades it already carries, plus (when selected) the one it is about to get on a free tile.
            var shown = new List<PieceTrait>(token.Traits);
            if (showTrait && _previewTraitKind.HasValue)
            {
                int cellCount = PieceShapeCatalog.Get(token.Shape).Cells.Count;
                int freeCell = 0;
                for (int c = 0; c < cellCount; c++)
                {
                    if (!token.UsesCell(c))
                    {
                        freeCell = c;
                        break;
                    }
                }
                // Tinted always tints to the token's own color; the exact tile is decided on confirm (this is a preview only).
                PieceColor? tintedColor = PieceTrait.TargetColorFor(_previewTraitKind.Value, token.Color);
                shown.RemoveAll(existing => existing.LocalCellIndex == freeCell && token.TraitCount >= cellCount);
                shown.Add(new PieceTrait(_previewTraitKind.Value, freeCell, tintedColor));
            }

            var badge = ShapePreviewFactory.BuildWithTraits(previewContainer, PieceShapeCatalog.Get(token.Shape), token.Color, shown, _tooltip, _cellBackgroundByIndex[deckIndex].gameObject, _stage.Tile > 0f ? _stage.Tile : ShapePreviewFactory.UniformTile(previewContainer.sizeDelta.x));
            if (animate && badge != null && !MotionSettings.Reduced)
            {
                StartCoroutine(FadeInBadge(badge));
            }
        }

        private static IEnumerator FadeInBadge(RectTransform badge)
        {
            var canvasGroup = badge.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < BadgeFadeDuration)
            {
                // Selecting a different piece before this finishes fading in destroys this badge — bail out.
                if (badge == null)
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / BadgeFadeDuration);
                yield return null;
            }
            if (badge != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void OnCellClicked(int deckIndex)
        {
            bool wasSelected = _selected.Contains(deckIndex);
            if (wasSelected)
            {
                _selected.Remove(deckIndex);
            }
            else if (_selected.Count < _requiredCount)
            {
                _selected.Add(deckIndex);
            }

            bool nowSelected = _selected.Contains(deckIndex);
            _cellBackgroundByIndex[deckIndex].GetComponent<ShelfCellFeedback>().SetSelected(nowSelected);
            // No preview of the upgrade on the seed any more: it only lands there when the choice is confirmed (see ApplyThenConfirm).
            RefreshConfirmInteractable();
        }

        private void RefreshConfirmInteractable()
        {
            _stage.ConfirmButton.interactable = _selected.Count == _requiredCount;
        }

        private void OnConfirmClicked()
        {
            if (_applying)
            {
                return;
            }
            if (MotionSettings.Reduced || _upgradeGlyph == null || _stage.SourceRect == null || !_stage.SourceRect.gameObject.activeInHierarchy)
            {
                FinishConfirm();
                return;
            }
            StartCoroutine(ApplyThenConfirm());
        }

        private void FinishConfirm()
        {
            _root.gameObject.SetActive(false);
            if (TileChoiceConfirmed != null)
            {
                TileChoiceConfirmed(new List<int>(_selected));
            }
        }

        private const float FlightStagger = 0.2f;
        private const float FlightDuration = 0.45f;
        private const float SettleDuration = 0.45f;

        /// <summary>The upgrade leaves the anvil/crate and flies onto each chosen seed one after the other; each seed shows it on landing, then the choice goes through.</summary>
        private IEnumerator ApplyThenConfirm()
        {
            _applying = true;
            _stage.ConfirmButton.interactable = false;
            var chosen = new List<int>();
            foreach (var candidate in _candidates) { if (_selected.Contains(candidate)) { chosen.Add(candidate); } }
            foreach (var cell in _cellBackgroundByIndex.Values)
            {
                var button = cell.GetComponent<Button>();
                if (button != null) { button.interactable = false; }
                var feedback = cell.GetComponent<ShelfCellFeedback>();
                if (feedback != null) { feedback.enabled = false; }
            }
            for (int i = 0; i < chosen.Count; i++)
            {
                StartCoroutine(FlyOntoSeed(chosen[i], i * FlightStagger));
            }
            float total = (chosen.Count - 1) * FlightStagger + FlightDuration + SettleDuration;
            float t = 0f;
            while (t < total)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }
            _applying = false;
            if (_root.gameObject.activeSelf)
            {
                FinishConfirm();
            }
        }

        private IEnumerator FlyOntoSeed(int deckIndex, float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }
            var target = _previewContainerByIndex[deckIndex];
            var flying = UIFactory.CreatePanel(_root, "FlyingUpgrade", Color.white);
            flying.sprite = _upgradeGlyph;
            flying.preserveAspect = true;
            flying.raycastTarget = false;
            var rect = flying.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(64f, 64f);
            rect.SetAsLastSibling();
            var from = WorldCenter(_stage.SourceRect);
            SfxManager.Play(SfxId.PickUpPiece);

            t = 0f;
            while (t < FlightDuration)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / FlightDuration);
                float eased = p * p * (3f - 2f * p);
                var to = WorldCenter(target);
                float arc = Mathf.Abs(from.y - to.y) * 0.25f + 40f * rect.lossyScale.y;
                rect.position = Vector3.Lerp(from, to, eased) + new Vector3(0f, Mathf.Sin(p * Mathf.PI) * arc, 0f);
                float scale = Mathf.Lerp(1.3f, 0.7f, eased);
                rect.localScale = Vector3.one * scale;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 2f) * 16f);
                yield return null;
            }
            Destroy(flying.gameObject);

            // Landing: the seed shows its new upgrade, punches and throws a few sparks.
            SfxManager.Play(SfxId.SlotPop);
            RebuildPreview(deckIndex, showTrait: true, animate: true);
            var center = WorldCenter(target);
            var sparks = new RectTransform[8];
            var velocity = new Vector2[8];
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = UIFactory.CreatePanel(_root, "LandSpark" + i, i % 2 == 0 ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 0.97f, 0.82f));
                spark.raycastTarget = false;
                var sr = spark.rectTransform;
                sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
                float size = UnityEngine.Random.Range(6f, 12f);
                sr.sizeDelta = new Vector2(size, size);
                sr.position = center;
                float angle = (i / (float)sparks.Length) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.3f, 0.3f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(120f, 260f);
                sparks[i] = sr;
            }
            float punchTime = 0f;
            const float PunchDuration = 0.3f;
            while (punchTime < PunchDuration)
            {
                punchTime += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(punchTime / PunchDuration);
                float punch = 1f + 0.22f * Mathf.Sin(p * Mathf.PI);
                if (target != null) { target.localScale = new Vector3(punch, punch, 1f); }
                for (int i = 0; i < sparks.Length; i++)
                {
                    velocity[i] *= 1f - 3f * Time.unscaledDeltaTime;
                    sparks[i].anchoredPosition += velocity[i] * Time.unscaledDeltaTime;
                    float s = 1f - p;
                    sparks[i].localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            if (target != null) { target.localScale = Vector3.one; }
            for (int i = 0; i < sparks.Length; i++) { if (sparks[i] != null) { Destroy(sparks[i].gameObject); } }
        }

        private static Vector3 WorldCenter(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }
    }
}
