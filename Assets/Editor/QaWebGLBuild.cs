#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Contigu.EditorTools
{
    /// <summary>
    /// Headless WebGL build for the QA bench (see docs/QA.md, .github/workflows/webgl-qa-build.yml): same settings as the build made by hand in the editor
    /// (the scenes of Build Settings, Development Build; the project's own WebGL define CONTIGU_DEBUG and uncompressed output come from Player Settings).
    /// Called with <c>-executeMethod Contigu.EditorTools.QaWebGLBuild.Build</c>; the output folder comes from <c>-customBuildPath</c> (game-ci) or defaults to Builds/QaBuild.
    /// </summary>
    public static class QaWebGLBuild
    {
        public static void Build()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) { scenes.Add(scene.path); }
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = ArgumentValue("-customBuildPath") ?? "Builds/QaBuild",
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Console.WriteLine("QaWebGLBuild: " + report.summary.result + " (" + report.summary.totalErrors + " errors) -> " + options.locationPathName);
            if (!ok)
            {
                EditorApplication.Exit(1);
            }
        }

        private static string ArgumentValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) { return args[i + 1]; }
            }
            return null;
        }
    }
}
#endif
