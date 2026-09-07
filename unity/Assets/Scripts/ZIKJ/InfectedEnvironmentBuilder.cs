using UnityEngine;
using UnityEngine.Rendering;

public class InfectedEnvironmentBuilder : MonoBehaviour
{
    public enum ArenaTheme
    {
        NeonArena,
        MoroccanTent,
        WarZone,
        ZombieApocalypse,
        ColorfulKartArena,
        Spotlight
    }

    [Range(24, 160)] public int barrierSegments = 96;
    public ArenaTheme theme = ArenaTheme.NeonArena;

    Material floorMat;
    Material wallMat;
    Material standMat;
    Material railRedMat;
    Material railWhiteMat;
    Material neonCyanMat;
    Material neonGreenMat;
    Material darkMat;
    Material yellowMat;
    Material creamMat;
    Material sandMat;
    Material oliveMat;
    Material concreteMat;
    Material woodMat;
    Material toxicMat;

    void Start()
    {
        Build();
    }

    [ContextMenu("Build Arena Scenery")]
    public void Build()
    {
        CreateMaterials();
        ConfigureLighting();
        ClearChildren();

        BuildVenueFloor();
        BuildPerimeterWalls();
        BuildGrandstands();
        BuildTrackBarriers();
        BuildProjectionStrips();
        BuildStartGate();
        BuildScoreboard();
        BuildLightTowers();
        BuildProjectors();
        BuildVenueDetails();
        BuildThemeDetails();
    }

    void CreateMaterials()
    {
        Color floor = new(0.035f, 0.04f, 0.052f);
        Color wall = new(0.085f, 0.095f, 0.115f);
        Color stand = new(0.12f, 0.13f, 0.155f);
        Color red = new(0.95f, 0.06f, 0.09f);
        Color white = new(0.86f, 0.88f, 0.82f);
        Color cyan = new(0.08f, 0.75f, 1f);
        Color green = new(0.1f, 1f, 0.4f);

        switch (theme)
        {
            case ArenaTheme.MoroccanTent:
                floor = new Color(0.115f, 0.038f, 0.035f);
                wall = new Color(0.18f, 0.028f, 0.026f);
                stand = new Color(0.12f, 0.19f, 0.095f);
                red = new Color(0.78f, 0.02f, 0.035f);
                white = new Color(0.92f, 0.82f, 0.58f);
                cyan = new Color(0.04f, 0.52f, 0.22f);
                green = new Color(0.02f, 0.58f, 0.22f);
                break;
            case ArenaTheme.WarZone:
                floor = new Color(0.12f, 0.11f, 0.085f);
                wall = new Color(0.09f, 0.095f, 0.082f);
                stand = new Color(0.14f, 0.13f, 0.1f);
                red = new Color(0.82f, 0.16f, 0.08f);
                white = new Color(0.58f, 0.55f, 0.45f);
                cyan = new Color(0.95f, 0.48f, 0.12f);
                green = new Color(0.24f, 0.34f, 0.18f);
                break;
            case ArenaTheme.ZombieApocalypse:
                floor = new Color(0.045f, 0.048f, 0.044f);
                wall = new Color(0.075f, 0.07f, 0.062f);
                stand = new Color(0.055f, 0.063f, 0.058f);
                red = new Color(0.55f, 0.045f, 0.035f);
                white = new Color(0.45f, 0.48f, 0.42f);
                cyan = new Color(0.25f, 0.78f, 0.22f);
                green = new Color(0.35f, 0.9f, 0.18f);
                break;
            case ArenaTheme.ColorfulKartArena:
                floor = new Color(0.05f, 0.10f, 0.18f);
                wall = new Color(0.10f, 0.11f, 0.22f);
                stand = new Color(0.11f, 0.16f, 0.30f);
                red = new Color(1f, 0.16f, 0.25f);
                white = new Color(0.96f, 0.92f, 0.72f);
                cyan = new Color(0.08f, 0.78f, 1f);
                green = new Color(0.35f, 1f, 0.32f);
                break;
            case ArenaTheme.Spotlight:
                floor = new Color(0.025f, 0.025f, 0.032f);
                wall = new Color(0.035f, 0.035f, 0.045f);
                stand = new Color(0.055f, 0.055f, 0.072f);
                red = new Color(0.9f, 0.12f, 0.35f);
                white = new Color(0.9f, 0.88f, 0.78f);
                cyan = new Color(1f, 0.9f, 0.3f);
                green = new Color(0.9f, 0.78f, 0.2f);
                break;
        }

        floorMat = RuntimeMaterials.Make(floor);
        wallMat = RuntimeMaterials.Make(wall);
        standMat = RuntimeMaterials.Make(stand);
        railRedMat = RuntimeMaterials.Make(red);
        railWhiteMat = RuntimeMaterials.Make(white);
        neonCyanMat = RuntimeMaterials.Emissive(cyan, cyan, 2.4f);
        neonGreenMat = RuntimeMaterials.Emissive(green, green, 2.2f);
        darkMat = RuntimeMaterials.Make(new Color(0.008f, 0.01f, 0.014f));
        yellowMat = RuntimeMaterials.Emissive(new Color(1f, 0.74f, 0.16f), new Color(1f, 0.55f, 0.06f), 1.6f);
        creamMat = RuntimeMaterials.Make(new Color(0.82f, 0.73f, 0.52f));
        sandMat = RuntimeMaterials.Make(new Color(0.45f, 0.39f, 0.24f));
        oliveMat = RuntimeMaterials.Make(new Color(0.18f, 0.25f, 0.13f));
        concreteMat = RuntimeMaterials.Make(new Color(0.24f, 0.25f, 0.23f));
        woodMat = RuntimeMaterials.Make(new Color(0.32f, 0.18f, 0.08f));
        toxicMat = RuntimeMaterials.Emissive(new Color(0.25f, 0.95f, 0.16f), new Color(0.25f, 1f, 0.12f), 1.8f);
    }

