using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The main menu's living notebook page: crayon doodles (Icons/Doodles: flowers, scribbles, a star, a leaf) drift slowly up the page, sway, turn and "boil"
    /// like a flipbook, behind the title and the buttons. A tap on the bare page makes a flower bloom where the finger is, with a few petals flying off.
    /// With reduced motion the doodles stand still and a tap only shows the flower.
    /// </summary>
    public sealed class MenuDoodleLayer : MonoBehaviour, IPointerClickHandler
    {
        private const int DoodleCount = 16;
        private const int KindCount = 8;
        private const float BoilSeconds = 0.16f;

        private sealed class Doodle
        {
            public RectTransform Rect;
            public float BaseX, Y, Speed, SwayAmp, SwayFreq, Phase, BaseRot, RotAmp, Size;
        }

        private RectTransform _rect;
        private readonly List<Doodle> _doodles = new List<Doodle>();
        private float _width = 1280f, _height = 720f;

        public static MenuDoodleLayer Build(Transform parent)
        {
            var go = UIFactory.CreateUIObject("MenuDoodleLayer", parent);
            UIFactory.StretchFull(go);
            var catcher = go.gameObject.AddComponent<Image>();
            catcher.color = new Color(1f, 1f, 1f, 0f);          // invisible, but it catches taps on the bare page (buttons in front still get theirs)
            catcher.raycastTarget = true;
            var layer = go.gameObject.AddComponent<MenuDoodleLayer>();
            layer._rect = go;
            layer.Populate();
            return layer;
        }

        private void Populate()
        {
            var canvas = (RectTransform)_rect.parent;
            if (canvas != null && canvas.rect.width > 100f) { _width = canvas.rect.width; _height = canvas.rect.height; }
            var rng = new System.Random(11);
            for (int i = 0; i < DoodleCount; i++)
            {
                var sprite = Resources.Load<Sprite>("Icons/Doodles/Doodle_" + (i % KindCount));
                if (sprite == null) { continue; }
                var image = UIFactory.CreatePanel(_rect, "Doodle" + i, new Color(1f, 1f, 1f, 0.5f));
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                var d = new Doodle
                {
                    Rect = image.rectTransform,
                    BaseX = Rnd(rng, 0.04f, 0.96f) * _width,
                    Y = Rnd(rng, 0f, 1f) * _height,
                    Speed = Rnd(rng, 10f, 26f),
                    SwayAmp = Rnd(rng, 14f, 46f),
                    SwayFreq = Rnd(rng, 0.25f, 0.6f),
                    Phase = Rnd(rng, 0f, 6.28f),
                    BaseRot = Rnd(rng, -25f, 25f),
                    RotAmp = Rnd(rng, 5f, 14f),
                    Size = Rnd(rng, 70f, 150f)
                };
                d.Rect.anchorMin = d.Rect.anchorMax = Vector2.zero;
                d.Rect.pivot = new Vector2(0.5f, 0.5f);
                d.Rect.sizeDelta = new Vector2(d.Size, d.Size);
                _doodles.Add(d);
            }
            Animate(0f);
        }

        private static float Rnd(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private void Update()
        {
            if (MotionSettings.Reduced) { return; }
            Animate(Time.unscaledDeltaTime);
        }

        private void Animate(float dt)
        {
            float t = Time.unscaledTime;
            // "Boiling": every doodle jumps a few degrees from one drawing-frame to the next.
            float boil = Mathf.Floor(t / BoilSeconds) % 2f == 0f ? 1.5f : -1.5f;
            for (int i = 0; i < _doodles.Count; i++)
            {
                var d = _doodles[i];
                d.Y += d.Speed * dt;
                if (d.Y > _height + d.Size) { d.Y = -d.Size; }
                float x = d.BaseX + Mathf.Sin(t * d.SwayFreq + d.Phase) * d.SwayAmp;
                d.Rect.anchoredPosition = new Vector2(x, d.Y);
                d.Rect.localRotation = Quaternion.Euler(0f, 0f, d.BaseRot + Mathf.Sin(t * 0.5f + d.Phase) * d.RotAmp + boil * (i % 2 == 0 ? 1f : -1f));
                float pulse = 1f + Mathf.Sin(t * 0.9f + d.Phase) * 0.035f;
                d.Rect.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out local)) { return; }
            // Local point is relative to the rect's pivot: the doodles use a bottom-left anchor, so shift to that origin.
            Vector2 at = local + new Vector2(_width * _rect.pivot.x, _height * _rect.pivot.y);
            StartCoroutine(Bloom(at));
        }

        private IEnumerator Bloom(Vector2 at)
        {
            int[] kinds = { 2, 3, 6, 4 };
            var main = Spawn(kinds[Random.Range(0, kinds.Length)], at, 150f, 1f);
            var petals = new List<RectTransform>();
            var directions = new List<Vector2>();
            for (int i = 0; i < 5; i++)
            {
                petals.Add(Spawn(5, at, 46f, 1f));
                float a = (i / 5f) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                directions.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
            }
            float duration = MotionSettings.Reduced ? 0.4f : 0.9f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float grow = p < 0.25f ? Mathf.Lerp(0f, 1.18f, p / 0.25f) : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((p - 0.25f) / 0.15f));
                float alpha = p < 0.55f ? 1f : 1f - (p - 0.55f) / 0.45f;
                if (main != null) { main.localScale = Vector3.one * grow; main.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha); main.localRotation = Quaternion.Euler(0f, 0f, (1f - p) * 25f); }
                for (int i = 0; i < petals.Count; i++)
                {
                    if (petals[i] == null) { continue; }
                    petals[i].anchoredPosition = at + directions[i] * (p * 150f);
                    petals[i].localScale = Vector3.one * Mathf.Sin(Mathf.Clamp01(p) * Mathf.PI);
                    petals[i].localRotation = Quaternion.Euler(0f, 0f, p * 200f);
                }
                yield return null;
            }
            if (main != null) { Destroy(main.gameObject); }
            foreach (var p in petals) { if (p != null) { Destroy(p.gameObject); } }
        }

        private RectTransform Spawn(int kind, Vector2 at, float size, float alpha)
        {
            var image = UIFactory.CreatePanel(_rect, "Bloom", new Color(1f, 1f, 1f, alpha));
            image.sprite = Resources.Load<Sprite>("Icons/Doodles/Doodle_" + kind);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = at;
            rect.localScale = Vector3.zero;
            return rect;
        }
    }
}
