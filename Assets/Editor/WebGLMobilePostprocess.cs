#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Contigu.EditorTools
{
    /// <summary>
    /// After a WebGL build, patches the generated index.html so the game fills an iPad's screen in Safari:
    /// iPadOS reports itself as a Mac ("desktop-class browsing"), which Unity's template takes for a desktop and then draws a small 960x600
    /// window; the page also gets the home-screen app tags and the CSS that stops page scrolling, zooming and text selection under the player's fingers.
    /// </summary>
    public static class WebGLMobilePostprocess
    {
        private const string OriginalCheck = "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {";
        private const string PatchedCheck = "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) {";

        private const string HeadAdditions =
            "    <meta name=\"apple-mobile-web-app-capable\" content=\"yes\">\n" +
            "    <meta name=\"mobile-web-app-capable\" content=\"yes\">\n" +
            "    <meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">\n" +
            "    <meta name=\"apple-mobile-web-app-title\" content=\"LASTBLOOM\">\n" +
            "    <style>\n" +
            "      html, body { overscroll-behavior: none; touch-action: none; -webkit-user-select: none; user-select: none; -webkit-touch-callout: none; -webkit-tap-highlight-color: transparent; background: #10202c; }\n" +
            "      canvas { touch-action: none; }\n" +
            "    </style>\n";

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.WebGL)
            {
                return;
            }
            string indexPath = Path.Combine(pathToBuiltProject, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }
            string html = File.ReadAllText(indexPath);
            if (html.Contains("apple-mobile-web-app-capable"))
            {
                return;
            }
            html = html.Replace(OriginalCheck, PatchedCheck);
            html = html.Replace("</head>", HeadAdditions + "  </head>");
            File.WriteAllText(indexPath, html);
        }
    }
}
#endif
