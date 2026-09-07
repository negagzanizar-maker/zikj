using UnityEngine;
using UnityEngine.UI;

public class InfectedSceneBootstrap : MonoBehaviour
{
    [Header("Build")]
    public bool buildOnStart = true;
    [Range(2, 12)] public int kartCount = 6;
    public int playerKartIndex = 0;
    public GameManager.GameMode gameMode = GameManager.GameMode.Infected;

    [Header("Materials")]
    public Material trackMaterial;
    public Material groundMaterial;

    void Start()
    {
        if (buildOnStart)
            BuildScene();
    }

    [ContextMenu("Build Infected Scene")]
    public void BuildScene()
    {
        EnsureLight();
        EnsureGround();
        var circuit = EnsureCircuit();
        EnsureTrack();
        EnsureEnvironment();
        EnsureProjectedMap();
        EnsureCamera();
        EnsureTrackingInfrastructure();

        if (gameMode == GameManager.GameMode.BattleRoyale)
        {
            var hud = EnsureBattleRoyaleHud();
            EnsureGame(circuit, null, hud, null, null, null, null);
        }
        else if (gameMode == GameManager.GameMode.GrandPrix)
        {
            var hud = EnsureGrandPrixHud();
            EnsureGame(circuit, null, null, hud, null, null, null);
        }
        else if (gameMode == GameManager.GameMode.DriftArena)
        {
            var hud = EnsureDriftArenaHud();
            EnsureGame(circuit, null, null, null, hud, null, null);
        }
        else if (gameMode == GameManager.GameMode.TronTrails)
        {
            var hud = EnsureTronTrailsHud();
            EnsureGame(circuit, null, null, null, null, hud, null);
        }
        else if (gameMode == GameManager.GameMode.LastOneLit)
        {
            var hud = EnsureLastOneLitHud();
            EnsureGame(circuit, null, null, null, null, null, hud);
        }
        else
        {
            var hud = EnsureInfectedHud();
            EnsureGame(circuit, hud, null, null, null, null, null);
        }
    }

    void EnsureLight()
    {
        if (FindFirstObjectByType<Light>() != null) return;

        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.75f;
        light.color = new Color(0.88f, 0.94f, 1f);
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
    }

    void EnsureGround()
    {
        if (GameObject.Find("Ground") != null) return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(32f, -0.02f, -18f);
        ground.transform.localScale = new Vector3(8f, 1f, 5f);

        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = groundMaterial != null
                ? groundMaterial
                : RuntimeMaterials.Make(DefaultGroundColor());
    }

    WaypointCircuit EnsureCircuit()
    {
        var circuit = WaypointCircuit.I ?? FindFirstObjectByType<WaypointCircuit>();
        if (circuit != null) return circuit;

        var go = new GameObject("WaypointCircuit");
        return go.AddComponent<WaypointCircuit>();
    }

    void EnsureTrack()
    {
        var existing = FindFirstObjectByType<TrackMeshBuilder>();
        if (existing != null)
        {
            if (!Application.isPlaying)
                existing.Build();
            return;
        }

        var go = new GameObject("Track");
        var builder = go.AddComponent<TrackMeshBuilder>();
        builder.trackMaterial = trackMaterial != null
            ? trackMaterial
            : RuntimeMaterials.Make(DefaultTrackColor());

        if (!Application.isPlaying)
            builder.Build();
    }

    void EnsureEnvironment()
    {
        var theme = EnvironmentThemeForMode();
        var existing = FindFirstObjectByType<InfectedEnvironmentBuilder>();
        if (existing != null)
        {
            existing.theme = theme;
            existing.Build();
            return;
        }

        var go = new GameObject("Arena Scenery");
        var builder = go.AddComponent<InfectedEnvironmentBuilder>();
        builder.theme = theme;

        if (!Application.isPlaying)
            builder.Build();
    }

    void EnsureProjectedMap()
    {
        var theme = ProjectionThemeForMode();
        var existing = FindFirstObjectByType<ProjectedBattleMapBuilder>();
        if (existing != null)
        {
            existing.theme = theme;
            existing.Build();
            return;
        }

        var go = new GameObject("Projected Battle Race Map");
        var builder = go.AddComponent<ProjectedBattleMapBuilder>();
        builder.theme = theme;

        if (!Application.isPlaying)
            builder.Build();
    }

    InfectedEnvironmentBuilder.ArenaTheme EnvironmentThemeForMode()
    {
        return gameMode switch
        {
            GameManager.GameMode.Infected => InfectedEnvironmentBuilder.ArenaTheme.ZombieApocalypse,
            GameManager.GameMode.BattleRoyale => InfectedEnvironmentBuilder.ArenaTheme.WarZone,
            GameManager.GameMode.GrandPrix => InfectedEnvironmentBuilder.ArenaTheme.ColorfulKartArena,
            GameManager.GameMode.DriftArena => InfectedEnvironmentBuilder.ArenaTheme.MoroccanTent,
            GameManager.GameMode.LastOneLit => InfectedEnvironmentBuilder.ArenaTheme.Spotlight,
            _ => InfectedEnvironmentBuilder.ArenaTheme.NeonArena
        };
    }

