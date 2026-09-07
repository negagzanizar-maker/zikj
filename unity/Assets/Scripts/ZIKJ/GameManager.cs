using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameMode
    {
        Infected,
        BattleRoyale,
        GrandPrix,
        DriftArena,
        TronTrails,
        LastOneLit
    }

    [Header("Mode")]
    public GameMode gameMode = GameMode.Infected;

    [Header("Kart Setup")]
    public GameObject kartPrefab;
    [Range(2, 12)] public int kartCount = 6;
    public int playerKartIndex = 0;

    [Header("References")]
    public WaypointCircuit circuit;
    public InfectedMode infectedMode;
    public BattleRoyaleMode battleRoyaleMode;
    public GrandPrixMode grandPrixMode;
    public DriftArenaMode driftArenaMode;
    public TronTrailsMode tronTrailsMode;
    public LastOneLitMode lastOneLitMode;

    public static readonly Color[] KartColors =
    {
        new Color(0.26f, 0.84f, 1.00f),
        new Color(1.00f, 0.82f, 0.26f),
        new Color(1.00f, 0.26f, 0.84f),
        new Color(0.26f, 1.00f, 0.53f),
        new Color(0.82f, 0.26f, 1.00f),
        new Color(1.00f, 0.53f, 0.26f),
    };

    KartController[] karts;

    public KartController[] Karts => karts;

    void Start()
    {
        circuit ??= WaypointCircuit.I ?? FindFirstObjectByType<WaypointCircuit>();
        infectedMode ??= FindFirstObjectByType<InfectedMode>();
        battleRoyaleMode ??= FindFirstObjectByType<BattleRoyaleMode>();
        grandPrixMode ??= FindFirstObjectByType<GrandPrixMode>();
        driftArenaMode ??= FindFirstObjectByType<DriftArenaMode>();
        tronTrailsMode ??= FindFirstObjectByType<TronTrailsMode>();
        lastOneLitMode ??= FindFirstObjectByType<LastOneLitMode>();

        if (circuit == null)
        {
            var circuitGo = new GameObject("WaypointCircuit");
            circuit = circuitGo.AddComponent<WaypointCircuit>();
        }

        if (gameMode == GameMode.Infected && infectedMode == null)
            infectedMode = gameObject.AddComponent<InfectedMode>();

        if (gameMode == GameMode.BattleRoyale && battleRoyaleMode == null)
            battleRoyaleMode = gameObject.AddComponent<BattleRoyaleMode>();

        if (gameMode == GameMode.GrandPrix && grandPrixMode == null)
            grandPrixMode = gameObject.AddComponent<GrandPrixMode>();

        if (gameMode == GameMode.DriftArena && driftArenaMode == null)
            driftArenaMode = gameObject.AddComponent<DriftArenaMode>();

        if (gameMode == GameMode.TronTrails && tronTrailsMode == null)
            tronTrailsMode = gameObject.AddComponent<TronTrailsMode>();

        if (gameMode == GameMode.LastOneLit && lastOneLitMode == null)
            lastOneLitMode = gameObject.AddComponent<LastOneLitMode>();

        karts = SpawnKarts();

        // Register non-player karts so live venue tracking can take over as soon as
        // packets arrive. Until then, AI remains active for local playable builds.
        var trackingMgr = FindFirstObjectByType<TrackingInputManager>();
        if (trackingMgr != null)
        {
            foreach (var kart in karts)
            {
                if (kart != null && !kart.isPlayer)
                {
                    trackingMgr.RegisterKart(kart);
                    kart.trackingMode = false;
                }
            }
        }

        switch (gameMode)
        {
            case GameMode.BattleRoyale:
                battleRoyaleMode.Init(karts);
                break;
            case GameMode.GrandPrix:
                grandPrixMode.Init(karts);
                break;
            case GameMode.DriftArena:
                driftArenaMode.Init(karts);
                break;
            case GameMode.TronTrails:
                tronTrailsMode.Init(karts);
                break;
            case GameMode.LastOneLit:
                lastOneLitMode.Init(karts);
                break;
            default:
                infectedMode.Init(karts);
                break;
        }
    }

    void FixedUpdate()
    {
        if (karts == null) return;

        const float minSep = WaypointCircuit.KartRadius * 2f;
        for (int i = 0; i < karts.Length; i++)
        for (int j = i + 1; j < karts.Length; j++)
        {
            if (karts[i] == null || karts[j] == null) continue;

            Vector3 delta = karts[j].transform.position - karts[i].transform.position;
            delta.y = 0f;
            float dist = delta.magnitude;

            if (dist < minSep && dist > 0.001f)
            {
                if (gameMode == GameMode.BattleRoyale && battleRoyaleMode != null)
                    battleRoyaleMode.OnKartImpact(karts[i], karts[j], ImpactSpeed(karts[i], karts[j], delta.normalized));

                Vector3 push = delta.normalized * (minSep - dist) * 0.5f;
                karts[i].NudgePosition(-push);
                karts[j].NudgePosition(push);
            }
        }
    }

    static float ImpactSpeed(KartController a, KartController b, Vector3 normal)
    {
        Vector3 aVelocity = HeadingVelocity(a);
        Vector3 bVelocity = HeadingVelocity(b);
        float closingSpeed = Vector3.Dot(aVelocity - bVelocity, normal);
        return Mathf.Abs(closingSpeed);
    }

    static Vector3 HeadingVelocity(KartController kart)
    {
        if (kart == null) return Vector3.zero;

        return new Vector3(
            Mathf.Cos(kart.heading) * kart.speed,
            0f,
            Mathf.Sin(kart.heading) * kart.speed);
    }

    KartController[] SpawnKarts()
    {
        int count = Mathf.Max(2, kartCount);
        playerKartIndex = Mathf.Clamp(playerKartIndex, 0, count - 1);

        var result = new KartController[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = circuit.SpawnPosition(i, count);
            float heading = circuit.SpawnHeading();

            GameObject go = kartPrefab != null
                ? Instantiate(kartPrefab, pos, Quaternion.identity)
                : CreateRuntimeKartObject();

            go.name = $"Kart_{i}";

            var kc = go.GetComponent<KartController>();
            if (kc == null) kc = go.AddComponent<KartController>();
            if (go.GetComponent<KartAI>() == null) go.AddComponent<KartAI>();
            if (go.GetComponent<KartVisualEffects>() == null) go.AddComponent<KartVisualEffects>();

            kc.kartIndex = i;
            kc.isPlayer = i == playerKartIndex;
            kc.kartColor = KartColors[i % KartColors.Length];
            kc.controlsEnabled = true;
            kc.maxSpeedMul = 1f;
            kc.steerMul = 1f;
            kc.SetVisualColor(kc.kartColor);
            kc.Teleport(pos, heading);

            result[i] = kc;
        }

        return result;
    }

    static GameObject CreateRuntimeKartObject()
    {
        GameObject go = new GameObject("Kart");
        var collider = go.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.24f, 0f);
        collider.size = new Vector3(0.95f, 0.5f, 1.45f);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var bodyMat = RuntimeMaterials.Make(Color.white);
        var glassMat = RuntimeMaterials.Emissive(new Color(0.04f, 0.18f, 0.28f), new Color(0.02f, 0.55f, 0.9f), 1.3f);
        var tireMat = RuntimeMaterials.Make(new Color(0.012f, 0.012f, 0.016f));
        var rimMat = RuntimeMaterials.Make(new Color(0.62f, 0.64f, 0.68f));

        AddKartCube(go.transform, "Body", new Vector3(0f, 0.28f, 0f), new Vector3(0.86f, 0.34f, 1.42f), bodyMat);
        AddKartCube(go.transform, "Nose", new Vector3(0f, 0.22f, 0.75f), new Vector3(0.62f, 0.24f, 0.34f), bodyMat);
        AddKartCube(go.transform, "Cockpit", new Vector3(0f, 0.58f, -0.08f), new Vector3(0.52f, 0.32f, 0.48f), glassMat);
        AddKartCube(go.transform, "Rear Spoiler", new Vector3(0f, 0.62f, -0.78f), new Vector3(1.02f, 0.12f, 0.22f), bodyMat);

        AddWheel(go.transform, "Wheel Front Left", new Vector3(-0.53f, 0.17f, 0.45f), tireMat, rimMat);
        AddWheel(go.transform, "Wheel Front Right", new Vector3(0.53f, 0.17f, 0.45f), tireMat, rimMat);
        AddWheel(go.transform, "Wheel Rear Left", new Vector3(-0.53f, 0.17f, -0.48f), tireMat, rimMat);
        AddWheel(go.transform, "Wheel Rear Right", new Vector3(0.53f, 0.17f, -0.48f), tireMat, rimMat);

        go.AddComponent<KartController>();
        go.AddComponent<KartAI>();
        go.AddComponent<KartVisualEffects>();
        return go;
    }

    static void AddKartCube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;

        var renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = material;

        var collider = part.GetComponent<Collider>();
        if (collider != null)
            UnityObjectUtil.Destroy(collider);
    }

    static void AddWheel(Transform parent, string name, Vector3 localPosition, Material tireMaterial, Material rimMaterial)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.18f, 0.13f, 0.18f);

        var renderer = wheel.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = tireMaterial;

        var collider = wheel.GetComponent<Collider>();
        if (collider != null)
            UnityObjectUtil.Destroy(collider);

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = name + " Rim";
        rim.transform.SetParent(parent, false);
        rim.transform.localPosition = localPosition + new Vector3(localPosition.x < 0f ? -0.006f : 0.006f, 0f, 0f);
        rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rim.transform.localScale = new Vector3(0.105f, 0.14f, 0.105f);

        var rimRenderer = rim.GetComponent<Renderer>();
        if (rimRenderer != null)
            rimRenderer.material = rimMaterial;

        var rimCollider = rim.GetComponent<Collider>();
        if (rimCollider != null)
            UnityObjectUtil.Destroy(rimCollider);
    }
}
