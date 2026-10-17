using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Clear hover / press / click / "denied" feedback for the garden-styled buttons:
    /// hover = the button swells a little and a soft white glow fades over it; press = it sinks slightly;
    /// click = a springy punch; <see cref="Deny"/> (a click that can't be honored, e.g. not enough Pollen) = a red flash
    /// plus a short horizontal shake and the invalid-drop sound.
    /// Owns the button's localScale (hover x press x punch), so <see cref="ButtonPunchEffect"/> hands its punch to this
    /// component when present. Added by UIFactory to illustrated buttons and garden-bodied pills.
    /// </summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.07f;
        private const float PressScale = 0.95f;
        private const float HoverGlowAlpha = 0.24f;
        private const float ScaleSpeed = 14f;
        private const float GlowSpeed = 9f;

        /// <summary>Menu buttons already scale on hover (MainMenuButtonHover); they set this to false and keep just the glow.</summary>
        public bool ScaleOnHover = true;

        private Button _button;
        private Image _glow;
        private bool _hovering;
        private bool _pressing;
        private float _hoverFactor = 1f;
        private float _punchFactor = 1f;
        private float _glowAlpha;
        private Color _glowColor = Color.white;
        private Coroutine _punchRoutine;
        private Coroutine _denyRoutine;
        private RectTransform _rect;
        private Vector2 _restPosition;
        private bool _restKnown;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _button = GetComponent<Button>();
        }

        /// <summary>Builds the glow overlay on the button's visible image (its "Art" child when it has one). Call after the art/body is set.</summary>
        public void BuildGlow()
        {
            var host = transform.Find("Art");
            var source = host != null ? host.GetComponent<Image>() : GetComponent<Image>();
            if (source == null || source.sprite == null)
            {
                return;
            }
            var go = UIFactory.CreatePanel(source.transform, "Glow", new Color(1f, 1f, 1f, 0f));
            go.sprite = source.sprite;
            go.type = source.type;
            go.preserveAspect = source.preserveAspect;
            go.raycastTarget = false;
            UIFactory.StretchFull(go.rectTransform);
            _glow = go;
        }

        private bool Active
        {
            get { return _button == null || _button.interactable; }
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovering = true; }
        public void OnPointerExit(PointerEventData eventData) { _hovering = false; _pressing = false; }
        public void OnPointerDown(PointerEventData eventData) { _pressing = true; }
        public void OnPointerUp(PointerEventData eventData) { _pressing = false; }

        private void OnDisable()
        {
            _hovering = false;
            _pressing = false;
            _hoverFactor = 1f;
            _punchFactor = 1f;
            if (_rect != null)
            {
                _rect.localScale = Vector3.one;
            }
            if (_glow != null)
            {
                _glow.color = new Color(1f, 1f, 1f, 0f);
            }
            _glowAlpha = 0f;
        }

        private void Update()
        {
            bool hot = _hovering && Active;
            float targetScale = hot && ScaleOnHover ? HoverScale : 1f;
            if (_pressing && Active)
            {
                targetScale = PressScale;
            }
            _hoverFactor = Mathf.Lerp(_hoverFactor, targetScale, 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime));
            float s = _hoverFactor * _punchFactor;
            _rect.localScale = new Vector3(s, s, 1f);

            if (_glow != null && _denyRoutine == null)
            {
                float targetGlow = hot ? HoverGlowAlpha : 0f;
                _glowAlpha = Mathf.Lerp(_glowAlpha, targetGlow, 1f - Mathf.Exp(-GlowSpeed * Time.unscaledDeltaTime));
                _glow.color = new Color(1f, 1f, 1f, _glowAlpha);
            }
        }

        /// <summary>Springy squish-and-return on click (called by ButtonPunchEffect).</summary>
        public void Punch()
        {
            if (_punchRoutine != null)
            {
                StopCoroutine(_punchRoutine);
            }
            _punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            const float duration = 0.2f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                _punchFactor = p < 0.4f ? Mathf.Lerp(1f, 0.88f, p / 0.4f) : Mathf.Lerp(0.88f, 1f, (p - 0.4f) / 0.6f) + Mathf.Sin((p - 0.4f) / 0.6f * Mathf.PI) * 0.06f;
                yield return null;
            }
            _punchFactor = 1f;
            _punchRoutine = null;
        }

        /// <summary>A click that can't be honored: red flash, a short shake, and the invalid sound.</summary>
        public void Deny()
        {
            SfxManager.Play(SfxId.InvalidDrop);
            if (_denyRoutine != null)
            {
                StopCoroutine(_denyRoutine);
                if (_restKnown)
                {
                    _rect.anchoredPosition = _restPosition;
                }
            }
            _denyRoutine = StartCoroutine(DenyRoutine());
        }

        private IEnumerator DenyRoutine()
        {
            _restPosition = _rect.anchoredPosition;
            _restKnown = true;
            const float duration = 0.38f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float decay = 1f - p;
                _rect.anchoredPosition = _restPosition + new Vector2(Mathf.Sin(p * Mathf.PI * 7f) * 9f * decay, 0f);
                if (_glow != null)
                {
                    _glow.color = new Color(1f, 0.25f, 0.2f, 0.65f * decay);
                }
                yield return null;
            }
            _rect.anchoredPosition = _restPosition;
            if (_glow != null)
            {
                _glow.color = new Color(1f, 1f, 1f, 0f);
            }
            _glowAlpha = 0f;
            _denyRoutine = null;
        }
    }
}
