using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The garden behind the board is alive: butterflies and a bee wander across (more butterflies as the round is won), leaves drift,
    /// and the weather paints its own particles (rain streaks, sun motes, wind-blown leaves, frost sparkles). Purely decorative: sits behind
    /// every other UI element and never blocks input.
    /// </summary>
    public sealed class AmbientLifeView : MonoBehaviour
    {
        private enum Kind { Butterfly, Bee, Leaf, Rain, Sparkle, SunMote }

        private sealed class Critter
        {
            public Kind Kind;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Phase;
            public float Size;
            public float Timer;
        }

        private const int MaxButterflies = 5;

        private RectTransform _area;
        private readonly List<Critter> _critters = new List<Critter>();
        private WeatherId _weather = WeatherId.Clear;
        private float _progress;
        private int _butterflyTarget = 1;

        public void Build(Transform parent)
        {
            _area = UIFactory.CreateUIObject("AmbientLife", parent);
            UIFactory.StretchFull(_area);
            _area.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Rebuild();
        }

        /// <summary>Swaps the weather particles (butterflies and the bee keep wandering).</summary>
        public void SetWeather(WeatherId weather)
        {
            if (weather == _weather)
            {
                return;
            }
            _weather = weather;
            Rebuild();
        }

        /// <summary>0-1: how far along the round is; the more of it is won, the more butterflies visit.</summary>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            int target = 1 + Mathf.RoundToInt(_progress * (MaxButterflies - 1));
            if (target != _butterflyTarget)
            {
                _butterflyTarget = target;
                SyncButterflies();
            }
        }

        private void Rebuild()
        {
            for (int i = _critters.Count - 1; i >= 0; i--)
            {
                Destroy(_critters[i].Image.gameObject);
            }
            _critters.Clear();

            for (int i = 0; i < _butterflyTarget; i++) { Spawn(Kind.Butterfly); }
            if (_weather == WeatherId.Clear || _weather == WeatherId.Sunshine || _weather == WeatherId.GoldenHour) { Spawn(Kind.Bee); }
            int leaves = _weather == WeatherId.Wind ? 10 : 3;
            for (int i = 0; i < leaves; i++) { Spawn(Kind.Leaf); }
            if (_weather == WeatherId.Rain) { for (int i = 0; i < 70; i++) { Spawn(Kind.Rain); } }
            if (_weather == WeatherId.Frost) { for (int i = 0; i < 22; i++) { Spawn(Kind.Sparkle); } }
            if (_weather == WeatherId.Sunshine) { for (int i = 0; i < 12; i++) { Spawn(Kind.SunMote); } }
            if (_weather == WeatherId.Dew) { for (int i = 0; i < 12; i++) { Spawn(Kind.Sparkle); } }
            if (_weather == WeatherId.GoldenHour) { for (int i = 0; i < 20; i++) { Spawn(Kind.SunMote); } }
            if (_weather == WeatherId.PollenShower) { for (int i = 0; i < 26; i++) { Spawn(Kind.SunMote); } Spawn(Kind.Bee); Spawn(Kind.Bee); }
        }

        private void SyncButterflies()
        {
            int have = 0;
            for (int i = 0; i < _critters.Count; i++)
            {
                if (_critters[i].Kind == Kind.Butterfly) { have++; }
            }
            while (have < _butterflyTarget)
            {
                Spawn(Kind.Butterfly);
                have++;
            }
        }

        private Rect Bounds()
        {
            var r = _area.rect;
            return r.width > 1f ? r : new Rect(-640f, -400f, 1280f, 800f);
        }

        private static string SpriteName(Kind kind)
        {
            switch (kind)
            {
                case Kind.Butterfly: return "Butterfly";
                case Kind.Bee: return "Bee";
                case Kind.Leaf: return "Leaf";
                case Kind.Rain: return "Raindrop";
                case Kind.Sparkle: return "Sparkle";
                default: return "SunMote";
            }
        }

        private void Spawn(Kind kind)
        {
            var sprite = VisualDefaults.GetAmbientSprite(SpriteName(kind));
            if (sprite == null)
            {
                return;
            }
            var image = UIFactory.CreatePanel(_area, "Ambient" + kind, Color.white);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.preserveAspect = true;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var b = Bounds();
            var c = new Critter { Kind = kind, Image = image, Phase = Random.value * 20f };
            c.Position = new Vector2(Random.Range(b.xMin, b.xMax), Random.Range(b.yMin, b.yMax));
            switch (kind)
            {
                case Kind.Butterfly:
                    c.Size = Random.Range(34f, 48f);
                    c.Velocity = new Vector2(Random.Range(28f, 55f) * (Random.value < 0.5f ? -1f : 1f), Random.Range(-8f, 8f));
                    break;
                case Kind.Bee:
                    c.Size = 32f;
                    c.Velocity = new Vector2(Random.Range(40f, 70f), 0f);
                    break;
                case Kind.Leaf:
                    c.Size = Random.Range(22f, 34f);
                    c.Velocity = _weather == WeatherId.Wind ? new Vector2(Random.Range(-320f, -200f), Random.Range(-70f, -20f)) : new Vector2(Random.Range(-14f, 14f), Random.Range(-44f, -26f));
                    break;
                case Kind.Rain:
                    c.Size = Random.Range(12f, 20f);
                    c.Velocity = new Vector2(Random.Range(-70f, -50f), Random.Range(-620f, -480f));
                    break;
                case Kind.Sparkle:
                    c.Size = Random.Range(12f, 24f);
                    c.Velocity = new Vector2(Random.Range(-10f, 10f), Random.Range(-34f, -16f));
                    break;
                default:
                    c.Size = Random.Range(14f, 24f);
                    c.Velocity = new Vector2(Random.Range(-6f, 6f), Random.Range(14f, 30f));
                    break;
            }
            rt.sizeDelta = new Vector2(c.Size, c.Size);
            _critters.Add(c);
        }

        private void Update()
        {
            if (_area == null)
            {
                return;
            }
            var b = Bounds();
            float dt = Time.deltaTime;
            float now = Time.time;
            for (int i = 0; i < _critters.Count; i++)
            {
                var c = _critters[i];
                float t = now + c.Phase;
                float rot = 0f;
                float scaleX = 1f;
                float alpha = 0.9f;
                var move = c.Velocity;
                switch (c.Kind)
                {
                    case Kind.Butterfly:
                        move += new Vector2(0f, Mathf.Sin(t * 1.6f) * 26f);
                        scaleX = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 9f));
                        rot = Mathf.Sin(t * 1.6f) * 12f - 90f * 0f;
                        if (c.Velocity.x < 0f) { scaleX = -scaleX; }
                        alpha = 0.85f;
                        break;
                    case Kind.Bee:
                        c.Timer -= dt;
                        if (c.Timer <= 0f)
                        {
                            c.Timer = Random.Range(0.8f, 1.8f);
                            c.Velocity = new Vector2(Random.Range(-90f, 90f), Random.Range(-60f, 60f));
                        }
                        scaleX = c.Velocity.x < 0f ? -1f : 1f;
                        rot = Mathf.Sin(t * 14f) * 4f;
                        alpha = 0.9f;
                        break;
                    case Kind.Leaf:
                        move += new Vector2(Mathf.Sin(t * 1.3f) * 24f, 0f);
                        rot = t * 70f;
                        alpha = 0.7f;
                        break;
                    case Kind.Rain:
                        rot = Mathf.Atan2(c.Velocity.x, -c.Velocity.y) * Mathf.Rad2Deg;
                        alpha = 0.5f;
                        break;
                    case Kind.Sparkle:
                        float tw = 0.5f + 0.5f * Mathf.Sin(t * 3f);
                        scaleX = 0.6f + 0.4f * tw;
                        rot = t * 25f;
                        alpha = 0.35f + 0.55f * tw;
                        break;
                    case Kind.SunMote:
                        alpha = 0.25f + 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 1.8f));
                        break;
                }
                c.Position += move * dt;
                // Wrap around the screen with a margin so nothing pops in the middle of the view.
                float m = c.Size;
                if (c.Position.x < b.xMin - m) { c.Position.x = b.xMax + m; }
                else if (c.Position.x > b.xMax + m) { c.Position.x = b.xMin - m; }
                if (c.Position.y < b.yMin - m) { c.Position.y = b.yMax + m; }
                else if (c.Position.y > b.yMax + m) { c.Position.y = b.yMin - m; }

                var rt = c.Image.rectTransform;
                rt.anchoredPosition = c.Position;
                rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                rt.localScale = new Vector3(scaleX, 1f, 1f);
                c.Image.color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }
}