    ProjectedBattleMapBuilder.ProjectionTheme ProjectionThemeForMode()
    {
        return gameMode switch
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

    Color DefaultGroundColor()
    {
        return gameMode switch
        {
            GameManager.GameMode.Infected => new Color(0.030f, 0.036f, 0.031f),
            GameManager.GameMode.BattleRoyale => new Color(0.105f, 0.094f, 0.072f),
            GameManager.GameMode.GrandPrix => new Color(0.034f, 0.082f, 0.16f),
            GameManager.GameMode.DriftArena => new Color(0.105f, 0.030f, 0.028f),
            GameManager.GameMode.LastOneLit => new Color(0.020f, 0.020f, 0.028f),
            _ => new Color(0.025f, 0.028f, 0.04f)
        };
    }

    Color DefaultTrackColor()
    {
        return gameMode switch
        {
            GameManager.GameMode.Infected => new Color(0.070f, 0.076f, 0.068f),
            GameManager.GameMode.BattleRoyale => new Color(0.13f, 0.12f, 0.095f),
            GameManager.GameMode.GrandPrix => new Color(0.055f, 0.12f, 0.22f),
            GameManager.GameMode.DriftArena => new Color(0.13f, 0.045f, 0.040f),
            GameManager.GameMode.LastOneLit => new Color(0.075f, 0.073f, 0.090f),
            _ => new Color(0.10f, 0.105f, 0.13f)
        };
    }

    void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }

