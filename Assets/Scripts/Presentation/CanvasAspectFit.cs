using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The layout is drawn for a 1280x800 (16:10) screen and the canvas scales to the screen's HEIGHT. On a squarer screen (an iPad is 4:3) that would push
    /// the sides of the layout off the edges, so below 16:10 the canvas scales to the WIDTH instead: everything stays on screen and the extra height is spare room.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasAspectFit : MonoBehaviour
    {
        private const float DesignAspect = 1280f / 800f;

        private CanvasScaler _scaler;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            float aspect = _lastHeight > 0 ? _lastWidth / (float)_lastHeight : DesignAspect;
            _scaler.matchWidthOrHeight = aspect >= DesignAspect ? 1f : 0f;
        }
    }
}