    void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.018f, 0.022f, 0.032f);
        RenderSettings.fogDensity = 0.018f;

        if (theme == ArenaTheme.MoroccanTent)
        {
            RenderSettings.ambientLight = new Color(0.34f, 0.23f, 0.16f);
            RenderSettings.fogColor = new Color(0.13f, 0.035f, 0.03f);
            RenderSettings.fogDensity = 0.01f;
        }
        else if (theme == ArenaTheme.WarZone)
        {
            RenderSettings.ambientLight = new Color(0.25f, 0.24f, 0.18f);
            RenderSettings.fogColor = new Color(0.16f, 0.13f, 0.08f);
            RenderSettings.fogDensity = 0.026f;
        }
        else if (theme == ArenaTheme.ZombieApocalypse)
        {
            RenderSettings.ambientLight = new Color(0.14f, 0.18f, 0.13f);
            RenderSettings.fogColor = new Color(0.035f, 0.072f, 0.035f);
            RenderSettings.fogDensity = 0.034f;
        }
        else if (theme == ArenaTheme.ColorfulKartArena)
        {
            RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.44f);
            RenderSettings.fogColor = new Color(0.025f, 0.055f, 0.10f);
            RenderSettings.fogDensity = 0.009f;
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyUnityObject(transform.GetChild(i).gameObject);
    }

    void BuildVenueFloor()
    {
        Cube("Venue Floor", new Vector3(32f, -0.10f, -18f), new Vector3(62f, 0.10f, 42f), Quaternion.identity, floorMat);
        Cube("Infield Platform", new Vector3(32f, 0.005f, -18f), new Vector3(23f, 0.04f, 12f), Quaternion.identity, RuntimeMaterials.Make(new Color(0.045f, 0.05f, 0.064f)));

        for (int i = 0; i < 8; i++)
        {
            float x = 14f + i * 5.1f;
            Cube($"Floor Cable {i}", new Vector3(x, 0.025f, -18f), new Vector3(0.08f, 0.025f, 12f), Quaternion.identity, darkMat);
        }
    }

    void BuildPerimeterWalls()
    {
        Cube("North Wall", new Vector3(32f, 1.15f, 1.7f), new Vector3(60f, 2.3f, 0.7f), Quaternion.identity, wallMat);
        Cube("South Wall", new Vector3(32f, 0.75f, -37.7f), new Vector3(60f, 1.5f, 0.7f), Quaternion.identity, wallMat);
        Cube("West Wall", new Vector3(1.7f, 1.05f, -18f), new Vector3(0.7f, 2.1f, 39f), Quaternion.identity, wallMat);
        Cube("East Wall", new Vector3(62.3f, 1.05f, -18f), new Vector3(0.7f, 2.1f, 39f), Quaternion.identity, wallMat);

        Cube("North Neon Rail", new Vector3(32f, 2.45f, 1.28f), new Vector3(58f, 0.08f, 0.08f), Quaternion.identity, neonCyanMat);
        Cube("West Neon Rail", new Vector3(2.13f, 2.25f, -18f), new Vector3(0.08f, 0.08f, 37f), Quaternion.identity, neonGreenMat);
        Cube("East Neon Rail", new Vector3(61.87f, 2.25f, -18f), new Vector3(0.08f, 0.08f, 37f), Quaternion.identity, neonGreenMat);
    }

    void BuildGrandstands()
    {
        for (int row = 0; row < 4; row++)
        {
            float y = 0.35f + row * 0.34f;
            float zNorth = 3.1f + row * 0.72f;
            float zSouth = -39.2f - row * 0.72f;
            Vector3 scale = new Vector3(50f - row * 2f, 0.36f, 0.56f);

            Cube($"North Stand Row {row}", new Vector3(32f, y, zNorth), scale, Quaternion.identity, standMat);
            Cube($"South Stand Row {row}", new Vector3(32f, y, zSouth), scale, Quaternion.identity, standMat);
        }

        for (int i = 0; i < 18; i++)
        {
            float x = 8f + i * 2.8f;
            Material mat = i % 3 == 0 ? neonCyanMat : (i % 3 == 1 ? yellowMat : railRedMat);
            Cube($"North Seat Glow {i}", new Vector3(x, 1.9f, 3.05f), new Vector3(0.8f, 0.15f, 0.12f), Quaternion.identity, mat);
        }
    }

    void BuildTrackBarriers()
    {
        int steps = Mathf.Max(24, barrierSegments);

        for (int i = 0; i < steps; i++)
        {
            float t0 = (float)i / steps;
            float t1 = (float)(i + 1) / steps;
            Vector3 p0 = WaypointCircuit.At(t0);
            Vector3 p1 = WaypointCircuit.At(t1);
            Vector3 forward = (p1 - p0).normalized;
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            Material mat = i % 2 == 0 ? railRedMat : railWhiteMat;

            BuildBarrierSegment($"Outer Barrier {i}", p0 + right * (WaypointCircuit.HalfWidth + 0.25f), p1 + right * (WaypointCircuit.HalfWidth + 0.25f), mat);
            BuildBarrierSegment($"Inner Barrier {i}", p0 - right * (WaypointCircuit.HalfWidth + 0.25f), p1 - right * (WaypointCircuit.HalfWidth + 0.25f), mat);
        }
    }

    void BuildBarrierSegment(string name, Vector3 a, Vector3 b, Material mat)
    {
        Vector3 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.01f) return;

        Vector3 pos = (a + b) * 0.5f + Vector3.up * 0.32f;
        Quaternion rot = Quaternion.LookRotation(delta.normalized, Vector3.up);
        Cube(name, pos, new Vector3(0.26f, 0.64f, length + 0.05f), rot, mat);
    }

    void BuildProjectionStrips()
    {
        int steps = 64;

        for (int i = 0; i < steps; i += 2)
        {
            Vector3 p0 = WaypointCircuit.At((float)i / steps);
            Vector3 p1 = WaypointCircuit.At((float)(i + 1) / steps);
            Vector3 delta = p1 - p0;
            Quaternion rot = Quaternion.LookRotation(delta.normalized, Vector3.up);
            Cube($"Projected Racing Line {i}", (p0 + p1) * 0.5f + Vector3.up * 0.04f, new Vector3(0.12f, 0.025f, delta.magnitude), rot, neonCyanMat);
        }
    }

    void BuildStartGate()
    {
        Vector3 center = new Vector3(18.2f, 0f, WaypointCircuit.TopZ);
        Cube("Start Gate Left Post", center + new Vector3(0f, 2f, -4.4f), new Vector3(0.45f, 4f, 0.45f), Quaternion.identity, darkMat);
        Cube("Start Gate Right Post", center + new Vector3(0f, 2f, 4.4f), new Vector3(0.45f, 4f, 0.45f), Quaternion.identity, darkMat);
        Cube("Start Gate Banner", center + new Vector3(0f, 4.15f, 0f), new Vector3(0.7f, 1.05f, 9.4f), Quaternion.identity, RuntimeMaterials.Emissive(new Color(0.04f, 0.07f, 0.09f), new Color(0.05f, 0.4f, 0.7f), 1.2f));
        Cube("Start Line Glow", center + new Vector3(0f, 0.045f, 0f), new Vector3(0.18f, 0.04f, 7.2f), Quaternion.identity, yellowMat);

        Label("Start Gate Label", "ZIKJ", center + new Vector3(-0.42f, 4.18f, 0f), Quaternion.Euler(0f, -90f, 0f), 1.1f, Color.white);
    }

    void BuildScoreboard()
    {
        Vector3 pos = new Vector3(32f, 4.5f, 1.25f);
        Cube("Main Scoreboard", pos, new Vector3(14f, 3f, 0.45f), Quaternion.identity, RuntimeMaterials.Emissive(new Color(0.01f, 0.012f, 0.02f), new Color(0.03f, 0.12f, 0.25f), 1.8f));
        Label("Scoreboard Text", ThemeTitle(), pos + new Vector3(0f, 0.05f, -0.26f), Quaternion.Euler(0f, 180f, 0f), 0.56f, ThemeAccentColor());
    }

    void BuildLightTowers()
    {
        Vector3[] positions =
        {
            new Vector3(6f, 0f, -4f),
            new Vector3(58f, 0f, -4f),
            new Vector3(6f, 0f, -34f),
            new Vector3(58f, 0f, -34f),
        };

        foreach (Vector3 pos in positions)
        {
            Cylinder("Light Tower Pole", pos + Vector3.up * 3.1f, new Vector3(0.16f, 3.1f, 0.16f), Quaternion.identity, darkMat);
            Cube("Light Tower Head", pos + Vector3.up * 6.35f, new Vector3(1.3f, 0.5f, 0.8f), Quaternion.identity, yellowMat);

            var lightGo = new GameObject("Arena Spotlight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.position = pos + Vector3.up * 6.1f;
            lightGo.transform.rotation = Quaternion.LookRotation(new Vector3(32f, 0f, -18f) - lightGo.transform.position, Vector3.up);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 50f;
            light.spotAngle = 62f;
            light.intensity = 3.5f;
            light.color = new Color(0.82f, 0.93f, 1f);
        }
    }

    void BuildProjectors()
    {
        for (int i = 0; i < 6; i++)
        {
            float x = 10f + i * 8.8f;
            Cube($"Ceiling Projector {i}", new Vector3(x, 5.8f, -35.6f), new Vector3(1f, 0.35f, 0.75f), Quaternion.Euler(14f, 0f, 0f), darkMat);
            Cube($"Projector Beam {i}", new Vector3(x, 2.9f, -29.8f), new Vector3(0.12f, 0.03f, 5.8f), Quaternion.Euler(18f, 0f, 0f), neonCyanMat);
        }
    }

    void BuildVenueDetails()
    {
        for (int i = 0; i < 10; i++)
        {
            float x = 7f + i * 5.6f;
            float height = 1.2f + (i % 3) * 0.35f;
            Cube($"Service Cabinet {i}", new Vector3(x, height * 0.5f, -36.2f), new Vector3(1.1f, height, 0.55f), Quaternion.identity, i % 2 == 0 ? wallMat : darkMat);
        }

        Cube("Operator Booth", new Vector3(55.5f, 1.55f, -31.5f), new Vector3(5.2f, 3.1f, 3.2f), Quaternion.identity, RuntimeMaterials.Make(new Color(0.07f, 0.08f, 0.1f)));
        Cube("Operator Booth Window", new Vector3(55.5f, 2.05f, -29.86f), new Vector3(4.3f, 1.2f, 0.08f), Quaternion.identity, RuntimeMaterials.Emissive(new Color(0.05f, 0.16f, 0.24f), new Color(0.04f, 0.55f, 0.9f), 1.8f));
    }

    void BuildThemeDetails()
    {
        switch (theme)
        {
            case ArenaTheme.MoroccanTent:
                BuildMoroccanTentTheme();
                break;
            case ArenaTheme.WarZone:
                BuildWarZoneTheme();
                break;
            case ArenaTheme.ZombieApocalypse:
                BuildZombieApocalypseTheme();
                break;
            case ArenaTheme.ColorfulKartArena:
                BuildColorfulKartTheme();
                break;
            case ArenaTheme.Spotlight:
                BuildSpotlightTheme();
                break;
        }
    }

    void BuildColorfulKartTheme()
    {
        Material pink = RuntimeMaterials.Emissive(new Color(1f, 0.20f, 0.68f), new Color(1f, 0.14f, 0.62f), 1.8f);
        Material blue = RuntimeMaterials.Emissive(new Color(0.08f, 0.55f, 1f), new Color(0.05f, 0.65f, 1f), 2.1f);
        Material green = RuntimeMaterials.Emissive(new Color(0.28f, 1f, 0.30f), new Color(0.16f, 1f, 0.20f), 1.8f);
        Material orange = RuntimeMaterials.Emissive(new Color(1f, 0.50f, 0.12f), new Color(1f, 0.35f, 0.05f), 1.8f);

        Vector3 gate = new Vector3(18.2f, 0f, WaypointCircuit.TopZ);
        Cube("Grand Prix Rainbow Start Pillar Left", gate + new Vector3(0f, 2.65f, -5.6f), new Vector3(0.75f, 5.3f, 0.75f), Quaternion.identity, blue);
        Cube("Grand Prix Rainbow Start Pillar Right", gate + new Vector3(0f, 2.65f, 5.6f), new Vector3(0.75f, 5.3f, 0.75f), Quaternion.identity, pink);

        Material[] rainbow = { railRedMat, orange, yellowMat, green, blue, pink };
        for (int i = 0; i < rainbow.Length; i++)
        {
            Cube($"Grand Prix Rainbow Start Arch {i}", gate + new Vector3(0f, 5.55f + i * 0.22f, 0f), new Vector3(0.84f, 0.16f, 11.8f - i * 0.35f), Quaternion.identity, rainbow[i]);
        }

        for (int i = 0; i < 12; i++)
        {
            float x = 5f + i * 5.0f;
            Material mat = rainbow[i % rainbow.Length];
            Cube($"Grand Prix Festival Pennant {i}", new Vector3(x, 3.0f, -3.0f), new Vector3(1.25f, 0.08f, 0.72f), Quaternion.Euler(0f, 0f, i % 2 == 0 ? 18f : -18f), mat);
            Cube($"Grand Prix Back Pennant {i}", new Vector3(x, 3.0f, -33.0f), new Vector3(1.25f, 0.08f, 0.72f), Quaternion.Euler(0f, 0f, i % 2 == 0 ? -18f : 18f), mat);
        }

        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = WaypointCircuit.At(0.07f + i * 0.085f);
            Material mat = rainbow[i % rainbow.Length];
            BuildBoostPadVisual($"Grand Prix Boost Pad Preview {i}", pos + Vector3.up * 0.055f, mat);
        }

        for (int i = 0; i < 8; i++)
        {
            Vector3 center = WaypointCircuit.At(0.12f + i * 0.10f);
            Vector3 next = WaypointCircuit.At(0.13f + i * 0.10f);
            Vector3 forward = (next - center).normalized;
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            Vector3 pos = center + right * (i % 2 == 0 ? -1f : 1f) * (WaypointCircuit.HalfWidth * 0.52f) + Vector3.up * 1.25f;
            Cube($"Grand Prix Floating Item Marker {i}", pos, new Vector3(0.75f, 0.75f, 0.75f), Quaternion.Euler(0f, 45f, 0f), rainbow[(i + 2) % rainbow.Length]);
            Label($"Grand Prix Item Marker Label {i}", "?", pos + Vector3.up * 0.07f, Quaternion.Euler(35f, 0f, 0f), 0.56f, Color.white);
        }

        for (int i = 0; i < 7; i++)
        {
            float x = 8f + i * 8f;
            BuildBalloonCluster($"Grand Prix Balloon Cluster {i}", new Vector3(x, 4.2f, i % 2 == 0 ? -5.4f : -31.6f), rainbow);
        }

        Cube("Grand Prix Finish Confetti Strip A", new Vector3(18.3f, 0.14f, WaypointCircuit.TopZ), new Vector3(0.22f, 0.045f, 7.2f), Quaternion.identity, pink);
        Cube("Grand Prix Finish Confetti Strip B", new Vector3(18.6f, 0.15f, WaypointCircuit.TopZ), new Vector3(0.22f, 0.045f, 7.2f), Quaternion.identity, yellowMat);
        Label("Grand Prix Festival Label", "COLOR RUSH GRAND PRIX", new Vector3(32f, 3.35f, -36.92f), Quaternion.Euler(0f, 0f, 0f), 0.52f, Color.white);
    }

    void BuildMoroccanTentTheme()
    {
        Cube("Moroccan Tent Ridge", new Vector3(32f, 7.05f, -18f), new Vector3(58f, 0.22f, 0.45f), Quaternion.identity, neonGreenMat);
        Cube("Moroccan Tent Canopy North Red", new Vector3(32f, 6.1f, -8.4f), new Vector3(60f, 0.10f, 20f), Quaternion.Euler(9f, 0f, 0f), railRedMat);
        Cube("Moroccan Tent Canopy South Red", new Vector3(32f, 6.1f, -27.6f), new Vector3(60f, 0.10f, 20f), Quaternion.Euler(-9f, 0f, 0f), railRedMat);
        Cube("Moroccan Green Draped North Wall", new Vector3(32f, 3.4f, 1.04f), new Vector3(57f, 2.6f, 0.16f), Quaternion.identity, neonGreenMat);
        Cube("Moroccan Red Draped South Wall", new Vector3(32f, 2.6f, -37.04f), new Vector3(57f, 2.2f, 0.16f), Quaternion.identity, railRedMat);

        for (int i = 0; i < 9; i++)
        {
            float z = -24f + i * 1.5f;
            Material mat = i % 3 == 0 ? railRedMat : (i % 3 == 1 ? neonGreenMat : creamMat);
            Cube($"Moroccan Carpet Stripe {i}", new Vector3(32f, 0.075f, z), new Vector3(24f, 0.035f, 0.72f), Quaternion.identity, mat);
        }

        for (int i = 0; i < 10; i++)
        {
            float x = 7.5f + i * 5.5f;
            BuildLantern($"Moroccan Brass Lantern North {i}", new Vector3(x, 3.1f, -3.2f));
            BuildLantern($"Moroccan Brass Lantern South {i}", new Vector3(x, 3.1f, -32.8f));
        }

        Vector3 gate = new Vector3(18.2f, 0f, WaypointCircuit.TopZ);
        Cube("Moroccan Arch Left Pillar", gate + new Vector3(-0.15f, 2.4f, -5.4f), new Vector3(0.9f, 4.8f, 0.9f), Quaternion.identity, railRedMat);
        Cube("Moroccan Arch Right Pillar", gate + new Vector3(-0.15f, 2.4f, 5.4f), new Vector3(0.9f, 4.8f, 0.9f), Quaternion.identity, railRedMat);
        Cube("Moroccan Arch Green Header", gate + new Vector3(-0.15f, 4.85f, 0f), new Vector3(1.1f, 0.95f, 11.7f), Quaternion.identity, neonGreenMat);
        Label("Moroccan Drift Label", "MOROCCAN DRIFT TENT", gate + new Vector3(-0.72f, 4.92f, 0f), Quaternion.Euler(0f, -90f, 0f), 0.46f, Color.white);

        BuildFloorStar("Moroccan Green Star Center", new Vector3(32f, 0.13f, -18f), 2.9f, neonGreenMat);
        BuildFloorStar("Moroccan Gold Star North", new Vector3(15f, 0.13f, -9f), 1.8f, yellowMat);
        BuildFloorStar("Moroccan Gold Star South", new Vector3(49f, 0.13f, -27f), 1.8f, yellowMat);
    }

    void BuildWarZoneTheme()
    {
        Cube("War Zone Command Bunker", new Vector3(55.5f, 1.05f, -7.4f), new Vector3(8f, 2.1f, 4.2f), Quaternion.identity, concreteMat);
        Cube("War Zone Bunker View Slit", new Vector3(55.5f, 1.65f, -5.25f), new Vector3(6.8f, 0.34f, 0.16f), Quaternion.identity, darkMat);
        Label("War Zone Bunker Label", "BATTLE ROYALE WAR ZONE", new Vector3(55.5f, 2.45f, -5.12f), Quaternion.Euler(0f, 180f, 0f), 0.36f, yellowMat.color);

        for (int i = 0; i < 9; i++)
        {
            BuildSandbagStack($"War Zone Sandbag North {i}", new Vector3(8f + i * 5.8f, 0.25f, -4.6f));
            BuildSandbagStack($"War Zone Sandbag South {i}", new Vector3(8f + i * 5.8f, 0.25f, -31.4f));
        }

        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = new Vector3(12f + i * 6f, 0.45f, i % 2 == 0 ? -14.6f : -21.4f);
            Cube($"War Zone Supply Crate {i}", pos, new Vector3(2.0f, 0.9f, 1.55f), Quaternion.Euler(0f, i * 17f, 0f), oliveMat);
            Cube($"War Zone Crate Strap {i}", pos + Vector3.up * 0.48f, new Vector3(2.08f, 0.08f, 0.12f), Quaternion.Euler(0f, i * 17f, 0f), darkMat);
        }

        BuildWatchTower("War Zone Watchtower West", new Vector3(6.8f, 0f, -7f));
        BuildWatchTower("War Zone Watchtower East", new Vector3(57.2f, 0f, -29f));

        for (int i = 0; i < 8; i++)
        {
            float x = 10f + i * 6.2f;
            Cube($"War Zone Hazard Stripe Black {i}", new Vector3(x, 0.09f, -18f), new Vector3(2.8f, 0.035f, 0.32f), Quaternion.Euler(0f, 25f, 0f), darkMat);
            Cube($"War Zone Hazard Stripe Yellow {i}", new Vector3(x + 1.4f, 0.095f, -18f), new Vector3(2.8f, 0.035f, 0.32f), Quaternion.Euler(0f, 25f, 0f), yellowMat);
        }

        Cube("War Zone Drop Zone Marker", new Vector3(32f, 0.11f, -18f), new Vector3(13f, 0.04f, 1.1f), Quaternion.Euler(0f, 45f, 0f), yellowMat);
        Cube("War Zone Drop Zone Marker Cross", new Vector3(32f, 0.12f, -18f), new Vector3(13f, 0.04f, 1.1f), Quaternion.Euler(0f, -45f, 0f), yellowMat);
        Label("War Zone Drop Zone Text", "DROP ZONE", new Vector3(32f, 0.18f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.54f, Color.black);
    }

    void BuildZombieApocalypseTheme()
    {
        Label("Zombie Quarantine Wall Text", "INFECTED QUARANTINE", new Vector3(32f, 3.15f, 1.06f), Quaternion.Euler(0f, 180f, 0f), 0.5f, toxicMat.color);

        for (int i = 0; i < 18; i++)
        {
            float x = 5f + (i * 11 % 54);
            float z = -33f + (i * 7 % 29);
            Cube($"Zombie Asphalt Crack {i}", new Vector3(x, 0.085f, z), new Vector3(3.2f, 0.028f, 0.10f), Quaternion.Euler(0f, i * 23f, 0f), darkMat);
        }

        for (int i = 0; i < 7; i++)
        {
            BuildBoardedBarricade($"Zombie Boarded Barricade {i}", new Vector3(7.5f + i * 8f, 0.8f, i % 2 == 0 ? -6.2f : -34.2f), i % 2 == 0 ? 0f : 180f);
        }

        BuildWreckedCar("Zombie Wrecked Car West", new Vector3(12f, 0.35f, -23f), -18f);
        BuildWreckedCar("Zombie Wrecked Car East", new Vector3(52f, 0.35f, -12f), 22f);

        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = new Vector3(8f + i * 5.2f, 0.55f, i % 2 == 0 ? -8.8f : -29.2f);
            Cylinder($"Zombie Toxic Barrel {i}", pos, new Vector3(0.55f, 0.55f, 0.55f), Quaternion.identity, toxicMat);
            Cube($"Zombie Barrel Stripe {i}", pos + Vector3.up * 0.23f, new Vector3(1.15f, 0.08f, 1.15f), Quaternion.identity, darkMat);
        }

        Cube("Zombie Quarantine Gate Left", new Vector3(18.2f, 2.6f, WaypointCircuit.TopZ - 5.2f), new Vector3(0.55f, 5.2f, 0.55f), Quaternion.identity, concreteMat);
        Cube("Zombie Quarantine Gate Right", new Vector3(18.2f, 2.6f, WaypointCircuit.TopZ + 5.2f), new Vector3(0.55f, 5.2f, 0.55f), Quaternion.identity, concreteMat);
        Cube("Zombie Quarantine Gate Sign", new Vector3(18.2f, 4.7f, WaypointCircuit.TopZ), new Vector3(0.55f, 0.9f, 10.8f), Quaternion.identity, toxicMat);
        Label("Zombie Quarantine Gate Label", "QUARANTINE", new Vector3(17.84f, 4.72f, WaypointCircuit.TopZ), Quaternion.Euler(0f, -90f, 0f), 0.46f, Color.black);
    }

    void BuildSpotlightTheme()
    {
        for (int i = 0; i < 8; i++)
        {
            float t = i / 8f;
            Vector3 pos = WaypointCircuit.At(t) + Vector3.up * 0.08f;
            Cylinder($"Spotlight Floor Pool {i}", pos, new Vector3(2.0f, 0.018f, 2.0f), Quaternion.identity, yellowMat);
        }
    }

    void BuildBoostPadVisual(string name, Vector3 center, Material material)
    {
        Vector3 next = center + Vector3.forward;
        Quaternion rotation = Quaternion.identity;

        if (WaypointCircuit.I != null)
        {
            int nearest = WaypointCircuit.I.Nearest(center);
            float t = (float)nearest / WaypointCircuit.Count;
            Vector3 circuitPos = WaypointCircuit.At(t);
            next = WaypointCircuit.At(t + 0.01f);
            Vector3 forward = (next - circuitPos).normalized;
            rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        Cube(name + " Base", center, new Vector3(2.4f, 0.045f, 1.1f), rotation, material);
        Cube(name + " Arrow A", center + rotation * new Vector3(0f, 0.035f, -0.22f), new Vector3(0.28f, 0.035f, 0.72f), rotation * Quaternion.Euler(0f, 28f, 0f), railWhiteMat);
        Cube(name + " Arrow B", center + rotation * new Vector3(0f, 0.04f, 0.22f), new Vector3(0.28f, 0.035f, 0.72f), rotation * Quaternion.Euler(0f, -28f, 0f), railWhiteMat);
    }

    void BuildBalloonCluster(string name, Vector3 center, Material[] materials)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3((i % 2 == 0 ? -0.34f : 0.34f), i * 0.22f, i < 2 ? -0.22f : 0.22f);
            Cylinder($"{name} String {i}", center + offset * 0.5f + Vector3.down * 0.72f, new Vector3(0.018f, 0.72f, 0.018f), Quaternion.identity, darkMat);
            var balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            balloon.name = $"{name} Balloon {i}";
            balloon.transform.SetParent(transform, false);
            balloon.transform.position = center + offset;
            balloon.transform.localScale = new Vector3(0.62f, 0.78f, 0.62f);
            ApplyMaterial(balloon, materials[i % materials.Length]);
            RemoveCollider(balloon);
        }
    }

    void BuildLantern(string name, Vector3 position)
    {
        Cylinder(name + " Cord", position + Vector3.up * 0.62f, new Vector3(0.035f, 0.62f, 0.035f), Quaternion.identity, darkMat);
        Cylinder(name, position, new Vector3(0.26f, 0.42f, 0.26f), Quaternion.identity, yellowMat);
        Cube(name + " Frame", position, new Vector3(0.58f, 0.08f, 0.58f), Quaternion.identity, darkMat);
    }

    void BuildFloorStar(string name, Vector3 center, float size, Material material)
    {
        for (int i = 0; i < 5; i++)
            Cube($"{name} Ray {i}", center + Vector3.up * (i * 0.002f), new Vector3(size * 0.18f, 0.03f, size), Quaternion.Euler(0f, i * 36f, 0f), material);
    }

    void BuildSandbagStack(string name, Vector3 basePosition)
    {
        for (int row = 0; row < 3; row++)
        {
            int count = 4 - row;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = basePosition + new Vector3((i - count * 0.5f) * 0.72f, row * 0.28f, 0f);
                Cube($"{name} Bag {row}-{i}", pos, new Vector3(0.68f, 0.24f, 0.42f), Quaternion.Euler(0f, i % 2 == 0 ? 8f : -8f, 0f), sandMat);
            }
        }
    }

    void BuildWatchTower(string name, Vector3 basePosition)
    {
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
                Cube($"{name} Leg {x} {z}", basePosition + new Vector3(x * 1.25f, 2f, z * 1.25f), new Vector3(0.18f, 4f, 0.18f), Quaternion.identity, woodMat);
        }

        Cube(name + " Deck", basePosition + new Vector3(0f, 4.05f, 0f), new Vector3(3.4f, 0.28f, 3.4f), Quaternion.identity, woodMat);
        Cube(name + " Roof", basePosition + new Vector3(0f, 5.28f, 0f), new Vector3(3.9f, 0.28f, 3.9f), Quaternion.identity, oliveMat);
        Cube(name + " Beacon", basePosition + new Vector3(0f, 5.65f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Quaternion.identity, yellowMat);
    }

    void BuildBoardedBarricade(string name, Vector3 position, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Cube(name + " Frame", position, new Vector3(4.4f, 1.1f, 0.18f), rotation, concreteMat);
        Cube(name + " Board A", position + Vector3.up * 0.12f, new Vector3(4.8f, 0.18f, 0.24f), rotation * Quaternion.Euler(0f, 0f, 12f), woodMat);
        Cube(name + " Board B", position + Vector3.up * 0.42f, new Vector3(4.8f, 0.18f, 0.24f), rotation * Quaternion.Euler(0f, 0f, -11f), woodMat);
        Cube(name + " Warning", position + Vector3.up * 0.78f, new Vector3(2.0f, 0.22f, 0.26f), rotation, toxicMat);
    }

    void BuildWreckedCar(string name, Vector3 position, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Cube(name + " Body", position + Vector3.up * 0.35f, new Vector3(3.5f, 0.7f, 1.75f), rotation, RuntimeMaterials.Make(new Color(0.18f, 0.22f, 0.18f)));
        Cube(name + " Hood", position + new Vector3(1.15f, 0.78f, 0f), new Vector3(1.35f, 0.25f, 1.55f), rotation * Quaternion.Euler(0f, 0f, -8f), concreteMat);
        Cube(name + " Window", position + new Vector3(-0.45f, 0.92f, 0f), new Vector3(1.2f, 0.42f, 1.35f), rotation, RuntimeMaterials.Make(new Color(0.04f, 0.08f, 0.06f)));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
                Cylinder($"{name} Wheel {sx} {sz}", position + rotation * new Vector3(sx * 1.2f, 0.24f, sz * 0.92f), new Vector3(0.28f, 0.18f, 0.28f), rotation * Quaternion.Euler(90f, 0f, 0f), darkMat);
        }
    }

    string ThemeTitle()
    {
        return theme switch
        {
            ArenaTheme.MoroccanTent => "MOROCCAN DRIFT TENT",
            ArenaTheme.WarZone => "BATTLE ROYALE WAR ZONE",
            ArenaTheme.ZombieApocalypse => "ZOMBIE QUARANTINE",
            ArenaTheme.ColorfulKartArena => "COLOR RUSH GRAND PRIX",
            ArenaTheme.Spotlight => "LAST ONE LIT",
            _ => "ZIKJ ARENA"
        };
    }

    Color ThemeAccentColor()
    {
        return theme switch
        {
            ArenaTheme.MoroccanTent => new Color(0.05f, 1f, 0.36f),
            ArenaTheme.WarZone => new Color(1f, 0.62f, 0.18f),
            ArenaTheme.ZombieApocalypse => new Color(0.46f, 1f, 0.25f),
            ArenaTheme.ColorfulKartArena => new Color(1f, 0.76f, 0.24f),
            ArenaTheme.Spotlight => new Color(1f, 0.86f, 0.25f),
            _ => new Color(0.35f, 1f, 0.7f)
        };
    }

    GameObject Cube(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        ApplyMaterial(go, material);
        RemoveCollider(go);
        return go;
    }

    GameObject Cylinder(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        ApplyMaterial(go, material);
        RemoveCollider(go);
        return go;
    }

    void Label(string name, string text, Vector3 position, Quaternion rotation, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);

        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = size;
        mesh.fontSize = 64;
        mesh.color = color;
        mesh.fontStyle = FontStyle.Bold;
    }

    static void ApplyMaterial(GameObject go, Material material)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.material = material;
    }

    static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            DestroyUnityObject(collider);
    }

    static void DestroyUnityObject(Object obj)
    {
        if (Application.isPlaying)
            UnityObjectUtil.Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
