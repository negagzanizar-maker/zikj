using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BattleRoyaleArenaSceneCreator
{
    const string ScenePath = "Assets/Scenes/BattleRoyaleArena.unity";

    [MenuItem("ZIKJ/Create Battle Royale Arena Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleRoyaleArena";

        var bootstrap = new GameObject("Bootstrap");
        var bootstrapper = bootstrap.AddComponent<InfectedSceneBootstrap>();
        bootstrapper.gameMode = GameManager.GameMode.BattleRoyale;
        bootstrapper.kartCount = 8;
        bootstrapper.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(GameManager.GameMode.BattleRoyale);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log($"Created {ScenePath}. Press Play to run Battle Royale.");
    }
}
