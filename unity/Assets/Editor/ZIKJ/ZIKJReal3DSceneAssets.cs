using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ZIKJReal3DSceneAssets
{
    const string DressingRootName = "Real 3D Asset Dressing";
    const string PrefabDir = "Assets/Prefabs/ZIKJ";
    const string KartPrefabPath = PrefabDir + "/RealKart.prefab";
    const string CarKitFbx = "Assets/ThirdParty/Kenney/CarKit/Models/FBX format/";
    const string RacingKitFbx = "Assets/ThirdParty/Kenney/RacingKit/Models/FBX format/";
    const string ToyCarKitFbx = "Assets/ThirdParty/Kenney/ToyCarKit/Models/FBX format/";

    public static GameObject EnsureRealKartPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(KartPrefabPath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(PrefabDir);

        var root = new GameObject("Real Kart Prefab");
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.28f, 0f);
        collider.size = new Vector3(0.95f, 0.56f, 1.55f);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        root.AddComponent<KartController>();
        root.AddComponent<KartAI>();
        root.AddComponent<KartVisualEffects>();

        GameObject model = LoadModel(CarKit("kart-ooli.fbx"))
            ?? LoadModel(CarKit("kart-oobi.fbx"))
            ?? LoadModel(Toy("vehicle-racer.fbx"));

        if (model != null)
        {
            GameObject child = InstantiateModel(model, root.transform);
            child.name = "Real Kart Model";
            child.transform.localRotation = Quaternion.identity;
            FitModelToKart(child);
            RemoveColliders(child);
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "Fallback Kart Model";
            fallback.transform.SetParent(root.transform, false);
            fallback.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            fallback.transform.localScale = new Vector3(0.85f, 0.4f, 1.35f);
            RemoveColliders(fallback);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, KartPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    public static void ApplyToGameScene(GameManager.GameMode mode)
    {
        GameObject root = ResetDressingRoot();
        AssignKartPrefab();
        BuildUniversalVenue(root);
        BuildCircuitBarrierModels(root);
        BuildModeDressing(root, mode);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void ApplyToLauncherScene()
    {
        GameObject root = ResetDressingRoot();
        BuildUniversalVenue(root);

        string[] kartModels =
        {
            CarKit("kart-oobi.fbx"),
            CarKit("kart-oodi.fbx"),
            CarKit("kart-ooli.fbx"),
            CarKit("kart-oopi.fbx"),
            CarKit("kart-oozi.fbx"),
            Toy("vehicle-speedster.fbx")
        };

        for (int i = 0; i < kartModels.Length; i++)
        {
            float t = 0.08f + i * 0.08f;
            Vector3 pos = WaypointCircuit.At(t) + new Vector3(0f, 0.08f, i % 2 == 0 ? -1.15f : 1.15f);
            PlaceModel(root, kartModels[i], $"Real 3D Launcher Kart {i + 1}", pos, Quaternion.Euler(0f, -90f + i * 13f, 0f), Vector3.one * 0.55f);
        }

        PlaceModel(root, Toy("gate-finish.fbx"), "Real 3D Launcher Finish Gate", new Vector3(18.2f, 0f, WaypointCircuit.TopZ), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.2f);
        PlaceModel(root, Racing("billboard.fbx"), "Real 3D Launcher Billboard", new Vector3(48f, 0f, -35.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.0f);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void ApplyToProjectionScene()
    {
        GameObject root = ResetDressingRoot();
        PlaceModel(root, Racing("camera_exclusive.fbx"), "Real 3D Tracking Camera North West", new Vector3(5.2f, 5.6f, -3.8f), Quaternion.Euler(20f, 135f, 0f), Vector3.one * 1.3f);
        PlaceModel(root, Racing("camera_exclusive.fbx"), "Real 3D Tracking Camera North East", new Vector3(58.8f, 5.6f, -3.8f), Quaternion.Euler(20f, 225f, 0f), Vector3.one * 1.3f);
        PlaceModel(root, Racing("overhead.fbx"), "Real 3D Projector Truss North", new Vector3(32f, 5.5f, -5f), Quaternion.identity, Vector3.one * 2.4f);
        PlaceModel(root, Racing("overheadLights.fbx"), "Real 3D Alignment Light Bar", new Vector3(32f, 5.7f, -31.4f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.4f);
        PlaceModel(root, Racing("roadStartPositions.fbx"), "Real 3D Calibration Track Start", new Vector3(18.2f, 0.02f, WaypointCircuit.TopZ), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.8f);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static int CurrentSceneReal3DObjectCount()
    {
        GameObject root = GameObject.Find(DressingRootName);
        return root == null ? 0 : root.transform.childCount;
    }

    static void AssignKartPrefab()
    {
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
            return;

        manager.kartPrefab = EnsureRealKartPrefab();
        EditorUtility.SetDirty(manager);
    }

    static GameObject ResetDressingRoot()
    {
        GameObject existing = GameObject.Find(DressingRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        return new GameObject(DressingRootName);
    }

    static void BuildUniversalVenue(GameObject root)
    {
        PlaceModel(root, Racing("grandStandCovered.fbx"), "Real 3D North Covered Grandstand", new Vector3(32f, 0f, 4.1f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.7f);
        PlaceModel(root, Racing("grandStandCovered.fbx"), "Real 3D South Covered Grandstand", new Vector3(32f, 0f, -39.7f), Quaternion.identity, Vector3.one * 2.7f);
        PlaceModel(root, Racing("overheadLights.fbx"), "Real 3D Start Overhead Lights", new Vector3(18.2f, 0f, WaypointCircuit.TopZ), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.1f);
        PlaceModel(root, Toy("gate-finish.fbx"), "Real 3D Finish Gate Model", new Vector3(18.2f, 0f, WaypointCircuit.TopZ), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.0f);
        PlaceModel(root, Racing("flagCheckers.fbx"), "Real 3D Checkered Flag West", new Vector3(17.2f, 0f, WaypointCircuit.TopZ - 5.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.7f);
        PlaceModel(root, Racing("flagCheckers.fbx"), "Real 3D Checkered Flag East", new Vector3(17.2f, 0f, WaypointCircuit.TopZ + 5.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.7f);

        Vector3[] lightPosts =
        {
            new(6f, 0f, -4.2f),
            new(58f, 0f, -4.2f),
            new(6f, 0f, -31.8f),
            new(58f, 0f, -31.8f)
        };

        for (int i = 0; i < lightPosts.Length; i++)
            PlaceModel(root, Racing("lightPostModern.fbx"), $"Real 3D Light Post {i}", lightPosts[i], Quaternion.identity, Vector3.one * 1.6f);

        for (int i = 0; i < 8; i++)
        {
            float t = 0.05f + i * 0.11f;
            Vector3 pos = WaypointCircuit.At(t);
            PlaceModel(root, Racing(i % 2 == 0 ? "pylon.fbx" : "flagRed.fbx"), $"Real 3D Trackside Prop {i}", pos + new Vector3(0f, 0f, i % 2 == 0 ? -3.9f : 3.9f), Quaternion.Euler(0f, i * 35f, 0f), Vector3.one * 1.2f);
        }
    }

    static void BuildCircuitBarrierModels(GameObject root)
    {
        int steps = 32;
        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / steps;
            Vector3 p0 = WaypointCircuit.At(t);
            Vector3 p1 = WaypointCircuit.At(t + 1f / steps);
            Vector3 forward = (p1 - p0).normalized;
            if (forward.sqrMagnitude < 0.001f)
                continue;

            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            string path = Racing(i % 2 == 0 ? "barrierRed.fbx" : "barrierWhite.fbx");

            PlaceModel(root, path, $"Real 3D Outer Track Barrier {i}", p0 + right * (WaypointCircuit.HalfWidth + 0.72f), rot, Vector3.one * 1.15f);
            PlaceModel(root, path, $"Real 3D Inner Track Barrier {i}", p0 - right * (WaypointCircuit.HalfWidth + 0.72f), rot, Vector3.one * 1.15f);
        }
    }

    static void BuildModeDressing(GameObject root, GameManager.GameMode mode)
    {
        switch (mode)
        {
            case GameManager.GameMode.BattleRoyale:
                BuildBattleRoyaleModels(root);
                break;
            case GameManager.GameMode.GrandPrix:
                BuildGrandPrixModels(root);
                break;
            case GameManager.GameMode.DriftArena:
                BuildDriftModels(root);
                break;
            case GameManager.GameMode.TronTrails:
                BuildTronModels(root);
                break;
            case GameManager.GameMode.LastOneLit:
                BuildLastOneLitModels(root);
                break;
            default:
                BuildInfectedModels(root);
                break;
        }
    }

    static void BuildInfectedModels(GameObject root)
    {
        PlaceModel(root, CarKit("ambulance.fbx"), "Real 3D Wrecked Ambulance", new Vector3(10.5f, 0f, -28f), Quaternion.Euler(0f, 26f, 0f), Vector3.one * 0.9f);
        PlaceModel(root, CarKit("police.fbx"), "Real 3D Abandoned Police Car", new Vector3(52f, 0f, -10.8f), Quaternion.Euler(0f, -22f, 0f), Vector3.one * 0.9f);
        PlaceModel(root, CarKit("garbage-truck.fbx"), "Real 3D Quarantine Truck", new Vector3(55f, 0f, -27.5f), Quaternion.Euler(0f, -68f, 0f), Vector3.one * 0.85f);
        PlaceScattered(root, CarKit("debris-tire.fbx"), "Real 3D Infected Tire Debris", new Vector3(14f, 0f, -13f), 7, 2.6f, 0.55f);
        PlaceScattered(root, CarKit("cone.fbx"), "Real 3D Infected Traffic Cone", new Vector3(47f, 0f, -28f), 8, 1.8f, 0.75f);
        PlaceModel(root, Racing("fenceStraight.fbx"), "Real 3D Quarantine Fence North", new Vector3(28f, 0f, -3.8f), Quaternion.identity, Vector3.one * 2.4f);
        PlaceModel(root, Racing("fenceStraight.fbx"), "Real 3D Quarantine Fence South", new Vector3(38f, 0f, -32.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.4f);
    }

    static void BuildBattleRoyaleModels(GameObject root)
    {
        PlaceModel(root, CarKit("truck.fbx"), "Real 3D Battle Supply Truck", new Vector3(54f, 0f, -8.5f), Quaternion.Euler(0f, -125f, 0f), Vector3.one * 1.0f);
        PlaceModel(root, Racing("radarEquipment.fbx"), "Real 3D Battle Radar", new Vector3(8.5f, 0f, -7.2f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 1.7f);
        PlaceModel(root, Racing("tentClosedLong.fbx"), "Real 3D Battle Field Tent", new Vector3(14f, 0f, -31f), Quaternion.Euler(0f, 12f, 0f), Vector3.one * 1.7f);
        PlaceScattered(root, CarKit("box.fbx"), "Real 3D Battle Crate", new Vector3(36f, 0f, -18f), 10, 2.4f, 1.1f);
        PlaceScattered(root, Racing("barrierWall.fbx"), "Real 3D Battle Concrete Barrier", new Vector3(20f, 0f, -12f), 6, 3.8f, 1.2f);
    }

    static void BuildGrandPrixModels(GameObject root)
    {
        PlaceModel(root, Racing("bannerTowerRed.fbx"), "Real 3D Grand Prix Banner Red", new Vector3(18f, 0f, -4.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Racing("bannerTowerGreen.fbx"), "Real 3D Grand Prix Banner Green", new Vector3(18f, 0f, -31.2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Toy("item-box.fbx"), "Real 3D Grand Prix Hero Item Box", new Vector3(32f, 0.4f, -18f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.3f);
        PlaceScattered(root, Toy("item-coin-gold.fbx"), "Real 3D Grand Prix Coin", new Vector3(38f, 0.4f, -11f), 8, 2.2f, 1.0f);
        PlaceModel(root, Toy("vehicle-racer.fbx"), "Real 3D Grand Prix Display Racer", new Vector3(47f, 0f, -29.5f), Quaternion.Euler(0f, -36f, 0f), Vector3.one * 0.7f);
        PlaceModel(root, Racing("roadStartPositions.fbx"), "Real 3D Grand Prix Start Grid", new Vector3(20.8f, 0.02f, WaypointCircuit.TopZ), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.7f);
    }

    static void BuildDriftModels(GameObject root)
    {
        PlaceModel(root, Racing("tentRoofDouble.fbx"), "Real 3D Drift Tent Roof", new Vector3(32f, 3.3f, -18f), Quaternion.identity, Vector3.one * 4.5f);
        PlaceModel(root, Racing("tentLong.fbx"), "Real 3D Drift Tent North", new Vector3(18f, 0f, -5.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Racing("tentLong.fbx"), "Real 3D Drift Tent South", new Vector3(46f, 0f, -30.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.8f);
        PlaceScattered(root, Racing("flagGreen.fbx"), "Real 3D Drift Green Flag", new Vector3(12f, 0f, -15f), 5, 4.0f, 1.3f);
        PlaceScattered(root, Racing("flagRed.fbx"), "Real 3D Drift Red Flag", new Vector3(50f, 0f, -22f), 5, 4.0f, 1.3f);
    }

    static void BuildTronModels(GameObject root)
    {
        PlaceModel(root, Toy("gate.fbx"), "Real 3D Tron Gate West", new Vector3(23f, 0f, -10f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Toy("gate.fbx"), "Real 3D Tron Gate East", new Vector3(41f, 0f, -26f), Quaternion.Euler(0f, -145f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Toy("track-striped-wide-straight.fbx"), "Real 3D Tron Striped Track Display", new Vector3(32f, 0.06f, -18f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.6f);
        PlaceScattered(root, Toy("item-coin-silver.fbx"), "Real 3D Tron Light Node", new Vector3(32f, 0.35f, -18f), 12, 4.5f, 0.9f);
    }

    static void BuildLastOneLitModels(GameObject root)
    {
        PlaceModel(root, Racing("overheadRoundColored.fbx"), "Real 3D Last One Lit Overhead Rig", new Vector3(32f, 5.8f, -18f), Quaternion.identity, Vector3.one * 3.2f);
        PlaceModel(root, Racing("lightRedDouble.fbx"), "Real 3D Last One Lit Red Light A", new Vector3(10f, 0f, -7f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.8f);
        PlaceModel(root, Racing("lightRedDouble.fbx"), "Real 3D Last One Lit Red Light B", new Vector3(54f, 0f, -29f), Quaternion.Euler(0f, -135f, 0f), Vector3.one * 1.8f);
        PlaceScattered(root, Racing("lightColored.fbx"), "Real 3D Last One Lit Floor Light", new Vector3(32f, 0f, -18f), 8, 5.0f, 1.4f);
    }

    static void PlaceScattered(GameObject root, string path, string baseName, Vector3 center, int count, float radius, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * 137.5f * Mathf.Deg2Rad;
            float r = radius * (0.35f + 0.65f * ((i % 5) / 4f));
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            PlaceModel(root, path, $"{baseName} {i}", pos, Quaternion.Euler(0f, i * 41f, 0f), Vector3.one * scale);
        }
    }

    static GameObject PlaceModel(GameObject root, string path, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject asset = LoadModel(path);
        if (asset == null)
        {
            Debug.LogWarning($"[ZIKJ Real 3D] Missing model asset: {path}");
            return null;
        }

        GameObject instance = InstantiateModel(asset, root.transform);
        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = scale;
        RemoveColliders(instance);
        return instance;
    }

    static GameObject InstantiateModel(GameObject asset, Transform parent)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
            instance = Object.Instantiate(asset);

        instance.transform.SetParent(parent, true);
        return instance;
    }

    static GameObject LoadModel(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void FitModelToKart(GameObject model)
    {
        Bounds bounds = RendererBounds(model);
        if (bounds.size.x > bounds.size.z * 1.18f)
        {
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            bounds = RendererBounds(model);
        }

        Vector3 target = new(0.95f, 0.58f, 1.55f);
        float scale = Mathf.Min(
            target.x / Mathf.Max(0.01f, bounds.size.x),
            target.y / Mathf.Max(0.01f, bounds.size.y),
            target.z / Mathf.Max(0.01f, bounds.size.z));
        model.transform.localScale *= scale;

        bounds = RendererBounds(model);
        Vector3 offset = new(-bounds.center.x, -bounds.min.y + 0.03f, -bounds.center.z);
        model.transform.position += offset;
    }

    static Bounds RendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    static void RemoveColliders(GameObject go)
    {
        foreach (var collider in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(collider);
    }

    static string CarKit(string file) => CarKitFbx + file;
    static string Racing(string file) => RacingKitFbx + file;
    static string Toy(string file) => ToyCarKitFbx + file;
}
