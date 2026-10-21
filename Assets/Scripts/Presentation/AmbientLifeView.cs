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
        public enum ParticleKind { Butterfly, Bee, Leaf, Rain, Sparkle, SunMote, Snow, Petal, Maple, Drip }

        private sealed class Critter
        {
            public ParticleKind Kind;
            public Color Tint = Color.white;
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

        private ChallengeAmbiance _ambiance = ChallengeAmbiance.For(ChallengeId.Classic);

        /// <summary>Switches to a challenge's own ambiance (its creatures and particles) — the weather still adds its own on top.</summary>
        public void SetChallenge(ChallengeAmbiance ambiance)
        {
            if (ambiance == _ambiance)
            {
                return;
            }
            _ambiance = ambiance;
            _butterflyTarget = Mathf.Min(_butterflyTarget, Mathf.Max(0, ambiance.MaxButterflies));
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
            int cap = Mathf.Min(MaxButterflies, _ambiance.MaxButterflies);
            int target = cap <= 0 ? 0 : 1 + Mathf.RoundToInt(_progress * (cap - 1));
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

            for (int i = 0; i < _butterflyTarget; i++) { Spawn(ParticleKind.Butterfly, _ambiance.ButterflyTint); }
            if (_ambiance.MaxButterflies > 0 && (_weather == WeatherId.Clear || _weather == WeatherId.Sunshine || _weather == WeatherId.GoldenHour)) { Spawn(ParticleKind.Bee); }
            for (int i = 0; i < _ambiance.Bees; i++) { Spawn(ParticleKind.Bee); }
            for (int p = 0; p < _ambiance.Particles.Length; p++)
            {
                for (int i = 0; i < _ambiance.Particles[p].Count; i++) { Spawn(_ambiance.Particles[p].Kind, _ambiance.Particles[p].Tint); }
            }
            int leaves = _weather == WeatherId.Hurricane ? 16 : _weather == WeatherId.Wind ? 10 : 3;
            for (int i = 0; i < leaves; i++) { Spawn(ParticleKind.Leaf); }
            if (_weather == WeatherId.Rain) { for (int i = 0; i < 70; i++) { Spawn(ParticleKind.Rain); } }
            if (_weather == WeatherId.Frost) { for (int i = 0; i < 22; i++) { Spawn(ParticleKind.Sparkle); } }
            if (_weather == WeatherId.Sunshine) { for (int i = 0; i < 12; i++) { Spawn(ParticleKind.SunMote); } }
            if (_weather == WeatherId.Hurricane) { for (int i = 0; i < 40; i++) { Spawn(ParticleKind.Rain); } }
            if (_weather == WeatherId.Heatwave) { for (int i = 0; i < 16; i++) { Spawn(ParticleKind.SunMote); } }
            if (_weather == WeatherId.Dew) { for (int i = 0; i < 12; i++) { Spawn(ParticleKind.Sparkle); } }
            if (_weather == WeatherId.GoldenHour) { for (int i = 0; i < 20; i++) { Spawn(ParticleKind.SunMote); } }
            if (_weather == WeatherId.PollenShower) { for (int i = 0; i < 26; i++) { Spawn(ParticleKind.SunMote); } Spawn(ParticleKind.Bee); Spawn(ParticleKind.Bee); }
        }

        private void SyncButterflies()
        {
            int have = 0;
            for (int i = 0; i < _critters.Count; i++)
            {
                if (_critters[i].Kind == ParticleKind.Butterfly) { have++; }
            }
            while (have < _butterflyTarget)
            {
                Spawn(ParticleKind.Butterfly, _ambiance.ButterflyTint);
                have++;
            }
        }

        private Rect Bounds()
        {
            var r = _area.rect;
            return r.width > 1f ? r : new Rect(-640f, -400f, 1280f, 800f);
        }

        private static string SpriteName(ParticleKind kind)
        {
            switch (kind)
            {
                case ParticleKind.Butterfly: return "Butterfly";
                case ParticleKind.Bee: return "Bee";
                case ParticleKind.Leaf: return "Leaf";
                case ParticleKind.Rain: return "Raindrop";
                case ParticleKind.Sparkle: return "Sparkle";
                case ParticleKind.Snow: return "Snowflake";
                case ParticleKind.Petal: return "Petal";
                case ParticleKind.Maple: return "MapleLeaf";
                case ParticleKind.Drip: return "Raindrop";
                default: return "SunMote";
            }
        }

        private void Spawn(ParticleKind kind, Color? tint = null)
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
            var c = new Critter { Kind = kind, Image = image, Phase = Random.value * 20f, Tint = tint ?? Color.white };
            c.Position = new Vector2(Random.Range(b.xMin, b.xMax), Random.Range(b.yMin, b.yMax));
            switch (kind)
            {
                case ParticleKind.Butterfly:
                    c.Size = Random.Range(34f, 48f);
                    c.Velocity = new Vector2(Random.Range(28f, 55f) * (Random.value < 0.5f ? -1f : 1f), Random.Range(-8f, 8f));
                    break;
                case ParticleKind.Bee:
                    c.Size = 32f;
                    c.Velocity = new Vector2(Random.Range(40f, 70f), 0f);
                    break;
                case ParticleKind.Leaf:
                    c.Size = Random.Range(22f, 34f);
                    c.Velocity = _weather == WeatherId.Wind || _weather == WeatherId.Hurricane ? new Vector2(Random.Range(-320f, -200f), Random.Range(-70f, -20f)) : new Vector2(Random.Range(-14f, 14f), Random.Range(-44f, -26f));
                    break;
                case ParticleKind.Rain:
                    c.Size = Random.Range(12f, 20f);
                    c.Velocity = new Vector2(Random.Range(-70f, -50f), Random.Range(-620f, -480f));
                    break;
                case ParticleKind.Snow:
                    c.Size = Random.Range(12f, 26f);
                    c.Velocity = new Vector2(Random.Range(-14f, 14f), Random.Range(-52f, -26f));
                    break;
                case ParticleKind.Petal:
                    c.Size = Random.Range(18f, 28f);
                    c.Velocity = new Vector2(Random.Range(-22f, 22f), Random.Range(-42f, -22f));
                    break;
                case ParticleKind.Maple:
                    c.Size = Random.Range(26f, 40f);
                    c.Velocity = new Vector2(Random.Range(-18f, 18f), Random.Range(-52f, -28f));
                    break;
                case ParticleKind.Drip:
                    c.Size = Random.Range(12f, 18f);
                    c.Velocity = new Vector2(0f, Random.Range(-110f, -60f));
                    break;
                case ParticleKind.Sparkle:
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
                    case ParticleKind.Butterfly:
                        move += new Vector2(0f, Mathf.Sin(t * 1.6f) * 26f);
                        scaleX = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 9f));
                        rot = Mathf.Sin(t * 1.6f) * 12f - 90f * 0f;
                        if (c.Velocity.x < 0f) { scaleX = -scaleX; }
                        alpha = 0.85f;
                        break;
                    case ParticleKind.Bee:
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
                    case ParticleKind.Leaf:
                        move += new Vector2(Mathf.Sin(t * 1.3f) * 24f, 0f);
                        rot = t * 70f;
                        alpha = 0.7f;
                        break;
                    case ParticleKind.Rain:
                        rot = Mathf.Atan2(c.Velocity.x, -c.Velocity.y) * Mathf.Rad2Deg;
                        alpha = 0.5f;
                        break;
                    case ParticleKind.Snow:
                        move += new Vector2(Mathf.Sin(t * 0.9f) * 16f, 0f);
                        rot = t * 25f;
                        alpha = 0.8f;
                        break;
                    case ParticleKind.Petal:
                        move += new Vector2(Mathf.Sin(t * 1.1f) * 28f, 0f);
                        rot = Mathf.Sin(t * 1.4f) * 40f;
                        scaleX = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t * 1.4f));
                        alpha = 0.75f;
                        break;
                    case ParticleKind.Maple:
                        move += new Vector2(Mathf.Sin(t * 1.2f) * 32f, 0f);
                        rot = Mathf.Sin(t * 1.0f) * 55f;
                        scaleX = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 1.9f));
                        alpha = 0.85f;
                        break;
                    case ParticleKind.Drip:
                        alpha = 0.55f;
                        break;
                    case ParticleKind.Sparkle:
                        float tw = 0.5f + 0.5f * Mathf.Sin(t * 3f);
                        scaleX = 0.6f + 0.4f * tw;
                        rot = t * 25f;
                        alpha = 0.35f + 0.55f * tw;
                        break;
                    case ParticleKind.SunMote:
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
                c.Image.color = new Color(c.Tint.r, c.Tint.g, c.Tint.b, alpha);
            }
        }
    }
}
