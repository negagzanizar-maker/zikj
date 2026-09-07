using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LastOneLitMode : MonoBehaviour
{
    public static LastOneLitMode I { get; private set; }

    [Header("Rules")]
    public float roundDuration = 180f;
    public float startingLightRadius = 9f;
    public float eliminationRadius = 0.9f;
    public float shrinkRate = 0.6f;
    public float overlapMultiplier = 3.5f;

    class KartState
    {
        public float lightRadius;
        public bool alive = true;
        public float eliminatedAt;
        public GameObject ring;
        public Light light;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();

    float roundTimer;
    bool running;

    public float RoundTimer => roundTimer;
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
        CleanupLights();

        karts = allKarts;
        states.Clear();
        roundTimer = Mathf.Max(10f, roundDuration);
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("LastOneLitMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;

            states[kart.kartIndex] = new KartState
            {
                lightRadius = Mathf.Max(eliminationRadius + 0.5f, startingLightRadius)
            };

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

        CreateLightVisuals();
        LastOneLitHUD.I?.Refresh();
    }

    void Update()
    {
        UpdateLightVisuals();
        if (!running) return;

        float dt = Time.deltaTime;
        roundTimer -= dt;

        UpdateAI();
        ShrinkLights(dt);
        LastOneLitHUD.I?.Refresh();

        if (roundTimer <= 0f || AliveKarts <= 1)
            EndRound();
    }

    void ShrinkLights(float dt)
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (!state.alive) continue;

            bool overlappingLight = false;
            foreach (var other in karts)
            {
                if (other == null || other == kart || !states.TryGetValue(other.kartIndex, out var otherState)) continue;
                if (!otherState.alive) continue;

                float dist = Vector3.Distance(kart.transform.position, other.transform.position);
                if (dist < otherState.lightRadius)
                {
                    overlappingLight = true;
                    break;
                }
            }

            float shrink = shrinkRate * (overlappingLight ? overlapMultiplier : 1f);
            state.lightRadius -= shrink * dt;
            if (state.lightRadius <= eliminationRadius)
                Eliminate(kart, state);
        }
    }

    void UpdateAI()
    {
        foreach (var kart in karts)
        {
            if (kart == null || kart.isPlayer || !IsAlive(kart.kartIndex)) continue;

            var ai = kart.GetComponent<KartAI>();
            if (ai == null) continue;

            KartController threat = NearestOverlappingLight(kart);
            if (threat != null)
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

    KartController NearestOverlappingLight(KartController self)
    {
        KartController best = null;
        float bestSq = float.MaxValue;

        foreach (var kart in karts)
        {
            if (kart == null || kart == self || !states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (!state.alive) continue;

            float sq = (kart.transform.position - self.transform.position).sqrMagnitude;
            if (sq < state.lightRadius * state.lightRadius && sq < bestSq)
            {
                bestSq = sq;
                best = kart;
            }
        }

        return best;
    }

    void Eliminate(KartController kart, KartState state)
    {
        state.alive = false;
        state.lightRadius = 0f;
        state.eliminatedAt = roundDuration - roundTimer;

        kart.controlsEnabled = false;
        kart.maxSpeedMul = 0f;
        kart.steerMul = 0f;
        kart.StopImmediately();
        kart.SetVisualColor(new Color(0.08f, 0.08f, 0.10f));

        var ai = kart.GetComponent<KartAI>();
        if (ai != null) ai.enabled = false;
    }

    void CreateLightVisuals()
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"Spotlight Ring {kart.kartIndex}";
            ring.transform.position = kart.transform.position + Vector3.up * 0.045f;

            var collider = ring.GetComponent<Collider>();
            if (collider != null) UnityObjectUtil.Destroy(collider);

            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color baseColor = Color.Lerp(kart.kartColor, new Color(1f, 0.88f, 0.35f), 0.55f);
                renderer.material = RuntimeMaterials.Emissive(baseColor, new Color(1f, 0.84f, 0.25f), 1.8f);
            }

            GameObject lightGo = new GameObject($"Spotlight Light {kart.kartIndex}");
            lightGo.transform.SetParent(kart.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var point = lightGo.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.86f, 0.42f);
            point.shadows = LightShadows.None;

            state.ring = ring;
            state.light = point;
        }
    }

    void UpdateLightVisuals()
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;

            if (state.ring != null)
            {
                state.ring.SetActive(state.alive);
                state.ring.transform.position = kart.transform.position + Vector3.up * 0.045f;
                state.ring.transform.localScale = new Vector3(
                    Mathf.Max(0.01f, state.lightRadius * 2f),
                    0.014f,
                    Mathf.Max(0.01f, state.lightRadius * 2f));
            }

            if (state.light != null)
            {
                state.light.enabled = state.alive;
                state.light.range = Mathf.Max(1f, state.lightRadius * 1.15f);
                state.light.intensity = Mathf.Lerp(0.4f, 2.6f, LightPercent(kart.kartIndex));
            }
        }
    }

    void CleanupLights()
    {
        foreach (var state in states.Values)
        {
            if (state.ring != null) UnityObjectUtil.Destroy(state.ring);
            if (state.light != null) UnityObjectUtil.Destroy(state.light.gameObject);
        }

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

        LastOneLitHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "Lights out";

        var winner = ranking[0];
        var sb = new StringBuilder();
        sb.Append("Last One Lit Winner: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            string status = IsAlive(kart.kartIndex)
                ? $"{LightRadius(kart.kartIndex):F1}m light"
                : $"out at {EliminatedAt(kart.kartIndex):F1}s";
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - {status}");
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
            if (aAlive) return LightRadius(b.kartIndex).CompareTo(LightRadius(a.kartIndex));
            return EliminatedAt(b.kartIndex).CompareTo(EliminatedAt(a.kartIndex));
        });
        return ranking;
    }

    public bool IsAlive(int idx) => states.TryGetValue(idx, out var state) && state.alive;
    public float LightRadius(int idx) => states.TryGetValue(idx, out var state) ? state.lightRadius : 0f;
    public float LightPercent(int idx) => Mathf.Clamp01(LightRadius(idx) / Mathf.Max(0.01f, startingLightRadius));
    public float EliminatedAt(int idx) => states.TryGetValue(idx, out var state) ? state.eliminatedAt : 0f;

    public int PlaceOf(int idx)
    {
        List<KartController> ranking = BuildRanking();
        for (int i = 0; i < ranking.Count; i++)
            if (ranking[i].kartIndex == idx) return i + 1;

        return Mathf.Max(1, ranking.Count);
    }
}
