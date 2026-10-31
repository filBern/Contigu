using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Keeps the board alive while the player thinks: sparkles twinkle over the planted tiles and the seeds in hand, and every few seconds a glint
    /// sweeps diagonally across the garden. Purely decorative overlays on their own layer (never block a click), off with reduced motion.
    /// </summary>
    public sealed class IdleLifeView : MonoBehaviour
    {
        private const float TwinkleMin = 0.45f;
        private const float TwinkleMax = 1.1f;
        private const float SparkleLife = 0.95f;
        private const float GlintEvery = 8f;
        private const float GlintDelayPerStep = 0.075f;
        private const float GlintLife = 0.7f;
        private const int PoolSize = 64;
        private static readonly Color SparkleColor = new Color(1f, 0.97f, 0.85f, 0.95f);

        private sealed class Sparkle
        {
            public RectTransform Holder;
            public Image[] Bars;
            public float Start;
            public float Life;
            public float Size;
            public bool Active;
        }

        private struct Scheduled
        {
            public float At;
            public Transform Target;
            public float Size;
        }

        private RectTransform _layer;
        private Func<GridManager> _grid;
        private GridView _gridView;
        private HandView _handView;
        private Func<bool> _boardVisible;
        private readonly List<Sparkle> _pool = new List<Sparkle>();
        private readonly List<Scheduled> _scheduled = new List<Scheduled>();
        private float _nextTwinkle;
        private float _nextGlint;

        public void Build(Transform parent, Func<GridManager> grid, GridView gridView, HandView handView, Func<bool> boardVisible)
        {
            _grid = grid;
            _gridView = gridView;
            _handView = handView;
            _boardVisible = boardVisible;
            _layer = UIFactory.CreateUIObject("IdleLife", parent);
            UIFactory.StretchFull(_layer);
            for (int i = 0; i < PoolSize; i++)
            {
                var holder = UIFactory.CreateUIObject("Sparkle" + i, _layer);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = new Vector2(1f, 1f);
                var bars = new Image[2];
                for (int b = 0; b < 2; b++)
                {
                    var bar = UIFactory.CreatePanel(holder, "Bar" + b, SparkleColor);
                    bar.raycastTarget = false;
                    bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    bars[b] = bar;
                }
                holder.gameObject.SetActive(false);
                _pool.Add(new Sparkle { Holder = holder, Bars = bars });
            }
            _nextTwinkle = Time.unscaledTime + 1.5f;
            _nextGlint = Time.unscaledTime + GlintEvery;
        }

        private void Update()
        {
            if (_layer == null)
            {
                return;
            }
            float now = Time.unscaledTime;
            bool live = !MotionSettings.Reduced && _boardVisible != null && _boardVisible();
            if (!live)
            {
                for (int i = 0; i < _pool.Count; i++)
                {
                    if (_pool[i].Active) { Deactivate(_pool[i]); }
                }
                _scheduled.Clear();
                _nextTwinkle = Mathf.Max(_nextTwinkle, now + 1f);
                _nextGlint = Mathf.Max(_nextGlint, now + GlintEvery);
                return;
            }

            if (now >= _nextTwinkle)
            {
                _nextTwinkle = now + UnityEngine.Random.Range(TwinkleMin, TwinkleMax);
                Twinkle(now);
            }
            if (now >= _nextGlint)
            {
                _nextGlint = now + GlintEvery + UnityEngine.Random.Range(-1.5f, 1.5f);
                ScheduleGlint(now);
            }
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (now >= _scheduled[i].At)
                {
                    if (_scheduled[i].Target != null) { Spawn(_scheduled[i].Target, _scheduled[i].Size, GlintLife, now, 0.2f); }
                    _scheduled.RemoveAt(i);
                }
            }
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Active) { Animate(_pool[i], now); }
            }
        }

        /// <summary>A sparkle on a random planted tile (mostly) or a seed in hand.</summary>
        private void Twinkle(float now)
        {
            var grid = _grid != null ? _grid() : null;
            if (grid != null && UnityEngine.Random.value < 0.7f)
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    int x = UnityEngine.Random.Range(0, GridManager.Size);
                    int y = UnityEngine.Random.Range(0, GridManager.Size);
                    if (!grid.GetCell(x, y).IsFilled) { continue; }
                    var cell = _gridView.GetCellTransform(x, y);
                    if (cell != null) { Spawn(cell, 26f, SparkleLife, now, 0.3f); return; }
                }
            }
            if (_handView != null)
            {
                int slot = UnityEngine.Random.Range(0, DeckManager.MaxHandSize);
                var preview = _handView.GetSlotPreviewTransform(slot);
                if (preview != null && preview.gameObject.activeInHierarchy && preview.childCount > 0) { Spawn(preview, 24f, SparkleLife, now, 0.35f); }
            }
        }

        /// <summary>A small glint on every planted tile, one diagonal step after the other.</summary>
        private void ScheduleGlint(float now)
        {
            var grid = _grid != null ? _grid() : null;
            if (grid == null) { return; }
            int filled = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (!grid.GetCell(x, y).IsFilled) { continue; }
                    var cell = _gridView.GetCellTransform(x, y);
                    if (cell == null) { continue; }
                    filled++;
                    _scheduled.Add(new Scheduled { At = now + (x + y) * GlintDelayPerStep, Target = cell, Size = 16f });
                }
            }
            if (filled < 3) { _scheduled.Clear(); }
        }

        private void Spawn(Transform target, float size, float life, float now, float jitter)
        {
            Sparkle free = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Active) { free = _pool[i]; break; }
            }
            if (free == null) { return; }
            free.Active = true;
            free.Start = now;
            free.Life = life;
            free.Size = size;
            free.Holder.gameObject.SetActive(true);
            free.Holder.position = target.position;
            var rect = target as RectTransform;
            if (rect != null)
            {
                // Somewhere on the tile rather than always at its center.
                var offset = new Vector2(UnityEngine.Random.Range(-jitter, jitter) * rect.rect.width, UnityEngine.Random.Range(-jitter, jitter) * rect.rect.height);
                free.Holder.position += free.Holder.parent.TransformVector(new Vector3(offset.x, offset.y, 0f));
            }
            free.Bars[0].rectTransform.sizeDelta = new Vector2(size, Mathf.Max(2f, size * 0.14f));
            free.Bars[1].rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, size * 0.14f), size);
            free.Holder.localScale = Vector3.zero;
        }

        private static void Animate(Sparkle sparkle, float now)
        {
            float p = (now - sparkle.Start) / sparkle.Life;
            if (p >= 1f)
            {
                Deactivate(sparkle);
                return;
            }
            float grow = Mathf.Sin(p * Mathf.PI);
            sparkle.Holder.localScale = new Vector3(grow, grow, 1f);
            sparkle.Holder.localRotation = Quaternion.Euler(0f, 0f, p * 80f);
        }

        private static void Deactivate(Sparkle sparkle)
        {
            sparkle.Active = false;
            sparkle.Holder.gameObject.SetActive(false);
        }
    }
}
