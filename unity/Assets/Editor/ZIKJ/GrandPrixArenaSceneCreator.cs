using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GrandPrixArenaSceneCreator
{
    const string ScenePath = "Assets/Scenes/GrandPrixArena.unity";

    [MenuItem("ZIKJ/Create Grand Prix Arena Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "GrandPrixArena";

        var bootstrap = new GameObject("Bootstrap");
        var bootstrapper = bootstrap.AddComponent<InfectedSceneBootstrap>();
        bootstrapper.gameMode = GameManager.GameMode.GrandPrix;
        bootstrapper.kartCount = 8;
        bootstrapper.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(GameManager.GameMode.GrandPrix);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log($"Created {ScenePath}. Press Play to run Grand Prix.");
    }
}
