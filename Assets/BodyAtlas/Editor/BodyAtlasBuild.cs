#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BodyAtlas.Editor
{
    public static class BodyAtlasBuild
    {
        const string ScenePath = "Assets/Scenes/BodyAtlas.unity";

        [MenuItem("Body Atlas/3. Build Android APK (debug)")]
        public static void BuildAndroidMenu() => BuildAndroid();

        public static void BuildAndroid()
        {
            // Ensure scene exists
            if (!File.Exists(ScenePath))
            {
                Debug.Log("[BodyAtlas] Scene missing — running Setup Scene first.");
                BodyAtlasSetup.SetupScene();
            }

            PlayerSettings.companyName = "Sumit";
            PlayerSettings.productName = "Body Atlas";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.sumit.bodyatlasunity");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
#endif
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildType = AndroidBuildType.Development;
            EditorUserBuildSettings.development = true;

            string outDir = "/workspace/artifacts";
            Directory.CreateDirectory(outDir);
            string apk = Path.Combine(outDir, "BodyAtlasUnity-debug.apk");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log("[BodyAtlas] Building Android → " + apk);
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary sum = report.summary;
            Debug.Log($"[BodyAtlas] Build result: {sum.result} size={sum.totalSize} time={sum.totalTime}");
            if (sum.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError(msg.content);
                throw new Exception("Android build failed: " + sum.result);
            }
            // Write marker
            File.WriteAllText(Path.Combine(outDir, "BodyAtlasUnity-build-ok.txt"),
                $"OK {DateTime.UtcNow:o}\n{apk}\nsize={sum.totalSize}\n");
        }

        /// <summary>Batchmode entry: setup scene then build.</summary>
        public static void SetupAndBuildAndroid()
        {
            BodyAtlasSetup.SetupScene();
            BuildAndroid();
        }

        public static void SetupOnly()
        {
            BodyAtlasSetup.SetupScene();
            EditorApplication.Exit(0);
        }
    }
}
#endif
