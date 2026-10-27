#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Contigu.Presentation
{
    /// <summary>On WebGL, copies the files written under persistentDataPath into the browser's IndexedDB (otherwise they are lost when the page closes or reloads). A no-op on every other platform.</summary>
    public static class WebFileSync
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void WebFileSync_Flush();

        public static void Flush()
        {
            WebFileSync_Flush();
        }
#else
        public static void Flush()
        {
        }
#endif
    }
}
