using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Gentle idle float for Sprig's portrait: a slow vertical bob with a slight sway and squash, on unscaled time so it also runs while the game is paused/overlaid.</summary>
    public sealed class SprigFloat : MonoBehaviour
    {
        private const float BobHeight = 7f;
        private const float BobSpeed = 2.2f;
        private const float SwayDegrees = 2.5f;
        private const float SquashAmount = 0.02f;

        private RectTransform _rect;
        private Vector2 _basePos;
        private float _phase;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void OnEnable()
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }
            _basePos = _rect.anchoredPosition;
        }

        private void LateUpdate()
        {
            float t = Time.unscaledTime * BobSpeed + _phase;
            float s = Mathf.Sin(t);
            _rect.anchoredPosition = _basePos + new Vector2(0f, s * BobHeight);
            _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * SwayDegrees);
            float squash = Mathf.Cos(t) * SquashAmount;
            _rect.localScale = new Vector3(1f - squash, 1f + squash, 1f);
        }

        private void OnDisable()
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = _basePos;
                _rect.localRotation = Quaternion.identity;
                _rect.localScale = Vector3.one;
            }
        }
    }
}
