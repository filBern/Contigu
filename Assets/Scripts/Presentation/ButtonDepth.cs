using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Faux-3D button: a dark copy of the button offset downward is its thickness (plus a softer shadow under it); pressing sinks the face onto
    /// that base (the whole button moves down while the thickness shrinks), and lifting lets it spring back.
    /// </summary>
    public sealed class ButtonDepth : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Thickness = 4f;
        private const float PressedThickness = 1.5f;

        private Shadow _base;
        private Shadow _soft;
        private Button _button;
        private RectTransform _rect;
        private bool _pressed;
        private Vector2 _restPosition;

        public static void Attach(Image face)
        {
            var depth = face.gameObject.AddComponent<ButtonDepth>();
            // The extrusion: a dark, near-opaque copy straight below; then a wide soft shadow beneath it.
            depth._soft = face.gameObject.AddComponent<Shadow>();
            depth._soft.effectColor = new Color(0f, 0f, 0f, 0.28f);
            depth._soft.effectDistance = new Vector2(1f, -Thickness - 5f);
            depth._base = face.gameObject.AddComponent<Shadow>();
            depth._base.effectColor = new Color(0.04f, 0.025f, 0.02f, 0.78f);
            depth._base.effectDistance = new Vector2(0f, -Thickness);
            depth._button = face.GetComponent<Button>();
            depth._rect = face.rectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pressed || _base == null || (_button != null && !_button.interactable))
            {
                return;
            }
            _pressed = true;
            _restPosition = _rect.anchoredPosition;
            _rect.anchoredPosition = _restPosition + new Vector2(0f, -(Thickness - PressedThickness));
            SetThickness(PressedThickness);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!_pressed)
            {
                return;
            }
            _pressed = false;
            _rect.anchoredPosition = _restPosition;
            SetThickness(Thickness);
        }

        private void SetThickness(float thickness)
        {
            _base.effectDistance = new Vector2(0f, -thickness);
            _soft.effectDistance = new Vector2(1f, -thickness - 5f);
        }
    }
}
