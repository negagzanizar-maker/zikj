using UnityEngine;
using UnityEngine.UI;

public class InfectedSceneBootstrap : MonoBehaviour
{
    [Header("Build")]
    public bool buildOnStart = true;
    [Range(2, 12)] public int kartCount = 6;
    public int playerKartIndex = 0;

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
        EnsureCamera();
        var hud = EnsureHud();
        EnsureGame(circuit, hud);
    }

    void EnsureLight()
    {
        if (FindFirstObjectByType<Light>() != null) return;

        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
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
                : RuntimeMaterials.Make(new Color(0.025f, 0.028f, 0.04f));
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
        if (FindFirstObjectByType<TrackMeshBuilder>() != null) return;

        var go = new GameObject("Track");
        var builder = go.AddComponent<TrackMeshBuilder>();
        builder.trackMaterial = trackMaterial != null
            ? trackMaterial
            : RuntimeMaterials.Make(new Color(0.10f, 0.105f, 0.13f));
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

    InfectedHUD EnsureHud()
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
        hud.helpText = CreateText(canvasGo.transform, "Help", "WASD / ARROWS - DRIVE", new Vector2(24f, 24f), 18, TextAnchor.LowerLeft);

        CreateResultPanel(canvasGo.transform, hud);
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

    void CreateResultPanel(Transform parent, InfectedHUD hud)
    {
        var panel = new GameObject("Result Panel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 420f);

        var image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);

        var result = CreateText(panel.transform, "Result Text", "RESULTS", Vector2.zero, 24, TextAnchor.MiddleCenter);
        var resultRect = result.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.offsetMin = new Vector2(28f, 28f);
        resultRect.offsetMax = new Vector2(-28f, -28f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        result.horizontalOverflow = HorizontalWrapMode.Wrap;
        result.verticalOverflow = VerticalWrapMode.Overflow;

        hud.resultPanel = panel;
        hud.resultText = result;
        panel.SetActive(false);
    }

    void EnsureGame(WaypointCircuit circuit, InfectedHUD hud)
    {
        if (FindFirstObjectByType<GameManager>() != null) return;

        var go = new GameObject("GameManager");
        var mode = go.AddComponent<InfectedMode>();
        var manager = go.AddComponent<GameManager>();
        manager.circuit = circuit;
        manager.infectedMode = mode;
        manager.kartCount = kartCount;
        manager.playerKartIndex = playerKartIndex;

        if (hud != null)
            hud.playerKartIndex = playerKartIndex;
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
