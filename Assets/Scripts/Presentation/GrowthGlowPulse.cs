using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>A Fruit's golden halo breathes slowly (still when the reduced-motion setting is on).</summary>
    public sealed class GrowthGlowPulse : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rect;
        private Vector3 _baseScale = Vector3.one;
        private float _baseAlpha = 0.8f;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
            _baseScale = _rect.localScale;
            _baseAlpha = _image.color.a;
        }

        private void Update()
        {
            if (MotionSettings.Reduced)
            {
                return;
            }
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
            _rect.localScale = _baseScale * (0.94f + 0.12f * wave);
            var color = _image.color;
            color.a = _baseAlpha * (0.7f + 0.3f * wave);
            _image.color = color;
        }
    }
}
