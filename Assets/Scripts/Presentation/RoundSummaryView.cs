using System;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>A short card between a cleared round and the shop: how the round went (score, biggest placement, harvests, Pollen), then on to the shop.</summary>
    public sealed class RoundSummaryView : MonoBehaviour
    {
        private const float CardWidth = 440f;
        private const float RowHeight = 34f;
        private const float TotalRowHeight = 40f;
        private const float PaperHeight = 250f;
        private const int MaxBodyRows = 6;
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 1f);
        // The Pollen gold (E8A82E), darkened so it reads on the cream parchment.
        private static readonly Color DarkPollenGold = new Color(0.72f, 0.46f, 0.02f, 1f);
        private static readonly Color Dark = new Color(0.13f, 0.09f, 0.06f, 1f);

        private RectTransform _root;
        private RectTransform _card;
        private Text _title;
        private RectTransform _rows;
        private Button _continue;
        private Action _onContinue;
        private Coroutine _pop;
        private Coroutine _growthPlay;
        private RectTransform _garden;
        private const float CardHeight = 420f;
        private const float GardenHeight = 150f;
        private const float GardenGap = 10f;
        private static readonly Color PipEmpty = new Color(0.10f, 0.13f, 0.17f, 0.9f);
        private static readonly Color PipBud = new Color(0.37f, 0.76f, 0.30f, 1f);
        private static readonly Color PipBloom = new Color(0.93f, 0.51f, 0.67f, 1f);
        private static readonly Color PipFruit = new Color(0.96f, 0.79f, 0.23f, 1f);

        // One fertilizer's column in the garden strip.
        private sealed class GardenCell
        {
            public GrowthStep Step;
            public RectTransform Root;
            public Image Sack;
            public Image Medallion;
            public Image[] Pips;
            public Text Label;
        }
        private readonly List<GardenCell> _gardenCells = new List<GardenCell>();

        /// <summary>Raised when a fertilizer's growth has just been played (slot index): the panel on the left then shows its new stage, not before.</summary>
        public event Action<int, GrowthStep> GrowthStepPlayed;
        /// <summary>Raised when the garden animation is over or skipped (all the new stages may show).</summary>
        public event Action GrowthFinished;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "RoundSummaryOverlay", new Color(0f, 0f, 0f, 0.55f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var board = UIFactory.CreateSlicedImage(_root, "Card", UISprites.CardBackground);
            board.color = Wood;
            UIFactory.AddThickOutline(board, Dark);
            _card = board.rectTransform;
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(CardWidth, 420f);

            _title = UIFactory.CreateText(_card, "Title", "", 34, UITheme.TextOnBackground);
            _title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            _title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            _title.rectTransform.sizeDelta = new Vector2(CardWidth - 30f, 46f);

            // A cream parchment holds the figures.
            var paper = UIFactory.CreateSlicedImage(_card, "Paper", UISprites.CardBackground);
            paper.color = UITheme.Panel;
            UIFactory.AddThickOutline(paper, Dark);
            paper.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            paper.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            paper.rectTransform.pivot = new Vector2(0.5f, 1f);
            paper.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            paper.rectTransform.sizeDelta = new Vector2(CardWidth - 40f, PaperHeight);
            _rows = paper.rectTransform;

            // Below the figures: how each held fertilizer grew this round (Maturation), played as a short animation.
            var garden = UIFactory.CreateSlicedImage(_card, "Garden", UISprites.CardBackground);
            garden.color = UITheme.Panel;
            UIFactory.AddThickOutline(garden, Dark);
            garden.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            garden.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            garden.rectTransform.pivot = new Vector2(0.5f, 1f);
            garden.rectTransform.anchoredPosition = new Vector2(0f, -(72f + PaperHeight + GardenGap));
            garden.rectTransform.sizeDelta = new Vector2(CardWidth - 40f, GardenHeight);
            _garden = garden.rectTransform;
            _garden.gameObject.SetActive(false);

            _continue = UIFactory.CreateButton(_card, "Continue", "To the shop", UISprites.Garden("Green") ?? UISprites.ChooseButtonBackground, 24);
            var rect = _continue.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(260f, 58f);
            _continue.onClick.AddListener(OnContinue);

            _root.gameObject.SetActive(false);
            return _root;
        }

        public void Show(RoundSummary summary, Action onContinue)
        {
            _onContinue = onContinue;
            BuildGarden(summary.Growth);
            _title.text = "Round " + summary.RoundNumber + " cleared";
            for (int i = _rows.childCount - 1; i >= 0; i--)
            {
                Destroy(_rows.GetChild(i).gameObject);
            }
            // Pollen only, by source: harvested lines, unused seeds, tile upgrades, challenge bonus, then each fertilizer.
            var lines = new System.Collections.Generic.List<KeyValuePair<string, int>>();
            lines.Add(new KeyValuePair<string, int>("Lines harvested (" + summary.LinesHarvested + ")", summary.LinesPollen));
            lines.Add(new KeyValuePair<string, int>("Unused seeds (" + summary.UnusedSeedsPollen + ")", summary.UnusedSeedsPollen));
            if (summary.TilePollen > 0) { lines.Add(new KeyValuePair<string, int>("Seed upgrades", summary.TilePollen)); }
            if (summary.ChallengePollen > 0) { lines.Add(new KeyValuePair<string, int>("Season bonus", summary.ChallengePollen)); }
            var fertilizers = summary.FertilizerPollen;
                        int hidden = 0;
            for (int i = 0; fertilizers != null && i < fertilizers.Count; i++)
            {
                if (lines.Count < MaxBodyRows - 1 || (lines.Count < MaxBodyRows && i == fertilizers.Count - 1))
                {
                    lines.Add(new KeyValuePair<string, int>(ModifierCatalog.Get(fertilizers[i].Id).Name, fertilizers[i].Amount));
                }
                else { hidden += fertilizers[i].Amount; }
            }
            if (hidden > 0) { lines.Add(new KeyValuePair<string, int>("Other fertilizers", hidden)); }
            int total = 0;
            for (int i = 0; i < lines.Count; i++) { total += lines[i].Value; }
            float rowHeight = Mathf.Min(RowHeight, (PaperHeight - 28f - TotalRowHeight) / lines.Count);
            int fontSize = rowHeight < 30f ? 18 : 20;
            int row = 0;
            float y = 14f;
            for (int i = 0; i < lines.Count; i++)
            {
                AddRow(ref row, lines[i].Key, "+" + lines[i].Value, false, rowHeight, fontSize, ref y);
            }
            y += 6f;
            AddRow(ref row, "Pollen earned", "+" + total, true, TotalRowHeight, 22, ref y);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            SfxManager.Play(SfxId.RoundClear);
            if (!MotionSettings.Reduced)
            {
                if (_pop != null) { StopCoroutine(_pop); }
                _pop = StartCoroutine(PopIn());
            }
            else
            {
                _card.localScale = Vector3.one;
            }
        }

        private void BuildGarden(List<GrowthStep> steps)
        {
            if (_growthPlay != null) { StopCoroutine(_growthPlay); _growthPlay = null; }
            for (int i = _garden.childCount - 1; i >= 0; i--)
            {
                Destroy(_garden.GetChild(i).gameObject);
            }
            _gardenCells.Clear();
            bool any = steps != null && steps.Count > 0;
            _garden.gameObject.SetActive(any);
            _card.sizeDelta = new Vector2(CardWidth, CardHeight + (any ? GardenHeight + GardenGap : 0f));
            if (!any)
            {
                return;
            }
            var title = UIFactory.CreateText(_garden, "GardenTitle", "Garden growth", 18, UITheme.TextMuted);
            title.raycastTarget = false;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            title.rectTransform.sizeDelta = new Vector2(0f, 24f);

            float width = CardWidth - 40f - 20f;
            float cellWidth = Mathf.Min(100f, width / steps.Count);
            float startX = -cellWidth * steps.Count * 0.5f + cellWidth * 0.5f;
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var cell = new GardenCell { Step = step };
                var root = UIFactory.CreateUIObject("GardenCell" + i, _garden);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = new Vector2(startX + i * cellWidth, -12f);
                root.sizeDelta = new Vector2(cellWidth, GardenHeight - 34f);
                cell.Root = root;

                cell.Sack = ModifierBadgeFactory.Create(root, ModifierCatalog.Get(step.Id), 60f, null, showBackground: false, attachTooltip: false);
                cell.Sack.raycastTarget = false;
                cell.Sack.rectTransform.anchorMin = cell.Sack.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cell.Sack.rectTransform.pivot = new Vector2(0.5f, 1f);
                cell.Sack.rectTransform.anchoredPosition = new Vector2(0f, -2f);

                cell.Medallion = UIFactory.CreatePanel(root, "Stage", Color.white);
                cell.Medallion.raycastTarget = false;
                cell.Medallion.preserveAspect = true;
                cell.Medallion.rectTransform.anchorMin = cell.Medallion.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cell.Medallion.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                cell.Medallion.rectTransform.anchoredPosition = new Vector2(30f, -12f);
                cell.Medallion.rectTransform.sizeDelta = new Vector2(40f, 40f);

                cell.Pips = new Image[FertilizerGrowth.FruitAt];
                for (int p = 0; p < cell.Pips.Length; p++)
                {
                    var pip = UIFactory.CreatePanel(root, "Pip" + p, PipEmpty);
                    pip.raycastTarget = false;
                    UIFactory.AddThickOutline(pip, Dark, 1.5f);
                    pip.rectTransform.anchorMin = pip.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    pip.rectTransform.pivot = new Vector2(0.5f, 1f);
                    pip.rectTransform.anchoredPosition = new Vector2((p - (cell.Pips.Length - 1) * 0.5f) * 22f, -68f);
                    pip.rectTransform.sizeDelta = new Vector2(18f, 12f);
                    cell.Pips[p] = pip;
                }

                cell.Label = UIFactory.CreateText(root, "State", "", 15, UITheme.TextPrimary);
                cell.Label.raycastTarget = false;
                cell.Label.rectTransform.anchorMin = new Vector2(0f, 0f);
                cell.Label.rectTransform.anchorMax = new Vector2(1f, 0f);
                cell.Label.rectTransform.pivot = new Vector2(0.5f, 0f);
                cell.Label.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                cell.Label.rectTransform.sizeDelta = new Vector2(0f, 20f);

                _gardenCells.Add(cell);
                // Starting point: the state before the round.
                ShowGrowth(cell, step.Before, step.Scored || step.Harvested ? 1f : 0.6f);
                cell.Label.text = "";
            }
            if (MotionSettings.Reduced)
            {
                for (int i = 0; i < _gardenCells.Count; i++)
                {
                    ShowFinalGrowth(_gardenCells[i]);
                }
                FinishGrowth();
            }
            else
            {
                _growthPlay = StartCoroutine(PlayGrowth());
            }
        }

        private static Color PipColorFor(int growth)
        {
            var stage = FertilizerGrowth.StageOf(growth);
            return stage == GrowthStage.Fruit ? PipFruit : stage == GrowthStage.Bloom ? PipBloom : PipBud;
        }

        private static void ShowGrowth(GardenCell cell, int growth, float alpha)
        {
            cell.Medallion.sprite = GrowthVisuals.StageSprite(FertilizerGrowth.StageOf(growth));
            cell.Medallion.enabled = cell.Medallion.sprite != null;
            for (int p = 0; p < cell.Pips.Length; p++)
            {
                cell.Pips[p].color = p < growth ? PipColorFor(growth) : PipEmpty;
            }
            var sack = cell.Sack.color;
            sack.a = alpha;
            cell.Sack.color = sack;
        }

        private static string FinalLabel(GrowthStep step)
        {
            if (step.Harvested) { return "Harvested!"; }
            if (!step.Scored) { return "Dormant"; }
            var stage = FertilizerGrowth.StageOf(step.After);
            if (stage != FertilizerGrowth.StageOf(step.Before)) { return stage == GrowthStage.Fruit ? "Fruit!" : "Bloom!"; }
            return "+1";
        }

        private void ShowFinalGrowth(GardenCell cell)
        {
            ShowGrowth(cell, cell.Step.After, cell.Step.Scored || cell.Step.Harvested ? 1f : 0.6f);
            cell.Label.text = FinalLabel(cell.Step);
        }

        private System.Collections.IEnumerator PlayGrowth()
        {
            yield return Wait(0.55f);
            for (int i = 0; i < _gardenCells.Count; i++)
            {
                var cell = _gardenCells[i];
                var step = cell.Step;
                if (!step.Scored && !step.Harvested)
                {
                    cell.Label.text = FinalLabel(step);
                    RaiseStepPlayed(i, step);
                    yield return Wait(0.12f);
                    continue;
                }
                var before = FertilizerGrowth.StageOf(step.Before);
                var after = FertilizerGrowth.StageOf(step.After);
                if (step.Harvested)
                {
                    // The Fruit is picked: the sack pops, the Pollen rises, the fruit gives way to a new Bud.
                    yield return PopRect(cell.Sack.rectTransform, 1.25f, 0.22f);
                    SfxManager.Play(SfxId.RoundClear);
                    StartCoroutine(RiseText(cell, "+" + FertilizerGrowth.HarvestPollen));
                    ShowGrowth(cell, 0, 1f);
                    cell.Label.text = FinalLabel(step);
                    RaiseStepPlayed(i, step);
                    yield return SwapMedallion(cell, before, after, PipFruit);
                }
                else
                {
                    // One more step: the next pip fills; a change of stage turns one medallion into the next.
                    ShowGrowth(cell, step.After, 1f);
                    cell.Label.text = FinalLabel(step);
                    var pip = cell.Pips[Mathf.Clamp(step.After - 1, 0, cell.Pips.Length - 1)].rectTransform;
                    StartCoroutine(PopRect(pip, 1.6f, 0.2f));
                    RaiseStepPlayed(i, step);
                    if (before != after)
                    {
                        SfxManager.Play(SfxId.RoundClear);
                        yield return SwapMedallion(cell, before, after, PipColorFor(step.After));
                    }
                    else
                    {
                        yield return PopRect(cell.Sack.rectTransform, 1.12f, 0.18f);
                    }
                }
                yield return Wait(0.25f);
            }
            _growthPlay = null;
            FinishGrowth();
        }

        private void RaiseStepPlayed(int index, GrowthStep step)
        {
            if (GrowthStepPlayed != null) { GrowthStepPlayed(index, step); }
        }

        private void FinishGrowth()
        {
            if (GrowthFinished != null) { GrowthFinished(); }
        }

        /// <summary>The old medallion shrinks and fades out while a ring flashes, then the new one grows in with an overshoot: one icon visibly becomes the next.</summary>
        private System.Collections.IEnumerator SwapMedallion(GardenCell cell, GrowthStage from, GrowthStage to, Color flash)
        {
            var host = cell.Medallion.rectTransform;
            // cell.Medallion already holds the new sprite (ShowGrowth); an overlay carries the old one away.
            var old = UIFactory.CreatePanel(cell.Root, "OldStage", Color.white);
            old.raycastTarget = false;
            old.preserveAspect = true;
            old.sprite = GrowthVisuals.StageSprite(from);
            old.enabled = old.sprite != null;
            old.rectTransform.anchorMin = host.anchorMin;
            old.rectTransform.anchorMax = host.anchorMax;
            old.rectTransform.pivot = host.pivot;
            old.rectTransform.anchoredPosition = host.anchoredPosition;
            old.rectTransform.sizeDelta = host.sizeDelta;
            var ring = UIFactory.CreatePanel(cell.Root, "Flash", new Color(flash.r, flash.g, flash.b, 0f));
            ring.raycastTarget = false;
            ring.sprite = GrowthVisuals.FruitGlow;
            ring.enabled = ring.sprite != null;
            ring.rectTransform.anchorMin = host.anchorMin;
            ring.rectTransform.anchorMax = host.anchorMax;
            ring.rectTransform.pivot = host.pivot;
            ring.rectTransform.anchoredPosition = host.anchoredPosition;
            ring.rectTransform.sizeDelta = host.sizeDelta * 2f;
            host.localScale = Vector3.zero;
            float t = 0f;
            const float Out = 0.2f;
            while (t < Out)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / Out);
                old.rectTransform.localScale = Vector3.one * (1f - p);
                var c = old.color; c.a = 1f - p; old.color = c;
                yield return null;
            }
            Destroy(old.gameObject);
            t = 0f;
            const float In = 0.35f;
            while (t < In)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / In);
                float s = p < 0.6f ? Mathf.Lerp(0f, 1.5f, p / 0.6f) : Mathf.Lerp(1.5f, 1f, (p - 0.6f) / 0.4f);
                host.localScale = Vector3.one * s;
                ring.rectTransform.localScale = Vector3.one * (0.6f + 1.2f * p);
                ring.color = new Color(flash.r, flash.g, flash.b, 0.9f * (1f - p));
                yield return null;
            }
            host.localScale = Vector3.one;
            Destroy(ring.gameObject);
        }

        private static System.Collections.IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameSpeed.AnimationDelta;
                yield return null;
            }
        }

        private static System.Collections.IEnumerator PopRect(RectTransform rect, float peak, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / duration);
                float s = p < 0.45f ? Mathf.Lerp(1f, peak, p / 0.45f) : Mathf.Lerp(peak, 1f, (p - 0.45f) / 0.55f);
                if (rect != null) { rect.localScale = new Vector3(s, s, 1f); }
                yield return null;
            }
            if (rect != null) { rect.localScale = Vector3.one; }
        }

        private System.Collections.IEnumerator RiseText(GardenCell cell, string text)
        {
            var label = UIFactory.CreateText(cell.Root, "Harvest", text, 22, DarkPollenGold);
            label.raycastTarget = false;
            var labelOutline = label.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = Dark;
            labelOutline.effectDistance = new Vector2(2f, -2f);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(80f, 28f);
            float t = 0f;
            const float Duration = 0.9f;
            while (t < Duration && label != null)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / Duration);
                label.rectTransform.anchoredPosition = new Vector2(0f, -20f + 34f * p);
                var color = label.color;
                color.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
                label.color = color;
                yield return null;
            }
            if (label != null) { Destroy(label.gameObject); }
        }

        private void AddRow(ref int row, string label, string value, bool highlight, float rowHeight, int fontSize, ref float y)
        {
            var left = UIFactory.CreateText(_rows, "Label" + row, label, highlight ? 24 : fontSize, UITheme.TextMuted, TextAnchor.MiddleLeft);
            left.raycastTarget = false;
            left.rectTransform.anchorMin = new Vector2(0f, 1f);
            left.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 1f);
            left.rectTransform.offsetMin = new Vector2(18f, -(y + rowHeight));
            left.rectTransform.offsetMax = new Vector2(0f, -y);
            var right = UIFactory.CreateText(_rows, "Value" + row, value, highlight ? 28 : fontSize + 2, highlight ? DarkPollenGold : UITheme.TextPrimary, TextAnchor.MiddleRight);
            right.raycastTarget = false;
            right.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 1f);
            right.rectTransform.offsetMin = new Vector2(0f, -(y + rowHeight));
            // Every figure here is Pollen: the Pollen icon sits right after it.
            float iconSize = Mathf.Clamp(rowHeight * 0.72f, 22f, 32f);
            right.rectTransform.offsetMax = new Vector2(-(18f + iconSize + 6f), -y);
            var icon = UIFactory.CreatePanel(_rows, "PollenIcon" + row, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            if (VisualDefaults.PollenSprite != null)
            {
                icon.sprite = VisualDefaults.PollenSprite;
            }
            else
            {
                icon.color = DarkPollenGold;
            }
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(1f, 1f);
            icon.rectTransform.pivot = new Vector2(1f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(-18f, -(y + rowHeight * 0.5f));
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            y += rowHeight;
            row++;
        }

        private System.Collections.IEnumerator PopIn()
        {
            float t = 0f;
            const float Duration = 0.3f;
            while (t < Duration)
            {
                t += GameSpeed.AnimationDelta;
                float p = Mathf.Clamp01(t / Duration);
                float s = p < 0.7f ? Mathf.Lerp(0.8f, 1.06f, p / 0.7f) : Mathf.Lerp(1.06f, 1f, (p - 0.7f) / 0.3f);
                _card.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _card.localScale = Vector3.one;
        }

        private void OnContinue()
        {
            if (_growthPlay != null) { StopCoroutine(_growthPlay); _growthPlay = null; FinishGrowth(); }
            _root.gameObject.SetActive(false);
            var next = _onContinue;
            _onContinue = null;
            if (next != null) { next(); }
        }

        public void Hide()
        {
            if (_growthPlay != null) { StopCoroutine(_growthPlay); _growthPlay = null; FinishGrowth(); }
            _root.gameObject.SetActive(false);
        }
    }
}
