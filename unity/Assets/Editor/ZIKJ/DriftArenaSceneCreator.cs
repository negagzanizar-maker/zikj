using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DriftArenaSceneCreator
{
    const string ScenePath = "Assets/Scenes/DriftArena.unity";

    [MenuItem("ZIKJ/Create Drift Arena Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DriftArena";

        var bootstrap = new GameObject("Bootstrap");
        var bootstrapper = bootstrap.AddComponent<InfectedSceneBootstrap>();
        bootstrapper.gameMode = GameManager.GameMode.DriftArena;
        bootstrapper.kartCount = 8;
        bootstrapper.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(GameManager.GameMode.DriftArena);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log($"Created {ScenePath}. Press Play to run Drift Arena.");
    }
}
