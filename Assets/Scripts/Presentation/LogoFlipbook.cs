using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Old-cartoon "boiling line" flipbook for the title logo: cycles quickly through a few slightly different drawings of the same logo
    /// (Resources/Title/LastBloom, LastBloom_1, LastBloom_2). Stays on the first drawing when "reduced motion" is on or a frame is missing.
    /// </summary>
    public sealed class LogoFlipbook : MonoBehaviour
    {
        private const float FrameSeconds = 0.14f;
        private static readonly int[] Order = { 0, 1, 2, 1 };

        private Image _image;
        private Sprite[] _frames;
        private float _timer;
        private int _step;

        public static void Attach(Image image, Sprite first)
        {
            var frames = new[] { first, Resources.Load<Sprite>("Title/LastBloom_1"), Resources.Load<Sprite>("Title/LastBloom_2") };
            foreach (var frame in frames)
            {
                if (frame == null)
                {
                    return;
                }
            }
            var flipbook = image.gameObject.AddComponent<LogoFlipbook>();
            flipbook._image = image;
            flipbook._frames = frames;
        }

        private void Update()
        {
            if (_image == null || MotionSettings.Reduced)
            {
                return;
            }
            _timer += Time.unscaledDeltaTime;
            if (_timer < FrameSeconds)
            {
                return;
            }
            _timer -= FrameSeconds;
            _step = (_step + 1) % Order.Length;
            _image.sprite = _frames[Order[_step]];
        }
    }
}
