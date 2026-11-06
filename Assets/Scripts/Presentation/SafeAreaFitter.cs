using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Keeps a full-screen RectTransform inside the device's safe area (notches, rounded corners, system bars); a no-op on a plain desktop window.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void Update()
        {
            var safe = Screen.safeArea;
            if (safe == _applied && _screen.x == Screen.width && _screen.y == Screen.height)
            {
                return;
            }
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