        if (cam.GetComponent<CameraRig>() == null)
            cam.gameObject.AddComponent<CameraRig>();
    }

    void EnsureTrackingInfrastructure()
    {
        if (FindFirstObjectByType<TrackingReceiver>() == null)
        {
            var go = new GameObject("Tracking Receiver");
            go.AddComponent<TrackingReceiver>();
        }

        if (FindFirstObjectByType<TrackingInputManager>() == null)
        {
            var go = new GameObject("Tracking Input Manager");
            go.AddComponent<TrackingInputManager>();
        }

        if (FindFirstObjectByType<PerformanceMonitor>() == null)
        {
            var go = new GameObject("Performance Monitor");
            go.AddComponent<PerformanceMonitor>();
        }

        if (FindFirstObjectByType<TrackingDebugHUD>() == null)
        {
            var go = new GameObject("Tracking Debug HUD");
            var hud = go.AddComponent<TrackingDebugHUD>();
            hud.showHUD = false;
        }
    }

    InfectedHUD EnsureInfectedHud()
    {
        var existing = FindFirstObjectByType<InfectedHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Infected HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<InfectedHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.timerText = CreateText(canvasGo.transform, "Timer", "3:00", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.statusText = CreateText(canvasGo.transform, "Status", "CLEAN", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.infectedCountText = CreateText(canvasGo.transform, "Infected Count", "1 / 6 INFECTED", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    BattleRoyaleHUD EnsureBattleRoyaleHud()
    {
        var existing = FindFirstObjectByType<BattleRoyaleHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Battle Royale HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<BattleRoyaleHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.timerText = CreateText(canvasGo.transform, "Timer", "3:00", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.statusText = CreateText(canvasGo.transform, "Status", "HEARTS 3.0 / 3", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.aliveCountText = CreateText(canvasGo.transform, "Alive Count", "6 / 6 ALIVE", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | RAM TO DAMAGE | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    GrandPrixHUD EnsureGrandPrixHud()
    {
        var existing = FindFirstObjectByType<GrandPrixHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Grand Prix HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<GrandPrixHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.timerText = CreateText(canvasGo.transform, "Timer", "4:00", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.lapText = CreateText(canvasGo.transform, "Lap", "LAP 1 / 3", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.placeText = CreateText(canvasGo.transform, "Place", "1ST PLACE", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.itemText = CreateText(canvasGo.transform, "Item", "ITEM: EMPTY", new Vector2(24f, -142f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | SPACE ITEM | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    DriftArenaHUD EnsureDriftArenaHud()
    {
        var existing = FindFirstObjectByType<DriftArenaHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Drift Arena HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<DriftArenaHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.timerText = CreateText(canvasGo.transform, "Timer", "3:00", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.scoreText = CreateText(canvasGo.transform, "Score", "SCORE 0", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.comboText = CreateText(canvasGo.transform, "Combo", "COMBO x1", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.hotZoneText = CreateText(canvasGo.transform, "Hot Zone", "HOT ZONE SEEK IT", new Vector2(24f, -142f), 21, TextAnchor.UpperLeft);
        hud.rankText = CreateText(canvasGo.transform, "Rank", "1ST / 6", new Vector2(24f, -176f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | HOLD HARD TURNS TO DRIFT | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    TronTrailsHUD EnsureTronTrailsHud()
    {
        var existing = FindFirstObjectByType<TronTrailsHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Tron Trails HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<TronTrailsHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.aliveCountText = CreateText(canvasGo.transform, "Alive Count", "6 / 6 ALIVE", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.statusText = CreateText(canvasGo.transform, "Status", "ALIVE", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.livesText = CreateText(canvasGo.transform, "Lives", "LIVES |||", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | AVOID LIGHT TRAILS | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    LastOneLitHUD EnsureLastOneLitHud()
    {
        var existing = FindFirstObjectByType<LastOneLitHUD>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Last One Lit HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var hud = canvasGo.AddComponent<LastOneLitHUD>();
        hud.playerKartIndex = playerKartIndex;

        hud.timerText = CreateText(canvasGo.transform, "Timer", "3:00", new Vector2(24f, -24f), 34, TextAnchor.UpperLeft);
        hud.lightText = CreateText(canvasGo.transform, "Light", "YOUR LIGHT 100%", new Vector2(24f, -72f), 25, TextAnchor.UpperLeft);
        hud.aliveCountText = CreateText(canvasGo.transform, "Alive Count", "6 / 6 STILL LIT", new Vector2(24f, -108f), 21, TextAnchor.UpperLeft);
        hud.rankText = CreateText(canvasGo.transform, "Rank", "1ST BY LIGHT", new Vector2(24f, -142f), 21, TextAnchor.UpperLeft);
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD DRIVE | AVOID OTHER SPOTLIGHTS | C/V CAM | 1-7 VIEWS", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, out hud.resultPanel, out hud.resultText);
        return hud;
    }

    Text CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 anchoredPos,
        int fontSize,
        TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(520f, 44f);
        SetAnchor(rect, anchor);
        rect.anchoredPosition = anchoredPos;

        var label = go.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = anchor;
        label.color = Color.white;
        label.font = BuiltInFont();
        return label;
    }

    void CreateResultPanel(Transform parent, out GameObject panel, out Text resultText)
    {
        panel = new GameObject("Result Panel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 420f);

        var image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);

        resultText = CreateText(panel.transform, "Result Text", "RESULTS", Vector2.zero, 24, TextAnchor.MiddleCenter);
        var resultRect = resultText.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.offsetMin = new Vector2(28f, 28f);
        resultRect.offsetMax = new Vector2(-28f, -28f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
        resultText.verticalOverflow = VerticalWrapMode.Overflow;

        panel.SetActive(false);
    }

    void EnsureGame(
        WaypointCircuit circuit,
        InfectedHUD infectedHud,
        BattleRoyaleHUD battleHud,
        GrandPrixHUD grandPrixHud,
        DriftArenaHUD driftHud,
        TronTrailsHUD tronHud,
        LastOneLitHUD litHud)
    {
        if (FindFirstObjectByType<GameManager>() != null) return;

        var go = new GameObject("GameManager");
        var manager = go.AddComponent<GameManager>();
        manager.gameMode = gameMode;
        manager.circuit = circuit;
        manager.kartCount = kartCount;
        manager.playerKartIndex = playerKartIndex;

        if (gameMode == GameManager.GameMode.BattleRoyale)
        {
            var mode = go.AddComponent<BattleRoyaleMode>();
            manager.battleRoyaleMode = mode;

            if (battleHud != null)
                battleHud.playerKartIndex = playerKartIndex;
        }
        else if (gameMode == GameManager.GameMode.GrandPrix)
        {
            var mode = go.AddComponent<GrandPrixMode>();
            manager.grandPrixMode = mode;

            if (grandPrixHud != null)
                grandPrixHud.playerKartIndex = playerKartIndex;
        }
        else if (gameMode == GameManager.GameMode.DriftArena)
        {
            var mode = go.AddComponent<DriftArenaMode>();
            manager.driftArenaMode = mode;

            if (driftHud != null)
                driftHud.playerKartIndex = playerKartIndex;
        }
        else if (gameMode == GameManager.GameMode.TronTrails)
        {
            var mode = go.AddComponent<TronTrailsMode>();
            manager.tronTrailsMode = mode;

            if (tronHud != null)
                tronHud.playerKartIndex = playerKartIndex;
        }
        else if (gameMode == GameManager.GameMode.LastOneLit)
        {
            var mode = go.AddComponent<LastOneLitMode>();
            manager.lastOneLitMode = mode;

            if (litHud != null)
                litHud.playerKartIndex = playerKartIndex;
        }
        else
        {
            var mode = go.AddComponent<InfectedMode>();
            manager.infectedMode = mode;

            if (infectedHud != null)
                infectedHud.playerKartIndex = playerKartIndex;
        }
    }

    static void SetAnchor(RectTransform rect, TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.LowerLeft:
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                break;
            default:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                break;
        }
    }

    static Font BuiltInFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
