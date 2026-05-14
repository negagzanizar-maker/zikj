using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Kart Setup")]
    public GameObject kartPrefab;
    [Range(2, 12)] public int kartCount = 6;
    public int playerKartIndex = 0;

    [Header("References")]
    public WaypointCircuit circuit;
    public InfectedMode infectedMode;

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

        if (circuit == null)
        {
            var circuitGo = new GameObject("WaypointCircuit");
            circuit = circuitGo.AddComponent<WaypointCircuit>();
        }

        if (infectedMode == null)
            infectedMode = gameObject.AddComponent<InfectedMode>();

        karts = SpawnKarts();
        infectedMode.Init(karts);
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
                Vector3 push = delta.normalized * (minSep - dist) * 0.5f;
                karts[i].NudgePosition(-push);
                karts[j].NudgePosition(push);
            }
        }
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

            kc.kartIndex = i;
            kc.isPlayer = i == playerKartIndex;
            kc.kartColor = KartColors[i % KartColors.Length];
            kc.SetVisualColor(kc.kartColor);
            kc.Teleport(pos, heading);

            result[i] = kc;
        }

        return result;
    }

    static GameObject CreateRuntimeKartObject()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(1.2f, 0.35f, 0.7f);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        go.AddComponent<KartController>();
        go.AddComponent<KartAI>();
        return go;
    }
}
