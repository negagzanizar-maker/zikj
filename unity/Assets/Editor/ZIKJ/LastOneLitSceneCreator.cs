using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LastOneLitSceneCreator
{
    const string ScenePath = "Assets/Scenes/LastOneLit.unity";

    [MenuItem("ZIKJ/Create Last One Lit Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "LastOneLit";

        var bootstrap = new GameObject("Bootstrap");
        var bootstrapper = bootstrap.AddComponent<InfectedSceneBootstrap>();
        bootstrapper.gameMode = GameManager.GameMode.LastOneLit;
        bootstrapper.kartCount = 8;
        bootstrapper.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(GameManager.GameMode.LastOneLit);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log($"Created {ScenePath}. Press Play to run Last One Lit.");
    }
}
