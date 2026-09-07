using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VenueLauncherScene : MonoBehaviour
{
    public const string SceneName = "VenueLauncher";

    static readonly LaunchItem[] Items =
    {
        new("Infected", "InfectedArena", "zombie quarantine tag", new Color(0.15f, 0.86f, 0.62f)),
        new("Battle Royale", "BattleRoyaleArena", "war-zone elimination", new Color(1f, 0.34f, 0.28f)),
        new("Grand Prix", "GrandPrixArena", "color boosts and item boxes", new Color(0.24f, 0.66f, 1f)),
        new("Drift Arena", "DriftArena", "Moroccan tent drift", new Color(1f, 0.68f, 0.2f)),
        new("Tron Trails", "TronTrails", "light trails and lives", new Color(0.2f, 0.96f, 1f)),
        new("Last One Lit", "LastOneLit", "spotlights and survival", new Color(1f, 0.38f, 0.72f)),
        new("Projection Alignment", "ProjectionAlignment", "projectors, grid, tracking", new Color(0.82f, 0.96f, 0.42f))
    };

    public bool buildOnStart = true;

    void Start()
    {
        if (buildOnStart)
            BuildScene();
    }

    void Update()
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) ||
                Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
            {
                LoadScene(Items[i].sceneName);
            }
        }
    }

    [ContextMenu("Build Venue Launcher Scene")]
    public void BuildScene()
    {
        EnsureReturnHotkey();
        EnsureLighting();
        EnsurePreviewWorld();
        EnsureCamera();
        EnsureEventSystem();
        EnsureCanvas();
    }

    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Launcher scene target is not in Build Settings: {sceneName}");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    void EnsureReturnHotkey()
    {
        if (FindFirstObjectByType<VenueLauncherReturnHotkey>() != null)
            return;

        var go = new GameObject("Venue Launcher Return Hotkey");
        go.AddComponent<VenueLauncherReturnHotkey>();
    }

    void EnsureLighting()
    {
        if (FindFirstObjectByType<Light>() != null)
            return;

        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.86f;
        light.color = new Color(0.92f, 0.97f, 1f);
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(54f, -36f, 0f);
    }

    void EnsurePreviewWorld()
    {
        if (GameObject.Find("Launcher Preview World") != null)
            return;

        var root = new GameObject("Launcher Preview World");

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Launcher Preview Floor";
        ground.transform.SetParent(root.transform, true);
        ground.transform.position = new Vector3(32f, -0.04f, -18f);
        ground.transform.localScale = new Vector3(8.5f, 1f, 5.3f);
        ground.GetComponent<Renderer>().material = RuntimeMaterials.Make(new Color(0.026f, 0.031f, 0.038f));

        if (FindFirstObjectByType<WaypointCircuit>() == null)
        {
            var circuit = new GameObject("WaypointCircuit");
            circuit.transform.SetParent(root.transform, true);
            circuit.AddComponent<WaypointCircuit>();
        }

        var track = new GameObject("Launcher Preview Track");
        track.transform.SetParent(root.transform, true);
        var builder = track.AddComponent<TrackMeshBuilder>();
        builder.trackMaterial = RuntimeMaterials.Make(new Color(0.075f, 0.085f, 0.11f));
        if (!Application.isPlaying)
            builder.Build();

        CreatePreviewKarts(root.transform);
        CreateProjectionRibbon(root.transform);
    }

    void CreatePreviewKarts(Transform parent)
    {
        Color[] colors =
        {
            new(0.15f, 0.86f, 0.62f),
            new(1f, 0.34f, 0.28f),
            new(0.24f, 0.66f, 1f),
            new(1f, 0.68f, 0.2f),
            new(0.2f, 0.96f, 1f),
            new(1f, 0.38f, 0.72f)
        };

        for (int i = 0; i < colors.Length; i++)
        {
            var kart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kart.name = $"Launcher Preview Kart {i + 1}";
            kart.transform.SetParent(parent, true);
            kart.transform.position = WaypointCircuit.At(0.06f + i * 0.055f) + new Vector3(0f, 0.22f, i % 2 == 0 ? -0.85f : 0.85f);
            kart.transform.localScale = new Vector3(0.82f, 0.35f, 1.12f);
            kart.GetComponent<Renderer>().material = RuntimeMaterials.Emissive(colors[i], colors[i], 0.75f);
        }
    }

    void CreateProjectionRibbon(Transform parent)
    {
        var ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ribbon.name = "Launcher Projection Preview Strip";
        ribbon.transform.SetParent(parent, true);
        ribbon.transform.position = new Vector3(32f, 0.03f, -35.2f);
        ribbon.transform.localScale = new Vector3(64f, 0.035f, 1.1f);
        ribbon.GetComponent<Renderer>().material = RuntimeMaterials.Emissive(
            new Color(0.18f, 0.31f, 0.46f),
            new Color(0.26f, 0.68f, 1f),
            0.9f);
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

        cam.orthographic = true;
        cam.orthographicSize = 25f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.009f, 0.011f, 0.014f);
        cam.transform.position = new Vector3(32f, 42f, -46f);
        cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void EnsureCanvas()
    {
        if (GameObject.Find("Venue Launcher Canvas") != null)
            return;

        var canvasGo = new GameObject("Venue Launcher Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        CreatePanel(canvasGo.transform);
        CreateText(canvasGo.transform, "Launcher Title", "ZIKJ UNITY LAUNCHER", new Vector2(36f, -28f), new Vector2(760f, 52f), 34, TextAnchor.UpperLeft, Color.white);
        CreateText(canvasGo.transform, "Launcher Subtitle", "Choose a Unity scene. Number keys 1-7 launch directly. Esc returns here.", new Vector2(38f, -78f), new Vector2(840f, 32f), 18, TextAnchor.UpperLeft, new Color(0.78f, 0.88f, 0.96f));

        for (int i = 0; i < Items.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;
            Vector2 position = new Vector2(38f + column * 408f, -136f - row * 104f);
            CreateLaunchButton(canvasGo.transform, Items[i], i + 1, position);
        }
    }

    void CreatePanel(Transform parent)
    {
        var go = new GameObject("Launcher Panel");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 0f);
        rect.anchoredPosition = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.018f, 0.023f, 0.03f, 0.88f);
    }

    void CreateLaunchButton(Transform parent, LaunchItem item, int number, Vector2 position)
    {
        var go = new GameObject($"Launch {number} {item.title}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(372f, 82f);
        rect.anchoredPosition = position;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.078f, 0.09f, 0.11f, 0.96f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.16f, 0.19f, 1f);
        colors.pressedColor = new Color(0.18f, 0.21f, 0.25f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        string scene = item.sceneName;
        button.onClick.AddListener(() => LoadScene(scene));

        CreateAccent(go.transform, item.accent);
        CreateText(go.transform, "Number", number.ToString(), new Vector2(16f, -13f), new Vector2(32f, 42f), 28, TextAnchor.UpperLeft, item.accent);
        CreateText(go.transform, "Title", item.title.ToUpperInvariant(), new Vector2(58f, -12f), new Vector2(286f, 32f), 20, TextAnchor.UpperLeft, Color.white);
        CreateText(go.transform, "Subtitle", item.subtitle, new Vector2(58f, -46f), new Vector2(286f, 24f), 14, TextAnchor.UpperLeft, new Color(0.74f, 0.82f, 0.9f));
    }

    void CreateAccent(Transform parent, Color color)
    {
        var go = new GameObject("Accent");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(5f, 0f);
        rect.anchoredPosition = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = color;
    }

    Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var label = go.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = anchor;
        label.color = color;
        label.font = BuiltInFont();
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    public static string[] TargetSceneNames()
    {
        var names = new string[Items.Length];
        for (int i = 0; i < Items.Length; i++)
            names[i] = Items[i].sceneName;

        return names;
    }

    static Font BuiltInFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    struct LaunchItem
    {
        public readonly string title;
        public readonly string sceneName;
        public readonly string subtitle;
        public readonly Color accent;

        public LaunchItem(string title, string sceneName, string subtitle, Color accent)
        {
            this.title = title;
            this.sceneName = sceneName;
            this.subtitle = subtitle;
            this.accent = accent;
        }
    }
}

public class VenueLauncherReturnHotkey : MonoBehaviour
{
    static VenueLauncherReturnHotkey instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && SceneManager.GetActiveScene().name != VenueLauncherScene.SceneName)
            SceneManager.LoadScene(VenueLauncherScene.SceneName);
    }

    void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == VenueLauncherScene.SceneName)
            return;

        GUI.color = new Color(1f, 1f, 1f, 0.72f);
        GUI.Label(new Rect(Screen.width - 170f, 12f, 158f, 24f), "Esc: Unity launcher");
        GUI.color = Color.white;
    }
}
