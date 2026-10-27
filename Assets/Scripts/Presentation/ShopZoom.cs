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

        private CanvasScaler _scaler;
        private Vector2 _baseReference;
        private bool _zoomed;

        // Half the width the shop's own content needs, in canvas units: a shelf (600 wide), a gap, a side panel and an edge margin.
        private const float ShelfHalfWidth = 300f;
        private const float ShelfToPanelGap = 24f;
        private const float PanelToEdgeMargin = 40f;

        /// <summary>Moves the side panels (and the Pollen counter above one) to a centre x, in the zoomed canvas' units; set by ShopView.</summary>
        public System.Action<float> PlacePanels { get; set; }

        /// <summary>Puts the side panels back where the board screen has them.</summary>
        public System.Action RestorePanels { get; set; }

        private void OnEnable()
        {
            _scaler = GetComponentInParent<CanvasScaler>();
            var rect = transform as RectTransform;
            if (_scaler == null || rect == null || _zoomed)
            {
                return;
            }
            float halfWidth = rect.rect.width * 0.5f;
            float panelWidth = ModifierPanelView.PanelWidth;
            float zoom = Mathf.Clamp(halfWidth / (ShelfHalfWidth + ShelfToPanelGap + panelWidth + PanelToEdgeMargin), 1f, MaxZoom);
            if (zoom <= 1.01f)
            {
                return;
            }
            _baseReference = _scaler.referenceResolution;
            _scaler.referenceResolution = _baseReference / zoom;
            _zoomed = true;
            if (PlacePanels != null)
            {
                // The side panels keep their on-screen place (about where the board screen has them): near the edges, outside the shelves.
                PlacePanels(halfWidth / zoom - panelWidth * 0.5f - PanelToEdgeMargin);
            }
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
                if (RestorePanels != null)
                {
                    RestorePanels();
                }
            }
            _zoomed = false;
        }
    }
}
