using System;
using System.Collections.Generic;
using Contigu.Core;
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
        private static float GlintAfterIdle { get { return UITweaks.Current.glintAfterIdle; } } // seconds without a tile placed, counted once the scoring of the last one is over
        private const float GlintSweepDuration = 1.1f;
        private const float GlintLife = 0.7f;
        private const float BandAngle = 18f;
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
        private Func<bool> _playerCanAct;
        private float _idleSince = -1f;

        // The sweeping band: a soft slanted streak of light that crosses the garden once in a while, clipped to the garden's outline.
        private RectTransform _bandMask;
        private RectTransform _band;
        private float _bandStart = -100f;
        private Vector3 _bandLeft;
        private Vector3 _bandRight;

        /// <param name="playerCanAct">True once the player can place a tile again (nothing is being scored); the idle count starts then.</param>
        public void Build(Transform parent, Func<GridManager> grid, GridView gridView, HandView handView, Func<bool> boardVisible, Func<bool> playerCanAct)
        {
            _grid = grid;
            _gridView = gridView;
            _handView = handView;
            _boardVisible = boardVisible;
            _playerCanAct = playerCanAct;
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
            _bandMask = UIFactory.CreateUIObject("GlintMask", _layer);
            _bandMask.anchorMin = _bandMask.anchorMax = _bandMask.pivot = new Vector2(0.5f, 0.5f);
            _bandMask.gameObject.AddComponent<RectMask2D>();
            _band = UIFactory.CreateUIObject("Band", _bandMask);
            _band.anchorMin = _band.anchorMax = _band.pivot = new Vector2(0.5f, 0.5f);
            _band.localRotation = Quaternion.Euler(0f, 0f, BandAngle);
            // Three bars side by side: a faint wide halo around a brighter core.
            foreach (var bar in new[] { (width: 150f, alpha: 0.07f), (width: 90f, alpha: 0.10f), (width: 34f, alpha: 0.16f) })
            {
                var image = UIFactory.CreatePanel(_band, "Bar", new Color(1f, 1f, 1f, bar.alpha));
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                image.rectTransform.sizeDelta = new Vector2(bar.width, 2000f);
            }
            _bandMask.gameObject.SetActive(false);
            _nextTwinkle = Time.unscaledTime + 1.5f;
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
                _bandStart = -100f;
                _bandMask.gameObject.SetActive(false);
                _nextTwinkle = Mathf.Max(_nextTwinkle, now + 1f);
                _idleSince = -1f;
                return;
            }

            if (now >= _nextTwinkle)
            {
                _nextTwinkle = now + UnityEngine.Random.Range(TwinkleMin, TwinkleMax);
                Twinkle(now);
            }
            // The band shows up when the player has not placed a tile for a while: the count starts once the last placement has been fully scored
            // (the hand is playable again), restarts at every placement, and repeats while the player keeps thinking.
            bool canAct = _playerCanAct == null || _playerCanAct();
            if (!canAct)
            {
                _idleSince = -1f;
            }
            else if (_idleSince < 0f)
            {
                _idleSince = now;
            }
            else if (now - _idleSince >= GlintAfterIdle)
            {
                _idleSince = now;
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
            AnimateBand(now);
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Active) { Animate(_pool[i], now); }
            }
        }

        private void AnimateBand(float now)
        {
            float p = (now - _bandStart) / GlintSweepDuration;
            if (p < 0f || p > 1f)
            {
                if (_bandMask.gameObject.activeSelf) { _bandMask.gameObject.SetActive(false); }
                return;
            }
            if (!_bandMask.gameObject.activeSelf) { _bandMask.gameObject.SetActive(true); }
            float eased = p * p * (3f - 2f * p);
            _band.position = Vector3.Lerp(_bandLeft, _bandRight, eased);
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
                if (preview != null && preview.gameObject.activeInHierarchy && preview.childCount > 0 && _handView.IsSlotPlayable(slot)) { Spawn(preview, 24f, SparkleLife, now, 0.35f); }
            }
        }

        /// <summary>The band crosses the garden from left to right; every planted tile throws a small sparkle at the moment the band passes over it.</summary>
        private void ScheduleGlint(float now)
        {
            var grid = _grid != null ? _grid() : null;
            if (grid == null) { return; }
            var first = _gridView.GetCellTransform(0, 0);
            var last = _gridView.GetCellTransform(GridManager.Size - 1, GridManager.Size - 1);
            if (first == null || last == null) { return; }
            var corners = new Vector3[4];
            first.GetWorldCorners(corners);
            var low = corners[0];
            last.GetWorldCorners(corners);
            var high = corners[2];
            float minX = Mathf.Min(low.x, high.x), maxX = Mathf.Max(low.x, high.x);
            float minY = Mathf.Min(low.y, high.y), maxY = Mathf.Max(low.y, high.y);

            // The mask is the garden's outline, in this layer's own units.
            float scale = Mathf.Max(0.0001f, _layer.lossyScale.x);
            _bandMask.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, low.z);
            _bandMask.sizeDelta = new Vector2((maxX - minX) / scale, (maxY - minY) / scale);
            float margin = 120f * scale;
            float midY = (minY + maxY) * 0.5f;
            _bandLeft = new Vector3(minX - margin, midY, low.z);
            _bandRight = new Vector3(maxX + margin, midY, low.z);
            _bandStart = now;

            int filled = 0;
            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    if (!grid.GetCell(x, y).IsFilled) { continue; }
                    var cell = _gridView.GetCellTransform(x, y);
                    if (cell == null) { continue; }
                    filled++;
                    // The moment the (eased) band is over this tile's column.
                    float along = Mathf.InverseLerp(minX - margin, maxX + margin, cell.position.x);
                    float linear = EaseInverse(along);
                    _scheduled.Add(new Scheduled { At = now + linear * GlintSweepDuration, Target = cell, Size = 18f });
                }
            }
            if (filled == 0) { _scheduled.Clear(); }
        }

        /// <summary>Inverse of smoothstep, so a sparkle fires when the eased band actually reaches its column.</summary>
        private static float EaseInverse(float y)
        {
            return 0.5f - Mathf.Sin(Mathf.Asin(1f - 2f * Mathf.Clamp01(y)) / 3f);
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
