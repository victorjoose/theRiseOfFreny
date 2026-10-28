using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Batch-mode WebGL build for the web site (github.com/victorjoose/freny-online, on Vercel):
// Unity.exe -batchmode -quit -projectPath <proj> -buildTarget WebGL -executeMethod WebGLBuild.Build -webglOut <dir>
// The site expects Gzip files named after the output folder (Build/WebGL.*), so use a folder called "WebGL".
public static class WebGLBuild {

    // In WebGL the "Hinted Raster" font mode draws uGUI text as giant blocky glyphs,
    // so the menu font is switched to Smooth for the build and restored afterwards.
    private const string MenuFont = "Assets/Sprites/Interface/Resources/Fonts/PressStart2P.ttf";

    public static void Build() {
        string[] args = System.Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-webglOut");
        string outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Builds/WebGL";

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        TrueTypeFontImporter font = (TrueTypeFontImporter)AssetImporter.GetAtPath(MenuFont);
        FontRenderingMode originalMode = font.fontRenderingMode;
        SetFontMode(font, FontRenderingMode.Smooth);

        BuildReport report;
        try {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
        } finally {
            SetFontMode(font, originalMode);
        }

        Debug.Log($"[WebGLBuild] {report.summary.result}, {report.summary.totalSize} bytes, {report.summary.totalErrors} errors");
        if (report.summary.result != BuildResult.Succeeded) {
            EditorApplication.Exit(1);
        }
    }

    private static void SetFontMode(TrueTypeFontImporter font, FontRenderingMode mode) {
        if (font.fontRenderingMode != mode) {
            font.fontRenderingMode = mode;
            font.SaveAndReimport();
        }
    }
}
