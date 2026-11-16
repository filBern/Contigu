#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Contigu.EditorTools
{
    /// <summary>
    /// After a WebGL build, patches the generated index.html so the game fills an iPad's screen in Safari:
    /// iPadOS reports itself as a Mac ("desktop-class browsing"), which Unity's template takes for a desktop and then draws a small 960x600
    /// window; the page also gets the home-screen app tags and the CSS that stops page scrolling, zooming and text selection under the player's fingers; and it restyles the loading screen (notebook look).
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
            "      html, body { overscroll-behavior: none; touch-action: none; -webkit-user-select: none; user-select: none; -webkit-touch-callout: none; -webkit-tap-highlight-color: transparent; background: #ecdfc3; }\n" +
            "      canvas { touch-action: none; }\n" +
            LoadingCss +
            "    </style>\n";

        // Notebook-style loading screen (ruled paper, hand-drawn logo, Sprig flipping between three drawings, a crayon progress bar). The PNGs are
        // copied from Assets/Resources into TemplateData by CopyLoadingArt.
        private const string LoadingCss =
            "      @font-face { font-family: PH; src: url('TemplateData/loading-font.ttf'); }\n" +
            "      html, body { background: #ecdfc3 repeating-linear-gradient(to bottom, transparent 0, transparent 33px, rgba(143,154,166,0.5) 33px, rgba(143,154,166,0.5) 34px); }\n" +
            "      #unity-canvas { background: #ecdfc3; }\n" +
            "      #unity-loading-bar { text-align: center; font-family: PH, 'Comic Sans MS', cursive; color: #2a2420; }\n" +
            "      #unity-logo { width: 440px; max-width: 86vw; height: 122px; margin: 0 auto; background: url('TemplateData/loading-logo.png') no-repeat center / contain; }\n" +
            "      #unity-sprig { width: 110px; height: 110px; margin: 6px auto 0; background: url('TemplateData/loading-sprig-0.png') no-repeat center / contain; animation: sprig-flip 0.56s steps(1) infinite; }\n" +
            "      @keyframes sprig-flip { 0% { background-image: url('TemplateData/loading-sprig-0.png'); } 25% { background-image: url('TemplateData/loading-sprig-1.png'); } 50% { background-image: url('TemplateData/loading-sprig-2.png'); } 75% { background-image: url('TemplateData/loading-sprig-1.png'); } }\n" +
            "      #unity-progress-bar-empty { width: 320px; max-width: 70vw; height: 22px; margin: 8px auto 6px; padding: 0; border: 3px solid #2a2420; border-radius: 16px 14px 15px 13px; background: #fff8e6 none; overflow: hidden; }\n" +
            "      #unity-progress-bar-full { width: 0%; height: 100%; margin: 0; background: #7fcf6a none; border-right: 3px solid #2a2420; box-sizing: border-box; }\n" +
            "      #unity-loading-bar::after { content: 'Planting...'; font-size: 22px; }\n" +
            "      #unity-footer { display: none; }\n";

        private const string LoadingMarkup = "<div id=\"unity-logo\"></div>\n        <div id=\"unity-sprig\"></div>";

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
            html = html.Replace("<div id=\"unity-logo\"></div>", LoadingMarkup);
            CopyLoadingArt(pathToBuiltProject);
            File.WriteAllText(indexPath, html);
        }

        private static void CopyLoadingArt(string buildPath)
        {
            string dest = Path.Combine(buildPath, "TemplateData");
            Directory.CreateDirectory(dest);
            string res = Path.Combine("Assets", "Resources");
            Copy(Path.Combine(res, "Title", "LastBloom.png"), Path.Combine(dest, "loading-logo.png"));
            Copy(Path.Combine(res, "Characters", "Sprig_Happy.png"), Path.Combine(dest, "loading-sprig-0.png"));
            Copy(Path.Combine(res, "Characters", "Sprig_Happy_1.png"), Path.Combine(dest, "loading-sprig-1.png"));
            Copy(Path.Combine(res, "Characters", "Sprig_Happy_2.png"), Path.Combine(dest, "loading-sprig-2.png"));
            Copy(Path.Combine(res, "Font", "PatrickHand-Regular.ttf"), Path.Combine(dest, "loading-font.ttf"));
        }

        private static void Copy(string from, string to)
        {
            if (File.Exists(from))
            {
                File.Copy(from, to, true);
            }
        }
    }
}
#endif
