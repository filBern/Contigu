using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Old-cartoon "boiling line" flipbook for an enemy portrait: whatever sprite the owner puts on the Image (Resources/Enemies/&lt;Name&gt;)
    /// is cycled with its sibling drawings &lt;Name&gt;_1 and &lt;Name&gt;_2, 0-1-2-1 every 0.14 s (each portrait with a slightly different phase).
    /// A sprite without siblings, or "reduced motion", stays still. The owner can keep assigning the base sprite as before.
    /// </summary>
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        private const float FrameSeconds = 0.14f;
        private static readonly int[] Order = { 0, 1, 2, 1 };
        private static readonly Dictionary<Sprite, Sprite[]> FrameCache = new Dictionary<Sprite, Sprite[]>();

        private Image _image;
        private Sprite _base;
        private Sprite _lastSet;
        private Sprite[] _frames;
        private float _phase;

        public static void Attach(Image image)
        {
            if (image != null && image.GetComponent<SpriteFlipbook>() == null)
            {
                image.gameObject.AddComponent<SpriteFlipbook>()._image = image;
            }
        }

        private void Awake()
        {
            _phase = Random.value * 4f;
        }

        private static Sprite[] FramesFor(Sprite baseSprite)
        {
            if (baseSprite == null)
            {
                return null;
            }
            if (!FrameCache.TryGetValue(baseSprite, out var frames))
            {
                var one = Resources.Load<Sprite>("Enemies/" + baseSprite.name + "_1");
                var two = Resources.Load<Sprite>("Enemies/" + baseSprite.name + "_2");
                frames = one != null && two != null ? new[] { baseSprite, one, two } : null;
                FrameCache[baseSprite] = frames;
            }
            return frames;
        }

        private void Update()
        {
            if (_image == null)
            {
                return;
            }
            // A sprite the flipbook did not set itself is a new base sprite (the owner changed the portrait).
            if (_image.sprite != _lastSet)
            {
                _base = _image.sprite;
                _frames = FramesFor(_base);
                _lastSet = _base;
            }
            if (_frames == null)
            {
                return;
            }
            int step = MotionSettings.Reduced ? 0 : Order[(int)((Time.unscaledTime + _phase) / FrameSeconds) % Order.Length];
            var wanted = _frames[step];
            if (_image.sprite != wanted)
            {
                _image.sprite = wanted;
                _lastSet = wanted;
            }
        }
    }
}
