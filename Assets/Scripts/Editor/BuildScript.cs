using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PlanetScrappers.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlanetScrappers.EditorTools
{
    /// <summary>
    /// Produces a versioned Windows QA package the client can run without Unity:
    ///   Builds/PlanetScrappers_v{version}_QA/PlanetScrappers.exe (+ data folder, CHANGELOG.txt)
    ///   Builds/PlanetScrappers_v{version}_QA.zip
    /// Batch mode: Unity.exe -batchmode -quit -projectPath . -executeMethod PlanetScrappers.EditorTools.BuildScript.BuildWindowsQA
    /// </summary>
    public static class BuildScript
    {
        const string ExeName = "PlanetScrappers.exe";

        [MenuItem("Planet Scrappers/Build Windows QA Zip", priority = 20)]
        public static void BuildWindowsQAFromMenu()
        {
            string zip = BuildWindowsQAInternal();
            if (zip != null) EditorUtility.RevealInFinder(zip);
        }

        public static void BuildWindowsQA()
        {
            string zip = BuildWindowsQAInternal();
            if (Application.isBatchMode) EditorApplication.Exit(zip != null ? 0 : 1);
        }

        static string BuildWindowsQAInternal()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] No scenes in Build Settings. Run Planet Scrappers > Build Rust Valley Greybox first.");
                return null;
            }

            ApplyPlayerSettings();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildName = $"PlanetScrappers_v{GameVersion.Version}_QA";
            string buildsDir = Path.Combine(projectRoot, "Builds");
            string outDir = Path.Combine(buildsDir, buildName);
            string zipPath = Path.Combine(buildsDir, buildName + ".zip");

            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] Failed: {report.summary.result}, {report.summary.totalErrors} error(s).");
                return null;
            }

            // Strip folders that must not ship to players.
            foreach (var dir in Directory.GetDirectories(outDir, "*DoNotShip*"))
                Directory.Delete(dir, true);

            string changelog = Path.Combine(projectRoot, "CHANGELOG.md");
            if (File.Exists(changelog)) File.Copy(changelog, Path.Combine(outDir, "CHANGELOG.txt"), true);
            string qa = Path.Combine(projectRoot, "docs", "QA-checklist.md");
            if (File.Exists(qa)) File.Copy(qa, Path.Combine(outDir, "QA-checklist.txt"), true);
            File.WriteAllText(Path.Combine(outDir, "VERSION.txt"),
                $"{GameVersion.Label}\nBuilt {DateTime.Now:yyyy-MM-dd HH:mm}\nUnity {Application.unityVersion}\n");

            ZipFile.CreateFromDirectory(outDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: true);

            double mb = new FileInfo(zipPath).Length / (1024.0 * 1024.0);
            Debug.Log($"[Build] OK: {zipPath} ({mb:0.0} MB, build took {report.summary.totalTime:mm\\:ss})");
            return zipPath;
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = GameVersion.ProductName;
            PlayerSettings.companyName = "Planet Scrappers";
            PlayerSettings.bundleVersion = GameVersion.Version;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = false;
        }
    }
}
