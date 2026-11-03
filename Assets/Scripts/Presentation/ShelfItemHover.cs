using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>
    /// What makes a bare shelf item feel interactive without any button chrome: a swell under the pointer (skipped with Reduced motion),
    /// a golden halo plus the tooltip while it has keyboard/controller focus, and a long press (touch) that shows the tooltip instead of buying.
    /// </summary>
    public sealed class ShelfItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.08f;
        private const float Speed = 14f;
        private const float LongPressSeconds = 0.45f;

        private RectTransform _art;
        private GameObject _halo;
        private bool _hovered;
        private bool _pressing;
        private float _pressStart;
        private bool _longPressShown;
        private bool _longPressPending;

        // Idle life: the item bobs and sways a little on its shelf, and now and then a glint pops on it. Each item has its own rhythm.
        private const float BobAmplitude = 2.4f;
        private const float SwayDegrees = 1.2f;
        private const float GlintLife = 0.9f;
        private float _idlePhase = -1f;
        private float _nextGlintAt;
        private float _glintStart = -10f;
        private RectTransform _glint;

        /// <summary>True if the latest press was a finger (not a mouse button).</summary>
        public bool LastPressWasTouch { get; private set; }

        /// <summary>The golden halo marks the item a touch has selected (tap again to buy).</summary>
        public void SetArmed(bool armed)
        {
            if (_halo != null)
            {
                _halo.SetActive(armed);
            }
        }

        public void Init(RectTransform art, GameObject halo)
        {
            _art = art;
            _halo = halo;
        }

        /// <summary>True once (and resets) if the press that just ended was a long press, so the click that follows must not buy.</summary>
        public bool ConsumeLongPress()
        {
            bool was = _longPressPending;
            _longPressPending = false;
            return was;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = eventData.pointerId < 0; // touch has no hover
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_halo != null)
            {
                _halo.SetActive(true);
            }
            // Only navigation (arrows / d-pad) opens the tooltip; a mouse click already shows it through hover.
            if (eventData is AxisEventData)
            {
                var tip = GetComponent<HoverTooltip>();
                if (tip != null)
                {
                    tip.ShowNow();
                }
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_halo != null)
            {
                _halo.SetActive(false);
            }
            var tip = GetComponent<HoverTooltip>();
            if (tip != null)
            {
                tip.HideNow();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Long press is a touch gesture only: a mouse button held down still buys on release.
            _pressing = eventData.pointerId >= 0;
            LastPressWasTouch = _pressing;
            _longPressShown = false;
            _longPressPending = false;
            _pressStart = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressing = false;
            if (_longPressShown)
            {
                _longPressPending = true;
                _longPressShown = false;
                var tip = GetComponent<HoverTooltip>();
                if (tip != null)
                {
                    tip.HideNow();
                }
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressing = false;
            if (_art != null)
            {
                _art.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (_pressing && !_longPressShown && Time.unscaledTime - _pressStart >= LongPressSeconds)
            {
                _longPressShown = true;
                var tip = GetComponent<HoverTooltip>();
                if (tip != null)
                {
                    tip.ShowNow();
                }
            }
            if (_art == null)
            {
                return;
            }
            UpdateIdleLife();
            float target = _hovered && !MotionSettings.Reduced ? HoverScale : 1f;
            float current = _art.localScale.x;
            if (Mathf.Abs(current - target) > 0.001f)
            {
                float next = Mathf.MoveTowards(current, target, Speed * Time.unscaledDeltaTime * 0.5f);
                _art.localScale = new Vector3(next, next, 1f);
            }
        }

        private void UpdateIdleLife()
        {
            bool still = MotionSettings.Reduced || _hovered || _pressing || (_halo != null && _halo.activeSelf);
            // A greyed-out item (not buyable) stays put.
            var group = _art.GetComponent<CanvasGroup>();
            if (group != null && group.alpha < 0.9f)
            {
                still = true;
            }
            if (_idlePhase < 0f)
            {
                _idlePhase = Random.value * 6.2832f;
                _nextGlintAt = Time.unscaledTime + 2f + Random.value * 6f;
            }
            float now = Time.unscaledTime;
            if (still)
            {
                _art.anchoredPosition = Vector2.Lerp(_art.anchoredPosition, Vector2.zero, 0.3f);
                _art.localRotation = Quaternion.Slerp(_art.localRotation, Quaternion.identity, 0.3f);
            }
            else
            {
                _art.anchoredPosition = new Vector2(0f, Mathf.Sin(now * 1.6f + _idlePhase) * BobAmplitude);
                _art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 1.1f + _idlePhase * 1.7f) * SwayDegrees);
            }
            if (still)
            {
                if (_glint != null) { _glint.gameObject.SetActive(false); }
                return;
            }
            if (now >= _nextGlintAt)
            {
                StartGlint(now);
                _nextGlintAt = now + 5f + Random.value * 6f;
            }
            AnimateGlint(now);
        }

        private void StartGlint(float now)
        {
            if (_glint == null)
            {
                _glint = UIFactory.CreateUIObject("IdleGlint", transform);
                _glint.sizeDelta = new Vector2(26f, 26f);
                for (int i = 0; i < 2; i++)
                {
                    var bar = UIFactory.CreatePanel(_glint, "Bar" + i, new Color(1f, 0.97f, 0.82f, 0.95f));
                    bar.raycastTarget = false;
                    bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    bar.rectTransform.sizeDelta = i == 0 ? new Vector2(26f, 4f) : new Vector2(4f, 26f);
                }
            }
            var rect = (RectTransform)transform;
            _glint.anchorMin = _glint.anchorMax = new Vector2(0.5f, 0.5f);
            _glint.anchoredPosition = new Vector2((Random.value - 0.5f) * rect.rect.width * 0.6f, (Random.value * 0.4f - 0.05f) * rect.rect.height);
            _glint.SetAsLastSibling();
            _glintStart = now;
        }

        private void AnimateGlint(float now)
        {
            if (_glint == null)
            {
                return;
            }
            float p = (now - _glintStart) / GlintLife;
            if (p < 0f || p > 1f)
            {
                _glint.gameObject.SetActive(false);
                return;
            }
            _glint.gameObject.SetActive(true);
            float scale = Mathf.Sin(p * Mathf.PI);
            _glint.localScale = new Vector3(scale, scale, 1f);
            _glint.localRotation = Quaternion.Euler(0f, 0f, p * 70f);
        }

        private void OnEnable()
        {
            _idlePhase = -1f;
        }
    }
}
