using UnityEngine;

public class ProjectedBattleMapBuilder : MonoBehaviour
{
    public enum ProjectionTheme
    {
        Infected,
        BattleRoyale,
        GrandPrix,
        DriftArena,
        TronTrails,
        LastOneLit
    }

    [Range(64, 192)] public int trackSegments = 144;
    public ProjectionTheme theme = ProjectionTheme.GrandPrix;

    public int ProjectedElementCount { get; private set; }

    Material trackMat;
    Material edgeCyanMat;
    Material edgePinkMat;
    Material laneMat;
    Material bonusMat;
    Material boostMat;
    Material oilMat;
    Material rocketMat;
    Material warningMat;
    Material shieldMat;
    Material waterMat;
    Material forestMat;
    Material purpleMat;
    Material darkProjectionMat;
    Material whiteMat;

    void Start()
    {
        Build();
    }

    [ContextMenu("Build Projected Battle Map")]
    public void Build()
    {
        ProjectedElementCount = 0;
        CreateMaterials();
        ClearChildren();

        BuildProjectionWash();
        BuildProjectedTrack();
        BuildStartFinishProjection();
        BuildBonusAndWeaponProjection();
        BuildNoCutProjection();
        BuildThemeProjection();
    }

    void CreateMaterials()
    {
        Color accent = AccentColor();
        trackMat = RuntimeMaterials.Emissive(new Color(0.018f, 0.024f, 0.04f), accent * 0.55f, 1.25f);
        edgeCyanMat = RuntimeMaterials.Emissive(new Color(0.02f, 0.58f, 0.95f), new Color(0.02f, 0.76f, 1f), 2.7f);
        edgePinkMat = RuntimeMaterials.Emissive(new Color(1f, 0.12f, 0.55f), new Color(1f, 0.08f, 0.48f), 2.25f);
        laneMat = RuntimeMaterials.Emissive(new Color(0.92f, 0.96f, 1f), new Color(0.70f, 0.90f, 1f), 1.8f);
        bonusMat = RuntimeMaterials.Emissive(new Color(1f, 0.76f, 0.08f), new Color(1f, 0.55f, 0.02f), 2.6f);
        boostMat = RuntimeMaterials.Emissive(new Color(0.02f, 0.55f, 1f), new Color(0.02f, 0.75f, 1f), 2.8f);
        oilMat = RuntimeMaterials.Emissive(new Color(0.005f, 0.006f, 0.008f), new Color(0.20f, 0.05f, 0.28f), 1.2f);
        rocketMat = RuntimeMaterials.Emissive(new Color(1f, 0.08f, 0.06f), new Color(1f, 0.16f, 0.04f), 2.7f);
        warningMat = RuntimeMaterials.Emissive(new Color(1f, 0.38f, 0.04f), new Color(1f, 0.24f, 0.02f), 2.0f);
        shieldMat = RuntimeMaterials.Emissive(new Color(0.08f, 0.95f, 0.90f), new Color(0.04f, 1f, 0.88f), 2.2f);
        waterMat = RuntimeMaterials.Emissive(new Color(0.02f, 0.16f, 0.34f), new Color(0.02f, 0.45f, 1f), 1.65f);
        forestMat = RuntimeMaterials.Emissive(new Color(0.05f, 0.36f, 0.16f), new Color(0.10f, 0.95f, 0.24f), 1.75f);
        purpleMat = RuntimeMaterials.Emissive(new Color(0.30f, 0.08f, 0.75f), new Color(0.58f, 0.16f, 1f), 2.0f);
        darkProjectionMat = RuntimeMaterials.Emissive(new Color(0.006f, 0.008f, 0.014f), new Color(0.02f, 0.04f, 0.09f), 1.1f);
        whiteMat = RuntimeMaterials.Emissive(new Color(0.94f, 0.96f, 1f), new Color(0.75f, 0.86f, 1f), 1.5f);
    }

