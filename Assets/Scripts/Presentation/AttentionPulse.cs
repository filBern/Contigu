using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>A slow, gentle swell that says "this is the one to press" (still when the reduced-motion setting is on).</summary>
    public sealed class AttentionPulse : MonoBehaviour
    {
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            if (_rect != null)
            {
                _rect.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (MotionSettings.Reduced)
            {
                _rect.localScale = Vector3.one;
                return;
            }
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            _rect.localScale = Vector3.one * (1f + 0.035f * wave);
        }
    }
}
