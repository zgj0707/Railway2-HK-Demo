using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuilder
{
    public static void BuildWebGL()
    {
        string outDir = Path.GetFullPath("Build/WebGL");
        Directory.CreateDirectory(outDir);
        var scenes = new[] { "Assets/Scenes/Main.unity" };
        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("WebGL build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
        }
    }
}
