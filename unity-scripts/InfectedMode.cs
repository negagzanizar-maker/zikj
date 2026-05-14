using System.Collections.Generic;
using UnityEngine;

// Ported 1-to-1 from modes/infected.html rules
public class InfectedMode : MonoBehaviour
{
    public static InfectedMode I { get; private set; }

    [Header("Rules (match browser prototype)")]
    public float roundDuration    = 180f;
    public float infectionRadius  = 36  * 0.05f;   // 1.8 u
    public float boostFactor      = 1.10f;
    public float boostTime        = 5f;
    public float immunityTime     = 3f;
    public float zoneLifetime     = 8f;
    public float zoneRadius       = 38  * 0.05f;   // 1.9 u
    public float zoneSpawnMin     = 12f;
    public float zoneSpawnMax     = 20f;

    [Header("References")]
    public GameObject safeZonePrefab;

    // ── Per-kart state ───────────────────────────────────────────────────────

    class KartState
    {
        public bool  infected;
        public float immuneUntil;
        public float boostUntil;
        public float totalInfTime;
    }

    KartController[]           karts;
    readonly Dictionary<int, KartState> state = new();

    float roundTimer;
    bool  running;
    float nextZoneSpawn;
    readonly List<GameObject> zones = new();

    void Awake() => I = this;

    // ── Init (called by GameManager.Start) ───────────────────────────────────

    public void Init(KartController[] allKarts)
    {
        karts = allKarts;
        state.Clear();
        foreach (var k in karts)
            state[k.kartIndex] = new KartState();

        // One random kart starts infected
        Infect(karts[Random.Range(0, karts.Length)].kartIndex, -1);

        roundTimer    = roundDuration;
        running       = true;
        nextZoneSpawn = Time.time + Random.Range(zoneSpawnMin, zoneSpawnMax);
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!running) return;

        roundTimer -= Time.deltaTime;
        if (roundTimer <= 0f) { EndRound(); return; }

        float now = Time.time;

        // Accumulate infection time + apply boost modifier
        foreach (var k in karts)
        {
            var s = state[k.kartIndex];
            if (s.infected)           s.totalInfTime += Time.deltaTime;
            k.maxSpeedMul = (now < s.boostUntil) ? boostFactor : 1f;
        }

        // O(n²) infection proximity check
        foreach (var a in karts)
        {
            if (!state[a.kartIndex].infected) continue;
            foreach (var b in karts)
            {
                if (b == a) continue;
                var bs = state[b.kartIndex];
                if (bs.infected || now < bs.immuneUntil) continue;
                if (Vector3.Distance(a.transform.position, b.transform.position) < infectionRadius)
                    Infect(b.kartIndex, a.kartIndex);
            }
        }

        // Safe zone spawning
        if (now >= nextZoneSpawn)
        {
            SpawnZone();
            nextZoneSpawn = now + Random.Range(zoneSpawnMin, zoneSpawnMax);
        }

        UpdateAI();
    }

    // ── Infection / immunity ─────────────────────────────────────────────────

    void Infect(int victim, int infectorIdx)
    {
        var vs      = state[victim];
        vs.infected = true;
        if (infectorIdx >= 0)
            state[infectorIdx].boostUntil = Time.time + boostTime;
        InfectedHUD.I?.Refresh();
    }

    public void GrantImmunity(int kartIdx)
    {
        var s        = state[kartIdx];
        s.infected   = false;
        s.immuneUntil = Time.time + immunityTime;
        InfectedHUD.I?.Refresh();
    }

    // ── Safe zones ───────────────────────────────────────────────────────────

    void SpawnZone()
    {
        if (safeZonePrefab == null) return;
        Vector3 pos = WaypointCircuit.At(Random.value);
        var go = Instantiate(safeZonePrefab, pos, Quaternion.identity);
        go.GetComponent<SafeZone>().Init(this, zoneRadius, zoneLifetime);
        zones.Add(go);
    }

    public void RemoveZone(GameObject go) => zones.Remove(go);

    // ── AI behaviour update ──────────────────────────────────────────────────

    void UpdateAI()
    {
        foreach (var k in karts)
        {
            if (k.isPlayer) continue;
            var ai = k.GetComponent<KartAI>();
            if (ai == null) continue;

            bool inf = state[k.kartIndex].infected;
            bool imm = Time.time < state[k.kartIndex].immuneUntil;

            if (inf)
            {
                // Chase nearest clean kart
                ai.behaviour = KartAI.Behaviour.Chase;
                ai.target    = NearestClean(k)?.transform;
                if (ai.target == null) ai.behaviour = KartAI.Behaviour.FollowTrack;
            }
            else if (imm)
            {
                ai.behaviour = KartAI.Behaviour.FollowTrack;
            }
            else
            {
                // Flee nearest infected if it's close; else follow track
                var nearInf = NearestInfected(k);
                float dist  = nearInf != null
                    ? Vector3.Distance(k.transform.position, nearInf.transform.position)
                    : float.MaxValue;

                if (nearInf != null && dist < 5f)
                {
                    ai.behaviour = KartAI.Behaviour.Flee;
                    ai.target    = nearInf.transform;
                }
                else
                {
                    ai.behaviour = KartAI.Behaviour.FollowTrack;
                }
            }
        }
    }

    KartController NearestClean(KartController self)
    {
        KartController best = null; float bestD = float.MaxValue;
        foreach (var k in karts)
        {
            if (k == self || state[k.kartIndex].infected) continue;
            float d = Vector3.Distance(self.transform.position, k.transform.position);
            if (d < bestD) { bestD = d; best = k; }
        }
        return best;
    }

    KartController NearestInfected(KartController self)
    {
        KartController best = null; float bestD = float.MaxValue;
        foreach (var k in karts)
        {
            if (k == self || !state[k.kartIndex].infected) continue;
            float d = Vector3.Distance(self.transform.position, k.transform.position);
            if (d < bestD) { bestD = d; best = k; }
        }
        return best;
    }

    // ── Round end ────────────────────────────────────────────────────────────

    void EndRound()
    {
        running = false;
        InfectedHUD.I?.ShowResult(Winner());
    }

    string Winner()
    {
        KartController cleanWinner   = null;
        KartController leastInfected = null;
        float          leastTime     = float.MaxValue;

        foreach (var k in karts)
        {
            var s = state[k.kartIndex];
            if (!s.infected)  { cleanWinner = k; break; }
            if (s.totalInfTime < leastTime) { leastTime = s.totalInfTime; leastInfected = k; }
        }

        if (cleanWinner   != null) return $"Kart {cleanWinner.kartIndex} wins — stayed clean!";
        if (leastInfected != null) return $"Kart {leastInfected.kartIndex} wins — least infected ({leastTime:F1}s)";
        return "Draw";
    }

    // ── Public accessors (for HUD) ───────────────────────────────────────────

    public float RoundTimer  => roundTimer;
    public int   TotalKarts  => karts?.Length ?? 0;
    public int   CleanKarts  { get { int c = 0; foreach (var s in state.Values) if (!s.infected) c++; return c; } }
    public bool  IsInfected(int idx) => state.TryGetValue(idx, out var s) && s.infected;
    public bool  IsImmune(int idx)   => state.TryGetValue(idx, out var s) && Time.time < s.immuneUntil;
}
