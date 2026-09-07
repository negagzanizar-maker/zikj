using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VenueLauncherSceneCreator
{
    const string SceneDir = "Assets/Scenes";
    public const string ScenePath = SceneDir + "/VenueLauncher.unity";

    [MenuItem("ZIKJ/Create Venue Launcher Scene")]
    public static void CreateScene()
    {
        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = VenueLauncherScene.SceneName;

        var bootstrap = new GameObject("Venue Launcher Bootstrap");
        var launcher = bootstrap.AddComponent<VenueLauncherScene>();
        launcher.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToLauncherScene();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Selection.activeGameObject = bootstrap;
        Debug.Log($"Created {ScenePath}. Press Play to open the Unity launcher.");
    }
}
