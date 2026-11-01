using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Makes a number label count up or down to its new value instead of jumping, and bounce when it grows (more for a big gain, and a
    /// bigger one still each time it crosses a multiple of ten). Put on the label by <see cref="Set"/>; the label is only ever scaled, never moved.
    /// </summary>
    public sealed class NumberRoll : MonoBehaviour
    {
        private const float RollSeconds = 0.45f;
        private const float BounceDecay = 3.2f;

        private Text _text;
        private int _shown;
        private int _target;
        private int _from;
        private float _rollT = 1f;
        private float _bounce;
        private bool _initialized;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>Shows <paramref name="value"/> on <paramref name="label"/>, rolling toward it. The first call (and reduced motion) just sets it.</summary>
        public static void Set(Text label, int value)
        {
            var roll = label.GetComponent<NumberRoll>();
            if (roll == null)
            {
                roll = label.gameObject.AddComponent<NumberRoll>();
            }
            roll.SetTarget(label, value);
        }

        private void SetTarget(Text label, int value)
        {
            _text = label;
            if (!_initialized || MotionSettings.Reduced || !isActiveAndEnabled)
            {
                _initialized = true;
                _shown = _target = value;
                _rollT = 1f;
                _text.text = value.ToString();
                return;
            }
            if (value == _target)
            {
                return;
            }
            int delta = value - _target;
            if (delta > 0)
            {
                float amount = Mathf.Clamp(0.12f + delta * 0.03f, 0.12f, 0.45f);
                if (value / 10 > _target / 10) { amount += 0.25f; }
                _bounce = Mathf.Max(_bounce, amount);
            }
            _from = _shown;
            _target = value;
            _rollT = 0f;
        }

        private void Update()
        {
            if (_text == null)
            {
                return;
            }
            if (_rollT < 1f)
            {
                _rollT = Mathf.Min(1f, _rollT + GameSpeed.AnimationDelta / RollSeconds);
                float eased = 1f - Mathf.Pow(1f - _rollT, 3f);
                int now = Mathf.RoundToInt(Mathf.Lerp(_from, _target, eased));
                if (now != _shown)
                {
                    _shown = now;
                    _text.text = now.ToString();
                }
            }
            if (_bounce > 0f)
            {
                _bounce = Mathf.Max(0f, _bounce - BounceDecay * Time.unscaledDeltaTime * Mathf.Max(0.5f, _bounce * 2f));
            }
            transform.localScale = _baseScale * (1f + _bounce);
        }

        private void OnDisable()
        {
            if (_text != null)
            {
                _text.text = _target.ToString();
                _shown = _target;
                _rollT = 1f;
                _bounce = 0f;
                transform.localScale = _baseScale;
            }
        }
    }
}
