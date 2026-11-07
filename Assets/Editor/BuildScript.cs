using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the game to an Android APK.
/// - Menu: Tools &gt; Racing Kit &gt; Build Android APK (local builds once the
///   Android module is installed in Unity Hub).
/// - CI: GitHub Actions (game-ci) calls BuildScript.PerformBuild, which
///   regenerates the demo scene from scratch, so no stale scene files are needed.
/// </summary>
public static class BuildScript
{
    [MenuItem("Tools/Racing Kit/Build Android APK")]
    public static void BuildAndroidMenu()
    {
        PerformBuild();
    }

    /// <summary>
    /// Entry point used by CI (game-ci unity-builder input: buildMethod).
    /// Also safe to run locally.
    /// </summary>
    public static void PerformBuild()
    {
        ConfigureAndroid();
        ConfigureSigning();

        // 1. Rebuild the demo scene fresh so the build never depends on stale scene files
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TrackBuilder.BuildNoPrompt();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DemoRace.unity");
        string[] scenes = { "Assets/Scenes/DemoRace.unity" };

        // 2. Build the APK
        string outputPath = Path.Combine("build", "CarRacing.apk");
        Directory.CreateDirectory("build");

        var report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, BuildOptions.None);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("APK build failed: " + report.summary.result);
            throw new InvalidOperationException("Android build failed");
        }

        Debug.Log("[Racing Kit] APK built successfully: " + Path.GetFullPath(outputPath));
    }

    static void ConfigureAndroid()
    {
        PlayerSettings.companyName = "RacingKit";
        PlayerSettings.productName = "Car Racing";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.racingkit.carracing");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Landscape;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // Modern Android requirement: 64-bit support via IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingBackend.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersion.24;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
    }

    /// <summary>
    /// CI passes the generated keystore as base-64 environment variables and
    /// signs the APK with it. Local builds keep whatever keystore is configured
    /// in Project Settings (Unity's debug keystore by default).
    /// </summary>
    static void ConfigureSigning()
    {
        string b64 = Environment.GetEnvironmentVariable("APK_KEYSTORE_B64");
        if (string.IsNullOrEmpty(b64)) return; // local/manual build

        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string keystorePath = Path.Combine(projectRoot, "racing.keystore");
            File.WriteAllBytes(keystorePath, Convert.FromBase64String(b64));

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("APK_KEYSTORE_PASS") ?? "Racing2026";
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("APK_KEYALIAS") ?? "racing";
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("APK_KEYALIAS_PASS") ?? "Racing2026";

            Debug.Log("[Racing Kit] APK will be signed with the CI keystore.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Racing Kit] Failed to configure signing keystore: " + e.Message + "\n" + e);
            throw;
        }
    }
}
