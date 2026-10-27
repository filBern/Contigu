using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Zooms the whole UI while the shop is open, on screens wider than the layout was drawn for. The shop has room to spare vertically
    /// (unlike the board, which is limited by its height), so everything in it (shelves, signs, side panels, the screens opened from it) is
    /// drawn bigger, as far as the side panels still fit in the width. Added to the shop's overlay: it zooms when the shop appears and goes
    /// back to normal when it is hidden or destroyed. The canvas itself rescales, so text and art stay sharp.
    /// </summary>
    public sealed class ShopZoom : MonoBehaviour
    {
        private const float MaxZoom = 1.3f;
        private const float EdgeMargin = 8f;

        private CanvasScaler _scaler;
        private Vector2 _baseReference;
        private bool _zoomed;

        /// <summary>Distance from the screen's middle to the outer edge of the side panels, in canvas units; set by ShopView once the layout is known.</summary>
        public float HalfContentWidth { get; set; }

        private void OnEnable()
        {
            _scaler = GetComponentInParent<CanvasScaler>();
            var rect = transform as RectTransform;
            if (_scaler == null || rect == null || HalfContentWidth <= 0f || _zoomed)
            {
                return;
            }
            float halfWidth = rect.rect.width * 0.5f;
            float zoom = Mathf.Clamp(halfWidth / (HalfContentWidth + EdgeMargin), 1f, MaxZoom);
            if (zoom <= 1.01f)
            {
                return;
            }
            _baseReference = _scaler.referenceResolution;
            _scaler.referenceResolution = _baseReference / zoom;
            _zoomed = true;
        }

        private void OnDisable()
        {
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }

        private void Restore()
        {
            if (_zoomed && _scaler != null)
            {
                _scaler.referenceResolution = _baseReference;
            }
            _zoomed = false;
        }
    }
}
