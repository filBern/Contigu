using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Little plants growing around a button — two per button, each on a random edge (top favored), at a random spot and
    /// size, growing outward from that edge. Each button gets its own variety (poppy, bluebell, sunflower, fern, lavender,
    /// mushrooms, berries, clover, tulips, ivy), handed out in turn by <see cref="UIFactory"/> so neighbouring
    /// buttons differ. Sized from the button's own rect and skipped on buttons too small to carry decoration
    /// (icon buttons, count badges). Art: Resources/UI/Plants/V&lt;n&gt;A.png / V&lt;n&gt;B.png (Design/Buttons/generate_plants.py).
    /// </summary>
    public sealed class ButtonVines : MonoBehaviour
    {
        public const int VarietyCount = 10;
        private const float MinHeight = 34f;
        private const float MinWidth = 84f;

        /// <summary>Which plant variety this button wears (1..VarietyCount); set by UIFactory right after creation.</summary>
        public int Variety = 1;

        private RectTransform _rect;
        private RectTransform _tall;
        private RectTransform _small;

        // Each plant's random edge / spot along it / size, rolled once from the variety + a per-button seed so a
        // button keeps the same look when its layout changes, but no two buttons are dressed alike.
        private struct Spot
        {
            public int Edge; // 0 top, 1 right, 2 bottom, 3 left
            public float T;  // 0..1 along that edge
            public float Scale;
        }
        private Spot _tallSpot;
        private Spot _smallSpot;

        /// <summary>Per-button random seed (set by UIFactory next to <see cref="Variety"/>).</summary>
        public int Seed = 1;

        private void Start()
        {
            _rect = (RectTransform)transform;
            var tallSprite = Resources.Load<Sprite>("UI/Plants/V" + Variety + "A");
            var smallSprite = Resources.Load<Sprite>("UI/Plants/V" + Variety + "B");
            if (tallSprite == null || smallSprite == null)
            {
                enabled = false;
                return;
            }
            var rng = new System.Random(Seed * 7919 + Variety);
            _tallSpot = RollSpot(rng, 0.9f, 1.35f, -1, 0f);
            _smallSpot = RollSpot(rng, 0.6f, 1.0f, _tallSpot.Edge, _tallSpot.T);
            _tall = Make("PlantTall", tallSprite);
            _small = Make("PlantSmall", smallSprite);
            Layout();
        }

        /// <summary>Random edge (the top is favored, since upright plants read best there) and position, kept clear of the other plant when both land on the same edge.</summary>
        private static Spot RollSpot(System.Random rng, float minScale, float maxScale, int otherEdge, float otherT)
        {
            Spot spot;
            for (int attempt = 0; ; attempt++)
            {
                double roll = rng.NextDouble();
                spot.Edge = roll < 0.4 ? 0 : (roll < 0.62 ? 1 : (roll < 0.8 ? 3 : 2));
                spot.T = 0.12f + 0.76f * (float)rng.NextDouble();
                spot.Scale = minScale + (maxScale - minScale) * (float)rng.NextDouble();
                if (spot.Edge != otherEdge || Mathf.Abs(spot.T - otherT) > 0.3f || attempt > 8)
                {
                    return spot;
                }
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_tall != null)
            {
                Layout();
            }
        }

        private RectTransform Make(string name, Sprite sprite)
        {
            var img = UIFactory.CreatePanel(transform, name, Color.white);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img.rectTransform;
        }

        private void Layout()
        {
            var r = _rect.rect;
            bool show = r.height >= MinHeight && r.width >= MinWidth;
            _tall.gameObject.SetActive(show);
            _small.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            float baseSize = Mathf.Clamp(r.height * 0.85f, 26f, 40f);
            Stand(_tall, _tallSpot, baseSize);
            Stand(_small, _smallSpot, baseSize);
        }

        private static void Stand(RectTransform rt, Spot spot, float baseSize)
        {
            float t = spot.T;
            Vector2 anchor;
            float rotation;
            switch (spot.Edge)
            {
                case 1: anchor = new Vector2(1f, t); rotation = -90f; break;
                case 2: anchor = new Vector2(t, 0f); rotation = 180f; break;
                case 3: anchor = new Vector2(0f, t); rotation = 90f; break;
                default: anchor = new Vector2(t, 1f); rotation = 0f; break;
            }
            float size = baseSize * spot.Scale;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.06f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }
    }
}
