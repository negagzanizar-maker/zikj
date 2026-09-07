using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DriftArenaMode : MonoBehaviour
{
    public static DriftArenaMode I { get; private set; }

    [Header("Rules")]
    public float roundDuration = 180f;
    public float hotZoneDuration = 30f;
    public float hotZoneArc = 0.25f;
    public float comboDecay = 1.5f;
    public float driftThreshold = 0.4f;
    public float minDriftSpeed = 4.2f;
    public float scoreScale = 18f;

    class KartState
    {
        public float score;
        public float combo = 1f;
        public float lastDriftAt = -99f;
        public float driftAngle;
        public bool drifting;
        public bool inHotZone;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();
    readonly List<GameObject> hotZoneMarkers = new();

    float roundTimer;
    float hotZoneT;
    bool running;

    public float RoundTimer => roundTimer;
    public bool IsRunning => running;
    public int TotalKarts => karts?.Length ?? 0;

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
        CleanupHotZoneMarkers();

        karts = allKarts;
        states.Clear();
        roundTimer = Mathf.Max(10f, roundDuration);
        hotZoneT = Random.value;
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("DriftArenaMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;

            states[kart.kartIndex] = new KartState();
            kart.controlsEnabled = true;
            kart.maxSpeedMul = 1f;
            kart.steerMul = 1f;
            kart.SetVisualColor(kart.kartColor);
            kart.GetComponent<KartVisualEffects>()?.SetState(false, false);

            var ai = kart.GetComponent<KartAI>();
            if (ai != null)
            {
                ai.enabled = !kart.isPlayer;
                ai.behaviour = KartAI.Behaviour.FollowTrack;
                ai.target = null;
            }
        }

        CreateHotZoneMarkers();
        DriftArenaHUD.I?.Refresh();
    }

    void Update()
    {
        UpdateHotZoneMarkers();
        if (!running) return;

        float dt = Time.deltaTime;
        float now = Time.time;

        roundTimer -= dt;
        hotZoneT = Mathf.Repeat(hotZoneT + dt / Mathf.Max(1f, hotZoneDuration), 1f);

        foreach (var kart in karts)
            UpdateKartScore(kart, dt, now);

        DriftArenaHUD.I?.Refresh();

        if (roundTimer <= 0f)
            EndRound();
    }

    void UpdateKartScore(KartController kart, float dt, float now)
    {
        if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) return;

        state.inHotZone = IsInHotZone(kart);

        float angularSpeed = Mathf.Abs(kart.angVel);
        state.drifting = kart.speed > minDriftSpeed && angularSpeed > driftThreshold;
        state.driftAngle = state.drifting ? angularSpeed * Mathf.Rad2Deg : 0f;

        kart.steerMul = !kart.isPlayer && state.inHotZone ? 1.18f : 1f;

        if (!state.drifting)
        {
            if (now - state.lastDriftAt > comboDecay)
                state.combo = 1f;

            if (!state.inHotZone)
                kart.SetVisualColor(kart.kartColor);
            return;
        }

        if (now - state.lastDriftAt > comboDecay)
            state.combo = 1f;
        else
            state.combo = Mathf.Min(8f, state.combo + dt * 0.5f);

        state.lastDriftAt = now;
        state.score += angularSpeed * kart.speed * dt * scoreScale * state.combo * (state.inHotZone ? 3f : 1f);

        kart.SetVisualColor(state.inHotZone
            ? new Color(1f, 0.54f, 0.10f)
            : new Color(0.84f, 0.30f, 1f));
    }

    public bool IsInHotZone(KartController kart)
    {
        if (kart == null || WaypointCircuit.I == null) return false;

        int nearest = WaypointCircuit.I.Nearest(kart.transform.position);
        float t = (float)nearest / WaypointCircuit.Count;
        float diff = Mathf.Abs(t - hotZoneT);
        if (diff > 0.5f) diff = 1f - diff;

        return diff <= hotZoneArc * 0.5f;
    }

    void CreateHotZoneMarkers()
    {
        Material material = RuntimeMaterials.Emissive(
            new Color(1f, 0.45f, 0.04f),
            new Color(1f, 0.62f, 0.04f),
            2.8f);

        for (int i = 0; i < 13; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Drift Hot Zone Marker {i}";
            marker.transform.localScale = new Vector3(WaypointCircuit.HalfWidth * 0.24f, 0.018f, WaypointCircuit.HalfWidth * 0.24f);

            var collider = marker.GetComponent<Collider>();
            if (collider != null) UnityObjectUtil.Destroy(collider);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = material;

            hotZoneMarkers.Add(marker);
        }
    }

    void UpdateHotZoneMarkers()
    {
        if (hotZoneMarkers.Count == 0) return;

        int count = hotZoneMarkers.Count;
        for (int i = 0; i < count; i++)
        {
            float offset = Mathf.Lerp(-hotZoneArc * 0.5f, hotZoneArc * 0.5f, count == 1 ? 0.5f : (float)i / (count - 1));
            Vector3 pos = WaypointCircuit.At(hotZoneT + offset);
            pos.y = 0.035f;
            hotZoneMarkers[i].transform.position = pos;
            hotZoneMarkers[i].SetActive(true);
        }
    }

    void CleanupHotZoneMarkers()
    {
        for (int i = hotZoneMarkers.Count - 1; i >= 0; i--)
            if (hotZoneMarkers[i] != null) UnityObjectUtil.Destroy(hotZoneMarkers[i]);

        hotZoneMarkers.Clear();
    }

    void EndRound()
    {
        if (!running) return;

        running = false;
        foreach (var kart in karts)
        {
            if (kart == null) continue;
            kart.controlsEnabled = false;
            kart.steerMul = 1f;
            kart.StopImmediately();
        }

        DriftArenaHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "Final scores unavailable";

        var winner = ranking[0];
        var sb = new StringBuilder();
        sb.Append("Drift Arena Winner: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - {Score(kart.kartIndex):F0} pts");
        }

        return sb.ToString();
    }

    List<KartController> BuildRanking()
    {
        var ranking = new List<KartController>();
        if (karts == null) return ranking;

        ranking.AddRange(karts);
        ranking.RemoveAll(k => k == null || !states.ContainsKey(k.kartIndex));
        ranking.Sort((a, b) => Score(b.kartIndex).CompareTo(Score(a.kartIndex)));
        return ranking;
    }

    public float Score(int idx) => states.TryGetValue(idx, out var state) ? state.score : 0f;
    public float Combo(int idx) => states.TryGetValue(idx, out var state) ? state.combo : 1f;
    public float DriftAngle(int idx) => states.TryGetValue(idx, out var state) ? state.driftAngle : 0f;
    public bool IsDrifting(int idx) => states.TryGetValue(idx, out var state) && state.drifting;
    public bool PlayerInHotZone(int idx) => states.TryGetValue(idx, out var state) && state.inHotZone;

    public int PlaceOf(int idx)
    {
        List<KartController> ranking = BuildRanking();
        for (int i = 0; i < ranking.Count; i++)
            if (ranking[i].kartIndex == idx) return i + 1;

        return Mathf.Max(1, ranking.Count);
    }
}
