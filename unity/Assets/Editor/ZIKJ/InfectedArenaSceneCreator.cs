using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InfectedArenaSceneCreator
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/InfectedArena.unity";

    [MenuItem("ZIKJ/Create Infected Arena Scene")]
    public static void CreateScene()
    {
        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "InfectedArena";

        var bootstrap = new GameObject("Bootstrap");
        bootstrap.AddComponent<InfectedSceneBootstrap>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Selection.activeGameObject = bootstrap;
        Debug.Log($"Created {ScenePath}. Press Play to run Infected.");
    }
}
