using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class InfectedMode : MonoBehaviour
{
    public static InfectedMode I { get; private set; }

    [Header("Rules")]
    public float roundDuration = 180f;
    public float infectionRadius = 36f * 0.05f;
    public float boostFactor = 1.10f;
    public float boostTime = 5f;
    public float immunityTime = 3f;
    public float zoneLifetime = 8f;
    public float zoneRadius = 38f * 0.05f;
    public float zoneSpawnMin = 12f;
    public float zoneSpawnMax = 20f;

    [Header("References")]
    public GameObject safeZonePrefab;

    class KartState
    {
        public bool infected;
        public float immuneUntil;
        public float boostUntil;
        public float totalInfectedTime;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();
    readonly List<GameObject> zones = new();

    float roundTimer;
    bool running;
    float nextZoneSpawn;

    public float RoundTimer => roundTimer;
    public bool IsRunning => running;
    public int TotalKarts => karts?.Length ?? 0;
    public int CleanKarts
    {
        get
        {
            int count = 0;
            foreach (var s in states.Values)
                if (!s.infected) count++;
            return count;
        }
    }
    public int InfectedKarts => TotalKarts - CleanKarts;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    public void Init(KartController[] allKarts)
    {
        CleanupZones();

        karts = allKarts;
        states.Clear();
        roundTimer = Mathf.Max(10f, roundDuration);
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("InfectedMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;
            states[kart.kartIndex] = new KartState();
            kart.maxSpeedMul = 1f;
            kart.steerMul = 1f;
            kart.SetVisualColor(kart.kartColor);
            kart.GetComponent<KartVisualEffects>()?.SetState(false, false);
        }

        int patientZero = Random.Range(0, karts.Length);
        Infect(karts[patientZero].kartIndex, -1);
        nextZoneSpawn = Time.time + Random.Range(zoneSpawnMin, zoneSpawnMax);
        InfectedHUD.I?.Refresh();
    }

    void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        float now = Time.time;

        roundTimer -= dt;
        if (roundTimer <= 0f)
        {
            EndRound();
            return;
        }

        UpdateKartTimersAndVisuals(dt, now);
        CheckInfections(now);

        if (now >= nextZoneSpawn)
        {
            SpawnZone();
            nextZoneSpawn = now + Random.Range(zoneSpawnMin, zoneSpawnMax);
        }

        UpdateAI(now);
    }

    void UpdateKartTimersAndVisuals(float dt, float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;

            if (state.infected)
                state.totalInfectedTime += dt;

            kart.maxSpeedMul = now < state.boostUntil ? boostFactor : 1f;

            if (state.infected)
            {
                kart.SetVisualColor(new Color(1f, 0.12f, 0.18f));
                kart.GetComponent<KartVisualEffects>()?.SetState(true, false);
            }
            else if (now < state.immuneUntil)
            {
                kart.SetVisualColor(new Color(0.30f, 1f, 0.55f));
                kart.GetComponent<KartVisualEffects>()?.SetState(false, true);
            }
            else
            {
                kart.SetVisualColor(kart.kartColor);
                kart.GetComponent<KartVisualEffects>()?.SetState(false, false);
            }
        }
    }

    void CheckInfections(float now)
    {
        float radiusSq = infectionRadius * infectionRadius;

        foreach (var infector in karts)
        {
            if (infector == null || !IsInfected(infector.kartIndex)) continue;

            foreach (var victim in karts)
            {
                if (victim == null || victim == infector) continue;
                if (!states.TryGetValue(victim.kartIndex, out var victimState)) continue;
                if (victimState.infected || now < victimState.immuneUntil) continue;

                Vector3 delta = victim.transform.position - infector.transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude <= radiusSq)
                    Infect(victim.kartIndex, infector.kartIndex);
            }
        }
    }

    void Infect(int victimIdx, int infectorIdx)
    {
        if (!states.TryGetValue(victimIdx, out var victim)) return;
        if (victim.infected) return;

        victim.infected = true;
        victim.immuneUntil = 0f;

        if (infectorIdx >= 0 && states.TryGetValue(infectorIdx, out var infector))
            infector.boostUntil = Time.time + boostTime;

        InfectedHUD.I?.Refresh();
    }

    public void GrantImmunity(int kartIdx)
    {
        if (!running || !states.TryGetValue(kartIdx, out var state)) return;

        state.infected = false;
        state.immuneUntil = Time.time + immunityTime;
        EnsureInfectionExists(kartIdx);
        InfectedHUD.I?.Refresh();
    }

    void EnsureInfectionExists(int avoidKartIdx)
    {
        foreach (var state in states.Values)
        {
            if (state.infected) return;
        }

        KartController fallback = null;
        foreach (var kart in karts)
        {
            if (kart == null) continue;
            if (fallback == null) fallback = kart;
            if (kart.kartIndex == avoidKartIdx || IsImmune(kart.kartIndex)) continue;

            Infect(kart.kartIndex, -1);
            return;
        }

        if (fallback != null)
            Infect(fallback.kartIndex, -1);
    }

    void SpawnZone()
    {
        Vector3 pos = WaypointCircuit.At(Random.value);
        GameObject go = safeZonePrefab != null
            ? Instantiate(safeZonePrefab, pos, Quaternion.identity)
            : CreateRuntimeSafeZone(pos);

        if (!go.TryGetComponent(out SafeZone zone))
            zone = go.AddComponent<SafeZone>();

        zone.Init(this, zoneRadius, zoneLifetime);
        zones.Add(go);
    }

    GameObject CreateRuntimeSafeZone(Vector3 pos)
    {
        var root = new GameObject("SafeZone");
        root.transform.position = pos;
        root.AddComponent<SphereCollider>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Safe Zone Ring";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        visual.transform.localScale = new Vector3(zoneRadius * 2.2f, 0.04f, zoneRadius * 2.2f);

        var collider = visual.GetComponent<Collider>();
        if (collider != null) UnityObjectUtil.Destroy(collider);

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = RuntimeMaterials.Emissive(new Color(0.10f, 1f, 0.42f), new Color(0.08f, 1f, 0.35f), 2.4f);

        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Safe Zone Beacon";
        pillar.transform.SetParent(root.transform, false);
        pillar.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        pillar.transform.localScale = new Vector3(0.22f, 1.35f, 0.22f);

        var pillarCollider = pillar.GetComponent<Collider>();
        if (pillarCollider != null) UnityObjectUtil.Destroy(pillarCollider);

        var pillarRenderer = pillar.GetComponent<Renderer>();
        if (pillarRenderer != null)
            pillarRenderer.material = RuntimeMaterials.Emissive(new Color(0.06f, 0.8f, 0.32f), new Color(0.02f, 1f, 0.45f), 3f);

        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Safe Zone Crown";
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        crown.transform.localScale = Vector3.one * 0.75f;

        var crownCollider = crown.GetComponent<Collider>();
        if (crownCollider != null) UnityObjectUtil.Destroy(crownCollider);

        var crownRenderer = crown.GetComponent<Renderer>();
        if (crownRenderer != null)
            crownRenderer.material = RuntimeMaterials.Emissive(new Color(0.12f, 1f, 0.5f), new Color(0.08f, 1f, 0.45f), 3.4f);

        var lightGo = new GameObject("Safe Zone Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);

        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.08f, 1f, 0.36f);
        light.range = 8f;
        light.intensity = 2.8f;
        light.shadows = LightShadows.None;

        root.AddComponent<SafeZoneVisual>();

        return root;
    }

    public void RemoveZone(GameObject go)
    {
        zones.Remove(go);
    }

    void CleanupZones()
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            if (zones[i] != null) UnityObjectUtil.Destroy(zones[i]);
        }

        zones.Clear();
    }

    void UpdateAI(float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || kart.isPlayer) continue;

            var ai = kart.GetComponent<KartAI>();
            if (ai == null) continue;

            bool infected = IsInfected(kart.kartIndex);
            bool immune = IsImmune(kart.kartIndex);

            if (infected)
            {
                ai.behaviour = KartAI.Behaviour.Chase;
                ai.target = NearestClean(kart, now)?.transform;
                if (ai.target == null) ai.behaviour = KartAI.Behaviour.FollowTrack;
            }
            else if (immune)
            {
                ai.behaviour = KartAI.Behaviour.FollowTrack;
                ai.target = null;
            }
            else
            {
                var threat = NearestInfected(kart);
                float dist = threat != null
                    ? Vector3.Distance(kart.transform.position, threat.transform.position)
                    : float.MaxValue;

                if (threat != null && dist < infectionRadius * 4f)
                {
                    ai.behaviour = KartAI.Behaviour.Flee;
                    ai.target = threat.transform;
                }
                else
                {
                    ai.behaviour = KartAI.Behaviour.FollowTrack;
                    ai.target = null;
                }
            }
        }
    }

    KartController NearestClean(KartController self, float now)
    {
        KartController best = null;
        float bestSq = float.MaxValue;

        foreach (var kart in karts)
        {
            if (kart == null || kart == self) continue;
            if (!states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (state.infected || now < state.immuneUntil) continue;

            float sq = (kart.transform.position - self.transform.position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = kart;
            }
        }

        return best;
    }

    KartController NearestInfected(KartController self)
    {
        KartController best = null;
        float bestSq = float.MaxValue;

        foreach (var kart in karts)
        {
            if (kart == null || kart == self || !IsInfected(kart.kartIndex)) continue;

            float sq = (kart.transform.position - self.transform.position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = kart;
            }
        }

        return best;
    }

    void EndRound()
    {
        if (!running) return;

        running = false;
        foreach (var kart in karts)
        {
            if (kart == null) continue;
            kart.StopImmediately();
            kart.maxSpeedMul = 1f;
            kart.steerMul = 1f;
        }

        InfectedHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "Draw";

        var winner = ranking[0];
        bool cleanWinner = !IsInfected(winner.kartIndex);

        var sb = new StringBuilder();
        sb.Append(cleanWinner ? "Winner: " : "Least infected: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            float time = states[kart.kartIndex].totalInfectedTime;
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - {time:F1}s infected");
        }

        return sb.ToString();
    }

    List<KartController> BuildRanking()
    {
        var ranking = new List<KartController>();
        if (karts == null) return ranking;

        ranking.AddRange(karts);
        ranking.RemoveAll(k => k == null || !states.ContainsKey(k.kartIndex));
        ranking.Sort((a, b) =>
        {
            bool aClean = !states[a.kartIndex].infected;
            bool bClean = !states[b.kartIndex].infected;

            if (aClean != bClean) return aClean ? -1 : 1;
            return states[a.kartIndex].totalInfectedTime.CompareTo(states[b.kartIndex].totalInfectedTime);
        });

        return ranking;
    }

    public bool IsInfected(int idx) => states.TryGetValue(idx, out var s) && s.infected;
    public bool IsImmune(int idx) => states.TryGetValue(idx, out var s) && Time.time < s.immuneUntil;
    public float InfectionTime(int idx) => states.TryGetValue(idx, out var s) ? s.totalInfectedTime : 0f;
}
