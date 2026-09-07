using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ZIKJSmokeTests
{
    const string VenueLauncherScenePath = "Assets/Scenes/VenueLauncher.unity";
    const string VenueLauncherExePath = "Builds/Windows/ZIKJ-VenueLauncher.exe";
    const string ProjectionAlignmentScenePath = "Assets/Scenes/ProjectionAlignment.unity";
    const string ProjectionAlignmentExePath = "Builds/Windows/ZIKJ-ProjectionAlignment.exe";

    class ModeSpec
    {
        public string name;
        public string scenePath;
        public string exePath;
        public GameManager.GameMode mode;
        public Type modeType;
        public Type hudType;
        public int kartCount;
    }

    static readonly ModeSpec[] Modes =
    {
        new()
        {
            name = "Infected",
            scenePath = "Assets/Scenes/InfectedArena.unity",
            exePath = "Builds/Windows/ZIKJ-Infected.exe",
            mode = GameManager.GameMode.Infected,
            modeType = typeof(InfectedMode),
            hudType = typeof(InfectedHUD),
            kartCount = 6
        },
        new()
        {
            name = "Battle Royale",
            scenePath = "Assets/Scenes/BattleRoyaleArena.unity",
            exePath = "Builds/Windows/ZIKJ-BattleRoyale.exe",
            mode = GameManager.GameMode.BattleRoyale,
            modeType = typeof(BattleRoyaleMode),
            hudType = typeof(BattleRoyaleHUD),
            kartCount = 8
        },
        new()
        {
            name = "Grand Prix",
            scenePath = "Assets/Scenes/GrandPrixArena.unity",
            exePath = "Builds/Windows/ZIKJ-GrandPrix.exe",
            mode = GameManager.GameMode.GrandPrix,
            modeType = typeof(GrandPrixMode),
            hudType = typeof(GrandPrixHUD),
            kartCount = 8
        },
        new()
        {
            name = "Drift Arena",
            scenePath = "Assets/Scenes/DriftArena.unity",
            exePath = "Builds/Windows/ZIKJ-DriftArena.exe",
            mode = GameManager.GameMode.DriftArena,
            modeType = typeof(DriftArenaMode),
            hudType = typeof(DriftArenaHUD),
            kartCount = 8
        },
        new()
        {
            name = "Tron Trails",
            scenePath = "Assets/Scenes/TronTrails.unity",
            exePath = "Builds/Windows/ZIKJ-TronTrails.exe",
            mode = GameManager.GameMode.TronTrails,
            modeType = typeof(TronTrailsMode),
            hudType = typeof(TronTrailsHUD),
            kartCount = 8
        },
        new()
        {
            name = "Last One Lit",
            scenePath = "Assets/Scenes/LastOneLit.unity",
            exePath = "Builds/Windows/ZIKJ-LastOneLit.exe",
            mode = GameManager.GameMode.LastOneLit,
            modeType = typeof(LastOneLitMode),
            hudType = typeof(LastOneLitHUD),
            kartCount = 8
        }
    };

    [MenuItem("ZIKJ/Run Smoke Tests")]
    public static void RunAll()
    {
        try
        {
            foreach (var spec in Modes)
                ValidateMode(spec);

            ValidateProjectionAlignment();
            ValidateVenueLauncher();

            Debug.Log("[ZIKJ Smoke] All deployable smoke tests passed.");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[ZIKJ Smoke] FAILED: " + e);

            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            else
                throw;
        }
    }

    static void ValidateMode(ModeSpec spec)
    {
        Debug.Log($"[ZIKJ Smoke] Validating {spec.name}...");

        Require(File.Exists(spec.scenePath), $"{spec.name}: missing scene asset {spec.scenePath}");
        Require(File.Exists(spec.exePath), $"{spec.name}: missing Windows build {spec.exePath}");
        Require(new FileInfo(spec.exePath).Length > 0, $"{spec.name}: Windows build is empty");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapGo = new GameObject("Smoke Bootstrap");
        var bootstrap = bootstrapGo.AddComponent<InfectedSceneBootstrap>();
        bootstrap.buildOnStart = false;
        bootstrap.gameMode = spec.mode;
        bootstrap.kartCount = spec.kartCount;
        bootstrap.playerKartIndex = 0;
        bootstrap.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToGameScene(spec.mode);

        ValidateSceneObjects(spec);
        ValidateRuntimeSpawn(spec);

        Debug.Log($"[ZIKJ Smoke] {spec.name} passed.");
    }

    static void ValidateProjectionAlignment()
    {
        Debug.Log("[ZIKJ Smoke] Validating Projection Alignment...");

        Require(File.Exists(ProjectionAlignmentScenePath), $"Projection Alignment: missing scene asset {ProjectionAlignmentScenePath}");
        Require(File.Exists(ProjectionAlignmentExePath), $"Projection Alignment: missing Windows build {ProjectionAlignmentExePath}");
        Require(new FileInfo(ProjectionAlignmentExePath).Length > 0, "Projection Alignment: Windows build is empty");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapGo = new GameObject("Smoke Projection Alignment Bootstrap");
        var alignment = bootstrapGo.AddComponent<ProjectionAlignmentScene>();
        alignment.buildOnStart = false;
        alignment.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToProjectionScene();

        Require(UnityEngine.Object.FindFirstObjectByType<ProjectionAlignmentScene>() != null,
            "Projection Alignment: missing ProjectionAlignmentScene component");
        Require(UnityEngine.Object.FindFirstObjectByType<WaypointCircuit>() != null,
            "Projection Alignment: missing WaypointCircuit");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackMeshBuilder>() != null,
            "Projection Alignment: missing TrackMeshBuilder");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingReceiver>() != null,
            "Projection Alignment: missing TrackingReceiver");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingInputManager>() != null,
            "Projection Alignment: missing TrackingInputManager");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingDebugHUD>() != null,
            "Projection Alignment: missing TrackingDebugHUD");
        Require(UnityEngine.Object.FindFirstObjectByType<PerformanceMonitor>() != null,
            "Projection Alignment: missing PerformanceMonitor");
        Require(ZIKJReal3DSceneAssets.CurrentSceneReal3DObjectCount() >= 5,
            "Projection Alignment: missing real 3D camera/projector dressing");

        Camera cam = Camera.main;
        Require(cam != null, "Projection Alignment: missing Main Camera");
        Require(cam.orthographic, "Projection Alignment: camera is not orthographic");

        for (int i = 0; i < 4; i++)
            Require(GameObject.Find($"Projector Coverage P{i}") != null, $"Projection Alignment: missing projector coverage P{i}");

        Require(UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length >= 18,
            "Projection Alignment: expected at least 18 alignment grid lines");
        Require(alignment.AlignmentPointCount >= 81,
            $"Projection Alignment: expected at least 81 alignment points, found {alignment.AlignmentPointCount}");
        Require(alignment.TrackingMarkerPreviewCount == 6,
            $"Projection Alignment: expected 6 tracking marker previews, found {alignment.TrackingMarkerPreviewCount}");

        Debug.Log("[ZIKJ Smoke] Projection Alignment passed.");
    }

    static void ValidateVenueLauncher()
    {
        Debug.Log("[ZIKJ Smoke] Validating Venue Launcher...");

        Require(File.Exists(VenueLauncherScenePath), $"Venue Launcher: missing scene asset {VenueLauncherScenePath}");
        Require(File.Exists(VenueLauncherExePath), $"Venue Launcher: missing Windows build {VenueLauncherExePath}");
        Require(new FileInfo(VenueLauncherExePath).Length > 0, "Venue Launcher: Windows build is empty");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapGo = new GameObject("Smoke Venue Launcher Bootstrap");
        var launcher = bootstrapGo.AddComponent<VenueLauncherScene>();
        launcher.buildOnStart = false;
        launcher.BuildScene();
        ZIKJReal3DSceneAssets.ApplyToLauncherScene();

        Require(UnityEngine.Object.FindFirstObjectByType<VenueLauncherScene>() != null,
            "Venue Launcher: missing VenueLauncherScene component");
        Require(UnityEngine.Object.FindFirstObjectByType<VenueLauncherReturnHotkey>() != null,
            "Venue Launcher: missing return hotkey component");
        Require(UnityEngine.Object.FindFirstObjectByType<WaypointCircuit>() != null,
            "Venue Launcher: missing preview WaypointCircuit");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackMeshBuilder>() != null,
            "Venue Launcher: missing preview TrackMeshBuilder");
        Require(UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null,
            "Venue Launcher: missing EventSystem");
        Require(UnityEngine.Object.FindFirstObjectByType<Canvas>() != null,
            "Venue Launcher: missing Canvas");
        Require(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length >= VenueLauncherScene.TargetSceneNames().Length,
            "Venue Launcher: missing launch buttons");
        Require(Camera.main != null, "Venue Launcher: missing Main Camera");
        Require(ZIKJReal3DSceneAssets.CurrentSceneReal3DObjectCount() >= 16,
            "Venue Launcher: missing real 3D launcher dressing");

        Debug.Log("[ZIKJ Smoke] Venue Launcher passed.");
    }

    static void ValidateSceneObjects(ModeSpec spec)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        Require(manager != null, $"{spec.name}: GameManager was not created");
        Require(manager.gameMode == spec.mode, $"{spec.name}: GameManager mode was {manager.gameMode}, expected {spec.mode}");
        Require(manager.kartCount == spec.kartCount, $"{spec.name}: kart count was {manager.kartCount}, expected {spec.kartCount}");
        Require(manager.kartPrefab != null, $"{spec.name}: missing real 3D kart prefab reference");
        Require(manager.kartPrefab.GetComponent<KartController>() != null, $"{spec.name}: real 3D kart prefab missing KartController");
        Require(manager.kartPrefab.GetComponentsInChildren<Renderer>().Length > 0, $"{spec.name}: real 3D kart prefab has no renderers");
        Require(ZIKJReal3DSceneAssets.CurrentSceneReal3DObjectCount() >= 24,
            $"{spec.name}: expected real 3D dressing models, found {ZIKJReal3DSceneAssets.CurrentSceneReal3DObjectCount()}");

        Require(UnityEngine.Object.FindFirstObjectByType<WaypointCircuit>() != null, $"{spec.name}: missing WaypointCircuit");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackMeshBuilder>() != null, $"{spec.name}: missing TrackMeshBuilder");
        ValidateProjectedMap(spec);
        ValidateEnvironmentTheme(spec);
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingReceiver>() != null, $"{spec.name}: missing TrackingReceiver");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingInputManager>() != null, $"{spec.name}: missing TrackingInputManager");
        Require(UnityEngine.Object.FindFirstObjectByType<TrackingDebugHUD>() != null, $"{spec.name}: missing TrackingDebugHUD");
        Require(UnityEngine.Object.FindFirstObjectByType<PerformanceMonitor>() != null, $"{spec.name}: missing PerformanceMonitor");

        Camera cam = Camera.main;
        Require(cam != null, $"{spec.name}: missing Main Camera");
        Require(cam.GetComponent<CameraRig>() != null, $"{spec.name}: missing CameraRig on Main Camera");

        Require(FindMonoBehaviour(spec.modeType) != null, $"{spec.name}: missing mode component {spec.modeType.Name}");
        Require(FindMonoBehaviour(spec.hudType) != null, $"{spec.name}: missing HUD component {spec.hudType.Name}");
    }

    static void ValidateEnvironmentTheme(ModeSpec spec)
    {
        var environment = UnityEngine.Object.FindFirstObjectByType<InfectedEnvironmentBuilder>();
        Require(environment != null, $"{spec.name}: missing themed environment");
        Require(environment.theme == ExpectedEnvironmentTheme(spec.mode),
            $"{spec.name}: environment theme was {environment.theme}, expected {ExpectedEnvironmentTheme(spec.mode)}");

        switch (spec.mode)
        {
            case GameManager.GameMode.Infected:
                Require(GameObject.Find("Zombie Quarantine Gate Sign") != null,
                    $"{spec.name}: missing zombie quarantine gate");
                break;
            case GameManager.GameMode.BattleRoyale:
                Require(GameObject.Find("War Zone Command Bunker") != null,
                    $"{spec.name}: missing war-zone command bunker");
                break;
            case GameManager.GameMode.GrandPrix:
                Require(GameObject.Find("Grand Prix Rainbow Start Arch 0") != null,
                    $"{spec.name}: missing colorful start arch");
                Require(GameObject.Find("Grand Prix Boost Pad Preview 0 Base") != null,
                    $"{spec.name}: missing boost pad preview dressing");
                break;
            case GameManager.GameMode.DriftArena:
                Require(GameObject.Find("Moroccan Tent Ridge") != null,
                    $"{spec.name}: missing Moroccan tent ridge");
                break;
        }
    }

    static void ValidateProjectedMap(ModeSpec spec)
    {
        var projectedMap = UnityEngine.Object.FindFirstObjectByType<ProjectedBattleMapBuilder>();
        Require(projectedMap != null, $"{spec.name}: missing projected battle-race map");
        Require(projectedMap.theme == ExpectedProjectionTheme(spec.mode),
            $"{spec.name}: projected map theme was {projectedMap.theme}, expected {ExpectedProjectionTheme(spec.mode)}");
        Require(projectedMap.ProjectedElementCount >= 120,
            $"{spec.name}: expected rich projected map dressing, found {projectedMap.ProjectedElementCount}");
        Require(GameObject.Find("Projected Yellow Bonus Box 0") != null,
            $"{spec.name}: missing yellow bonus box projection");
        Require(GameObject.Find("Projected No Cut Label") != null,
            $"{spec.name}: missing no-cut projection label");
    }

    static InfectedEnvironmentBuilder.ArenaTheme ExpectedEnvironmentTheme(GameManager.GameMode mode)
    {
        return mode switch
        {
            GameManager.GameMode.Infected => InfectedEnvironmentBuilder.ArenaTheme.ZombieApocalypse,
            GameManager.GameMode.BattleRoyale => InfectedEnvironmentBuilder.ArenaTheme.WarZone,
            GameManager.GameMode.GrandPrix => InfectedEnvironmentBuilder.ArenaTheme.ColorfulKartArena,
            GameManager.GameMode.DriftArena => InfectedEnvironmentBuilder.ArenaTheme.MoroccanTent,
            GameManager.GameMode.LastOneLit => InfectedEnvironmentBuilder.ArenaTheme.Spotlight,
            _ => InfectedEnvironmentBuilder.ArenaTheme.NeonArena
        };
    }

    static ProjectedBattleMapBuilder.ProjectionTheme ExpectedProjectionTheme(GameManager.GameMode mode)
    {
        return mode switch
        {
            GameManager.GameMode.Infected => ProjectedBattleMapBuilder.ProjectionTheme.Infected,
            GameManager.GameMode.BattleRoyale => ProjectedBattleMapBuilder.ProjectionTheme.BattleRoyale,
            GameManager.GameMode.GrandPrix => ProjectedBattleMapBuilder.ProjectionTheme.GrandPrix,
            GameManager.GameMode.DriftArena => ProjectedBattleMapBuilder.ProjectionTheme.DriftArena,
            GameManager.GameMode.TronTrails => ProjectedBattleMapBuilder.ProjectionTheme.TronTrails,
            GameManager.GameMode.LastOneLit => ProjectedBattleMapBuilder.ProjectionTheme.LastOneLit,
            _ => ProjectedBattleMapBuilder.ProjectionTheme.GrandPrix
        };
    }

    static void ValidateRuntimeSpawn(ModeSpec spec)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        Require(manager != null, $"{spec.name}: GameManager missing before runtime spawn validation");

        MethodInfo start = typeof(GameManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(start != null, $"{spec.name}: could not find GameManager.Start");
        start.Invoke(manager, null);

        var karts = manager.Karts;
        Require(karts != null, $"{spec.name}: GameManager did not spawn karts");
        Require(karts.Length == spec.kartCount, $"{spec.name}: spawned {karts.Length} karts, expected {spec.kartCount}");

        int playerCount = 0;
        int aiFallbackCount = 0;
        for (int i = 0; i < karts.Length; i++)
        {
            var kart = karts[i];
            Require(kart != null, $"{spec.name}: kart {i} is null");
            Require(kart.kartIndex == i, $"{spec.name}: kart index mismatch at {i}");
            Require(kart.GetComponent<KartAI>() != null, $"{spec.name}: kart {i} missing KartAI");
            Require(kart.GetComponent<KartVisualEffects>() != null, $"{spec.name}: kart {i} missing KartVisualEffects");

            if (kart.isPlayer)
                playerCount++;
            else if (!kart.trackingMode)
                aiFallbackCount++;
        }

        Require(playerCount == 1, $"{spec.name}: expected exactly one player kart, found {playerCount}");
        Require(aiFallbackCount == spec.kartCount - 1,
            $"{spec.name}: expected {spec.kartCount - 1} AI fallback non-player karts before live tracking, found {aiFallbackCount}");

        var tracking = UnityEngine.Object.FindFirstObjectByType<TrackingInputManager>();
        Require(tracking != null, $"{spec.name}: missing TrackingInputManager after spawn");
        Require(tracking.GetTrackedKartCount() == spec.kartCount - 1,
            $"{spec.name}: tracking manager registered {tracking.GetTrackedKartCount()} karts, expected {spec.kartCount - 1}");

        if (spec.mode == GameManager.GameMode.GrandPrix)
        {
            var grandPrix = UnityEngine.Object.FindFirstObjectByType<GrandPrixMode>();
            Require(grandPrix != null, $"{spec.name}: missing GrandPrixMode after spawn");
            Require(grandPrix.ActiveItemBoxCount() >= 3,
                $"{spec.name}: expected spawned item boxes, found {grandPrix.ActiveItemBoxCount()}");
            Require(grandPrix.ActiveBoostPadCount() >= 2,
                $"{spec.name}: expected spawned boost pads, found {grandPrix.ActiveBoostPadCount()}");
        }
    }

    static MonoBehaviour FindMonoBehaviour(Type type)
    {
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && type.IsInstanceOfType(behaviour))
                return behaviour;
        }

        return null;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
