using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BattleRoyaleMode : MonoBehaviour
{
    public static BattleRoyaleMode I { get; private set; }

    [Header("Rules")]
    public float roundDuration = 180f;
    public float startingHearts = 3f;
    public float baseImpactDamage = 0.22f;
    public float impactSpeedDamage = 0.035f;
    public float maxDamagePerImpact = 0.75f;
    public float minDamageSpeed = 1.2f;
    public float impactCooldown = 0.55f;

    class KartState
    {
        public float hearts;
        public bool eliminated;
        public int hitsTaken;
        public int hitsLanded;
        public float lastHitTime = -999f;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();

    float roundTimer;
    bool running;

    public float RoundTimer => roundTimer;
    public float StartingHearts => Mathf.Max(1f, startingHearts);
    public bool IsRunning => running;
    public int TotalKarts => karts?.Length ?? 0;
    public int AliveKarts
    {
        get
        {
            int count = 0;
            foreach (var state in states.Values)
                if (!state.eliminated) count++;
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
        karts = allKarts;
        states.Clear();
        roundTimer = Mathf.Max(10f, roundDuration);
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("BattleRoyaleMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;

            states[kart.kartIndex] = new KartState
            {
                hearts = Mathf.Max(1f, startingHearts)
            };

            kart.controlsEnabled = true;
            kart.maxSpeedMul = 1f;
            kart.steerMul = 1f;
            kart.SetVisualColor(kart.kartColor);
            kart.GetComponent<KartVisualEffects>()?.SetState(false, false);

            var ai = kart.GetComponent<KartAI>();
            if (ai != null)
                ai.enabled = !kart.isPlayer;
        }

        BattleRoyaleHUD.I?.Refresh();
    }

    void Update()
    {
        if (!running) return;

        roundTimer -= Time.deltaTime;
        if (roundTimer <= 0f || AliveKarts <= 1)
        {
            EndRound();
            return;
        }

        UpdateAI();
        BattleRoyaleHUD.I?.Refresh();
    }

    public void OnKartImpact(KartController a, KartController b, float impactSpeed)
    {
        if (!running || a == null || b == null) return;
        if (impactSpeed < minDamageSpeed) return;
        if (!IsAlive(a.kartIndex) || !IsAlive(b.kartIndex)) return;

        float now = Time.time;
        
        if (!states.TryGetValue(a.kartIndex, out var aState) || !states.TryGetValue(b.kartIndex, out var bState))
            return;

        if (now - aState.lastHitTime < impactCooldown || now - bState.lastHitTime < impactCooldown)
            return;

        float damage = Mathf.Clamp(
            baseImpactDamage + impactSpeed * impactSpeedDamage,
            0.12f,
            maxDamagePerImpact);

        DamageKart(a, damage);
        DamageKart(b, damage);

        aState.hitsLanded++;
        bState.hitsLanded++;
        aState.lastHitTime = now;
        bState.lastHitTime = now;

        BattleRoyaleHUD.I?.Refresh();
    }

    void DamageKart(KartController kart, float damage)
    {
        if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) return;
        if (state.eliminated) return;

        state.hearts = Mathf.Max(0f, state.hearts - damage);
        state.hitsTaken++;

        if (state.hearts <= 0f)
            Eliminate(kart);
        else
            UpdateKartVisual(kart, state);
    }

    void Eliminate(KartController kart)
    {
        if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) return;
        if (state.eliminated) return;

        state.eliminated = true;
        state.hearts = 0f;

        kart.controlsEnabled = false;
        kart.maxSpeedMul = 0f;
        kart.steerMul = 0f;
        kart.StopImmediately();
        kart.SetVisualColor(new Color(0.09f, 0.09f, 0.10f));
        kart.GetComponent<KartVisualEffects>()?.SetState(false, false);

        var ai = kart.GetComponent<KartAI>();
        if (ai != null)
            ai.enabled = false;

        if (AliveKarts <= 1)
            EndRound();
    }

    void UpdateKartVisual(KartController kart, KartState state)
    {
        if (kart == null || state == null) return;

        float health01 = Mathf.Clamp01(state.hearts / Mathf.Max(1f, startingHearts));
        Color damaged = Color.Lerp(new Color(1f, 0.24f, 0.16f), kart.kartColor, health01);
        kart.SetVisualColor(damaged);
    }

    void UpdateAI()
    {
        foreach (var kart in karts)
        {
            if (kart == null || kart.isPlayer || !IsAlive(kart.kartIndex)) continue;

            var ai = kart.GetComponent<KartAI>();
            if (ai == null) continue;

            KartController target = NearestAliveOpponent(kart);
            if (target == null)
            {
                ai.behaviour = KartAI.Behaviour.FollowTrack;
                ai.target = null;
                continue;
            }

            if (GetHearts(kart.kartIndex) <= startingHearts * 0.35f &&
                Vector3.Distance(kart.transform.position, target.transform.position) < 5.5f)
            {
                ai.behaviour = KartAI.Behaviour.Flee;
                ai.target = target.transform;
            }
            else
            {
                ai.behaviour = KartAI.Behaviour.Chase;
                ai.target = target.transform;
            }
        }
    }

    KartController NearestAliveOpponent(KartController self)
    {
        KartController best = null;
        float bestSq = float.MaxValue;

        foreach (var kart in karts)
        {
            if (kart == null || kart == self || !IsAlive(kart.kartIndex)) continue;

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
            kart.controlsEnabled = false;
            kart.StopImmediately();

            var ai = kart.GetComponent<KartAI>();
            if (ai != null)
                ai.enabled = false;
        }

        BattleRoyaleHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "Draw";

        var winner = ranking[0];
        var sb = new StringBuilder();
        sb.Append("Battle Royale Winner: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - {GetHearts(kart.kartIndex):F1} hearts");
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
            return GetHearts(b.kartIndex).CompareTo(GetHearts(a.kartIndex));
        });

        return ranking;
    }

    public bool IsAlive(int idx) => states.TryGetValue(idx, out var state) && !state.eliminated;
    public float GetHearts(int idx) => states.TryGetValue(idx, out var state) ? state.hearts : 0f;
    public int HitsTaken(int idx) => states.TryGetValue(idx, out var state) ? state.hitsTaken : 0;
}