    Color AccentColor()
    {
        return theme switch
        {
            ProjectionTheme.Infected => new Color(0.30f, 1f, 0.18f),
            ProjectionTheme.BattleRoyale => new Color(1f, 0.34f, 0.08f),
            ProjectionTheme.DriftArena => new Color(0.08f, 1f, 0.36f),
            ProjectionTheme.TronTrails => new Color(0.06f, 0.95f, 1f),
            ProjectionTheme.LastOneLit => new Color(1f, 0.84f, 0.22f),
            _ => new Color(0.14f, 0.62f, 1f)
        };
    }

    void BuildProjectionWash()
    {
        Cube("Projected Arena Darkness Base", new Vector3(32f, 0.018f, -18f), new Vector3(61f, 0.018f, 40f), Quaternion.identity, darkProjectionMat);

        Cube("Projected Map North Color Wash", new Vector3(32f, 0.028f, -5.4f), new Vector3(58f, 0.018f, 2.2f), Quaternion.identity, edgeCyanMat);
        Cube("Projected Map South Color Wash", new Vector3(32f, 0.029f, -30.6f), new Vector3(58f, 0.018f, 2.2f), Quaternion.identity, edgePinkMat);
        Cube("Projected Map Infield Wash", new Vector3(32f, 0.031f, -18f), new Vector3(20.5f, 0.018f, 10.5f), Quaternion.identity, RuntimeMaterials.Emissive(new Color(0.025f, 0.038f, 0.065f), AccentColor() * 0.75f, 1.25f));

        for (int i = 0; i < 11; i++)
        {
            float x = 6.5f + i * 5.1f;
            Material mat = i % 2 == 0 ? edgeCyanMat : edgePinkMat;
            Cube($"Projected Ceiling Beam Footprint {i}", new Vector3(x, 0.045f, -18f), new Vector3(0.12f, 0.02f, 32f), Quaternion.identity, mat);
        }
    }

    void BuildProjectedTrack()
    {
        int steps = Mathf.Max(64, trackSegments);

        for (int i = 0; i < steps; i++)
        {
            float t0 = (float)i / steps;
            float t1 = (float)(i + 1) / steps;
            Vector3 p0 = WaypointCircuit.At(t0);
            Vector3 p1 = WaypointCircuit.At(t1);
            Vector3 delta = p1 - p0;
            if (delta.sqrMagnitude < 0.0001f)
                continue;

            Vector3 forward = delta.normalized;
            Vector3 right = new(-forward.z, 0f, forward.x);
            float halfWidth = WaypointCircuit.HalfWidth;

            Strip($"Projected Road Surface {i}", p0, p1, halfWidth * 2.06f, 0.032f, 0.058f, trackMat);
            Strip($"Projected Road Outer Glow {i}", p0 + right * (halfWidth + 0.22f), p1 + right * (halfWidth + 0.22f), 0.18f, 0.022f, 0.086f, i % 3 == 0 ? edgePinkMat : edgeCyanMat);
            Strip($"Projected Road Inner Glow {i}", p0 - right * (halfWidth + 0.22f), p1 - right * (halfWidth + 0.22f), 0.18f, 0.022f, 0.087f, i % 3 == 0 ? edgeCyanMat : edgePinkMat);

            if (i % 4 == 0)
            {
                Vector3 dashStart = Vector3.Lerp(p0, p1, 0.18f);
                Vector3 dashEnd = Vector3.Lerp(p0, p1, 0.72f);
                Strip($"Projected Dashed Center Line {i}", dashStart, dashEnd, 0.11f, 0.024f, 0.102f, laneMat);
            }

            if (i % 5 == 0)
            {
                Material curb = i % 10 == 0 ? rocketMat : whiteMat;
                Vector3 curbOuter = Vector3.Lerp(p0, p1, 0.50f) + right * (halfWidth + 0.02f);
                Vector3 curbInner = Vector3.Lerp(p0, p1, 0.50f) - right * (halfWidth + 0.02f);
                Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                Cube($"Projected Outer Curb Block {i}", curbOuter + Vector3.up * 0.111f, new Vector3(0.42f, 0.026f, delta.magnitude * 0.56f), rot, curb);
                Cube($"Projected Inner Curb Block {i}", curbInner + Vector3.up * 0.112f, new Vector3(0.42f, 0.026f, delta.magnitude * 0.56f), rot, curb);
            }
        }
    }

