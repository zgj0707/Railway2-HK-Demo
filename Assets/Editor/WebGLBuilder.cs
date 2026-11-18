using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuilder
{
    static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

    public static void BuildWebGL()
    {
        string outDir = Path.GetFullPath("Build/WebGL");
        Directory.CreateDirectory(outDir);
        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
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

    public static void BuildWindows()
    {
        string outDir = Path.GetFullPath("Build/Windows");
        Directory.CreateDirectory(outDir);
        string exePath = Path.Combine(outDir, "Railway2.exe");
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 720;
        PlayerSettings.defaultScreenHeight = 1280;
        PlayerSettings.resizableWindow = true;
        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Windows build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
        }
    }

    public static void BuildWebGLAndWindows()
    {
        BuildWebGL();
        BuildWindows();
    }
}
