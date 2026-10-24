using UnityEngine;
using UnityEngine.EventSystems;

namespace Contigu.Presentation
{
    /// <summary>A shop item sitting on a shelf swells slightly under the pointer, so it reads as clickable without any button chrome.</summary>
    public sealed class ShelfItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.08f;
        private const float Speed = 14f;

        private RectTransform _art;
        private bool _hovered;

        public void Init(RectTransform art)
        {
            _art = art;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void OnDisable()
        {
            _hovered = false;
            if (_art != null)
            {
                _art.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (_art == null)
            {
                return;
            }
            float target = _hovered ? HoverScale : 1f;
            float current = _art.localScale.x;
            if (Mathf.Abs(current - target) > 0.001f)
            {
                float next = Mathf.MoveTowards(current, target, Speed * Time.unscaledDeltaTime * 0.5f);
                _art.localScale = new Vector3(next, next, 1f);
            }
        }
    }
}
