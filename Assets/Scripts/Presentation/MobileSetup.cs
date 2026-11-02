using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Touch-device settings applied once at startup (iPad, or a browser on a tablet): smooth frame rate, no screen dimming mid-run, one finger at a time.</summary>
    public static class MobileSetup
    {
        /// <summary>True on a device whose main input is the touch screen (a native tablet/phone build, or a browser reporting touch).</summary>
        public static bool IsTouchDevice
        {
            get { return Application.isMobilePlatform || Input.touchSupported; }
        }

        /// <summary>
        /// Whether a pointer entering an element may open a tooltip by itself. On a touch screen there is no hover: a tooltip may only open while a
        /// finger is actually down (a tap), never because a screen appeared under a pointer that was already resting there (the shop opening).
        /// </summary>
        public static bool PassiveHoverAllowed
        {
            get { return !IsTouchDevice || Input.touchCount > 0 || Input.GetMouseButton(0); }
        }

        /// <summary>
        /// Smallest comfortable touch target in canvas units: Apple asks for 44 pt, and the canvas shrinks to 0.8 on an iPad (4:3, see CanvasAspectFit),
        /// so 56 units stay above 44 points. Desktop keeps the size it was designed with.
        /// </summary>
        public const float MinTouchSize = 56f;

        public static float TouchMin(float size)
        {
            return IsTouchDevice ? Mathf.Max(size, MinTouchSize) : size;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            if (!IsTouchDevice)
            {
                return;
            }
            // iOS defaults to 30 fps: drag and drop and the score count look much better at 60.
            Application.targetFrameRate = 60;
            // A level can sit untouched while the player reads a tooltip: the screen must not dim and lock.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Two fingers on two seeds at once would start two drags.
            Input.multiTouchEnabled = false;
        }
    }
}
