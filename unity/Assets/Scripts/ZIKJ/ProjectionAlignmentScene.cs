using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ProjectionAlignmentScene : MonoBehaviour
{
    const float WorldWidthPixels = 1280f;
    const float WorldHeightPixels = 720f;
    const float OverlayY = 0.018f;
    const float LineY = 0.045f;

    static readonly Rect[] ProjectorRects =
    {
        new(0f, 0f, 720f, 400f),
        new(560f, 0f, 720f, 400f),
        new(0f, 320f, 720f, 400f),
        new(560f, 320f, 720f, 400f)
    };

    static readonly Color[] ProjectorColors =
    {
        new(0.15f, 0.82f, 1f, 0.28f),
        new(1f, 0.78f, 0.18f, 0.28f),
        new(0.35f, 1f, 0.42f, 0.28f),
        new(1f, 0.32f, 0.52f, 0.28f)
    };

    public bool buildOnStart = true;
    public Material trackMaterial;
    public Material groundMaterial;

    public int ProjectorCoverageCount { get; private set; }
    public int GridLineCount { get; private set; }
    public int AlignmentPointCount { get; private set; }
    public int TrackingMarkerPreviewCount { get; private set; }

    void Start()
    {
        if (buildOnStart)
            BuildScene();
    }

    [ContextMenu("Build Projection Alignment Scene")]
    public void BuildScene()
    {
        EnsureLighting();
        EnsureGround();
        EnsureCircuit();
        EnsureTrack();
        EnsureProjectionCoverage();
        EnsureAlignmentGrid();
        EnsureTrackingMarkerPreviews();
        EnsureCamera();
        EnsureTrackingInfrastructure();
        EnsureHud();
    }

    void EnsureLighting()
    {
        if (FindFirstObjectByType<Light>() != null)
            return;

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.82f;
        light.color = new Color(0.9f, 0.96f, 1f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(58f, -34f, 0f);
    }

    void EnsureGround()
    {
        if (GameObject.Find("Projection Surface") != null)
            return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Projection Surface";
        ground.transform.position = PixelToWorld(WorldWidthPixels * 0.5f, WorldHeightPixels * 0.5f, -0.025f);
        ground.transform.localScale = new Vector3(
            WorldWidthPixels * WaypointCircuit.Scale / 10f,
            1f,
            WorldHeightPixels * WaypointCircuit.Scale / 10f);

        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = groundMaterial != null
                ? groundMaterial
                : RuntimeMaterials.Make(new Color(0.032f, 0.038f, 0.045f));
        }
    }

    void EnsureCircuit()
    {
        if (FindFirstObjectByType<WaypointCircuit>() != null)
            return;

        new GameObject("WaypointCircuit").AddComponent<WaypointCircuit>();
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

        var trackGo = new GameObject("Projected Track");
        var builder = trackGo.AddComponent<TrackMeshBuilder>();
        builder.trackMaterial = trackMaterial != null
            ? trackMaterial
            : RuntimeMaterials.Make(new Color(0.075f, 0.085f, 0.105f));

        if (!Application.isPlaying)
            builder.Build();
    }

    void EnsureProjectionCoverage()
    {
        var existing = GameObject.Find("Projector Coverage");
        if (existing != null)
        {
            ProjectorCoverageCount = CountChildrenWithPrefix(existing.transform, "Projector Coverage P");
            return;
        }

        var parent = new GameObject("Projector Coverage");

        for (int i = 0; i < ProjectorRects.Length; i++)
        {
            var coverage = CreateCoverageQuad($"Projector Coverage P{i}", ProjectorRects[i], ProjectorColors[i]);
            coverage.transform.SetParent(parent.transform, true);
            CreateWorldLabel(
                parent.transform,
                $"Projector Label P{i}",
                $"P{i}",
                PixelToWorld(ProjectorRects[i].center.x, ProjectorRects[i].center.y, 0.12f),
                ProjectorColors[i].WithAlpha(1f));
        }

        CreateCoverageQuad("Projector Overlap Vertical", new Rect(560f, 0f, 160f, 720f), new Color(1f, 1f, 1f, 0.16f))
            .transform.SetParent(parent.transform, true);
        CreateCoverageQuad("Projector Overlap Horizontal", new Rect(0f, 320f, 1280f, 80f), new Color(1f, 1f, 1f, 0.16f))
            .transform.SetParent(parent.transform, true);

        ProjectorCoverageCount = ProjectorRects.Length;
    }

    void EnsureAlignmentGrid()
    {
        var existing = GameObject.Find("Alignment Grid");
        if (existing != null)
        {
            GridLineCount = existing.GetComponentsInChildren<LineRenderer>(true).Length;
            AlignmentPointCount = CountChildrenWithPrefix(existing.transform, "Alignment Point");
            return;
        }

        var parent = new GameObject("Alignment Grid");
        var lineMaterial = MakeLineMaterial(new Color(0.78f, 0.9f, 1f, 0.72f));
        var majorMaterial = MakeLineMaterial(new Color(1f, 1f, 1f, 0.92f));

        GridLineCount = 0;
        for (float x = 0f; x <= WorldWidthPixels + 0.1f; x += 160f)
        {
            bool major = Mathf.Approximately(x, 0f) ||
                Mathf.Approximately(x, WorldWidthPixels) ||
                Mathf.Approximately(x, WorldWidthPixels * 0.5f);
            CreateLine(parent.transform, $"Grid V {x:0}", PixelToWorld(x, 0f, LineY), PixelToWorld(x, WorldHeightPixels, LineY), major ? majorMaterial : lineMaterial, major ? 0.065f : 0.035f);
            GridLineCount++;
        }

        for (float y = 0f; y <= WorldHeightPixels + 0.1f; y += 90f)
        {
            bool major = Mathf.Approximately(y, 0f) ||
                Mathf.Approximately(y, WorldHeightPixels) ||
                Mathf.Approximately(y, WorldHeightPixels * 0.5f);
            CreateLine(parent.transform, $"Grid H {y:0}", PixelToWorld(0f, y, LineY), PixelToWorld(WorldWidthPixels, y, LineY), major ? majorMaterial : lineMaterial, major ? 0.065f : 0.035f);
            GridLineCount++;
        }

        var pointParent = new GameObject("Alignment Points");
        pointParent.transform.SetParent(parent.transform, true);

        AlignmentPointCount = 0;
        for (float x = 0f; x <= WorldWidthPixels + 0.1f; x += 160f)
        {
            for (float y = 0f; y <= WorldHeightPixels + 0.1f; y += 90f)
            {
                CreateAlignmentPoint(pointParent.transform, x, y);
                AlignmentPointCount++;
            }
        }
    }

    void EnsureTrackingMarkerPreviews()
    {
        var existing = GameObject.Find("Tracking Marker Previews");
        if (existing != null)
        {
            TrackingMarkerPreviewCount = CountChildrenWithPrefix(existing.transform, "Tracking Marker Preview");
            return;
        }

        var parent = new GameObject("Tracking Marker Previews");
        var markerMaterial = RuntimeMaterials.Emissive(new Color(0.96f, 0.98f, 1f), new Color(0.3f, 0.82f, 1f), 1.2f);

        TrackingMarkerPreviewCount = 0;
        for (int i = 0; i < 6; i++)
        {
            Vector3 basePos = WaypointCircuit.At(0.015f + i * 0.018f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Tracking Marker Preview {i}";
            marker.transform.SetParent(parent.transform, true);
            marker.transform.position = basePos + new Vector3(0f, 0.18f, i % 2 == 0 ? -0.8f : 0.8f);
            marker.transform.localScale = new Vector3(0.62f, 0.045f, 0.62f);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = markerMaterial;

            CreateWorldLabel(parent.transform, $"Tracking Marker Label {i}", i.ToString(), marker.transform.position + new Vector3(0f, 0.16f, 0f), Color.black, 0.42f);
            TrackingMarkerPreviewCount++;
        }
    }

    void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cam = cameraGo.AddComponent<Camera>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 22.5f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 90f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.012f, 0.014f, 0.018f);
        cam.transform.position = PixelToWorld(WorldWidthPixels * 0.5f, WorldHeightPixels * 0.5f, 54f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void EnsureTrackingInfrastructure()
    {
        if (FindFirstObjectByType<TrackingReceiver>() == null)
        {
            var receiverGo = new GameObject("Tracking Receiver");
            var receiver = receiverGo.AddComponent<TrackingReceiver>();
            receiver.listenPort = 5555;
            receiver.autoStart = true;
        }

        if (FindFirstObjectByType<TrackingInputManager>() == null)
        {
            var inputGo = new GameObject("Tracking Input Manager");
            var input = inputGo.AddComponent<TrackingInputManager>();
            input.sourceUsesPrototypePixels = true;
            input.useInterpolation = true;
        }

        if (FindFirstObjectByType<PerformanceMonitor>() == null)
            new GameObject("Performance Monitor").AddComponent<PerformanceMonitor>();

        if (FindFirstObjectByType<TrackingDebugHUD>() == null)
        {
            var hudGo = new GameObject("Tracking Debug HUD");
            var hud = hudGo.AddComponent<TrackingDebugHUD>();
            hud.showHUD = true;
            hud.hudPosition = new Vector2(18f, 116f);
        }
    }

    void EnsureHud()
    {
        if (GameObject.Find("Projection Alignment HUD") != null)
            return;

        var canvasGo = new GameObject("Projection Alignment HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateText(canvasGo.transform, "Title", "ZIKJ PROJECTION ALIGNMENT", new Vector2(22f, -20f), 30, TextAnchor.UpperLeft, Color.white, new Vector2(760f, 40f));
        CreateText(canvasGo.transform, "Scene Status", "1280 x 720 WORLD | 4 PROJECTORS | UDP :5555", new Vector2(22f, -60f), 18, TextAnchor.UpperLeft, new Color(0.74f, 0.86f, 1f), new Vector2(720f, 32f));
        CreateText(canvasGo.transform, "Grid Status", $"{GridLineCount} GRID LINES | {AlignmentPointCount} ALIGNMENT POINTS", new Vector2(22f, -88f), 16, TextAnchor.UpperLeft, new Color(0.86f, 0.96f, 0.82f), new Vector2(720f, 30f));
    }

    GameObject CreateCoverageQuad(string name, Rect rect, Color color)
    {
        var go = new GameObject(name);
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();

        Vector3 topLeft = PixelToWorld(rect.xMin, rect.yMin, OverlayY);
        Vector3 topRight = PixelToWorld(rect.xMax, rect.yMin, OverlayY);
        Vector3 bottomRight = PixelToWorld(rect.xMax, rect.yMax, OverlayY);
        Vector3 bottomLeft = PixelToWorld(rect.xMin, rect.yMax, OverlayY);

        var mesh = new Mesh { name = name + " Mesh" };
        mesh.SetVertices(new[] { topLeft, topRight, bottomRight, bottomLeft });
        mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.SetUVs(0, new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = MakeTransparentMaterial(color);
        return go;
    }

    void CreateLine(Transform parent, string name, Vector3 start, Vector3 end, Material material, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.widthMultiplier = width;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = material;
    }

    void CreateAlignmentPoint(Transform parent, float x, float y)
    {
        var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = $"Alignment Point {x:0} {y:0}";
        point.transform.SetParent(parent, true);
        point.transform.position = PixelToWorld(x, y, 0.11f);
        point.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);

        var renderer = point.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = RuntimeMaterials.Emissive(new Color(0.92f, 0.98f, 1f), new Color(0.35f, 0.85f, 1f), 1.4f);
    }

    void CreateWorldLabel(Transform parent, string name, string text, Vector3 position, Color color, float size = 0.9f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 64;
        label.characterSize = size * 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
        label.font = BuiltInFont();
    }

    Text CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 anchoredPos,
        int fontSize,
        TextAnchor anchor,
        Color color,
        Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;

        var label = go.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = anchor;
        label.color = color;
        label.font = BuiltInFont();
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    static Vector3 PixelToWorld(float x, float y, float height)
    {
        return new Vector3(x * WaypointCircuit.Scale, height, -y * WaypointCircuit.Scale);
    }

    static Material MakeTransparentMaterial(Color color)
    {
        var material = RuntimeMaterials.Make(color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    static Material MakeLineMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Standard");

        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);
        return material;
    }

    static int CountChildrenWithPrefix(Transform root, string prefix)
    {
        int count = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name.StartsWith(prefix))
                count++;
        }

        return count;
    }

    static Font BuiltInFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}

static class ProjectionAlignmentColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
