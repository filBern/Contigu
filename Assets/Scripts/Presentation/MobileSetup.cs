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