    void BuildStartFinishProjection()
    {
        Vector3 gate = new(18.2f, 0f, WaypointCircuit.TopZ);
        Quaternion rot = Quaternion.Euler(0f, 90f, 0f);

        Cube("Projected Start Finish Glow", gate + Vector3.up * 0.14f, new Vector3(0.32f, 0.04f, 8.2f), Quaternion.identity, bonusMat);

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 10; col++)
            {
                float z = -3.85f + col * 0.85f;
                float x = row == 0 ? -0.22f : 0.22f;
                Material mat = (row + col) % 2 == 0 ? whiteMat : darkProjectionMat;
                Cube($"Projected Checkered Finish {row}-{col}", gate + new Vector3(x, 0.17f, z), new Vector3(0.42f, 0.026f, 0.42f), Quaternion.identity, mat);
            }
        }

        Label("Projected Battle Race Title", "ZIKJ BATTLE RACE", gate + new Vector3(0.58f, 0.20f, 0f), rot * Quaternion.Euler(90f, 0f, 0f), 0.38f, Color.white);
    }

    void BuildBonusAndWeaponProjection()
    {
        float[] bonusT = { 0.08f, 0.17f, 0.29f, 0.39f, 0.51f, 0.62f, 0.74f, 0.85f };
        for (int i = 0; i < bonusT.Length; i++)
            BuildBonusSquare($"Projected Yellow Bonus Box {i}", bonusT[i], i % 2 == 0 ? -0.45f : 0.45f);

        float[] boostT = { 0.13f, 0.34f, 0.57f, 0.78f };
        for (int i = 0; i < boostT.Length; i++)
            BuildBoostArrow($"Projected Nitro Boost Pad {i}", boostT[i], i % 2 == 0 ? 0.0f : 0.35f);

        float[] oilT = { 0.23f, 0.47f, 0.69f, 0.91f };
        for (int i = 0; i < oilT.Length; i++)
            BuildOilSlick($"Projected Oil Slick Trap {i}", oilT[i], i % 2 == 0 ? 0.44f : -0.44f);

        float[] rocketT = { 0.20f, 0.44f, 0.66f, 0.88f };
        for (int i = 0; i < rocketT.Length; i++)
            BuildRocketLane($"Projected Rocket Warning Lane {i}", rocketT[i], i % 2 == 0 ? -0.15f : 0.15f);

        float[] shieldT = { 0.31f, 0.55f, 0.81f };
        for (int i = 0; i < shieldT.Length; i++)
            BuildShieldIcon($"Projected Shield Pickup {i}", shieldT[i], i % 2 == 0 ? -0.52f : 0.52f);
    }

    void BuildNoCutProjection()
    {
        Vector3 center = new(32f, 0.18f, -18f);
        Cube("Projected No Cut X A", center, new Vector3(15f, 0.035f, 0.34f), Quaternion.Euler(0f, 36f, 0f), rocketMat);
        Cube("Projected No Cut X B", center + Vector3.up * 0.012f, new Vector3(15f, 0.035f, 0.34f), Quaternion.Euler(0f, -36f, 0f), rocketMat);
        Label("Projected No Cut Label", "NO CUT", center + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f), 0.54f, Color.white);
    }

    void BuildThemeProjection()
    {
        switch (theme)
        {
            case ProjectionTheme.Infected:
                BuildInfectedProjection();
                break;
            case ProjectionTheme.BattleRoyale:
                BuildBattleRoyaleProjection();
                break;
            case ProjectionTheme.DriftArena:
                BuildDriftProjection();
                break;
            case ProjectionTheme.TronTrails:
                BuildTronProjection();
                break;
            case ProjectionTheme.LastOneLit:
                BuildLastOneLitProjection();
                break;
            default:
                BuildGrandPrixProjection();
                break;
        }
    }

    void BuildGrandPrixProjection()
    {
        Material[] mats = { edgeCyanMat, edgePinkMat, bonusMat, boostMat, forestMat };

        for (int i = 0; i < 12; i++)
        {
            float x = 8f + i * 4.4f;
            Material mat = mats[i % mats.Length];
            Cube($"Projected Classic Confetti Tile {i}", new Vector3(x, 0.16f, i % 2 == 0 ? -7.5f : -28.5f), new Vector3(2.6f, 0.03f, 1.0f), Quaternion.Euler(0f, i * 18f, 0f), mat);
        }

        BuildFloorTarget("Projected Classic Bonus Target A", new Vector3(13f, 0.18f, -18f), 3.2f, bonusMat);
        BuildFloorTarget("Projected Classic Bonus Target B", new Vector3(51f, 0.18f, -18f), 3.2f, boostMat);
        Label("Projected Classic Map Label", "BONUS RUN", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.48f, Color.white);
    }

    void BuildBattleRoyaleProjection()
    {
        BuildFloorTarget("Projected Battle Royale Drop Zone", new Vector3(32f, 0.18f, -18f), 5.2f, warningMat);
        Label("Projected Battle Royale Drop Label", "DROP ZONE", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.50f, Color.black);

        for (int i = 0; i < 12; i++)
        {
            float x = 7f + i * 4.7f;
            Cube($"Projected Battle Hazard Stripe {i}", new Vector3(x, 0.16f, i % 2 == 0 ? -12.2f : -23.8f), new Vector3(2.8f, 0.03f, 0.34f), Quaternion.Euler(0f, i % 2 == 0 ? 32f : -32f, 0f), i % 2 == 0 ? warningMat : darkProjectionMat);
        }
    }

    void BuildInfectedProjection()
    {
        for (int i = 0; i < 9; i++)
        {
            float t = 0.05f + i * 0.105f;
            TrackPose(t, i % 2 == 0 ? -0.62f : 0.62f, out Vector3 pos, out Quaternion rot);
            Cylinder($"Projected Toxic Spill {i}", pos + Vector3.up * 0.16f, new Vector3(1.45f, 0.018f, 0.82f), rot, forestMat);
        }

        Cube("Projected Quarantine Scan Line A", new Vector3(32f, 0.18f, -11f), new Vector3(36f, 0.03f, 0.22f), Quaternion.identity, forestMat);
        Cube("Projected Quarantine Scan Line B", new Vector3(32f, 0.18f, -25f), new Vector3(36f, 0.03f, 0.22f), Quaternion.identity, forestMat);
        Label("Projected Quarantine Label", "QUARANTINE ROUTE", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.42f, forestMat.color);
    }

    void BuildDriftProjection()
    {
        for (int i = 0; i < 10; i++)
        {
            float z = -24.5f + i * 1.45f;
            Material mat = i % 3 == 0 ? rocketMat : (i % 3 == 1 ? forestMat : bonusMat);
            Cube($"Projected Drift Carpet Stripe {i}", new Vector3(32f, 0.16f, z), new Vector3(22f, 0.03f, 0.48f), Quaternion.identity, mat);
        }

        BuildFloorStar("Projected Drift Green Star", new Vector3(16f, 0.18f, -18f), 2.2f, forestMat);
        BuildFloorStar("Projected Drift Gold Star", new Vector3(48f, 0.18f, -18f), 2.2f, bonusMat);
        Label("Projected Drift Label", "DRIFT SCORE ZONE", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.46f, Color.white);
    }

    void BuildTronProjection()
    {
        for (int i = 0; i < 12; i++)
        {
            float x = 6f + i * 4.7f;
            Cube($"Projected Tron Grid X {i}", new Vector3(x, 0.16f, -18f), new Vector3(0.10f, 0.026f, 26f), Quaternion.identity, i % 2 == 0 ? edgeCyanMat : purpleMat);
        }

        for (int i = 0; i < 8; i++)
        {
            float z = -30f + i * 3.4f;
            Cube($"Projected Tron Grid Z {i}", new Vector3(32f, 0.165f, z), new Vector3(52f, 0.026f, 0.10f), Quaternion.identity, i % 2 == 0 ? purpleMat : edgeCyanMat);
        }

        Label("Projected Tron Label", "LIGHT TRAIL ARENA", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.44f, Color.white);
    }

    void BuildLastOneLitProjection()
    {
        for (int i = 0; i < 10; i++)
        {
            float t = i / 10f;
            Vector3 pos = WaypointCircuit.At(t) + Vector3.up * 0.16f;
            Cylinder($"Projected Moving Spotlight Pool {i}", pos, new Vector3(2.0f + (i % 3) * 0.28f, 0.018f, 2.0f + (i % 2) * 0.36f), Quaternion.identity, bonusMat);
        }

        BuildFloorTarget("Projected Last One Lit Center Ring", new Vector3(32f, 0.18f, -18f), 4.8f, bonusMat);
        Label("Projected Last One Lit Label", "STAY LIT", new Vector3(32f, 0.22f, -18f), Quaternion.Euler(90f, 0f, 0f), 0.56f, Color.black);
    }

    void BuildBonusSquare(string name, float t, float side)
    {
        TrackPose(t, side, out Vector3 pos, out Quaternion rot);
        Cube(name, pos + Vector3.up * 0.18f, new Vector3(1.35f, 0.04f, 1.35f), rot, bonusMat);
        Cube(name + " Border X", pos + Vector3.up * 0.205f, new Vector3(1.54f, 0.025f, 0.12f), rot, whiteMat);
        Cube(name + " Border Z", pos + Vector3.up * 0.208f, new Vector3(0.12f, 0.025f, 1.54f), rot, whiteMat);
        Label(name + " Question Mark", "?", pos + Vector3.up * 0.24f, rot * Quaternion.Euler(90f, 0f, 0f), 0.52f, Color.black);
    }

    void BuildBoostArrow(string name, float t, float side)
    {
        TrackPose(t, side, out Vector3 pos, out Quaternion rot);
        Cube(name + " Base", pos + Vector3.up * 0.18f, new Vector3(1.75f, 0.04f, 2.5f), rot, boostMat);
        Cube(name + " Arrow Stem", pos + rot * new Vector3(0f, 0.04f, -0.25f) + Vector3.up * 0.20f, new Vector3(0.34f, 0.026f, 1.25f), rot, whiteMat);
        Cube(name + " Arrow Wing L", pos + rot * new Vector3(-0.28f, 0.05f, 0.47f) + Vector3.up * 0.20f, new Vector3(0.28f, 0.026f, 0.95f), rot * Quaternion.Euler(0f, -38f, 0f), whiteMat);
        Cube(name + " Arrow Wing R", pos + rot * new Vector3(0.28f, 0.05f, 0.47f) + Vector3.up * 0.20f, new Vector3(0.28f, 0.026f, 0.95f), rot * Quaternion.Euler(0f, 38f, 0f), whiteMat);
    }

    void BuildOilSlick(string name, float t, float side)
    {
        TrackPose(t, side, out Vector3 pos, out Quaternion rot);
        Cylinder(name, pos + Vector3.up * 0.18f, new Vector3(1.15f, 0.016f, 0.72f), rot, oilMat);
        Cylinder(name + " Shine", pos + rot * new Vector3(-0.28f, 0f, 0.10f) + Vector3.up * 0.20f, new Vector3(0.24f, 0.010f, 0.10f), rot, purpleMat);
    }

    void BuildRocketLane(string name, float t, float side)
    {
        TrackPose(t, side, out Vector3 pos, out Quaternion rot);
        Cube(name + " Trail", pos + Vector3.up * 0.18f, new Vector3(0.35f, 0.034f, 3.5f), rot, rocketMat);
        Cube(name + " Head L", pos + rot * new Vector3(-0.34f, 0f, 1.38f) + Vector3.up * 0.20f, new Vector3(0.25f, 0.026f, 1.05f), rot * Quaternion.Euler(0f, -34f, 0f), rocketMat);
        Cube(name + " Head R", pos + rot * new Vector3(0.34f, 0f, 1.38f) + Vector3.up * 0.20f, new Vector3(0.25f, 0.026f, 1.05f), rot * Quaternion.Euler(0f, 34f, 0f), rocketMat);
    }

    void BuildShieldIcon(string name, float t, float side)
    {
        TrackPose(t, side, out Vector3 pos, out Quaternion rot);
        BuildFloorTarget(name, pos + Vector3.up * 0.02f, 1.55f, shieldMat);
        Label(name + " Label", "SHIELD", pos + Vector3.up * 0.24f, rot * Quaternion.Euler(90f, 0f, 0f), 0.20f, Color.black);
    }

    void BuildFloorTarget(string name, Vector3 center, float radius, Material mat)
    {
        Cylinder(name + " Ring Outer", center + Vector3.up * 0.01f, new Vector3(radius, 0.016f, radius), Quaternion.identity, mat);
        Cylinder(name + " Ring Inner", center + Vector3.up * 0.025f, new Vector3(radius * 0.68f, 0.016f, radius * 0.68f), Quaternion.identity, darkProjectionMat);
        Cube(name + " Cross A", center + Vector3.up * 0.045f, new Vector3(radius * 1.7f, 0.02f, 0.14f), Quaternion.identity, mat);
        Cube(name + " Cross B", center + Vector3.up * 0.055f, new Vector3(0.14f, 0.02f, radius * 1.7f), Quaternion.identity, mat);
    }

    void BuildFloorStar(string name, Vector3 center, float size, Material mat)
    {
        for (int i = 0; i < 5; i++)
            Cube($"{name} Ray {i}", center + Vector3.up * (i * 0.006f), new Vector3(size * 0.16f, 0.025f, size), Quaternion.Euler(0f, i * 36f, 0f), mat);
    }

    void TrackPose(float t, float side, out Vector3 position, out Quaternion rotation)
    {
        Vector3 center = WaypointCircuit.At(t);
        Vector3 next = WaypointCircuit.At(t + 0.01f);
        Vector3 forward = (next - center).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Vector3 right = new(-forward.z, 0f, forward.x);
        position = center + right * (side * WaypointCircuit.HalfWidth);
        rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void Strip(string name, Vector3 a, Vector3 b, float width, float height, float y, Material mat)
    {
        Vector3 delta = b - a;
        if (delta.sqrMagnitude < 0.0001f)
            return;

        Quaternion rot = Quaternion.LookRotation(delta.normalized, Vector3.up);
        Cube(name, (a + b) * 0.5f + Vector3.up * y, new Vector3(width, height, delta.magnitude + 0.02f), rot, mat);
    }

    GameObject Cube(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        ApplyMaterial(go, mat);
        RemoveCollider(go);
        ProjectedElementCount++;
        return go;
    }

    GameObject Cylinder(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        ApplyMaterial(go, mat);
        RemoveCollider(go);
        ProjectedElementCount++;
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
        mesh.fontSize = 72;
        mesh.color = color;
        mesh.fontStyle = FontStyle.Bold;
        ProjectedElementCount++;
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyUnityObject(transform.GetChild(i).gameObject);
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && mat != null)
            renderer.material = mat;
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
