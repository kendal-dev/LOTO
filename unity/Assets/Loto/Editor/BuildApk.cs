using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Loto.EditorTools
{
    /// <summary>
    /// The Quest APK, the same as File > Build with the project's own settings: the scenes of the build
    /// list, Android, a Development build (so logcat shows the app's lines).
    ///   Menu: Loto > Build APK (with Unity open).
    ///   Command line, Unity closed (Tools/build.ps1 does it):
    ///   Unity.exe -batchmode -force-d3d11 -projectPath . -buildTarget Android -executeMethod Loto.EditorTools.BuildApk.Run -logFile Logs/build.log -quit
    /// The APK lands in Builds/loto.apk (APKs are never committed: .gitignore).
    /// </summary>
    public static class BuildApk
    {
        public const string OutputPath = "Builds/loto.apk";

        [MenuItem("Loto/Build APK")]
        public static void Run()
        {
            // The project must BE on Android, not only build for it. Meta's build step (OVRGradleGeneration)
            // reads EditorUserBuildSettings.selectedBuildTargetGroup: with the editor still on Windows it skips
            // its work and the APK ends up with two libopenxr_loader.so (Unity's and OVRPlugin's), and Gradle
            // fails in mergeDebugNativeLibs. Seen on 20/11/2026 with a project freshly opened on Windows.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Loto] Switching the project to Android before the build (first time: Unity reimports, a few minutes).");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[Loto] Could not switch to Android. Do it by hand: File > Build Profiles > Android > Switch Platform, then Loto > Build APK.");
                    return;
                }
            }
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

            // First build on a fresh clone: the scene is made by code, so make it if it is not there yet.
            if (!File.Exists(LotoSceneBuilder.ScenePath))
            {
                Debug.Log("[Loto] " + LotoSceneBuilder.ScenePath + " does not exist yet: building it first.");
                LotoSceneBuilder.Build(true);
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { LotoSceneBuilder.ScenePath };

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[Loto] APK: " + summary.result + ", " + (summary.totalSize / 1048576f).ToString("F1") + " MB, " +
                summary.totalTime.TotalSeconds.ToString("F0") + " s, " + Path.GetFullPath(OutputPath));
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
