using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ZIKJBuild
{
    const string VenueLauncherScenePath = "Assets/Scenes/VenueLauncher.unity";
    const string InfectedScenePath = "Assets/Scenes/InfectedArena.unity";
    const string BattleRoyaleScenePath = "Assets/Scenes/BattleRoyaleArena.unity";
    const string GrandPrixScenePath = "Assets/Scenes/GrandPrixArena.unity";
    const string DriftArenaScenePath = "Assets/Scenes/DriftArena.unity";
    const string TronTrailsScenePath = "Assets/Scenes/TronTrails.unity";
    const string LastOneLitScenePath = "Assets/Scenes/LastOneLit.unity";
    const string ProjectionAlignmentScenePath = "Assets/Scenes/ProjectionAlignment.unity";
    const string InfectedOutputPath = "Builds/Windows/ZIKJ-Infected.exe";
    const string BattleRoyaleOutputPath = "Builds/Windows/ZIKJ-BattleRoyale.exe";
    const string GrandPrixOutputPath = "Builds/Windows/ZIKJ-GrandPrix.exe";
    const string DriftArenaOutputPath = "Builds/Windows/ZIKJ-DriftArena.exe";
    const string TronTrailsOutputPath = "Builds/Windows/ZIKJ-TronTrails.exe";
    const string LastOneLitOutputPath = "Builds/Windows/ZIKJ-LastOneLit.exe";
    const string ProjectionAlignmentOutputPath = "Builds/Windows/ZIKJ-ProjectionAlignment.exe";
    const string VenueLauncherOutputPath = "Builds/Windows/ZIKJ-VenueLauncher.exe";
    static readonly string[] AllDeployableScenePaths =
    {
        VenueLauncherScenePath,
        InfectedScenePath,
        BattleRoyaleScenePath,
        GrandPrixScenePath,
        DriftArenaScenePath,
        TronTrailsScenePath,
        LastOneLitScenePath,
        ProjectionAlignmentScenePath
    };

    [MenuItem("ZIKJ/Build Windows Infected")]
    public static void BuildWindows()
    {
        if (!File.Exists(InfectedScenePath))
            InfectedArenaSceneCreator.CreateScene();

        BuildScene(InfectedScenePath, InfectedOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Battle Royale")]
    public static void BuildWindowsBattleRoyale()
    {
        if (!File.Exists(BattleRoyaleScenePath))
            BattleRoyaleArenaSceneCreator.CreateScene();

        BuildScene(BattleRoyaleScenePath, BattleRoyaleOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Grand Prix")]
    public static void BuildWindowsGrandPrix()
    {
        if (!File.Exists(GrandPrixScenePath))
            GrandPrixArenaSceneCreator.CreateScene();

        BuildScene(GrandPrixScenePath, GrandPrixOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Drift Arena")]
    public static void BuildWindowsDriftArena()
    {
        if (!File.Exists(DriftArenaScenePath))
            DriftArenaSceneCreator.CreateScene();

        BuildScene(DriftArenaScenePath, DriftArenaOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Tron Trails")]
    public static void BuildWindowsTronTrails()
    {
        if (!File.Exists(TronTrailsScenePath))
            TronTrailsSceneCreator.CreateScene();

        BuildScene(TronTrailsScenePath, TronTrailsOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Last One Lit")]
    public static void BuildWindowsLastOneLit()
    {
        if (!File.Exists(LastOneLitScenePath))
            LastOneLitSceneCreator.CreateScene();

        BuildScene(LastOneLitScenePath, LastOneLitOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Projection Alignment")]
    public static void BuildWindowsProjectionAlignment()
    {
        if (!File.Exists(ProjectionAlignmentScenePath))
            ProjectionAlignmentSceneCreator.CreateScene();

        BuildScene(ProjectionAlignmentScenePath, ProjectionAlignmentOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows Venue Launcher")]
    public static void BuildWindowsVenueLauncher()
    {
        EnsureAllScenesExist();
        BuildScenes(AllDeployableScenePaths, VenueLauncherOutputPath);
    }

    [MenuItem("ZIKJ/Build Windows All Deployables")]
    public static void BuildWindowsAll()
    {
        RegenerateAllScenes();
        BuildWindows();
        BuildWindowsBattleRoyale();
        BuildWindowsGrandPrix();
        BuildWindowsDriftArena();
        BuildWindowsTronTrails();
        BuildWindowsLastOneLit();
        BuildWindowsProjectionAlignment();
        BuildWindowsVenueLauncher();
        ConfigureAllDeployableScenes();
    }

    [MenuItem("ZIKJ/Configure All Deployable Scenes")]
    public static void ConfigureAllDeployableScenes()
    {
        EnsureAllScenesExist();
        EditorBuildSettings.scenes = BuildSettingsScenes(AllDeployableScenePaths);
    }

    [MenuItem("ZIKJ/Regenerate All Scenes")]
    public static void RegenerateAllScenes()
    {
        VenueLauncherSceneCreator.CreateScene();
        InfectedArenaSceneCreator.CreateScene();
        BattleRoyaleArenaSceneCreator.CreateScene();
        GrandPrixArenaSceneCreator.CreateScene();
        DriftArenaSceneCreator.CreateScene();
        TronTrailsSceneCreator.CreateScene();
        LastOneLitSceneCreator.CreateScene();
        ProjectionAlignmentSceneCreator.CreateScene();
        ConfigureAllDeployableScenes();
    }

    static void BuildScene(string scenePath, string outputPath)
    {
        BuildScenes(new[] { scenePath }, outputPath);
    }

    static void BuildScenes(string[] scenePaths, string outputPath)
    {
        EditorBuildSettings.scenes = BuildSettingsScenes(scenePaths);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        BuildReport report = BuildPipeline.BuildPlayer(
            scenePaths,
            outputPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Windows build failed: {report.summary.result}");

        Debug.Log($"Windows build created: {outputPath}");
    }

    static void EnsureAllScenesExist()
    {
        if (!File.Exists(VenueLauncherScenePath))
            VenueLauncherSceneCreator.CreateScene();
        if (!File.Exists(InfectedScenePath))
            InfectedArenaSceneCreator.CreateScene();
        if (!File.Exists(BattleRoyaleScenePath))
            BattleRoyaleArenaSceneCreator.CreateScene();
        if (!File.Exists(GrandPrixScenePath))
            GrandPrixArenaSceneCreator.CreateScene();
        if (!File.Exists(DriftArenaScenePath))
            DriftArenaSceneCreator.CreateScene();
        if (!File.Exists(TronTrailsScenePath))
            TronTrailsSceneCreator.CreateScene();
        if (!File.Exists(LastOneLitScenePath))
            LastOneLitSceneCreator.CreateScene();
        if (!File.Exists(ProjectionAlignmentScenePath))
            ProjectionAlignmentSceneCreator.CreateScene();
    }

    static EditorBuildSettingsScene[] BuildSettingsScenes(params string[] scenePaths)
    {
        var scenes = new EditorBuildSettingsScene[scenePaths.Length];
        for (int i = 0; i < scenePaths.Length; i++)
            scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);

        return scenes;
    }
}
