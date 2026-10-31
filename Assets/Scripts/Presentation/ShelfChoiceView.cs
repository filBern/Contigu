using System;
using System.Collections;
using System.Collections.Generic;
using Contigu.Data;
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
        /// <summary>Shown rising over the option when it is chosen with the celebration on (e.g. the new mastery level).</summary>
        public string CelebrationText;
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

        /// <param name="destroyChosen">The chosen option is destroyed (Compost): it shakes and crumbles before the choice is applied.</param>
        public void Show(ChoiceSource source, string title, IReadOnlyList<ChoiceEntry> entries, bool needsConfirm, Action<int> onChosen, bool destroyChosen = false, bool celebrateChosen = false, bool flyToSource = false)
        {
            _flyToSource = flyToSource;
            _onChosen = onChosen;
            _destroyChosen = destroyChosen;
            _celebrateChosen = celebrateChosen;
            _celebrationTexts.Clear();
            for (int i = 0; i < entries.Count; i++) { _celebrationTexts.Add(entries[i].CelebrationText); }
            _resolving = false;
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
            if (entry.FillCell)
            {
                // Icons stand on the shelf too, like the seeds, instead of floating in the middle of their cell.
                host.anchorMin = host.anchorMax = new Vector2(0.5f, 0f);
                host.pivot = new Vector2(0.5f, 0f);
                host.anchoredPosition = new Vector2(0f, 6f);
            }
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

        private bool _destroyChosen;
        private bool _celebrateChosen;
        private bool _flyToSource;
        private readonly List<string> _celebrationTexts = new List<string>();
        private bool _resolving;

        private void Resolve(int index)
        {
            if (_resolving)
            {
                return;
            }
            if (_destroyChosen && !MotionSettings.Reduced && _cellByIndex.TryGetValue(index, out var cell) && cell != null && cell.transform.Find("Preview") != null)
            {
                StartCoroutine(DestroyThenResolve(index, cell));
                return;
            }
            if (_flyToSource && !MotionSettings.Reduced && _cellByIndex.TryGetValue(index, out var flier) && flier != null && flier.transform.Find("Preview") != null && _stage.SourceRect != null && _stage.SourceRect.gameObject.activeInHierarchy)
            {
                StartCoroutine(FlyToSourceThenResolve(index, flier));
                return;
            }
            if (_celebrateChosen && !MotionSettings.Reduced && _cellByIndex.TryGetValue(index, out var winner) && winner != null && winner.transform.Find("Preview") != null)
            {
                StartCoroutine(CelebrateThenResolve(index, winner));
                return;
            }
            Finish(index);
        }

        /// <summary>The picture of the option chosen last (still valid right after the screen closes), so the caller can fly it somewhere; null if none.</summary>
        public RectTransform LastChosenPreview { get; private set; }

        private void Finish(int index)
        {
            LastChosenPreview = _cellByIndex.TryGetValue(index, out var chosenCell) && chosenCell != null ? chosenCell.transform.Find("Preview") as RectTransform : null;
            _root.gameObject.SetActive(false);
            if (_onChosen != null)
            {
                _onChosen(index);
            }
        }

        private const float FlyWindUp = 0.14f;
        private const float FlyDuration = 0.4f;
        private const float HitDuration = 0.28f;
        private const int HitSparks = 12;

        /// <summary>
        /// Forge / Sprout Pack: the chosen upgrade winds up, then shoots up to the anvil (or crate) at the top of the screen, which takes the hit
        /// (a punch and a burst of sparks), before the seed choice comes up.
        /// </summary>
        private IEnumerator FlyToSourceThenResolve(int index, Image cell)
        {
            _resolving = true;
            foreach (var other in _cellByIndex)
            {
                var button = other.Value.GetComponent<Button>();
                if (button != null) { button.interactable = false; }
                if (other.Key != index) { other.Value.gameObject.AddComponent<CanvasGroup>().alpha = 1f; }
            }
            _stage.ConfirmButton.interactable = false;
            var preview = (RectTransform)cell.transform.Find("Preview");
            var feedback = cell.GetComponent<ShelfCellFeedback>();
            if (feedback != null) { feedback.enabled = false; }
            var source = _stage.SourceRect;

            var start = WorldCenterOf(preview);
            float startWidth = Mathf.Max(0.0001f, WorldWidthOf(preview));
            var home = preview.position;
            SfxManager.Play(SfxId.PickUpPiece);

            // Wind-up: a quick squash down while the others dim.
            float t = 0f;
            while (t < FlyWindUp)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / FlyWindUp);
                preview.localScale = new Vector3(1f + 0.12f * p, 1f - 0.18f * p, 1f);
                foreach (var other in _cellByIndex)
                {
                    if (other.Key == index) { continue; }
                    var g = other.Value.GetComponent<CanvasGroup>();
                    if (g != null) { g.alpha = Mathf.Lerp(1f, 0.2f, p); }
                }
                yield return null;
            }

            // Flight: accelerates toward the source, shrinking to roughly its glyph's size.
            t = 0f;
            while (t < FlyDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / FlyDuration);
                float eased = p * p * p;
                var end = WorldCenterOf(source);
                float endScale = Mathf.Clamp(WorldWidthOf(source) * 0.5f / startWidth, 0.2f, 1f);
                preview.position = Vector3.Lerp(home, home + (end - start), eased);
                float scale = Mathf.Lerp(1f, endScale, eased);
                preview.localScale = new Vector3(scale * (1.12f - 0.12f * eased), scale * (0.82f + 0.18f * eased), 1f);
                yield return null;
            }
            preview.gameObject.SetActive(false);

            // The hit: the source punches and sparks fly.
            SfxManager.Play(SfxId.ValidDrop);
            var host = source.parent as RectTransform;
            var sparks = new RectTransform[HitSparks];
            var velocity = new Vector2[HitSparks];
            var colors = new[] { new Color(1f, 0.89f, 0.40f), new Color(1f, 0.97f, 0.80f), new Color(1f, 0.62f, 0.2f) };
            for (int i = 0; i < HitSparks; i++)
            {
                var spark = UIFactory.CreatePanel(host, "HitSpark" + i, colors[i % colors.Length]);
                spark.raycastTarget = false;
                var rect = spark.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = UnityEngine.Random.Range(6f, 13f);
                rect.sizeDelta = new Vector2(size, size);
                rect.position = WorldCenterOf(source);
                float angle = (i / (float)HitSparks) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(180f, 360f);
                sparks[i] = rect;
            }
            var sourceBase = source.localScale;
            t = 0f;
            while (t < HitDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / HitDuration);
                float punch = 1f + 0.2f * Mathf.Sin(p * Mathf.PI);
                source.localScale = sourceBase * punch;
                for (int i = 0; i < HitSparks; i++)
                {
                    velocity[i] *= 1f - 3f * Time.unscaledDeltaTime;
                    sparks[i].anchoredPosition += velocity[i] * Time.unscaledDeltaTime;
                    float s = 1f - p;
                    sparks[i].localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
            source.localScale = sourceBase;
            for (int i = 0; i < HitSparks; i++) { if (sparks[i] != null) { Destroy(sparks[i].gameObject); } }
            _resolving = false;
            if (_root.gameObject.activeSelf)
            {
                Finish(index);
            }
        }

        private static Vector3 WorldCenterOf(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private static float WorldWidthOf(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Mathf.Abs(corners[2].x - corners[0].x);
        }

        private const float PopDuration = 0.28f;
        private const float CelebrationDuration = 1.0f;
        private const int SparkCount = 18;

        /// <summary>
        /// Mastery: the chosen option pops up with an overshoot while the others dim, a golden flash and a burst of sparkles go off behind it, and its
        /// new level rises over it, before the choice is applied.
        /// </summary>
        private IEnumerator CelebrateThenResolve(int index, Image cell)
        {
            _resolving = true;
            foreach (var other in _cellByIndex)
            {
                var button = other.Value.GetComponent<Button>();
                if (button != null) { button.interactable = false; }
                if (other.Key != index)
                {
                    other.Value.gameObject.AddComponent<CanvasGroup>().alpha = 1f;
                }
            }
            _stage.ConfirmButton.interactable = false;
            var preview = (RectTransform)cell.transform.Find("Preview");
            var feedback = cell.GetComponent<ShelfCellFeedback>();
            if (feedback != null) { feedback.enabled = false; } // the animation owns the preview's scale and position now
            var home = preview.anchoredPosition;
            var center = home + new Vector2(0f, preview.sizeDelta.y * 0.5f);
            SfxManager.Play(SfxId.LueurGain);

            // A warm flash behind the option.
            var flash = UIFactory.CreatePanel(cell.transform, "Flash", new Color(1f, 0.86f, 0.35f, 0f));
            flash.raycastTarget = false;
            flash.sprite = UISprites.CardBackground;
            flash.type = Image.Type.Sliced;
            var flashRect = flash.rectTransform;
            flashRect.anchorMin = flashRect.anchorMax = new Vector2(0.5f, 0f);
            flashRect.pivot = new Vector2(0.5f, 0.5f);
            flashRect.anchoredPosition = center;
            flashRect.SetSiblingIndex(preview.GetSiblingIndex());

            // Sparkles flying out of it in every direction.
            var sparks = new RectTransform[SparkCount];
            var velocity = new Vector2[SparkCount];
            var spin = new float[SparkCount];
            var gold = new[] { new Color(1f, 0.89f, 0.40f), new Color(1f, 0.97f, 0.80f), new Color(1f, 0.72f, 0.25f) };
            for (int i = 0; i < SparkCount; i++)
            {
                var spark = UIFactory.CreatePanel(cell.transform, "Spark" + i, gold[i % gold.Length]);
                spark.raycastTarget = false;
                var rect = spark.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = UnityEngine.Random.Range(9f, 18f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = center;
                float angle = (i / (float)SparkCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(220f, 420f);
                spin[i] = UnityEngine.Random.Range(-540f, 540f);
                sparks[i] = rect;
            }

            // The new level rising above the option.
            string label = index < _celebrationTexts.Count ? _celebrationTexts[index] : null;
            RectTransform labelRect = null;
            Text labelText = null;
            if (!string.IsNullOrEmpty(label))
            {
                labelText = UIFactory.CreateText(cell.transform, "Celebration", label, 38, VisualDefaults.GoldenColor);
                labelText.raycastTarget = false;
                labelRect = labelText.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.sizeDelta = new Vector2(260f, 50f);
                labelRect.anchoredPosition = center + new Vector2(0f, preview.sizeDelta.y * 0.5f + 8f);
            }

            float t = 0f;
            while (t < CelebrationDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / PopDuration);
                // Pop: up past 1.35 and settling at 1.2, with a small hop.
                float pop = p < 0.6f ? Mathf.Lerp(1f, 1.4f, p / 0.6f) : Mathf.Lerp(1.4f, 1.2f, (p - 0.6f) / 0.4f);
                preview.localScale = new Vector3(pop, pop, 1f);
                preview.anchoredPosition = home + new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 18f);
                float life = Mathf.Clamp01(t / CelebrationDuration);

                flashRect.sizeDelta = Vector2.one * Mathf.Lerp(40f, preview.sizeDelta.y * 2.4f, Mathf.Clamp01(t / 0.35f));
                var fc = flash.color;
                fc.a = Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI) * 0.55f;
                flash.color = fc;

                foreach (var other in _cellByIndex)
                {
                    if (other.Key == index) { continue; }
                    var g = other.Value.GetComponent<CanvasGroup>();
                    if (g != null) { g.alpha = Mathf.Lerp(1f, 0.2f, Mathf.Clamp01(t / 0.2f)); }
                }

                for (int i = 0; i < SparkCount; i++)
                {
                    velocity[i] *= 1f - 2.2f * Time.unscaledDeltaTime;
                    sparks[i].anchoredPosition += velocity[i] * Time.unscaledDeltaTime;
                    sparks[i].localRotation = Quaternion.Euler(0f, 0f, 45f + spin[i] * t);
                    float s = 1f - life * life;
                    sparks[i].localScale = new Vector3(s, s, 1f);
                }

                if (labelRect != null)
                {
                    float lp = Mathf.Clamp01(t / 0.25f);
                    float labelScale = lp < 0.7f ? Mathf.Lerp(0f, 1.25f, lp / 0.7f) : Mathf.Lerp(1.25f, 1f, (lp - 0.7f) / 0.3f);
                    labelRect.localScale = new Vector3(labelScale, labelScale, 1f);
                    labelRect.anchoredPosition = center + new Vector2(0f, preview.sizeDelta.y * 0.5f * pop + 8f + life * 34f);
                    var lc = labelText.color;
                    lc.a = life < 0.7f ? 1f : 1f - (life - 0.7f) / 0.3f;
                    labelText.color = lc;
                }
                yield return null;
            }
            _resolving = false;
            if (_root.gameObject.activeSelf)
            {
                Finish(index);
            }
        }

        private const float ShakeDuration = 0.2f;
        private const float CrumbleDuration = 0.45f;
        private const float DebrisLife = 0.7f;
        private const int DebrisCount = 16;
        private const float DebrisGravity = 900f;

        /// <summary>Compost: the chosen seed shakes, then crumbles into earth and leaf bits that fly out and fall, before the choice is applied.</summary>
        private IEnumerator DestroyThenResolve(int index, Image cell)
        {
            _resolving = true;
            foreach (var other in _cellByIndex.Values)
            {
                var button = other.GetComponent<Button>();
                if (button != null) { button.interactable = false; }
            }
            _stage.ConfirmButton.interactable = false;
            var preview = (RectTransform)cell.transform.Find("Preview");
            var feedback = cell.GetComponent<ShelfCellFeedback>();
            if (feedback != null) { feedback.enabled = false; } // the animation owns the preview's scale and position now
            var group = preview.gameObject.AddComponent<CanvasGroup>();
            var home = preview.anchoredPosition;
            var center = home + new Vector2(0f, preview.sizeDelta.y * 0.5f);
            SfxManager.Play(SfxId.InvalidDrop);

            float t = 0f;
            while (t < ShakeDuration)
            {
                t += Time.unscaledDeltaTime;
                preview.anchoredPosition = home + new Vector2(Mathf.Sin(t * 70f) * 5f, 0f);
                yield return null;
            }
            preview.anchoredPosition = home;

            // Debris: small earth/leaf squares thrown up and out from the middle of the seed.
            var colors = new[] { new Color(0.45f, 0.30f, 0.17f), new Color(0.60f, 0.42f, 0.25f), new Color(0.43f, 0.69f, 0.31f), new Color(0.30f, 0.20f, 0.12f) };
            var bits = new RectTransform[DebrisCount];
            var velocity = new Vector2[DebrisCount];
            var spin = new float[DebrisCount];
            for (int i = 0; i < DebrisCount; i++)
            {
                var bit = UIFactory.CreatePanel(cell.transform, "Debris" + i, colors[i % colors.Length]);
                bit.raycastTarget = false;
                var rect = bit.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = UnityEngine.Random.Range(7f, 15f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = center + new Vector2(UnityEngine.Random.Range(-18f, 18f), UnityEngine.Random.Range(-14f, 14f));
                float angle = UnityEngine.Random.Range(35f, 145f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(150f, 340f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spin[i] = UnityEngine.Random.Range(-420f, 420f);
                bits[i] = rect;
            }

            t = 0f;
            float total = Mathf.Max(CrumbleDuration, DebrisLife);
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / CrumbleDuration);
                float scale = Mathf.Lerp(1f, 0.15f, p * p);
                preview.localScale = new Vector3(scale, scale, 1f);
                preview.localRotation = Quaternion.Euler(0f, 0f, -22f * p);
                group.alpha = 1f - p;
                float life = Mathf.Clamp01(t / DebrisLife);
                for (int i = 0; i < DebrisCount; i++)
                {
                    if (bits[i] == null) { continue; }
                    velocity[i].y -= DebrisGravity * Time.unscaledDeltaTime;
                    bits[i].anchoredPosition += velocity[i] * Time.unscaledDeltaTime;
                    bits[i].localRotation = Quaternion.Euler(0f, 0f, spin[i] * t);
                    var image = bits[i].GetComponent<Image>();
                    var c = image.color;
                    c.a = 1f - life * life;
                    image.color = c;
                }
                yield return null;
            }
            _resolving = false;
            // Skipped meanwhile (the screen was hidden by the abandon): nothing left to apply.
            if (_root.gameObject.activeSelf)
            {
                Finish(index);
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
