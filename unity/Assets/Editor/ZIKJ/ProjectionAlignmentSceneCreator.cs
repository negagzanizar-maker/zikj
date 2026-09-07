using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectionAlignmentSceneCreator
{
    const string SceneDir = "Assets/Scenes";
    public const string ScenePath = SceneDir + "/ProjectionAlignment.unity";

    [MenuItem("ZIKJ/Create Projection Alignment Scene")]
    public static void CreateScene()
    {
        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ProjectionAlignment";

        var bootstrap = new GameObject("Projection Alignment Bootstrap");
        var alignment = bootstrap.AddComponent<ProjectionAlignmentScene>();
        alignment.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToProjectionScene();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Selection.activeGameObject = bootstrap;
        Debug.Log($"Created {ScenePath}. Press Play to run projection alignment.");
    }
}
