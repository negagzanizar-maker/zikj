using UnityEngine;

// Top-level coordinator: spawns karts and starts the game mode.
public class GameManager : MonoBehaviour
{
    [Header("Kart Setup")]
    public GameObject kartPrefab;
    public int        kartCount        = 6;
    public int        playerKartIndex  = 0;

    [Header("References")]
    public WaypointCircuit circuit;
    public InfectedMode    infectedMode;

    static readonly Color[] Colors =
    {
        new Color(0.26f, 0.84f, 1.00f),  // cyan
        new Color(1.00f, 0.82f, 0.26f),  // yellow
        new Color(1.00f, 0.26f, 0.84f),  // pink
        new Color(0.26f, 1.00f, 0.53f),  // green
        new Color(0.82f, 0.26f, 1.00f),  // purple
        new Color(1.00f, 0.53f, 0.26f),  // orange
    };

    KartController[] karts;

    void Start()
    {
        karts = SpawnKarts();
        infectedMode.Init(karts);
    }

    // Called every frame to resolve kart overlap (soft push, no rigid-body collision needed)
    void FixedUpdate()
    {
        if (karts == null) return;
        const float MIN_SEP = WaypointCircuit.KartRadius * 2f;
        for (int i = 0; i < karts.Length; i++)
        for (int j = i + 1; j < karts.Length; j++)
        {
            Vector3 delta = karts[j].transform.position - karts[i].transform.position;
            delta.y       = 0f;
            float dist    = delta.magnitude;
            if (dist < MIN_SEP && dist > 0.001f)
            {
                Vector3 push = delta.normalized * (MIN_SEP - dist) * 0.5f;
                karts[i].NudgePosition(-push);
                karts[j].NudgePosition( push);
            }
        }
    }

    KartController[] SpawnKarts()
    {
        var result = new KartController[kartCount];
        for (int i = 0; i < kartCount; i++)
        {
            Vector3 pos     = circuit.SpawnPosition(i, kartCount);
            float   heading = circuit.SpawnHeading();

            var go  = Instantiate(kartPrefab, pos, Quaternion.identity);
            go.name = $"Kart_{i}";

            var kc          = go.GetComponent<KartController>();
            kc.kartIndex    = i;
            kc.isPlayer     = (i == playerKartIndex);
            kc.kartColor    = Colors[i % Colors.Length];
            kc.Teleport(pos, heading);

            // Tint the kart mesh
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var mat   = new Material(rend.sharedMaterial);
                mat.color = kc.kartColor;
                rend.material = mat;
            }

            result[i] = kc;
        }
        return result;
    }
}
