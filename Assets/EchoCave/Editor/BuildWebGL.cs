using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoCave.Editor
{
    // One menu item that builds the Game scene for the web the same way every time, and zips the result for
    // itch.io (which wants index.html at the root of the zip). The scene list of the Build Profiles window is
    // not used, so the build never depends on someone remembering to tick the right scene.
    public static class BuildWebGL
    {
        private const string GameScene = "Assets/EchoCave/Scenes/Game.unity";
        private const string Template = "PROJECT:EchoCave";
        private const string OutputFolder = "Builds/WebGL";
        private const string ZipPath = "Builds/EchoCave-WebGL.zip";

        [MenuItem("EchoCave/Build/WebGL (zip for itch.io)")]
        public static void Build()
        {
            if (!File.Exists(GameScene))
            {
                Debug.LogError($"[BuildWebGL] The Game scene is missing: {GameScene}");
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[BuildWebGL] WebGL Build Support is not installed for this editor. Add it from Unity Hub (Installs > Add modules).");
                return;
            }

            ApplySettings();

            // Only our own output folder: a stale file from an older build must not end up in the zip.
            if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { GameScene },
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildWebGL] Build {report.summary.result}: {report.summary.totalErrors} error(s). See the console above.");
                return;
            }

            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            ZipFile.CreateFromDirectory(OutputFolder, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);

            Debug.Log($"[BuildWebGL] Built {Megabytes(report.summary.totalSize)} MB in {report.summary.totalTime:mm\\:ss}. " +
                      $"Zip for itch.io: {ZipPath} ({Megabytes(new FileInfo(ZipPath).Length)} MB).");
            EditorUtility.RevealInFinder(ZipPath);
        }

        // WebGL 2 only (D-025). Gzip with the decompression fallback works on a host that does not send
        // Content-Encoding headers; the fallback only costs a slightly larger loader.
        private static void ApplySettings()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = Template;
        }

        private static string Megabytes(ulong bytes) => (bytes / 1048576.0).ToString("0.0");

        private static string Megabytes(long bytes) => Megabytes((ulong)bytes);
    }
}
