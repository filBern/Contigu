using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>A ripe Fruit's sack bobs gently and, every few seconds, lets a small fruit and a few grains of Pollen rise from it (still when the reduced-motion setting is on).</summary>
    public sealed class RipeFruitLoop : MonoBehaviour
    {
        /// <summary>Called with the sack's world position each time a puff is due.</summary>
        public System.Action<Vector3> Puff;

        private RectTransform _rect;
        private float _phase;
        private float _nextPuff;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _nextPuff = Random.Range(0.4f, 1.6f);
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
                return;
            }
            float t = Time.unscaledTime;
            // A slow breath with a little squash, like a fruit about to drop.
            float wave = Mathf.Sin(t * 3.2f + _phase);
            _rect.localScale = new Vector3(1f + 0.035f * wave, 1f - 0.035f * wave, 1f);
            _nextPuff -= Time.unscaledDeltaTime;
            if (_nextPuff <= 0f)
            {
                _nextPuff = Random.Range(2.2f, 3.2f);
                if (Puff != null)
                {
                    Puff(_rect.position);
                }
            }
        }
    }
}
