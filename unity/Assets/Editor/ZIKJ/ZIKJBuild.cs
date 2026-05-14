using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ZIKJBuild
{
    const string ScenePath = "Assets/Scenes/InfectedArena.unity";
    const string OutputPath = "Builds/Windows/ZIKJ-Infected.exe";

    [MenuItem("ZIKJ/Build Windows Infected")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath))
            InfectedArenaSceneCreator.CreateScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { ScenePath },
            OutputPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Windows build failed: {report.summary.result}");

        Debug.Log($"Windows build created: {OutputPath}");
    }
}
