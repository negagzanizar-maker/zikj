using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TronTrailsMode : MonoBehaviour
{
    public static TronTrailsMode I { get; private set; }

    [Header("Rules")]
    public int startingLives = 3;
    public float freezeDuration = 2f;
    public float trailHitRadius = 0.42f;
    public int maxTrailPoints = 600;
    public int ownTrailSafePoints = 28;

    class KartState
    {
        public int lives;
        public bool alive = true;
        public float frozenUntil;
        public readonly List<Vector3> trail = new();
        public LineRenderer line;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();

    bool running;

    public bool IsRunning => running;
    public int TotalKarts => karts?.Length ?? 0;
    public int AliveKarts
    {
        get
        {
            int count = 0;
            foreach (var state in states.Values)
                if (state.alive) count++;
            return count;
        }
    }

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
        CleanupTrails();

        karts = allKarts;
        states.Clear();
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("TronTrailsMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;

            var state = new KartState
            {
                lives = Mathf.Max(1, startingLives),
                line = CreateTrailLine(kart)
            };
            state.trail.Add(TrailPoint(kart.transform.position));
            states[kart.kartIndex] = state;

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

        TronTrailsHUD.I?.Refresh();
    }

    void Update()
    {
        if (!running) return;

        float now = Time.time;

        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;

            if (!state.alive)
            {
                kart.controlsEnabled = false;
                continue;
            }

            bool frozen = now < state.frozenUntil;
            kart.controlsEnabled = !frozen;
            if (frozen)
            {
                kart.StopImmediately();
                kart.SetVisualColor(Color.white);
            }
            else
            {
                kart.SetVisualColor(kart.kartColor);
            }

            var ai = kart.GetComponent<KartAI>();
            if (ai != null)
                ai.enabled = !kart.isPlayer && !frozen;

            AppendTrail(kart, state);
            UpdateTrailLine(state);
        }

        CheckTrailHits(now);
        TronTrailsHUD.I?.Refresh();

        if (AliveKarts <= 1)
            EndRound();
    }

    void AppendTrail(KartController kart, KartState state)
    {
        Vector3 point = TrailPoint(kart.transform.position);
        if (state.trail.Count == 0 || Vector3.Distance(state.trail[state.trail.Count - 1], point) > 0.25f)
        {
            state.trail.Add(point);
            while (state.trail.Count > Mathf.Max(10, maxTrailPoints))
                state.trail.RemoveAt(0);
        }
    }

    void CheckTrailHits(float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var kartState)) continue;
            if (!kartState.alive || now < kartState.frozenUntil) continue;

            Vector3 pos = TrailPoint(kart.transform.position);

            foreach (var other in karts)
            {
                if (other == null || !states.TryGetValue(other.kartIndex, out var otherState)) continue;
                if (!otherState.alive || otherState.trail.Count < 2) continue;

                int end = otherState.trail.Count;
                if (other == kart)
                    end = Mathf.Max(0, end - ownTrailSafePoints);

                for (int i = 1; i < end; i++)
                {
                    if (!PointNearSegment(pos, otherState.trail[i - 1], otherState.trail[i], WaypointCircuit.KartRadius + trailHitRadius))
                        continue;

                    HitTrail(kart, kartState, now);
                    goto NextKart;
                }
            }

        NextKart:
            continue;
        }
    }

    void HitTrail(KartController kart, KartState state, float now)
    {
        state.lives--;
        kart.StopImmediately();

        if (state.lives <= 0)
        {
            state.alive = false;
            kart.controlsEnabled = false;
            kart.maxSpeedMul = 0f;
            kart.steerMul = 0f;
            kart.SetVisualColor(new Color(0.08f, 0.08f, 0.10f));

            var ai = kart.GetComponent<KartAI>();
            if (ai != null) ai.enabled = false;
        }
        else
        {
            state.frozenUntil = now + freezeDuration;
        }
    }

    static bool PointNearSegment(Vector3 point, Vector3 a, Vector3 b, float threshold)
    {
        Vector3 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f)
            return Vector3.Distance(point, a) <= threshold;

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lenSq);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(point, closest) <= threshold;
    }

    LineRenderer CreateTrailLine(KartController kart)
    {
        var go = new GameObject($"Trail_{kart.kartIndex}");
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = 0.16f;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = RuntimeMaterials.Emissive(kart.kartColor, kart.kartColor, 3f);
        return line;
    }

    static Vector3 TrailPoint(Vector3 pos)
    {
        pos.y = 0.11f;
        return pos;
    }

    static void UpdateTrailLine(KartState state)
    {
        if (state.line == null) return;

        state.line.positionCount = state.trail.Count;
        for (int i = 0; i < state.trail.Count; i++)
            state.line.SetPosition(i, state.trail[i]);
    }

    void CleanupTrails()
    {
        foreach (var state in states.Values)
            if (state.line != null) UnityObjectUtil.Destroy(state.line.gameObject);

        states.Clear();
    }

    void EndRound()
    {
        if (!running) return;

        running = false;
        foreach (var kart in karts)
        {
            if (kart == null) continue;
            kart.controlsEnabled = false;
            kart.StopImmediately();
        }

        TronTrailsHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "No surviving karts";

        var winner = ranking[0];
        var sb = new StringBuilder();
        sb.Append("Tron Trails Winner: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - {Lives(kart.kartIndex)} lives");
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
            bool aAlive = IsAlive(a.kartIndex);
            bool bAlive = IsAlive(b.kartIndex);

            if (aAlive != bAlive) return aAlive ? -1 : 1;
            return Lives(b.kartIndex).CompareTo(Lives(a.kartIndex));
        });
        return ranking;
    }

    public bool IsAlive(int idx) => states.TryGetValue(idx, out var state) && state.alive;
    public bool IsFrozen(int idx) => states.TryGetValue(idx, out var state) && state.alive && Time.time < state.frozenUntil;
    public float FrozenRemaining(int idx) => states.TryGetValue(idx, out var state) ? Mathf.Max(0f, state.frozenUntil - Time.time) : 0f;
    public int Lives(int idx) => states.TryGetValue(idx, out var state) ? state.lives : 0;
}
