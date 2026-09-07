using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TronTrailsSceneCreator
{
    const string ScenePath = "Assets/Scenes/TronTrails.unity";

    [MenuItem("ZIKJ/Create Tron Trails Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "TronTrails";

        var bootstrap = new GameObject("Bootstrap");
        var bootstrapper = bootstrap.AddComponent<InfectedSceneBootstrap>();
        bootstrapper.gameMode = GameManager.GameMode.TronTrails;
        bootstrapper.kartCount = 8;
        bootstrapper.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(GameManager.GameMode.TronTrails);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log($"Created {ScenePath}. Press Play to run Tron Trails.");
    }
}
