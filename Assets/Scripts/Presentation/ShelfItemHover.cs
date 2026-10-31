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
            float target = _hovered && !MotionSettings.Reduced ? HoverScale : 1f;
            float current = _art.localScale.x;
            if (Mathf.Abs(current - target) > 0.001f)
            {
                float next = Mathf.MoveTowards(current, target, Speed * Time.unscaledDeltaTime * 0.5f);
                _art.localScale = new Vector3(next, next, 1f);
            }
        }
    }
}
